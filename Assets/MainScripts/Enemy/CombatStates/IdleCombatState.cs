using UnityEngine;

public class IdleCombatState : FsmCombatState
{
    public override FsmCombatActionType GetCombatActionType()
    {
        return FsmCombatActionType.IdleAction;
    }
    
    public override FsmEnemyCombat GetStateTag()
    {
        return FsmEnemyCombat.CombatIdle;
    }

    public override bool CanEnter()
    {
        return true;
    }

    public override void EnterState()
    {
        base.EnterState();
        fsmOwner.npcFsmController.DoIdle();
    }
}
