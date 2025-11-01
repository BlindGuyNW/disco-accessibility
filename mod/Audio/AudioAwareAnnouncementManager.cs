using MelonLoader;
using UnityEngine;
using System.Collections.Generic;

namespace AccessibilityMod.Audio
{
    /// <summary>
    /// Types of announcements for selective queue management
    /// </summary>
    public enum AnnouncementSource
    {
        UI,              // UI navigation, dialogue options, continue buttons
        Dialogue,        // Dialogue text and speakers
        Notification,    // Game notifications, skill checks, task completions
        Other            // Everything else
    }

    public class AudioAwareAnnouncementManager
    {
        private static AudioAwareAnnouncementManager _instance;
        public static AudioAwareAnnouncementManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AudioAwareAnnouncementManager();
                }
                return _instance;
            }
        }

        private class QueuedAnnouncement
        {
            public string Text { get; set; }
            public bool Interrupt { get; set; }
            public float QueueTime { get; set; }
            public bool HasWaitedForAudio { get; set; }
            public AnnouncementSource Source { get; set; }
        }

        private Queue<QueuedAnnouncement> announcementQueue = new Queue<QueuedAnnouncement>();
        private float lastAudioStopTime = 0f;
        private bool wasPlayingLastFrame = false;
        private const float DELAY_AFTER_AUDIO_STOPS = 0.4f; // 400ms delay after audio stops
        private const float AUDIO_START_DELAY = 0.25f; // 250ms delay to let game start audio

        public void Update()
        {
            // Check if audio is currently playing
            bool isAudioPlaying = IsVoiceAudioPlaying();

            // Detect when audio stops
            if (wasPlayingLastFrame && !isAudioPlaying)
            {
                lastAudioStopTime = Time.time;
            }

            wasPlayingLastFrame = isAudioPlaying;

            // Process queue if audio is not playing
            if (!isAudioPlaying && announcementQueue.Count > 0)
            {
                // Check if the next item in queue has waited long enough
                var nextAnnouncement = announcementQueue.Peek();
                float timeInQueue = Time.time - nextAnnouncement.QueueTime;

                // If it hasn't waited for audio start delay yet, wait
                if (!nextAnnouncement.HasWaitedForAudio && timeInQueue < AUDIO_START_DELAY)
                {
                    return; // Wait longer
                }

                // Mark that we've waited and checked for audio
                if (!nextAnnouncement.HasWaitedForAudio)
                {
                    nextAnnouncement.HasWaitedForAudio = true;
                }

                // Also respect the delay after audio stops
                float timeSinceAudioStopped = Time.time - lastAudioStopTime;
                if (timeSinceAudioStopped >= DELAY_AFTER_AUDIO_STOPS)
                {
                    ProcessQueue();
                }
            }
        }

        private bool IsVoiceAudioPlaying()
        {
            try
            {
                // Don't cache - check fresh every time to ensure we have current references
                bool dialogSoundControllerPlaying = false;
                bool voiceClipsPlayerPlaying = false;

                // Check DialogSoundController voiceOverAudioSource
                try
                {
                    var dialogSoundController = Il2Cpp.DialogSoundController.Singleton;
                    if (dialogSoundController != null)
                    {
                        var audioSource = dialogSoundController.voiceOverAudioSource;
                        if (audioSource != null)
                        {
                            dialogSoundControllerPlaying = audioSource.isPlaying;
                        }
                    }
                }
                catch (System.Exception)
                {
                    // Silently handle errors - audio system may not be initialized yet
                }

                // Check VoiceOverClipsPlayer audioSource
                try
                {
                    var voiceClipsPlayer = Il2CppVOTool.VoiceOverClipsPlayer.Singleton;
                    if (voiceClipsPlayer != null)
                    {
                        var audioSource = voiceClipsPlayer.audioSource;
                        if (audioSource != null)
                        {
                            voiceClipsPlayerPlaying = audioSource.isPlaying;
                        }
                    }
                }
                catch (System.Exception)
                {
                    // Silently handle errors - audio system may not be initialized yet
                }

                return dialogSoundControllerPlaying || voiceClipsPlayerPlaying;
            }
            catch (System.Exception e)
            {
                MelonLogger.Warning($"[AudioAware] Error checking audio state: {e.Message}");
                return false;
            }
        }

        public void QueueAnnouncement(string text, bool interrupt = false, AnnouncementSource source = AnnouncementSource.Other)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            // Check for duplicates already in queue
            foreach (var existing in announcementQueue)
            {
                if (existing.Text == text)
                {
                    return; // Skip duplicate
                }
            }

            var announcement = new QueuedAnnouncement
            {
                Text = text,
                Interrupt = interrupt,
                QueueTime = Time.time,
                HasWaitedForAudio = false,
                Source = source
            };

            announcementQueue.Enqueue(announcement);

            // Don't process immediately - always wait AUDIO_START_DELAY first to give game time to start audio
        }

        private void ProcessQueue()
        {
            if (announcementQueue.Count == 0)
            {
                return;
            }

            // Don't process next item if screen reader is still speaking
            // This prevents queued items from interrupting each other
            if (TolkScreenReader.Instance.IsSpeaking())
            {
                return;
            }

            // Dequeue and speak the next announcement
            var announcement = announcementQueue.Dequeue();

            // Always use interrupt=false for queued items so they don't interrupt each other
            // They should speak in sequence, respecting the user's interrupt preference
            TolkScreenReader.Instance.Speak(announcement.Text, false);

            // If there are more announcements, they will be processed in subsequent Update calls
            // This prevents rapid-fire announcements and allows the screen reader to speak each one
        }

        public int GetQueueSize()
        {
            return announcementQueue.Count;
        }

        public void ClearQueue()
        {
            announcementQueue.Clear();
        }

        /// <summary>
        /// Clear only UI announcements (dialogue options, continue buttons)
        /// Keeps important notifications like skill checks and task completions
        /// </summary>
        public void ClearUIAnnouncements()
        {
            var itemsToKeep = new List<QueuedAnnouncement>();

            // Keep all non-UI announcements
            foreach (var item in announcementQueue)
            {
                if (item.Source != AnnouncementSource.UI)
                {
                    itemsToKeep.Add(item);
                }
            }

            int removedCount = announcementQueue.Count - itemsToKeep.Count;
            announcementQueue.Clear();
            foreach (var item in itemsToKeep)
            {
                announcementQueue.Enqueue(item);
            }

            if (removedCount > 0)
            {
                MelonLogger.Msg($"[AudioAware] Cleared {removedCount} UI announcement(s), kept {itemsToKeep.Count} important announcement(s)");
            }
        }

        /// <summary>
        /// Check if voice audio is currently playing
        /// This can be used by other systems to decide whether to queue announcements
        /// </summary>
        public bool IsDialogueAudioPlaying()
        {
            return IsVoiceAudioPlaying();
        }
    }
}
