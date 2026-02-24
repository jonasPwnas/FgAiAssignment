using System;
using Input;
using Player;
using UnityEngine;
using UnityEngine.AI;

namespace Characters
{
    public class EnemyKnightRootMotionController : MonoBehaviour, ILockOnAble
    {
        private static readonly int DodgeRoll = Animator.StringToHash("DodgeRoll");
        private static readonly int Speed = Animator.StringToHash("MoveSpeed");
        private static readonly int AttackMomentum = Animator.StringToHash("AttackMomentum");
        private static readonly int Died = Animator.StringToHash("Death");
        private static readonly int Charging = Animator.StringToHash("Charging");
        private static readonly int MoveX = Animator.StringToHash("MoveX");
        private static readonly int MoveZ = Animator.StringToHash("MoveZ");
        private static readonly int AttackStateTag = Animator.StringToHash("Attack");
        private static readonly int Blocking = Animator.StringToHash("Blocking");
        private static readonly int Looking = Animator.StringToHash("Looking");
        private static readonly int LightAttack = Animator.StringToHash("LightAttack");
        private static readonly int HeavyAttack = Animator.StringToHash("HeavyAttack");
        private static readonly int SpecialAttack1 = Animator.StringToHash("SpecialAttack1");
        private static readonly int SpecialAttack2 = Animator.StringToHash("SpecialAttack2");
        private static readonly int SpecialAttack3 = Animator.StringToHash("SpecialAttack3"); 
        private static readonly int Damaged = Animator.StringToHash("Damaged");

        [SerializeField] private float gravityIntensity = -6f;
        [SerializeField] private Transform lockOnIndicatorPosition;
        [SerializeField] private EnemyHitboxHandler mainHitbox;
        [SerializeField] private EnemyHitboxHandler secondaryHitbox;

        private NavMeshAgent m_navAgent;
        private Animator m_animator;
        private Transform m_lockOnTarget;

        private bool m_inputWindowOpen;
        private bool m_isDead;
        private bool m_isLockedOnByPlayer;
        private bool m_isBlocking = false;
        private bool m_doSyncMovement = true;

        private Vector2 m_velocity;
        private Vector2 m_smoothDeltaPosition;

        private PlayerController m_playerController;
        private CharacterController m_characterController;
        private float m_startMoveDeltaTimer;
        private float m_stopMoveDeltaTimer;
        private float m_currentMoveSpeed;
        private float m_targetMoveSpeed = 1f;

        public event Action ComboWindowStarted;
        public event Action ComboWindowEnded;

        public bool IsComboWindowOpen => m_inputWindowOpen;
        public bool IsBlocking => m_isBlocking;

        public bool IsBusy
        {
            get
            {
                if (m_animator == null) return false;
                var state = m_animator.GetCurrentAnimatorStateInfo(0);
                if (state.tagHash == AttackStateTag) return true;
                if (m_animator.IsInTransition(0))
                {
                    var next = m_animator.GetNextAnimatorStateInfo(0);
                    if (next.tagHash == AttackStateTag) return true;
                }
                return false;
            }
        }

        private void OnEnable()
        {
            PlayerElementCharger.OnFinishedCharging += ChargingFinished;
            ThirdPersonCamera.OnStopLockOn += StopStrafing;
        }

        private void OnDisable()
        {
            PlayerElementCharger.OnFinishedCharging -= ChargingFinished;
            ThirdPersonCamera.OnStopLockOn -= StopStrafing;
        }

        private void AnimEventFinished(bool lookDone, bool actionDone)
        {
            if (lookDone)
                m_animator.SetBool(Looking, false);

            m_doSyncMovement = actionDone;
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
            SetLockOnTarget(null);
        }

        void Awake()
        {
            m_playerController = FindAnyObjectByType<PlayerController>();
            m_animator = GetComponent<Animator>();
            m_navAgent = GetComponent<NavMeshAgent>();
            m_navAgent.updatePosition = false;
            m_navAgent.updateRotation = true;

            m_characterController = GetComponent<CharacterController>();
        }

        void Update()
        {
            if (Time.timeScale == 0f)
                return;

            //if (m_doSyncMovement)
                MatchAgentAndAnimatorMovement();
        }

        public void DoLookAround() => m_animator.SetBool(Looking, true);

        public void DoLightAttack()
        {
            m_doSyncMovement = false;
            m_animator.SetTrigger(LightAttack);
        }

        public void DoStrongAttack()
        {
            Debug.Log("Enemy doing strong attack");
            m_doSyncMovement = false;
            m_animator.SetTrigger(HeavyAttack);
        }

        public void DoFireAoE()
        {
            m_doSyncMovement = false;
            m_animator.SetTrigger(SpecialAttack1);
        }

        public void DoWaterHeal()
        {
            m_doSyncMovement = false;
            m_animator.SetTrigger(SpecialAttack2);
        }

        public void DoWindPush()
        {
            m_doSyncMovement = false;
            m_animator.SetTrigger(SpecialAttack3); 
        }

        public void DoRoll()
        {
            m_doSyncMovement = false;
            m_animator.SetTrigger(DodgeRoll);
        }

        public void StartCharging()
        {
            m_doSyncMovement = false;
            m_animator.SetBool(Charging, true);
        }

        public void StopCharging()
        {
            m_animator.SetBool(Charging, false);
        }

        public void SetBlocking(bool isBlocking)
        {
            m_isBlocking = isBlocking;
            m_animator.SetBool(Blocking, isBlocking);
        }

        public void SetLockOnTarget(Transform target)
        {
            m_lockOnTarget = target;
        }

        public void OnDeath()
        {
            m_animator.SetTrigger(Died);
            m_isDead = true;
            m_navAgent.enabled = false;
            m_characterController.enabled = false;

            if (m_isLockedOnByPlayer)
                m_playerController.DisableLockOn();
        }

        private void MatchAgentAndAnimatorMovement()
        {
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

            if (shouldMove)
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

            m_animator.SetFloat(MoveX, m_velocity.x);
            m_animator.SetFloat(MoveZ, m_velocity.y);
            m_animator.SetFloat(Speed, m_currentMoveSpeed);
            //Quaternion lookRotation = Quaternion.LookRotation(new Vector3(m_velocity.x, 0, m_velocity.y));
            //transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, smooth);
            transform.LookAt(m_navAgent.nextPosition);//->smooth this out pls

            //If the agent is too far away from the root position, correct it.
            float deltaMagnitude = worldDeltaPosition.magnitude;
            if (deltaMagnitude > m_navAgent.radius / 3f)
            {
                print("Correcting NavPosition");
                transform.position = Vector3.Lerp(m_animator.rootPosition,
                    m_navAgent.nextPosition, smooth);
            }
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


        public void InputStart()
        {
            m_inputWindowOpen = true;
            ComboWindowStarted?.Invoke();
        }

        public void InputEnd()
        {
            m_inputWindowOpen = false;
            ComboWindowEnded?.Invoke();
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
            return ElementTypes.ElementType.NoElement;
        }
    }
}
