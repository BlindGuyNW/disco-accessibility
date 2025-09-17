using System;
using System.Text;
using HarmonyLib;
using Il2Cpp;
using Il2CppSunshine;
using Il2CppSunshine.Metric;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using AccessibilityMod.UI;

namespace AccessibilityMod.Patches
{
    /// <summary>
    /// Patches to extract and vocalize detailed skill check tooltip information
    /// </summary>
    public static class SkillCheckTooltipPatches
    {
        // Track last announced check to avoid spam
        private static string lastAnnouncedCheck = "";
        private static float lastCheckTime = 0f;
        private static readonly float CHECK_COOLDOWN = 0.5f;

        /// <summary>
        /// Patch CheckAdvisor.SetAdvisorContent to capture skill check details when tooltip is shown
        /// </summary>
        [HarmonyPatch(typeof(CheckAdvisor), "SetAdvisorContent")]
        public static class CheckAdvisor_SetAdvisorContent_Patch
        {
            public static void Postfix(CheckAdvisor __instance, CheckResult data)
            {
                try
                {
                    if (data == null) return;

                    // Build comprehensive check information
                    var checkInfo = ExtractCheckInformation(data);

                    // Check for duplicates and cooldown
                    if (checkInfo != lastAnnouncedCheck ||
                        (UnityEngine.Time.time - lastCheckTime) > CHECK_COOLDOWN)
                    {
                        // Announce the detailed check information
                        TolkScreenReader.Instance.Speak(checkInfo, true);

                        lastAnnouncedCheck = checkInfo;
                        lastCheckTime = UnityEngine.Time.time;

                        MelonLogger.Msg($"[SKILL CHECK TOOLTIP] {checkInfo}");
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Error in CheckAdvisor.SetAdvisorContent patch: {ex}");
                }
            }
        }

        /// <summary>
        /// Patch CheckTooltip.SetTooltipContent to capture skill check details from regular tooltip
        /// </summary>
        [HarmonyPatch(typeof(CheckTooltip), "SetTooltipContent")]
        public static class CheckTooltip_SetTooltipContent_Patch
        {
            public static void Postfix(CheckTooltip __instance, TooltipSource tooltipSource)
            {
                try
                {
                    if (tooltipSource == null) return;

                    // Extract the text content from the tooltip UI elements
                    var tooltipInfo = ExtractTooltipText(__instance);

                    if (!string.IsNullOrEmpty(tooltipInfo))
                    {
                        // Check for duplicates and cooldown
                        if (tooltipInfo != lastAnnouncedCheck ||
                            (UnityEngine.Time.time - lastCheckTime) > CHECK_COOLDOWN)
                        {
                            TolkScreenReader.Instance.Speak(tooltipInfo, true);

                            lastAnnouncedCheck = tooltipInfo;
                            lastCheckTime = UnityEngine.Time.time;

                            MelonLogger.Msg($"[CHECK TOOLTIP] {tooltipInfo}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Error in CheckTooltip.SetTooltipContent patch: {ex}");
                }
            }
        }

        /// <summary>
        /// Extract comprehensive information from CheckResult and combine with dialog context
        /// </summary>
        private static string ExtractCheckInformation(CheckResult check)
        {
            var sb = new StringBuilder();

            try
            {
                // Try to find the corresponding dialog response button to get the check type and text
                string checkType = "Check";
                string dialogText = "";

                var responseButtons = UnityEngine.Object.FindObjectsOfType<Il2Cpp.SunshineResponseButton>();
                foreach (var button in responseButtons)
                {
                    if (button != null && (button.whiteCheck || button.redCheck))
                    {
                        checkType = button.whiteCheck ? "White Check" : "Red Check";

                        // Get dialog text
                        if (button.optionText?.textField?.text != null)
                        {
                            dialogText = button.optionText.textField.text.Trim();
                        }
                        else if (button.optionText?.originalText != null)
                        {
                            dialogText = button.optionText.originalText.Trim();
                        }
                        break; // Take the first match
                    }
                }

                // Get all the data we need
                string skillName = check.SkillName();
                string difficulty = check.difficulty;
                int targetNumber = check.TargetNumber();
                float probability = check.Probability();
                int percentChance = (int)(probability * 100);
                int skillValue = check.SkillValue();

                // Build modifier text
                string modifierText = "";

                // Add roll bonuses if any
                if (check.rollBonuses != null && check.rollBonuses.Count > 0)
                {
                    int totalBonus = CheckResult.CalcCheckBonusTotal(check.rollBonuses);
                    if (totalBonus != 0)
                    {
                        string bonusText = totalBonus > 0 ? $"+{totalBonus}" : totalBonus.ToString();
                        modifierText += $", Roll bonus {bonusText}";
                    }
                }

                // Add individual modifiers if any
                if (check.applicableTargetModifiers != null && check.applicableTargetModifiers.Count > 0)
                {
                    foreach (var modifier in check.applicableTargetModifiers)
                    {
                        if (modifier != null && !string.IsNullOrEmpty(modifier.explanation))
                        {
                            // Flip sign to match tooltip display (negative internal values = positive user display)
                            int displayValue = -modifier.bonus;
                            string modValue = displayValue > 0 ? $"+{displayValue}" : displayValue.ToString();
                            modifierText += $", {modValue} from {modifier.explanation}";
                        }
                    }
                }

                // Add special status
                if (check.isLocked)
                {
                    modifierText += ", LOCKED";
                }

                if (check.IsPassiveType)
                {
                    modifierText += ", Passive check";
                }

                // Format the final announcement with dialog first if available
                if (!string.IsNullOrEmpty(dialogText))
                {
                    // Clean up the dialog text by removing the skill check notation that's already announced
                    string cleanDialog = dialogText;
                    if (cleanDialog.Contains("[") && cleanDialog.Contains("]"))
                    {
                        // Remove the [Skill - Difficulty X] part since we're announcing it separately
                        int bracketStart = cleanDialog.IndexOf('[');
                        int bracketEnd = cleanDialog.IndexOf(']');
                        if (bracketStart >= 0 && bracketEnd > bracketStart)
                        {
                            cleanDialog = cleanDialog.Substring(bracketEnd + 1).Trim();
                        }
                    }

                    return $"{cleanDialog}. {checkType}: {skillName} - {difficulty} {targetNumber}, {percentChance}% chance, Skill level {skillValue}{modifierText}";
                }
                else
                {
                    return $"{checkType}: {skillName} - {difficulty} {targetNumber}, {percentChance}% chance, Skill level {skillValue}{modifierText}";
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error extracting check information: {ex}");
                return "Skill check information unavailable";
            }
        }

        /// <summary>
        /// Extract text directly from CheckTooltip UI elements
        /// </summary>
        private static string ExtractTooltipText(CheckTooltip tooltip)
        {
            var sb = new StringBuilder();

            try
            {
                // Get title text
                if (tooltip.titleText != null && !string.IsNullOrEmpty(tooltip.titleText.text))
                {
                    sb.Append(tooltip.titleText.text);
                }

                // Get probability text
                if (tooltip.titleProbability != null && !string.IsNullOrEmpty(tooltip.titleProbability.text))
                {
                    sb.Append(", ");
                    sb.Append(tooltip.titleProbability.text);
                }

                // Get results breakdown
                if (tooltip.resultsBox != null && !string.IsNullOrEmpty(tooltip.resultsBox.text))
                {
                    sb.Append(", Details: ");
                    sb.Append(tooltip.resultsBox.text);
                }

                // Get explanation if available
                if (tooltip.explanation != null && !string.IsNullOrEmpty(tooltip.explanation.text))
                {
                    sb.Append(", ");
                    sb.Append(tooltip.explanation.text);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error extracting tooltip text: {ex}");
                return null;
            }

            return sb.Length > 0 ? sb.ToString() : null;
        }
    }
}