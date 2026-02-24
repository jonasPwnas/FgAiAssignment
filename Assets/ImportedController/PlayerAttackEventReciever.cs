using UnityEngine;

public class PlayerAttackEventReciever : MonoBehaviour
{
    public delegate void ToggleHitbox(bool enable);
    public static event ToggleHitbox OnToggleHitbox;

    public delegate void DoToggleIFrames();
    public static event DoToggleIFrames OnDoToggleIFrames;

    public delegate void Blocking();
    public static event Blocking OnToggleBlocking;
    

    public void EnableHitbox(int enableHitbox)//called from animation events
    {
        if(enableHitbox == 1)
            OnToggleHitbox?.Invoke(true);
        
        if(enableHitbox == 0)
            OnToggleHitbox?.Invoke(false);
        //please get into perforce NOW!!!!
    }

    public void DoAttack()
    {
        //Do attack things here
    }

    public void ToggleIFrames()
    {
        OnDoToggleIFrames?.Invoke();
    }

    public void ToggleBlocking()
    {
        OnToggleBlocking?.Invoke();
    }
}
