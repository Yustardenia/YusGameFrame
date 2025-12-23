using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System;
using System.Reflection;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

public class ExcelYusTool : EditorWindow
{
    public static string EXCEL_PATH = Application.dataPath + "/YusGameFrame/ExcelTool/Excels/";
    public static string CODE_GEN_PATH = Application.dataPath + "/YusGameFrame/ExcelTool/Yus/Gen/";
    public static string SO_OUTPUT_PATH = "Assets/Resources/YusData/";

    [MenuItem(YusGameFrameEditorMenu.Root + "Data/Excel/生成代码 (Gen Code)")]
    public static void GenerateCode()
    {
        if (!Directory.Exists(EXCEL_PATH)) { Debug.LogError("Excel目录不存在"); return; }
        if (!Directory.Exists(CODE_GEN_PATH)) Directory.CreateDirectory(CODE_GEN_PATH);

        string[] files = Directory.GetFiles(EXCEL_PATH, "*.xlsx");
        foreach (var file in files)
        {
            if (Path.GetFileName(file).StartsWith("~$")) continue;
            ProcessExcel(file, true);
        }
        AssetDatabase.Refresh();
        Debug.Log("代码生成完毕！请等待编译。");
    }

    [MenuItem(YusGameFrameEditorMenu.Root + "Data/Excel/导出数据到 SO (Export Data)")]
    public static void ExportData()
    {
        if (!Directory.Exists(SO_OUTPUT_PATH)) Directory.CreateDirectory(SO_OUTPUT_PATH);
        string[] files = Directory.GetFiles(EXCEL_PATH, "*.xlsx");
        foreach (var file in files)
        {
            if (Path.GetFileName(file).StartsWith("~$")) continue;
            ProcessExcel(file, false);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("数据导出完毕！");
    }

    private static void ProcessExcel(string filePath, bool isGenCode)
    {
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            IWorkbook workbook = new XSSFWorkbook(fs);
            ISheet sheet = workbook.GetSheetAt(0);

            IRow rowName = sheet.GetRow(0);
            IRow rowType = sheet.GetRow(1);
            IRow rowKey = sheet.GetRow(2);

            if (rowName == null || rowType == null) return;

            List<string> fieldNames = new List<string>();
            List<string> fieldTypes = new List<string>();
            string keyName = "id";
            string keyType = "int";
            
            // 收集字段信息
            int colCount = rowName.LastCellNum;
            for (int i = 0; i < colCount; i++)
            {
                string fName = rowName.GetCell(i)?.ToString();
                string fType = rowType.GetCell(i)?.ToString().ToLower();
                string meta = rowKey?.GetCell(i)?.ToString().ToLower();

                if (string.IsNullOrEmpty(fName)) continue;

                fieldNames.Add(fName);
                fieldTypes.Add(fType);

                if (meta == "key")
                {
                    keyName = fName;
                    keyType = MapType(fType);
                }
            }

            string className = workbook.GetSheetName(0) + "Data"; // 例如 BackpackData

            if (isGenCode)
            {
                GenCSharp(className, fieldNames, fieldTypes, keyName, keyType);
            }
            else
            {
                ExportSO(className, sheet, fieldNames, fieldTypes, keyName);
            }
        }
    }

    private static void GenCSharp(string className, List<string> names, List<string> types, string keyName, string keyType)
    {
        StringBuilder sbData = new StringBuilder();
        sbData.AppendLine("using UnityEngine;");
        sbData.AppendLine("using System;");
        sbData.AppendLine("using System.IO;");
        sbData.AppendLine("using System.Collections.Generic;"); // List 需要
        sbData.AppendLine("");
        sbData.AppendLine("[System.Serializable]");
        sbData.AppendLine($"public class {className} : IYusBinaryData, IYusCloneable<{className}>");
        sbData.AppendLine("{");

        // 字段定义
        for (int i = 0; i < names.Count; i++)
        {
            string type = MapType(types[i]);
            sbData.AppendLine($"    public {type} {names[i]};");
        }

        // Clone 方法
        sbData.AppendLine("");
        sbData.AppendLine($"    public {className} Clone() {{");
        sbData.AppendLine($"        {className} copy = new {className}();");
        for (int i = 0; i < names.Count; i++)
        {
            string t = MapType(types[i]);
            if (t.StartsWith("List<"))
            {
                // List 深拷贝
                sbData.AppendLine($"        if (this.{names[i]} != null) copy.{names[i]} = new {t}(this.{names[i]});");
                sbData.AppendLine($"        else copy.{names[i]} = new {t}();");
            }
            else
            {
                sbData.AppendLine($"        copy.{names[i]} = this.{names[i]};");
            }
        }
        sbData.AppendLine("        return copy;");
        sbData.AppendLine("    }");

        // Write 方法
        sbData.AppendLine("");
        sbData.AppendLine("    public void Write(BinaryWriter bw) {");
        for (int i = 0; i < names.Count; i++)
        {
            string t = MapType(types[i]);
            string n = names[i];

            if (t.StartsWith("List<"))
            {
                string innerType = GetInnerType(t);
                sbData.AppendLine($"        bw.Write({n} != null ? {n}.Count : 0);");
                sbData.AppendLine($"        if ({n} != null) foreach(var item in {n}) {{");
                WriteSingleValue(sbData, innerType, "item", "            ");
                sbData.AppendLine("        }");
            }
            else
            {
                WriteSingleValue(sbData, t, n, "        ");
            }
        }
        sbData.AppendLine("    }");

        // Read 方法 (带版本号)
        sbData.AppendLine("");
        sbData.AppendLine("    public void Read(BinaryReader br, int version) {");
        for (int i = 0; i < names.Count; i++)
        {
            string t = MapType(types[i]);
            string n = names[i];

            if (t.StartsWith("List<"))
            {
                string innerType = GetInnerType(t);
                sbData.AppendLine($"        int count{i} = br.ReadInt32();");
                sbData.AppendLine($"        {n} = new {t}(count{i});");
                sbData.AppendLine($"        for(int k=0; k<count{i}; k++) {{");
                sbData.AppendLine($"            {innerType} val;");
                ReadSingleValue(sbData, innerType, "val", "            ");
                sbData.AppendLine($"            {n}.Add(val);");
                sbData.AppendLine("        }");
            }
            else
            {
                ReadSingleValue(sbData, t, n, "        ");
            }
        }
        sbData.AppendLine("    }");

        sbData.AppendLine("}");
        File.WriteAllText(CODE_GEN_PATH + className + ".cs", sbData.ToString(), Encoding.UTF8);

        // 生成 Table 类
        string tableClassName = className.Replace("Data", "") + "Table";
        StringBuilder sbTable = new StringBuilder();
        sbTable.AppendLine("using UnityEngine;");
        sbTable.AppendLine("using System.Collections.Generic;");
        sbTable.AppendLine("");
        sbTable.AppendLine($"[CreateAssetMenu(fileName = \"{tableClassName}\", menuName = \"YusData/{tableClassName}\")]");
        sbTable.AppendLine($"public class {tableClassName} : YusTableSO<{keyType}, {className}>");
        sbTable.AppendLine("{");
        sbTable.AppendLine($"    public override {keyType} GetKey({className} data) => data.{keyName};");
        sbTable.AppendLine("}");
        File.WriteAllText(CODE_GEN_PATH + tableClassName + ".cs", sbTable.ToString(), Encoding.UTF8);
    }

    private static void WriteSingleValue(StringBuilder sb, string type, string varName, string indent)
    {
        if (type == "Vector3") sb.AppendLine($"{indent}bw.Write({varName}.x); bw.Write({varName}.y); bw.Write({varName}.z);");
        else if (type == "string") sb.AppendLine($"{indent}bw.Write({varName} ?? \"\");");
        else sb.AppendLine($"{indent}bw.Write({varName});");
    }

    private static void ReadSingleValue(StringBuilder sb, string type, string varName, string indent)
    {
        if (type == "Vector3") sb.AppendLine($"{indent}{varName} = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());");
        else if (type == "string") sb.AppendLine($"{indent}{varName} = br.ReadString();");
        else sb.AppendLine($"{indent}{varName} = br.{GetReadFunc(type)}();");
    }

    private static void ExportSO(string className, ISheet sheet, List<string> names, List<string> types, string keyName)
    {
        Type dataType = GetTypeByString(className);
        string tableName = className.Replace("Data", "") + "Table";
        Type tableType = GetTypeByString(tableName);

        if (dataType == null || tableType == null) { Debug.LogError($"找不到类: {className} 或 {tableName}，请先生成代码并等待编译。"); return; }

        string assetPath = SO_OUTPUT_PATH + tableName + ".asset";
        ScriptableObject so = AssetDatabase.LoadAssetAtPath(assetPath, tableType) as ScriptableObject;
        if (so == null) { so = ScriptableObject.CreateInstance(tableType); AssetDatabase.CreateAsset(so, assetPath); }

        Type listType = typeof(List<>).MakeGenericType(dataType);
        object listInstance = Activator.CreateInstance(listType);
        MethodInfo listAdd = listType.GetMethod("Add");
        
        HashSet<string> keyCheck = new HashSet<string>();

        for (int r = 3; r <= sheet.LastRowNum; r++) {
            IRow row = sheet.GetRow(r);
            if (row == null) continue;
            
            object dataObj = Activator.CreateInstance(dataType);
            
            // 查找 Key 值进行校验
            int keyIndex = names.IndexOf(keyName);
            if (keyIndex != -1)
            {
                string keyVal = row.GetCell(keyIndex)?.ToString();
                if (!string.IsNullOrEmpty(keyVal))
                {
                    if (keyCheck.Contains(keyVal)) Debug.LogError($"[Validation] 重复的 Key: {keyVal} (行 {r + 1})");
                    else keyCheck.Add(keyVal);
                }
            }

            for (int c = 0; c < names.Count; c++) {
                FieldInfo f = dataType.GetField(names[c]);
                if (f == null) continue;
                
                string cellVal = row.GetCell(c)?.ToString() ?? "";
                object val = ParseValue(MapType(types[c]), cellVal, f.FieldType);
                f.SetValue(dataObj, val);
            }
            listAdd.Invoke(listInstance, new object[] { dataObj });
        }
        tableType.GetMethod("EditorSetData").Invoke(so, new object[] { listInstance });
        EditorUtility.SetDirty(so);
    }
    
    private static object ParseValue(string type, string val, Type targetType)
    {
        if (type.StartsWith("List<"))
        {
            // 处理 List
            string innerTypeStr = GetInnerType(type);
            Type innerType = targetType.GetGenericArguments()[0];
            
            // 创建 List 实例
            var list = Activator.CreateInstance(targetType);
            var addMethod = targetType.GetMethod("Add");
            
            if (!string.IsNullOrEmpty(val))
            {
                string[] parts = val.Split('|'); // 使用 | 分隔
                foreach (var part in parts)
                {
                    object itemVal = ParseSingleValue(innerTypeStr, part, innerType);
                    addMethod.Invoke(list, new object[] { itemVal });
                }
            }
            return list;
        }
        else
        {
            return ParseSingleValue(type, val, targetType);
        }
    }

    private static object ParseSingleValue(string type, string val, Type targetType)
    {
        if (string.IsNullOrEmpty(val)) return type == "string" ? "" : Activator.CreateInstance(targetType);
        if (type == "int") return int.TryParse(val, out int i) ? i : 0;
        if (type == "float") return float.TryParse(val, out float f) ? f : 0f;
        if (type == "bool") return val.ToLower() == "true" || val == "1";
        if (type == "string") return val;
        if (type == "Vector3") { var p = val.Split(','); return p.Length==3 ? new Vector3(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2])) : Vector3.zero; }
        // 资源类型现在统一存路径字符串
        return val; 
    }

    private static string MapType(string t) 
    { 
        if (t.Contains("[]") || t.Contains("list"))
        {
            string inner = "string";
            if (t.Contains("int")) inner = "int";
            else if (t.Contains("float")) inner = "float";
            else if (t.Contains("bool")) inner = "bool";
            else if (t.Contains("vector")) inner = "Vector3";
            return $"List<{inner}>";
        }

        if (t.Contains("int")) return "int"; 
        if (t.Contains("float")) return "float"; 
        if (t.Contains("bool")) return "bool"; 
        if (t.Contains("vector")) return "Vector3"; 
        // GameObject/Sprite/Prefab 统一映射为 string (路径)
        if (t.Contains("sprite") || t.Contains("prefab") || t.Contains("gameobject")) return "string"; 
        return "string"; 
    }

    private static string GetInnerType(string listType)
    {
        // List<int> -> int
        return listType.Substring(5, listType.Length - 6);
    }

    private static string GetReadFunc(string type) { if (type == "int") return "ReadInt32"; if (type == "float") return "ReadSingle"; if (type == "bool") return "ReadBoolean"; return "ReadString"; }
    private static Type GetTypeByString(string name) { foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) { var type = assembly.GetType(name); if (type != null) return type; } return null; }

    private static void GenLanguageEnum(List<string> names, string keyName)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("namespace YusGameFrame.Localization");
        sb.AppendLine("{");
        sb.AppendLine("    // 自动生成，请勿手动修改");
        sb.AppendLine("    public enum Language");
        sb.AppendLine("    {");
        
        foreach (var name in names)
        {
            if (name != keyName)
            {
                sb.AppendLine($"        {name},");
            }
        }
        
        sb.AppendLine("    }");
        sb.AppendLine("}");
        
        string path = Application.dataPath + "/YusGameFrame/Localization/LanguageDef.cs";
        File.WriteAllText(path, sb.ToString());
        Debug.Log("已自动生成 Language 枚举: " + path);
    }
}
