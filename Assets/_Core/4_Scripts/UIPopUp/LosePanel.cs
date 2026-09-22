using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SE001.System.Management;

public class LosePanel : Panel<LosePanel>
{
    [SerializeField] private Button retryButton;
    [SerializeField] private TMP_Text resultLabel;
    private bool restartRequested;

    private void OnEnable()
    {
        restartRequested = false;
        if (retryButton != null)
            retryButton.interactable = true;
    }

    public void RestartLevel()
    {
        if (retryButton != null)
            retryButton.interactable = false;
        restartRequested = true;
    }

    public void SetResultData(string levelId, LoseReason reason)
    {
        if (resultLabel != null)
            resultLabel.text = levelId + "\n" + reason;
    }

    private void Update()
    {
        if (!restartRequested)
            return;

        restartRequested = false;
        if (LevelManager.Instance != null && LevelManager.Instance.IsReady)
            LevelManager.Instance.ReloadCurrentLevel();
    }
}
