using System.Reflection;
using Fungus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlotUIManager : MonoBehaviour
{
    [Header("Prefab & Container")]
    public SaveSlot saveSlotPrefab;
    public Transform contentPanel;
    public int maxSlots = 6;

    [Header("Panel Controls")]
    public Button btnPanelSave;
    public Button btnPanelRefresh;
    public Button btnPanelClose;
    public GameObject panelRoot;

    [Header("Optional")]
    public TextMeshProUGUI hintText;

    [Header("Pause Settings")]
    public bool pauseOnOpen = true;
    public bool blockFungusInput = true;

    private DialogInput dialogInput;
    private ClickMode originalClickMode = ClickMode.ClickAnywhere;
    private FieldInfo clickModeField;
    private float previousTimeScale = 1f;

    private int selectedSlot = 1;
    private SaveSlot[] instantiatedSlots;

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        clickModeField = typeof(DialogInput).GetField("clickMode", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    private void Start()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.OnSaveComplete += OnAnySaveComplete;

        if (btnPanelSave != null) btnPanelSave.onClick.AddListener(OnPanelSaveClicked);
        if (btnPanelRefresh != null) btnPanelRefresh.onClick.AddListener(RefreshSlots);
        if (btnPanelClose != null) btnPanelClose.onClick.AddListener(() => TogglePanel(false));

        RefreshSlots();
    }

    private void OnDestroy()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.OnSaveComplete -= OnAnySaveComplete;
    }

    private void OnAnySaveComplete(int slot) => RefreshSlots();

    public void RefreshSlots()
    {
        if (contentPanel == null)
        {
            Debug.LogError("[SaveSlotUIManager] contentPanel 未设置");
            return;
        }

        if (saveSlotPrefab == null)
        {
            Debug.LogError("[SaveSlotUIManager] saveSlotPrefab 未设置");
            return;
        }

        foreach (Transform child in contentPanel)
            Destroy(child.gameObject);

        instantiatedSlots = new SaveSlot[maxSlots];

        for (int i = 1; i <= maxSlots; i++)
        {
            SaveSlot slot = Instantiate(saveSlotPrefab, contentPanel);
            slot.slotIndex = i;
            slot.owner = this;
            instantiatedSlots[i - 1] = slot;

            slot.OnSelected += OnSlotSelected;

            SaveData data = SaveManager.Instance != null ? SaveManager.Instance.LoadData(i) : null;
            if (data != null)
            {
                slot.SetData(data);
            }
            else
            {
                SaveData empty = new SaveData
                {
                    saveName = $"空存档 {i}",
                    saveTimeISO = "",
                    thumbnailBase64 = "",
                    flowchartName = "",
                    blockName = "",
                    commandIndex = 0,
                    variables = new SaveData.SerializableVariable[0]
                };
                slot.SetData(empty);
            }
        }

        SetSelectedSlot(selectedSlot);
    }

    private void OnSlotSelected(int slotIndex) => SetSelectedSlot(slotIndex);

    private void SetSelectedSlot(int slotIndex)
    {
        selectedSlot = Mathf.Clamp(slotIndex, 1, maxSlots);
        if (hintText != null) hintText.text = $"选中槽：{selectedSlot}";

        if (instantiatedSlots == null) return;
        for (int i = 0; i < instantiatedSlots.Length; i++)
        {
            if (instantiatedSlots[i] != null)
                instantiatedSlots[i].SetSelected(i + 1 == selectedSlot);
        }
    }

    public void OnPanelSaveClicked()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("[SaveSlotUIManager] SaveManager 未找到");
            return;
        }

        if (SaveManager.Instance.Save(selectedSlot))
        {
            RefreshSlots();
            TogglePanel(true);
        }
    }

    public void TogglePanel(bool? force = null)
    {
        if (panelRoot == null) return;

        bool newState = force ?? !panelRoot.activeSelf;
        panelRoot.SetActive(newState);

        if (newState)
        {
            RefreshSlots();
            OpenPanelEffects();
        }
        else
        {
            ClosePanelEffects();
        }
    }

    private void OpenPanelEffects()
    {
        if (pauseOnOpen)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        if (!blockFungusInput || clickModeField == null) return;

        if (dialogInput == null)
        {
            var sayDialog = SayDialog.GetSayDialog();
            dialogInput = sayDialog != null ? sayDialog.GetComponent<DialogInput>() : null;
        }

        if (dialogInput == null) return;

        object value = clickModeField.GetValue(dialogInput);
        if (value is ClickMode mode) originalClickMode = mode;
        clickModeField.SetValue(dialogInput, ClickMode.Disabled);
    }

    private void ClosePanelEffects()
    {
        if (pauseOnOpen)
            Time.timeScale = previousTimeScale;

        if (!blockFungusInput || dialogInput == null || clickModeField == null) return;
        clickModeField.SetValue(dialogInput, originalClickMode);
    }
}
