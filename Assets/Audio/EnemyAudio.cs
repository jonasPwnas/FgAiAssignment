using System;
using FMOD.Studio;
using FMODUnity;
using Player;
using UnityEngine;

public class EnemyAudio : MonoBehaviour
{
    [SerializeField] private EventReference FireAttackSwing;
    [SerializeField] private EventReference WaterAttackSwing;
    [SerializeField] private EventReference AirAttackSwing;
    [SerializeField] private EventReference AttackSwing;
    [SerializeField] private EventReference FireAttackConnected;
    [SerializeField] private EventReference WaterAttackConnected;
    [SerializeField] private EventReference AirAttackConnected;
    [SerializeField] private EventReference DamagedVoice;
    [SerializeField] private EventReference HeavyAtkVoice;
    [SerializeField] private EventReference LightAtkVoice;
    [SerializeField] private EventReference DeathVoice;
    [SerializeField] private EventReference StunnedSound;
    [SerializeField] private EventReference FootstepsSound;
    [SerializeField] private EventReference ExplosionSfx;
    [SerializeField] private Transform ExplosionLocation;


    //Privates blöööörk
    private EnemyStatus m_enemyStatus;
    private EnemyFsm m_enemyController;

    private void Awake()
    {
        m_enemyStatus = GetComponent<EnemyStatus>();
    }
    
    public void HeavyAttackSfx() //from animation
    {
        RuntimeManager.PlayOneShot(HeavyAtkVoice,transform.position);
        switch (m_enemyStatus.GetSourceElement())
        {
            case ElementTypes.ElementType.Fire:
                RuntimeManager.PlayOneShot(FireAttackSwing, transform.position);
                break;
            case ElementTypes.ElementType.Air:
                RuntimeManager.PlayOneShot(AirAttackSwing, transform.position);
                break;
            case ElementTypes.ElementType.Water:
                RuntimeManager.PlayOneShot(WaterAttackSwing, transform.position);
                break;
        }
    }
    
    public void LightAttackSfx() //from animation
    {
        RuntimeManager.PlayOneShot(LightAtkVoice, transform.position);
        switch (m_enemyStatus.GetSourceElement())
        {
            case ElementTypes.ElementType.Fire:
                RuntimeManager.PlayOneShot(FireAttackSwing, transform.position);
                break;
            case ElementTypes.ElementType.Air:
                RuntimeManager.PlayOneShot(AirAttackSwing, transform.position);
                break;
            case ElementTypes.ElementType.Water:
                RuntimeManager.PlayOneShot(WaterAttackSwing, transform.position);
                break;
        }
    }
    
    public void AttackAoe() //From animation only for boss
    {
        RuntimeManager.PlayOneShot(ExplosionSfx, ExplosionLocation.position);
    }
    

    public void FootstepSfx() //from animation
    {
        RuntimeManager.PlayOneShot(FootstepsSound, transform.position);
    }

    public void DamagedSound(ElementTypes.ElementType element)
    {
        RuntimeManager.PlayOneShot(DamagedVoice, transform.position);
        switch (element)
        {
            case ElementTypes.ElementType.Fire:
                RuntimeManager.PlayOneShot(FireAttackConnected, transform.position);
                break;
            case ElementTypes.ElementType.Air:
                RuntimeManager.PlayOneShot(AirAttackConnected, transform.position);
                break;
            case ElementTypes.ElementType.Water:
                RuntimeManager.PlayOneShot(WaterAttackConnected, transform.position);
                break;
        }
    }
    
    public void PlayStunnedSound(bool stunned)
    {
        if (stunned)
        {
            RuntimeManager.PlayOneShot(StunnedSound, transform.position);
            RuntimeManager.PlayOneShot(DamagedVoice, transform.position);
        }
    }
    
    public void AttackHitSound(ElementTypes.ElementType element)
    {
        
    }

    public void DeathSfx()
    {
        RuntimeManager.PlayOneShot(DeathVoice, transform.position); //cool sounds wow much good
    }
}
