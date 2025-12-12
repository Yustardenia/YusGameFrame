using System.IO;

public interface IYusBinaryData
{
    // 写入到二进制流  
    void Write(BinaryWriter bw);
    
    // 从二进制流读取 (带版本号)
    void Read(BinaryReader br, int version);
}