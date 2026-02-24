using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Input
{
    public class PlayerInputHandler : MonoBehaviour
    {
        [HideInInspector] public InputAction moveAction;
        [HideInInspector] public InputAction jumpAction;
        [HideInInspector] public InputAction sprintAction;
        [HideInInspector] public InputAction attackAction1;
        [HideInInspector] public InputAction pauseAction;
        [HideInInspector] public InputAction chargeElementAction;
        [HideInInspector] public InputAction cycleForwardAction;
        [HideInInspector] public InputAction cycleBackwardAction;
        [HideInInspector] public InputAction lockOnAction;
        [HideInInspector] public InputAction attackAction2;
        [HideInInspector] public InputAction blockAction;
        [HideInInspector] public InputAction dodgeAction;
        [HideInInspector] public InputAction castSpellAction;


        // NOTE: These are for testing but miiight be kept as settings setting?
        public enum AttackInputMode { QueuedPress, Held }

        public AttackInputMode attackMode = AttackInputMode.QueuedPress;




        // Design Question: Do we want to allow �hold to auto-chain attacks or one press = one attempt?
        //                  When using the IsPressed() it will auto-repeat logg a held down key every 0.25s
        //                  If we don't wan't that maybe using something like WasPressedThisFrame()


        // When choosing between option 1 and 2, we could keep the second way as an accessability feature? with default setting to way-1. 
        // Design way 1: option using .IsPressed() allows attack buttons to be held down.
        //               Held mode will re-trigger as soon as m_canAttack becomes true again (which might be fine for �hold to auto-chain�).
        //               If we don�t want auto-repeat, use edge input (queued press) only.
        // Design way 2: only store the last pressed attack, only registers att the moment and wont be activated by holding button down


        // Design way 1:
        public bool AnyAttackHeld =>
            (attackAction1?.IsPressed() ?? false) || (attackAction2?.IsPressed() ?? false);

        // Design way 2:
        public int QueuedAttackType { get; private set; }   // 0=None, 1=Light, 2=Heavy
        public bool HasQueuedAttack => QueuedAttackType != 0;





        private void Awake()
        {
            moveAction = InputSystem.actions.FindAction("Move");
            dodgeAction = InputSystem.actions.FindAction("Dodge");
            blockAction = InputSystem.actions.FindAction("Block");
            sprintAction = InputSystem.actions.FindAction("Sprint"); 
            attackAction1 = InputSystem.actions.FindAction("Attack1");
            attackAction2 = InputSystem.actions.FindAction("Attack2");
            pauseAction = InputSystem.actions.FindAction("Pause");
            lockOnAction = InputSystem.actions.FindAction("LockOn");
            chargeElementAction = InputSystem.actions.FindAction("ChargeElement");
            castSpellAction = InputSystem.actions.FindAction("CastSpell");
            cycleBackwardAction = InputSystem.actions.FindAction("CycleBackward");
            cycleForwardAction = InputSystem.actions.FindAction("CycleForward");
        }



        private void OnEnable()
        {
            if (attackAction1 != null) attackAction1.started += OnAttackStarted;
            if (attackAction2 != null) attackAction2.started += OnAttackStarted;
        }

        private void OnDisable()
        {
            if (attackAction1 != null) attackAction1.started -= OnAttackStarted;
            if (attackAction2 != null) attackAction2.started -= OnAttackStarted;
        }


        // NOTE: I am testing way 1 and way 2, if we decide to only keep one method the entry point will be TryConsumeQueuedAttack / GetAttackTypeHeld
        public bool TryGetAnyAttack(out int type)
        {
            if (attackMode == AttackInputMode.QueuedPress)
                return TryConsumeQueuedAttack(out type);

            type = GetAttackTypeHeld();
            return type != 0;
        }


        // Design way 1:
        public int GetAttackTypeHeld()
        {
            bool lightAttack = attackAction1?.IsPressed() ?? false;
            bool heavyAttack = attackAction2?.IsPressed() ?? false;

            if (lightAttack) return 1;  // first one will get pririty when contested, so light attacks are currently default. 
            if (heavyAttack) return 2;
            return 0;

        }

        // Design way 2:
        private void OnAttackStarted(InputAction.CallbackContext ctx)
        {
            if (ctx.action == attackAction1) QueueAttackType(1);
            else if (ctx.action == attackAction2) QueueAttackType(2);
        }

        private void QueueAttackType(int type)
        {
            // Also have to choose between:
            //
            //      1. don't overwrite if something is already queued unless higher priority. If both get pressed, define deterministic priority.
            //if (QueuedAttackType == 0 || type > QueuedAttackType)
            //    QueuedAttackType = type;
            //
            //      2. allowe overwrite so last pressed attack Id is allways stored.
            QueuedAttackType = type;

        }

        public bool TryConsumeQueuedAttack(out int type)
        {
            type = QueuedAttackType;
            if (type == 0) return false;

            QueuedAttackType = 0;
            return true;
        }

        public void ClearQueuedAttack()
        {
            QueuedAttackType = 0;
        }

    }
}