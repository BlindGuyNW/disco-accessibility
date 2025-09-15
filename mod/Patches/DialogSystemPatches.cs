using System;
using HarmonyLib;
using Il2Cpp;
using Il2CppPixelCrushers.DialogueSystem;
using Il2CppSunshine;
using MelonLoader;
using AccessibilityMod.UI;

namespace AccessibilityMod.Patches
{
    /// <summary>
    /// Patches to hook into the dialog system for comprehensive dialog reading with speaker identification
    /// </summary>
    public static class DialogSystemPatches
    {
        // Track the last spoken entry to avoid duplicates
        private static string lastSpokenDialog = "";
        private static float lastDialogTime = 0f;
        private static readonly float DIALOG_COOLDOWN = 0.5f; // 500ms cooldown to prevent spam

        // Track the last speaker for speaker-only mode
        private static string lastAnnouncedSpeaker = "";
        private static float lastSpeakerTime = 0f;
        private static readonly float SPEAKER_COOLDOWN = 1.0f; // 1 second cooldown for same speaker
        
        /// <summary>
        /// Patch LogRenderer.AddToLog to capture localized dialog text as it's rendered to the UI
        /// </summary>
        [HarmonyPatch(typeof(LogRenderer), "AddToLog", typeof(FinalEntry))]
        public static class LogRenderer_AddToLog_Patch
        {
            public static void Postfix(LogRenderer __instance, FinalEntry entry)
            {
                try
                {
                    if (entry == null) return;
                    
                    // Check if any dialog reading mode is enabled
                    if (!DialogStateManager.IsDialogReadingEnabled) return;

                    // Get localized dialog text and speaker name from FinalEntry
                    string dialogText = entry.spokenLine ?? "";
                    string speakerName = entry.speakerName ?? "";

                    // Skip if no text to speak
                    if (string.IsNullOrEmpty(dialogText))
                    {
                        MelonLogger.Msg($"[DIALOG] Got FinalEntry but no spokenLine. Speaker: '{speakerName}'");
                        return;
                    }

                    // Handle different dialog modes
                    if (DialogStateManager.IsSpeakerOnlyMode)
                    {
                        // Speaker-only mode: Just announce who's speaking (but skip "You")
                        if (!string.IsNullOrEmpty(speakerName))
                        {
                            string cleanSpeaker = CleanSpeakerName(speakerName);

                            // Skip announcing "You" in speaker-only mode
                            if (cleanSpeaker.Equals("You", StringComparison.OrdinalIgnoreCase))
                            {
                                return;
                            }

                            // Only announce if it's a different speaker or enough time has passed
                            if (cleanSpeaker != lastAnnouncedSpeaker ||
                                (UnityEngine.Time.time - lastSpeakerTime) > SPEAKER_COOLDOWN)
                            {
                                string speakerAnnouncement = FormatSpeakerOnly(cleanSpeaker);
                                TolkScreenReader.Instance.Speak(speakerAnnouncement, false);

                                lastAnnouncedSpeaker = cleanSpeaker;
                                lastSpeakerTime = UnityEngine.Time.time;
                            }
                        }
                    }
                    else if (DialogStateManager.ShouldReadFullDialog)
                    {
                        // Full dialog mode: Read everything
                        string formattedDialog = FormatDialogWithSpeaker(speakerName, dialogText);

                        // Check for duplicates and cooldown
                        if (formattedDialog != lastSpokenDialog ||
                            (UnityEngine.Time.time - lastDialogTime) > DIALOG_COOLDOWN)
                        {
                            // Announce the localized dialog with speaker
                            TolkScreenReader.Instance.Speak(formattedDialog, false);

                            lastSpokenDialog = formattedDialog;
                            lastDialogTime = UnityEngine.Time.time;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Error in LogRenderer.AddToLog patch: {ex}");
                }
            }
        }
        
        /// <summary>
        /// Patch FinalEntry constructor to capture dialog entries as they're created
        /// COMMENTED OUT: This patch fails to find the correct constructor signature
        /// </summary>
        /*
        [HarmonyPatch(typeof(FinalEntry), ".ctor", typeof(DialogueEntry), typeof(string), typeof(string))]
        public static class FinalEntry_Constructor_Patch
        {
            public static void Postfix(FinalEntry __instance, DialogueEntry entry, string overrideSpeakerName, string overrideText)
            {
                try
                {
                    if (!DialogStateManager.IsDialogReadingEnabled) return;
                    if (__instance == null) return;
                    
                    // Update DialogStateManager with the new entry
                    DialogStateManager.OnNewDialogEntry(__instance);
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Error in FinalEntry constructor patch: {ex}");
                }
            }
        }
        */
        
        /// <summary>
        /// Format dialog text with speaker identification
        /// </summary>
        private static string FormatDialogWithSpeaker(string speakerName, string dialogText)
        {
            if (string.IsNullOrEmpty(speakerName))
            {
                return dialogText;
            }
            
            // Clean up and format speaker name
            string cleanSpeaker = CleanSpeakerName(speakerName);
            
            // Identify speaker type and format accordingly
            if (IsSkillName(cleanSpeaker))
            {
                // Skills get special formatting
                return $"{cleanSpeaker} skill: {dialogText}";
            }
            else if (IsNarrative(cleanSpeaker))
            {
                // Narrative text
                return $"Narrative: {dialogText}";
            }
            else
            {
                // NPCs and other speakers
                return $"{cleanSpeaker}: {dialogText}";
            }
        }
        
        /// <summary>
        /// Clean up speaker names for better pronunciation
        /// </summary>
        private static string CleanSpeakerName(string speakerName)
        {
            if (string.IsNullOrEmpty(speakerName)) return "Unknown";
            
            // Remove any formatting tags
            speakerName = speakerName.Replace("_", " ");
            
            // Handle special cases
            if (speakerName.Equals("You", StringComparison.OrdinalIgnoreCase))
            {
                return "You";
            }
            
            // Only treat truly generic narrator text as "Narrative"
            if (speakerName.Equals("Narrator", StringComparison.OrdinalIgnoreCase))
            {
                return "Narrative";
            }
            
            return speakerName;
        }
        
        /// <summary>
        /// Check if the speaker is a skill
        /// </summary>
        private static bool IsSkillName(string speakerName)
        {
            string[] skillNames = {
                "Logic", "Encyclopedia", "Rhetoric", "Drama", "Conceptualization", "Visual Calculus",
                "Volition", "Inland Empire", "Empathy", "Authority", "Suggestion", "Esprit de Corps",
                "Physical Instrument", "Electrochemistry", "Endurance", "Half Light", "Pain Threshold", "Shivers",
                "Hand Eye Coordination", "Perception", "Reaction Speed", "Savoir Faire", "Interfacing", "Composure"
            };
            
            foreach (string skill in skillNames)
            {
                if (speakerName.IndexOf(skill, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Check if the speaker is narrative/description
        /// </summary>
        private static bool IsNarrative(string speakerName)
        {
            return speakerName.Equals("Narrative", StringComparison.OrdinalIgnoreCase) ||
                   speakerName.Equals("Narrator", StringComparison.OrdinalIgnoreCase) ||
                   string.IsNullOrEmpty(speakerName);
        }

        /// <summary>
        /// Format speaker name for speaker-only mode
        /// </summary>
        private static string FormatSpeakerOnly(string speakerName)
        {
            if (IsSkillName(speakerName))
            {
                return $"{speakerName} skill";
            }
            else if (IsNarrative(speakerName))
            {
                return "Narrative";
            }
            else if (speakerName.Equals("You", StringComparison.OrdinalIgnoreCase))
            {
                return "You";
            }
            else
            {
                return speakerName;
            }
        }
    }
}