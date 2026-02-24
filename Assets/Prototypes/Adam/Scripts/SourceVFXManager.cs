using DG.Tweening;
using System.Collections;
using UnityEngine;

public class SourceVFXManager : MonoBehaviour
{
    [Header("Fire References")]
    [SerializeField] public ParticleSystem whileChargingVFX;
    [SerializeField] public GameObject particleEffectSourceObject;

    [Header("Fire Settings")]
    [SerializeField] private float endValue;
    [SerializeField] private float startValue;
    [SerializeField] private float duration;

    [Header("Water References")]
    [SerializeField] private GameObject waterParticleSourceObject;
    [SerializeField] public bool waterSpinActive;
    [SerializeField] public bool waterParticleSpinIncrease;

    [Header("Water Settings")]
    [SerializeField] private float m_waterSourceRotateMaxSpeed;
    [SerializeField] private float m_waterSourceRotateModifier;
    [SerializeField] private float m_waterSourceRotateSpeed;
    
    private void Update()
    {
        if (waterSpinActive)
            WaterVFXSpinning();
    }

    public void IncreaseSize()
    {
        particleEffectSourceObject.transform.DOScaleY(endValue, duration);
    }

    public void DecreaseSize()
    {
        particleEffectSourceObject.transform.DOScaleY(startValue, duration);
    }

    private void WaterVFXSpinning()
    {
        if (waterParticleSpinIncrease)
        {
            if(m_waterSourceRotateSpeed <= m_waterSourceRotateMaxSpeed)
            {
                waterParticleSourceObject.transform.Rotate(0, m_waterSourceRotateSpeed * Time.deltaTime, 0, Space.Self);
                m_waterSourceRotateSpeed += m_waterSourceRotateModifier * Time.deltaTime;
            }
            else
            {
                waterParticleSourceObject.transform.Rotate(0, m_waterSourceRotateSpeed * Time.deltaTime, 0, Space.Self);
            }
        }
        else
        {
            waterParticleSourceObject.transform.Rotate(0, m_waterSourceRotateSpeed * Time.deltaTime, 0, Space.Self);
            m_waterSourceRotateSpeed -= m_waterSourceRotateModifier * 2 * Time.deltaTime;

            if (m_waterSourceRotateSpeed <= 0)
            {
                waterSpinActive = false;
            }
        }
    }
}
