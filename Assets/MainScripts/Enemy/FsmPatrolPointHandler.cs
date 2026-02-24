using System;
using System.Collections.Generic;
using UnityEngine;

public class FsmPatrolPointHandler : MonoBehaviour
{
    private List<Transform> m_patrolPoints = new List<Transform>();
    private int m_currentPatrolPointIndex = 0;
    
    
    private void Awake()
    {
        m_patrolPoints.AddRange(transform.GetComponentsInChildren<Transform>());
    }

    //public Transform GetClosestPatrolPoint(Vector3 currentPosition) nice to have
    //{
    //    
    //}

    public Transform GetNextPatrolPoint()
    {
        m_currentPatrolPointIndex++;
        
        if (m_currentPatrolPointIndex == m_patrolPoints.Count)
        {
            m_currentPatrolPointIndex = 0;
        }
        
        return m_patrolPoints[m_currentPatrolPointIndex];
    }
}
