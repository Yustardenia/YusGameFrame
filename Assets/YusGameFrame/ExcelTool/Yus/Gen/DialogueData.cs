using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class DialogueData : IYusBinaryData, IYusCloneable<DialogueData>
{
    public int id;
    public int npcId;
    public bool canTrigger;
    public int triggerCount;
    public string dialogueText;
    public string triggerConditionName;

    public DialogueData Clone() {
        DialogueData copy = new DialogueData();
        copy.id = this.id;
        copy.npcId = this.npcId;
        copy.canTrigger = this.canTrigger;
        copy.triggerCount = this.triggerCount;
        copy.dialogueText = this.dialogueText;
        copy.triggerConditionName = this.triggerConditionName;
        return copy;
    }

    public void Write(BinaryWriter bw) {
        bw.Write(id);
        bw.Write(npcId);
        bw.Write(canTrigger);
        bw.Write(triggerCount);
        bw.Write(dialogueText ?? "");
        bw.Write(triggerConditionName ?? "");
    }

    public void Read(BinaryReader br, int version) {
        id = br.ReadInt32();
        npcId = br.ReadInt32();
        canTrigger = br.ReadBoolean();
        triggerCount = br.ReadInt32();
        dialogueText = br.ReadString();
        triggerConditionName = br.ReadString();
    }
}
