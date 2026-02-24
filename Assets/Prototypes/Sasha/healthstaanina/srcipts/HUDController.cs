using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HUDController : MonoBehaviour
{
    [Header("Bars")]
    [SerializeField] private Image m_healthFill;
    [SerializeField] private Image m_staminaFill;
    [SerializeField] private RectTransform m_staminaContainer; 
    [SerializeField] private Image m_staminaFrame; 

    [Header("Stats")]
    [SerializeField] private float m_maxHealth = 100f;
    [SerializeField] private float m_maxStamina = 100f;
    
    [Header("Costs & Regen")]
    [SerializeField] private float m_staminaRegenRate = 15f;
    [SerializeField] private float m_staminaRegenDelay = 1.0f;

    [Header("Animation")]
    [SerializeField] private float m_fillSpeed = 5f; 

    [Header("Disturbed State")]
    [SerializeField] private float m_shakeAmount = 5f;
    [SerializeField] private float m_shakeSpeed = 50f;
    [SerializeField] private Sprite m_alertSprite; 
    [SerializeField] private Color m_normalFrameColor = Color.white;
    [SerializeField] private Color m_alertFrameColor = Color.red;
    [SerializeField] private float m_pulseSpeed = 10f;

    private float m_currentHealth;
    private float m_currentStamina;
    private Vector3 m_initialStaminaPos;
    private bool m_isDisturbed;
    private float m_lastStaminaUseTime;
    
    private Sprite m_normalSprite; 

    private void Start()
    {
        m_currentStamina = m_maxStamina;

        if (m_staminaContainer)
            m_initialStaminaPos = m_staminaContainer.anchoredPosition;

        if (m_staminaFrame)
        {
            m_normalFrameColor = m_staminaFrame.color;
            m_normalSprite = m_staminaFrame.sprite;
        }

        if (m_healthFill) m_healthFill.fillAmount = 1f;
        if (m_staminaFill) m_staminaFill.fillAmount = 1f;
    }

    private void UpdateDisturbedState()
    {
        bool shouldDisturb = (m_currentStamina <= 0.01f); 

        if (shouldDisturb)
        {
            if (!m_isDisturbed)
            {
                m_isDisturbed = true;
                if (m_alertSprite && m_staminaFrame) m_staminaFrame.sprite = m_alertSprite;
            }

            if (m_staminaContainer)
            {
                float offsetX = Mathf.Sin(Time.time * m_shakeSpeed) * m_shakeAmount;
                m_staminaContainer.anchoredPosition = m_initialStaminaPos + new Vector3(offsetX, 0, 0);
            }

            if (m_staminaFrame)
            {
                float t = Mathf.PingPong(Time.time * m_pulseSpeed, 1f);
                m_staminaFrame.color = Color.Lerp(m_normalFrameColor, m_alertFrameColor, t);
            }
        }
        else
        {
            if (m_isDisturbed)
            {
                m_isDisturbed = false;
                
                if (m_staminaContainer) m_staminaContainer.anchoredPosition = m_initialStaminaPos;
                
                if (m_staminaFrame)
                {
                    m_staminaFrame.color = m_normalFrameColor;
                    if (m_normalSprite) m_staminaFrame.sprite = m_normalSprite;
                }
            }
        }
    }

    private void Update()
    {
        UpdateDisturbedState();
    }

    private void HandleInput()
    {
      //  if (Input.GetKeyDown(KeyCode.Space))
      //  {
      //      m_currentHealth -= m_damageAmount;
      //      if (m_currentHealth < 0) m_currentHealth = 0;
      //  }

      //  if (Input.GetKeyDown(KeyCode.T))
      //  {
      //      if (m_currentStamina >= m_staminaCost)
       //     {
       //         m_currentStamina -= m_staminaCost;
       //         m_lastStaminaUseTime = Time.time;
       //     }
       //     else
        //    {
        //        m_currentStamina = 0; 
        //        m_lastStaminaUseTime = Time.time;
        //    }
       // }
    }

    private void HandleRegen()
    {
        if (Time.time > m_lastStaminaUseTime + m_staminaRegenDelay)
        {
            if (m_currentStamina < m_maxStamina)
            {
                m_currentStamina += m_staminaRegenRate * Time.deltaTime;
                if (m_currentStamina > m_maxStamina) m_currentStamina = m_maxStamina;
            }
        }
    }

    private void OnEnable()
    {
        PlayerStatus.OnHealthUpdate += HealthUpdate;
        PlayerStatus.OnStaminaUpdate += StaminaUpdate;
    }

    private void OnDisable()
    {
        PlayerStatus.OnHealthUpdate -= HealthUpdate;
        PlayerStatus.OnStaminaUpdate -= StaminaUpdate;
    }

    private void HealthUpdate(float currentHealth, ElementTypes.ElementType element)
    {
        m_currentHealth = currentHealth;
        m_healthFill.fillAmount = currentHealth;
    }

    private void StaminaUpdate(float currentStamina)
    {
        m_currentStamina = currentStamina;
        m_staminaFill.fillAmount = currentStamina;
    }

    /*private void UpdateVisuals(currentpercentage)
    {
        if (m_healthFill)
        {
            float targetFill = m_currentHealth / m_maxHealth;
            m_healthFill.fillAmount = Mathf.Lerp(m_healthFill.fillAmount, targetFill, Time.deltaTime * m_fillSpeed);
        }

        if (m_staminaFill)
        {
            float targetFill = m_currentStamina / m_maxStamina;
            m_staminaFill.fillAmount = Mathf.Lerp(m_staminaFill.fillAmount, targetFill, Time.deltaTime * m_fillSpeed);
        }
    }*/
}
