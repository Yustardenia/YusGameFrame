using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

public static class ExcelYusWriter
{
    /// <summary>
    /// 将列表数据反写回 Excel (仅编辑器可用)
    /// </summary>
    public static void WriteBack<T>(List<T> dataList)
    {
        string className = typeof(T).Name; // 例如 ItemData
        // 假设 Excel 名字是 Item.xlsx (去掉 Data 后缀)
        string excelName = className.Replace("Data", ""); 
        string filePath = ExcelYusTool.EXCEL_PATH + excelName + ".xlsx";

        if (!File.Exists(filePath))
        {
            Debug.LogError($"无法找到对应的 Excel 文件: {filePath}");
            return;
        }

        try
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                IWorkbook workbook = new XSSFWorkbook(fs);
                ISheet sheet = workbook.GetSheetAt(0);
                
                // 获取表头映射 (列名 -> 列索引)
                IRow headerRow = sheet.GetRow(0);
                IRow keyRow = sheet.GetRow(2); // key 标记行
                Dictionary<string, int> colMap = new Dictionary<string, int>();
                string keyFieldName = "";
                
                for (int i = 0; i < headerRow.LastCellNum; i++)
                {
                    string colName = headerRow.GetCell(i)?.ToString();
                    if (!string.IsNullOrEmpty(colName))
                    {
                        colMap[colName] = i;
                        // 找主键
                        if (keyRow.GetCell(i)?.ToString().ToLower() == "key")
                            keyFieldName = colName;
                    }
                }

                if (string.IsNullOrEmpty(keyFieldName))
                {
                    Debug.LogError("Excel 中未标记 key 行，无法定位数据");
                    return;
                }

                // 获取 C# 类的主键字段
                FieldInfo keyField = typeof(T).GetField(keyFieldName);
                if (keyField == null)
                {
                    Debug.LogError($"类 {className} 中找不到字段 {keyFieldName}");
                    return;
                }

                // 遍历内存中的数据列表
                int updateCount = 0;
                FieldInfo[] allFields = typeof(T).GetFields();

                foreach (var data in dataList)
                {
                    // 获取当前数据的主键值
                    string idVal = keyField.GetValue(data).ToString();

                    // 在 Excel 中查找对应的行
                    IRow targetRow = FindRowByKey(sheet, colMap[keyFieldName], idVal);
                    
                    if (targetRow != null)
                    {
                        // 更新该行所有列
                        foreach (var field in allFields)
                        {
                            if (colMap.ContainsKey(field.Name))
                            {
                                int colIndex = colMap[field.Name];
                                object val = field.GetValue(data);
                                SetCellValue(targetRow.GetCell(colIndex) ?? targetRow.CreateCell(colIndex), val);
                            }
                        }
                        updateCount++;
                    }
                    else
                    {
                        // 可选：如果没找到 ID，是否新增一行？
                        // 这里暂时不新增，防止存档数据污染配置表
                        Debug.LogWarning($"ID {idVal} 在 Excel 中不存在，跳过反写。");
                    }
                }

                // 保存文件
                using (FileStream outFs = new FileStream(filePath, FileMode.Create))
                {
                    workbook.Write(outFs);
                }
                Debug.Log($"<color=green>成功反写 Excel: {filePath} (更新了 {updateCount} 行)</color>");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"反写 Excel 失败: {ex.Message}");
        }
    }

    private static IRow FindRowByKey(ISheet sheet, int keyColIndex, string keyVal)
    {
        // 从第 4 行开始 (3, 0-based)
        for (int r = 3; r <= sheet.LastRowNum; r++)
        {
            IRow row = sheet.GetRow(r);
            if (row == null) continue;
            string cellVal = row.GetCell(keyColIndex)?.ToString();
            if (cellVal == keyVal) return row;
        }
        return null;
    }

    private static void SetCellValue(ICell cell, object val)
    {
        if (val is int i) cell.SetCellValue(i);
        else if (val is float f) cell.SetCellValue(f);
        else if (val is bool b) cell.SetCellValue(b);
        else if (val is string s) cell.SetCellValue(s);
        else if (val is Vector3 v) cell.SetCellValue($"{v.x},{v.y},{v.z}");
        else if (val is UnityEngine.Object obj) cell.SetCellValue(obj != null ? AssetDatabase.GetAssetPath(obj) : ""); // 资源写回路径
        else cell.SetCellValue(val?.ToString() ?? "");
    }
}