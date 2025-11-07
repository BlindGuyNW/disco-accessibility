using System;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AccessibilityMod.Patches
{
    /// <summary>
    /// Patches for signature skill selection screen accessibility
    /// </summary>
    public class SignatureSkillSelectionPatches
    {
        private static bool hasAnnouncedHelpMessage = false;
        private static float lastResetTime = -10f;
        private static readonly float RESET_TIMEOUT = 30f; // Reset flag after 30 seconds

        /// <summary>
        /// Patch SkillPortraitSelection.OnSelect to announce help message on first selection
        /// </summary>
        [HarmonyPatch(typeof(SkillPortraitSelection), nameof(SkillPortraitSelection.OnSelect))]
        public static class SkillPortraitSelection_OnSelect_Patch
        {
            public static void Postfix(SkillPortraitSelection __instance, BaseEventData eventData)
            {
                try
                {
                    // Check if we're in character creation context
                    if (!IsInCharacterCreation())
                    {
                        return;
                    }

                    // Reset the flag if enough time has passed (user might have returned to this screen)
                    float currentTime = Time.time;
                    if (currentTime - lastResetTime > RESET_TIMEOUT)
                    {
                        hasAnnouncedHelpMessage = false;
                    }

                    // Announce help message only once per session at this screen
                    if (!hasAnnouncedHelpMessage)
                    {
                        hasAnnouncedHelpMessage = true;
                        lastResetTime = currentTime;

                        string helpMessage = "Choose a signature skill, then press X or Square on your controller to continue.";
                        TolkScreenReader.Instance.Speak(helpMessage, true);
                        MelonLogger.Msg($"[SIGNATURE SKILL] Announced help message");
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"[SIGNATURE SKILL] Error in OnSelect patch: {ex}");
                }
            }
        }

        /// <summary>
        /// Check if we're in character creation (not gameplay)
        /// </summary>
        private static bool IsInCharacterCreation()
        {
            try
            {
                // World.RunningOrCollage is true during gameplay, false during character creation
                return !World.RunningOrCollage;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[SIGNATURE SKILL] Error checking context: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reset the help message flag (called when leaving character creation)
        /// </summary>
        public static void ResetHelpMessageFlag()
        {
            hasAnnouncedHelpMessage = false;
        }
    }
}
