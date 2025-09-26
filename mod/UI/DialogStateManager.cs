using System;
using System.Collections.Generic;
using Il2Cpp;
using Il2CppPixelCrushers.DialogueSystem;
using MelonLoader;
using UnityEngine;
using AccessibilityMod.Settings;

namespace AccessibilityMod.UI
{
    /// <summary>
    /// Dialog reading modes
    /// </summary>
    public enum DialogReadingMode
    {
        Disabled,     // No dialog reading
        Full,         // Read speaker and dialog text
        SpeakerOnly   // Only announce speaker names
    }

    /// <summary>
    /// Manages dialog state, tracking conversations and speaker identification
    /// </summary>
    public static class DialogStateManager
    {
        // Current dialog reading mode - loaded from settings
        private static DialogReadingMode dialogReadingMode = DialogReadingMode.Disabled;
        
        // Track recent dialog to avoid duplicates
        private static Queue<string> recentDialogQueue = new Queue<string>();
        private static readonly int MAX_RECENT_ENTRIES = 10;
        
        // Track current conversation state
        private static string currentSpeaker = "";
        private static bool isInConversation = false;
        
        // Track response options separately
        private static List<string> currentResponses = new List<string>();
        private static int selectedResponseIndex = -1;
        
        static DialogStateManager()
        {
            // Load initial dialog mode from preferences
            dialogReadingMode = AccessibilityPreferences.GetDialogMode();
        }

        /// <summary>
        /// Gets whether dialog reading mode is enabled (Full or SpeakerOnly)
        /// </summary>
        public static bool IsDialogReadingEnabled => dialogReadingMode != DialogReadingMode.Disabled;

        /// <summary>
        /// Gets the current dialog reading mode
        /// </summary>
        public static DialogReadingMode CurrentDialogMode => dialogReadingMode;

        /// <summary>
        /// Gets whether we should read full dialog (not just speaker)
        /// </summary>
        public static bool ShouldReadFullDialog => dialogReadingMode == DialogReadingMode.Full;

        /// <summary>
        /// Gets whether we should only announce speakers
        /// </summary>
        public static bool IsSpeakerOnlyMode => dialogReadingMode == DialogReadingMode.SpeakerOnly;
        
        /// <summary>
        /// Toggle dialog reading mode between Disabled -> Full -> SpeakerOnly -> Disabled
        /// </summary>
        public static void ToggleDialogReading()
        {
            // Cycle through the modes
            switch (dialogReadingMode)
            {
                case DialogReadingMode.Disabled:
                    dialogReadingMode = DialogReadingMode.Full;
                    break;
                case DialogReadingMode.Full:
                    dialogReadingMode = DialogReadingMode.SpeakerOnly;
                    break;
                case DialogReadingMode.SpeakerOnly:
                    dialogReadingMode = DialogReadingMode.Disabled;
                    break;
            }

            // Announce the new mode
            string announcement = dialogReadingMode switch
            {
                DialogReadingMode.Disabled => "Dialog reading disabled",
                DialogReadingMode.Full => "Dialog reading enabled: Full dialog with speakers",
                DialogReadingMode.SpeakerOnly => "Dialog reading enabled: Speaker names only",
                _ => "Dialog reading mode changed"
            };

            TolkScreenReader.Instance.Speak(announcement, true);
            MelonLogger.Msg($"[DIALOG] Reading mode changed to: {dialogReadingMode}");

            // Save the new setting
            AccessibilityPreferences.SetDialogMode(dialogReadingMode);
        }
        
        /// <summary>
        /// Handle a new dialog entry
        /// </summary>
        public static void OnNewDialogEntry(FinalEntry entry)
        {
            try
            {
                if (entry == null) return;
                
                // Mark that we're in a conversation
                isInConversation = true;
                
                // Extract and store speaker information
                if (!string.IsNullOrEmpty(entry.speakerName))
                {
                    currentSpeaker = entry.speakerName;
                }
                
                // Track the dialog for duplicate detection
                string dialogKey = $"{entry.speakerName}:{entry.spokenLine}";
                if (!IsRecentDialog(dialogKey))
                {
                    AddToRecentDialog(dialogKey);
                    
                    // Log for debugging
                    MelonLogger.Msg($"[DIALOG-STATE] New entry - Speaker: {entry.speakerName}, Has Check: {entry.HasCheck}, Only Check: {entry.OnlyCheck}");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error handling new dialog entry: {ex}");
            }
        }
        
        /// <summary>
        /// Handle new response options appearing
        /// </summary>
        public static void OnResponsesUpdated(List<string> responses)
        {
            try
            {
                currentResponses.Clear();
                if (responses != null)
                {
                    currentResponses.AddRange(responses);
                }
                
                // Log single responses but don't announce them here - UINavigationHandler will handle it
                if (currentResponses.Count == 1)
                {
                    MelonLogger.Msg($"[DIALOG-STATE] Single response available: {currentResponses[0]}");
                }
                else if (currentResponses.Count > 1)
                {
                    MelonLogger.Msg($"[DIALOG-STATE] Multiple responses available: {currentResponses.Count}");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error updating responses: {ex}");
            }
        }
        
        /// <summary>
        /// Handle response selection
        /// </summary>
        public static void OnResponseSelected(int index)
        {
            try
            {
                selectedResponseIndex = index;
                
                if (index >= 0 && index < currentResponses.Count)
                {
                    string selectedResponse = currentResponses[index];
                    // Debug logging removed
                    
                    // If this is a single response that wasn't clearly announced, announce it now
                    if (currentResponses.Count == 1 && !IsDialogReadingEnabled)
                    {
                        // This helps catch those single "Continue" options
                        TolkScreenReader.Instance.Speak($"Selected: {selectedResponse}", true);
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error handling response selection: {ex}");
            }
        }
        
        /// <summary>
        /// Clear conversation state when dialog ends
        /// </summary>
        public static void OnConversationEnd()
        {
            isInConversation = false;
            currentSpeaker = "";
            currentResponses.Clear();
            selectedResponseIndex = -1;
            
            MelonLogger.Msg("[DIALOG-STATE] Conversation ended");
        }
        
        /// <summary>
        /// Check if dialog was recently spoken (to avoid duplicates)
        /// </summary>
        private static bool IsRecentDialog(string dialogKey)
        {
            return recentDialogQueue.Contains(dialogKey);
        }
        
        /// <summary>
        /// Add dialog to recent history
        /// </summary>
        private static void AddToRecentDialog(string dialogKey)
        {
            recentDialogQueue.Enqueue(dialogKey);
            
            // Keep queue size limited
            while (recentDialogQueue.Count > MAX_RECENT_ENTRIES)
            {
                recentDialogQueue.Dequeue();
            }
        }
        
        /// <summary>
        /// Get formatted speaker name for current conversation
        /// </summary>
        public static string GetCurrentSpeaker()
        {
            return currentSpeaker;
        }
        
        /// <summary>
        /// Check if we're currently in a conversation
        /// </summary>
        public static bool IsInConversation()
        {
            return isInConversation;
        }
        
        /// <summary>
        /// Force announce current responses (useful for debugging)
        /// </summary>
        public static void AnnounceCurrentResponses()
        {
            if (currentResponses.Count == 0)
            {
                TolkScreenReader.Instance.Speak("No response options available", true);
            }
            else if (currentResponses.Count == 1)
            {
                TolkScreenReader.Instance.Speak($"Single response: {currentResponses[0]}", true);
            }
            else
            {
                TolkScreenReader.Instance.Speak($"{currentResponses.Count} response options available", true);
            }
        }
    }
}