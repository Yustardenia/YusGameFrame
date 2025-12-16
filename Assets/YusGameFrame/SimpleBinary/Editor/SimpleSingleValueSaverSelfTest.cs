using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SimpleSingleValueSaverSelfTest
{
    [MenuItem("Tools/Yus Data/A.3b Simple Saver Self Test")]
    public static void Run()
    {
        const string prefix = "__SimpleSaverSelfTest__";

        try
        {
            SimpleSingleValueSaver.Save(prefix + "_int", 123);
            Expect(SimpleSingleValueSaver.Load(prefix + "_int", 0) == 123, "int");

            SimpleSingleValueSaver.Save(prefix + "_float", 0.25f);
            Expect(Mathf.Approximately(SimpleSingleValueSaver.Load(prefix + "_float", 0f), 0.25f), "float");

            SimpleSingleValueSaver.Save(prefix + "_bool", true);
            Expect(SimpleSingleValueSaver.Load(prefix + "_bool", false) == true, "bool");

            SimpleSingleValueSaver.Save(prefix + "_string", "hello");
            Expect(SimpleSingleValueSaver.Load(prefix + "_string", "") == "hello", "string");

            var arr = new[] { 1, 2, 3 };
            SimpleSingleValueSaver.Save(prefix + "_arr", arr);
            var arr2 = SimpleSingleValueSaver.Load<int[]>(prefix + "_arr", null);
            Expect(arr2 != null && arr2.Length == 3 && arr2[0] == 1 && arr2[2] == 3, "int[]");

            var list = new List<string> { "a", "b", "c" };
            SimpleSingleValueSaver.Save(prefix + "_list", list);
            var list2 = SimpleSingleValueSaver.Load<List<string>>(prefix + "_list", null);
            Expect(list2 != null && list2.Count == 3 && list2[1] == "b", "List<string>");

            var obj = new TestData
            {
                id = 7,
                name = "test",
                values = new[] { 9f, 8f },
                flags = new List<int> { 1, 0, 1 }
            };
            SimpleSingleValueSaver.Save(prefix + "_obj", obj);
            var obj2 = SimpleSingleValueSaver.Load<TestData>(prefix + "_obj", null);
            Expect(obj2 != null && obj2.id == 7 && obj2.name == "test", "custom class");
            Expect(obj2.values != null && obj2.values.Length == 2 && Mathf.Approximately(obj2.values[0], 9f), "custom class array field");
            Expect(obj2.flags != null && obj2.flags.Count == 3 && obj2.flags[2] == 1, "custom class List field");

            Debug.Log("[SimpleSingleValueSaverSelfTest] All checks passed.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SimpleSingleValueSaverSelfTest] Exception: {e}");
        }
    }

    [Serializable]
    private class TestData
    {
        public int id;
        public string name;
        public float[] values;
        public List<int> flags;
    }

    private static void Expect(bool condition, string label)
    {
        if (!condition) throw new Exception("Check failed: " + label);
    }
}

