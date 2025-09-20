using System;
using HarmonyLib;
using MelonLoader;
using Il2CppSunshine.Journal;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AccessibilityMod.Patches
{
    /// <summary>
    /// Harmony patches for map and fast travel accessibility
    /// </summary>

    // Patch to allow fast travel from anywhere - bypass location restrictions
    [HarmonyPatch(typeof(QuicktravelController), "IsQuicktravelAvailable")]
    public static class QuicktravelController_IsQuicktravelAvailable_Patch
    {
        public static bool Prefix(ref bool __result)
        {
            // Always allow quicktravel for accessibility
            __result = true;
            return false; // Skip original method
        }
    }

    // Patch to allow fast travel even when not at a valid location
    [HarmonyPatch(typeof(QuicktravelController), "IsWithingQuicktravelRange")]
    public static class QuicktravelController_IsWithingQuicktravelRange_Patch
    {
        public static bool Prefix(ref bool __result)
        {
            // Always return true to allow travel from anywhere
            __result = true;
            return false; // Skip original method
        }
    }

    // Patch for QuicktravelButton selection to announce proper location names
    [HarmonyPatch(typeof(QuicktravelButton), "OnSelect")]
    public static class QuicktravelButton_OnSelect_Patch
    {
        private static QuicktravelButton lastSelectedButton = null;
        private static float lastSelectionTime = 0f;
        private const float DEBOUNCE_INTERVAL = 0.5f; // Prevent repeated announcements within 500ms

        public static void Postfix(QuicktravelButton __instance, BaseEventData eventData)
        {
            try
            {
                if (__instance == null) return;

                // Debounce: Skip if same button was selected very recently
                float currentTime = UnityEngine.Time.time;
                if (lastSelectedButton == __instance && currentTime - lastSelectionTime < DEBOUNCE_INTERVAL)
                {
                    MelonLogger.Msg($"[Map] Skipping duplicate fast travel selection within {DEBOUNCE_INTERVAL}s");
                    return;
                }

                lastSelectedButton = __instance;
                lastSelectionTime = currentTime;

                // Get the location marker name which should identify the location
                string locationMarker = __instance.locationMarker;
                string locationName = GetFriendlyLocationName(locationMarker);

                // Check if player is at this location
                bool isCurrentLocation = __instance.CheckTequilaInActivationRadius();

                string announcement;
                if (isCurrentLocation)
                {
                    announcement = $"You are here: {locationName}";
                }
                else
                {
                    announcement = $"Travel to {locationName}";
                }

                MelonLogger.Msg($"[Map] Fast travel button selected: {announcement}");
                TolkScreenReader.Instance.Speak(announcement, false);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in QuicktravelButton_OnSelect_Patch: {ex}");
            }
        }

        /// <summary>
        /// Convert location marker IDs to friendly names
        /// </summary>
        internal static string GetFriendlyLocationName(string locationMarker)
        {
            if (string.IsNullOrEmpty(locationMarker))
                return "Unknown location";

            // Clean up the location marker name
            string cleanName = locationMarker.ToLower().Trim();

            // Map the three fast travel locations in Disco Elysium
            // "main" is likely the marker for Whirling-In-Rags (the main starting area)
            if (cleanName == "main" || cleanName.Contains("whirling") || cleanName.Contains("cafeteria") || cleanName.Contains("hotel"))
                return "Whirling-In-Rags";
            else if (cleanName.Contains("church") || cleanName.Contains("dolorian"))
                return "Dolorian Church";
            else if (cleanName.Contains("village") || cleanName.Contains("fishing"))
                return "Fishing Village";

            // Fallback: clean up the marker name for display
            string friendlyName = locationMarker.Replace("_", " ").Replace("-", " ");

            // Capitalize first letter of each word
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            bool capitalizeNext = true;
            foreach (char c in friendlyName)
            {
                if (char.IsWhiteSpace(c))
                {
                    sb.Append(c);
                    capitalizeNext = true;
                }
                else if (capitalizeNext)
                {
                    sb.Append(char.ToUpper(c));
                    capitalizeNext = false;
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }

    // Patch for hovering over fast travel buttons (mouse users)
    [HarmonyPatch(typeof(QuicktravelButton), "OnPointerEnter")]
    public static class QuicktravelButton_OnPointerEnter_Patch
    {
        public static void Postfix(QuicktravelButton __instance, PointerEventData eventData)
        {
            try
            {
                if (__instance == null) return;

                // Get the location marker name
                string locationMarker = __instance.locationMarker;
                string locationName = QuicktravelButton_OnSelect_Patch.GetFriendlyLocationName(locationMarker);

                // Check if player is at this location
                bool isCurrentLocation = __instance.CheckTequilaInActivationRadius();

                string announcement;
                if (isCurrentLocation)
                {
                    announcement = $"You are here: {locationName}";
                }
                else
                {
                    announcement = $"Travel to {locationName}";
                }

                MelonLogger.Msg($"[Map] Fast travel button hovered: {announcement}");
                TolkScreenReader.Instance.Speak(announcement, false);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in QuicktravelButton_OnPointerEnter_Patch: {ex}");
            }
        }
    }
}