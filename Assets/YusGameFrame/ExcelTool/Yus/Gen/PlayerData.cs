using UnityEngine;
using System;
using System.IO;

[System.Serializable]
public class PlayerData : IYusBinaryData, IYusCloneable<PlayerData>
{
    public int id;
    public int hp;
    public int maxHp;
    public int mp;
    public int maxMp;
    public Vector3 location;
    public string map;

    public PlayerData Clone() {
        PlayerData copy = new PlayerData();
        copy.id = this.id;
        copy.hp = this.hp;
        copy.maxHp = this.maxHp;
        copy.mp = this.mp;
        copy.maxMp = this.maxMp;
        copy.location = this.location;
        copy.map = this.map;
        return copy;
    }

    public void Write(BinaryWriter bw) {
        bw.Write(id);
        bw.Write(hp);
        bw.Write(maxHp);
        bw.Write(mp);
        bw.Write(maxMp);
        bw.Write(location.x); bw.Write(location.y); bw.Write(location.z);
        bw.Write(map);
    }
    public void Read(BinaryReader br) {
        id = br.ReadInt32();
        hp = br.ReadInt32();
        maxHp = br.ReadInt32();
        mp = br.ReadInt32();
        maxMp = br.ReadInt32();
        location = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        map = br.ReadString();
    }
}
