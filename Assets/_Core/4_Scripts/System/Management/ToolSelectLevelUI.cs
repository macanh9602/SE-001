using System;
using UnityEngine;

namespace SE001.System.Management
{
    /// <summary>Development UI that selects a level id and delegates loading to LevelManager.</summary>
    [DisallowMultipleComponent]
    public sealed class ToolSelectLevelUI : MonoBehaviour
    {
        [Header("Manager Reference")]
        [SerializeField] private LevelManager _levelManager;

        [Header("UI References")]
        [SerializeField] private UnityEngine.UI.Text _selectLevelText;
        [SerializeField] private UnityEngine.UI.Button _btnMinus;
        [SerializeField] private UnityEngine.UI.Button _btnPlus;
        [SerializeField] private UnityEngine.UI.Button _btnLoadLv;

        [Header("Level Selection")]
        [SerializeField, Min(1)] private int _firstLevelNumber = 1;
        [SerializeField, Min(1)] private int _lastLevelNumber = 1;

        private int selectedLevelNumber;

        private void Awake()
        {
            NormalizeRange();
            selectedLevelNumber = _firstLevelNumber;
            RefreshView();
        }

        private void OnEnable()
        {
            if (_btnMinus != null)
            {
                _btnMinus.onClick.AddListener(SelectPrevious);
            }

            if (_btnPlus != null)
            {
                _btnPlus.onClick.AddListener(SelectNext);
            }

            if (_btnLoadLv != null)
            {
                _btnLoadLv.onClick.AddListener(LoadSelected);
            }

            ResolveLevelManager();
            RefreshView();
        }

        private void OnDisable()
        {
            if (_btnMinus != null)
            {
                _btnMinus.onClick.RemoveListener(SelectPrevious);
            }

            if (_btnPlus != null)
            {
                _btnPlus.onClick.RemoveListener(SelectNext);
            }

            if (_btnLoadLv != null)
            {
                _btnLoadLv.onClick.RemoveListener(LoadSelected);
            }
        }

        private void OnValidate()
        {
            NormalizeRange();
            selectedLevelNumber = Mathf.Clamp(selectedLevelNumber, _firstLevelNumber, _lastLevelNumber);
            RefreshView();
        }

        private void SelectPrevious()
        {
            selectedLevelNumber = Mathf.Max(_firstLevelNumber, selectedLevelNumber - 1);
            RefreshView();
        }

        private void SelectNext()
        {
            selectedLevelNumber = Mathf.Min(_lastLevelNumber, selectedLevelNumber + 1);
            RefreshView();
        }

        private void LoadSelected()
        {
            ResolveLevelManager();
            if (_levelManager == null)
            {
                Debug.LogWarning("[ToolSelectLevelUI] LevelManager is not available.", this);
                return;
            }

            if (!TryBuildLevelId(selectedLevelNumber, out string levelId))
            {
                return;
            }

            _levelManager.BeginLevel(levelId);
        }

        private void ResolveLevelManager()
        {
            if (_levelManager == null)
            {
                _levelManager = LevelManager.Instance;
            }
        }

        private bool TryBuildLevelId(int levelNumber, out string levelId)
        {
            levelId = null;
            ResolveLevelManager();
            if (_levelManager == null)
            {
                Debug.LogError("[ToolSelectLevelUI] LevelManager is not available.", this);
                return false;
            }

            var sequence = _levelManager.GetLevelSequence();
            int index = levelNumber - _firstLevelNumber;
            if (index < 0 || index >= sequence.Count)
            {
                return false;
            }

            levelId = sequence[index].levelId;
            return !string.IsNullOrWhiteSpace(levelId);
        }

        private void NormalizeRange()
        {
            _firstLevelNumber = Mathf.Max(1, _firstLevelNumber);
            ResolveLevelManager();
            if (_levelManager != null && _levelManager.GetLevelSequence().Count > 0)
                _lastLevelNumber = _firstLevelNumber + _levelManager.GetLevelSequence().Count - 1;
            else
                _lastLevelNumber = Mathf.Max(_firstLevelNumber, _lastLevelNumber);
        }

        private void RefreshView()
        {
            if (_selectLevelText != null)
            {
                _selectLevelText.text = selectedLevelNumber.ToString();
            }

            if (_btnMinus != null)
            {
                _btnMinus.interactable = selectedLevelNumber > _firstLevelNumber;
            }

            if (_btnPlus != null)
            {
                _btnPlus.interactable = selectedLevelNumber < _lastLevelNumber;
            }

            if (_btnLoadLv != null)
            {
                _btnLoadLv.interactable = _levelManager != null &&
                    _levelManager.GetLevelSequence().Count > 0;
            }
        }
    }
}
