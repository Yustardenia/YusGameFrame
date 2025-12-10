using UnityEngine;
using Fungus;

[CommandInfo("MyDLC", 
    "Dialogue Trigger Condition", 
    "判断对话键是否可触发 (基于 DialogueKeyManager)")]
[AddComponentMenu("")]
public class DialogueTriggerCondition : Condition
{
    [Tooltip("Excel 中的对话 ID")]
    [SerializeField] protected int dialogueId;

    protected override bool EvaluateCondition()
    {
        // 直接调用 Manager (它会自动处理加载)
        // 注意：如果 Manager 还没 Awake，这里 Instance 会自动创建并加载
        var dlg = DialogueKeyManager.Instance.GetDialogue(dialogueId);
        
        // 只有找到数据且 canTrigger 为 true 才返回 true
        return (dlg != null) && dlg.canTrigger;
    }

    protected override bool HasNeededProperties()
    {
        return dialogueId != 0;
    }

    public override string GetSummary()
    {
        return dialogueId == 0 ? "Error: No ID" : $"Check ID {dialogueId} CanTrigger?";
    }
}