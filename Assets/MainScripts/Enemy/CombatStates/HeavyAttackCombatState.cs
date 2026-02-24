using System;
using UnityEngine;

public class HeavyAttackCombatState : FsmCombatState
{
    public override FsmCombatActionType GetCombatActionType()
    {
        return FsmCombatActionType.AttackAction;
    }
    
    public override FsmEnemyCombat GetStateTag()
    {
        return FsmEnemyCombat.HeavyAttack;
    }

    public override void EnterState()
    {
        base.EnterState();
        fsmOwner.npcFsmController.DoHeavyAttack();
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

    public override bool CanEnter()
    {
        return base.CanEnter();
    }
}
