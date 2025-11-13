using System;
using System.Linq;
using HarmonyLib;
using Il2Cpp;
using UnityEngine;
using MelonLoader;
using Il2CppTMPro;
using AccessibilityMod.Settings;

namespace AccessibilityMod.Patches
{
    [HarmonyPatch]
    public static class OrbTextVocalizationPatches
    {
        private static bool orbAnnouncementsEnabled = true;
        private static string lastAnnouncedOrbText = "";
        private static float lastOrbAnnouncementTime = 0f;
        private const float ORB_COOLDOWN = 0.5f; // 500ms cooldown to prevent duplicate announcements

        static OrbTextVocalizationPatches()
        {
            // Load initial setting from preferences
            orbAnnouncementsEnabled = AccessibilityPreferences.GetOrbAnnouncements();
        }

        public static void ToggleOrbAnnouncements()
        {
            orbAnnouncementsEnabled = !orbAnnouncementsEnabled;
            string status = orbAnnouncementsEnabled ? "enabled" : "disabled";
            TolkScreenReader.Instance.Speak($"Orb announcements {status}", true);
            MelonLogger.Msg($"[ORB TOGGLE] Orb announcements {status}");

            // Save the new setting
            AccessibilityPreferences.SetOrbAnnouncements(orbAnnouncementsEnabled);
        }

        /// <summary>
        /// Helper method to announce orb text with duplicate detection
        /// </summary>
        private static void AnnounceOrbText(string text, string prefix = "Orb text")
        {
            if (!orbAnnouncementsEnabled || string.IsNullOrEmpty(text))
                return;

            string trimmedText = text.Trim();
            float currentTime = Time.time;

            // Check if this is a duplicate within the cooldown period
            if (trimmedText == lastAnnouncedOrbText &&
                (currentTime - lastOrbAnnouncementTime) < ORB_COOLDOWN)
            {
                return;
            }

            // Update tracking
            lastAnnouncedOrbText = trimmedText;
            lastOrbAnnouncementTime = currentTime;

            // Announce the text
            TolkScreenReader.Instance.Speak($"{prefix}: {trimmedText}", true, AnnouncementCategory.Queueable);
        }
        /// <summary>
        /// Patch for FloatFactory.ShowFloat(string, Transform) to vocalize text
        /// </summary>
        [HarmonyPatch(typeof(Il2Cpp.FloatFactory), "ShowFloat", new System.Type[] { typeof(string), typeof(Transform) })]
        [HarmonyPostfix]
        public static void FloatFactory_ShowFloat_TwoParam_Postfix(string text, Transform target)
        {
            try
            {
                AnnounceOrbText(text);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in FloatFactory ShowFloat (2-param) patch: {ex}");
            }
        }

        /// <summary>
        /// Patch for FloatFactory.ShowFloat(string, Transform, Vector3, float) to vocalize text
        /// </summary>
        [HarmonyPatch(typeof(Il2Cpp.FloatFactory), "ShowFloat", new System.Type[] { typeof(string), typeof(Transform), typeof(Vector3), typeof(float) })]
        [HarmonyPostfix]
        public static void FloatFactory_ShowFloat_FourParam_Postfix(string text, Transform target, Vector3 offset, float time)
        {
            try
            {
                AnnounceOrbText(text);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in FloatFactory ShowFloat (4-param) patch: {ex}");
            }
        }

        /// <summary>
        /// Patch for FloatFactory.ShowLocalizedFloat(string, string, Transform) to vocalize localized text
        /// </summary>
        [HarmonyPatch(typeof(Il2Cpp.FloatFactory), "ShowLocalizedFloat", new System.Type[] { typeof(string), typeof(string), typeof(Transform) })]
        [HarmonyPostfix]
        public static void FloatFactory_ShowLocalizedFloat_ThreeParam_Postfix(string term, string fallbackText, Transform target, Il2Cpp.FloatTemplate __result)
        {
            try
            {
                if (__result != null)
                {
                    string displayedText = __result.text;
                    if (!string.IsNullOrEmpty(displayedText))
                    {
                        AnnounceOrbText(displayedText);
                    }
                    else if (!string.IsNullOrEmpty(fallbackText))
                    {
                        AnnounceOrbText(fallbackText);
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in FloatFactory ShowLocalizedFloat (3-param) patch: {ex}");
            }
        }

        /// <summary>
        /// Patch for FloatFactory.ShowLocalizedFloat(string, string, Transform, Vector3, float) to vocalize localized text
        /// </summary>
        [HarmonyPatch(typeof(Il2Cpp.FloatFactory), "ShowLocalizedFloat", new System.Type[] { typeof(string), typeof(string), typeof(Transform), typeof(Vector3), typeof(float) })]
        [HarmonyPostfix]
        public static void FloatFactory_ShowLocalizedFloat_FiveParam_Postfix(string term, string fallbackText, Transform target, Vector3 offset, float time, Il2Cpp.FloatTemplate __result)
        {
            try
            {
                if (__result != null)
                {
                    string displayedText = __result.text;
                    if (!string.IsNullOrEmpty(displayedText))
                    {
                        AnnounceOrbText(displayedText);
                    }
                    else if (!string.IsNullOrEmpty(fallbackText))
                    {
                        AnnounceOrbText(fallbackText);
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in FloatFactory ShowLocalizedFloat (5-param) patch: {ex}");
            }
        }

        /// <summary>
        /// Alternative approach: Patch FloatTemplate.set_text to catch when text is actually set
        /// </summary>
        [HarmonyPatch(typeof(Il2Cpp.FloatTemplate), "set_text")]
        [HarmonyPostfix]
        public static void FloatTemplate_SetText_Postfix(Il2Cpp.FloatTemplate __instance, string value)
        {
            try
            {
                AnnounceOrbText(value, "Float text");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in FloatTemplate set_text patch: {ex}");
            }
        }
    }
}