using System.Collections;
using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

public class OpenDoorOnEnemiesKilled : MonoBehaviour
{
    [SerializeField] private int doorKillsThreshold = 1;
    [SerializeField] private EventReference doorLoop;
    [SerializeField] private EventReference doorStart;
    
    //Privates
    private EventInstance doorLoopInstance;
    private int m_doorKillsCollected;
    private float m_doorOpenTime = 1.3f;
    
    private void Awake()
    {
        doorLoopInstance = RuntimeManager.CreateInstance(doorLoop);
        RuntimeManager.AttachInstanceToGameObject(doorLoopInstance, gameObject);
    }
    
    
    public void TryOpenDoor()
    {
        m_doorKillsCollected++;

        if (m_doorKillsCollected >= doorKillsThreshold)
        {
            OpenDoor();
        }
    }

    private void OpenDoor()
    {
        StartCoroutine(OpenDoorWithAudio());
    }

    IEnumerator OpenDoorWithAudio()
    {
        RuntimeManager.PlayOneShotAttached(doorStart, gameObject);
        doorLoopInstance.start();
        yield return new WaitForSeconds(0.5f);
        float endValue = transform.position.y - 4.5f;
        transform.DOMoveY(endValue, m_doorOpenTime);

        yield return new WaitForSeconds(m_doorOpenTime);
        doorLoopInstance.stop(STOP_MODE.ALLOWFADEOUT);
        RuntimeManager.PlayOneShotAttached(doorStart, gameObject);

    }
}
