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
        /// Patch for FloatFactory.ShowFloat(string, Transform) to vocalize text
        /// </summary>
        [HarmonyPatch(typeof(Il2Cpp.FloatFactory), "ShowFloat", new System.Type[] { typeof(string), typeof(Transform) })]
        [HarmonyPostfix]
        public static void FloatFactory_ShowFloat_TwoParam_Postfix(string text, Transform target)
        {
            try
            {
                if (!orbAnnouncementsEnabled) return;

                if (!string.IsNullOrEmpty(text))
                {
                    TolkScreenReader.Instance.Speak($"Orb text: {text.Trim()}", true, AnnouncementCategory.Queueable);
                }
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
                if (!orbAnnouncementsEnabled) return;

                if (!string.IsNullOrEmpty(text))
                {
                    TolkScreenReader.Instance.Speak($"Orb text: {text.Trim()}", true, AnnouncementCategory.Queueable);
                }
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
                if (!orbAnnouncementsEnabled) return;

                if (__result != null)
                {
                    string displayedText = __result.text;
                    if (!string.IsNullOrEmpty(displayedText))
                    {
                        TolkScreenReader.Instance.Speak($"Orb text: {displayedText.Trim()}", true, AnnouncementCategory.Queueable);
                    }
                    else if (!string.IsNullOrEmpty(fallbackText))
                    {
                        TolkScreenReader.Instance.Speak($"Orb text: {fallbackText.Trim()}", true, AnnouncementCategory.Queueable);
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
                if (!orbAnnouncementsEnabled) return;

                if (__result != null)
                {
                    string displayedText = __result.text;
                    if (!string.IsNullOrEmpty(displayedText))
                    {
                        TolkScreenReader.Instance.Speak($"Orb text: {displayedText.Trim()}", true, AnnouncementCategory.Queueable);
                    }
                    else if (!string.IsNullOrEmpty(fallbackText))
                    {
                        TolkScreenReader.Instance.Speak($"Orb text: {fallbackText.Trim()}", true, AnnouncementCategory.Queueable);
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
                if (!orbAnnouncementsEnabled) return;

                if (!string.IsNullOrEmpty(value))
                {
                    TolkScreenReader.Instance.Speak($"Float text: {value.Trim()}", true, AnnouncementCategory.Queueable);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in FloatTemplate set_text patch: {ex}");
            }
        }
    }
}