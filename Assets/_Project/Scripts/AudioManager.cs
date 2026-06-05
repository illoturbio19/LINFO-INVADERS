using System.Collections.Generic;
using UnityEngine;

public enum GameSfx
{
    PlayerShoot,
    EnemyShoot,
    EnemyHit,
    EnemyDeath,
    ResistantReflect,
    PlayerHit,
    TreatmentChange,
    Victory,
    Defeat,
    BossSpawn,
    BossShoot,
    BossHit,
    BossDeath,
    ComboStart,
    ComboRankUp,
    ComboBreak,
    ExtraLife
}

public class AudioManager : MonoBehaviour
{
    [Header("Mixer")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 0.75f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.9f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.28f;
    [SerializeField] private bool playMusicOnStart = true;
    [SerializeField] private float baseMusicPitch = 1f;
    [SerializeField] private float maxPressureMusicPitch = 1.72f;
    [SerializeField] private float musicPitchLerpSpeed = 5.5f;

    [Header("Optional clips")]
    [SerializeField] private AudioClip musicLoopClip;
    [SerializeField] private AudioClip playerShootClip;
    [SerializeField] private AudioClip enemyShootClip;
    [SerializeField] private AudioClip enemyHitClip;
    [SerializeField] private AudioClip enemyDeathClip;
    [SerializeField] private AudioClip resistantReflectClip;
    [SerializeField] private AudioClip playerHitClip;
    [SerializeField] private AudioClip treatmentChangeClip;
    [SerializeField] private AudioClip victoryClip;
    [SerializeField] private AudioClip defeatClip;
    [SerializeField] private AudioClip extraLifeClip;

    private const int SampleRate = 44100;
    private static AudioManager instance;

    private readonly Dictionary<GameSfx, AudioClip> generatedClips = new Dictionary<GameSfx, AudioClip>();
    private AudioSource audioSource;
    private AudioSource musicSource;
    private float targetMusicPitch = 1f;

    public static void EnsureMusic()
    {
        GetOrCreateInstance().StartMusic();
    }

    public static void SetMusicPressure(float pressure01)
    {
        GetOrCreateInstance().SetMusicPressureInternal(pressure01);
    }

    public static void Play(GameSfx sfx, Vector3 worldPosition)
    {
        AudioManager manager = GetOrCreateInstance();
        manager.PlayInternal(sfx, worldPosition);
    }

    private static AudioManager GetOrCreateInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        AudioManager existing = FindFirstObjectByType<AudioManager>();
        if (existing != null)
        {
            instance = existing;
            return instance;
        }

        GameObject audioObject = new GameObject("AudioManager");
        instance = audioObject.AddComponent<AudioManager>();
        DontDestroyOnLoad(audioObject);
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        EnsureAudioListener();

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = Mathf.Clamp01(masterVolume * musicVolume);
        musicSource.pitch = baseMusicPitch;
        targetMusicPitch = baseMusicPitch;
    }

    private void Start()
    {
        if (playMusicOnStart)
        {
            StartMusic();
        }
    }

    private void Update()
    {
        if (musicSource == null)
        {
            return;
        }

        musicSource.pitch = Mathf.Lerp(musicSource.pitch, targetMusicPitch, Time.deltaTime * musicPitchLerpSpeed);
    }

    private static void EnsureAudioListener()
    {
        if (FindFirstObjectByType<AudioListener>() != null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.gameObject.AddComponent<AudioListener>();
        }
    }

    private void PlayInternal(GameSfx sfx, Vector3 worldPosition)
    {
        AudioClip clip = GetClip(sfx);
        if (clip == null || audioSource == null)
        {
            return;
        }

        float volume = Mathf.Clamp01(masterVolume * sfxVolume);
        audioSource.PlayOneShot(clip, volume);
    }

    private void StartMusic()
    {
        if (musicSource == null)
        {
            return;
        }

        if (musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip = musicLoopClip != null ? musicLoopClip : CreateMusicLoop();
        musicSource.volume = Mathf.Clamp01(masterVolume * musicVolume);
        musicSource.pitch = targetMusicPitch;
        musicSource.Play();
    }

    private void SetMusicPressureInternal(float pressure01)
    {
        float pressure = Mathf.Clamp01(pressure01);
        float shapedPressure = pressure * pressure * (3f - 2f * pressure);
        targetMusicPitch = Mathf.Lerp(baseMusicPitch, maxPressureMusicPitch, shapedPressure);
    }

    private AudioClip GetClip(GameSfx sfx)
    {
        AudioClip assignedClip = GetAssignedClip(sfx);
        if (assignedClip != null)
        {
            return assignedClip;
        }

        if (generatedClips.TryGetValue(sfx, out AudioClip generatedClip))
        {
            return generatedClip;
        }

        generatedClip = CreateGeneratedClip(sfx);
        generatedClips[sfx] = generatedClip;
        return generatedClip;
    }

    private AudioClip GetAssignedClip(GameSfx sfx)
    {
        switch (sfx)
        {
            case GameSfx.PlayerShoot:
                return playerShootClip;
            case GameSfx.EnemyShoot:
                return enemyShootClip;
            case GameSfx.EnemyHit:
                return enemyHitClip;
            case GameSfx.EnemyDeath:
                return enemyDeathClip;
            case GameSfx.ResistantReflect:
                return resistantReflectClip;
            case GameSfx.PlayerHit:
                return playerHitClip;
            case GameSfx.TreatmentChange:
                return treatmentChangeClip;
            case GameSfx.Victory:
                return victoryClip;
            case GameSfx.Defeat:
                return defeatClip;
            case GameSfx.ExtraLife:
                return extraLifeClip;
            default:
                return null;
        }
    }

    private static AudioClip CreateGeneratedClip(GameSfx sfx)
    {
        switch (sfx)
        {
            case GameSfx.PlayerShoot:
                return CreateTone("SFX_PlayerShoot", 0.08f, 720f, 1040f, Waveform.Square, 0.28f);
            case GameSfx.EnemyShoot:
                return CreateTone("SFX_EnemyShoot", 0.11f, 360f, 220f, Waveform.Saw, 0.24f);
            case GameSfx.EnemyHit:
                return CreateTone("SFX_EnemyHit", 0.07f, 480f, 280f, Waveform.Square, 0.22f);
            case GameSfx.EnemyDeath:
                return CreateTone("SFX_EnemyDeath", 0.16f, 1180f, 2320f, Waveform.Square, 0.24f);
            case GameSfx.ResistantReflect:
                return CreateTone("SFX_ResistantReflect", 0.16f, 240f, 760f, Waveform.Square, 0.3f);
            case GameSfx.PlayerHit:
                return CreateTone("SFX_PlayerHit", 0.22f, 180f, 70f, Waveform.Noise, 0.32f);
            case GameSfx.TreatmentChange:
                return CreateTone("SFX_TreatmentChange", 0.12f, 420f, 900f, Waveform.Sine, 0.2f);
            case GameSfx.Victory:
                return CreateTone("SFX_Victory", 0.32f, 520f, 1040f, Waveform.Sine, 0.22f);
            case GameSfx.Defeat:
                return CreateTone("SFX_Defeat", 0.35f, 240f, 80f, Waveform.Saw, 0.26f);
            case GameSfx.BossSpawn:
                return CreateTone("SFX_BossSpawn", 0.34f, 110f, 280f, Waveform.Saw, 0.32f);
            case GameSfx.BossShoot:
                return CreateTone("SFX_BossShoot", 0.18f, 170f, 72f, Waveform.Square, 0.31f);
            case GameSfx.BossHit:
                return CreateTone("SFX_BossHit", 0.12f, 290f, 120f, Waveform.Noise, 0.24f);
            case GameSfx.BossDeath:
                return CreateTone("SFX_BossDeath", 0.8f, 165f, 34f, Waveform.Saw, 0.38f);
            case GameSfx.ComboStart:
                return CreateEchoTone("SFX_ComboStart", 0.24f, 620f, 980f, Waveform.Sine, 0.18f, 3);
            case GameSfx.ComboRankUp:
                return CreateEchoTone("SFX_ComboRankUp", 0.34f, 860f, 1560f, Waveform.Square, 0.2f, 4);
            case GameSfx.ComboBreak:
                return CreateTone("SFX_ComboBreak", 0.16f, 360f, 130f, Waveform.Saw, 0.18f);
            case GameSfx.ExtraLife:
                return CreateArpeggio("SFX_ExtraLife", 0.46f, new[] { 660f, 880f, 1320f, 1760f }, Waveform.Square, 0.18f);
            default:
                return null;
        }
    }

    private static AudioClip CreateMusicLoop()
    {
        const float bpm = 132f;
        float beat = 60f / bpm;
        float duration = beat * 16f;
        int sampleCount = Mathf.RoundToInt(duration * SampleRate);
        float[] samples = new float[sampleCount];
        int[] bassNotes = { 55, 55, 65, 55, 73, 65, 55, 49, 55, 55, 65, 55, 82, 73, 65, 55 };
        int[] leadNotes = { 440, 0, 523, 0, 660, 0, 523, 0, 587, 0, 698, 0, 784, 0, 698, 0 };
        uint noiseState = 13579u;

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)SampleRate;
            int step = Mathf.FloorToInt(time / beat) % 16;
            float stepTime = (time % beat) / beat;
            float beatEnvelope = Mathf.Exp(-stepTime * 7f);
            float bass = Mathf.Sin(2f * Mathf.PI * bassNotes[step] * time) * 0.34f * beatEnvelope;

            float pulse = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * bassNotes[step] * 2f * time)) * 0.08f * beatEnvelope;
            float lead = 0f;
            if (leadNotes[step] > 0)
            {
                float leadEnvelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(stepTime));
                lead = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * leadNotes[step] * time)) * 0.12f * leadEnvelope;
            }

            float kick = step % 4 == 0 ? Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(120f, 45f, stepTime) * time) * Mathf.Exp(-stepTime * 16f) * 0.45f : 0f;
            noiseState = noiseState * 1664525u + 1013904223u;
            float noise = ((noiseState >> 16) / 32768f) * 2f - 1f;
            float snare = step % 8 == 4 ? noise * Mathf.Exp(-stepTime * 18f) * 0.15f : 0f;
            samples[i] = Mathf.Clamp((bass + pulse + lead + kick + snare) * 0.55f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("MUS_LINFO_Invaders_PrototypeLoop", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateTone(string name, float duration, float startFrequency, float endFrequency, Waveform waveform, float volume)
    {
        int sampleCount = Mathf.Max(1, Mathf.RoundToInt(duration * SampleRate));
        float[] samples = new float[sampleCount];
        uint noiseState = 22222u;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            float frequency = Mathf.Lerp(startFrequency, endFrequency, t);
            float phase = 2f * Mathf.PI * frequency * i / SampleRate;
            float envelope = Mathf.Sin(Mathf.PI * t);
            samples[i] = GetWaveSample(waveform, phase, ref noiseState) * envelope * volume;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateEchoTone(string name, float duration, float startFrequency, float endFrequency, Waveform waveform, float volume, int echoCount)
    {
        int sampleCount = Mathf.Max(1, Mathf.RoundToInt(duration * SampleRate));
        float[] samples = new float[sampleCount];
        uint noiseState = 33333u;

        for (int echo = 0; echo < echoCount; echo++)
        {
            int delaySamples = Mathf.RoundToInt(echo * 0.045f * SampleRate);
            float echoVolume = volume * Mathf.Pow(0.54f, echo);

            for (int i = delaySamples; i < sampleCount; i++)
            {
                float localT = (i - delaySamples) / (float)Mathf.Max(1, sampleCount - delaySamples);
                float frequency = Mathf.Lerp(startFrequency, endFrequency, localT);
                float phase = 2f * Mathf.PI * frequency * (i - delaySamples) / SampleRate;
                float envelope = Mathf.Sin(Mathf.PI * localT) * (1f - localT * 0.25f);
                samples[i] += GetWaveSample(waveform, phase, ref noiseState) * envelope * echoVolume;
            }
        }

        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = Mathf.Clamp(samples[i], -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateArpeggio(string name, float duration, float[] frequencies, Waveform waveform, float volume)
    {
        int sampleCount = Mathf.Max(1, Mathf.RoundToInt(duration * SampleRate));
        float[] samples = new float[sampleCount];
        uint noiseState = 44444u;
        int noteCount = Mathf.Max(1, frequencies.Length);

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            int noteIndex = Mathf.Min(noteCount - 1, Mathf.FloorToInt(t * noteCount));
            float localT = (t * noteCount) - noteIndex;
            float frequency = frequencies[noteIndex];
            float phase = 2f * Mathf.PI * frequency * i / SampleRate;
            float envelope = Mathf.Sin(Mathf.PI * localT) * Mathf.Lerp(1f, 0.75f, t);
            samples[i] = GetWaveSample(waveform, phase, ref noiseState) * envelope * volume;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static float GetWaveSample(Waveform waveform, float phase, ref uint noiseState)
    {
        switch (waveform)
        {
            case Waveform.Square:
                return Mathf.Sin(phase) >= 0f ? 1f : -1f;
            case Waveform.Saw:
                return 2f * (phase / (2f * Mathf.PI) - Mathf.Floor(0.5f + phase / (2f * Mathf.PI)));
            case Waveform.Noise:
                noiseState = noiseState * 1664525u + 1013904223u;
                return ((noiseState >> 16) / 32768f) * 2f - 1f;
            default:
                return Mathf.Sin(phase);
        }
    }

    private enum Waveform
    {
        Sine,
        Square,
        Saw,
        Noise
    }
}
