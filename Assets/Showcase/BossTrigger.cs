using System;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

public class BossTrigger : MonoBehaviour
{
    //Editor exposed
    [SerializeField] private EventReference musicLoop;
    [SerializeField] private GameObject bossHealth;
    
    //Privates
    private EventInstance ambianceLoopInstance;
    
    private void Awake()
    {
        ambianceLoopInstance = RuntimeManager.CreateInstance(musicLoop);
    }

    private void OnTriggerEnter(Collider other)
    {
        ambianceLoopInstance.start();
        bossHealth.SetActive(true);
        GetComponent<Collider>().enabled = false;
    }

    private void OnDestroy()
    {
        ambianceLoopInstance.stop(STOP_MODE.IMMEDIATE);
    }
}
