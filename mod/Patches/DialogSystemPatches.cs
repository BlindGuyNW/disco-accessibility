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
        
        /// <summary>
        /// Patch ConversationLogger.OnConversationLine to capture all dialog as it appears
        /// </summary>
        [HarmonyPatch(typeof(Il2CppSunshine.ConversationLogger), "OnConversationLine", typeof(Subtitle))]
        public static class ConversationLogger_OnConversationLine_Patch
        {
            public static void Postfix(Il2CppSunshine.ConversationLogger __instance, Subtitle subtitle)
            {
                try
                {
                    if (subtitle == null) return;
                    
                    // Check if dialog reading is enabled
                    if (!DialogStateManager.IsDialogReadingEnabled) return;
                    
                    // Try to extract speaker name and dialog text from subtitle
                    string speakerName = "";
                    string dialogText = "";
                    
                    // Use FinalEntry.GetSpeakerName method if available
                    try
                    {
                        speakerName = FinalEntry.GetSpeakerName(subtitle) ?? "";
                    }
                    catch
                    {
                        speakerName = "";
                    }
                    
                    // Try to get dialog text from subtitle
                    try
                    {
                        var subtitleObj = subtitle as Il2CppSystem.Object;
                        if (subtitleObj != null)
                        {
                            var type = subtitleObj.GetType();
                            
                            // Try to get formattedText property first
                            var formattedTextProp = type.GetProperty("formattedText");
                            if (formattedTextProp != null)
                            {
                                var formattedTextObj = formattedTextProp.GetValue(subtitleObj);
                                if (formattedTextObj != null)
                                {
                                    // FormattedText should have a 'text' property
                                    var formattedTextType = formattedTextObj.GetType();
                                    var textProp = formattedTextType.GetProperty("text");
                                    if (textProp != null)
                                    {
                                        dialogText = textProp.GetValue(formattedTextObj)?.ToString() ?? "";
                                    }
                                }
                            }
                            
                            // Fallback: try direct text property
                            if (string.IsNullOrEmpty(dialogText))
                            {
                                var directTextProp = type.GetProperty("text");
                                if (directTextProp != null)
                                {
                                    dialogText = directTextProp.GetValue(subtitleObj)?.ToString() ?? "";
                                }
                            }
                        }
                    }
                    catch
                    {
                        dialogText = "";
                    }
                    
                    // Skip if no text to speak
                    if (string.IsNullOrEmpty(dialogText)) 
                    {
                        // Log that we got a subtitle but couldn't extract text
                        MelonLogger.Msg($"[DIALOG] Got subtitle but no text. Speaker: '{speakerName}'");
                        return;
                    }
                    
                    // Format the output with speaker identification
                    string formattedDialog = FormatDialogWithSpeaker(speakerName, dialogText);
                    
                    // Check for duplicates and cooldown
                    if (formattedDialog != lastSpokenDialog || 
                        (UnityEngine.Time.time - lastDialogTime) > DIALOG_COOLDOWN)
                    {
                        // Announce the dialog with speaker
                        TolkScreenReader.Instance.Speak(formattedDialog, false);
                        
                        lastSpokenDialog = formattedDialog;
                        lastDialogTime = UnityEngine.Time.time;
                        
                        // Debug logging removed to prevent spam
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Error in ConversationLogger.OnConversationLine patch: {ex}");
                }
            }
        }
        
        /// <summary>
        /// Patch FinalEntry constructor to capture dialog entries as they're created
        /// </summary>
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
    }
}