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

                    // Wait for UI to settle before reading button state
                    MelonLoader.MelonCoroutines.Start(DelayedSkillCheckAnnouncement(data));
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Error in CheckAdvisor.SetAdvisorContent patch: {ex}");
                }
            }

            private static System.Collections.IEnumerator DelayedSkillCheckAnnouncement(CheckResult data)
            {
                yield return new UnityEngine.WaitForSeconds(0.25f);

                try
                {
                    // Build comprehensive check information
                    var checkInfo = ExtractCheckInformation(data);

                    // Announce the detailed check information
                    TolkScreenReader.Instance.Speak(checkInfo, true);
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Error in delayed skill check announcement: {ex}");
                }
            }
        }

        /// <summary>
        /// Patch CheckTooltip.SetTooltipContent to capture skill check details from regular tooltip
        /// DISABLED - Using CheckAdvisor patch instead to avoid duplicate announcements
        /// </summary>
        // [HarmonyPatch(typeof(CheckTooltip), "SetTooltipContent")]
        public static class CheckTooltip_SetTooltipContent_Patch
        {
            // DISABLED to prevent duplicate announcements
            /*
            public static void Postfix(CheckTooltip __instance, TooltipSource tooltipSource)
            {
                try
                {
                    if (tooltipSource == null) return;

                    // Extract the text content from the tooltip UI elements
                    var tooltipInfo = ExtractTooltipText(__instance);

                    if (!string.IsNullOrEmpty(tooltipInfo))
                    {
                        TolkScreenReader.Instance.Speak(tooltipInfo, true);
                        MelonLogger.Msg($"[CHECK TOOLTIP] {tooltipInfo}");
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Error in CheckTooltip.SetTooltipContent patch: {ex}");
                }
            }
            */
        }

        /// <summary>
        /// Extract comprehensive information from CheckResult and combine with dialog context
        /// </summary>
        private static string ExtractCheckInformation(CheckResult check)
        {
            var sb = new StringBuilder();

            try
            {
                // Find the currently selected/highlighted button
                string checkType = "Check";
                string dialogText = "";

                // Look for the currently selected UI element
                var currentSelection = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
                if (currentSelection != null)
                {
                    var selectedButton = currentSelection.GetComponent<Il2Cpp.SunshineResponseButton>();
                    if (selectedButton != null && (selectedButton.whiteCheck || selectedButton.redCheck))
                    {
                        checkType = selectedButton.whiteCheck ? "White Check" : "Red Check";

                        // Get dialog text from the selected button
                        if (selectedButton.optionText?.textField?.text != null)
                        {
                            dialogText = selectedButton.optionText.textField.text.Trim();
                        }
                        else if (selectedButton.optionText?.originalText != null)
                        {
                            dialogText = selectedButton.optionText.originalText.Trim();
                        }
                    }
                }

                // Build the check info using the shared method
                string checkInfo = BuildCheckInfoString(check, checkType);

                // Format the final announcement - ALWAYS put dialog first when available, then check details
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

                    return $"{cleanDialog}. {checkInfo}";
                }
                else
                {
                    return checkInfo;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error extracting check information: {ex}");
                return "Skill check information unavailable";
            }
        }

        /// <summary>
        /// Build a formatted string with check information - reusable for both dialog and map checks
        /// </summary>
        public static string BuildCheckInfoString(CheckResult check, string checkType = "Check")
        {
            try
            {
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

                return $"{checkType}: {skillName} - {difficulty} {targetNumber}, {percentChance}% chance, Skill level {skillValue}{modifierText}";
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error building check info string: {ex}");
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