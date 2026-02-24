using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Options
{
    public class InfoPopupController : MonoBehaviour
    {
        public delegate void GiveBackControl();
        public static event GiveBackControl OnGiveBackControl;


        [Header("UI Components")]
        [SerializeField] private Image m_progressBar;
        [SerializeField] private CanvasGroup m_canvasGroup;
        
        [Header("Settings")]
        [SerializeField] private float m_fillTime = 1.0f;
        [SerializeField] private InputActionReference m_closeAction;
        [SerializeField] private float m_fadeSpeed = 4.0f; // Speed for fade in/out
        [SerializeField] private PlayerInput playerInput;

        private bool m_isHolding;
        private float m_progress;
        private bool m_isClosing = false;


        private void Awake()
        {
            // Start transparent
            if (m_canvasGroup) m_canvasGroup.alpha = 0f;
        }

        private void OnEnable()
        {
            playerInput.SwitchCurrentActionMap("UI");
            // Reset state - when enabled, become visible
            m_progress = 0f;
            m_isClosing = false;
            m_isHolding = false;
            
            if (m_progressBar) m_progressBar.fillAmount = 0f;
        }

        private void Update()
        {
            UpdateFade();
            
            // If we are in the process of closing, ignore input
            if (m_isClosing) return;

            HandleInput();
            UpdateProgress();
        }

        private void UpdateFade()
        {
            if (!m_canvasGroup) return;

            // Target is 0 if closing, 1 otherwise
            float targetAlpha = m_isClosing ? 0f : 1f;
            
            if (Mathf.Abs(m_canvasGroup.alpha - targetAlpha) > 0.01f)
            {
                m_canvasGroup.alpha = Mathf.MoveTowards(m_canvasGroup.alpha, targetAlpha, Time.deltaTime * m_fadeSpeed);
            }
            else
            {
                m_canvasGroup.alpha = targetAlpha;
                
                // Disable object after fully fading out
                if (m_isClosing && targetAlpha <= 0.01f)
                {
                    gameObject.SetActive(false);
                    playerInput.SwitchCurrentActionMap("Player");
                    OnGiveBackControl?.Invoke();
                }
            }
        }

        private void HandleInput()
        {
            // Check if holding the key
            m_isHolding = m_closeAction.action.IsPressed();
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
                        Close();
                        OnInteractionComplete();
                    }
                }
            }
            else
            {
                // Decay progress if released
                if (m_progress > 0f)
                {
                    m_progress -= Time.deltaTime * 3f; 
                    if (m_progress < 0) m_progress = 0;
                }
            }

            if (m_progressBar)
            {
                m_progressBar.fillAmount = m_progress;
            }
        }

        private void OnInteractionComplete()
        {
            Debug.Log("Popup Closed");
        }

        public void Close()
        {
            m_isClosing = true;
            // The UpdateFade loop will handle the rest
        }

        
        public void Setup(Image progressBar, CanvasGroup cg)
        {
            m_progressBar = progressBar;
            m_canvasGroup = cg;
        }
    }
}
