using System.Collections.Generic;
using UnityEngine;

// SFX
// pool sfx sounds, using a limit for certain types and priority sounds for players etc
public static class SfxManager
{

  //
  public enum AudioClass
  {
    NONE,
    FOOTSTEP,
    BULLET_SFX
  }
  struct AudioData
  {
    public AudioSource _audioSource;
    public AudioClass _audioClass;
    public float _volume, _pitch;

    public bool _changePitch;
  }

  //
  static Queue<AudioSource> s_audioSourcesAvailable;
  static List<AudioData> s_audioSourcesPlaying;
  static Dictionary<AudioClass, int> s_audioClassAmounts, s_audioClassCounts;
  static Transform s_audioContainer, s_audioContainerPlaying;

  // Create audio pool
  public static void Init()
  {
    s_audioSourcesAvailable = new Queue<AudioSource>();
    s_audioSourcesPlaying = new List<AudioData>();
    s_audioContainer = new GameObject("AudioContainer").transform;
#if UNITY_EDITOR
    s_audioContainerPlaying = new GameObject("AudioContainerPlaying").transform;
#endif

    for (var i = 0; i < 150; i++)
    {
      var audioGameObject = new GameObject($"audio{i}");
      audioGameObject.transform.parent = s_audioContainer;

      var audioSource = audioGameObject.AddComponent<AudioSource>();
      audioSource.playOnAwake = false;
      audioSource.spatialBlend = 0.75f;

      s_audioSourcesAvailable.Enqueue(audioSource);
    }

    s_audioClassAmounts = new Dictionary<AudioClass, int>();
    s_audioClassCounts = new Dictionary<AudioClass, int>();
    s_audioClassAmounts.Add(AudioClass.NONE, -1);
    s_audioClassCounts.Add(AudioClass.NONE, 0);
    s_audioClassAmounts.Add(AudioClass.FOOTSTEP, 20);
    s_audioClassCounts.Add(AudioClass.FOOTSTEP, 0);
    s_audioClassAmounts.Add(AudioClass.BULLET_SFX, 15);
    s_audioClassCounts.Add(AudioClass.BULLET_SFX, 0);
  }

  //
  public static void Reset()
  {
    if (s_audioSourcesPlaying.Count == 0)
    {
      return;
    }

    foreach (var audioData in s_audioSourcesPlaying)
    {
      var audioSource = audioData._audioSource;
      if (audioSource.clip.name == "rain") { continue; }
      audioSource.Stop();
      if (audioSource.loop)
      {
        audioSource.loop = false;
      }
    }
  }

  //
  static AudioSource GetAudioSource(Vector3 position, AudioClip clip, float volume, float pitch)
  {

    // Check if source available
    if (s_audioSourcesAvailable.Count == 0)
    {
      Debug.LogWarning("No more audio sources!");
      return null;
    }

    // Get a new audio source
    var audioSource = s_audioSourcesAvailable.Dequeue();
    audioSource.transform.position = position;
    audioSource.clip = clip;
    audioSource.volume = volume;
    audioSource.pitch = pitch;
#if UNITY_EDITOR
    audioSource.transform.parent = s_audioContainerPlaying;
#endif

    //if (audioSource.loop)
    //  audioSource.loop = false;

    return audioSource;
  }

  //
  public static AudioSource GetAudioSource(Vector3 position, AudioClip clip, AudioClass audioClass, bool priority, float volume, float pitch)
  {

    // Check if max audio types playing for class or priority
    if (!priority)
    {
      var classCount = s_audioClassCounts[audioClass];
      var classCountMax = s_audioClassAmounts[audioClass];
      if (classCountMax != -1 && classCount >= classCountMax)
      {
        //Debug.LogWarning($"Unable to play low priority clip {clip.name} ... {classCount} / {classCountMax}");

        return null;
      }
    }

    // Gather and return audio
    return GetAudioSource(position, clip, volume, pitch);
  }

  //
  public static void PlayAudioSource(AudioSource audioSource, AudioClass audioClass, bool changePitch = true)
  {
    s_audioSourcesPlaying.Add(new AudioData()
    {
      _audioSource = audioSource,
      _audioClass = audioClass,

      _volume = audioSource.volume,
      _pitch = audioSource.pitch,

      _changePitch = changePitch
    });

#if UNITY_EDITOR
    audioSource.gameObject.name = audioSource.clip.name;
#endif
    audioSource.volume *= Settings.s_SaveData.Settings.VolumeSFX / 5f;
    audioSource.Play();

    s_audioClassCounts[audioClass]++;
  }
  static void StopAudioSource(int index)
  {
    var audioData = s_audioSourcesPlaying[index];
    if (audioData._audioSource.isPlaying)
      audioData._audioSource.Stop();
    s_audioSourcesPlaying.RemoveAt(index);
    s_audioSourcesAvailable.Enqueue(audioData._audioSource);
    s_audioClassCounts[audioData._audioClass]--;
#if UNITY_EDITOR
    audioData._audioSource.transform.parent = s_audioContainer;
#endif
  }

  public static AudioSource PlayAudioSourceSimple(Vector3 position, AudioClip clip, AudioClass audioClass, float volume, float pitch, bool priority = false, bool changePitch = true)
  {
    var audioSource = GetAudioSource(position, clip, audioClass, priority, volume, pitch);
    if (audioSource == null)
    {
      return null;
    }

    PlayAudioSource(audioSource, audioClass, changePitch);
    return audioSource;
  }
  public static AudioSource PlayAudioSourceSimple(Vector3 position, AudioClip clip, float volume, float pitchMin = 0.9f, float pitchMax = 1.1f, AudioClass audioClass = AudioClass.NONE, bool priority = false, bool changePitch = true)
  {
    return PlayAudioSourceSimple(position, clip, audioClass, volume, pitchMin != pitchMax ? Random.Range(pitchMin, pitchMax) : pitchMin, priority, changePitch);
  }
  public static AudioSource PlayAudioSourceSimple(Vector3 position, string audioPath, float pitchMin = 0.9f, float pitchMax = 1.1f, AudioClass audioClass = AudioClass.NONE, bool priority = false, bool changePitch = true)
  {
    var audioClipData = FunctionsC.GetAudioSource(audioPath);
    return PlayAudioSourceSimple(position, audioClipData.clip, audioClass, audioClipData.volume, pitchMin != pitchMax ? Random.Range(pitchMin, pitchMax) : pitchMin, priority, changePitch);
  }

  public static AudioSource PlayAudioSourceSimple(this Transform transform, string audioPath, float pitchMin = 0.9f, float pitchMax = 1.1f, AudioClass audioClass = AudioClass.NONE, bool priority = false, bool changePitch = true)
  {
    return PlayAudioSourceSimple(transform.position, audioPath, pitchMin, pitchMax, audioClass, priority, changePitch);
  }
  public static AudioSource PlayAudioSourceSimple(this Transform transform, AudioClip clip, AudioClass audioClass, float volume, float pitch, bool priority = false, bool changePitch = true)
  {
    return PlayAudioSourceSimple(transform.position, clip, audioClass, volume, pitch, priority, changePitch);
  }


  // Update volume and pitch of SFX for settings and slowmso
  public static void Update(float pitch)
  {
    //Debug.Log($"playing: {s_audioSourcesPlaying.Count} avail: {s_audioSourcesAvailable.Count}");
    if (s_audioSourcesPlaying.Count == 0)
    {
      return;
    }
    for (var i = s_audioSourcesPlaying.Count - 1; i >= 0; i--)
    {

      var audioData = s_audioSourcesPlaying[i];
      var audioSource = audioData._audioSource;

      // Remove audio that stopped playing; set defaults back
      if (!audioSource.isPlaying)
      {
        StopAudioSource(i);
        continue;
      }

      // Update volume and pitch
      audioSource.volume = audioData._volume * (Settings.s_SaveData.Settings.VolumeSFX / 5f);
      if (audioData._changePitch)
        audioSource.pitch = audioData._pitch * pitch;
    }
  }
}
