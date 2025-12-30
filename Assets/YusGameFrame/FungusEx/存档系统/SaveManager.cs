// SaveManager.cs (完整，基于上次版，微调 LoadCoroutine)
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fungus;
using System.Reflection; // 用于反射 ClickMode

[Serializable]
public class SaveData
{
    public string saveName;           // 存档名称
    public string saveTimeISO;        // 存档时间（ISO 字符串）
    public string thumbnailBase64;    // 缩略图（Base64）

    public string flowchartName;      // Flowchart 名称
    public string blockName;          // Block 名称（空 = 无需跳转）
    public int commandIndex;          // 命令索引（如果 == commandList.Count - 1，则执行最后一个命令，然后自然结束）

    [Serializable]
    public class SerializableVariable
    {
        public string key;
        public string type;
        public string value;
    }
    public SerializableVariable[] variables;
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    [Header("Save settings")]
    public string saveFolderName = "saves";
    public string saveFilePrefix = "save_"; // final filename: save_{index}.json

    [Header("Runtime")]
    public string savePath; // 运行时可读（用于 UI）
    public Flowchart targetFlowchart; // 指向要保存/读档的 Flowchart（在 Inspector 指定）

    /// <summary>
    /// 存档完成后触发（参数 = slot index）
    /// </summary>
    public Action<int> OnSaveComplete;

    // 新增：跟踪最后结束的 Block，用于存档时执行栈为空的情况
    private string lastBlockName = "";
    private bool wasExecuting = false;
    private string previousBlockName = "";
    private FieldInfo clickModeField; // 新增：反射字段

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            savePath = Path.Combine(Application.persistentDataPath, saveFolderName);
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            // 新增：初始化反射（用于临时恢复 ClickMode）
            clickModeField = typeof(DialogInput).GetField("clickMode", BindingFlags.NonPublic | BindingFlags.Instance);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (targetFlowchart == null) return;

        var currentExecuting = targetFlowchart.GetExecutingBlocks();
        bool isExecuting = currentExecuting != null && currentExecuting.Count > 0;

        if (wasExecuting && !isExecuting)
        {
            // Block 刚刚结束，记录最后 Block
            lastBlockName = previousBlockName;
            Debug.Log($"[SaveManager] Block 结束：{lastBlockName}（已记录为最后 Block）");
        }

        if (isExecuting)
        {
            previousBlockName = currentExecuting[0].BlockName ?? "";
        }

        wasExecuting = isExecuting;
    }

    private string GetSaveFilePath(int slot) => Path.Combine(savePath, $"{saveFilePrefix}{slot}.json");

    /// <summary>
    /// 保存当前游戏状态到指定槽 (slot 从 1 开始)
    /// 返回 true 表示成功
    /// </summary>
    public bool Save(int slot)
    {
        if (targetFlowchart == null)
        {
            Debug.LogError("[SaveManager] targetFlowchart 未指定！");
            return false;
        }

        try
        {
            SaveData data = new SaveData();
            data.saveName = $"存档 {slot}";
            data.saveTimeISO = DateTime.Now.ToString("o"); // ISO 8601

            // 生成缩略图（Base64）
            Texture2D tex = CaptureThumbnail();
            if (tex != null)
            {
                try
                {
                    byte[] png = tex.EncodeToPNG();
                    data.thumbnailBase64 = Convert.ToBase64String(png);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[SaveManager] 缩略图编码失败: " + e.Message);
                    data.thumbnailBase64 = "";
                }
                Destroy(tex);
            }
            else
            {
                data.thumbnailBase64 = "";
            }

            // Flowchart 状态
            data.flowchartName = targetFlowchart.name;
            var executingBlocks = targetFlowchart.GetExecutingBlocks();

            if (executingBlocks != null && executingBlocks.Count > 0)
            {
                // 运行中：保存当前 Block 和命令索引
                var executingBlock = executingBlocks[0]; // 假设线性，拿栈顶
                if (executingBlock != null)
                {
                    data.blockName = executingBlock.BlockName ?? "";
                    try
                    {
                        if (executingBlock.ActiveCommand != null)
                        {
                            data.commandIndex = executingBlock.CommandList.IndexOf(executingBlock.ActiveCommand);
                        }
                        else
                        {
                            // Block 运行中但无 activeCommand（罕见），设为 0
                            data.commandIndex = 0;
                        }
                    }
                    catch
                    {
                        data.commandIndex = 0;
                    }
                }
                else
                {
                    data.blockName = "";
                    data.commandIndex = 0;
                }
                Debug.Log($"[SaveManager] 存档时运行中 Block: {data.blockName} @ {data.commandIndex}");
            }
            else
            {
                // 执行栈为空：使用最后结束的 Block，并设索引为 Count - 1（最后一个命令，执行后自然结束）
                if (!string.IsNullOrEmpty(lastBlockName))
                {
                    Block lastBlock = targetFlowchart.FindBlock(lastBlockName);
                    if (lastBlock != null && lastBlock.CommandList.Count > 0)
                    {
                        data.blockName = lastBlockName;
                        data.commandIndex = lastBlock.CommandList.Count - 1; // 最后一个命令
                        Debug.Log($"[SaveManager] 存档时无执行，使用最后 Block: {data.blockName} @ {data.commandIndex} (最后一个命令)");
                    }
                    else
                    {
                        data.blockName = "";
                        data.commandIndex = 0;
                    }
                }
                else
                {
                    data.blockName = "";
                    data.commandIndex = 0;
                }
            }

            // 保存变量（支持常用四种）
            var list = new List<SaveData.SerializableVariable>();
            foreach (var v in targetFlowchart.Variables)
            {
                var sv = new SaveData.SerializableVariable
                {
                    key = v.Key,
                    type = v.GetType().Name,
                    value = GetVariableValueAsString(v)
                };
                list.Add(sv);
            }
            data.variables = list.ToArray();

            // 写文件
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetSaveFilePath(slot), json);

            Debug.Log($"[SaveManager] 存档成功 slot={slot} path={GetSaveFilePath(slot)}");

            // 通知 UI 刷新
            OnSaveComplete?.Invoke(slot);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[SaveManager] 存档异常: " + e.Message);
            return false;
        }
    }

    /// <summary>
    /// 加载指定槽的 SaveData（仅返回数据，不执行 Flowchart）
    /// </summary>
    public SaveData LoadData(int slot)
    {
        string filePath = GetSaveFilePath(slot);
        if (!File.Exists(filePath)) return null;

        try
        {
            string json = File.ReadAllText(filePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] 读取存档失败 slot={slot} -> {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 真正的读档：异步恢复变量并尝试跳转到存档位置（若 blockName 为空则不跳转）
    /// </summary>
    public void Load(int slot) // 改为 void + Coroutine 调用
    {
        StartCoroutine(LoadCoroutine(slot));
    }

    private IEnumerator LoadCoroutine(int slot)
    {
        string filePath = GetSaveFilePath(slot);
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"[SaveManager] 读档失败：slot {slot} 文件不存在");
            yield break;
        }

        string json = File.ReadAllText(filePath);
        SaveData data = null;
        try
        {
            data = JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError("[SaveManager] JSON 解析异常: " + e.Message);
            yield break;
        }

        if (targetFlowchart == null)
        {
            Debug.LogError("[SaveManager] targetFlowchart 未指定，无法读档");
            yield break;
        }

        if (!string.IsNullOrEmpty(data.flowchartName) && data.flowchartName != targetFlowchart.name)
        {
            Debug.LogWarning("[SaveManager] Flowchart 名称不匹配，读档已被取消。");
            yield break;
        }

        // 恢复变量
        if (data.variables != null)
        {
            foreach (var sv in data.variables)
            {
                if (!targetFlowchart.HasVariable(sv.key)) continue;
                var variable = targetFlowchart.GetVariable(sv.key);
                SetVariableFromString(variable, sv);
            }
        }

        // 停止所有当前执行的 Block，防止冲突
        targetFlowchart.StopAllBlocks();
        Debug.Log("[SaveManager] 已停止所有当前 Block 执行");

        // 如果保存了 blockName，才尝试跳转（index == Count - 1 时执行最后一个命令，然后自然结束）
        if (!string.IsNullOrEmpty(data.blockName))
        {
            Block block = targetFlowchart.FindBlock(data.blockName);
            if (block != null)
            {
                int safeIndex = Mathf.Clamp(data.commandIndex, 0, block.CommandList.Count - 1); // 确保不超过最后一个
                targetFlowchart.ExecuteBlock(block, safeIndex);
                string status = (data.commandIndex == block.CommandList.Count - 1) ? " (最后一个命令)" : $" @ {safeIndex}";

                // yield 下一帧，确保 ExecuteBlock 完成 UI 更新
                yield return null;

                // 新增：临时恢复 ClickMode（防输入禁用影响激活）
                DialogInput dialogInput = null;
                ClickMode tempOriginalMode = ClickMode.ClickAnywhere;
                SayDialog sayDialog = SayDialog.GetSayDialog();
                if (sayDialog != null)
                {
                    dialogInput = sayDialog.GetComponent<DialogInput>();
                    if (dialogInput != null && clickModeField != null)
                    {
                        tempOriginalMode = (ClickMode)clickModeField.GetValue(dialogInput);
                        clickModeField.SetValue(dialogInput, ClickMode.ClickAnywhere); // 临时启用
                        Debug.Log("[SaveManager] ClickMode 临时恢复为 ClickAnywhere");
                    }
                }

                // 强制激活 SayDialog UI（解决不弹出问题）
                if (sayDialog != null)
                {
                    sayDialog.gameObject.SetActive(true);
                    Debug.Log("[SaveManager] SayDialog 已强制激活");

                    // 额外保险：检查当前命令是否为 Say，并手动触发 Write
                    Command currentCommand = block.ActiveCommand;
                    if (currentCommand is Say sayCommand && !string.IsNullOrEmpty(sayCommand.GetStandardText()))
                    {
                        Writer writer = sayDialog.GetComponentInChildren<Writer>();
                        if (writer != null && !writer.IsWriting)
                        {
                            // 手动调用 Write（从 Say 获取 content）
                            yield return writer.StartCoroutine(writer.Write(sayCommand.GetStandardText(), false, true, false, false, null, null));
                            Debug.Log("[SaveManager] Writer 已手动启动写作 (从 Say 命令)");
                        }
                    }
                }

                // 恢复原 ClickMode（如果临时改了）
                if (dialogInput != null && clickModeField != null)
                {
                    clickModeField.SetValue(dialogInput, tempOriginalMode);
                    Debug.Log($"[SaveManager] ClickMode 恢复为: {tempOriginalMode}");
                }

                Debug.Log($"[SaveManager] 读档成功：slot={slot} -> {data.blockName}{status}");
            }
            else
            {
                Debug.LogError($"[SaveManager] 读档时找不到 Block: {data.blockName}");
            }
        }
        else
        {
            Debug.Log($"[SaveManager] 读档完成（slot={slot}），但存档中未包含执行块信息（仅恢复变量）。");
        }
    }

    public void Delete(int slot)
    {
        string path = GetSaveFilePath(slot);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"[SaveManager] 删除存档 slot={slot}");
            OnSaveComplete?.Invoke(slot);
        }
    }

    public bool HasSave(int slot) => File.Exists(GetSaveFilePath(slot));

    // ---------------- helpers ----------------
    private Texture2D CaptureThumbnail()
    {
        // 使用 Camera.main 截图（不包含 UI）。若要包含 UI，请改用 ScreenCapture.CaptureScreenshotAsTexture()
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[SaveManager] 未找到 Camera.main，无法截图");
            return null;
        }

        const int w = 256, h = 144;
        RenderTexture rt = new RenderTexture(w, h, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();

        cam.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        return tex;
    }

    private string GetVariableValueAsString(Variable v)
    {
        if (v is StringVariable sv) return sv.Value ?? "";
        if (v is IntegerVariable iv) return iv.Value.ToString();
        if (v is FloatVariable fv) return fv.Value.ToString();
        if (v is BooleanVariable bv) return bv.Value.ToString();
        return "";
    }

    private void SetVariableFromString(Variable variable, SaveData.SerializableVariable sv)
    {
        try
        {
            switch (sv.type)
            {
                case "StringVariable": ((StringVariable)variable).Value = sv.value; break;
                case "IntegerVariable": ((IntegerVariable)variable).Value = int.Parse(sv.value); break;
                case "FloatVariable": ((FloatVariable)variable).Value = float.Parse(sv.value); break;
                case "BooleanVariable": ((BooleanVariable)variable).Value = bool.Parse(sv.value); break;
                default:
                    Debug.LogWarning($"[SaveManager] 不支持变量类型：{sv.type}");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] 恢复变量失败: {sv.key} => {e.Message}");
        }
    }
}
