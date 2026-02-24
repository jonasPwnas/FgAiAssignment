using System;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

public class WorldAmbAudioPlayer : MonoBehaviour
{
    //Editor exposed
    [SerializeField] private EventReference WorldAmbLoop;

    //Privates
    private EventInstance worldAmbInstance;

    private void Awake()
    {
        worldAmbInstance = RuntimeManager.CreateInstance(WorldAmbLoop);
        RuntimeManager.AttachInstanceToGameObject(worldAmbInstance, gameObject);
    }

    private void Start()
    {
        worldAmbInstance.start();
    }

    public void StartWorldAmbiance()
    {
        worldAmbInstance.start();
    }
    
    public void StopWorldAmbiance()
    {
        worldAmbInstance.stop(STOP_MODE.ALLOWFADEOUT);
    }
}
