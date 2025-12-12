using UnityEngine;
using System.Collections;

public class SceneAudioManager : MonoBehaviour
{
    public static SceneAudioManager Instance { get; private set; }

    [Header("配置")]
    [SerializeField] private AudioClip defaultBGM;
    [SerializeField] private bool playBgmOnStart = true;
    
    [Header("音效库")]
    [SerializeField] private AudioLibrary[] audioLibraries; 

    [Header("运行时自动生成的组件")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    // 用于记录被“临时中断”的音乐状态
    private AudioClip _lastMusicClip;
    private float _lastMusicTime;
    
    // 用于控制淡入淡出的协程
    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        Instance = this;

        if (audioLibraries != null)
        {
            foreach (var lib in audioLibraries) if(lib != null) lib.Initialize();
        }

        SetupAudioSources();
    }

    private void Start()
    {
        this.YusRegister<float>(YusEvents.OnMusicVolChange,UpdateMusicVolume);
        this.YusRegister<float>(YusEvents.OnSFXVolChange,UpdateSFXVolume);

        UpdateMusicVolume(AudioData.MusicVolume);
        UpdateSFXVolume(AudioData.SFXVolume);

        if (playBgmOnStart && defaultBGM != null)
        {
            PlayMusic(defaultBGM);
        }
    }
    

    private void SetupAudioSources()
    {
        if (musicSource == null)
        {
            GameObject musicObj = new GameObject("MusicSource_Auto");
            musicObj.transform.SetParent(this.transform);
            musicSource = musicObj.AddComponent<AudioSource>();
        }
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;

        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFXSource_Auto");
            sfxObj.transform.SetParent(this.transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
        }
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
    }

    // ================== 【新增/修改】 音乐控制接口 ==================

    /// <summary>
    /// [基础] 播放指定 AudioClip
    /// </summary>
    public void PlayMusic(AudioClip clip, float fadeDuration = 0f)
    {
        if (clip == null) return;
        
        // 如果是同一首且正在播放，就不重置了
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        // 停止之前的淡入淡出（如果有）
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);

        musicSource.clip = clip;
        musicSource.loop = true; // 确保是循环的
        musicSource.time = 0;    // 重置播放进度
        
        if (fadeDuration > 0)
        {
            musicSource.volume = 0;
            musicSource.Play();
            _fadeCoroutine = StartCoroutine(FadeMusicRoutine(AudioData.MusicVolume, fadeDuration));
        }
        else
        {
            musicSource.volume = AudioData.MusicVolume;
            musicSource.Play();
        }
    }

    /// <summary>
    /// [重载] 通过名字在库里查找并播放音乐 (例如: "BossTheme")
    /// </summary>
    public void PlayMusic(string musicName, float fadeDuration = 0f)
    {
        SoundItem item = FindSoundItem(musicName);
        if (item != null)
        {
            PlayMusic(item.clip, fadeDuration);
        }
        else
        {
            Debug.LogWarning($"[SceneAudioManager] 找不到音乐: {musicName}");
        }
    }

    /// <summary>
    /// 暂停当前音乐（保持进度）
    /// 适用场景：打开暂停菜单、弹出广告
    /// </summary>
    public void PauseMusic()
    {
        if (musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }

    /// <summary>
    /// 恢复暂停的音乐
    /// 适用场景：关闭菜单
    /// </summary>
    public void ResumeMusic()
    {
        // UnPause 只有在 AudioSource 处于 Pause 状态时才有效
        musicSource.UnPause();
    }

    /// <summary>
    /// 彻底停止音乐（进度归零）
    /// </summary>
    public void StopMusic(float fadeDuration = 0f)
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);

        if (fadeDuration > 0 && musicSource.isPlaying)
        {
            _fadeCoroutine = StartCoroutine(FadeMusicRoutine(0, fadeDuration, true));
        }
        else
        {
            musicSource.Stop();
        }
    }
    
    private IEnumerator FadeMusicRoutine(float targetVolume, float duration, bool stopOnComplete = false)
    {
        float startVolume = musicSource.volume;
        float timer = 0;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);
            yield return null;
        }
        
        musicSource.volume = targetVolume;
        
        if (stopOnComplete)
        {
            musicSource.Stop();
            musicSource.volume = AudioData.MusicVolume; 
        }
        
        _fadeCoroutine = null;
    }
    

    /// <summary>
    /// 临时切换音乐（例如：进入战斗，或者播放剧情插曲）
    /// 系统会记住当前正在播放的BGM和进度
    /// </summary>
    /// <param name="newClip">要播放的新音乐</param>
    public void SwitchMusicTemporary(AudioClip newClip)
    {
        // 1. 记录当前状态
        if (musicSource.isPlaying || musicSource.time > 0)
        {
            _lastMusicClip = musicSource.clip;
            _lastMusicTime = musicSource.time;
        }

        // 2. 播放新音乐
        PlayMusic(newClip);
    }

    /// <summary>
    /// 重载：通过名字临时切换
    /// </summary>
    public void SwitchMusicTemporary(string musicName)
    {
        SoundItem item = FindSoundItem(musicName);
        if (item != null)
        {
            SwitchMusicTemporary(item.clip);
        }
    }

    /// <summary>
    /// 恢复之前被打断的音乐
    /// 适用场景：战斗结束，切回地图BGM
    /// </summary>
    public void ReturnToPreviousMusic()
    {
        if (_lastMusicClip != null)
        {
            musicSource.clip = _lastMusicClip;
            musicSource.time = _lastMusicTime; // 恢复进度
            musicSource.Play();
            
            // 清空记录，防止逻辑混乱
            _lastMusicClip = null;
            _lastMusicTime = 0;
        }
        else
        {
            Debug.LogWarning("没有可以恢复的上一首音乐，或许你可以直接调用 PlayMusic 播放默认音乐");
        }
    }

    // ================== 辅助与其他 ==================


    public void PlaySFX(string soundName)
    {
        SoundItem item = FindSoundItem(soundName);
        if (item != null)
        {
            // 【修复】不要再乘 AudioData.SFXVolume 了
            // 因为 sfxSource.volume 已经被 UpdateSFXVolume 设置过了
            // 这里只需要传素材自身的修正系数 (volumeScale)
            sfxSource.PlayOneShot(item.clip, item.volumeScale);
        }
        else
        {
            Debug.LogWarning($"[SceneAudioManager] 未找到音效: {soundName}");
        }
    }

    private SoundItem FindSoundItem(string name)
    {
        foreach (var lib in audioLibraries)
        {
            if (lib != null)
            {
                var item = lib.GetSound(name);
                if (item != null) return item;
            }
        }
        return null;
    }

    private void UpdateMusicVolume(float volume) { if (musicSource != null) musicSource.volume = volume; }
    private void UpdateSFXVolume(float volume) { if (sfxSource != null) sfxSource.volume = volume; }
}