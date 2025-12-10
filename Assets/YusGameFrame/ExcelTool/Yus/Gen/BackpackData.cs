using UnityEngine;
using System;
using System.IO;

[System.Serializable]
public class BackpackData : IYusBinaryData, IYusCloneable<BackpackData>
{
    public int id;
    public string name;
    public string logicType;
    public string desc;
    public float durability;
    public Sprite icon;

    public BackpackData Clone() {
        BackpackData copy = new BackpackData();
        copy.id = this.id;
        copy.name = this.name;
        copy.logicType = this.logicType;
        copy.desc = this.desc;
        copy.durability = this.durability;
        copy.icon = this.icon;
        return copy;
    }

    public void Write(BinaryWriter bw) {
        bw.Write(id);
        bw.Write(name);
        bw.Write(logicType);
        bw.Write(desc);
        bw.Write(durability);
        bw.Write(icon != null ? icon.name : "");
    }
    public void Read(BinaryReader br) {
        id = br.ReadInt32();
        name = br.ReadString();
        logicType = br.ReadString();
        desc = br.ReadString();
        durability = br.ReadSingle();
        br.ReadString();
    }
}
