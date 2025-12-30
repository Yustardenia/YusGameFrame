using UnityEngine;
using UnityEngine.UI;

public class FastForwardToggleUI : MonoBehaviour
{
    public KeyCode toggleKey = KeyCode.V;
    public Button toggleButton;
    public bool enableKeyboardToggle = true;

    private void Awake()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(Toggle);
        }
    }

    private void Update()
    {
        if (!enableKeyboardToggle) return;

        if (Input.GetKeyDown(toggleKey))
        {
            Toggle();
        }
    }

    private void Toggle()
    {
        FastForwardManager.Instance?.ToggleFastForward();
    }
}

