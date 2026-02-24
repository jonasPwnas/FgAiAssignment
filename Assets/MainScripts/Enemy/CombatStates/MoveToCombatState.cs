using System.Collections;
using UnityEngine;

public class MoveToCombatState : FsmCombatState
{

    [Header("Stop this behavior when this close to the target")]
    [Tooltip("Stop moving toward target, will instead move away if no attack is available")]
    [SerializeField] private float stopMoveToDistance = 2.5f;
    
    public override FsmCombatActionType GetCombatActionType()
    {
        return FsmCombatActionType.MovementAction;
    }
    
    public override FsmEnemyCombat GetStateTag()
    {
        return FsmEnemyCombat.MoveTo;
    }

    public override void EnterState()
    {
        base.EnterState();  
        StartCoroutine(DoMoveToTrackedTarget(0.5f));
    }

    public override void ExitState()
    {
        StopAllCoroutines();
        base.ExitState();
    }

    public override void AbortState(bool doCooldown)
    {
        StopAllCoroutines();
        fsmOwner.npcFsmController.StopMoveTo();
        base.AbortState(doCooldown);
    }

    IEnumerator DoMoveToTrackedTarget(float trackFreq)
    {
        if (fsmOwner.GetDistanceToTarget() < stopMoveToDistance)
        {
            fsmOwner.npcFsmController.StopMoveTo();
            ExitState();
            yield break;
        }
        
        fsmOwner.npcFsmController.DoMoveTo(fsmOwner.playerController.transform.position);
        yield return new WaitForSeconds(trackFreq);
        StartCoroutine(DoMoveToTrackedTarget(trackFreq));
    }
}
