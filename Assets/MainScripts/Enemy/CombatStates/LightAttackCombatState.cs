using UnityEngine;

public class LightAttackCombatState : FsmCombatState
{
    public override FsmCombatActionType GetCombatActionType()
    {
        return FsmCombatActionType.AttackAction;
    }

    public override FsmEnemyCombat GetStateTag()
    {
        return FsmEnemyCombat.LightAttack;
    }

    public override void EnterState()
    {
        base.EnterState();
        fsmOwner.npcFsmController.DoLightAttack();
    }
    

    public override void ExitState()
    {
        base.ExitState();
    }

    public override void AbortState(bool doCooldown)
    {
        base.AbortState(doCooldown);
    }

    public override void UpdateState()
    {
        if (fsmOwner.animator.GetNextAnimatorStateInfo(0).IsTag("Movement"))
        {
            ExitState();
        }
        else if (fsmOwner.animator.GetAnimatorTransitionInfo(0).anyState)
        {
            AbortState(true);
        }
    }

    public override bool UseUpdate()
    {
        return true;
    }

    public override bool CanEnter()
    {
        return base.CanEnter();
    }
}
