using Player;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathMenu : MonoBehaviour
{
    public GameObject DeathMenuPanel;
    SceneLoader sceneLoader;
    PauseManager pauseManager;

    private void Awake()
    {
        sceneLoader = FindAnyObjectByType<SceneLoader>();
        pauseManager = FindAnyObjectByType<PauseManager>();
    }

    private void Start()
    {
        DeathMenuPanel.SetActive(false);
    }
    private void OnEnable()
    {
        PlayerStatus.OnPlayerDied += OpenDeathMenu;
    }
    private void OnDisable()
    {
        PlayerStatus.OnPlayerDied -= OpenDeathMenu;
    }
    private void OpenDeathMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        DeathMenuPanel.SetActive(true);
    }
}
