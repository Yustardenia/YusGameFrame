using System;
using UnityEngine;
using System.IO;
using System.Text;

/// <summary>
/// 极简的单值存档工具 (存 int, float, bool, string)
/// 保存路径: Application.persistentDataPath/YusSimple/
/// </summary>
public static class SimpleSingleValueSaver
{
    // 使用 persistentDataPath 以确保在手机端可读写
    // 专门建一个子文件夹 YusSimple 以免跟主存档混淆
    public static string SAVE_PATH => Path.Combine(Application.persistentDataPath, "YusSimple");

    private const string MAGIC = "YUSV"; // Yus Single Value

    /// <summary>
    /// 保存值
    /// </summary>
    public static void Save<T>(string key, T value)
    {
        try
        {
            if (!Directory.Exists(SAVE_PATH)) Directory.CreateDirectory(SAVE_PATH);
            string filePath = Path.Combine(SAVE_PATH, key + ".yus");

            using (var bw = new BinaryWriter(File.Open(filePath, FileMode.Create)))
            {
                // Header
                bw.Write(Encoding.UTF8.GetBytes(MAGIC));
                bw.Write(1); // Version

                // Type & Data
                string typeStr = GetTypeString(typeof(T));
                WriteString(bw, typeStr);
                WriteBinaryValue(bw, typeStr, value);
            }
            
            // Debug.Log($"[SimpleSave] Saved {key} = {value}");
        }
        catch (Exception ex)
        {
            YusLogger.Error($"[SimpleSave] Save Error ({key}): {ex.Message}");
        }
    }

    /// <summary>
    /// 读取值 (带默认值)
    /// </summary>
    public static T Load<T>(string key, T defaultValue = default)
    {
        string filePath = Path.Combine(SAVE_PATH, key + ".yus");
        if (!File.Exists(filePath)) return defaultValue;

        try
        {
            using (var br = new BinaryReader(File.Open(filePath, FileMode.Open)))
            {
                // 1. Check Magic
                byte[] magic = br.ReadBytes(4);
                if (Encoding.UTF8.GetString(magic) != MAGIC) return defaultValue;

                // 2. Check Version
                int version = br.ReadInt32(); 

                // 3. Check Type
                string savedType = ReadString(br);
                string expectedType = GetTypeString(typeof(T));

                // 如果类型不匹配（比如存的是int，取的时候按string取），返回默认值
                if (savedType != expectedType)
                {
                    YusLogger.Warning($"[SimpleSave] Type mismatch for {key}. Saved: {savedType}, Expected: {expectedType}");
                    return defaultValue;
                }

                // 4. Read Data
                object val = ReadBinaryValue(br, savedType);
                return (T)Convert.ChangeType(val, typeof(T));
            }
        }
        catch (Exception ex)
        {
            YusLogger.Error($"[SimpleSave] Load Error ({key}): {ex.Message}");
            return defaultValue;
        }
    }

    /// <summary>
    /// 删除某个存档
    /// </summary>
    public static void Delete(string key)
    {
        string filePath = Path.Combine(SAVE_PATH, key + ".yus");
        if (File.Exists(filePath)) File.Delete(filePath);
    }

    /// <summary>
    /// 检查是否存在
    /// </summary>
    public static bool HasKey(string key)
    {
        return File.Exists(Path.Combine(SAVE_PATH, key + ".yus"));
    }

    // --- 内部辅助方法 ---

    private static string GetTypeString(Type t)
    {
        if (t == typeof(int)) return "int";
        if (t == typeof(float)) return "float";
        if (t == typeof(bool)) return "bool";
        if (t == typeof(string)) return "string";
        return "unknown";
    }

    private static void WriteBinaryValue(BinaryWriter bw, string typeStr, object value)
    {
        switch (typeStr)
        {
            case "int": bw.Write(Convert.ToInt32(value)); break;
            case "float": bw.Write(Convert.ToSingle(value)); break;
            case "bool": bw.Write(Convert.ToBoolean(value) ? (byte)1 : (byte)0); break;
            case "string": WriteString(bw, value.ToString()); break;
        }
    }

    private static object ReadBinaryValue(BinaryReader br, string typeStr)
    {
        switch (typeStr)
        {
            case "int": return br.ReadInt32();
            case "float": return br.ReadSingle();
            case "bool": return br.ReadByte() != 0;
            case "string": return ReadString(br);
            default: return null;
        }
    }

    private static void WriteString(BinaryWriter bw, string s)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(s ?? "");
        bw.Write(bytes.Length);
        bw.Write(bytes);
    }

    private static string ReadString(BinaryReader br)
    {
        int len = br.ReadInt32();
        byte[] bytes = br.ReadBytes(len);
        return Encoding.UTF8.GetString(bytes);
    }
}