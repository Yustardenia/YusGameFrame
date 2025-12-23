using UnityEditor;
using UnityEngine;

#if YUS_CINEMACHINE
using Unity.Cinemachine;
#endif

public static class YusCamera2DSetupMenu
{
    [MenuItem(YusGameFrameEditorMenu.Root + "Systems/Camera/Setup 2D Cinemachine Rig", true)]
    private static bool ValidateSetup2DRig()
    {
        return YusCinemachine2DSettings.HasCinemachineInstalled() && YusCinemachine2DSettings.IsDefineEnabled();
    }

    [MenuItem(YusGameFrameEditorMenu.Root + "Systems/Camera/Setup 2D Cinemachine Rig")]
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
        var brain = cam.GetComponent<CinemachineBrain>();
        if (brain == null) brain = Undo.AddComponent<CinemachineBrain>(cam.gameObject);

        var root = new GameObject("YusCamera2D");
        Undo.RegisterCreatedObjectUndo(root, "Create YusCamera2D");

        var manager = Undo.AddComponent<YusCamera2DManager>(root);

        var vcamGo = new GameObject("Default");
        Undo.RegisterCreatedObjectUndo(vcamGo, "Create VCam2D");
        vcamGo.transform.SetParent(root.transform, false);

        var vcam = Undo.AddComponent<CinemachineCamera>(vcamGo);
        vcam.Priority = 20;

        var lens = vcam.Lens;
        lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
        lens.OrthographicSize = 5f;
        vcam.Lens = lens;

        var composer = Undo.AddComponent<CinemachinePositionComposer>(vcamGo);
        composer.CameraDistance = 10f;
        composer.Damping = new Vector3(0.5f, 0.5f, 0f);

        Undo.AddComponent<CinemachineBasicMultiChannelPerlin>(vcamGo);

        // Confiner2D is only available when CINEMACHINE_PHYSICS_2D is enabled; add via reflection to be safe.
        var confinerType = FindTypeInLoadedAssemblies("Unity.Cinemachine.CinemachineConfiner2D");
        if (confinerType != null)
        {
            var confiner = Undo.AddComponent(vcamGo, confinerType) as Behaviour;
            if (confiner != null) confiner.enabled = false;
        }

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
            "当前未启用 Cinemachine 支持。\n请在菜单启用：Tools/YusGameFrame/Systems/Camera/Cinemachine 2D/Enable",
            "确定");
#endif
    }

#if YUS_CINEMACHINE
    private static System.Type FindTypeInLoadedAssemblies(string fullName)
    {
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            if (asm == null) continue;
            var t = asm.GetType(fullName, false);
            if (t != null) return t;
        }
        return null;
    }
#endif
}
