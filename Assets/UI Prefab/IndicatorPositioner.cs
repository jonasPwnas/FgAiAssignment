using System;
using UnityEngine;
using UnityEngine.UI;

public class IndicatorPositioner : MonoBehaviour
{
    private Image m_indicator;
    private Transform m_worldEnemyTarget;
    private Camera m_camera;
    
    public void EnableAndSetTarget(Transform target, Camera cam)
    {
        m_worldEnemyTarget = target;
        m_camera = cam;
        m_indicator = GetComponent<Image>();
        gameObject.SetActive(true);
    }
    
    void Update()
    {
        m_indicator.transform.position = m_camera.WorldToScreenPoint(m_worldEnemyTarget.position);
    }
}