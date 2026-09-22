using UnityEngine;
using UnityEngine.UI;
using SE001.System.Management;

public class WinPanel : Panel<WinPanel>
{
    [SerializeField] private Button nextButton;
    private bool nextRequested;

    private void OnEnable()
    {
        nextRequested = false;
        if (nextButton != null)
            nextButton.interactable = LevelManager.Instance != null && LevelManager.Instance.CanBeginNextLevel();
    }

    public void NextLevel()
    {
        if (nextButton != null)
            nextButton.interactable = false;
        nextRequested = true;
    }

    private void Update()
    {
        if (!nextRequested)
            return;

        nextRequested = false;
        if (LevelManager.Instance != null && LevelManager.Instance.IsReady)
            LevelManager.Instance.BeginNextLevel();
    }
}
