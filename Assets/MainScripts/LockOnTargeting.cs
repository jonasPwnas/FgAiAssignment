using System;
using UnityEngine;

public class LockOnTargeting : MonoBehaviour, ILockOnAble
{

    public delegate void StopLockOn();
    public static event StopLockOn OnStopLockOn;
    
    [SerializeField] private Transform lockOnIndicatorPosition;
    
    private bool m_isLockedOnByPlayer = false;


    public void StopLockOnWhenPhysics()
    {
        if(m_isLockedOnByPlayer)
            OnStopLockOn?.Invoke();
    }
    
    private void OnDestroy()
    {
        OnStopLockOn?.Invoke();
    }

    public Transform GetLockOnTarget()
    {
        m_isLockedOnByPlayer = true;
        return lockOnIndicatorPosition;
    }

    public void StopBeingLockedOn()
    {
        m_isLockedOnByPlayer = false;
    }

    public bool UseBossHealthBar()
    {
        return false;
    }

    public ElementTypes.ElementType GetElementWeakness()
    {
        return ElementTypes.ElementType.NoElement;
    }
}
