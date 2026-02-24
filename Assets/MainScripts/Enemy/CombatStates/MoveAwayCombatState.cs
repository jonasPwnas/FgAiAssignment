using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class MoveAwayCombatState : FsmCombatState
{
    [Header("Stop this behavior when this far away from the target")]
    [SerializeField] private float stopMoveAwayDistance = 5.5f;
    
    private bool m_hasMoveAwayTarget;

    public override bool CanEnter()
    {
        return fsmOwner.GetDistanceToTarget() < 2.5f;
    }

    public override FsmCombatActionType GetCombatActionType()
    {
        return FsmCombatActionType.MovementAction;
    }

    public override FsmEnemyCombat GetStateTag()
    {
        return FsmEnemyCombat.MoveAway;
    }

    public override void EnterState()
    {
        base.EnterState();
        m_hasMoveAwayTarget = false;
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

    private Vector3 GetMoveAwayLocation(Vector3 moveToRandom, float moveToRandomCheckRange)
    {
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomPoint = moveToRandom + Random.insideUnitSphere * moveToRandomCheckRange;
            NavMeshHit hit;

            if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        return transform.position;
    }

    IEnumerator DoMoveToTrackedTarget(float trackFreq)
    {
        if (fsmOwner.GetDistanceToTarget() > stopMoveAwayDistance)
        {
            print("stopped move away");
            fsmOwner.npcFsmController.StopMoveTo();
            m_hasMoveAwayTarget = false;
            ExitState();
            yield break;
        }

        print("tries move away");

        if(!m_hasMoveAwayTarget)
        {
            Vector3 moveToRandom = transform.position + transform.forward * Random.Range(-7f, -4f);
            fsmOwner.npcFsmController.DoMoveTo(GetMoveAwayLocation(moveToRandom, 10f));
            m_hasMoveAwayTarget = true;
        }
        yield return new WaitForSeconds(trackFreq);
        StartCoroutine(DoMoveToTrackedTarget(trackFreq));
    }
}