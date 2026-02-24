using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject m_optionsPanel;
    [SerializeField] private GameObject m_pauseRoot;
    [SerializeField] private Button m_resumeButton;
    [SerializeField] private Button m_loadButton;
    [SerializeField] private Button m_settingsButton;
    [SerializeField] private Button m_quitButton;

    [Header("Visual Settings")]
    [SerializeField] private float m_animSpeed = 10f; // Speed of selection lerp
    [SerializeField] private float m_selectedScale = 1.1f;
    [SerializeField] private Color m_selectedColor = Color.white;
    [SerializeField] private Color m_normalColor = new Color(0.6f, 0.6f, 0.6f);
    
    [Header("Quit Button Colors")]
    [SerializeField] private Color m_quitSelectedColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private Color m_quitNormalColor = new Color(0.7f, 0.2f, 0.2f);

    [Header("Window Animation")]
    [SerializeField] private float m_windowAnimDuration = 0.3f;


    private int m_selectedIndex = 0;
    private List<Button> m_buttons = new List<Button>();
    
    // Animation targets
    private class ButtonAnimState
    {
        public float currentScale = 1f;
        public float currentIconAlpha = 0f;
        public Color currentColor;
        public RectTransform textRect;
        public Image iconImage;
        public Text textComponent;
    }
    private Dictionary<Button, ButtonAnimState> m_animStates = new Dictionary<Button, ButtonAnimState>();

    private CanvasGroup m_canvasGroup;
    private Transform m_modal;
    private Coroutine m_windowRoutine;
    PauseManager pauseManager;
    SceneLoader sceneLoader;

    void Awake()
    {
        pauseManager = FindAnyObjectByType<PauseManager>();
        sceneLoader = FindAnyObjectByType<SceneLoader>();
    }

    private void Start()
    {
        if (EventSystem.current == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        if (m_pauseRoot)
        {
            m_canvasGroup = m_pauseRoot.GetComponent<CanvasGroup>();
            if (m_canvasGroup == null) m_canvasGroup = m_pauseRoot.AddComponent<CanvasGroup>();
            m_canvasGroup.alpha = 0f;
            
            if (m_pauseRoot.transform.childCount > 0)
            {
                Transform overlay = m_pauseRoot.transform.Find("Overlay");
                if (overlay) m_modal = overlay.Find("ModalWindow");
            }
        }

        m_buttons.Clear();
        AddButton(m_resumeButton);
        AddButton(m_loadButton);
        AddButton(m_settingsButton);
        AddButton(m_quitButton);

        if (m_resumeButton) m_resumeButton.onClick.AddListener(ResumeGame);
        if (m_loadButton) m_loadButton.onClick.AddListener(LoadCheckpoint);
        if (m_settingsButton) m_settingsButton.onClick.AddListener(OpenSettings);
        if (m_quitButton) m_quitButton.onClick.AddListener(QuitGame);
    }
    
    private void AddButton(Button btn)
    {
        if (btn == null) return;
        m_buttons.Add(btn);
        
        // Init anim state
        ButtonAnimState state = new ButtonAnimState();
        state.textComponent = btn.GetComponentInChildren<Text>();
        if (state.textComponent) state.textRect = state.textComponent.rectTransform;
        
        Transform iconTr = btn.transform.Find("Icon");
        if (iconTr) state.iconImage = iconTr.GetComponent<Image>();
        
        // Set initial color immediately
        state.currentColor = (btn == m_quitButton) ? m_quitNormalColor : m_normalColor;
        
        m_animStates[btn] = state;

        btn.onClick.AddListener(() => OnButtonClicked(btn));

        EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
        if (!trigger) trigger = btn.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerEnter;
        entry.callback.AddListener((data) => { OnPointerEnter(btn); });
        trigger.triggers.Add(entry);
    }
    
    private void Update()
    {
        UpdateAnimations();
    }

    private void ChangeSelection(int direction)
    {
        m_selectedIndex += direction;
        if (m_selectedIndex < 0) m_selectedIndex = m_buttons.Count - 1;
        else if (m_selectedIndex >= m_buttons.Count) m_selectedIndex = 0;
    }

    private void OnPointerEnter(Button btn)
    {
        int index = m_buttons.IndexOf(btn);
        if (index != -1 && index != m_selectedIndex)
        {
            m_selectedIndex = index;
        }
    }
    
    private void OnButtonClicked(Button btn) { }

    private void UpdateAnimations()
    {
        if (!pauseManager.isPaused && m_pauseRoot && !m_pauseRoot.activeInHierarchy) return;

        float dt = Time.unscaledDeltaTime;

        for (int i = 0; i < m_buttons.Count; i++)
        {
            Button btn = m_buttons[i];
            if (!m_animStates.ContainsKey(btn)) continue;

            ButtonAnimState state = m_animStates[btn];
            bool isSelected = (i == m_selectedIndex);

            // Targets
            float targetScale = isSelected ? m_selectedScale : 1f;
            float targetAlpha = isSelected ? 1f : 0f;
            Color targetColor;
            
            if (btn == m_quitButton) targetColor = isSelected ? m_quitSelectedColor : m_quitNormalColor;
            else targetColor = isSelected ? m_selectedColor : m_normalColor;

            // Lerp Scale
            state.currentScale = Mathf.Lerp(state.currentScale, targetScale, dt * m_animSpeed);
            if (state.textRect) state.textRect.localScale = Vector3.one * state.currentScale;

            // Lerp Color
            state.currentColor = Color.Lerp(state.currentColor, targetColor, dt * m_animSpeed);
            if (state.textComponent) state.textComponent.color = state.currentColor;

            // Lerp Icon Alpha
            state.currentIconAlpha = Mathf.Lerp(state.currentIconAlpha, targetAlpha, dt * m_animSpeed);
            if (state.iconImage)
            {
                Color c = state.iconImage.color;
                c.a = state.currentIconAlpha;
                state.iconImage.color = c;
            }
        }
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            TogglePauseMenu();
        }
    }

    public void PerformPause()
    {
        TogglePauseMenu();
    }
    
    public void TogglePauseMenu()
    {
        if (pauseManager.isPaused)
        {
            pauseManager.UnPause();
            m_optionsPanel.SetActive(false);
            if (m_windowRoutine != null) StopCoroutine(m_windowRoutine);
            m_windowRoutine = StartCoroutine(AnimateWindow(false));
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            pauseManager.Pause();
            if (m_windowRoutine != null) StopCoroutine(m_windowRoutine);
            m_windowRoutine = StartCoroutine(AnimateWindow(true));
            m_selectedIndex = 0;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    /*public void PauseGame()
    {
        m_isPaused = true;
        Time.timeScale = 0f;
        m_selectedIndex = 0;
        
        if (m_windowRoutine != null) StopCoroutine(m_windowRoutine);
        m_windowRoutine = StartCoroutine(AnimateWindow(true));
    }*/

    public void ResumeGame()
    {
        pauseManager.UnPause();
        if (m_windowRoutine != null) StopCoroutine(m_windowRoutine);
        m_windowRoutine = StartCoroutine(AnimateWindow(false));
    }
    

    // for animation of apeearance of pause menu
    private IEnumerator AnimateWindow(bool show)
    {
        if (m_pauseRoot && !m_pauseRoot.activeSelf && show) m_pauseRoot.SetActive(true);

        float t = 0f;
        float startAlpha = m_canvasGroup ? m_canvasGroup.alpha : (show ? 0 : 1);
        float endAlpha = show ? 1f : 0f;
        
        Vector3 startScale = m_modal ? m_modal.localScale : Vector3.one;
        Vector3 endScale = show ? Vector3.one : Vector3.one * 0.95f;

        while (t < m_windowAnimDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = t / m_windowAnimDuration;
            p = Mathf.Sin(p * Mathf.PI * 0.5f); // Ease Out Sine

            if (m_canvasGroup) m_canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, p);
            if (m_modal) m_modal.localScale = Vector3.Lerp(startScale, endScale, p);

            yield return null;
        }

        if (m_canvasGroup) m_canvasGroup.alpha = endAlpha;
        if (m_modal) m_modal.localScale = endScale;

        if (!show)
        {
            if (m_pauseRoot) m_pauseRoot.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    private void LoadCheckpoint() 
    { 
        Time.timeScale = 1f; 
    }
    private void OpenSettings() 
    { 
        m_optionsPanel.SetActive(true);
    }
    private void QuitGame() 
    { 
        pauseManager.UnPause();
        sceneLoader.LoadPreviousScene();
    }

    public void Back()
    {
        m_optionsPanel.SetActive(false);
    }
}
