using UnityEngine;
using Fungus;
using TMPro;
using Fungus.TMProLinkAnimEffects;

namespace YusGameFrame
{
    /// <summary>
    /// 自定义 TextMeshPro 链接动画效果注册类。
    /// 自动在游戏启动时注册新的 link 标签效果。
    /// </summary>
    public class CustomTMProEffects
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void RegisterCustomEffects()
        {
            // ---------------------------------------------------------
            // 1. Heartbeat (心跳)
            // 效果：文字像心脏一样有节奏地缩放。
            // 用法：<link="heartbeat">扑通扑通</link>
            // ---------------------------------------------------------
            TMProLinkAnimLookup.AddHelper("heartbeat", new PulseEffect()
            {
                mode = TMPLinkAnimatorMode.PerWord, // 按词缩放，避免字符分散
                speed = 8f,
                scale = new Vector3(0.15f, 0.15f, 0), // 缩放幅度
                hueScale = 0,
                saturationScale = 0,
                valueScale = 0
            });

            // ---------------------------------------------------------
            // 2. Spin (旋转)
            // 效果：字符原地旋转。
            // 用法：<link="spin">转转转</link>
            // ---------------------------------------------------------
            TMProLinkAnimLookup.AddHelper("spin", new PivotEffect()
            {
                mode = TMPLinkAnimatorMode.PerCharacter,
                speed = 2f,
                degScale = 20f // 旋转角度幅度
            });

            // ---------------------------------------------------------
            // 3. Rain (下雨/下坠)
            // 效果：文字向下坠落的阶梯效果。
            // 用法：<link="rain">下雨了</link>
            // ---------------------------------------------------------
            TMProLinkAnimLookup.AddHelper("rain", new AscendEffect()
            {
                mode = TMPLinkAnimatorMode.PerCharacter,
                totalStep = -0.5f // 负值表示向下
            });

            // ---------------------------------------------------------
            // 4. Glitch (故障)
            // 效果：赛博朋克风格的故障闪烁和位移。
            // 用法：<link="glitch">系统错误</link>
            // ---------------------------------------------------------
            TMProLinkAnimLookup.AddHelper("glitch", new GlitchEffect()
            {
                mode = TMPLinkAnimatorMode.PerCharacter,
                intensity = 2.0f,
                speed = 15f
            });
        }

        /// <summary>
        /// 自定义的故障效果实现
        /// </summary>
        public class GlitchEffect : BaseEffect
        {
            public float intensity = 1f;
            public float speed = 10f;

            public override Matrix4x4 TransFunc(int index)
            {
                // 使用离散的时间步长来模拟帧率不稳的感觉
                float timeStep = Mathf.Floor(Time.time * speed);
                
                // 基于时间和索引的伪随机数
                float rX = (Mathf.Sin(timeStep + index * 13.0f) > 0.5f ? 1 : -1) * Mathf.Sin(timeStep * 1.1f) * intensity * 0.05f;
                float rY = (Mathf.Cos(timeStep * 0.7f + index * 17.0f) > 0.5f ? 1 : -1) * Mathf.Cos(timeStep * 1.3f) * intensity * 0.05f;

                // 偶尔发生的缩放扭曲
                float scaleDistort = (Mathf.Sin(timeStep * 0.3f + index) > 0.95f) ? 1.5f : 1.0f;

                // 偶尔发生的颜色反转或透明度变化（这里只做位移和缩放，颜色在ColorFunc里做）
                
                return Matrix4x4.TRS(new Vector3(rX, rY, 0), Quaternion.identity, new Vector3(scaleDistort, 1, 1));
            }

            public override Color32 ColorFunc(int index, Color32 col)
            {
                float timeStep = Mathf.Floor(Time.time * speed);
                // 偶尔变色
                if (Mathf.Sin(timeStep * 0.8f + index * 2) > 0.9f)
                {
                    return new Color32(255, 0, 0, 255); // 故障红
                }
                return col;
            }
        }
    }
}
