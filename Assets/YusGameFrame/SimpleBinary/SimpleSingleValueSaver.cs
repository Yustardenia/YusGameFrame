using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
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
    private const int CURRENT_VERSION = 2;

    private enum ValueTag : byte
    {
        Null = 0,
        Bool = 1,
        Int32 = 2,
        Single = 3,
        Double = 4,
        Int64 = 5,
        String = 6,
        Byte = 7,
        Int16 = 8,
        UInt16 = 9,
        UInt32 = 10,
        UInt64 = 11,
        Char = 12,
        Decimal = 13,
        Enum = 14,
        Array = 20,
        List = 21,
        Object = 30,
    }

    private static readonly Dictionary<Type, FieldInfo[]> SerializableFieldCache = new Dictionary<Type, FieldInfo[]>();

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
                bw.Write(CURRENT_VERSION); // Version

                // Type & Data
                string typeStr = GetTypeString(typeof(T));
                WriteString(bw, typeStr);

                // v2+: tag-based binary, supports complex types.
                WriteAnyValue(bw, value, typeof(T));
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
                if (!IsTypeCompatible(savedType, typeof(T), expectedType))
                {
                    YusLogger.Warning($"[SimpleSave] Type mismatch for {key}. Saved: {savedType}, Expected: {expectedType}");
                    return defaultValue;
                }

                // 4. Read Data
                object val = version <= 1 ? ReadBinaryValueV1(br, savedType) : ReadAnyValue(br);
                if (val == null) return defaultValue;
                if (val is T ok) return ok;

                try { return (T)Convert.ChangeType(val, typeof(T)); }
                catch { return defaultValue; }
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

    /// <summary>
    /// Editor/调试用：直接读取文件里的类型字符串与原始对象（如果无法反序列化会返回 false）。
    /// </summary>
    public static bool TryReadFile(string filePath, out string savedType, out object value)
    {
        savedType = null;
        value = null;

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return false;

        try
        {
            using (var br = new BinaryReader(File.Open(filePath, FileMode.Open)))
            {
                byte[] magic = br.ReadBytes(4);
                if (Encoding.UTF8.GetString(magic) != MAGIC) return false;

                int version = br.ReadInt32();
                savedType = ReadString(br);

                value = version <= 1 ? ReadBinaryValueV1(br, savedType) : ReadAnyValue(br);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    // --- 内部辅助方法 ---

    private static string GetTypeString(Type t)
    {
        if (t == typeof(int)) return "int";
        if (t == typeof(float)) return "float";
        if (t == typeof(bool)) return "bool";
        if (t == typeof(string)) return "string";
        return GetPortableTypeName(t);
    }

    private static string GetPortableTypeName(Type t)
    {
        if (t == null) return "";
        string aqn = t.AssemblyQualifiedName ?? "";
        if (string.IsNullOrEmpty(aqn)) return "";

        // Strip version/culture/token to make saves more resilient across Unity/.NET variations.
        aqn = Regex.Replace(aqn, @", Version=\d+\.\d+\.\d+\.\d+", "");
        aqn = Regex.Replace(aqn, @", Culture=[^,\]]+", "");
        aqn = Regex.Replace(aqn, @", PublicKeyToken=[^,\]]+", "");
        return aqn;
    }

    private static bool IsTypeCompatible(string savedType, Type expectedClrType, string expectedTypeStr)
    {
        if (savedType == expectedTypeStr) return true;

        if (TryResolveType(savedType, out var savedClrType))
        {
            return expectedClrType.IsAssignableFrom(savedClrType);
        }

        return false;
    }

    private static bool TryResolveType(string typeStr, out Type t)
    {
        t = null;
        if (string.IsNullOrEmpty(typeStr)) return false;

        switch (typeStr)
        {
            case "int": t = typeof(int); return true;
            case "float": t = typeof(float); return true;
            case "bool": t = typeof(bool); return true;
            case "string": t = typeof(string); return true;
        }

        t = Type.GetType(typeStr);
        return t != null;
    }

    // v1: only supports int/float/bool/string
    private static object ReadBinaryValueV1(BinaryReader br, string typeStr)
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

    private static void WriteAnyValue(BinaryWriter bw, object value, Type declaredType)
    {
        if (value == null)
        {
            bw.Write((byte)ValueTag.Null);
            return;
        }

        Type actualType = value.GetType();

        if (typeof(UnityEngine.Object).IsAssignableFrom(actualType))
        {
            throw new NotSupportedException($"UnityEngine.Object is not supported: {actualType.FullName}");
        }

        if (TryWritePrimitive(bw, value, actualType)) return;

        if (actualType.IsEnum)
        {
            bw.Write((byte)ValueTag.Enum);
            WriteString(bw, GetPortableTypeName(actualType));
            bw.Write(Convert.ToInt32(value));
            return;
        }

        if (actualType.IsArray)
        {
            bw.Write((byte)ValueTag.Array);
            var elementType = actualType.GetElementType() ?? typeof(object);
            WriteString(bw, GetTypeString(elementType));

            var arr = (Array)value;
            bw.Write(arr.Length);
            for (int i = 0; i < arr.Length; i++)
            {
                WriteAnyValue(bw, arr.GetValue(i), elementType);
            }
            return;
        }

        if (TryGetListElementType(actualType, out var listElementType))
        {
            bw.Write((byte)ValueTag.List);
            WriteString(bw, GetPortableTypeName(actualType));
            WriteString(bw, GetTypeString(listElementType));

            var list = (IList)value;
            bw.Write(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                WriteAnyValue(bw, list[i], listElementType);
            }
            return;
        }

        bw.Write((byte)ValueTag.Object);
        WriteString(bw, GetPortableTypeName(actualType));

        var fields = GetSerializableFields(actualType);
        bw.Write(fields.Length);
        for (int i = 0; i < fields.Length; i++)
        {
            var f = fields[i];
            WriteString(bw, f.Name);
            WriteAnyValue(bw, f.GetValue(value), f.FieldType);
        }
    }

    private static object ReadAnyValue(BinaryReader br)
    {
        ValueTag tag = (ValueTag)br.ReadByte();
        switch (tag)
        {
            case ValueTag.Null:
                return null;
            case ValueTag.Bool:
                return br.ReadByte() != 0;
            case ValueTag.Int32:
                return br.ReadInt32();
            case ValueTag.Single:
                return br.ReadSingle();
            case ValueTag.Double:
                return br.ReadDouble();
            case ValueTag.Int64:
                return br.ReadInt64();
            case ValueTag.String:
                return ReadString(br);
            case ValueTag.Byte:
                return br.ReadByte();
            case ValueTag.Int16:
                return br.ReadInt16();
            case ValueTag.UInt16:
                return br.ReadUInt16();
            case ValueTag.UInt32:
                return br.ReadUInt32();
            case ValueTag.UInt64:
                return br.ReadUInt64();
            case ValueTag.Char:
                return br.ReadChar();
            case ValueTag.Decimal:
                return br.ReadDecimal();
            case ValueTag.Enum:
            {
                string enumTypeName = ReadString(br);
                int raw = br.ReadInt32();
                if (!TryResolveType(enumTypeName, out var enumType) || enumType == null || !enumType.IsEnum) return raw;
                try { return Enum.ToObject(enumType, raw); }
                catch { return raw; }
            }
            case ValueTag.Array:
            {
                string elementTypeName = ReadString(br);
                if (!TryResolveType(elementTypeName, out var elementType) || elementType == null) elementType = typeof(object);

                int len = br.ReadInt32();
                Array arr = Array.CreateInstance(elementType, len);
                for (int i = 0; i < len; i++)
                {
                    object elem = ReadAnyValue(br);
                    arr.SetValue(ConvertToType(elem, elementType), i);
                }
                return arr;
            }
            case ValueTag.List:
            {
                string listTypeName = ReadString(br);
                string elementTypeName = ReadString(br);

                if (!TryResolveType(elementTypeName, out var elementType) || elementType == null) elementType = typeof(object);

                int count = br.ReadInt32();
                IList list = CreateListInstance(listTypeName, elementType);
                for (int i = 0; i < count; i++)
                {
                    object elem = ReadAnyValue(br);
                    list.Add(ConvertToType(elem, elementType));
                }
                return list;
            }
            case ValueTag.Object:
            {
                string typeName = ReadString(br);
                int fieldCount = br.ReadInt32();

                if (!TryResolveType(typeName, out var objType) || objType == null)
                {
                    for (int i = 0; i < fieldCount; i++)
                    {
                        _ = ReadString(br);
                        _ = ReadAnyValue(br);
                    }
                    return null;
                }

                object obj;
                try { obj = Activator.CreateInstance(objType); }
                catch
                {
                    for (int i = 0; i < fieldCount; i++)
                    {
                        _ = ReadString(br);
                        _ = ReadAnyValue(br);
                    }
                    return null;
                }

                var fieldMap = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
                var fields = GetSerializableFields(objType);
                for (int i = 0; i < fields.Length; i++) fieldMap[fields[i].Name] = fields[i];

                for (int i = 0; i < fieldCount; i++)
                {
                    string fieldName = ReadString(br);
                    object fieldVal = ReadAnyValue(br);

                    if (fieldMap.TryGetValue(fieldName, out var f))
                    {
                        try { f.SetValue(obj, ConvertToType(fieldVal, f.FieldType)); }
                        catch { /* ignore */ }
                    }
                }

                return obj;
            }
            default:
                return null;
        }
    }

    private static bool TryWritePrimitive(BinaryWriter bw, object value, Type t)
    {
        if (t == typeof(bool)) { bw.Write((byte)ValueTag.Bool); bw.Write((byte)((bool)value ? 1 : 0)); return true; }
        if (t == typeof(int)) { bw.Write((byte)ValueTag.Int32); bw.Write((int)value); return true; }
        if (t == typeof(float)) { bw.Write((byte)ValueTag.Single); bw.Write((float)value); return true; }
        if (t == typeof(double)) { bw.Write((byte)ValueTag.Double); bw.Write((double)value); return true; }
        if (t == typeof(long)) { bw.Write((byte)ValueTag.Int64); bw.Write((long)value); return true; }
        if (t == typeof(string)) { bw.Write((byte)ValueTag.String); WriteString(bw, (string)value); return true; }
        if (t == typeof(byte)) { bw.Write((byte)ValueTag.Byte); bw.Write((byte)value); return true; }
        if (t == typeof(short)) { bw.Write((byte)ValueTag.Int16); bw.Write((short)value); return true; }
        if (t == typeof(ushort)) { bw.Write((byte)ValueTag.UInt16); bw.Write((ushort)value); return true; }
        if (t == typeof(uint)) { bw.Write((byte)ValueTag.UInt32); bw.Write((uint)value); return true; }
        if (t == typeof(ulong)) { bw.Write((byte)ValueTag.UInt64); bw.Write((ulong)value); return true; }
        if (t == typeof(char)) { bw.Write((byte)ValueTag.Char); bw.Write((char)value); return true; }
        if (t == typeof(decimal)) { bw.Write((byte)ValueTag.Decimal); bw.Write((decimal)value); return true; }
        return false;
    }

    private static bool TryGetListElementType(Type t, out Type elementType)
    {
        elementType = null;
        if (t == null) return false;
        if (!t.IsGenericType) return false;

        var def = t.GetGenericTypeDefinition();
        if (def != typeof(List<>)) return false;

        elementType = t.GetGenericArguments()[0];
        return elementType != null;
    }

    private static IList CreateListInstance(string listTypeName, Type elementType)
    {
        if (TryResolveType(listTypeName, out var listType) && listType != null)
        {
            try
            {
                if (typeof(IList).IsAssignableFrom(listType))
                {
                    return (IList)Activator.CreateInstance(listType);
                }
            }
            catch { /* ignore */ }
        }

        return (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
    }

    private static FieldInfo[] GetSerializableFields(Type t)
    {
        if (t == null) return Array.Empty<FieldInfo>();

        if (SerializableFieldCache.TryGetValue(t, out var cached) && cached != null) return cached;

        var list = new List<FieldInfo>();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (var f in t.GetFields(flags))
        {
            if (f.IsStatic) continue;
            if (Attribute.IsDefined(f, typeof(NonSerializedAttribute))) continue;
            if (f.IsPublic || Attribute.IsDefined(f, typeof(SerializeField)))
            {
                list.Add(f);
            }
        }

        list.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

        cached = list.ToArray();
        SerializableFieldCache[t] = cached;
        return cached;
    }

    private static object ConvertToType(object val, Type targetType)
    {
        if (targetType == null) return val;
        if (val == null) return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

        Type valType = val.GetType();
        if (targetType.IsAssignableFrom(valType)) return val;

        if (targetType.IsEnum)
        {
            try
            {
                if (val is int i) return Enum.ToObject(targetType, i);
                if (val is string s) return Enum.Parse(targetType, s);
                return Enum.ToObject(targetType, Convert.ToInt32(val));
            }
            catch { return Activator.CreateInstance(targetType); }
        }

        try { return Convert.ChangeType(val, targetType); }
        catch { return targetType.IsValueType ? Activator.CreateInstance(targetType) : null; }
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
