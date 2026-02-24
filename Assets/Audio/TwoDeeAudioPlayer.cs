using System;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class TwoDeeAudioPlayer : MonoBehaviour
{
    //Editor exposed
    [SerializeField] private EventReference ambianceLoop;

    //Privates
    private EventInstance ambianceLoopInstance;

    private void Awake()
    {
        ambianceLoopInstance = RuntimeManager.CreateInstance(ambianceLoop);
    }

    private void Start()
    {
        ambianceLoopInstance.start();
    }
}
