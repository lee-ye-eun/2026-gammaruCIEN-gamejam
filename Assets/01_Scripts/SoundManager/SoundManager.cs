using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SoundEntry
{
    public string key;
    public AudioClip clip;
}

// 전역 사운드 매니저. BGM은 전용 AudioSource 하나로 재생하고,
// SFX는 재생 요청이 올 때마다 풀에서 비어있는 AudioSource를 꺼내 쓰기 때문에
// 여러 SFX가 동시에 겹쳐도(루프 SFX 포함) 서로 끊기지 않는다.
// 자주 쓰는 클립은 key로 미리 등록해두고 PlaySFX("key")/PlayBGM("key")처럼 바로 호출할 수 있다.
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("BGM (한 번에 하나만 재생, 반복)")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip defaultBgm; // 지정해두면 시작할 때 자동으로 재생됨
    [SerializeField] private List<SoundEntry> bgmClips = new List<SoundEntry>(); // key로 재생할 BGM 등록

    [Header("SFX 풀 (동시 재생 가능 개수의 기본값, 부족하면 자동으로 늘어남)")]
    [SerializeField] private int initialSfxPoolSize = 4;
    [SerializeField] private List<SoundEntry> sfxClips = new List<SoundEntry>(); // key로 재생할 SFX 등록

    private readonly List<AudioSource> sfxPool = new List<AudioSource>();
    private Dictionary<string, AudioClip> bgmLookup;
    private Dictionary<string, AudioClip> sfxLookup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        bgmLookup = BuildLookup(bgmClips);
        sfxLookup = BuildLookup(sfxClips);

        for (int i = 0; i < initialSfxPoolSize; i++)
        {
            CreateSfxSource();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (defaultBgm != null) PlayBGM(defaultBgm);
    }

    private static Dictionary<string, AudioClip> BuildLookup(List<SoundEntry> entries)
    {
        var lookup = new Dictionary<string, AudioClip>();

        foreach (var entry in entries)
        {
            if (entry == null || string.IsNullOrEmpty(entry.key) || entry.clip == null) continue;
            lookup[entry.key] = entry.clip;
        }

        return lookup;
    }

    public void PlayBGM(AudioClip clip, bool loop = true, float volume = 0.5f)
    {
        if (bgmSource == null || clip == null) return;

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.volume = volume;
        bgmSource.Play();
    }

    // 미리 등록해둔 BGM을 key로 바로 재생
    public void PlayBGM(string key, bool loop = true, float volume = 0.5f)
    {
        if (bgmLookup != null && bgmLookup.TryGetValue(key, out AudioClip clip))
        {
            PlayBGM(clip, loop, volume);
        }
        else
        {
            Debug.LogWarning($"SoundManager: '{key}' 키로 등록된 BGM이 없습니다.");
        }
    }

    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    // 풀에서 비어있는 AudioSource를 꺼내(없으면 새로 만들어) 클립을 재생한다.
    // 반환된 AudioSource로 특정 SFX 인스턴스만 따로 Stop() 하는 것도 가능하다.
    public AudioSource PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        if (clip == null) return null;

        AudioSource source = GetAvailableSfxSource();
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.loop = loop;
        source.Play();

        return source;
    }

    // 미리 등록해둔 SFX를 key로 바로 재생
    public AudioSource PlaySFX(string key, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        if (sfxLookup != null && sfxLookup.TryGetValue(key, out AudioClip clip))
        {
            return PlaySFX(clip, volume, pitch, loop);
        }

        Debug.LogWarning($"SoundManager: '{key}' 키로 등록된 SFX가 없습니다.");
        return null;
    }

    private AudioSource GetAvailableSfxSource()
    {
        foreach (var source in sfxPool)
        {
            if (!source.isPlaying) return source;
        }

        return CreateSfxSource();
    }

    private AudioSource CreateSfxSource()
    {
        var sourceObject = new GameObject("SFX Source");
        sourceObject.transform.SetParent(transform);

        var source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;

        sfxPool.Add(source);
        return source;
    }
}
