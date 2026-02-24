using Input;
using System.Collections;
using AbilityUI;
using FMODUnity;
using UnityEngine;
using UnityEngine.InputSystem;

//Add DOTween package to use this, is then used to move the camera to death position
//using DG.Tweening;

namespace Player
{ 
    public class PlayerController : MonoBehaviour
    {
        //delegates and events
        public delegate void DeathCamMoved();
        public static event DeathCamMoved OnDeathCamMoved;
        
        public delegate void GamePaused();
        public static event GamePaused OnGamePaused;

        public delegate void ChargeElement(bool isCharging);
        public static event ChargeElement OnChargeElement;

        public delegate void LockOnModeUpdate(bool enable, Transform target, ElementTypes.ElementType elementWeakness);
        public static event LockOnModeUpdate OnLockOnModeUpdate;

        public delegate void CastSpell(ElementTypes.ElementType elementType);
        public static event CastSpell OnCastSpell;

        public delegate void Attacking();
        public static event Attacking OnAttacking;

        public delegate void Blocking(bool isBlocking);
        public static event Blocking OnBlocking;

        public delegate void Interact();
        public static event Interact OnInteract;
        
        
        // Not Implemented Yet 
        public delegate void PerfectBlock(bool hasPerfectBlock);
        public static event PerfectBlock OnPerfectBlock;

        public delegate void ToggleIframes(bool hasIframes);
        public static event ToggleIframes OnToggleIframes;
        

        //Animator parameters
        private static readonly int Speed = Animator.StringToHash("MoveSpeed");
        private static readonly int MoveX = Animator.StringToHash("MoveX");
        private static readonly int MoveZ = Animator.StringToHash("MoveZ");
        private static readonly int IsGrounded = Animator.StringToHash("isGrounded");
        
        private static readonly int DodgeStateTag = Animator.StringToHash("Dodge");   // NEW: Currently only on DodgeRoll, might need better setup in the future
        private static readonly int DodgeRollTrigger = Animator.StringToHash("DodgeRoll");//Add parameter to animator
        private static readonly int IFramesWindow = Animator.StringToHash("IFrames");
        
        private static readonly int BlockStateTag = Animator.StringToHash("Block");
        private static readonly int BlockingBool = Animator.StringToHash("Blocking");
        private static readonly int BlockWindow = Animator.StringToHash("BlockingWindow");
        private static readonly int PerfectBlockWindow = Animator.StringToHash("PerfectBlockWindow"); 

        private static readonly int AttackStateTag = Animator.StringToHash("Attack");   // used ONLY with state.tagHash,  tag field on Animator states (select a state -> Inspector -> "Tag" dropdown).
        private static readonly int AttackTrigger = Animator.StringToHash("AttackTrigger"); // used for performing an attack 
        private static readonly int AttackType = Animator.StringToHash("AttackType");

        private static readonly int BaseForwardSpeed = Animator.StringToHash("BaseForwardSpeed");
        private static readonly int AttackMomentum = Animator.StringToHash("AttackMomentum");
        private static readonly int AttackSteerDeg = Animator.StringToHash("AttackSteerDeg");
        private static readonly int AttackMagnetWindow = Animator.StringToHash("AttackMagnetWindow");       // NEW
        private static readonly int ComboWindow = Animator.StringToHash("ComboWindow");
        private static readonly int Died = Animator.StringToHash("Died");
        
        private static readonly int ChargeStateTag = Animator.StringToHash("Charging");
        private static readonly int ChargingBool = Animator.StringToHash("ChargingElement");

        //New, again! :D
        private static readonly int SpellcastingStateTag = Animator.StringToHash("Spellcasting");
        
        private static readonly int InteruptStateTag = Animator.StringToHash("Interupted");
        private static readonly int BigHit = Animator.StringToHash("BigHit");
        private static readonly int Hit = Animator.StringToHash("Hit");
        private static readonly int Falling = Animator.StringToHash("Falling");

        //Editor editable
        [Tooltip("How fast we can jump after landing, in seconds")] [SerializeField] private float jumpFrequency = 0.4f;
        [Tooltip("Downward force intensity")] [SerializeField] private float gravityIntensity = -6f;
        [Tooltip("How fast the player rotates")] [SerializeField] private float rotationSpeed = 10f;
        [Tooltip("The camera used by the player")] [SerializeField] private ThirdPersonCamera playerCamera;
        //[SerializeField] Trail trail;
        [SerializeField] private Transform lockOnCastPoint;
        [SerializeField] private PlayerElementCharger playerElementCharger;
        [SerializeField] private Transform lockOnIndicatorPosition;
        
        [Header("Stamina Costs")]
        [SerializeField] private float staminaRegenPerSecond = 12f;
        [SerializeField] private float overdraftTimePenalty = 1f;
        [SerializeField] private float dodgeCost = 20f;
        [SerializeField] private float lightAttackCost = 24f;
        [SerializeField] private float heavyAttackCost = 37f;
        [SerializeField] private float sprintCost = 0.5f;
        [SerializeField] private float blockingStaminaMultiplier = 0.25f;

        // Time To Live timers for input requests, reserve stamina calculation and refunds if actions were not performed in allocated time window   
        [Header("Stamina Requested TTL")]
        [SerializeField] private float m_dodgeRequestTTL = 0.35f;
        [SerializeField] private float m_attackRequestTTL = 0.35f;
        // private float m_sprintRequestTTL = 0.25f;
        
        [Header("Movement accelerations")]
        [SerializeField] private float accelDampTime = 0.1f;      // NEW
        [SerializeField] private float runToWalkDampTime = 0.5f;  // NEW
        [SerializeField] private float stopDampTime = 0.05f;      // NEW

        [Header("Attack Magnet")]
        [SerializeField, Tooltip("meters: engage range")] private float magnetStartPullDist = 2.0f;
        [SerializeField, Tooltip("meters: don’t pull closer than this ")] private float magnetStopPullDist = 0.9f;
        [SerializeField, Tooltip("m/s: pull speed")] private float attackCorrectionPull = 2.5f;     
        [SerializeField, Tooltip("deg/s: rotation towards target speed")] private float magnetTurnRateDeg = 720f;        


        private struct FrameAnimatorFlags
        {
            public bool inStableInterupted, interuptedRelevant;
            public bool inStableDodge, dodgeRelevant;
            public bool inStableBlock, blockRelevant;
            public bool inStableAttack, attackRelevant;
            public bool inStableCharge, chargeRelevant; 
            public bool inStableSpellcast, spellcastRelevant;
        }

        private FrameAnimatorFlags _f;

        //Privates
        private PlayerInput m_playerInput; //whyyyyyyyyy
        private AbilityUIController m_abilityUiController;
        private PlayerAudio m_audio;
        private Animator m_animator;
        private PlayerStatus m_playerStatus;
        private PlayerInputHandler m_PlayerInputHandler;
        private CharacterController m_characterController;
        private PauseMenuController m_pauseManager;
        private PlayerObjectInteraction m_playerObjectInteraction = null;
        private OpenTutorialWindow m_openTutorialWindow;
        private SelectionWheel m_selectionWheel;
        
        // NEW - for lock-on movement
        private Transform m_lockOnTarget;
        private Vector3 m_playerVelocity;

        // NEW - might delete and keep as mutable fields to makes it more obvious what frame’s data is being used, preventing stale stored values? 
        private bool m_isInTransition;
        private AnimatorStateInfo m_stateCurrent;
        private AnimatorStateInfo m_stateNext;

        // NEW - Temp torso layer for blocking
        private bool m_torsoInTransition;
        private AnimatorStateInfo m_torsoStateCurrent;
        private AnimatorStateInfo m_torsoStateNext;
        private int _torsoLayerIndex;

        private float m_targetMoveSpeed = 0.5f;
        private bool m_isStrafing = false;      // will be used for lock-on movement. (true = rotate toward a facing direction. fales = rotate toward movement, almost never walks backwards) 
        private float m_moveX;
        private float m_moveZ;

        private bool m_isFalling;
        private bool m_mightBeFalling;
        private bool m_groundedPlayer;
        private float m_enterFallStateTimer = 0.2f;
        private float m_fallStateTimeStamp = 0f;
        private float m_appliedGravityIntensity;

        private PlayerStatus.StaminaReservation m_dodgeReservation;         // TTL handling
        private float m_dodgeRequestExpiresAt;                              // TTL handling
        private bool m_dodgeRequested = false;                              // TTL handling
        private bool m_wasDodgingTruth = false;  
        private bool m_hasIFrames = false;

        private bool m_blockRequested = false;
        private bool m_hasBlock = false;
        private bool m_hasPerfectBlock = false;
        private bool m_wasBlockingTruth = false; 
        
        private PlayerStatus.StaminaReservation m_comboAttackReservation;   // TTL handling
        private PlayerStatus.StaminaReservation m_attackReservation;        // TTL handling
        private int m_attackRequestedType;                                  // TTL handling     // not really used right now, but good for something later (debug, analytics, HUD, cancel rules)
        private float m_attackRequestExpiresAt;                             // TTL handling
        private bool m_attackRequested = false;                             // TTL handling
        private bool m_wasAttackingTruth = false;
        private bool m_canAttack = true;
        private float m_attackTimer;
        private bool m_comboConsumedThisWindow = false;

        private float m_maxAttackSteerDegPerSec = 240f;
        private int m_cachedAttackInputFrame = -1;
        Vector3 m_cachedAttackInputDir;     // world dir normalized
        float m_cachedAttackInputMag;       // 0..1
        bool m_cachedAttackHasInput;

        private bool m_chargeElementRequested = false;
        private bool m_wasChargingElementTruth = false;
        private bool m_chargeSystemEnabled = false;
        //private bool m_isCharging = false;
        private bool m_isCastingSpell = false;

        private float m_staminaPauseTimestamp;
        private float m_staminaMultiplier = 1f;
        
        private bool m_updateCharacter = false;
        private bool m_isDead;
        private bool m_doInteract = false;
        


        //Read-only
        private readonly float m_attackInputFrequency = 0.25f;
        
        //Constants
        private const float DEADZONE = 0.15f;  // mainly to combat stick drift starting animations 
        
        //Publics
        [HideInInspector] public Vector3 moveVelocity;

        private void OnEnable()
        {
            PlayerElementCharger.OnFinishedCharging += CancelChargeElementRequest;
            ThirdPersonCamera.OnStopLockOn += StopStrafing;
            LockOnTargeting.OnStopLockOn += DisableLockOn;
            PlayerStatus.OnTookDamage += OnDamaged;
            PlayerStatus.OnPlayerDied += OnDeath;
            PlayerObjectInteraction.OnPickUp += SetDoPickupInsteadOfCharge;
            OpenTutorialWindow.OnOpenTutorial += OpenTheTutorialWindow;
            PlayerStatus.OnStaminaBroken += HandleStaminaBroken;
        }

        private void OnDisable()
        {
            PlayerElementCharger.OnFinishedCharging -= CancelChargeElementRequest;
            ThirdPersonCamera.OnStopLockOn -= StopStrafing;
            LockOnTargeting.OnStopLockOn -= DisableLockOn;
            PlayerStatus.OnTookDamage -= OnDamaged;
            PlayerStatus.OnPlayerDied -= OnDeath;
            PlayerObjectInteraction.OnPickUp -= SetDoPickupInsteadOfCharge; 
            OpenTutorialWindow.OnOpenTutorial -= OpenTheTutorialWindow;
            PlayerStatus.OnStaminaBroken -= HandleStaminaBroken;
        }

        private void Awake()
        {
            m_PlayerInputHandler = GetComponent<PlayerInputHandler>();
            m_characterController = GetComponent<CharacterController>();
            m_animator = GetComponent<Animator>();
            m_playerStatus = GetComponent<PlayerStatus>();
            m_pauseManager = FindAnyObjectByType<PauseMenuController>();
            m_selectionWheel = FindAnyObjectByType<SelectionWheel>();
            m_openTutorialWindow = FindAnyObjectByType<OpenTutorialWindow>();
            m_audio = GetComponent<PlayerAudio>();
            m_abilityUiController = FindAnyObjectByType<AbilityUIController>();

            // IMPORTANT 
            // TEMP - remove after block has changed layer
            _torsoLayerIndex = m_animator.GetLayerIndex("Torso");
            if (_torsoLayerIndex < 0) Debug.LogError("Torso layer not found");
        }

        private void Start()
        {

            Cursor.lockState = CursorLockMode.Locked;
            StartCoroutine(WaitToStartUpdate(0.3f));
        }

        void Update()
        {
            if (!m_updateCharacter) return;
            
            if (Time.timeScale == 0f) return;

            if (m_isDead)
                return;

            /*if(m_PlayerInputHandler.pauseAction.IsPressed())
                   OnGamePaused?.Invoke();*/

            // --- States Maintenance ---

            // Clear stale cached input for attack correction
            m_cachedAttackInputFrame = -1;
            m_cachedAttackInputDir = Vector3.zero;
            m_cachedAttackInputMag = 0f;
            m_cachedAttackHasInput = false;

            // Update recorded state info once per update       // IMPORTANT: OnAnimatorMove() should not use flags! Needs to get fresh info on its own
            UpdateAnimStatesInfo();
            GetInteruptStateFlags(out _f.inStableInterupted, out _f.interuptedRelevant);
            GetDodgingStateFlags(out _f.inStableDodge, out _f.dodgeRelevant);
            GetBlockingStateFlags(out _f.inStableBlock, out _f.blockRelevant);
            GetAttackStateFlags(out _f.inStableAttack, out  _f.attackRelevant);
            GetMagicStateFlags(out _f.inStableCharge, out _f.chargeRelevant, out _f.inStableSpellcast, out _f.spellcastRelevant);

            DodgeMaintenance();         // TTL + iFrames sync
            BlockMaintenance();         // stamina multiplier + OnBlocking truth events
            AttackMaintenance();        // TTL + cooldown unlock + cleanup
            ChargeElementMaintenance(); // state matching animator truth + requests 


            // --- Input/request phase ---
            DoPause();
            CycleEquippedElement();
            Dodge();
            Block();
            DoCastSpell();
            Attack();
            DoChargeElement();
            Movement();
            UpdateStamina();
        }

        private void ClearRequestedInputs()
        {
            CancelDodgeRequest();
            CancelBlockRequest();
            CancelCastSpellRequest();
            CancelAttackRequests();
            CancelChargeElementRequest();
        }

        private void CycleEquippedElement()
        {
            if (m_PlayerInputHandler.cycleForwardAction.triggered)
            {
                m_selectionWheel.OnNextElement();
                return;
            }
            if (m_PlayerInputHandler.cycleBackwardAction.triggered)
            {
                m_selectionWheel.OnPreviousElement();
            }
        }
        
        private void DoPause()
        {
            if (m_PlayerInputHandler.pauseAction.IsPressed())
            {
                m_pauseManager.PerformPause();
            }
        }
        
        
        private void UpdateStamina()
        {
            if (m_playerStatus.GetCurrentStaminaPerc() > 0.999f)
                return;

            if (Time.time < m_staminaPauseTimestamp)
                return;

            m_playerStatus.AddStamina(staminaRegenPerSecond * Time.deltaTime * m_staminaMultiplier);

            // NOTE: current regen is frame-rate dependent. Look int how something in this style would work: 
            //       m_playerStatus.AddStamina(staminaRegenPerSecond * Time.deltaTime * m_staminaMultiplier);
        }

        private void PauseStaminaRegen(float pauseSeconds = 1.2f)
        {
            // pause time does not stack but the longest one is the one that gets set, will prevent a shorter pause from overriding a longer one.
            m_staminaPauseTimestamp = Mathf.Max(m_staminaPauseTimestamp, Time.time + pauseSeconds);
        }

        private void CommitReservedStaminaCost(ref PlayerStatus.StaminaReservation staminaReservation, float pauseSeconds = 1.2f)
        {
            if (staminaReservation.requestedActionCost <= 0f) return;
            m_playerStatus.CommitReservedStamina(staminaReservation);
            
            // NOTE: This needs fixing, 
            if (staminaReservation.wouldOverdraw)
            {
                pauseSeconds += overdraftTimePenalty; 
                Debug.Log($"Stamina Penalty!  cost: {pauseSeconds}");
            }

            PauseStaminaRegen(pauseSeconds);
            staminaReservation = default;
        }

        private void ReleaseReservedStaminaCost(ref PlayerStatus.StaminaReservation staminaReservation)
        {
            if (staminaReservation.requestedActionCost <= 0f) return;
            m_playerStatus.ReleaseReservedStamina(staminaReservation); // clamp inside PlayerStatus
            staminaReservation = default; 
        }

        private void HandleStaminaBroken(bool broken)
        {
            if (!broken) return;

            ClearRequestedInputs();

            // Should there be regen punishment here? 
            PauseStaminaRegen(1.5f);

            // Force an interrupt animation here (guardbreak/stagger) so animator truth stomps any in-flight transitions.
            // Placeholder:
            m_animator.SetTrigger(BigHit);

            Debug.Log("You suck at stamina");
            StartCoroutine(StaminaBreakWait()); 
        }

        IEnumerator StaminaBreakWait()
        {
            yield return new WaitForSeconds(1f);

            m_playerStatus.ClearStaminaBroken();    
            Debug.Log("Stamina activated again, NOT BROKEN!");
        }


        private void SetDoPickupInsteadOfCharge(bool doPickup, PlayerObjectInteraction objectInteract)
        {
            m_doInteract = doPickup;
            if(doPickup)
            {
                m_playerObjectInteraction = objectInteract;
            }
            else
            {
                m_playerObjectInteraction = null;
            }
        }
        
        private void OpenTheTutorialWindow(bool open, OpenTutorialWindow cooltuttis)
        {
            m_doInteract = open;
            if (open)
            {
                print("did open tutorial");
                cooltuttis.OpenWindow();
                m_updateCharacter = false;
            }
            else
            {
                m_updateCharacter = true;
            }
        }


        private void CancelCastSpellRequest()
        {
            m_isCastingSpell = false;

            m_animator.ResetTrigger("CastAoE");
            m_animator.ResetTrigger("Heal");
            m_animator.ResetTrigger("CastForward");
        }
        
        private void DoCastSpell()
        {
            // --- Priority Locks for new input requests (After Maintenance) ---

            bool interruptedLocksSpells = _f.interuptedRelevant;
            bool dodgeLocksSpells = _f.dodgeRelevant || m_dodgeRequested;
            // NOTE: change later if attack state is not a hard blocker just in itself, but a part of a set of conditions 
            //      bool canCancelToAction = m_animator.GetFloat(EarlyOut) > 0.5f; // or CanDodge
            //      bool attackLocksAction = _f.isAttackRelevant && !canCancelToDodge;
            bool attackLocksSpells = _f.attackRelevant;
            bool spellsLocksSpells = m_isCastingSpell;

            bool locked = (interruptedLocksSpells || dodgeLocksSpells || attackLocksSpells || spellsLocksSpells);
            if (locked)
                return; 

            // --- Input handling (request) ---
            // 
            if (m_PlayerInputHandler.castSpellAction.triggered)
            {
                if (m_playerStatus.GetSourceElement() == ElementTypes.ElementType.NoElement)
                    return;
                
                if (m_playerStatus.GetCurrentElementAmount() == 0)
                    return;

                if (m_playerStatus.GetSourceElement() == ElementTypes.ElementType.Fire && m_playerStatus.GetCurrentElementAmount() <= 2)
                    return;

                m_isCastingSpell = true;

                ElementTypes.ElementType coolElement = m_playerStatus.GetSourceElement();
                
                m_abilityUiController.UseAbility();
                switch (coolElement)
                {
                    case ElementTypes.ElementType.Fire:
                        m_animator.SetTrigger("CastAoE");
                        m_playerStatus.RemoveElement(coolElement, 2);
                        m_audio.PlayFireSpellSfxOne();
                        break;
                    case ElementTypes.ElementType.Water:
                        m_animator.SetTrigger("Heal");
                        m_playerStatus.RemoveElement(coolElement, 1);
                        break;
                    case ElementTypes.ElementType.Air:
                        m_animator.SetTrigger("CastForward");
                        m_audio.PlayAirSpellSfx();
                        m_playerStatus.RemoveElement(coolElement, 1);
                        break;
                }
            }
        }

        public void CastFinished()//Called from anim event
        {
            m_isCastingSpell = false;
        }
        
        public void TriggerCast()//Called from anim event
        {
            OnCastSpell?.Invoke(ElementTypes.ElementType.Air);
        }

        public void TriggerAoe()//Called from anim event
        {
            OnCastSpell?.Invoke(ElementTypes.ElementType.Fire);
            m_audio.PlayFireSpellSfxTwo();
        }

        public void TriggerHeal()//Called from anim event
        {
            OnCastSpell?.Invoke(ElementTypes.ElementType.Water);
            m_audio.PlayWaterSpellSfx();
        }

        private void OnDamaged(float damage, ElementTypes.ElementType elementType)
        {
            // NOTE: Change to this when curves exists 
            //if (m_hasBlock || m_hasPerfectBlock) return;

            if (_f.blockRelevant || m_hasIFrames)
                return; //implement shield hit react pls and also stamina
            
            if (damage > 30)
            {
                m_animator.SetTrigger(BigHit);
            }
            else
            {
                m_animator.SetTrigger(Hit);
            }
        }

        private void OnDeath()
        {
            //Move camera to nice cinematic location on death
            //StartCoroutine(MovecameraOnDeath());
            
            m_animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            m_animator.SetTrigger(Died);
            m_isDead = true;
            enabled = false;
            //trail.gameObject.SetActive(false);
        }
        
        private void DodgeMaintenance()
        {
            bool dodgeTruth = _f.dodgeRelevant;
            bool enteredDodge = !m_wasDodgingTruth && dodgeTruth;

            // Confirm state was enterd succesfully: predicted stamina drain is locked as truth
            if (enteredDodge && m_dodgeRequested)
            {
                m_dodgeRequested = false;
                CommitReservedStaminaCost(ref m_dodgeReservation);
            }

            // If dodge was requested, but animator never transitioned: refund reserved stamina to player 
            if (m_dodgeRequested && Time.time >= m_dodgeRequestExpiresAt)
            {
                CancelDodgeRequest();
            }


            // Curve driven Iframes, only fire event on a change + inside dodge state. 
            bool iFramesWindowOn = GetCurveFloatOrDefault(IFramesWindow, 0f) >= 0.5f;
            bool iFramesNow = dodgeTruth && iFramesWindowOn;            // bool iFramesNow = dodgeTruth && (m_animator.GetFloat(IFramesWindow) > 0.5f);
            if (iFramesNow != m_hasIFrames)
            {
                // NOTE WARNING: if IFrames can be gained from elsewhere, this would turn of that as well!!

                // also cleans up if we exited dodge abruptly without resetting IFrames     
                m_hasIFrames = iFramesNow;
                OnToggleIframes?.Invoke(m_hasIFrames);
            }

            m_wasDodgingTruth = dodgeTruth;
        }

        private void CancelDodgeRequest()
        {
            if (m_dodgeRequested)
                ReleaseReservedStaminaCost(ref m_dodgeReservation);

            m_dodgeRequested = false;
            m_animator.ResetTrigger(DodgeRollTrigger);  // prevent late free dodge 
        }

        private void Dodge()
        {
            // --- Priority Locks for new input requests (After Maintenance) ---

            bool interruptedLocksDodge = _f.interuptedRelevant;
            // NOTE:
            //      Change later if attack state is not a hard blocker just in itself, but a part of a set of conditions 
            //      bool canCancelToAction = m_animator.GetFloat(EarlyOut) > 0.5f; // or CanDodge
            //      bool attackLocksAction = _f.isAttackRelevant && !canCancelToDodge;
            bool attackLocksDodge = _f.attackRelevant;
            bool spellsLocksDodge = m_isCastingSpell;

            bool locked = (interruptedLocksDodge || attackLocksDodge || spellsLocksDodge);
            if (locked)
                return;


            // --- Input handling (request) ---   

            // NOTE:
            //       There is currently no dodge follow up by another dodge limmiter,
            //       can chain dodge in infinity if animator alows.
            //       But animator setup should? be handling how ir feels  

            // Early outs after TTL and Maintenance is done 
            if (!m_PlayerInputHandler.dodgeAction.triggered)    // Input gate
                return;

            bool dodgeTruth = _f.dodgeRelevant;
            if (dodgeTruth || m_dodgeRequested)                 // Input-Spam / already in dodge-state gate
                return;

            if (!m_playerStatus.TryReserveStamina(dodgeCost, out m_dodgeReservation))   // Stamina gate
                return;

            // All conditions for a dodge has been met, stamina was paid by the check above but can be refunded
            m_dodgeRequested = true;
            m_dodgeRequestExpiresAt = Time.time + m_dodgeRequestTTL; 
            m_animator.SetTrigger(DodgeRollTrigger);
        }

        private void BlockMaintenance()
        {
            bool blockTruth = _f.blockRelevant;     // will be fixed more precise by curves, right now "block = true" as soon as transition starts  

            if (blockTruth != m_wasBlockingTruth)
            {
                m_staminaMultiplier = blockTruth ? blockingStaminaMultiplier : 1f;  // mstch stamina behaviour to state 
                OnBlocking?.Invoke(blockTruth);                                     // send event only on truth change
                m_wasBlockingTruth = blockTruth;
            }


            // Curve driven block / perfect block, only fire event on a change while inside block state. 
            bool blockWindowOn = GetCurveFloatOrDefault(BlockWindow, 0f) >= 0.5f;
            bool blockNow = blockTruth && blockWindowOn;         // bool blockNow = blockTruth && (m_animator.GetFloat(BlockingWindow) > 0.5f);
            if (blockNow != m_hasBlock)
            {
                // also cleans up if we exited block abruptly without resetting     
                m_hasBlock = blockNow;
                OnBlocking?.Invoke(m_hasBlock);
            }

            bool perfectBlockWindowOn = GetCurveFloatOrDefault(PerfectBlockWindow, 0f) >= 0.5f;
            bool perfectBlockNow = blockTruth && perfectBlockWindowOn;          // bool perfectBlockNow = blockTruth && (m_animator.GetFloat(PerfectBlockWindow) > 0.5f);
            if (perfectBlockNow != m_hasPerfectBlock)
            {
                // also cleans up if we exited block abruptly without resetting     
                m_hasPerfectBlock = perfectBlockNow;
                OnPerfectBlock?.Invoke(m_hasPerfectBlock);
            }
        }

        private void CancelBlockRequest()
        {
            m_blockRequested = false;
            m_animator.SetBool(BlockingBool, false);

            m_staminaMultiplier = 1f;
        }

        private void Block()
        {
            // --- Priority Locks for new input requests (After Maintenance) ---

            bool interruptedLocksDefence = _f.interuptedRelevant;
            bool dodgeLocksDefense = _f.dodgeRelevant || m_dodgeRequested;
            // NOTE: change later if attack state is not a hard blocker just in itself, but a part of a set of conditions 
            //      bool canCancelToAction = m_animator.GetFloat(EarlyOut) > 0.5f; // or CanDodge
            //      bool attackLocksAction = _f.isAttackRelevant && !canCancelToDodge;
            bool attackLocksDefense = _f.attackRelevant;
            bool spellsLocksDefense = m_isCastingSpell;
            bool chargingLocksDefense = _f.chargeRelevant || m_chargeElementRequested;

            bool locked = (interruptedLocksDefence || dodgeLocksDefense || attackLocksDefense || spellsLocksDefense || chargingLocksDefense);


            // --- Input handling (request) ---            

            bool wantsBlock = m_PlayerInputHandler.blockAction.IsPressed();
            bool shouldBlock = wantsBlock && !locked;

            // Sync request 
            if (shouldBlock != m_blockRequested)
            {
                m_blockRequested = shouldBlock;                  // Toggle request status
                m_animator.SetBool(BlockingBool, m_blockRequested); // Update animator with current request
            }
        }


        private void ChargeElementMaintenance()
        {

            // NOTE: This did not work.. 
            // Trying to find solution to charge problem:  prevent hard-lock if reference is missing
            if (playerElementCharger == null)
            {
                // stop requesting and stop animator bool to try not get stuck
                m_chargeSystemEnabled = false;
                m_chargeElementRequested = false;
                m_animator.SetBool(ChargingBool, false);

                // log that something is wrong
                Debug.LogError("playerElementCharger is null - forcing charge cancel to avoid lock.");

                // try keep truth tracking consistent
                m_wasChargingElementTruth = false;
                return;
            }



            bool chargeTruth = _f.chargeRelevant;     // will be fixed more precise by curves, right now "block = true" as soon as transition starts  
            
            bool entered = !m_wasChargingElementTruth && chargeTruth;
            bool exited = m_wasChargingElementTruth && !chargeTruth;

            // Fire event ONLY on truth change (entered/exited)
            if (entered)
            {
                Debug.Log("Enterd ChargeElement State");

                if (!m_chargeSystemEnabled)
                {
                    m_chargeSystemEnabled = true;
                    playerElementCharger.EnableCharger(true);
                }

                OnChargeElement?.Invoke(true);
            }
            else if (exited)
            {
                Debug.Log("Exited ChargeElement State");
                
                // cleanup on state-exit or on early out exit, drop request so animator doesn’t try to re-enter by accident
                m_chargeSystemEnabled = false;
                playerElementCharger.EnableCharger(false);
                
                m_chargeElementRequested = false;
                m_animator.SetBool(ChargingBool, false);

                OnChargeElement?.Invoke(false);
            }

            m_wasChargingElementTruth = chargeTruth;
        }

        private void CancelChargeElementRequest()
        {
            // If nothing is happening, bail
            bool chargeTruth = _f.chargeRelevant;
            if (!m_chargeElementRequested && !chargeTruth)
                return;

            m_chargeElementRequested = false;
            m_animator.SetBool(ChargingBool, false);

            m_chargeSystemEnabled = false;
            playerElementCharger.EnableCharger(false);
        }

        private void DoChargeElement()
        {
            // --- Priority Locks for new input requests (After Maintenance) ---

            bool interruptedLocksCharge = _f.interuptedRelevant;
            bool dodgeLocksCharging     = _f.dodgeRelevant || m_dodgeRequested;
            // NOTE: change later if attack state is not a hard blocker just in itself, but a part of a set of conditions 
            //      bool canCancelToAction = m_animator.GetFloat(EarlyOut) > 0.5f; // or CanDodge
            //      bool attackLocksAction = _f.isAttackRelevant && !canCancelToDodge;
            bool attackLocksCharging    = _f.attackRelevant;
            bool spellsLocksCharging    = m_isCastingSpell;

            bool locked = (interruptedLocksCharge || dodgeLocksCharging || attackLocksCharging || spellsLocksCharging);


            // --- Input handling (request) --- 

            bool wantsCharge = m_PlayerInputHandler.chargeElementAction.IsPressed();

            // Option A (priority): if pickup possible, do pickup on press/hold and do NOT request charge.
            //if (m_PlayerInputHandler.chargeElementAction.triggered && m_doPickup && m_playerObjectInteraction != null)    // if one-press action instead of held, gate to avoid per-frame spam
            if (wantsCharge && m_doInteract && m_playerObjectInteraction != null)
            {
                Debug.Log("Triggerd Charge Pickup!");

                m_playerObjectInteraction.DoInteract();
                return;
            }

            bool shouldCharge = wantsCharge && !locked && !m_doInteract;

            // Option B (secondary): sync request for an actual charge to toggle on/off 
            if (shouldCharge != m_chargeElementRequested)
            {
                m_chargeElementRequested = shouldCharge;
                m_animator.SetBool(ChargingBool, m_chargeElementRequested);
            }
        }


        private void AttackMaintenance()
        {
            bool attackRelevant = _f.attackRelevant;
            bool enteredAttack = !m_wasAttackingTruth && attackRelevant;
            bool leftAttackState = m_wasAttackingTruth && !attackRelevant;

            // Confirm: commit reserved stamina only when we actually entered an attack
            if (enteredAttack && m_attackRequested)
            {
                m_attackRequested = false;
                CommitReservedStaminaCost(ref m_attackReservation);
                m_attackRequestedType = 0;

                // Debounce window (rate limiter) only if attack actually happens
                m_attackTimer = Time.time + m_attackInputFrequency;
                m_canAttack = false;
            }

            // Expire: release reservation + clear animator params so it can't late fire
            if (m_attackRequested && Time.time >= m_attackRequestExpiresAt)
            {
                ReleaseReservedStaminaCost(ref m_attackReservation);
                m_attackRequestedType = 0;

                ClearAttacksFromAnimator(); // resets trigger + AttackType=0
                if (m_PlayerInputHandler.attackMode == PlayerInputHandler.AttackInputMode.QueuedPress)
                    m_PlayerInputHandler.ClearQueuedAttack();
                
                m_attackRequested = false;
            }


            // Leaving attack state cleanup: do NOT stomp pending TTL requests. Prevents any stale queued inputs or attack-triggers leaking into movement.
            if (leftAttackState && !m_attackRequested) 
            {
                m_PlayerInputHandler.ClearQueuedAttack();
                ClearAttacksFromAnimator();
            }

            m_wasAttackingTruth = attackRelevant;


            if (!attackRelevant && !m_canAttack)
            {
                // NOTE WARNING: Cooldown might block combo window if they happen before this timer?  
                //               Might work as if (!attackRelevant && !m_canAttack) so QueuedPress doesn’t get consumed and
                //               lost during neutral cooldown but Combo chaining isn’t blocked by cooldown

                // Cooldown tick only matters when not in attack context
                if (Time.time >= m_attackTimer)
                {
                    m_canAttack = true;
                    m_attackTimer = 0f;
                }

                // if any attack input was stored during this attack-cooldown period, (and not in any state to make use of inComboWindow), ignore that input
                if (m_PlayerInputHandler.attackMode == PlayerInputHandler.AttackInputMode.QueuedPress)
                {                    
                    m_PlayerInputHandler.TryGetAnyAttack(out _);    // consume-and-drop any queued attack so it cannot fire later
                    m_PlayerInputHandler.ClearQueuedAttack();       // Extra safety: ensure no value was left 
                }

                // NOTE: Held Mode - do nothing special, holding will attempt again next frame (easy mode)
            }
        }

        private void CancelAttackRequests()
        {
            if (m_attackRequested)
                ReleaseReservedStaminaCost(ref m_attackReservation);
            m_comboAttackReservation = default;

            m_attackRequested = false;
            m_attackRequestedType = 0;
            m_comboConsumedThisWindow = false;

            ClearAttacksFromAnimator();
            if (m_PlayerInputHandler.attackMode == PlayerInputHandler.AttackInputMode.QueuedPress)
                    m_PlayerInputHandler.ClearQueuedAttack();
        }

        private void Attack()
        {
            // --- Priority Locks for new input requests (After Maintenance) ---

            bool interruptedLocksAttacks = _f.interuptedRelevant;
            // NOTE:
            //      Move this if combo attacks for some reson should not be blocked? 
            bool dodgeLocksAttacks = _f.dodgeRelevant || m_dodgeRequested;
            bool spellsLocksAttacks = m_isCastingSpell;

            bool locked = (interruptedLocksAttacks || spellsLocksAttacks || dodgeLocksAttacks);
            if (locked)
                return;


            // --- Input handling (request) --- 

            // If in neutral cooldown, don't process input this frame
            if (!_f.attackRelevant && !m_canAttack)
                return;
            
            bool inComboWindow = GetCurveFloatOrDefault(ComboWindow, 0f) >= 0.5f;               // bool inComboWindow = m_animator.GetFloat(ComboWindow) > 0.5f;

            // Re-arm check when combo window closes
            if (!inComboWindow)
                m_comboConsumedThisWindow = false;

            // Read attack input once (queued press or held mode). If there hasn't been any input, stop here 
            if (!m_PlayerInputHandler.TryGetAnyAttack(out int attackType) || attackType == 0)
                return;

            // If already in an attack but not in combo window: strict timing (clear all stored attack inputs, no late buffering)
            if (_f.attackRelevant && !inComboWindow)
            {
                // Only matters in QueuedPress (Held has nothing queued)
                m_PlayerInputHandler.ClearQueuedAttack();
                return;
            }

            // NOTE: Combo chaining attacks is not blocked by the attack cooldown, animation curves has priority 
            //       If chaining from an ongoing attack, consume the combo window after one successful input, guarded against consuming inputs twice
            //       Might want to change to using attackRelevant instead of attackStable if inComboWindow ever overlaps with transition windows.
            //          - Allow combo buffering during transition once the combo window curve is open.
            //              - would maybe work to make combow window and ExitWindow to overlap a bit in a more stable fasion?

            // Combo chain
            if (_f.inStableAttack && inComboWindow && !m_comboConsumedThisWindow)
            {
                FireComboAttackImmediate(attackType);
                m_comboConsumedThisWindow = true;   // attack input was consumed during this animation, prevent this triggering multiple times  
                return;
            }

            // Neutral start
            if (!_f.attackRelevant)
            {
                RequestNeutralAttackWithTTL(attackType);
            }
        }

        private void RequestNeutralAttackWithTTL(int attackType)
        {
            float cost = GetAttackCost(attackType);
            if (cost <= 0f) return;

            if (m_attackRequested) return;

            if (!m_playerStatus.TryReserveStamina(cost, out m_attackReservation))
            {
                // Strict mode: clear queued input on failed attack
                if (m_PlayerInputHandler.attackMode == PlayerInputHandler.AttackInputMode.QueuedPress)
                    m_PlayerInputHandler.ClearQueuedAttack();
                return;
            }
            
            OnAttacking?.Invoke(); //for the ai, attack accepted (request created)

            // Tell animator which attack variant this trigger represents
            m_animator.SetInteger(AttackType, attackType);
            m_animator.SetTrigger(AttackTrigger);

            // TTL bookkeeping (only if request succeeded)
            m_attackRequested = true;
            m_attackRequestedType = attackType;
            m_attackRequestExpiresAt = Time.time + m_attackRequestTTL;
        }

        private void FireComboAttackImmediate(int attackType)
        {
            float cost = GetAttackCost(attackType);
            if (cost <= 0f) return;

            if (!m_playerStatus.TryReserveStamina(cost, out m_comboAttackReservation))
            {
                // Strict mode: clear queued input on failed attack
                if (m_PlayerInputHandler.attackMode == PlayerInputHandler.AttackInputMode.QueuedPress)
                    m_PlayerInputHandler.ClearQueuedAttack();
                return;
            }
            
            OnAttacking?.Invoke(); //for the ai, attack accepted (request created)

            // Tell animator which attack variant this trigger represents
            m_animator.SetInteger(AttackType, attackType);
            m_animator.SetTrigger(AttackTrigger);

            // Immediate spend for combo chain (request and confirm are effectively the same)
            // unlike normal attack I will asumme combo's will always be executed without needing to wait for "request + confirm" 
            CommitReservedStaminaCost(ref m_comboAttackReservation);

            // Debounce window (rate limiter)
            m_attackTimer = Time.time + m_attackInputFrequency;
            m_canAttack = false;
        }

        public void AnimEvent_Done()
        {
            //NO NO NOOOOOOO (we use this in the smart knight enemy)
        }
        public void AnimEvent_LookDone()
        {
            //NO NO NOOOOOOO (we use this in the smart knight enemy)
        }
        
        public void AddParentVelocity(Vector3 velocityToAdd)
        {
            transform.position += velocityToAdd;
        }

        private bool GetIsGrounded()
        {
            LayerMask layerMask = LayerMask.GetMask("Default");
            RaycastHit hit;
            Vector3 origin = transform.position + transform.up * 0.5f;
            Vector3 target = transform.position + -transform.up;
            Vector3 direction = (target - origin).normalized;
            float radius = 0.3f;
            Physics.SphereCast(origin, radius, direction, out hit, 0.55f, layerMask);

            //if(hit.collider != null)
            //    print("we hit this: " + hit.collider.name);

            return hit.collider != null;
        }

        void Movement()
        {
            if(m_PlayerInputHandler.lockOnAction.WasPressedThisFrame())
            {
                if(m_lockOnTarget == null)
                {
                    TrySetLockOnTarget();
                }
                else
                {
                    EnableLockOnMode(false);
                }
            }

            if (m_characterController.isGrounded && GetIsGrounded())
            {
                m_mightBeFalling = false;
                m_groundedPlayer = true;
                m_playerVelocity = Vector3.zero;
                m_animator.SetBool(Falling, false);
            }

            if (!GetIsGrounded() && !m_characterController.isGrounded)
            {
                if (!m_mightBeFalling)
                {
                    m_mightBeFalling = true;
                    m_fallStateTimeStamp = Time.time;
                }

                if (Time.time > m_fallStateTimeStamp + m_enterFallStateTimer)
                {
                    m_groundedPlayer = false;
                }
            }


            Vector2 input = m_PlayerInputHandler.moveAction.ReadValue<Vector2>();
            float inputMag = Mathf.Clamp01(input.magnitude);

            // dont read too small jitter values as real inputs 
            if (inputMag < DEADZONE) 
            {
                input = Vector2.zero;
                inputMag = 0f;
            }

            // camera relative direction on ground plane
            Vector3 camForward = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up).normalized;
            Vector3 camRight   = Vector3.ProjectOnPlane(playerCamera.transform.right, Vector3.up).normalized;

            // build world direction from input and normalize directions safely 
            Vector3 worldDir = camForward * input.y + camRight * input.x;
            Vector3 worldDirNorm = worldDir.sqrMagnitude > 1e-8f ? worldDir.normalized : Vector3.zero;


            m_appliedGravityIntensity = gravityIntensity * 0.1f;

            if (!m_groundedPlayer)
            {
                m_isFalling = true;
                m_animator.SetBool(Falling, true);
                Vector3 fallingMovement = worldDirNorm;
                m_playerVelocity += fallingMovement * (0.2f * Time.deltaTime);
                m_playerVelocity.y += m_appliedGravityIntensity * Time.deltaTime;
                m_playerVelocity.y = Mathf.Clamp(m_playerVelocity.y, -7f, 0f);      // Testing clamping fall speed 
                m_characterController.Move(m_playerVelocity);
                return;
            }


            // NEW
            // stop updating if we are in attack animation, but some clips will still allow some aiming / steering controll while attacking
            bool skipLocomotion = _f.attackRelevant;
            if (skipLocomotion)
            {
                // Optional rotation help, only applies if AttackSteerDeg curve is present & > 0.
                // TryApplyAttackSteeringToTravelDir(worldDirNorm, inputMag);

                m_cachedAttackInputFrame = Time.frameCount;
                m_cachedAttackInputDir = worldDirNorm;          // cache last movement input direction (world, normalized) used for attack-steering / animator-driven movement during attack states
                m_cachedAttackInputMag = inputMag;              // cached last movement input magnitude (0..1)
                m_cachedAttackHasInput = inputMag > DEADZONE;

                return; // IMPORTANT: still block normal locomotion logic & free rotation while attacking or transitioning into attack.
            }

            
            bool chargeLock = _f.chargeRelevant || m_chargeElementRequested; 

            // if camera feels twitchy in idle state change to:  if (m_isStrafing && inputMag > 0.01f)
            // having rotation happen before the localDir gets sett might be more accurate for first frame of rotatin when strafing 
            if (!chargeLock && m_isStrafing && m_stateCurrent.tagHash != DodgeStateTag)   // NEW: Lock-on should now ignore dodge rolls 
            {
                bool inTransition = m_animator.IsInTransition(0);
                if (inTransition && m_animator.GetNextAnimatorStateInfo(0).tagHash == DodgeStateTag) // not yet in a dodge roll, but transitioning to one
                {
                    // turn player towards input direction
                    if (inputMag > 0f)
                    {
                        Quaternion targetInputRotation = Quaternion.LookRotation(worldDirNorm, Vector3.up);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetInputRotation, (rotationSpeed * 4) * Time.deltaTime); // still apply slerp rotation, but much faster than normally
                    }
                }


                Vector3 faceDir = GetStrafeFacingDir();
                Quaternion targetRotation = Quaternion.LookRotation(faceDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            else if (!chargeLock)
            {
                if (inputMag > 0f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(worldDirNorm, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }

            // convert character local direction for animator 
            Vector3 localDir = transform.InverseTransformDirection(worldDirNorm);
            m_moveX = localDir.x;
            m_moveZ = localDir.z;

            bool sprint = false;

                        
            if (!_f.blockRelevant)
                sprint = m_PlayerInputHandler.sprintAction.IsPressed(); 
            
            float moveTier = sprint ? 1f : 0.5f;      // set to sprint speed or to walk speed

            m_targetMoveSpeed= inputMag * moveTier;

            const float dampDir = 0.10f;
            //const float dampSpeed = 0.06f;
            m_animator.SetFloat(MoveX, m_moveX, dampDir, Time.deltaTime);
            m_animator.SetFloat(MoveZ, m_moveZ, dampDir, Time.deltaTime);
            //m_animator.SetFloat(Speed, m_targetMoveSpeed, dampSpeed, Time.deltaTime);

            // NEW - replaces two commented out lines above 
            float targetSpeed = m_targetMoveSpeed;
            float currentSpeed = m_animator.GetFloat(Speed);

            bool hasMoveInput = inputMag > 0f;

            // Pick different damping depending on what transition is happening
            float dampTime = !hasMoveInput ? stopDampTime :                         // movement to stop sped deceleration 
                (targetSpeed < currentSpeed ? runToWalkDampTime : accelDampTime);   // decel vs accel

            m_animator.SetFloat(Speed, targetSpeed, dampTime, Time.deltaTime);
            RuntimeManager.StudioSystem.setParameterByName("FootstepsVolume", targetSpeed);
            //print("speed: " + targetSpeed);

            //should we still use this?
            m_playerVelocity.y += m_appliedGravityIntensity * Time.deltaTime;
            m_characterController.Move(m_playerVelocity);

            //if (currentState.tagHash != AttackStateTag)
            //{
            //    trail.enabled = false;
            //}
        }


        private void TryApplyAttackSteeringToTravelDir(ref Vector3 travelDir, float dt)
        {
            if (m_cachedAttackInputFrame != Time.frameCount) return;
            if (!m_cachedAttackHasInput) return;

            if (m_cachedAttackInputDir.sqrMagnitude < 1e-8f) return;

            // If the curve doesn't exist / is not authored / is 0: Unity returns 0 -> and do nothing, ignoring movement input exactly as before.
            // Curve reading is caped 0-1 (and should on be set between those values anyways), and works as 0% - 100% of controll over character rotation
            float steer01 = Mathf.Clamp01(GetCurveFloatOrDefault(AttackSteerDeg, 0f));
            if (steer01 <= 0.001f) return;

            Vector3 desired = Vector3.ProjectOnPlane(m_cachedAttackInputDir, Vector3.up);
            if (desired.sqrMagnitude < 1e-8f) return;
            desired.Normalize();

            // interpreting AttackSteerDeg curve as:  (control-deg %)/sec
            float turnRateRadPerSec = steer01 * m_maxAttackSteerDegPerSec * Mathf.Deg2Rad;
            float maxTurnRadThisFrame = turnRateRadPerSec * dt;

            travelDir = Vector3.RotateTowards(travelDir, desired, maxTurnRadThisFrame, 0f).normalized;
        }

        private bool TryGetPlanarToTarget(Vector3 origin, Vector3 targetPoint, out Vector3 toTargetDir, out float dist)
        {
            // Direction to target (planar),    do we need 3D math here? 
            Vector3 toTarget = targetPoint - origin;
            Vector3 toTargetPlanar = Vector3.ProjectOnPlane(toTarget, Vector3.up);
                        
            dist = toTargetPlanar.magnitude;
            if (dist <= 1e-4f)                   // epsilon guard to prevent rounding errors
            {
                toTargetDir = default;
                return false;
            }

            toTargetDir = toTargetPlanar / dist;
            return true;
        }

        private Vector3 LockOnMagnetPostProcess(ref Vector3 displacement, Vector3 origin, float dt, bool attackRelevant, out bool magnetUsed)
        {
            magnetUsed = false;

            Vector3 travelDir = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (travelDir.sqrMagnitude < 1e-8f) travelDir = transform.forward;
            travelDir.Normalize();

            // Must have lock-on target and be in attack state gate
            if (!attackRelevant || m_lockOnTarget == null) return travelDir;

            // Curve gate
            bool magnetAllowedGate = GetCurveFloatOrDefault(AttackMagnetWindow, 0f) >= 0.5f;
            if (!magnetAllowedGate) return travelDir;

            // Direction to target (planar) 
            if (!TryGetPlanarToTarget(origin, m_lockOnTarget.position, out var toTargetDir, out var dist))      
                return travelDir;

            // Too far away for magnet to kick-in gate
            if (dist > magnetStartPullDist)
                return travelDir;

            // when magnet is active, apply travel direction correction aimed toward travelDir 
            float maxTurnRad = (magnetTurnRateDeg * Mathf.Deg2Rad) * dt;
            travelDir = Vector3.RotateTowards(travelDir, toTargetDir, maxTurnRad, 0f).normalized;

            // pull speed scales by proximity (stronger when closer)
            float proximity01 = Mathf.Clamp01(1f - (dist / magnetStartPullDist)); // 0 at edge, 1 near target
            float pullThisFrame = attackCorrectionPull * proximity01 * dt;

            // don’t overshoot, clamped to a stop distance - Will hopefully stop movement right before the enemy 
            float maxPullAllowed = Mathf.Max(0f, dist - magnetStopPullDist);
            pullThisFrame = Mathf.Min(pullThisFrame, maxPullAllowed);

            if (pullThisFrame > 0.0001f)
                displacement += toTargetDir * pullThisFrame;

            magnetUsed = true;
            return travelDir;
        }

        private void ClampTowardTargetStopDistance(ref Vector3 displacement, Vector3 origin, Vector3 targetPoint, float stopDist)
        {
            // Direction to target (planar) 
            if (!TryGetPlanarToTarget(origin, targetPoint, out var toTargetDir, out var dist))      
                return;

            // Check how much movement towards the target this frame, only clamp when approaching the stop distance
            Vector3 dispPlanar = Vector3.ProjectOnPlane(displacement, Vector3.up);

            float toward = Vector3.Dot(dispPlanar, toTargetDir);
            if (toward <= 0f) return; // if moving sideways or away, don't clamp

            if (dist <= stopDist)
            {
                // if allready inside stopDist, remove all toward-target motion this frame
                displacement -= toTargetDir * toward;
                return;
            }

            // Don't allow this frame to go closer than stopDist
            float maxTowardAllowed = dist - stopDist;
            if (toward > maxTowardAllowed)
            {
                float reduce = toward - maxTowardAllowed;
                displacement -= toTargetDir * reduce;
            }
        }


        private void OnAnimatorMove() //Animator movement - root movement
        {
            if (!m_updateCharacter) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // Might not be in sync with the _f. calls so recomputing relevance localy 
            bool inTrans = m_animator.IsInTransition(0);
            var cur = m_animator.GetCurrentAnimatorStateInfo(0);
            var next = inTrans ? m_animator.GetNextAnimatorStateInfo(0) : default;

            bool attackRelevant =
                cur.tagHash == AttackStateTag || (inTrans && next.tagHash == AttackStateTag);

            bool dodgeRelevant =
                cur.tagHash == DodgeStateTag || (inTrans && next.tagHash == DodgeStateTag);

            bool applyAttackMotion = attackRelevant || dodgeRelevant;

            Vector3 displacement = m_animator.deltaPosition;
            Vector3 rootDisp = displacement; // pure root motion for speed measurement

            if (applyAttackMotion)
            {
                // use this check somewhere
                bool hasFreshAttackInput =
                    (m_cachedAttackInputFrame == Time.frameCount) &&
                    m_cachedAttackHasInput &&
                    (m_cachedAttackInputDir.sqrMagnitude > 1e-8f);


                // use this instead of normal player transform for more accuracy?;
                //Vector3 origin = m_characterController.bounds.center;
                // or
                Vector3 origin = transform.position;

                // Magnet affects: travel direction + optional pull, is gated internally by attackRelevant + hasTarget + curve
                Vector3 travelDir = LockOnMagnetPostProcess(ref displacement, origin, dt, attackRelevant, out bool magnetUsed);

                // Apply rotation modifiers 
                if (!magnetUsed && hasFreshAttackInput)
                {
                    TryApplyAttackSteeringToTravelDir(ref travelDir, dt);
                }

                if (travelDir.sqrMagnitude > 1e-8f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(travelDir, Vector3.up);

                    float turnRateDeg = magnetUsed
                        ? magnetTurnRateDeg
                        : (Mathf.Clamp01(GetCurveFloatOrDefault(AttackSteerDeg, 0f)) * m_maxAttackSteerDegPerSec);

                    if (turnRateDeg > 0.01f)
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnRateDeg * dt);
                }

                // get artificial baseline speed top-up from curve, if curve is present
                float baseSpeed = Mathf.Max(0f, GetCurveFloatOrDefault(BaseForwardSpeed, 0f));

                // This might be what would work with the playrate modifier? if we want baseline to scale with animation playback speed,
                // multiply by effective playback speed
                //float playback = Mathf.Abs(cur.speed * cur.speedMultiplier);
                //baseSpeed *= playback;
                if (baseSpeed > 0f)
                {
                    Vector3 rootVelPlanar = Vector3.ProjectOnPlane(rootDisp / dt, Vector3.up);
                    float rootForwardSpeed = Mathf.Max(0f, Vector3.Dot(rootVelPlanar, travelDir));        // clamped, so should not cancle backward motion now 

                    float topUp = Mathf.Max(0f, baseSpeed - rootForwardSpeed);
                    displacement += travelDir * topUp * dt;
                }

                // addative boosts or modifiers, if curve is present 
                float boost = Mathf.Max(0f, GetCurveFloatOrDefault(AttackMomentum, 0f));
                if (boost > 0f)
                    displacement += travelDir * boost * dt;

                //displacement.y = 0f;              // fine to leave y value as it is or set to zero here?

                if (attackRelevant && m_lockOnTarget != null)
                {
                    Vector3 targetPoint = m_lockOnTarget.position;
                    ClampTowardTargetStopDistance(ref displacement, origin, targetPoint, magnetStopPullDist);
                }

                //trail.enabled = true;
            }

            m_characterController.Move(displacement);
        }

        float GetCurveFloatOrDefault(int hash, float defaultValue)
        {
            // If the parameter is NOT controlled by a curve, treat it as 0 to avoid bleed-over.
            return m_animator.IsParameterControlledByCurve(hash) 
                ? m_animator.GetFloat(hash) 
                : defaultValue;
        }

        private void UpdateAnimStatesInfo()
        {
            // Base layer (0)
            m_isInTransition = m_animator.IsInTransition(0);  // when in transitioning, checking current state will only see: “Is the FROM-state tagged Attack (even if we are already leaving it)
            m_stateCurrent = m_animator.GetCurrentAnimatorStateInfo(0); // (the curent state, or state being transitioned from)  
            m_stateNext = m_isInTransition ? m_animator.GetNextAnimatorStateInfo(0) : default;   // (the “to” state) Should always check m_inTransition before using!!! 

            // Torso layer
            m_torsoInTransition = m_animator.IsInTransition(_torsoLayerIndex);
            m_torsoStateCurrent = m_animator.GetCurrentAnimatorStateInfo(_torsoLayerIndex);
            m_torsoStateNext = m_torsoInTransition ? m_animator.GetNextAnimatorStateInfo(_torsoLayerIndex) : default;
        }

        private void GetInteruptStateFlags(out bool interuptedStable, out bool interuptedRelevant)
        {
            bool currentIsInterupt = m_stateCurrent.tagHash == InteruptStateTag;
            bool nextIsInterupt = m_isInTransition && (m_stateNext.tagHash == InteruptStateTag);

            // Fully committed inside a block state (no blending)
            interuptedStable = currentIsInterupt && !m_isInTransition;

            // Block is involved right now:
            interuptedRelevant = currentIsInterupt || nextIsInterupt;
        }

        private void GetDodgingStateFlags(out bool dodgeStable, out bool dodgeRelevant)
        {
            // Currently counting dodging as when we are in the dodge state or currently transitioning into it. 
            bool currentIsDodge = m_stateCurrent.tagHash == DodgeStateTag;

            bool nextIsDodge = false;
            if (m_isInTransition)
            {
                nextIsDodge = m_stateNext.tagHash == DodgeStateTag;
            }

            // Fully committed inside a dodge state (no blending)
            dodgeStable = currentIsDodge && !m_isInTransition;

            // Dodge is involved right now:
            dodgeRelevant = currentIsDodge || (m_isInTransition && nextIsDodge);
        }

        private void GetBlockingStateFlags(out bool blockStable, out bool blockRelevant)
        {
            bool currentIsBlock = m_torsoStateCurrent.tagHash == BlockStateTag;
            bool nextIsBlock = m_torsoInTransition && (m_torsoStateNext.tagHash == BlockStateTag);

            // Fully committed inside a block state (no blending)
            blockStable = currentIsBlock && !m_torsoInTransition;

            // Block is involved right now:
            blockRelevant = currentIsBlock || nextIsBlock;
        }

        private void GetMagicStateFlags(out bool chargeStable, out bool chargeRelevant, out bool spellStable, out bool spellRelevant)
        {
            bool currentIsCharge = m_stateCurrent.tagHash == ChargeStateTag;
            bool nextIsCharge = m_isInTransition && (m_stateNext.tagHash == ChargeStateTag);

            bool currentIsSpellCast = m_stateCurrent.tagHash == SpellcastingStateTag;
            bool nextIsSpellCast = m_isInTransition && (m_stateNext.tagHash == SpellcastingStateTag);

            // Fully committed inside a state (no blending)
            chargeStable = currentIsCharge && !m_isInTransition;
            spellStable = currentIsSpellCast && !m_isInTransition;

            // State is involved right now:
            chargeRelevant = currentIsCharge || (m_isInTransition && nextIsCharge);
            spellRelevant = currentIsSpellCast || (m_isInTransition && nextIsSpellCast);
        }

        // should create more stability and no flickering states while transitioning.
        // Attack-state rules to remain active during:
        //      - Attack1 -> Attack2 blends
        //      - Movement -> Attack blends (so player don’t briefly regain locomotion/rotation)
        //      - Attack -> Movement blends (so player don’t get a brief free rotate burst on exit)
        //      - Trails and special effects wont flicker in transitions 
        private void GetAttackStateFlags(out bool attackStable, out bool attackRelevant)
        {      
            bool currentIsAttack = m_stateCurrent.tagHash == AttackStateTag;

            bool nextIsAttack = false;
            if (m_isInTransition)
            {
                nextIsAttack = m_stateNext.tagHash == AttackStateTag;
            }

            // Fully committed inside an attack state (no blending)
            attackStable = currentIsAttack && !m_isInTransition;

            // Attack is involved right now:
            // - either we are in an attack state already
            // - or we are blending into an attack state
            attackRelevant = currentIsAttack || (m_isInTransition && nextIsAttack);
        }

        private float GetAttackCost(int attackType)
        {
            return attackType == 1 ? lightAttackCost :
                   attackType == 2 ? heavyAttackCost :
                   0f;
        }

        private void ClearAttacksFromAnimator()
        {
            m_animator.ResetTrigger(AttackTrigger);
            m_animator.SetInteger(AttackType, 0);
        }

        
        // NOTE: camera is a placeholder for testing strafing,
        // later replace with something like (target.position - transform.position) projected onto plane.
        private Vector3 GetStrafeFacingDir()
        {
            
            Vector3 faceDir = Vector3.ProjectOnPlane(m_lockOnTarget.position - transform.position, Vector3.up); //add lock on target here
            if (faceDir.sqrMagnitude < 1e-6f) return transform.forward;

            return faceDir.normalized;
        }

        private void EnableLockOnMode(bool enable) //handle hud and other things for lock on mode here
        {
            if (enable)
            {
                m_isStrafing = enable;
                OnLockOnModeUpdate?.Invoke(enable, m_lockOnTarget, m_lockOnTarget.GetComponent<ILockOnAble>().GetElementWeakness());
            }
            else
            {
                m_lockOnTarget.GetComponent<ILockOnAble>().StopBeingLockedOn();
                m_isStrafing = enable;
                OnLockOnModeUpdate?.Invoke(enable, m_lockOnTarget, ElementTypes.ElementType.NoElement);
                m_lockOnTarget = null;
            }

        }

        private void TrySetLockOnTarget()
        {
            if (m_lockOnTarget != null)
                return;
            
            Physics.SphereCast(lockOnCastPoint.position, 1.6f, lockOnCastPoint.forward,
                out RaycastHit hit, playerCamera.lockOnDistance, playerCamera.lockOnLayerMask);
            
            if(hit.collider != null)
            {
                m_lockOnTarget = hit.transform;
                EnableLockOnMode(true);
            }
        }

        public void DisableLockOn()
        {
            m_isStrafing = false;
            m_lockOnTarget = null;
            OnLockOnModeUpdate?.Invoke(false, m_lockOnTarget, ElementTypes.ElementType.NoElement); //what???
        }

        private void StopStrafing()
        {
            EnableLockOnMode(false);
        }

        public void EnableCharacter(bool enable)
        {
            m_updateCharacter = enable;
        }
        
        IEnumerator WaitToStartUpdate(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            m_updateCharacter = true;
        }
        
    }
}