using System.Collections.Generic;
using Characters;
using FMODUnity;
using Options;
using UnityEngine;
using UnityEngine.InputSystem;

public class OpenTutorialWindow : MonoBehaviour
{
    public delegate void OpenTutorial(bool doTutorialThing, OpenTutorialWindow tuttWindow);
    public static event OpenTutorial OnOpenTutorial;

    [Tooltip("OnBoardingWindow")]
    [SerializeField] private GameObject tutorialWindow;
    [SerializeField] private bool isTriggeredWithInput;
    [SerializeField] InputAction openAction;
    [SerializeField] private bool stopEnemiesDuringTutorial = false;
    [SerializeField] private List<FsmRootMotionController> enemies;
    
    
    private bool m_openWindow = false;
    private Collider m_collider;

    private void Awake()
    {
        m_collider = GetComponent<Collider>();
    }

    private void OnEnable()
    {
        openAction.Enable();
        InfoPopupController.OnGiveBackControl += AssumeControl;
    }

    private void OnDisable()
    {
        openAction.Disable();
        InfoPopupController.OnGiveBackControl -= AssumeControl;
    }

    private void OnTriggerEnter(Collider other) //tell UI to show pickup prompt here
    {
        OnOpenTutorial?.Invoke(true, this);
        m_collider.enabled = false;
        RuntimeManager.StudioSystem.setParameterByName("FootstepsVolume", 0f);
        if (stopEnemiesDuringTutorial)
        {
            foreach (FsmRootMotionController enemy in enemies)
            {
                enemy.PauseEnemy(true);
            }
        }
    }

    private void AssumeControl()
    {
        OnOpenTutorial?.Invoke(false, this);
        if (stopEnemiesDuringTutorial)
        {
            foreach (FsmRootMotionController enemy in enemies)
            {
                enemy.PauseEnemy(false);
            }
        }
    }

    public void OpenWindow()
    {
        tutorialWindow.SetActive(true);
    }
}
