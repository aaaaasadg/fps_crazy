using UnityEngine;

public class PauseMenuButton : MonoBehaviour
{
    public void OpenPauseMenu()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TogglePause();
        }
    }
}

