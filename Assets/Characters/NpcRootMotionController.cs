using System;
using System.Collections;
using Input;
using Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

//Add DOTween package to use this, is then used to move the camera to death position
//using DG.Tweening;

namespace Characters
{
    public class NpcRootMotionController : MonoBehaviour, ILockOnAble //make the enemy know that its locked on and tell player to stop locking on when enemy/object dies
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

        public bool IsBusy => !m_doSyncMovement; // simple: when attacks/casts disable sync, treat as busy
        public bool IsComboWindowOpen => m_inputWindowOpen;
        public bool IsBlocking => m_isBlocking;

        private Vector2 m_moveIntent;
        private bool m_strafeMode;




        [Tooltip("Downward force intensity")]
        [SerializeField] private float gravityIntensity = -6f;
        [SerializeField] private Transform lockOnIndicatorPosition;
        [SerializeField] private EnemyHitboxHandler mainHitbox;
        [SerializeField] private EnemyHitboxHandler secondaryHitbox;


        //Privates
        private EnemyStatus m_status;
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
        private float m_targetMoveSpeed = 0.5f;
        private bool m_isLockedOnByPlayer;

        // will be used for lock-on movement. (true = rotate toward a facing direction. fales = rotate toward movement, almost never walks backwards) 
        private bool m_isStrafing = false;

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


        //Read-only
        private readonly float m_attackInputFrequency = 0.25f;

        //Publics
        [HideInInspector] public Vector3 moveVelocity;

        private void OnEnable()
        {
            ThirdPersonCamera.OnStopLockOn += StopStrafing;
        }

        private void OnDisable()
        {
            ThirdPersonCamera.OnStopLockOn -= StopStrafing;
        }


        public void ToggleMainHitbox(int toggleState, float damageMultiplier) //Called from animation BLORK
        {
            print("Toggle Box: " + toggleState);

            if (toggleState == 0)
                mainHitbox.DoToggleHitbox(false, 1f);
            else
                mainHitbox.DoToggleHitbox(true, damageMultiplier);
        }

        public void ToggleSecondHitbox(int toggleState, float damageMultiplier) //Called from animation
        {
            print("Toggle Box: " + toggleState);

            if (toggleState == 0)
                secondaryHitbox.DoToggleHitbox(false, 1f);
            else
                secondaryHitbox.DoToggleHitbox(true, damageMultiplier);
        }

        private void AnimEventFinished(bool lookDone, bool actionDone)
        {
            if (lookDone)
                m_animator.SetBool(Looking, false);

            if (actionDone)
                m_doSyncMovement = true;
        }


        public void OnDamaged()
        {
            m_animator.SetTrigger(Damaged);
        }


        private void ChargingFinished()
        {
            m_animator.SetBool(Charging, false);
        }

        private void StopStrafing()
        {
            EnableLockOnMode(false);
        }

        void Awake()
        {
            m_playerController = FindAnyObjectByType<PlayerController>();
            m_animator = GetComponent<Animator>();
            m_navAgent = GetComponent<NavMeshAgent>();
            m_navAgent.updatePosition = false;
            m_navAgent.updateRotation = true;
            m_startLocation = transform.position;
            m_characterController = GetComponent<CharacterController>();
            m_status = GetComponent<EnemyStatus>();
        }

        void Update()
        {
            if (Time.timeScale == 0f)
            {
                return;
            }

            /*if(m_PlayerInputHandler.pauseAction.IsPressed())
                   OnGamePaused?.Invoke();*/
            if (m_doSyncMovement)
                MatchAgentAndAnimatorMovement();

            if (m_isStrafing && m_lockOnTarget != null)
            {
                Vector3 faceDir = Vector3.ProjectOnPlane(m_lockOnTarget.position - transform.position, Vector3.up);
                if (faceDir.sqrMagnitude > 0.001f)
                {
                    Quaternion desired = Quaternion.LookRotation(faceDir.normalized, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, 720f * Time.deltaTime);
                }
            }

        }

        public void DoLookAround()
        {
            m_animator.SetBool(Looking, true);
        }

        public void DoBlock()
        {
            SetBlocking(true);
        }

        public void SetBlocking(bool isBlocking)
        {
            m_isBlocking = isBlocking;
            m_animator.SetBool(Blocking, isBlocking);
        }


        // public void DoLightAttack()
        // {
        //     m_doSyncMovement = false;
        //     m_animator.SetTrigger(LightAttack);
        // }

        // public void DoStrongAttack()
        // {
        //     m_doSyncMovement = false;
        //     m_animator.SetTrigger(HeavyAttack);
        // }

        // public void DoSpecialAttackOne()
        // {
        //     m_doSyncMovement = false;
        //     m_animator.SetTrigger(SpecialAttack1);
        // }

        // public void DoSpecialAttackTwo()
        // {
        //     m_doSyncMovement = false;
        //     m_animator.SetTrigger(SpecialAttack2);
        // }
        public void DoLightAttack()
        {
            TryLightAttack();
        }

        public void DoStrongAttack()
        {
            TryHeavyAttack();
        }

        public void SetLockOnTarget(Transform target)
        {
            m_lockOnTarget = target;
            bool enable = (m_lockOnTarget != null);

            m_isStrafing = enable;
            m_animator.SetBool(Animator.StringToHash("LockOn"), enable); // optional if you have it
        }
        public void SetMoveIntent(Vector2 move, bool strafeMode)
        {
            m_moveIntent = Vector2.ClampMagnitude(move, 1f);
            m_strafeMode = strafeMode;
        }

        public bool TryLightAttack()
        {
            if (m_isDead)
                return false;

            // Allow chaining if combo window open OR if not busy
            if (IsBusy && !m_inputWindowOpen)
                return false;

            m_doSyncMovement = false;
            m_animator.SetTrigger(LightAttack);
            return true;
        }

        public bool TryHeavyAttack()
        {
            if (m_isDead)
                return false;

            if (IsBusy)
                return false;

            m_doSyncMovement = false;
            m_animator.SetTrigger(HeavyAttack);
            return true;
        }

        public bool TryRoll()
        {
            if (m_isDead)
                return false;

            if (IsBusy)
                return false;

            m_doSyncMovement = false;
            m_animator.SetTrigger(DodgeRoll);
            return true;
        }

        public bool TryCastFireAoE()
        {
            if (m_isDead || IsBusy)
                return false;

            m_doSyncMovement = false;
            m_animator.SetTrigger(SpecialAttack1);
            return true;
        }

        public bool TryCastWaterHeal()
        {
            if (m_isDead || IsBusy)
                return false;

            m_doSyncMovement = false;
            m_animator.SetTrigger(SpecialAttack2);
            return true;
        }

        public bool TryCastWindPush()
        {
            if (m_isDead || IsBusy)
                return false;

            m_doSyncMovement = false;
            m_animator.SetTrigger(Animator.StringToHash("SpecialAttack3")); // if you have it
            return true;
        }

        public bool TryStartCharging()
        {
            if (m_isDead || IsBusy)
                return false;

            m_isCharging = true;
            m_doSyncMovement = false;
            m_animator.SetBool(Charging, true);
            return true;
        }

        public void StopCharging()
        {
            m_isCharging = false;
            m_animator.SetBool(Charging, false);
        }


        public void OnDeath()
        {
            m_animator.SetTrigger(Died);
            m_isDead = true;
            m_navAgent.enabled = false;
            m_characterController.enabled = false;

            if (m_isLockedOnByPlayer)
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

        private void MatchAgentAndAnimatorMovement()
        {
            if (!m_animator.GetCurrentAnimatorStateInfo(0).IsTag("Movement"))
                return;

            Vector3 worldDeltaPosition = m_navAgent.nextPosition - transform.position;
            worldDeltaPosition.y = 0;
            float dx = Vector3.Dot(transform.right, worldDeltaPosition);
            float dy = Vector3.Dot(transform.forward, worldDeltaPosition);
            Vector2 deltaPosition = new Vector2(dx, dy);

            float smooth = Mathf.Min(1f, Time.deltaTime / 0.1f);
            m_smoothDeltaPosition = Vector2.Lerp(m_smoothDeltaPosition, deltaPosition, smooth);

            m_velocity = m_smoothDeltaPosition / Time.deltaTime;

            if (m_navAgent.remainingDistance <= m_navAgent.stoppingDistance)
            {
                m_velocity = Vector2.Lerp(Vector2.zero, m_velocity,
                    m_navAgent.remainingDistance / m_navAgent.stoppingDistance);
            }

            bool shouldMove = m_navAgent.remainingDistance > m_navAgent.stoppingDistance;

            if (m_navAgent.isStopped)
                shouldMove = false;

            m_animator.SetFloat(MoveX, m_velocity.x);
            m_animator.SetFloat(MoveZ, m_velocity.y);

            float speedPercentage = m_navAgent.speed / m_navAgent.velocity.magnitude;

            if (shouldMove)
            {
                m_animator.SetFloat(Speed, speedPercentage);
            }
            else
            {
                m_animator.SetFloat(Speed, 0f);
            }
        }

        public void SetMovementSpeed(float newSpeed)
        {
            m_animator.SetFloat(Speed, newSpeed);
        }

        private void OnAnimatorMove() //Animator movement - root movement
        {
            Vector3 rootPosition = m_animator.rootPosition;
            rootPosition.y = m_navAgent.nextPosition.y;
            transform.position = rootPosition;
            m_navAgent.nextPosition = rootPosition;

            //From player controller beneath here
            var state = m_animator.GetCurrentAnimatorStateInfo(0);

            gravityIntensity = -8f;

            if (state.tagHash == AttackStateTag)
            {
                Vector3 velocity = m_animator.deltaPosition;
                velocity += transform.forward * m_animator.GetFloat(AttackMomentum) * Time.deltaTime;
                velocity.y += gravityIntensity * Time.deltaTime;
                m_characterController.Move(velocity);
            }
        }

        //Called from animation events
        public void InputStart()
        {
            m_inputWindowOpen = true;
        }

        public void InputEnd()
        {
            m_inputWindowOpen = false;
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

        private void EnableLockOnMode(bool enable) //handle hud and other things for lock on mode here
        {
            if (enable)
            {
                m_isStrafing = enable;
            }
            else
            {
                m_isStrafing = enable;
                m_lockOnTarget = null;
            }
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

        public bool UseBossHealthBar()
        {
            return false;
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