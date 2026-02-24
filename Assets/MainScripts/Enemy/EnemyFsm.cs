using System;
using System.Collections;
using System.Collections.Generic;
using Characters;
using Player;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;


public enum FsmEnemyStates
{
    Idle,
    Patrol,
    Combat,
    Dead,
    Stunned
}

public enum FsmEnemyCombat
{
    CombatIdle,
    MoveTo,
    MoveAway,
    AttackCombo,
    LightAttack,
    HeavyAttack,
    RangedAttack,
    JumpAway,
    JumpToPlayer,
    Hit,
    KnockedDown
}

public enum FsmCombatActionType
{
    AttackAction,
    MovementAction,
    IdleAction,
    DamagedAction
}

public class EnemyFsm : MonoBehaviour
{
    //Editor exposed
    [SerializeField] private FsmEnemyStates startingState = FsmEnemyStates.Idle;
    [SerializeField] private FsmEnemyCombat startingCombatState = FsmEnemyCombat.CombatIdle;
    [SerializeField] public float generalAttackCooldown = 1f;
    [SerializeField] private float enterCombatMinDistance = 15f; //1 = 1m
    [SerializeField] private float enterCombatMinViewAngle = 180f;
    [SerializeField] private float enterCombatAllowedHeightDifference = 1.5f;
    [SerializeField] private float stoppingDistance = 1.2f;
    [Header("Add object that holds the combat-state-components here")] [SerializeField]
    private GameObject combatStatesHolder;
    [Header("Add the object holding the patrol points here, leave empty if stationary")] [SerializeField]
    private FsmPatrolPointHandler patrolPointsHolder;
    [Header("Layers for the AI to trace against when detecting")] [SerializeField]
    private LayerMask m_sightlineMask;

    //public
    [HideInInspector] public FsmRootMotionController npcFsmController;
    [HideInInspector] public PlayerController playerController;
    [HideInInspector] public Animator animator;

    //members
    private List<FsmCombatState> m_allAttackStates = new List<FsmCombatState>();
    private List<FsmCombatState> m_combatAttackStates = new List<FsmCombatState>();
    private List<FsmCombatState> m_combatMovementStates = new List<FsmCombatState>();
    private List<FsmCombatState> m_combatDamagedStates = new List<FsmCombatState>();
    private List<FsmCombatState> m_combatIdleStates = new List<FsmCombatState>();
    private FsmEnemyStates m_currentState;
    private FsmCombatState m_currentCombatState;
    private NavMeshAgent m_navAgent;
    private bool m_onGeneralAtkCooldown;
    private Coroutine m_enterCombatCoroutine;
    private Coroutine m_patrolCoroutine;
    private Coroutine m_distanceCheckCoroutine;
    
    
    //Constants
    private const float ENTER_COMBAT_CHECK_FREQUENCY = 0.5f;
    private const float UPDATE_COMBAT_STATE_CHECK_FREQUENCY = 0.5f;

    private void Awake()
    {
        playerController = FindAnyObjectByType<PlayerController>();
        npcFsmController = GetComponent<FsmRootMotionController>();
        animator = GetComponent<Animator>();
        m_navAgent = GetComponent<NavMeshAgent>();
        m_allAttackStates.AddRange(combatStatesHolder.GetComponents<FsmCombatState>());
    }

    private void Start()
    {
        foreach (var state in m_allAttackStates)
        {
            state.SetOwnerFsm(this);
        }

        foreach (var state in m_allAttackStates) //sort the states
        {
            switch (state.GetCombatActionType())
            {
                case FsmCombatActionType.IdleAction:
                    m_combatIdleStates.Add(state);
                    break;
                case FsmCombatActionType.AttackAction:
                    m_combatAttackStates.Add(state);
                    break;
                case FsmCombatActionType.MovementAction:
                    m_combatMovementStates.Add(state);
                    break;
                case FsmCombatActionType.DamagedAction:
                    m_combatDamagedStates.Add(state);
                    break;
            }
        }

        m_currentCombatState = GetCombatStateFromEnumTag(startingCombatState);
        if (startingState == FsmEnemyStates.Patrol && patrolPointsHolder != null)
        {
            EnterNewMainState(FsmEnemyStates.Patrol);
        }
        else
        {
            EnterNewMainState(FsmEnemyStates.Idle);
        }
    }

    public void SetPatrolState()
    {
        EnterNewMainState(FsmEnemyStates.Patrol);
    }

    public void SetStunnedState()
    {
        EnterNewMainState(FsmEnemyStates.Stunned);
    }

    public FsmEnemyStates GetCurrentFsmMainState()
    {
        return m_currentState;
    }

    private void Update()
    {
        if (m_currentState == FsmEnemyStates.Dead)
            return;
        if (m_currentState != FsmEnemyStates.Combat)
            return;
        if (m_currentCombatState == null)
        {
            // this gets nulled, why? It shouldn't. does it happen on death?
            print("Combat state is null!?");
            return;
        }

        if (!m_currentCombatState.UseUpdate())
            return;

        print("Is in combat state:  " + m_currentCombatState.GetStateTag());


        m_currentCombatState.UpdateState();
    }

    private void EnterNewMainState(FsmEnemyStates newState)
    {
        switch (newState)
        {
            case FsmEnemyStates.Combat:
                StopAllCoroutines();
                m_currentCombatState = m_combatIdleStates[0];
                EnterNewCombatState(GetBestCombatState());
                break;

            case FsmEnemyStates.Patrol:
                m_patrolCoroutine = StartCoroutine(DoPatrol(1f, 1f));
                m_enterCombatCoroutine = StartCoroutine(CheckShouldEnterCombatState());
                m_navAgent.SetDestination(patrolPointsHolder.GetNextPatrolPoint().position);
                break;

            case FsmEnemyStates.Idle:
                m_enterCombatCoroutine = StartCoroutine(CheckShouldEnterCombatState());
                break;

            case FsmEnemyStates.Dead: //make sure to turn everything off, maybe disable this whole thing
                StopAllCoroutines();
                npcFsmController.OnDeath();
                break;
            case FsmEnemyStates.Stunned: //make sure to turn everything off, maybe disable this whole thing
                StopAllCoroutines();
                StartCoroutine(StunnedTimer());
                break;
        }

        m_currentState = newState;
    }

    //private float GetDistanceFreq()
    //{
    //    if (m_currentState != FsmEnemyStates.Combat)
    //    {
    //        return NON_COMBAT_DISTANCE_FREQUENCY;
    //    }
    //    else
    //    {
    //        return COMBAT_DISTANCE_FREQUENCY;
    //    }
    //}

    public void OnDeath()
    {
        if (m_currentCombatState != null)
            m_currentCombatState.AbortState(true);

        print("enemy " + gameObject.name + " died!!!! in FSM");
        EnterNewMainState(FsmEnemyStates.Dead);
    }

    public void OnDamaged()
    {
        if (m_currentCombatState != null)
            m_currentCombatState.AbortState(true);
        animator.SetTrigger("Damaged");
    }

    private void EnterNewCombatState(FsmCombatState newState)
    {
        if (m_currentCombatState.IsActiveState())
        {
            m_currentCombatState.AbortState(false);
            EnterNewCombatState(GetBestCombatState());
            return;
        }

        if (newState.GetCombatActionType() != FsmCombatActionType.AttackAction)
        {
            StartCoroutine(GetNewCombatStateWhenMoving());
        }
        
        if(newState.GetCombatActionType() == FsmCombatActionType.AttackAction
           && m_onGeneralAtkCooldown)
        {
            EnterNewCombatState(GetBestCombatState());
            return;
        }
        if(newState.GetCombatActionType() == FsmCombatActionType.AttackAction)
        {
            m_onGeneralAtkCooldown = true;
        }
        
        m_currentCombatState = newState;
        m_currentCombatState.EnterState();
    }

    public void OnDidExitCombatState()
    {
        if (m_currentCombatState.GetCombatActionType() == FsmCombatActionType.AttackAction)
        {
            StartCoroutine(GeneralAttackCooldown());
        }
        EnterNewCombatState(GetBestCombatState());
    }

    private FsmCombatState GetCombatStateFromEnumTag(FsmEnemyCombat stateEnumTag)
    {
        return m_combatAttackStates.Find(delegate(FsmCombatState s)
        {
            return s.thisCombatStateTag.Equals(stateEnumTag);
        });
    }

    public float GetDistanceToTarget()
    {
        return Vector3.Distance(transform.position, playerController.transform.position);
    }

    private bool PlayerIsVisible()
    {
        if (playerController.transform.position.y - transform.position.y > enterCombatAllowedHeightDifference)
            return false;

        Vector3 dirToTarget = playerController.transform.position - transform.position;
        if (Vector3.Angle(transform.forward, dirToTarget) <= enterCombatMinViewAngle)
        {
            Vector3 origin = transform.position + Vector3.up;
            Vector3 target = playerController.transform.position + Vector3.up;
            Vector3 direction = (target - origin).normalized;

            RaycastHit hit;
            Physics.Raycast(origin, direction, out hit, enterCombatMinDistance, m_sightlineMask); //blork

            if (hit.collider != null)
            {
                return hit.collider.gameObject == playerController.gameObject;
            }
        }

        return false;
    }


    private FsmCombatState GetBestCombatState()
    {
        List<FsmCombatState> avaliableCombatStates = new List<FsmCombatState>();

        if (!m_onGeneralAtkCooldown && AttacksAreAvaliable())
        {
            foreach (FsmCombatState state in m_combatAttackStates)
            {
                if (state.CanEnter())
                {
                    avaliableCombatStates.Add(state);
                }
            }
        }
        else if (MovementsAreAvaliable())
        {
            foreach (FsmCombatState state in m_combatMovementStates)
            {
                if (state.CanEnter())
                {
                    avaliableCombatStates.Add(state);
                }
            }
        }
        else if (IdlesAreAvaliable())
        {
            foreach (FsmCombatState state in m_combatIdleStates)
            {
                if (state.CanEnter())
                {
                    avaliableCombatStates.Add(state);
                }
            }
        }

        float minWeight = 0f;
        FsmCombatState bestState = null;

        foreach (FsmCombatState state in avaliableCombatStates)
        {
            if (state.stateWeight > minWeight)
            {
                minWeight = state.stateWeight;
                bestState = state;
            }
        }

        if (bestState == null)
        {
            return m_combatIdleStates[0];
        }

        return bestState;
    }

    private bool AttacksAreAvaliable()
    {
        foreach (FsmCombatState state in m_combatAttackStates)
        {
            if (state.CanEnter())
            {
                return true;
            }
        }

        return false;
    }

    private bool MovementsAreAvaliable()
    {
        foreach (FsmCombatState state in m_combatMovementStates)
        {
            if (state.CanEnter())
            {
                return true;
            }
        }

        return false;
    }

    private bool IdlesAreAvaliable()
    {
        foreach (FsmCombatState state in m_combatIdleStates)
        {
            if (state.CanEnter())
            {
                return true;
            }
        }

        return false;
    }

    IEnumerator GetNewCombatStateWhenMoving()
    {
        FsmCombatState newState = GetBestCombatState();

        if (newState != null && newState.GetCombatActionType() == FsmCombatActionType.AttackAction)
        {
            EnterNewCombatState(newState);
            yield break;
        }

        yield return new WaitForSeconds(UPDATE_COMBAT_STATE_CHECK_FREQUENCY);
        StartCoroutine(GetNewCombatStateWhenMoving());
    }


    IEnumerator CheckShouldEnterCombatState()
    {
        yield return new WaitForSeconds(ENTER_COMBAT_CHECK_FREQUENCY);

        if (GetDistanceToTarget() < enterCombatMinDistance && PlayerIsVisible())
        {
            EnterNewMainState(FsmEnemyStates.Combat);
            yield break;
        }

        m_enterCombatCoroutine = StartCoroutine(CheckShouldEnterCombatState());
    }

    IEnumerator DoPatrol(float updateFrequency, float patrolPause)
    {
        yield return new WaitForSeconds(updateFrequency);

        if (m_navAgent.remainingDistance <= m_navAgent.stoppingDistance)
        {
            yield return new WaitForSeconds(patrolPause);
            m_navAgent.SetDestination(patrolPointsHolder.GetNextPatrolPoint().position);
        }

        m_patrolCoroutine = StartCoroutine(DoPatrol(updateFrequency, Random.Range(1.2f, 3f)));
    }

    IEnumerator GeneralAttackCooldown()
    {
        yield return new WaitForSeconds(generalAttackCooldown);
        m_onGeneralAtkCooldown = false;
    }

    IEnumerator StunnedTimer()
    {
        npcFsmController.DoStunned();
        yield return new WaitForSeconds(5f);
        EnterNewMainState(FsmEnemyStates.Combat);
        npcFsmController.StopStunned();
    }
}