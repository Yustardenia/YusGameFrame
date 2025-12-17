using UnityEditor;
using UnityEngine;

public static class YusCamera2DSetupMenu
{
    [MenuItem("YusGameFrame/Camera/Setup 2D Cinemachine Rig", true)]
    private static bool ValidateSetup2DRig()
    {
        return YusCinemachine2DSettings.HasCinemachineInstalled() && YusCinemachine2DSettings.IsDefineEnabled();
    }

    [MenuItem("YusGameFrame/Camera/Setup 2D Cinemachine Rig")]
    public static void Setup2DRig()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindObjectOfType<Camera>();

        if (cam == null)
        {
            var camGo = new GameObject("Main Camera");
            cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            Undo.RegisterCreatedObjectUndo(camGo, "Create Main Camera");
        }

        cam.orthographic = true;
#if YUS_CINEMACHINE
        var brain = cam.GetComponent<Cinemachine.CinemachineBrain>();
        if (brain == null) brain = Undo.AddComponent<Cinemachine.CinemachineBrain>(cam.gameObject);

        var root = new GameObject("YusCamera2D");
        Undo.RegisterCreatedObjectUndo(root, "Create YusCamera2D");

        var manager = Undo.AddComponent<YusCamera2DManager>(root);

        var vcamGo = new GameObject("Default");
        Undo.RegisterCreatedObjectUndo(vcamGo, "Create VCam2D");
        vcamGo.transform.SetParent(root.transform, false);

        var vcam = Undo.AddComponent<Cinemachine.CinemachineVirtualCamera>(vcamGo);
        vcam.Priority = 20;

        var lens = vcam.m_Lens;
        lens.Orthographic = true;
        lens.OrthographicSize = 5f;
        vcam.m_Lens = lens;

        var framing = vcam.AddCinemachineComponent<Cinemachine.CinemachineFramingTransposer>();
        framing.m_ScreenX = 0.5f;
        framing.m_ScreenY = 0.5f;
        framing.m_DeadZoneWidth = 0.1f;
        framing.m_DeadZoneHeight = 0.1f;
        framing.m_SoftZoneWidth = 0.8f;
        framing.m_SoftZoneHeight = 0.8f;
        framing.m_XDamping = 0.5f;
        framing.m_YDamping = 0.5f;

        var noise = vcam.AddCinemachineComponent<Cinemachine.CinemachineBasicMultiChannelPerlin>();
        noise.m_AmplitudeGain = 0f;
        noise.m_FrequencyGain = 1f;

        var confiner = Undo.AddComponent<Cinemachine.CinemachineConfiner2D>(vcamGo);
        confiner.enabled = false;

        var singletonManager = Object.FindObjectOfType<YusSingletonManager>();
        if (singletonManager != null && singletonManager.Camera2D == null)
        {
            singletonManager.Camera2D = manager;
            EditorUtility.SetDirty(singletonManager);
        }

        Selection.activeObject = root;
#else
        EditorUtility.DisplayDialog(
            "YusCamera2D",
            "当前未启用 Cinemachine 支持。\n请在菜单启用：YusGameFrame/Camera/Cinemachine 2D/Enable",
            "确定");
#endif
    }
}
