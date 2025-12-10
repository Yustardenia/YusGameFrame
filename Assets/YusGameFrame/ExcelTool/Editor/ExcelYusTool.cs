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

    [MenuItem("Tools/Yus Data/1. 生成代码 (Gen Code)")]
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

    [MenuItem("Tools/Yus Data/2. 导出数据到 SO (Export Data)")]
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
            int keyIndex = 0;

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
                    keyIndex = i;
                }
            }

            string className = workbook.GetSheetName(0) + "Data"; // 例如 BackpackData

            if (isGenCode)
            {
                // 生成代码
                GenCSharp(className, fieldNames, fieldTypes, keyName, keyType);
            }
            else
            {
                // 导出 SO
                ExportSO(className, sheet, fieldNames, fieldTypes);
            }
        }
    }

 // === 修改后的 GenCSharp：生成两个独立文件 ===
    private static void GenCSharp(string className, List<string> names, List<string> types, string keyName, string keyType)
    {
        // ==========================================
        // 1. 生成 Data 类 (例如 BackpackData.cs)
        // ==========================================
        StringBuilder sbData = new StringBuilder();
        sbData.AppendLine("using UnityEngine;");
        sbData.AppendLine("using System;");
        sbData.AppendLine("using System.IO;");
        sbData.AppendLine("");
        sbData.AppendLine("[System.Serializable]");
        sbData.AppendLine($"public class {className} : IYusBinaryData, IYusCloneable<{className}>");
        sbData.AppendLine("{");

        // 字段
        for (int i = 0; i < names.Count; i++)
        {
            sbData.AppendLine($"    public {MapType(types[i])} {names[i]};");
        }

        // Clone 方法
        sbData.AppendLine("");
        sbData.AppendLine($"    public {className} Clone() {{");
        sbData.AppendLine($"        {className} copy = new {className}();");
        for (int i = 0; i < names.Count; i++)
        {
            sbData.AppendLine($"        copy.{names[i]} = this.{names[i]};");
        }
        sbData.AppendLine("        return copy;");
        sbData.AppendLine("    }");

        // Write 方法
        sbData.AppendLine("");
        sbData.AppendLine("    public void Write(BinaryWriter bw) {");
        for (int i = 0; i < names.Count; i++)
        {
            string t = MapType(types[i]);
            if (t == "Vector3") sbData.AppendLine($"        bw.Write({names[i]}.x); bw.Write({names[i]}.y); bw.Write({names[i]}.z);");
            else if (t == "Sprite" || t == "GameObject") sbData.AppendLine($"        bw.Write({names[i]} != null ? {names[i]}.name : \"\");");
            else sbData.AppendLine($"        bw.Write({names[i]});");
        }
        sbData.AppendLine("    }");

        // Read 方法
        sbData.AppendLine("    public void Read(BinaryReader br) {");
        for (int i = 0; i < names.Count; i++)
        {
            string t = MapType(types[i]);
            if (t == "Vector3") sbData.AppendLine($"        {names[i]} = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());");
            else if (t == "Sprite" || t == "GameObject") sbData.AppendLine($"        br.ReadString();");
            else sbData.AppendLine($"        {names[i]} = br.{GetReadFunc(t)}();");
        }
        sbData.AppendLine("    }");

        sbData.AppendLine("}");

        // 写入 Data 文件
        File.WriteAllText(CODE_GEN_PATH + className + ".cs", sbData.ToString(), Encoding.UTF8);


        // ==========================================
        // 2. 生成 Table 类 (例如 BackpackTable.cs)
        // ==========================================
        string tableClassName = className.Replace("Data", "") + "Table"; // BackpackTable
        
        StringBuilder sbTable = new StringBuilder();
        sbTable.AppendLine("using UnityEngine;");
        sbTable.AppendLine("using System.Collections.Generic;");
        sbTable.AppendLine("");
        sbTable.AppendLine($"[CreateAssetMenu(fileName = \"{tableClassName}\", menuName = \"YusData/{tableClassName}\")]");
        sbTable.AppendLine($"public class {tableClassName} : YusTableSO<{keyType}, {className}>");
        sbTable.AppendLine("{");
        sbTable.AppendLine($"    public override {keyType} GetKey({className} data) => data.{keyName};");
        sbTable.AppendLine("}");

        // 写入 Table 文件 (注意：文件名必须与类名完全一致)
        File.WriteAllText(CODE_GEN_PATH + tableClassName + ".cs", sbTable.ToString(), Encoding.UTF8);
    }
    // ... (保留 ExportSO, MapType, ParseValue 等辅助方法不变，见上一条回答) ...
    // 为节省篇幅，辅助方法省略，请确保它们还在类里
    // -------------------------------------------------------------
    // 以下是必须存在的辅助方法，防止你漏掉
    private static void ExportSO(string className, ISheet sheet, List<string> names, List<string> types)
    {
        Type dataType = GetTypeByString(className);
        string tableName = className.Replace("Data", "") + "Table"; // 这里要对应
        Type tableType = GetTypeByString(tableName);

        if (dataType == null || tableType == null) { Debug.LogError($"找不到类: {className} 或 {tableName}"); return; }

        string assetPath = SO_OUTPUT_PATH + tableName + ".asset";
        ScriptableObject so = AssetDatabase.LoadAssetAtPath(assetPath, tableType) as ScriptableObject;
        if (so == null) { so = ScriptableObject.CreateInstance(tableType); AssetDatabase.CreateAsset(so, assetPath); }

        Type listType = typeof(List<>).MakeGenericType(dataType);
        object listInstance = Activator.CreateInstance(listType);
        MethodInfo listAdd = listType.GetMethod("Add");
        
        for (int r = 3; r <= sheet.LastRowNum; r++) {
            IRow row = sheet.GetRow(r);
            if (row == null) continue;
            object dataObj = Activator.CreateInstance(dataType);
            for (int c = 0; c < names.Count; c++) {
                FieldInfo f = dataType.GetField(names[c]);
                if (f == null) continue;
                object val = ParseValue(MapType(types[c]), row.GetCell(c)?.ToString() ?? "", f.FieldType);
                f.SetValue(dataObj, val);
            }
            listAdd.Invoke(listInstance, new object[] { dataObj });
        }
        tableType.GetMethod("EditorSetData").Invoke(so, new object[] { listInstance });
        EditorUtility.SetDirty(so);
    }
    
    private static object ParseValue(string type, string val, Type targetType)
    {
        if (string.IsNullOrEmpty(val)) return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        if (type == "int") return int.TryParse(val, out int i) ? i : 0;
        if (type == "float") return float.TryParse(val, out float f) ? f : 0f;
        if (type == "bool") return val.ToLower() == "true" || val == "1";
        if (type == "string") return val;
        if (type == "Vector3") { var p = val.Split(','); return p.Length==3 ? new Vector3(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2])) : Vector3.zero; }
        if (type == "Sprite" || type == "GameObject") return AssetDatabase.LoadAssetAtPath(val, targetType); 
        return null;
    }
    private static string MapType(string t) { if (t.Contains("int")) return "int"; if (t.Contains("float")) return "float"; if (t.Contains("bool")) return "bool"; if (t.Contains("vector")) return "Vector3"; if (t.Contains("sprite")) return "Sprite"; if (t.Contains("prefab")) return "GameObject"; return "string"; }
    private static string GetReadFunc(string type) { if (type == "int") return "ReadInt32"; if (type == "float") return "ReadSingle"; if (type == "bool") return "ReadBoolean"; return "ReadString"; }
    private static Type GetTypeByString(string name) { foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) { var type = assembly.GetType(name); if (type != null) return type; } return null; }
}