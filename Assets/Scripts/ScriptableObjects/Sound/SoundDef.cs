using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Audio/Sound Definition", fileName = "New Sound Def")]
public class SoundDef : ScriptableObject
{
    [Header("Clips")]
    public AudioClip[] clips;

    [Header("Mixer")]
    public AudioMixerGroup mixerGroup;

    [Header("Properties")]
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;

    [Header("3D Settings")]
    [Range(0f, 1f)] public float spatialBlend = 0.8f; 
    public float minDistance = 2f;  
    public float maxDistance = 25f; 
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;

    [Header("Randomization")]
    [Range(0f, 0.5f)] public float volumeRandomness = 0.05f;
    [Range(0f, 0.5f)] public float pitchRandomness = 0.05f;



    [Header("Spam Prevention")]
    public bool useCooldown = false;
    public float cooldownTime = 0.1f;

    [Header("Stagger Settings")]
    public bool useStagger = false;
    public float staggerDelay = 0.1f;

    [System.NonSerialized]
    public float lastPlayedTime;

    public AudioClip GetRandomClip()
    {
        if (clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }
}