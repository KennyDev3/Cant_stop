using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Pool Settings")]
    [SerializeField] private int initialPoolSize = 20;
    [SerializeField] private int maxPoolSize = 50;
    [SerializeField] private AudioSource sourcePrefab;

    private List<AudioSource> _pool;
    private GameObject _poolContainer;
    private Dictionary<SoundDef, float> _staggerTimers = new Dictionary<SoundDef, float>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePool()
    {
        _pool = new List<AudioSource>();
        _poolContainer = new GameObject("AudioPool");
        _poolContainer.transform.SetParent(transform);

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewSource();
        }
    }

    private AudioSource CreateNewSource()
    {
        GameObject obj = new GameObject("PooledAudioSource");
        obj.transform.SetParent(_poolContainer.transform);

        AudioSource source;
        if (sourcePrefab != null)
        {
            source = Instantiate(sourcePrefab, obj.transform);
        }
        else
        {
            source = obj.AddComponent<AudioSource>();
            // Default 2.5D settings if no prefab is used
            source.spatialBlend = 0.8f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 2f;
            source.maxDistance = 25f;
        }

        source.playOnAwake = false;
        _pool.Add(source);
        return source;
    }

    private AudioSource GetAvailableSource()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            if (!_pool[i].isPlaying) return _pool[i];
        }

        if (_pool.Count < maxPoolSize)
        {
            return CreateNewSource();
        }

        return null;
    }

    // --- Public API ---

    public void Play(SoundDef soundDef, Vector3 position, float pitchOverride = -1f)
    {
        if (soundDef == null) return;

        if (soundDef.useStagger)
        {
            HandleStaggeredPlay(soundDef, position, pitchOverride);
        }
        else
        {
            ExecutePlay(soundDef, position, pitchOverride);
        }
    }

    public void Play2D(SoundDef soundDef, float pitchOverride = -1f)
    {
        if (Camera.main != null)
        {
            Play(soundDef, Camera.main.transform.position, pitchOverride);
        }
    }

    // --- Internal Logic ---

    private void HandleStaggeredPlay(SoundDef soundDef, Vector3 position, float pitchOverride)
    {
        float currentTime = Time.time;

        if (!_staggerTimers.ContainsKey(soundDef) || _staggerTimers[soundDef] < currentTime)
        {
            _staggerTimers[soundDef] = currentTime;
        }

        float playDelay = _staggerTimers[soundDef] - currentTime;
        _staggerTimers[soundDef] += soundDef.staggerDelay;

        if (playDelay < 1.0f)
        {
            StartCoroutine(PlayDelayed(soundDef, position, pitchOverride, playDelay));
        }
    }

    private IEnumerator PlayDelayed(SoundDef soundDef, Vector3 position, float pitchOverride, float delay)
    {
        yield return new WaitForSeconds(delay);
        ExecutePlay(soundDef, position, pitchOverride);
    }

    private void ExecutePlay(SoundDef soundDef, Vector3 position, float pitchOverride)
    {
        if (soundDef.useCooldown && !soundDef.useStagger)
        {
            if (soundDef.lastPlayedTime > Time.unscaledTime)
            {
                soundDef.lastPlayedTime = 0f;
            }

            if (Time.time - soundDef.lastPlayedTime < soundDef.cooldownTime) return;
            soundDef.lastPlayedTime = Time.time;
        }

        AudioSource source = GetAvailableSource();
        if (source == null) return;

        source.transform.position = position;
        source.clip = soundDef.GetRandomClip();
        source.outputAudioMixerGroup = soundDef.mixerGroup;

        float finalVolume = soundDef.volume + Random.Range(-soundDef.volumeRandomness, soundDef.volumeRandomness);
        float finalPitch;

        if (pitchOverride > 0)
        {
            finalPitch = pitchOverride;
        }
        else
        {
            finalPitch = soundDef.pitch + Random.Range(-soundDef.pitchRandomness, soundDef.pitchRandomness);
        }

        source.volume = Mathf.Clamp01(finalVolume);
        source.pitch = Mathf.Clamp(finalPitch, 0.1f, 3f);

        source.Play();
    }
}