using System;
using FMOD.Studio;
using FMODUnity;
using Player;
using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [SerializeField] private EventReference BlockedAttack;
    [SerializeField] private EventReference FireAttackSwing;
    [SerializeField] private EventReference WaterAttackSwing;
    [SerializeField] private EventReference AirAttackSwing;
    [SerializeField] private EventReference FireAttackConnected;
    [SerializeField] private EventReference WaterAttackConnected;
    [SerializeField] private EventReference AirAttackConnected;
    [SerializeField] private EventReference FireSpell1;
    [SerializeField] private EventReference FireSpell2;
    [SerializeField] private EventReference WaterSpell;
    [SerializeField] private EventReference AirSpell;
    [SerializeField] private EventReference AttackSwing;
    [SerializeField] private EventReference DamagedVoice;
    [SerializeField] private EventReference DodgeVoice;
    [SerializeField] private EventReference HeavyAtkVoice;
    [SerializeField] private EventReference LightAtkVoice;
    [SerializeField] private EventReference DeathVoice;
    [SerializeField] private EventReference StunnedSound;
    [SerializeField] private EventReference FootstepsSound;

    //Privates
    private PlayerStatus m_playerStatus;
    private PlayerController m_playerController;

    private void Awake()
    {
        m_playerStatus = GetComponent<PlayerStatus>();
    }

    private void OnEnable()
    {
        PlayerController.OnCastSpell += SpellSound;
        PlayerStatus.OnTookDamage += DamagedSound;
        PlayerStatus.OnStaminaBroken += PlayStunnedSound;
        PlayerStatus.OnBlockedDamage += BlockedAttackSfx;
    }

    private void OnDisable()
    {
        PlayerController.OnCastSpell -= SpellSound;
        PlayerStatus.OnTookDamage -= DamagedSound;
        PlayerStatus.OnStaminaBroken -= PlayStunnedSound;
        PlayerStatus.OnBlockedDamage -= BlockedAttackSfx;
    }

    private void BlockedAttackSfx(float damage, ElementTypes.ElementType elementType)
    {
        RuntimeManager.PlayOneShot(BlockedAttack);
    }
    
    public void HeavyAttackSfx() //from animation
    {
        RuntimeManager.PlayOneShot(HeavyAtkVoice);
        switch (m_playerStatus.GetSourceElement())
        {
            case ElementTypes.ElementType.Fire:
                RuntimeManager.PlayOneShot(FireAttackSwing);
                break;
            case ElementTypes.ElementType.Air:
                RuntimeManager.PlayOneShot(AirAttackSwing);
                break;
            case ElementTypes.ElementType.Water:
                RuntimeManager.PlayOneShot(WaterAttackSwing);
                break;
        }
    }

    public void PlayFireSpellSfxOne()
    {
        RuntimeManager.PlayOneShot(FireSpell1);
    }
    public void PlayFireSpellSfxTwo()
    {
        RuntimeManager.PlayOneShot(FireSpell2);
    }
    public void PlayWaterSpellSfx()
    {
        RuntimeManager.PlayOneShot(WaterSpell);
    }
    public void PlayAirSpellSfx()
    {
        RuntimeManager.PlayOneShot(AirSpell);
    }
    
    public void LightAttackSfx() //from animation
    {
        RuntimeManager.PlayOneShot(LightAtkVoice);
        switch (m_playerStatus.GetSourceElement())
        {
            case ElementTypes.ElementType.Fire:
                RuntimeManager.PlayOneShot(FireAttackSwing);
                break;
            case ElementTypes.ElementType.Air:
                RuntimeManager.PlayOneShot(AirAttackSwing);
                break;
            case ElementTypes.ElementType.Water:
                RuntimeManager.PlayOneShot(WaterAttackSwing);
                break;
        }
    }
    
    public void FootstepSfx() //from animation
    {
        RuntimeManager.PlayOneShot(FootstepsSound);
    }

    private void DamagedSound(float damage, ElementTypes.ElementType element)
    {
        switch (element)
        {
            case ElementTypes.ElementType.Fire:
                RuntimeManager.PlayOneShot(FireAttackConnected);
                break;
            case ElementTypes.ElementType.Air:
                RuntimeManager.PlayOneShot(AirAttackConnected);
                break;
            case ElementTypes.ElementType.Water:
                RuntimeManager.PlayOneShot(WaterAttackConnected);
                break;
        }
        RuntimeManager.PlayOneShot(DamagedVoice);
    }
    
    private void PlayStunnedSound(bool stunned)
    {
        if (stunned)
        {
            RuntimeManager.PlayOneShot(StunnedSound);
        }
    }

    private void SpellSound(ElementTypes.ElementType element)
    {
        switch (element)
        {
            case ElementTypes.ElementType.Fire:
                break;
            case ElementTypes.ElementType.Air:
                break;
            case ElementTypes.ElementType.Water:
                break;
        }
    }
}
