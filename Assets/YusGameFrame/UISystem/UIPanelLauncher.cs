using UnityEngine;
using UnityEngine.UI;

// 自动依赖 Button 组件，防止你挂错地方
[RequireComponent(typeof(Button))]
public class UIPanelLauncher : MonoBehaviour
{
    [Tooltip("这里填入你在 Database 里配置的 Panel Name")]
    public string targetPanelName;

    private void Start()
    {
        Button btn = GetComponent<Button>();
        
        // 自动绑定点击事件
        btn.onClick.AddListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        // 这里使用 <BasePanel> 是因为我们不需要获取具体的子类，只管打开就行
        UIManager.Instance.OpenPanel<BasePanel>(targetPanelName);
        
        // 调试日志（可选）
        // Debug.Log($"尝试打开面板: {targetPanelName}");
    }
}