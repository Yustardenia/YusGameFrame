using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "YusData/Animator Config", fileName = "NewAnimConfig")]
public class AnimatorConfigSO : ScriptableObject
{
    [System.Serializable]
    public class StateInfo
    {
        // 状态机路径（用于区分同名 State），例如 "BaseLayer/Locomotion/Idle"
        public string path;
        public string stateName;
        public int hash;
    }

    [System.Serializable]
    public class ParamInfo
    {
        public string paramName;
        public int hash;
        public AnimatorControllerParameterType type;
    }

    [System.Serializable]
    public class ConditionInfo
    {
        public string paramName;
        public int paramHash;
        public ConditionMode mode;
        public float threshold;
    }

    // 运行时代码不能引用 UnityEditor.Animations.AnimatorConditionMode，因此这里定义一份可序列化的等价枚举。
    // 数值与 UnityEditor.Animations.AnimatorConditionMode 对齐，便于 Editor 侧直接 cast 存取。
    public enum ConditionMode
    {
        If = 1,
        IfNot = 2,
        Greater = 3,
        Less = 4,
        Equals = 6,
        NotEqual = 7
    }

    [System.Serializable]
    public class TransitionInfo
    {
        public bool isAnyState;
        public string fromPath;
        public int fromHash;
        public string toPath;
        public int toHash;

        public bool hasExitTime;
        public float exitTime;
        public float duration;
        public float offset;

        public List<ConditionInfo> conditions = new List<ConditionInfo>();
    }

    public List<StateInfo> states = new List<StateInfo>();
    public List<ParamInfo> parameters = new List<ParamInfo>();
    public List<TransitionInfo> transitions = new List<TransitionInfo>();

    private Dictionary<string, int> _stateHashByPath;
    private Dictionary<string, int> _stateHashByName;
    private Dictionary<string, int> _paramHashByName;

    private void OnEnable()
    {
        BuildCaches();
    }

    private void BuildCaches()
    {
        _stateHashByPath = new Dictionary<string, int>();
        _stateHashByName = new Dictionary<string, int>();
        _paramHashByName = new Dictionary<string, int>();

        if (states != null)
        {
            foreach (var s in states)
            {
                if (s == null) continue;
                if (!string.IsNullOrEmpty(s.path) && !_stateHashByPath.ContainsKey(s.path))
                    _stateHashByPath[s.path] = s.hash;

                // name 不是唯一键，但常见用法仍会查；这里保留“第一个”作为默认返回
                if (!string.IsNullOrEmpty(s.stateName) && !_stateHashByName.ContainsKey(s.stateName))
                    _stateHashByName[s.stateName] = s.hash;
            }
        }

        if (parameters != null)
        {
            foreach (var p in parameters)
            {
                if (p == null) continue;
                if (!string.IsNullOrEmpty(p.paramName) && !_paramHashByName.ContainsKey(p.paramName))
                    _paramHashByName[p.paramName] = p.hash;
            }
        }
    }

    public bool TryGetStateHash(string name, out int hash)
    {
        if (_stateHashByName == null) BuildCaches();
        return _stateHashByName.TryGetValue(name, out hash);
    }

    public bool TryGetStateHashByPath(string path, out int hash)
    {
        if (_stateHashByPath == null) BuildCaches();
        return _stateHashByPath.TryGetValue(path, out hash);
    }

    public bool TryGetParamHash(string name, out int hash)
    {
        if (_paramHashByName == null) BuildCaches();
        return _paramHashByName.TryGetValue(name, out hash);
    }

    // 兼容旧 API：找不到返回 0
    public int GetStateHash(string name) => TryGetStateHash(name, out var h) ? h : 0;
    public int GetParamHash(string name) => TryGetParamHash(name, out var h) ? h : 0;
}
