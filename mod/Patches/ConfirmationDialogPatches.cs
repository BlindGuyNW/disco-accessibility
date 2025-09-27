using System;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using AccessibilityMod.UI;

namespace AccessibilityMod.Patches
{
    /// <summary>
    /// Patches to make confirmation dialogs accessible with screen readers
    /// </summary>
    public class ConfirmationDialogPatches
    {
        // Debouncing fields
        private static float lastAnnouncementTime = -10f; // Start with negative time to avoid false positives
        private static string lastAnnouncedText = "";
        private static readonly float DEBOUNCE_INTERVAL = 2.0f; // Don't re-announce same dialog within 2 seconds

        /// <summary>
        /// Check if the game is in a state where we should announce dialogs
        /// </summary>
        private static bool ShouldAnnounceDialog(string dialogText = null)
        {
            try
            {
                // Check for debouncing - don't announce same text twice in quick succession
                if (!string.IsNullOrEmpty(dialogText))
                {
                    float currentTime = Time.time;
                    if (dialogText == lastAnnouncedText &&
                        (currentTime - lastAnnouncementTime) < DEBOUNCE_INTERVAL)
                    {
                        return false; // Same dialog announced recently, skip
                    }
                }

                // Check if we're in a loading screen - block during loading
                var loadScreen = UnityEngine.Object.FindObjectOfType<Il2CppSunshine.LoadScreenOverride>();
                if (loadScreen != null && loadScreen.isActiveAndEnabled)
                {
                    return false; // Don't announce during loading screens
                }

                // We've passed debouncing and loading checks, allow the announcement
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error checking game state: {ex}");
                // If we can't determine state, allow it (better to announce than miss important dialogs)
                return true;
            }
        }

        /// <summary>
        /// Record that we announced a dialog for debouncing
        /// </summary>
        private static void RecordAnnouncement(string text)
        {
            lastAnnouncedText = text;
            lastAnnouncementTime = Time.time;
        }
        // DISABLED - ShowLocalizedConfirmation seems to call this internally, causing duplicates
        /*
        /// <summary>
        /// Patch ShowConfirmation to announce when confirmation dialogs appear
        /// </summary>
        [HarmonyPatch(typeof(Il2Cpp.ConfirmationController), nameof(Il2Cpp.ConfirmationController.ShowConfirmation))]
        public static class ConfirmationController_ShowConfirmation_Patch
        {
            public static void Postfix(Il2Cpp.ConfirmationController __instance, string text, bool showCancel)
            {
                try
                {
                    // Check if we should announce
                    if (!ShouldAnnounceDialog(text))
                    {
                        return;
                    }

                    // Announce the confirmation dialog content
                    if (!string.IsNullOrEmpty(text))
                    {
                        string announcement = $"Confirmation dialog: {text}";

                        // Add button information
                        if (showCancel)
                        {
                            announcement += ". Press Enter to confirm or Escape to cancel.";
                        }
                        else
                        {
                            announcement += ". Press Enter to confirm.";
                        }

                        TolkScreenReader.Instance.Speak(announcement, true);
                        RecordAnnouncement(text);

                        // Also announce button labels if they exist
                        AnnounceButtonLabels(__instance, showCancel);
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Error in ShowConfirmation patch: {ex}");
                }
            }
        }
        */

        /// <summary>
        /// Patch ShowLocalizedConfirmation to announce localized confirmation dialogs
        /// </summary>
        [HarmonyPatch(typeof(Il2Cpp.ConfirmationController), nameof(Il2Cpp.ConfirmationController.ShowLocalizedConfirmation))]
        public static class ConfirmationController_ShowLocalizedConfirmation_Patch
        {
            public static void Postfix(Il2Cpp.ConfirmationController __instance, string localizationTerm, bool showCancel)
            {
                try
                {
                    // Try to get the localized text from the Text component
                    if (__instance.Text != null && !string.IsNullOrEmpty(__instance.Text.text))
                    {
                        // Check if we should announce
                        if (!ShouldAnnounceDialog(__instance.Text.text))
                        {
                            return;
                        }

                        string announcement = $"Confirmation dialog: {__instance.Text.text}";

                        TolkScreenReader.Instance.Speak(announcement, true);
                        RecordAnnouncement(__instance.Text.text);

                        // Also announce button labels
                        AnnounceButtonLabels(__instance, showCancel);
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Error in ShowLocalizedConfirmation patch: {ex}");
                }
            }
        }

        // DISABLED - ShowLocalizedConfirmation should catch these too
        /*
        /// <summary>
        /// Patch ShowLocalizedConfirmationWithFormat to announce formatted confirmation dialogs
        /// </summary>
        [HarmonyPatch(typeof(Il2Cpp.ConfirmationController), nameof(Il2Cpp.ConfirmationController.ShowLocalizedConfirmationWithFormat))]
        public static class ConfirmationController_ShowLocalizedConfirmationWithFormat_Patch
        {
            public static void Postfix(Il2Cpp.ConfirmationController __instance, string localizationFormatTerm, string textToApply, bool showCancel)
            {
                try
                {
                    // Try to get the formatted text from the Text component
                    if (__instance.Text != null && !string.IsNullOrEmpty(__instance.Text.text))
                    {
                        // Check if we should announce
                        if (!ShouldAnnounceDialog(__instance.Text.text))
                        {
                            return;
                        }

                        string announcement = $"Confirmation dialog: {__instance.Text.text}";

                        // Add button information
                        if (showCancel)
                        {
                            announcement += ". Press Enter to confirm or Escape to cancel.";
                        }
                        else
                        {
                            announcement += ". Press Enter to confirm.";
                        }

                        TolkScreenReader.Instance.Speak(announcement, true);
                        RecordAnnouncement(__instance.Text.text);

                        // Also announce button labels
                        AnnounceButtonLabels(__instance, showCancel);
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Error in ShowLocalizedConfirmationWithFormat patch: {ex}");
                }
            }
        }
        */

        // DISABLED - Not needed, the basic Show methods handle this
        /*
        /// <summary>
        /// Patch ShowConfirmationTimer to announce timed confirmation dialogs
        /// </summary>
        [HarmonyPatch(typeof(Il2Cpp.ConfirmationController), nameof(Il2Cpp.ConfirmationController.ShowConfirmationTimer))]
        public static class ConfirmationController_ShowConfirmationTimer_Patch
        {
            public static void Postfix(Il2Cpp.ConfirmationController __instance, string localizationTerm, bool showCancel, float time)
            {
                try
                {
                    // Try to get the text from the Text component
                    if (__instance.Text != null && !string.IsNullOrEmpty(__instance.Text.text))
                    {
                        // Check if we should announce
                        if (!ShouldAnnounceDialog(__instance.Text.text))
                        {
                            return;
                        }

                        string announcement = $"Timed confirmation dialog: {__instance.Text.text}";

                        // Add timer information if applicable
                        if (time > 0)
                        {
                            announcement += $". Timer: {time:F0} seconds.";
                        }

                        // Add button information
                        if (showCancel)
                        {
                            announcement += " Press Enter to confirm or Escape to cancel.";
                        }
                        else
                        {
                            announcement += " Press Enter to confirm.";
                        }

                        TolkScreenReader.Instance.Speak(announcement, true);
                        RecordAnnouncement(__instance.Text.text);

                        // Also announce button labels
                        AnnounceButtonLabels(__instance, showCancel);
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Error in ShowConfirmationTimer patch: {ex}");
                }
            }
        }
        */

        // DISABLED - Keeping this one for error messages specifically
        /// <summary>
        /// Patch ShowErrorMessage to announce error messages
        /// </summary>
        [HarmonyPatch(typeof(Il2Cpp.ConfirmationController), nameof(Il2Cpp.ConfirmationController.ShowErrorMessage))]
        [HarmonyPatch(new Type[] { typeof(string), typeof(Il2CppSystem.Action), typeof(Il2CppSystem.Action) })]
        public static class ConfirmationController_ShowErrorMessage_Patch
        {
            public static void Postfix(string errorTitle)
            {
                try
                {
                    if (!string.IsNullOrEmpty(errorTitle))
                    {
                        // Check if we should announce
                        if (!ShouldAnnounceDialog(errorTitle))
                        {
                            return;
                        }

                        string announcement = $"Error message: {errorTitle}";
                        TolkScreenReader.Instance.Speak(announcement, true);
                        RecordAnnouncement(errorTitle);
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Error in ShowErrorMessage patch: {ex}");
                }
            }
        }

        // DISABLED - Using Show methods instead
        /*
        /// <summary>
        /// Patch OnEnable to announce when confirmation dialog becomes visible
        /// </summary>
        [HarmonyPatch(typeof(Il2Cpp.ConfirmationController), "OnEnable")]
        public static class ConfirmationController_OnEnable_Patch
        {
            private static float lastOnEnableTime = -10f; // Start with negative time to avoid false positives

            public static void Postfix(Il2Cpp.ConfirmationController __instance)
            {
                try
                {
                    // Prevent OnEnable from firing multiple times in quick succession
                    float currentTime = Time.time;
                    if (currentTime - lastOnEnableTime < 0.5f)
                    {
                        return;
                    }
                    lastOnEnableTime = currentTime;

                    // Check if dialog has text content
                    if (__instance.Text != null && !string.IsNullOrEmpty(__instance.Text.text))
                    {
                        // Only announce if we haven't announced this text recently through other patches
                        if (!ShouldAnnounceDialog(__instance.Text.text))
                        {
                            return;
                        }

                        // Check if cancel button is visible to determine dialog type
                        bool hasCancelButton = __instance.Cancel != null && __instance.Cancel.gameObject.activeSelf;

                        string announcement = $"Dialog: {__instance.Text.text}";

                        if (hasCancelButton)
                        {
                            announcement += ". Press Enter to confirm or Escape to cancel.";
                        }
                        else
                        {
                            announcement += ". Press Enter to confirm.";
                        }

                        TolkScreenReader.Instance.Speak(announcement, true);
                        RecordAnnouncement(__instance.Text.text);
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Error in OnEnable patch: {ex}");
                }
            }
        }
        */

        // DISABLED - Timer countdown announcements
        /*
        /// <summary>
        /// Patch Update to announce countdown timer updates
        /// </summary>
        [HarmonyPatch(typeof(Il2Cpp.ConfirmationController), "Update")]
        public static class ConfirmationController_Update_Patch
        {
            private static float lastAnnouncedTime = -1f;

            public static void Postfix(Il2Cpp.ConfirmationController __instance)
            {
                try
                {
                    // Check if counting down
                    if (__instance.isCounting && __instance.currCountdownValue > 0)
                    {
                        float currentTime = Mathf.Ceil(__instance.currCountdownValue);

                        // Announce countdown at key intervals (10, 5, 3, 2, 1)
                        if (currentTime != lastAnnouncedTime &&
                            (currentTime == 10f || currentTime == 5f || currentTime <= 3f))
                        {
                            lastAnnouncedTime = currentTime;
                            TolkScreenReader.Instance.Speak($"{currentTime:F0} seconds remaining", false);
                        }
                    }
                    else
                    {
                        lastAnnouncedTime = -1f;
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Error in Update patch: {ex}");
                }
            }
        }
        */

        /// <summary>
        /// Helper method to announce button labels
        /// </summary>
        private static void AnnounceButtonLabels(Il2Cpp.ConfirmationController controller, bool showCancel)
        {
            try
            {
                // Try to get button text labels
                if (controller.Confirm != null)
                {
                    var confirmText = UIElementFormatter.FormatUIElementForSpeech(controller.Confirm.gameObject);
                    if (!string.IsNullOrEmpty(confirmText))
                    {
                        MelonLogger.Msg($"Confirm button: {confirmText}");
                    }
                }

                if (showCancel && controller.Cancel != null)
                {
                    var cancelText = UIElementFormatter.FormatUIElementForSpeech(controller.Cancel.gameObject);
                    if (!string.IsNullOrEmpty(cancelText))
                    {
                        MelonLogger.Msg($"Cancel button: {cancelText}");
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error announcing button labels: {ex}");
            }
        }
    }
}