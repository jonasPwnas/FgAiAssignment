using UnityEngine;

public class AttackEnterResetSMB : StateMachineBehaviour
{
    static readonly int AttackMomentum = Animator.StringToHash("AttackMomentum");
    static readonly int AttackSteerDeg = Animator.StringToHash("AttackSteerDeg");

    // NOTE: This script is meant to fix missing-curve stickiness inside AttackSM combos,
    //       as not all clips have all curves, but parameter values might persist and effect next state anyways.
    //       No functionality change
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetFloat(AttackMomentum, 0f);
        animator.SetFloat(AttackSteerDeg, 0f);
    }

}
