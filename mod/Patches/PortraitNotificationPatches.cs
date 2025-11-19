using System;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;

namespace AccessibilityMod.Patches
{
    /// <summary>
    /// Patches to announce when Kim Kitsuragi has new dialogue available
    /// </summary>
    public static class PortraitNotificationPatches
    {
        // Track notification state to prevent duplicate announcements
        private static bool kimDialogueActive = false;

        /// <summary>
        /// Gets whether Kim currently has dialogue available
        /// </summary>
        public static bool IsKimDialogueAvailable()
        {
            try
            {
                var manager = UnityEngine.Object.FindObjectOfType<PortraitNotificationManager>();
                if (manager != null && manager.kimTalkNotification != null)
                {
                    return manager.kimTalkNotification.isActive;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[KIM] Error checking Kim dialogue status: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Patch PortraitNotification.Show to announce when Kim has new dialogue available
        /// </summary>
        [HarmonyPatch(typeof(PortraitNotification), nameof(PortraitNotification.Show))]
        public static class PortraitNotification_Show_Patch
        {
            public static void Postfix(PortraitNotification __instance)
            {
                try
                {
                    // Find the PortraitNotificationManager to compare instances
                    var manager = UnityEngine.Object.FindObjectOfType<PortraitNotificationManager>();
                    if (manager == null) return;

                    // Check if this is Kim's dialogue notification
                    if (manager.kimTalkNotification != null && __instance.Pointer == manager.kimTalkNotification.Pointer)
                    {
                        if (!kimDialogueActive)
                        {
                            kimDialogueActive = true;
                            TolkScreenReader.Instance.Speak("Kim has new dialogue available", true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"[KIM] Error in Show patch: {ex}");
                }
            }
        }

        /// <summary>
        /// Patch PortraitNotification.Hide to track when Kim's notification is dismissed
        /// </summary>
        [HarmonyPatch(typeof(PortraitNotification), nameof(PortraitNotification.Hide))]
        public static class PortraitNotification_Hide_Patch
        {
            public static void Postfix(PortraitNotification __instance)
            {
                try
                {
                    // Find the PortraitNotificationManager to compare instances
                    var manager = UnityEngine.Object.FindObjectOfType<PortraitNotificationManager>();
                    if (manager == null) return;

                    // Check if this is Kim's dialogue notification being hidden
                    if (manager.kimTalkNotification != null && __instance.Pointer == manager.kimTalkNotification.Pointer)
                    {
                        kimDialogueActive = false;
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"[KIM] Error in Hide patch: {ex}");
                }
            }
        }
    }
}
