using System;
using System.Text;
using HarmonyLib;
using Il2Cpp;
using Il2CppSunshine;
using Il2CppSunshine.Metric;
using MelonLoader;
using UnityEngine.EventSystems;
using AccessibilityMod.UI;

namespace AccessibilityMod.Patches
{
    /// <summary>
    /// Patches to vocalize skill check tooltips on the journal map tab
    /// </summary>
    public static class MapTabSkillCheckPatches
    {
        /// <summary>
        /// Patch PageSystemJournalWhiteCheckUI.OnSelect to announce skill check details when selected on map tab
        /// </summary>
        [HarmonyPatch(typeof(PageSystemJournalWhiteCheckUI), "OnSelect")]
        public static class PageSystemJournalWhiteCheckUI_OnSelect_Patch
        {
            public static void Postfix(PageSystemJournalWhiteCheckUI __instance, BaseEventData eventData)
            {
                try
                {
                    // Check if we have a white check associated with this UI element
                    if (__instance.whiteCheck == null) return;

                    // Create a CheckResult from the WhiteCheck data
                    var whiteCheck = __instance.whiteCheck;
                    var checkResult = CreateCheckResultFromWhiteCheck(whiteCheck);
                    if (checkResult == null) return;

                    // Build the check information string
                    string checkInfo = BuildMapCheckInfo(__instance.titleText, checkResult, __instance.IsCheckLocked(), __instance.IsCheckReopened(), !__instance.isSeen);

                    // Announce it
                    if (!string.IsNullOrEmpty(checkInfo))
                    {
                        TolkScreenReader.Instance.Speak(checkInfo, true);
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Error in PageSystemJournalWhiteCheckUI.OnSelect patch: {ex}");
                }
            }
        }

        /// <summary>
        /// Patch JournalWhiteCheckUI.OnSelect to announce skill check details when selected on map tab
        /// </summary>
        [HarmonyPatch(typeof(JournalWhiteCheckUI), "OnSelect")]
        public static class JournalWhiteCheckUI_OnSelect_Patch
        {
            public static void Postfix(JournalWhiteCheckUI __instance, BaseEventData eventData)
            {
                try
                {
                    // Check if we have a white check associated with this UI element
                    if (__instance.whiteCheck == null) return;

                    // Create a CheckResult from the WhiteCheck data
                    var whiteCheck = __instance.whiteCheck;
                    var checkResult = CreateCheckResultFromWhiteCheck(whiteCheck);
                    if (checkResult == null) return;

                    // Build the check information string
                    string checkInfo = BuildMapCheckInfo(__instance.actorText, checkResult, __instance.IsCheckLocked(), __instance.isReopened, !__instance.isSeen);

                    // Announce it
                    if (!string.IsNullOrEmpty(checkInfo))
                    {
                        TolkScreenReader.Instance.Speak(checkInfo, true);
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Error in JournalWhiteCheckUI.OnSelect patch: {ex}");
                }
            }
        }

        /// <summary>
        /// Create a CheckResult from WhiteCheck data for vocalization purposes
        /// </summary>
        private static CheckResult CreateCheckResultFromWhiteCheck(WhiteCheck whiteCheck)
        {
            try
            {
                // Create a new CheckResult with the check type
                var checkResult = new CheckResult(CheckType.WHITE);

                // Set the skill type from the white check
                checkResult.skillType = whiteCheck.SkillType;

                // Set the target value and skill value from saved data
                checkResult.baseTarget = whiteCheck.LastTargetValue > 0 ? whiteCheck.LastTargetValue : whiteCheck.difficulty;
                checkResult.skillBase = whiteCheck.LastSkillValue;

                // Set modifiers if available
                if (whiteCheck.CheckModifiers != null && whiteCheck.CheckModifiers.Count > 0)
                {
                    // Get the first list of modifiers from the dictionary
                    foreach (var kvp in whiteCheck.CheckModifiers)
                    {
                        if (kvp.Value != null && kvp.Value.Count > 0)
                        {
                            checkResult.SetTargetModifiers(kvp.Value);
                            break;
                        }
                    }
                }

                return checkResult;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error creating CheckResult from WhiteCheck: {ex}");

                // Fallback: create a simple CheckResult with minimal info
                try
                {
                    var fallbackResult = new CheckResult(CheckType.WHITE);
                    fallbackResult.skillType = whiteCheck.SkillType;
                    fallbackResult.baseTarget = whiteCheck.LastTargetValue;
                    fallbackResult.skillBase = whiteCheck.LastSkillValue;
                    return fallbackResult;
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Build check info string for map tab checks
        /// </summary>
        private static string BuildMapCheckInfo(Il2CppTMPro.TextMeshProUGUI titleText, CheckResult check, bool isLocked, bool isReopened, bool isNew)
        {
            try
            {
                var sb = new StringBuilder();

                // First, add the basic check text (actor and location)
                if (titleText != null && !string.IsNullOrEmpty(titleText.text))
                {
                    string title = titleText.text.Trim();

                    // Clean up any formatting tags
                    if (title.Contains("<"))
                    {
                        // Simple tag removal - could be improved
                        title = System.Text.RegularExpressions.Regex.Replace(title, @"<[^>]+>", "");
                    }

                    sb.Append(title);
                    sb.Append(". ");
                }

                // Determine if this is locked, reopened, or available
                // Check isReopened first - if a check is reopened, it's available regardless of lock status
                string status = "";
                if (isReopened)
                {
                    status = "Reopened ";
                }
                else if (isLocked)
                {
                    status = "Locked ";
                }
                else if (isNew)
                {
                    status = "New ";
                }

                // Add the detailed check information using the shared method
                string checkType = status + "White Check";
                string checkDetails = SkillCheckTooltipPatches.BuildCheckInfoString(check, checkType);
                sb.Append(checkDetails);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error building map check info: {ex}");

                // Fallback to basic info
                try
                {
                    if (titleText != null && !string.IsNullOrEmpty(titleText.text))
                    {
                        return titleText.text;
                    }
                }
                catch { }

                return "Map check information unavailable";
            }
        }

    }
}