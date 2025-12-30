using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SaveSlot : MonoBehaviour, IPointerClickHandler
{
    public RawImage imgThumbnail;
    public TextMeshProUGUI txtTitle;
    public TextMeshProUGUI txtTime;
    public Button btnSaveLoad;
    public Button btnDelete;
    public Image selectionBorder;

    [HideInInspector]
    public int slotIndex;

    [HideInInspector]
    public SaveSlotUIManager owner;

    public Action<int> OnSelected;

    private Texture2D loadedThumbnail;

    private void Awake()
    {
        if (btnSaveLoad != null) btnSaveLoad.onClick.AddListener(OnSaveLoadClicked);
        if (btnDelete != null) btnDelete.onClick.AddListener(OnDeleteClicked);
        SetSelected(false);
    }

    private void OnDestroy()
    {
        ReleaseThumbnail();
    }

    public void SetData(SaveData data)
    {
        if (txtTitle != null)
            txtTitle.text = string.IsNullOrEmpty(data.saveName) ? $"存档 {slotIndex}" : data.saveName;

        if (txtTime != null)
        {
            string timeStr = "-";
            var iso = data.saveTimeISO;
            if (!string.IsNullOrEmpty(iso))
            {
                if (DateTime.TryParse(iso, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                    timeStr = dt.ToString("yyyy-MM-dd HH:mm:ss");
                else
                    timeStr = iso;
            }

            txtTime.text = timeStr;
        }

        if (imgThumbnail != null)
        {
            SetThumbnailFromBase64(data.thumbnailBase64);
        }
    }

    private void SetThumbnailFromBase64(string base64)
    {
        ReleaseThumbnail();

        if (string.IsNullOrEmpty(base64))
        {
            imgThumbnail.texture = null;
            return;
        }

        try
        {
            byte[] bytes = Convert.FromBase64String(base64);
            loadedThumbnail = new Texture2D(2, 2);
            if (loadedThumbnail.LoadImage(bytes))
            {
                imgThumbnail.texture = loadedThumbnail;
            }
            else
            {
                Debug.LogWarning($"[SaveSlot] 缩略图 LoadImage 返回 false (slot {slotIndex})");
                ReleaseThumbnail();
                imgThumbnail.texture = null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[SaveSlot] 缩略图解码失败: " + e.Message);
            ReleaseThumbnail();
            imgThumbnail.texture = null;
        }
    }

    private void ReleaseThumbnail()
    {
        if (loadedThumbnail == null) return;
        Destroy(loadedThumbnail);
        loadedThumbnail = null;
    }

    private void OnSaveLoadClicked()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("[SaveSlot] SaveManager.Instance 为 null");
            return;
        }

        bool hasSave = SaveManager.Instance.HasSave(slotIndex);
        if (hasSave)
        {
            owner?.TogglePanel(false);
            SaveManager.Instance.Load(slotIndex);
        }
        else
        {
            SaveManager.Instance.Save(slotIndex);
        }

        OnSelected?.Invoke(slotIndex);
    }

    private void OnDeleteClicked()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("[SaveSlot] SaveManager.Instance 为 null");
            return;
        }

        SaveManager.Instance.Delete(slotIndex);
        OnSelected?.Invoke(slotIndex);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnSelected?.Invoke(slotIndex);
    }

    public void SetSelected(bool selected)
    {
        if (selectionBorder != null)
            selectionBorder.enabled = selected;
    }
}
