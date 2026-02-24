using System;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    //public
    [HideInInspector] public bool isUsed = true;
    [SerializeField] private bool bossBar = false;

    
    //members
    [SerializeField] private Image healthFillSprite;
    private RectTransform healthBarRectTransform;
    private Camera m_camera;
    private EnemyHealthBarManager m_healthBarManager;
    private Transform m_targetTransform;
    
    
    private void OnEnable()
    {
        m_camera = Camera.main;
        //healthFillSprite = GetComponentInChildren<Image>();
        m_healthBarManager = GetComponentInParent<EnemyHealthBarManager>();
        healthBarRectTransform = GetComponentInParent<RectTransform>();
    }

    public void UpdateHealth(float healthPercentage, Transform trans)
    {
        m_targetTransform = trans;
        healthFillSprite.fillAmount = healthPercentage;
    }

    private void Update()
    {
        if (m_targetTransform == null)
            return;
        
        if (bossBar)
            return;
        
        healthBarRectTransform.position = m_camera.WorldToScreenPoint(m_targetTransform.position);
    }

    public void StopUsingHealthbar()
    {
        gameObject.SetActive(false);
        m_healthBarManager.SendBarToPool(this);
        m_targetTransform = null;
    }
}