using UnityEngine;

public interface ILockOnAble
{
    public Transform GetLockOnTarget();
    public void StopBeingLockedOn();
    public bool UseBossHealthBar();
    public ElementTypes.ElementType GetElementWeakness(); //funka dååååå
}
