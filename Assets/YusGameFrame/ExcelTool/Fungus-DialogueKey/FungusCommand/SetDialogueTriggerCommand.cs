using UnityEngine;
using Fungus;

[CommandInfo("MyDLC", "Set Dialogue Trigger", "设置对话键是否可触发 (会自动保存)")]
public class SetDialogueTriggerCommand : Command
{
    [Tooltip("Excel 中的对话 ID")]
    [SerializeField] protected int dialogueId;

    [Tooltip("设置为 True 或 False")]
    [SerializeField] protected bool canTrigger;

    public override void OnEnter()
    {
        bool success = DialogueKeyManager.Instance.SetCanTrigger(dialogueId, canTrigger);

        if (!success)
        {
            Debug.LogWarning($"[Fungus] 设置状态失败，找不到 ID: {dialogueId}");
        }

        Continue();
    }

    public override string GetSummary()
    {
        return dialogueId == 0 ? "Error: No ID" : $"Set ID {dialogueId} = {canTrigger}";
    }
}