using System;
using HarmonyLib;
using Il2CppNotificationSystem;
using MelonLoader;

namespace AccessibilityMod.Patches
{
    [HarmonyPatch]
    public static class NotificationVocalizationPatches
    {
        /// <summary>
        /// Single hook for all notifications - only patch PlayNextNotification to avoid duplicates
        /// This should capture all notifications when they actually display
        /// </summary>
        [HarmonyPatch(typeof(NotificationManager), "PlayNextNotification")]
        [HarmonyPostfix]
        public static void NotificationManager_PlayNextNotification_Postfix(NotificationManager __instance)
        {
            try
            {
                if (__instance != null && __instance._currentlyPlayedNotification != null)
                {
                    var notification = __instance._currentlyPlayedNotification;
                    string headerText = notification.HeaderText;
                    string descriptionText = notification.DescriptionText;

                    // Build notification text from available components
                    string notificationText = "";
                    if (!string.IsNullOrEmpty(headerText))
                    {
                        notificationText = headerText.Trim();
                    }
                    if (!string.IsNullOrEmpty(descriptionText))
                    {
                        if (!string.IsNullOrEmpty(notificationText))
                        {
                            notificationText += " - " + descriptionText.Trim();
                        }
                        else
                        {
                            notificationText = descriptionText.Trim();
                        }
                    }

                    if (!string.IsNullOrEmpty(notificationText))
                    {
                        TolkScreenReader.Instance.Speak($"Notification: {notificationText}", true);
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in NotificationManager PlayNextNotification patch: {ex}");
            }
        }

        /// <summary>
        /// Patch for CheckResult.CheckText() to catch skill check results and build proper text
        /// We'll construct the full text ourselves using available properties
        /// </summary>
        [HarmonyPatch(typeof(Il2CppSunshine.Metric.CheckResult), "CheckText")]
        [HarmonyPostfix]
        public static void CheckResult_CheckText_Postfix(Il2CppSunshine.Metric.CheckResult __instance, string __result)
        {
            try
            {
                if (__instance != null)
                {
                    // Build complete skill check text using CheckResult properties
                    string skillName = __instance.SkillName();
                    string difficulty = __instance.difficulty;
                    bool isSuccess = __instance.IsSuccess;
                    
                    // Clean the original result text by removing HTML tags
                    string cleanResult = __result;
                    if (!string.IsNullOrEmpty(cleanResult))
                    {
                        cleanResult = System.Text.RegularExpressions.Regex.Replace(cleanResult, @"<[^>]*>", "");
                        cleanResult = cleanResult.Replace("[", "").Replace("]", "").Trim();
                    }
                    
                    // Build the complete text: "SkillName Difficulty: Success/Failure"
                    string fullText = "";
                    if (!string.IsNullOrEmpty(skillName))
                    {
                        fullText = skillName;
                        
                        if (!string.IsNullOrEmpty(difficulty))
                        {
                            fullText += " " + difficulty;
                        }
                        
                        string result = isSuccess ? "Success" : "Failure";
                        fullText += ": " + result;
                        
                        TolkScreenReader.Instance.Speak($"Skill check: {fullText}", true);
                    }
                    else if (!string.IsNullOrEmpty(cleanResult))
                    {
                        // Fallback to cleaned result if we can't get skill name
                        TolkScreenReader.Instance.Speak($"Skill check: {cleanResult}", true);
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in CheckResult CheckText patch: {ex}");
            }
        }
    }
}