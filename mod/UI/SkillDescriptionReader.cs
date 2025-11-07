using System;
using Il2Cpp;
using MelonLoader;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AccessibilityMod.UI
{
    /// <summary>
    /// Handles reading skill descriptions from the character sheet
    /// </summary>
    public class SkillDescriptionReader
    {
        /// <summary>
        /// Read the long description for the currently selected skill in the character sheet
        /// </summary>
        public static void ReadSelectedSkillDescription()
        {
            try
            {
                // Check if character sheet info panel is active (used in gameplay)
                var infoPanel = CharacterSheetInfoPanel.Singleton;
                bool hasInfoPanel = infoPanel != null && infoPanel.gameObject.activeInHierarchy;

                // Get currently selected UI element
                var eventSystem = EventSystem.current;
                if (eventSystem == null)
                {
                    TolkScreenReader.Instance.Speak("No UI system active", true);
                    MelonLogger.Warning("[SKILL DESCRIPTION] No EventSystem found");
                    return;
                }

                var selectedObject = eventSystem.currentSelectedGameObject;
                if (selectedObject == null)
                {
                    TolkScreenReader.Instance.Speak("No skill selected", true);
                    MelonLogger.Msg("[SKILL DESCRIPTION] No UI element selected");
                    return;
                }

                // Try to get SkillPortraitPanel component from selected object or its parents
                var skillPanel = selectedObject.GetComponentInParent<SkillPortraitPanel>();
                if (skillPanel == null || skillPanel.currentSkill == null)
                {
                    TolkScreenReader.Instance.Speak("No skill panel found. Please select a skill first", true);
                    MelonLogger.Msg("[SKILL DESCRIPTION] Selected object is not a skill panel");
                    return;
                }

                // Get the skill name for better feedback
                string skillName = "Unknown Skill";
                try
                {
                    skillName = Il2CppSunshine.Metric.Skill.SkillTypeToLocalizedName(
                        skillPanel.skill,
                        true
                    );
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"[SKILL DESCRIPTION] Could not get skill name: {ex.Message}");
                }

                string longDescription = null;

                // If we have info panel (gameplay context), use it to get description
                if (hasInfoPanel)
                {
                    // Update the info panel to show the current skill's information
                    try
                    {
                        // Get the modifiable data for this skill
                        var modifiable = skillPanel.GetModifiable();
                        if (modifiable != null)
                        {
                            // Tell the info panel to display this skill's data
                            infoPanel.ShowModifiable(modifiable);
                            MelonLogger.Msg($"[SKILL DESCRIPTION] Called ShowModifiable for {skillName}");
                        }
                        else
                        {
                            MelonLogger.Warning($"[SKILL DESCRIPTION] Could not get modifiable for {skillName}");
                        }

                        // Toggle the info panel on to show the description
                        infoPanel.ToggleInfo(true);
                        MelonLogger.Msg($"[SKILL DESCRIPTION] Toggled info panel to Info tab");
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Warning($"[SKILL DESCRIPTION] Could not update info panel: {ex.Message}");
                    }

                    // Method 1: Direct access to InfoText GameObject (optimized for gameplay)
                    try
                    {
                        // Navigate to InfoText: infoPanel -> find "InfoPanel" child -> "MaskedPanel" -> "InfoText"
                        Transform infoPanelChild = infoPanel.transform.Find("PortraitMask/Scalable Text/InfoPanel");
                        if (infoPanelChild != null)
                        {
                            Transform maskedPanel = infoPanelChild.Find("MaskedPanel");
                            if (maskedPanel != null)
                            {
                                Transform infoTextObj = maskedPanel.Find("InfoText");
                                if (infoTextObj != null)
                                {
                                    var infoTextComponent = infoTextObj.GetComponent<Il2CppTMPro.TextMeshProUGUI>();
                                    if (infoTextComponent != null && !string.IsNullOrEmpty(infoTextComponent.text))
                                    {
                                        longDescription = infoTextComponent.text.Trim();
                                        MelonLogger.Msg($"[SKILL DESCRIPTION] Got description directly from InfoText ({longDescription.Length} chars)");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Warning($"[SKILL DESCRIPTION] Error accessing InfoText directly: {ex.Message}");
                    }
                }

                // Method 2: Fallback - try to find description text from skill panel's children (works for both contexts)
                if (string.IsNullOrEmpty(longDescription))
                {
                    MelonLogger.Msg("[SKILL DESCRIPTION] Looking for description in skill panel UI");

                    try
                    {
                        // Search for description text in the skill panel or its siblings
                        var root = skillPanel.transform.root;
                        var allTexts = root.GetComponentsInChildren<Il2CppTMPro.TextMeshProUGUI>(true);
                        MelonLogger.Msg($"[SKILL DESCRIPTION] Found {allTexts.Length} TextMeshProUGUI components in scene");

                        foreach (var textComponent in allTexts)
                        {
                            if (textComponent != null && !string.IsNullOrEmpty(textComponent.text))
                            {
                                string text = textComponent.text.Trim();

                                // Skip if too short to be a description
                                if (text.Length < 100)
                                    continue;

                                // Skip mechanical info (contains numbers and specific keywords)
                                if (text.Contains("Base:") ||
                                    text.Contains("Bonus:") ||
                                    text.Contains("Total:") ||
                                    text.Contains("Penalty:") ||
                                    text.Contains("+") && text.Contains("-") ||
                                    System.Text.RegularExpressions.Regex.IsMatch(text, @"\d+"))
                                {
                                    continue;
                                }

                                // Found potential description
                                if (longDescription == null || text.Length > longDescription.Length)
                                {
                                    longDescription = text;
                                    MelonLogger.Msg($"[SKILL DESCRIPTION] Found description ({text.Length} chars): {text.Substring(0, Math.Min(50, text.Length))}...");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Warning($"[SKILL DESCRIPTION] Error in fallback search: {ex.Message}");
                    }
                }

                // Method 3: Final fallback - use hardcoded descriptions
                if (string.IsNullOrEmpty(longDescription))
                {
                    MelonLogger.Msg("[SKILL DESCRIPTION] Using hardcoded description fallback");
                    longDescription = CharacterCreationFormatter.GetHardcodedSkillDescription(skillPanel.skill.ToString());
                }


                if (string.IsNullOrEmpty(longDescription))
                {
                    TolkScreenReader.Instance.Speak($"{skillName}: No description found", true);
                    MelonLogger.Warning($"[SKILL DESCRIPTION] No description found for {skillName}");
                    return;
                }

                // Clean up the description text (remove excessive whitespace, etc.)
                longDescription = CleanDescriptionText(longDescription);

                // Announce the skill name and description
                string announcement = $"{skillName}: {longDescription}";
                TolkScreenReader.Instance.Speak(announcement, true);
                MelonLogger.Msg($"[SKILL DESCRIPTION] Read description for {skillName}: {longDescription.Substring(0, Math.Min(50, longDescription.Length))}...");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[SKILL DESCRIPTION] Error reading skill description: {ex}");
                TolkScreenReader.Instance.Speak("Error reading skill description", true);
            }
        }

        /// <summary>
        /// Clean up description text for better speech output
        /// </summary>
        private static string CleanDescriptionText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Trim whitespace
            text = text.Trim();

            // Replace multiple spaces with single space
            while (text.Contains("  "))
            {
                text = text.Replace("  ", " ");
            }

            // Replace multiple newlines with single newline
            while (text.Contains("\n\n\n"))
            {
                text = text.Replace("\n\n\n", "\n\n");
            }

            // Replace newlines with spaces for better speech flow
            text = text.Replace("\n", " ");
            text = text.Replace("\r", "");

            return text;
        }
    }
}
