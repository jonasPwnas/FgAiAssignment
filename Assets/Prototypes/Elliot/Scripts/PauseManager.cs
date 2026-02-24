using Player;
using UnityEngine;
using UnityEngine.InputSystem;


public class PauseManager : MonoBehaviour
{
    public GameObject Player;
    public bool isPaused { get; private set; }

    private void Awake()
    {
        Player = FindAnyObjectByType<PlayerController>().gameObject;
    }
    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        Player.GetComponentInParent<PlayerInput>().SwitchCurrentActionMap("UI");
    }

    public void UnPause()
    {
        isPaused = false;
        Time.timeScale = 1f;
        Player.GetComponentInParent<PlayerInput>().SwitchCurrentActionMap("Player");
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            UnPause();
        }
        else
        {
            Pause();
        }
    }
}
