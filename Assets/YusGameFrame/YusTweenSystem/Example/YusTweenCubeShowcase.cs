#if YUS_DOTWEEN
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// YusTween 系统演示：
/// - 运行后自动生成一排 Cube
/// - 按不同按键，对所有 Cube 施放不同“动画法术”
/// </summary>
public class YusTweenCubeShowcase : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private int count = 12;
    [SerializeField] private float spacing = 2.2f;
    [SerializeField] private Vector3 startPosition = new Vector3(-12f, 0f, 0f);
    [SerializeField] private Vector3 cubeScale = Vector3.one;
    [SerializeField] private bool addBoxCollider = true;

    [Header("Animation Defaults")]
    [SerializeField] private float moveDistance = 1.5f;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private bool unscaledTime = false;

    private readonly List<Transform> _cubes = new List<Transform>();
    private readonly List<Vector3> _basePositions = new List<Vector3>();
    private readonly List<Quaternion> _baseRotations = new List<Quaternion>();
    private readonly List<Vector3> _baseScales = new List<Vector3>();
    private readonly List<Color> _baseColors = new List<Color>();

    private bool _infiniteRotateEnabled;

    private void Start()
    {
        SpawnCubes();
    }

    private void Update()
    {
        if (_cubes.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetAll();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            MoveUpDown();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            RotateSpin();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ScalePop();
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            PunchPosition();
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            ShakePosition();
        }

        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            ShakeRotation();
        }

        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            ShakeScale();
        }

        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            ColorInstant();
        }

        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            ColorFade();
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            ToggleInfiniteRotate();
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 520, 260), GUI.skin.box);
        GUILayout.Label("YusTweenCubeShowcase");
        GUILayout.Label("1: MoveUpDown   2: RotateSpin   3: ScalePop");
        GUILayout.Label("4: PunchPos     5: ShakePos     6: ShakeRot");
        GUILayout.Label("7: ShakeScale   8: ColorInstant 9: ColorFade");
        GUILayout.Label("0: Toggle InfiniteRotate");
        GUILayout.Label("R: Reset (stop + restore transforms/colors)");
        GUILayout.EndArea();
    }

    private YusTweenManager Tween
    {
        get
        {
            if (YusSingletonManager.Instance != null && YusSingletonManager.Instance.Tween != null)
            {
                return YusSingletonManager.Instance.Tween;
            }

            if (YusTweenManager.Instance != null) return YusTweenManager.Instance;

            var found = FindObjectOfType<YusTweenManager>();
            if (found != null) return found;

            var go = new GameObject(nameof(YusTweenManager));
            return go.AddComponent<YusTweenManager>();
        }
    }

    private void SpawnCubes()
    {
        ClearOld();

        for (int i = 0; i < count; i++)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"TweenCube_{i:D2}";
            cube.transform.SetParent(transform, worldPositionStays: true);

            Vector3 pos = startPosition + new Vector3(i * spacing, 0f, 0f);
            cube.transform.position = pos;
            cube.transform.localScale = cubeScale;

            if (!addBoxCollider)
            {
                var col = cube.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }

            var renderer = cube.GetComponent<Renderer>();
            Color baseColor = Color.HSVToRGB((i / Mathf.Max(1f, count - 1f)) * 0.85f, 0.8f, 0.9f);
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = baseColor;
            }

            _cubes.Add(cube.transform);
            _basePositions.Add(pos);
            _baseRotations.Add(cube.transform.rotation);
            _baseScales.Add(cube.transform.localScale);
            _baseColors.Add(baseColor);
        }
    }

    private void ClearOld()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        _cubes.Clear();
        _basePositions.Clear();
        _baseRotations.Clear();
        _baseScales.Clear();
        _baseColors.Clear();
        _infiniteRotateEnabled = false;
    }

    private void ResetAll()
    {
        _infiniteRotateEnabled = false;

        for (int i = 0; i < _cubes.Count; i++)
        {
            Transform t = _cubes[i];
            if (t == null) continue;

            Tween.StopTarget(t);
            t.position = _basePositions[i];
            t.rotation = _baseRotations[i];
            t.localScale = _baseScales[i];

            var renderer = t.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = _baseColors[i];
            }
        }
    }

    private void MoveUpDown()
    {
        for (int i = 0; i < _cubes.Count; i++)
        {
            Transform t = _cubes[i];
            if (t == null) continue;

            Vector3 up = _basePositions[i] + Vector3.up * moveDistance;
            Sequence seq = YusTween.NewSequence(unscaledTime: unscaledTime, id: this, linkGameObject: t.gameObject);
            seq.Append(Tween.MoveTo(t, up, duration, YusEase.FastToSlow, unscaledTime: unscaledTime, id: this));
            seq.Append(Tween.MoveTo(t, _basePositions[i], duration, YusEase.SlowToFast, unscaledTime: unscaledTime, id: this));
        }
    }

    private void RotateSpin()
    {
        for (int i = 0; i < _cubes.Count; i++)
        {
            Transform t = _cubes[i];
            if (t == null) continue;

            Vector3 target = _baseRotations[i].eulerAngles + new Vector3(0f, 360f, 0f);
            Tween.RotateTo(t, target, duration, YusEase.CleanStop, rotateMode: RotateMode.FastBeyond360, unscaledTime: unscaledTime, id: this);
        }
    }

    private void ScalePop()
    {
        for (int i = 0; i < _cubes.Count; i++)
        {
            Transform t = _cubes[i];
            if (t == null) continue;

            Vector3 baseScale = _baseScales[i];
            Sequence seq = YusTween.NewSequence(unscaledTime: unscaledTime, id: this, linkGameObject: t.gameObject);
            seq.Append(Tween.ScaleTo(t, baseScale * 1.25f, duration * 0.6f, YusEase.PopOut, unscaledTime: unscaledTime, id: this));
            seq.Append(Tween.ScaleTo(t, baseScale, duration * 0.4f, YusEase.PopIn, unscaledTime: unscaledTime, id: this));
        }
    }

    private void PunchPosition()
    {
        for (int i = 0; i < _cubes.Count; i++)
        {
            Transform t = _cubes[i];
            if (t == null) continue;
            Tween.PunchPosition(t, new Vector3(0.4f, 0.15f, 0f), duration * 0.6f, unscaledTime: unscaledTime, id: this);
        }
    }

    private void ShakePosition()
    {
        for (int i = 0; i < _cubes.Count; i++)
        {
            Transform t = _cubes[i];
            if (t == null) continue;
            Tween.ShakePosition(t, duration * 0.8f, new Vector3(0.25f, 0.25f, 0.25f), unscaledTime: unscaledTime, id: this);
        }
    }

    private void ShakeRotation()
    {
        for (int i = 0; i < _cubes.Count; i++)
        {
            Transform t = _cubes[i];
            if (t == null) continue;
            Tween.ShakeRotation(t, duration * 0.8f, new Vector3(0f, 20f, 0f), unscaledTime: unscaledTime, id: this);
        }
    }

    private void ShakeScale()
    {
        for (int i = 0; i < _cubes.Count; i++)
        {
            Transform t = _cubes[i];
            if (t == null) continue;
            Tween.ShakeScale(t, duration * 0.8f, new Vector3(0.25f, 0.25f, 0f), unscaledTime: unscaledTime, id: this);
        }
    }

    private void ColorInstant()
    {
        for (int i = 0; i < _cubes.Count; i++)
        {
            Transform t = _cubes[i];
            if (t == null) continue;

            var renderer = t.GetComponent<Renderer>();
            if (renderer == null) continue;

            Color c = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.7f, 1f);
            Tween.ColorInstant(renderer, c);
        }
    }

    private void ColorFade()
    {
        for (int i = 0; i < _cubes.Count; i++)
        {
            Transform t = _cubes[i];
            if (t == null) continue;

            var renderer = t.GetComponent<Renderer>();
            if (renderer == null) continue;

            Color c = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.7f, 1f);
            Tween.ColorFade(renderer, c, duration: duration, unscaledTime: unscaledTime, id: this);
        }
    }

    private void ToggleInfiniteRotate()
    {
        _infiniteRotateEnabled = !_infiniteRotateEnabled;

        for (int i = 0; i < _cubes.Count; i++)
        {
            Transform t = _cubes[i];
            if (t == null) continue;

            if (_infiniteRotateEnabled)
            {
                Tween.InfiniteRotate(t, axis: Vector3.up, degreesPerSecond: 240f, localRotate: false, unscaledTime: unscaledTime, id: this);
            }
            else
            {
                Tween.StopTarget(t);
                t.rotation = _baseRotations[i];
            }
        }
    }
}

#endif
