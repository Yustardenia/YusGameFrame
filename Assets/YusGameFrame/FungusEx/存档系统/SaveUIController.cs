using UnityEngine;

public class SaveUIController : MonoBehaviour
{
    public SaveSlotUIManager uiManager; // Inspector ??
    public KeyCode toggleKey = KeyCode.F5;
    public KeyCode quickSaveKey = KeyCode.F6;

    private void Update()
    {
        if (uiManager == null) return;

        if (Input.GetKeyDown(toggleKey))
        {
            uiManager.TogglePanel();
        }

        if (Input.GetKeyDown(quickSaveKey))
        {
            // ???????????????????????
            uiManager.OnPanelSaveClicked();
        }
    }
}