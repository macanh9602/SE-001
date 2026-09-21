// using Cysharp.Threading.Tasks;
// using UnityEngine;
// using UnityEngine.UI;
// using VTLTools;

// namespace IN006.Systems.Management
// {
//     public class ToolSelectLevelUI : MonoBehaviour
//     {
//         [Header("Manager Reference")]
//         [SerializeField] private LevelManager _levelManager;

//         [Header("UI References")]
//         [SerializeField] private Text _selectLevelText;
//         [SerializeField] private Button _btnMinus;
//         [SerializeField] private Button _btnPlus;
//         [SerializeField] private Button _btnLoadLv;

//         [Header("Thu gọn Tool UI")]
//         [SerializeField] private bool _startCollapsed = true;
//         [SerializeField, Min(24f)] private float _toggleButtonSize = 200f;
//         [SerializeField] private Vector2 _toggleButtonOffset = new Vector2(110f, 110f);
//         [SerializeField, Range(0f, 1f), Tooltip("Độ mờ nền nút toggle. 0 = trong suốt hẳn (vẫn bấm được).")]
//         private float _toggleBgAlpha = 0f;
//         [SerializeField, Range(0f, 1f), Tooltip("Độ mờ chữ '•••' để còn định vị được nút. 0 = ẩn hẳn (trong suốt hoàn toàn).")]
//         private float _toggleLabelAlpha = 0f;

//         private int _selectedIndex = 0;
//         private Button _toggleButton;
//         private InputField _levelInput;
//         private bool _isToolVisible;

//         private void Start()
//         {
//             if (_levelManager == null)
//             {
//                 _levelManager = FindFirstObjectByType<LevelManager>();
//             }

//             if (_levelManager != null)
//             {
//                 _selectedIndex = Mathf.Clamp(
//                     StaticVariables.CurrentLevel - _levelManager.FirstLevelIndex,
//                     0,
//                     _levelManager.LevelCount - 1);
//             }

//             UpdateLevelText();

//             if (_btnMinus != null) _btnMinus.onClick.AddListener(OnMinusClick);
//             if (_btnPlus != null) _btnPlus.onClick.AddListener(OnPlusClick);
//             if (_btnLoadLv != null) _btnLoadLv.onClick.AddListener(OnLoadClick);

//             CreateLevelInput();
//             CreateToggleButton();
//             SetToolVisible(!_startCollapsed);
//             UpdateLevelText();
//         }

//         private void CreateToggleButton()
//         {
//             var toggleObject = new GameObject("ToolToggle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
//             toggleObject.layer = gameObject.layer;
//             toggleObject.transform.SetParent(transform, false);

//             var rect = (RectTransform)toggleObject.transform;
//             rect.anchorMin = Vector2.zero;
//             rect.anchorMax = Vector2.zero;
//             rect.pivot = new Vector2(0.5f, 0.5f);
//             rect.anchoredPosition = _toggleButtonOffset;
//             rect.sizeDelta = Vector2.one * _toggleButtonSize;

//             var image = toggleObject.GetComponent<Image>();
//             image.color = new Color(0.12f, 0.13f, 0.2f, _toggleBgAlpha);
//             image.raycastTarget = true;

//             _toggleButton = toggleObject.GetComponent<Button>();
//             _toggleButton.targetGraphic = image;
//             _toggleButton.navigation = new Navigation { mode = Navigation.Mode.None };
//             _toggleButton.onClick.AddListener(ToggleTool);

//             var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
//             labelObject.layer = gameObject.layer;
//             labelObject.transform.SetParent(toggleObject.transform, false);

//             var labelRect = (RectTransform)labelObject.transform;
//             labelRect.anchorMin = Vector2.zero;
//             labelRect.anchorMax = Vector2.one;
//             labelRect.offsetMin = Vector2.zero;
//             labelRect.offsetMax = Vector2.zero;

//             var label = labelObject.GetComponent<Text>();
//             label.text = "\u2022\u2022\u2022";
//             label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
//             label.fontSize = Mathf.RoundToInt(_toggleButtonSize * 0.42f);
//             label.alignment = TextAnchor.MiddleCenter;
//             label.color = new Color(1f, 1f, 1f, _toggleLabelAlpha);
//             label.raycastTarget = false;
//         }

//         private void CreateLevelInput()
//         {
//             if (_selectLevelText == null) return;

//             Transform parent = _selectLevelText.transform.parent;
//             var sourceRect = (RectTransform)_selectLevelText.transform;

//             var inputObject = new GameObject("LevelInput", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
//             inputObject.layer = gameObject.layer;
//             inputObject.transform.SetParent(parent, false);

//             var rect = (RectTransform)inputObject.transform;
//             rect.anchorMin = sourceRect.anchorMin;
//             rect.anchorMax = sourceRect.anchorMax;
//             rect.pivot = sourceRect.pivot;
//             rect.anchoredPosition = sourceRect.anchoredPosition;
//             rect.sizeDelta = sourceRect.sizeDelta;

//             var background = inputObject.GetComponent<Image>();
//             background.color = new Color(0.12f, 0.13f, 0.2f, 0.85f);

//             var textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
//             textObject.layer = gameObject.layer;
//             textObject.transform.SetParent(inputObject.transform, false);

//             var textRect = (RectTransform)textObject.transform;
//             textRect.anchorMin = Vector2.zero;
//             textRect.anchorMax = Vector2.one;
//             textRect.offsetMin = new Vector2(8f, 4f);
//             textRect.offsetMax = new Vector2(-8f, -4f);

//             var text = textObject.GetComponent<Text>();
//             text.font = _selectLevelText.font != null ? _selectLevelText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
//             text.fontSize = _selectLevelText.fontSize;
//             text.alignment = TextAnchor.MiddleCenter;
//             text.color = Color.white;
//             text.supportRichText = false;

//             _levelInput = inputObject.GetComponent<InputField>();
//             _levelInput.targetGraphic = background;
//             _levelInput.textComponent = text;
//             _levelInput.contentType = InputField.ContentType.IntegerNumber;
//             _levelInput.onEndEdit.AddListener(OnLevelInputEndEdit);

//             _selectLevelText.enabled = false;
//         }

//         private void OnLevelInputEndEdit(string value)
//         {
//             if (!int.TryParse(value, out int levelNumber) || levelNumber < 1)
//             {
//                 UpdateLevelText();
//                 return;
//             }

//             _selectedIndex = levelNumber - 1;
//             UpdateLevelText();
//         }

//         private void ToggleTool() => SetToolVisible(!_isToolVisible);

//         private void SetToolVisible(bool visible)
//         {
//             _isToolVisible = visible;
//             SetControlActive(_selectLevelText != null ? _selectLevelText.transform.parent.gameObject : null, visible);
//             SetControlActive(_btnMinus != null ? _btnMinus.gameObject : null, visible);
//             SetControlActive(_btnPlus != null ? _btnPlus.gameObject : null, visible);
//             SetControlActive(_btnLoadLv != null ? _btnLoadLv.gameObject : null, visible);
//         }

//         private static void SetControlActive(GameObject control, bool active)
//         {
//             if (control != null && control.activeSelf != active)
//                 control.SetActive(active);
//         }

//         private void OnMinusClick()
//         {
//             if (_selectedIndex > 0)
//             {
//                 _selectedIndex--;
//                 UpdateLevelText();
//             }
//         }

//         private void OnPlusClick()
//         {
//             if (_levelManager != null && _selectedIndex < _levelManager.LevelCount - 1)
//             {
//                 _selectedIndex++;
//                 UpdateLevelText();
//             }
//         }

//         private void OnLoadClick()
//         {
//             if (_levelManager != null)
//             {
//                 int levelNumber = _levelManager.FirstLevelIndex + _selectedIndex;
//                 CakeStripReloadAuditLog.Write(
//                     "StripReload.ToolSelect.Request",
//                     $"fromLevel={StaticVariables.CurrentLevel}; targetLevel={levelNumber}; selectedIndex={_selectedIndex}");
//                 StaticVariables.CurrentLevel = levelNumber;
//                 _levelManager.LoadLevelAsync(levelNumber).Forget();
//             }
//             else
//             {
//                 Debug.LogWarning("[ToolSelectLevelUI] Không tìm thấy LevelManager để load!");
//             }
//         }

//         private void UpdateLevelText()
//         {
//             string display = (_selectedIndex + 1).ToString();
//             if (_levelInput != null)
//                 _levelInput.text = display;
//             if (_selectLevelText != null)
//                 _selectLevelText.text = display;
//         }

//         private void OnDestroy()
//         {
//             if (_btnMinus != null) _btnMinus.onClick.RemoveListener(OnMinusClick);
//             if (_btnPlus != null) _btnPlus.onClick.RemoveListener(OnPlusClick);
//             if (_btnLoadLv != null) _btnLoadLv.onClick.RemoveListener(OnLoadClick);
//             if (_toggleButton != null) _toggleButton.onClick.RemoveListener(ToggleTool);
//             if (_levelInput != null) _levelInput.onEndEdit.RemoveListener(OnLevelInputEndEdit);
//         }
//     }
// }
