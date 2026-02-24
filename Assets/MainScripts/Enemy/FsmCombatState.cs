using System;
using System.Collections;
using UnityEngine;

public class FsmCombatState : MonoBehaviour
{
    [Header("We use this value to prioritize actions when several are available, higher value = more important")]
    [Range(0.001f, 9f)]
    [SerializeField] public float stateWeight = 4f;
    [Header("The time it takes before we can enter this state again")]
    [SerializeField] private float stateCooldown = 2.5f;
    [Header("Is distance a factor for activating this state?")]
    [SerializeField] private bool useDistance = true;
    [Header("The closest we can be to target to activate the state")]
    [SerializeField] public float minActivationDistance = 1f;
    [Header("The furthest we can be to target to activate the state")]
    [SerializeField] public float maxActivationDistance = 10f;

    [HideInInspector] public FsmEnemyCombat thisCombatStateTag;
    [HideInInspector] public EnemyFsm fsmOwner;
    
    private bool m_onCooldown = false;
    private bool m_inState = false;

    public virtual void SetOwnerFsm(EnemyFsm  fsmOwner)
    {
        this.fsmOwner = fsmOwner;
    }

    public virtual FsmCombatActionType GetCombatActionType()
    {
        return FsmCombatActionType.IdleAction;
    }
    
    public virtual FsmEnemyCombat GetStateTag()
    {
        return thisCombatStateTag;
    }

    public bool IsActiveState()
    {
        return m_inState;
    }
    
    public virtual void EnterState()
    {
        m_inState = true;
    }

    public virtual void ExitState()
    {
        m_inState = false;
        m_onCooldown = true;
        StartCoroutine(DoStateCooldown()); 
        fsmOwner.OnDidExitCombatState();
    }

    public virtual void AbortState(bool doCooldown)
    {
        if (doCooldown)
        {
            StartCoroutine(DoStateCooldown());
        }
        m_inState = false;
    }
    
    /// <summary>
    /// Use this instead of regular update, we update instead from the state machine
    /// </summary>
    public virtual void UpdateState() 
    {
        //no?
    }

    public virtual bool UseUpdate()
    {
        return true;
    }
    
    public virtual bool CanEnter()
    {
        if(useDistance)
        {
            float distance = fsmOwner.GetDistanceToTarget();
            return !m_inState && !m_onCooldown && distance > minActivationDistance && distance < maxActivationDistance;
        }
        else
        {
            return !m_inState && !m_onCooldown;
        }
    }

    IEnumerator DoStateCooldown()
    {
        yield return new WaitForSeconds(stateCooldown);
        m_onCooldown = false;
    }
}
