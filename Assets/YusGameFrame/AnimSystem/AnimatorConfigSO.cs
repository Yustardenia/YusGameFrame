using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "YusData/Animator Config", fileName = "NewAnimConfig")]
public class AnimatorConfigSO : ScriptableObject
{
    [System.Serializable]
    public class StateInfo
    {
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

    public List<StateInfo> states = new List<StateInfo>();
    public List<ParamInfo> parameters = new List<ParamInfo>();

    public int GetStateHash(string name) => states.Find(x => x.stateName == name).hash;
    public int GetParamHash(string name) => parameters.Find(x => x.paramName == name).hash;
}