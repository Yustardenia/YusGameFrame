using UnityEngine;
using Fungus;

[CommandInfo("MyDLC", "Increment Dialogue Count", "增加对话键的触发计数 (会自动保存)")]
public class IncrementDialogueCountCommand : Command
{
    [Tooltip("Excel 中的对话 ID")]
    [SerializeField] protected int dialogueId;

    public override void OnEnter()
    {
        bool success = DialogueKeyManager.Instance.IncrementTriggerCount(dialogueId);
        
        if (!success)
        {
            Debug.LogWarning($"[Fungus] 增加计数失败，找不到 ID: {dialogueId}");
        }

        Continue();
    }

    public override string GetSummary()
    {
        return dialogueId == 0 ? "Error: No ID" : $"ID {dialogueId} Count++";
    }
}