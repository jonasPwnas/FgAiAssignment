using System;
using System.Collections;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

public class PlayerElementCharger : MonoBehaviour
{
    //delegates blooppppp
    public delegate void FinishedCharging();
    public static event FinishedCharging OnFinishedCharging;

    //editor exposed
    [SerializeField] private PlayerStatus playerStatus;
    [SerializeField] private ParticleSystem airChargeVFX;
    [SerializeField] private ParticleSystem fireChargeVFX;
    [SerializeField] private ParticleSystem waterChargeVFX;
    [SerializeField] private EventReference FireChargeSfx;
    [SerializeField] private EventReference WaterChargeSfx;
    [SerializeField] private EventReference AirChargeSfx;
    
    private SourceVFXManager m_targetSourceLogic;
    private IncreaseEmissionIntensity m_eyeLightIntensity;
    private IDamageableElementalSource m_currentSourceElement;
    private SelectionWheel m_selectionWheel;
    private EventInstance m_fireChargeSfx;
    private EventInstance m_waterChargeSfx;
    private EventInstance m_airChargeSfx;

    private bool m_chargingAir;
    
    //private
    private Collider m_collider;
    private IDamageableElementalSource m_source;
    
    private void Awake()
    {
        m_collider = GetComponent<Collider>();
        gameObject.SetActive(false);
        m_selectionWheel = FindAnyObjectByType<SelectionWheel>();
        m_fireChargeSfx = RuntimeManager.CreateInstance(FireChargeSfx);
        m_waterChargeSfx = RuntimeManager.CreateInstance(WaterChargeSfx);
        m_airChargeSfx = RuntimeManager.CreateInstance(AirChargeSfx);
    }

    public void EnableCharger(bool enable)
    {
        if (enable)
        {
            gameObject.SetActive(enable);
            StartCoroutine(AirChargeWait(0.4f));
        }
        else
        {
            if (!m_chargingAir)
            {
                switch (m_currentSourceElement.GetSourceElement())
                {
                    case ElementTypes.ElementType.Fire:
                        m_targetSourceLogic.DecreaseSize();
                        fireChargeVFX.Stop();
                        m_eyeLightIntensity.increase = false;
                        break;
                    case ElementTypes.ElementType.Water:
                        waterChargeVFX.Stop();
                        m_eyeLightIntensity.increase = false;
                        m_targetSourceLogic.waterParticleSpinIncrease = false;
                        break;
                }
            }

            airChargeVFX.Stop();

            StopAllCoroutines();

            gameObject.SetActive(enable);
        }
    }

    private void OnDisable()
    {
        m_fireChargeSfx.stop(STOP_MODE.ALLOWFADEOUT);
        m_waterChargeSfx.stop(STOP_MODE.ALLOWFADEOUT);
        m_airChargeSfx.stop(STOP_MODE.ALLOWFADEOUT);
        m_collider.enabled = true;
        m_source = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<IDamageableElementalSource>() != null)
        {
            m_source = other.gameObject.GetComponent<IDamageableElementalSource>();

            m_currentSourceElement = other.gameObject.GetComponent<IDamageableElementalSource>();

            m_targetSourceLogic = other.gameObject.GetComponent<SourceVFXManager>();

            m_eyeLightIntensity = other.gameObject.GetComponent<IncreaseEmissionIntensity>();

            m_chargingAir = false;

            switch (m_source.GetSourceElement())
            {
                case ElementTypes.ElementType.Fire:

                    m_targetSourceLogic.IncreaseSize();

                    m_eyeLightIntensity.active = true;
                    m_eyeLightIntensity.increase = true;

                    StartCoroutine(StartFireVFX());

                    StartCoroutine(ChargeFireOrWater(m_source));

                    break;
                case ElementTypes.ElementType.Water:

                    m_eyeLightIntensity.active = true;
                    m_eyeLightIntensity.increase = true;

                    if(!waterChargeVFX.isPlaying)
                        waterChargeVFX.Play();

                    m_targetSourceLogic.waterSpinActive = true;
                    m_targetSourceLogic.waterParticleSpinIncrease = true;

                    StartCoroutine(ChargeFireOrWater(m_source));

                    break;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        StopAllCoroutines();
    }

    IEnumerator AirChargeWait(float airWaitTime)
    {
        yield return new WaitForSeconds(airWaitTime);
        if (m_source == null)
        {
            airChargeVFX.Play();

            m_chargingAir = true;
            m_collider.enabled = false;
            StartCoroutine(ChargeAir(6f));
        }
    }

    IEnumerator ChargeAir(float chargeDuration)
    {
        m_airChargeSfx.start();
        m_selectionWheel.SwitchToChargingElement(ElementTypes.ElementType.Air);
        yield return new WaitForSeconds(chargeDuration * 0.333f);
        playerStatus.AddElement(ElementTypes.ElementType.Air, 1, true);

        yield return new WaitForSeconds(chargeDuration * 0.333f);
        playerStatus.AddElement(ElementTypes.ElementType.Air, 1, true);

        yield return new WaitForSeconds(chargeDuration * 0.333f);
        playerStatus.AddElement(ElementTypes.ElementType.Air, 1, true);

        OnFinishedCharging?.Invoke();
        m_airChargeSfx.stop(STOP_MODE.ALLOWFADEOUT);
        gameObject.SetActive(false);
    }

    IEnumerator ChargeFireOrWater(IDamageableElementalSource source)
    {
        if (source.GetSourceElement() == ElementTypes.ElementType.Fire)
        {
            m_fireChargeSfx.start();
        }
        else
        {
            m_waterChargeSfx.start();
        }
        m_selectionWheel.SwitchToChargingElement(source.GetSourceElement());
        yield return new WaitForSeconds(source.ChargeElementTime() * 0.333f);
        playerStatus.AddElement(source.GetSourceElement(), 1, true);

        yield return new WaitForSeconds(source.ChargeElementTime() * 0.333f);
        playerStatus.AddElement(source.GetSourceElement(), 1, true);

        yield return new WaitForSeconds(source.ChargeElementTime() * 0.333f);
        playerStatus.AddElement(source.GetSourceElement(), 1, true);

        OnFinishedCharging?.Invoke();
        if (source.GetSourceElement() == ElementTypes.ElementType.Fire)
        {
            m_fireChargeSfx.stop(STOP_MODE.ALLOWFADEOUT);
        }
        else
        {
            m_waterChargeSfx.stop(STOP_MODE.ALLOWFADEOUT);
        }
        gameObject.SetActive(false);
    }

    private IEnumerator StartFireVFX()
    {
        yield return new WaitForSeconds(0.5f);

        fireChargeVFX.Play();
    }
}
