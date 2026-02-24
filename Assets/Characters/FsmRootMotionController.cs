using Player;
using UnityEngine;
using UnityEngine.AI;

namespace Characters
{
    public class FsmRootMotionController : MonoBehaviour, ILockOnAble //make the enemy know that its locked on and tell player to stop locking on when enemy/object dies
    {
        //Animator parameters
        private static readonly int DodgeRoll = Animator.StringToHash("DodgeRoll"); //Add parameter to animator
        private static readonly int Speed = Animator.StringToHash("MoveSpeed");
        private static readonly int IsGrounded = Animator.StringToHash("isGrounded");
        private static readonly int AttackMomentum = Animator.StringToHash("AttackMomentum");
        private static readonly int Died = Animator.StringToHash("Death");
        private static readonly int Charging = Animator.StringToHash("Charging");

        // New - Animator parameters
        private static readonly int MoveX = Animator.StringToHash("MoveX");
        private static readonly int MoveZ = Animator.StringToHash("MoveZ");
        private static readonly int AttackType = Animator.StringToHash("AttackType");

        // used ONLY with state.tagHash,  tag field on Animator states (select a state -> Inspector -> "Tag" dropdown).
        private static readonly int AttackStateTag = Animator.StringToHash("Attack"); 
        
        // used for performing an attack
        private static readonly int AttackTrigger = Animator.StringToHash("AttackTrigger");  

        private static readonly int Blocking = Animator.StringToHash("Blocking");
        private static readonly int Looking = Animator.StringToHash("Looking");
        private static readonly int LightAttack = Animator.StringToHash("LightAttack");
        private static readonly int HeavyAttack = Animator.StringToHash("HeavyAttack");
        private static readonly int SpecialAttack1 = Animator.StringToHash("SpecialAttack1");
        private static readonly int SpecialAttack2 = Animator.StringToHash("SpecialAttack2");
        private static readonly int Damaged = Animator.StringToHash("Damaged");
        private static readonly int Rotate = Animator.StringToHash("Rotate");

        [Tooltip("Downward force intensity")]
        [SerializeField] private float gravityIntensity = -6f;
        [SerializeField] [Range(0.5f, 1f)]  private float nonCombatMovementSpeed;
        [SerializeField] [Range(0.5f, 1f)]  private float combatMovementSpeed;
        [SerializeField] private Transform lockOnIndicatorPosition;
        [SerializeField] private EnemyHitboxHandler mainHitbox;
        [SerializeField] private EnemyHitboxHandler secondaryHitbox;
        [SerializeField] private bool useBossHealthbar = false;
        
        //Privates
        private EnemyAudio m_audio;
        private EnemyFsm m_fsm;
        private NavMeshAgent m_navAgent;
        private Animator m_animator;
        private Transform m_lockOnTarget;
        private Vector3 m_playerVelocity;
        private bool m_groundedPlayer;
        private bool m_allowJump = true;
        private bool m_isDead;
        private bool m_canAttack = true;
        private bool m_inputWindowOpen;
        private float m_accumulatedAttackTime;
        private float m_attackTimer;
        private float m_targetMoveSpeed = 1f;
        private bool m_isLockedOnByPlayer;

        private bool m_pauseUpdate = false;
        private bool m_isStrafing = false;
        
        private float m_rotationSpeed = 10f;
        private float m_startMoveDeltaTimer;
        private float m_stopMoveDeltaTimer;
        private float m_currentMoveSpeed;
        private float m_moveX;
        private float m_moveZ;
        private float m_jumpTimer;
        private bool m_isCharging = false;
        private bool m_isBlocking = false;
        private Vector3 m_startLocation;
        private CharacterController m_characterController;
        private bool m_doSyncMovement = true;
        private PlayerController m_playerController;

        //Privates for navigation/root motion
        private Vector2 m_velocity;
        private Vector2 m_smoothDeltaPosition;
        private bool m_isBeingPushed = false;
        private float m_pushedStartTimeStamp = 0f;
        private EnemyStatus m_status;

        //Read-only
        private readonly float m_attackInputFrequency = 0.25f;

        //Publics
        [HideInInspector] public Vector3 moveVelocity;

        public void ToggleMainHitbox(int toggleState) //Called from animation
        {
            float dmgMultiplier = toggleState;
            
            if(toggleState == 0)
                mainHitbox.DoToggleHitbox(false, 1f);
            else
                mainHitbox.DoToggleHitbox(true, dmgMultiplier);
        }
        
        public void ToggleSecondHitbox(int toggleState) //Called from animation
        {
            float dmgMultiplier = toggleState;

            if(toggleState == 0)
                secondaryHitbox.DoToggleHitbox(false, 1f);
            else
                secondaryHitbox.DoToggleHitbox(true, dmgMultiplier);
        }

        public void OnDamaged()
        {
            m_animator.SetTrigger(Damaged);
        }

        public void AttackAoe() //From animation only for boss
        {
            //do cool attack thing here
        }

        public void DoStunned()
        {
            
        }
        
        public void StopStunned()
        {
            
        }
        
        public void SetIsBeingPushed(bool isBeingPushed)
        {
            m_isBeingPushed = isBeingPushed;
            m_pushedStartTimeStamp = Time.time;
        }

        void Awake()
        {
            m_playerController = FindAnyObjectByType<PlayerController>();
            m_animator = GetComponent<Animator>();
            m_animator.applyRootMotion = true;
            m_characterController = GetComponent<CharacterController>();
            m_navAgent = GetComponent<NavMeshAgent>();
            m_navAgent.updatePosition = false;
            m_navAgent.updateRotation = false;
            m_startLocation = transform.position;
            m_fsm = GetComponent<EnemyFsm>();
            m_audio = GetComponent<EnemyAudio>();
            m_status = GetComponent<EnemyStatus>();
        }
        
        void Update()
        {
            if (Time.timeScale == 0f)
            {
                return;
            }

            if (m_pauseUpdate)
                return;
            
            /*if(m_PlayerInputHandler.pauseAction.IsPressed())
                   OnGamePaused?.Invoke();*/ 
            MatchAgentAndAnimatorMovement();
        }

        public void PauseEnemy(bool pause)
        {
            if(pause)
            {
                m_animator.speed = 0f;
                m_pauseUpdate = true;
            }
            else
            {
                m_animator.speed = 1f;
                m_pauseUpdate = false;
            }
        }

        public void ClearAnimatorTriggers()
        {
            
        }
        
        public void DoIdle()
        {
            m_animator.SetFloat(Speed, 0f);
            m_animator.SetFloat(MoveX, 0f);
            m_animator.SetFloat(MoveZ, 0f);
        }

        public void DoMoveTo(Vector3 moveToPosition)
        {
            m_navAgent.destination = moveToPosition;
            m_navAgent.isStopped = false;
        }

        public void StopMoveTo()
        {
            m_navAgent.isStopped = true;
        }

        public void DoLightAttack()
        {
            m_doSyncMovement = false;
            m_animator.SetTrigger(LightAttack);
        }

        public void DoHeavyAttack()
        {
            m_doSyncMovement = false;
            m_animator.SetTrigger(HeavyAttack);
        }

        public void DoSpecialAttackOne()
        {
            m_doSyncMovement = false;
            m_animator.SetTrigger(SpecialAttack1);
        }
        
        public void DoSpecialAttackTwo()
        {
            m_doSyncMovement = false;
            m_animator.SetTrigger(SpecialAttack2);
        }

        public void OnDeath()
        {
            print("enemy " + gameObject.name + " died!!!! in CONTROLLER");
            m_animator.SetTrigger(Died);
            m_isDead = true;
            m_navAgent.enabled = false;
            m_characterController.enabled = false;
            m_audio.DeathSfx();
            
            if(m_isLockedOnByPlayer)
            {
                m_playerController.DisableLockOn();
            }
            
        }

        private void Dodge()
        {
            m_animator.SetTrigger(DodgeRoll);
        }

        private void Block()
        {
            if (m_isBlocking)
            {
                m_isBlocking = false;
                m_animator.SetBool(Blocking, m_isBlocking);
                return;
            }

            if (!m_isBlocking)
            {
                m_isBlocking = true;
                m_animator.SetBool(Blocking, m_isBlocking);
            }
        }

        private float GetMovementSpeedCap()
        {
            if (m_fsm.GetCurrentFsmMainState() != FsmEnemyStates.Combat)
                return nonCombatMovementSpeed;
            
            return combatMovementSpeed;
        }

        private Vector3 GetMovementTarget()
        {
            if(m_fsm.GetCurrentFsmMainState() == FsmEnemyStates.Combat)
            {
                if (m_animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
                {
                    if(m_animator.GetFloat(Rotate) > 0.5f)
                    {
                        return transform.position + transform.forward * 2f;
                    }
                }
                
                if (NavMesh.SamplePosition(m_playerController.transform.position, out var hit, m_navAgent.radius * 4f, NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }
            return m_navAgent.nextPosition;
        }

        private void MatchAgentAndAnimatorMovement()
        {
            
            if (m_isBeingPushed)
            {
                print("is pushed two");
                if (Time.time > m_pushedStartTimeStamp + 2f)
                {
                    m_isBeingPushed = false;
                    m_status.TakeDamage(100f, ElementTypes.ElementType.NoElement, false);
                }
                return;
            }
            
            if (m_fsm.GetCurrentFsmMainState() == FsmEnemyStates.Idle ||
                m_fsm.GetCurrentFsmMainState() == FsmEnemyStates.Stunned || m_isDead)
                return;
            
            Vector3 worldDeltaPosition = m_navAgent.nextPosition - transform.position;
            worldDeltaPosition.y = 0;
            
            float dx = Vector3.Dot(transform.right, worldDeltaPosition);
            float dy = Vector3.Dot(transform.forward, worldDeltaPosition);
            Vector2 deltaPosition = new Vector2(dx, dy);

            float smooth = Mathf.Min(0.5f, Time.deltaTime / 0.1f);
            m_smoothDeltaPosition = Vector2.Lerp(m_smoothDeltaPosition, deltaPosition, smooth);

            m_velocity = m_smoothDeltaPosition / Time.deltaTime;
            
            if (m_navAgent.remainingDistance <= m_navAgent.stoppingDistance)
            {
                m_velocity = Vector2.Lerp(Vector2.zero, m_velocity,
                    m_navAgent.remainingDistance / m_navAgent.stoppingDistance);
            }
            
            float deltaMultiplier = 0.05f;
            
            bool shouldMove = m_navAgent.remainingDistance > m_navAgent.stoppingDistance;
            
            if(shouldMove)
            {
                m_startMoveDeltaTimer = 0;
                m_stopMoveDeltaTimer += Time.deltaTime;
                m_currentMoveSpeed = Mathf.Lerp(m_currentMoveSpeed, m_targetMoveSpeed, m_stopMoveDeltaTimer * deltaMultiplier);
                m_currentMoveSpeed = Mathf.Clamp(m_currentMoveSpeed, 0f, 1f);
            }
            else
            {
                m_stopMoveDeltaTimer = 0;
                m_startMoveDeltaTimer += Time.deltaTime;
                m_currentMoveSpeed = Mathf.Lerp(m_currentMoveSpeed, 0f, m_startMoveDeltaTimer * deltaMultiplier);
            }

            
            m_currentMoveSpeed = Mathf.Clamp(m_currentMoveSpeed, 0f, GetMovementSpeedCap());
            
            m_animator.SetFloat(MoveX, m_velocity.x);
            m_animator.SetFloat(MoveZ, m_velocity.y);
            m_animator.SetFloat(Speed, m_currentMoveSpeed);
            
            //check if in attack state, and if true -> use closest position to player on navmesh instead of nextPosition!
            Vector3 relativePos = GetMovementTarget() - transform.position;
            Quaternion newRotation = Quaternion.LookRotation(relativePos, Vector3.up);
            newRotation.x = 0f;
            newRotation.z = 0f;
            transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, Time.deltaTime * m_rotationSpeed);
            
            //If the agent is too far away from the root position, correct it.
            float deltaMagnitude = worldDeltaPosition.magnitude;
            if (deltaMagnitude > m_navAgent.radius / 3f)
            {
                transform.position = Vector3.Lerp(m_animator.rootPosition,
                    m_navAgent.nextPosition, smooth);
            }
        }

        public void SetMovementSpeed(float newSpeed)
        {
            m_animator.SetFloat(Speed, newSpeed);
        }
        
        private void OnAnimatorMove() //Animator movement - root movement
        {
            if (m_isDead || m_isBeingPushed)
                return;
            
            Vector3 rootPosition = m_animator.rootPosition;
            rootPosition.y = m_navAgent.nextPosition.y;
            transform.position = rootPosition;
            m_navAgent.nextPosition = rootPosition;
            //From player controller beneath here
            var state = m_animator.GetCurrentAnimatorStateInfo(0);
            
            gravityIntensity = -8f;

            if (state.tagHash == AttackStateTag && !m_isDead)
            {
                Vector3 velocity = m_animator.deltaPosition;
                velocity += transform.forward * m_animator.GetFloat(AttackMomentum) * Time.deltaTime;
                velocity.y += gravityIntensity * Time.deltaTime;
                m_characterController.Move(velocity);
            }
        }
        
        // NOTE: camera is a placeholder for testing strafing,
        // later replace with something like (target.position - transform.position) projected onto plane.
        private Vector3 GetStrafeFacingDir()
        {
            Vector3 faceDir =
                Vector3.ProjectOnPlane(m_lockOnTarget.position - transform.position,
                    Vector3.up); //add lock on target here
            if (faceDir.sqrMagnitude < 1e-6f) return transform.forward;

            return faceDir.normalized;
        }

        public Transform GetLockOnTarget()
        {
            m_isLockedOnByPlayer = true;
            return lockOnIndicatorPosition;
        }

        public void StopBeingLockedOn()
        {
            m_isLockedOnByPlayer = false;
        }

        public bool UseBossHealthBar() //do it
        {
            return useBossHealthbar;
        }

        public ElementTypes.ElementType GetElementWeakness()
        {
            switch (m_status.GetSourceElement())
            {
                case ElementTypes.ElementType.Fire:
                    return ElementTypes.ElementType.Water;
                case ElementTypes.ElementType.Air:
                    return ElementTypes.ElementType.Fire;
                case ElementTypes.ElementType.Water:
                    return ElementTypes.ElementType.Air;
            }
            return ElementTypes.ElementType.NoElement;
        }
    }
}