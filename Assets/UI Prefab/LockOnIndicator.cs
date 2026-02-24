using Player;
using UnityEngine;
using UnityEngine.UI;

public class LockOnIndicator : MonoBehaviour
{
    [SerializeField] private Image fireIndicator;
    [SerializeField] private Image airIndicator;
    [SerializeField] private Image waterIndicator;
    
    private Camera m_camera;
    private IndicatorPositioner m_positioner;
    
    private void Awake()
    {
        m_camera = Camera.main;
        m_positioner = GetComponentInChildren<IndicatorPositioner>();
        m_positioner.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        PlayerController.OnLockOnModeUpdate += ToggleIndicator;
    }

    private void OnDisable()
    {
        PlayerController.OnLockOnModeUpdate -= ToggleIndicator;
    }

    private void ToggleIndicator(bool enable, Transform target, ElementTypes.ElementType elementWeakness) //pling
    {
        if (enable)
        {
            if (elementWeakness == ElementTypes.ElementType.NoElement)
            {
                fireIndicator.gameObject.SetActive(false);
                airIndicator.gameObject.SetActive(false);
                waterIndicator.gameObject.SetActive(false);
            }
            else
            {
                switch (elementWeakness)
                {
                    case ElementTypes.ElementType.Fire:
                        fireIndicator.gameObject.SetActive(true);
                        airIndicator.gameObject.SetActive(false);
                        waterIndicator.gameObject.SetActive(false);
                        break;
                    case ElementTypes.ElementType.Air:
                        airIndicator.gameObject.SetActive(true);
                        fireIndicator.gameObject.SetActive(false);
                        waterIndicator.gameObject.SetActive(false);
                        break;
                    case ElementTypes.ElementType.Water:
                        waterIndicator.gameObject.SetActive(true);
                        fireIndicator.gameObject.SetActive(false);
                        airIndicator.gameObject.SetActive(false);
                        break;
                }
            }
            
            Transform offset = target.gameObject.GetComponent<ILockOnAble>().GetLockOnTarget();
            m_positioner.EnableAndSetTarget(offset, m_camera);
        }
        else
        {
            m_positioner.gameObject.SetActive(false);
        }
    }
}