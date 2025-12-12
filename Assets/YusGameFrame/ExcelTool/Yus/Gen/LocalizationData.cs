using UnityEngine;
using System;
using System.IO;

[System.Serializable]
public class LocalizationData : IYusBinaryData, IYusCloneable<LocalizationData>
{
    public string key;
    public string zh_cn;
    public string en_us;

    public LocalizationData Clone() {
        LocalizationData copy = new LocalizationData();
        copy.key = this.key;
        copy.zh_cn = this.zh_cn;
        copy.en_us = this.en_us;
        return copy;
    }

    public void Write(BinaryWriter bw) {
        bw.Write(key);
        bw.Write(zh_cn);
        bw.Write(en_us);
    }
    public void Read(BinaryReader br) {
        key = br.ReadString();
        zh_cn = br.ReadString();
        en_us = br.ReadString();
    }
}
