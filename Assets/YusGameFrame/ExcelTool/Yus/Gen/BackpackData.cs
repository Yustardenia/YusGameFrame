using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class BackpackData : IYusBinaryData, IYusCloneable<BackpackData>
{
    public int id;
    public string name;
    public string logicType;
    public string desc;
    public float durability;
    public string icon;

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
        bw.Write(name ?? "");
        bw.Write(logicType ?? "");
        bw.Write(desc ?? "");
        bw.Write(durability);
        bw.Write(icon ?? "");
    }

    public void Read(BinaryReader br, int version) {
        id = br.ReadInt32();
        name = br.ReadString();
        logicType = br.ReadString();
        desc = br.ReadString();
        durability = br.ReadSingle();
        icon = br.ReadString();
    }
}
