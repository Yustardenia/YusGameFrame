using UnityEngine;
using System;
using System.IO;

[System.Serializable]
public class BubbleDialogueData : IYusBinaryData, IYusCloneable<BubbleDialogueData>
{
    public int id;
    public string text;
    public string textName;
    public bool isRight;

    public BubbleDialogueData Clone() {
        BubbleDialogueData copy = new BubbleDialogueData();
        copy.id = this.id;
        copy.text = this.text;
        copy.textName = this.textName;
        copy.isRight = this.isRight;
        return copy;
    }

    public void Write(BinaryWriter bw) {
        bw.Write(id);
        bw.Write(text);
        bw.Write(textName);
        bw.Write(isRight);
    }
    public void Read(BinaryReader br, int version) {
        id = br.ReadInt32();
        text = br.ReadString();
        textName = br.ReadString();
        isRight = br.ReadBoolean();
    }
}
