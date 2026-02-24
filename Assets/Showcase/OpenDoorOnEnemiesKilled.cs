using System.Collections;
using DG.Tweening;
using UnityEngine;

public class OpenDoorOnEnemiesKilled : MonoBehaviour
{
    [SerializeField] private int doorKillsThreshold = 1;

    private int m_doorKillsCollected;
    private float m_doorOpenTime = 1.3f;
    
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
        yield return new WaitForSeconds(0.5f);
        float endValue = transform.position.y - 4.5f;
        transform.DOMoveY(endValue, m_doorOpenTime);

        yield return new WaitForSeconds(m_doorOpenTime);

    }
}
