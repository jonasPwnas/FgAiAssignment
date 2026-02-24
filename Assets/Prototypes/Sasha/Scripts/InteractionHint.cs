using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InteractionHint : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private CanvasGroup m_canvasGroup;
    [SerializeField] private RectTransform m_hintContainer;
    [SerializeField] private Image m_progressBar;
    
    [Header("Key Setup")]
    [SerializeField] private Image m_keyImage;
    [SerializeField] private Sprite m_keyNormalSprite;
    [SerializeField] private Sprite m_keyPressedSprite;
    [SerializeField] private float m_keyPressScaleAdjustment = 1.0f;
    [SerializeField] private InputAction m_inputAction;
    
    [Header("Settings")]
    [SerializeField] private float m_fillTime = 2.0f;
    [SerializeField] private float m_floatSpeed = 2.0f;
    [SerializeField] private float m_floatAmplitude = 5.0f;

    // Private member variables
    private bool m_isHolding;
    private float m_progress;
    private float m_floatTimer;
    private Vector2 m_containerInitialPos;
    private System.Action m_onComplete;
    private bool m_isVisible = true;

    private void Awake()
    {
        if (m_hintContainer) m_containerInitialPos = m_hintContainer.anchoredPosition;
    }

    private void Update()
    {
        HandleInput();
        UpdateVisibility();
        UpdateAnimations();
        UpdateProgress();
    }

    private void OnEnable()
    {
        m_inputAction.Enable();
    }

    private void OnDisable()
    {
        m_inputAction.Disable();
    }

    private void HandleInput()
    {
        m_isHolding = m_inputAction.IsPressed();
        // Use GetKey for continuous checking
        //m_isHolding = Input.GetKey(KeyCode.F); There was an old control system for pressing F
    }

    private void UpdateVisibility()
    {
        if (m_canvasGroup)
        {
            m_canvasGroup.alpha = Mathf.MoveTowards(m_canvasGroup.alpha, 1f, Time.deltaTime * 5f);
        }
    }

    private void UpdateAnimations()
    {
        HandleFloatAnimation();
        HandleKeySprite();
    }

    private void HandleFloatAnimation()
    {
        if (m_hintContainer)
        {
            m_floatTimer += Time.deltaTime * m_floatSpeed;
            float yOffset = Mathf.Sin(m_floatTimer) * m_floatAmplitude;
            m_hintContainer.anchoredPosition = m_containerInitialPos + new Vector2(0, yOffset);
        }
    }

    private void HandleKeySprite()
    {
        if (m_keyImage)
        {
            // Swap sprite based on holding state
            Sprite targetSprite = (m_isHolding && m_keyPressedSprite != null) ? m_keyPressedSprite : m_keyNormalSprite;
            if (targetSprite != null) m_keyImage.sprite = targetSprite;

            // Apply scale adjustment when holding to compensate for sprite size differences
            float scale = m_isHolding ? m_keyPressScaleAdjustment : 1.0f;
            m_keyImage.rectTransform.localScale = Vector3.one * scale;
        }
    }

    private void UpdateProgress()
    {
        if (m_isHolding)
        {
            if (m_progress < 1f)
            {
                m_progress += Time.deltaTime / m_fillTime;
                
                if (m_progress >= 1f)
                {
                    m_progress = 1f;
                    OnInteractionComplete();
                }
            }
        }
        else
        {
            // Instant reset when key is released
            m_progress = 0f;
        }

        if (m_progressBar)
        {
            m_progressBar.fillAmount = m_progress;
        }
    }

    private void OnInteractionComplete()
    {
        m_onComplete?.Invoke();
    }

    public void SetCallback(System.Action callback)
    {
        m_onComplete = callback;
    }
}
