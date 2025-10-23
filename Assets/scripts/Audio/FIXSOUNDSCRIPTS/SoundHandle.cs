using UnityEngine;

namespace GeminiGauntlet.Audio
{
    /// <summary>
    /// Handle for controlling individual sound instances
    /// Provides safe access to sound operations even after the sound has stopped
    /// </summary>
    public class SoundHandle
    {
        public static readonly SoundHandle Invalid = new SoundHandle();

        private PooledAudioSource audioSource;
        private SoundSystemCore soundSystem;
        private bool isValid;

        public bool IsValid => isValid && audioSource != null && audioSource.AudioSource != null;
        public bool IsPlaying => IsValid && audioSource.IsPlaying();
        public PooledAudioSource AudioSource => audioSource;

        // Private constructor for invalid handle
        private SoundHandle()
        {
            isValid = false;
        }

        // Internal constructor for valid handles
        internal SoundHandle(PooledAudioSource source, SoundSystemCore system)
        {
            audioSource = source;
            soundSystem = system;
            isValid = true;
        }

        /// <summary>
        /// Stop the sound immediately
        /// </summary>
        public void Stop()
        {
            if (IsValid)
            {
                audioSource.Stop();
            }
        }

        /// <summary>
        /// Set the volume of this sound
        /// </summary>
        public void SetVolume(float volume)
        {
            if (IsValid)
            {
                audioSource.SetVolume(Mathf.Clamp01(volume));
            }
        }

        /// <summary>
        /// Set the pitch of this sound
        /// </summary>
        public void SetPitch(float pitch)
        {
            if (IsValid)
            {
                audioSource.SetPitch(Mathf.Clamp(pitch, 0.1f, 3f));
            }
        }

        /// <summary>
        /// Fade the volume over time
        /// </summary>
        public void FadeVolume(float targetVolume, float duration)
        {
            if (IsValid && soundSystem != null)
            {
                soundSystem.StartCoroutine(FadeVolumeCoroutine(targetVolume, duration));
            }
        }

        /// <summary>
        /// Fade out and stop the sound
        /// </summary>
        public void FadeOutAndStop(float duration)
        {
            if (IsValid && soundSystem != null)
            {
                soundSystem.StartCoroutine(FadeOutAndStopCoroutine(duration));
            }
        }

        /// <summary>
        /// Fade out and stop the sound (alias for FadeOutAndStop)
        /// </summary>
        public void FadeOut(float duration)
        {
            FadeOutAndStop(duration);
        }

        /// <summary>
        /// Get the current playback time of the sound
        /// </summary>
        public float GetPlaybackTime()
        {
            return IsValid ? audioSource.GetPlaybackTime() : 0f;
        }

        /// <summary>
        /// Get the total duration of the sound clip
        /// </summary>
        public float GetClipDuration()
        {
            if (IsValid && audioSource.AudioSource.clip != null)
            {
                return audioSource.AudioSource.clip.length;
            }
            return 0f;
        }

        /// <summary>
        /// Get the current volume of the sound
        /// </summary>
        public float GetVolume()
        {
            return IsValid ? audioSource.AudioSource.volume : 0f;
        }

        /// <summary>
        /// Get the current pitch of the sound
        /// </summary>
        public float GetPitch()
        {
            return IsValid ? audioSource.AudioSource.pitch : 1f;
        }

        /// <summary>
        /// Mark this handle as invalid (called when the audio source is destroyed)
        /// </summary>
        internal void MarkAsInvalid()
        {
            isValid = false;
            audioSource = null;
        }

        private System.Collections.IEnumerator FadeVolumeCoroutine(float targetVolume, float duration)
        {
            if (!IsValid) yield break;

            float startVolume = audioSource.AudioSource.volume;
            float elapsed = 0f;

            while (elapsed < duration && IsValid)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float currentVolume = Mathf.Lerp(startVolume, targetVolume, progress);
                audioSource.SetVolume(currentVolume);
                yield return null;
            }

            if (IsValid)
            {
                audioSource.SetVolume(targetVolume);
            }
        }

        private System.Collections.IEnumerator FadeOutAndStopCoroutine(float duration)
        {
            if (!IsValid) yield break;

            float startVolume = audioSource.AudioSource.volume;
            float elapsed = 0f;

            while (elapsed < duration && IsValid)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float currentVolume = Mathf.Lerp(startVolume, 0f, progress);
                audioSource.SetVolume(currentVolume);
                yield return null;
            }

            if (IsValid)
            {
                audioSource.Stop();
            }
        }

        public override string ToString()
        {
            if (IsValid)
            {
                string clipName = audioSource.AudioSource.clip?.name ?? "Unknown";
                return $"SoundHandle({clipName}, Playing: {IsPlaying})";
            }
            return "SoundHandle(Invalid)";
        }
    }
}
