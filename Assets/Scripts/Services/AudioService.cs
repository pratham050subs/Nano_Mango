using UnityEngine;
using CardMatch.Core;
using CardMatch.Utils;
using System.Collections.Generic;

namespace CardMatch.Services
{
    /// <summary>
    /// Audio clip priority levels
    /// </summary>
    public enum AudioPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// Extended audio clip data with control parameters
    /// </summary>
    [System.Serializable]
    public class AudioClipData
    {
        public AudioClip clip;
        public float volumeMultiplier = 1f;
        public AudioPriority priority = AudioPriority.Normal;
        public bool canOverride = true;
        public float maxDuration = 0f; // 0 = use clip length
        public float cooldownTime = 0f; // Minimum time between plays
        public bool loop = false;

        [System.NonSerialized]
        public float lastPlayTime = 0f;

        public bool IsInCooldown => cooldownTime > 0 && Time.time - lastPlayTime < cooldownTime;
    }

    /// <summary>
    /// Service responsible for advanced audio playback with priority, duration, and volume control
    /// </summary>
    public class AudioService : MonoBehaviour, IAudioService
    {
        public static AudioService Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource uiSource;
        [SerializeField] private AudioSource prioritySource; // For high priority sounds

        [Header("Audio Clips Configuration")]
        [SerializeField] private AudioClipData[] audioClipData;

        [Header("Volume Settings")]
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float musicVolume = 1f;
        [SerializeField] private float sfxVolume = 1f;
        [SerializeField] private float uiVolume = 1f;

        private Dictionary<int, Coroutine> activeAudioCoroutines = new Dictionary<int, Coroutine>();
        private AudioSource currentlyPlayingPriorityAudio = null;
        private float globalAudioCooldown = 0.01f; // Minimal cooldown to prevent true spam
        private float lastGlobalPlayTime = 0f;

        private void Awake()
        {
            InitializeSingleton();
            InitializeAudioSources();
            ValidateAudioClipData();
        }

        private void InitializeSingleton()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("[AudioService] Instance created and set to DontDestroyOnLoad");
            }
            else
            {
                Debug.Log("[AudioService] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }
        }

        private void InitializeAudioSources()
        {
            // Create audio sources if not assigned
            if (musicSource == null) musicSource = CreateAudioSource("MusicSource", true);
            if (sfxSource == null) sfxSource = CreateAudioSource("SFXSource", false);
            if (uiSource == null) uiSource = CreateAudioSource("UISource", false);
            if (prioritySource == null) prioritySource = CreateAudioSource("PrioritySource", false);

            // Configure audio sources
            musicSource.loop = true;
            musicSource.volume = masterVolume * musicVolume;

            sfxSource.volume = masterVolume * sfxVolume;
            uiSource.volume = masterVolume * uiVolume;
            prioritySource.volume = masterVolume * sfxVolume;
        }

        /// <summary>
        /// Ensure all audio sources exist (useful in editor validation or if fields are unassigned).
        /// </summary>
        private void EnsureAudioSources()
        {
            if (musicSource == null || sfxSource == null || uiSource == null || prioritySource == null)
            {
                InitializeAudioSources();
            }
        }

        private AudioSource CreateAudioSource(string name, bool isMusic)
        {
            GameObject sourceObj = new GameObject(name);
            sourceObj.transform.SetParent(transform);
            AudioSource source = sourceObj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            if (isMusic) source.loop = true;
            return source;
        }

        private void ValidateAudioClipData()
        {
            if (audioClipData == null || audioClipData.Length == 0)
            {
                Debug.LogError("[AudioService] Audio clip data array is not set!");
                return;
            }

            Debug.Log($"[AudioService] Audio clips count: {audioClipData.Length}");
            for (int i = 0; i < audioClipData.Length; i++)
            {
                if (audioClipData[i].clip == null)
                {
                    Debug.LogWarning($"[AudioService] Audio clip at index {i} is NULL");
                }
                else
                {
                    Debug.Log($"[AudioService] Clip {i}: {audioClipData[i].clip.name}, " +
                             $"Priority: {audioClipData[i].priority}, " +
                             $"Volume: {audioClipData[i].volumeMultiplier}");
                }
            }
        }

        // Public interface methods
        public void PlayCardFlip()
        {
            PlayAudioWithConfig((int)AudioClipType.CardFlip);
        }

        public void PlayCardMatch()
        {
            PlayAudioWithConfig((int)AudioClipType.CardMatch);
        }

        public void PlayCardMismatch()
        {
            PlayAudioWithConfig((int)AudioClipType.CardMismatch);
        }

        public void PlayGameOver()
        {
            PlayAudioWithConfig((int)AudioClipType.GameOver);
        }

        /// <summary>
        /// Implements IAudioService.SetVolume. Maps to master volume.
        /// </summary>
        public void SetVolume(float volume)
        {
            SetMasterVolume(volume);
        }

        /// <summary>
        /// Play audio with full configuration support
        /// </summary>
        public void PlayAudioWithConfig(int clipIndex)
        {
            if (!ValidateClipIndex(clipIndex)) return;

            AudioClipData data = audioClipData[clipIndex];

            // Check cooldown
            if (data.IsInCooldown)
            {
                Debug.Log($"[AudioService] Clip {clipIndex} is in cooldown");
                return;
            }

            // Skip global cooldown check for card flip sounds to allow rapid clicking
            bool isCardFlip = (clipIndex == (int)AudioClipType.CardFlip);
            
            // Check global cooldown for non-priority sounds (except card flips)
            if (!isCardFlip && data.priority <= AudioPriority.Normal && Time.time - lastGlobalPlayTime < globalAudioCooldown)
            {
                Debug.Log($"[AudioService] Global cooldown active for clip {clipIndex}");
                return;
            }

            // Determine which audio source to use
            AudioSource source = GetAudioSourceForPriority(data.priority);
            float calculatedVolume = GetCalculatedVolume(data, source);

            // Use PlayOneShot for card flip sounds to allow overlapping
            if (isCardFlip)
            {
                source.PlayOneShot(data.clip, calculatedVolume);
                data.lastPlayTime = Time.time;
                
                Debug.Log($"[AudioService] Playing (OneShot): {data.clip.name}, " +
                         $"Priority: {data.priority}, " +
                         $"Volume: {calculatedVolume}, " +
                         $"Source: {source.gameObject.name}");
                return;
            }

            // For other sounds, check if we can override currently playing audio
            if (!CanPlayAudio(source, data))
            {
                Debug.Log($"[AudioService] Cannot play clip {clipIndex} - override not allowed");
                return;
            }

            // Stop any currently playing audio on this source if needed
            if (source.isPlaying && data.canOverride)
            {
                source.Stop();
            }

            // Play the audio normally
            PlayAudioOnSource(source, data.clip, calculatedVolume, data.loop);

            // Update timing data
            data.lastPlayTime = Time.time;
            lastGlobalPlayTime = Time.time;

            // Handle max duration
            if (data.maxDuration > 0 && !data.loop)
            {
                StartCoroutine(StopAudioAfterDuration(source, data.maxDuration));
            }

            Debug.Log($"[AudioService] Playing: {data.clip.name}, " +
                     $"Priority: {data.priority}, " +
                     $"Volume: {calculatedVolume}, " +
                     $"Source: {source.gameObject.name}");
        }

        /// <summary>
        /// Play audio with custom parameters (overrides clip data)
        /// </summary>
        public void PlayAudioCustom(int clipIndex, float volumeMultiplier = 1f,
                                   AudioPriority priority = AudioPriority.Normal,
                                   float maxDuration = 0f)
        {
            if (!ValidateClipIndex(clipIndex)) return;

            AudioClipData data = audioClipData[clipIndex];
            AudioClipData customData = new AudioClipData
            {
                clip = data.clip,
                volumeMultiplier = volumeMultiplier,
                priority = priority,
                canOverride = data.canOverride,
                maxDuration = maxDuration > 0 ? maxDuration : data.maxDuration,
                cooldownTime = data.cooldownTime
            };

            // Temporary play with custom data
            AudioSource source = GetAudioSourceForPriority(customData.priority);
            float calculatedVolume = GetCalculatedVolume(customData, source);
            PlayAudioOnSource(source, customData.clip, calculatedVolume, false);

            if (customData.maxDuration > 0)
            {
                StartCoroutine(StopAudioAfterDuration(source, customData.maxDuration));
            }
        }

        private bool ValidateClipIndex(int clipIndex)
        {
            if (audioClipData == null)
            {
                Debug.LogError("[AudioService] Audio clip data array is NULL!");
                return false;
            }

            if (clipIndex < 0 || clipIndex >= audioClipData.Length)
            {
                Debug.LogWarning($"[AudioService] Clip index {clipIndex} out of range");
                return false;
            }

            if (audioClipData[clipIndex].clip == null)
            {
                Debug.LogWarning($"[AudioService] Clip at index {clipIndex} is NULL");
                return false;
            }

            return true;
        }

        private AudioSource GetAudioSourceForPriority(AudioPriority priority)
        {
            switch (priority)
            {
                case AudioPriority.Critical:
                    return prioritySource;
                case AudioPriority.High:
                    return uiSource;
                case AudioPriority.Normal:
                    return sfxSource;
                case AudioPriority.Low:
                    return sfxSource;
                default:
                    return sfxSource;
            }
        }

        private bool CanPlayAudio(AudioSource source, AudioClipData data)
        {
            if (!source.isPlaying) return true;

            // Get current clip's priority
            int currentClipIndex = GetClipIndexForSource(source);
            if (currentClipIndex >= 0)
            {
                AudioPriority currentPriority = audioClipData[currentClipIndex].priority;

                // Higher priority can override lower priority
                if (data.priority > currentPriority && data.canOverride)
                    return true;

                // Same priority needs override permission
                if (data.priority == currentPriority && data.canOverride)
                    return true;

                return false;
            }

            return data.canOverride;
        }

        private int GetClipIndexForSource(AudioSource source)
        {
            for (int i = 0; i < audioClipData.Length; i++)
            {
                if (audioClipData[i].clip == source.clip)
                    return i;
            }
            return -1;
        }

        private float GetCalculatedVolume(AudioClipData data, AudioSource source)
        {
            float baseVolume = masterVolume;

            // Apply category volume
            if (source == musicSource)
                baseVolume *= musicVolume;
            else if (source == uiSource)
                baseVolume *= uiVolume;
            else
                baseVolume *= sfxVolume;

            // Apply clip-specific volume multiplier
            return Mathf.Clamp01(baseVolume * data.volumeMultiplier);
        }

        private void PlayAudioOnSource(AudioSource source, AudioClip clip, float volume, bool loop)
        {
            source.clip = clip;
            source.volume = volume;
            source.loop = loop;
            source.Play();
        }

        private System.Collections.IEnumerator StopAudioAfterDuration(AudioSource source, float duration)
        {
            yield return new WaitForSeconds(duration);
            if (source.isPlaying && source.clip != null)
            {
                source.Stop();
                Debug.Log($"[AudioService] Stopped audio after {duration} seconds");
            }
        }

        // Volume control methods
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateAllAudioVolumes();
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            musicSource.volume = masterVolume * musicVolume;
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            sfxSource.volume = masterVolume * sfxVolume;
            prioritySource.volume = masterVolume * sfxVolume;
        }

        public void SetUIVolume(float volume)
        {
            uiVolume = Mathf.Clamp01(volume);
            uiSource.volume = masterVolume * uiVolume;
        }

        private void UpdateAllAudioVolumes()
        {
            EnsureAudioSources();

            if (musicSource == null || sfxSource == null || uiSource == null || prioritySource == null)
            {
                return; // Safety guard
            }

            musicSource.volume = masterVolume * musicVolume;
            sfxSource.volume = masterVolume * sfxVolume;
            uiSource.volume = masterVolume * uiVolume;
            prioritySource.volume = masterVolume * sfxVolume;
        }

        // Additional control methods
        public void StopAllAudio()
        {
            musicSource.Stop();
            sfxSource.Stop();
            uiSource.Stop();
            prioritySource.Stop();
        }

        public void StopAudio(int clipIndex)
        {
            if (!ValidateClipIndex(clipIndex)) return;

            AudioClip targetClip = audioClipData[clipIndex].clip;
            AudioSource[] sources = { musicSource, sfxSource, uiSource, prioritySource };

            foreach (AudioSource source in sources)
            {
                if (source.isPlaying && source.clip == targetClip)
                {
                    source.Stop();
                    Debug.Log($"[AudioService] Stopped clip: {targetClip.name}");
                    break;
                }
            }
        }

        public void PauseAllAudio()
        {
            musicSource.Pause();
            sfxSource.Pause();
            uiSource.Pause();
            prioritySource.Pause();
        }

        public void ResumeAllAudio()
        {
            musicSource.UnPause();
            sfxSource.UnPause();
            uiSource.UnPause();
            prioritySource.UnPause();
        }

        // For Unity Editor debugging
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                EnsureAudioSources();
                UpdateAllAudioVolumes();
            }
        }
#endif
    }
}