using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Il2CppTMPro;
using Il2Cpp;
using Il2CppSunshine;
using Il2CppSunshine.Metric;
using Il2CppFortressOccident;
using Il2CppDiscoPages.Elements.MainMenu;
using Il2CppPages.MainMenu;
using Il2CppPages.Gameplay.Charsheet;
using Il2CppI2.Loc;
using Il2CppCollageMode.Scripts.Localization;
using AccessibilityMod.Utils;
using MelonLoader;

namespace AccessibilityMod.UI
{
    /// <summary>
    /// Handles formatting for character creation elements including stats, skills, and archetypes
    /// </summary>
    public static class CharacterCreationFormatter
    {
        /// <summary>
        /// Try to format character creation attribute context
        /// </summary>
        public static string GetCharacterCreationContext(GameObject uiObject)
        {
            try
            {
                if (uiObject == null) return null;

                // First check if this is a StatPanel with tooltip support
                // BUT ONLY IN GAMEPLAY - not during character creation!
                var statPanel = uiObject.GetComponentInParent<Il2Cpp.StatPanel>();
                if (statPanel != null && IsInGameplayContext(uiObject))
                {
                    try
                    {
                        // Try to get rich tooltip information for stats (gameplay only)
                        var modifiable = statPanel.GetModifiable();
                        if (modifiable != null)
                        {
                            string tooltipData = Il2Cpp.CharacterSheetInfoPanel.GatherModifiableData(modifiable);
                            if (!string.IsNullOrEmpty(tooltipData))
                            {
                                // Add the total value at the end for convenience
                                int totalValue = modifiable.value;
                                string result = $"{tooltipData}\nTotal: {totalValue}";

                                // Get the attribute name from the object
                                string attributeName = GetAttributeNameFromObject(uiObject);
                                if (!string.IsNullOrEmpty(attributeName))
                                {
                                    return $"{attributeName}: {result}";
                                }
                                return result;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Warning($"Error getting stat tooltip data: {ex.Message}");
                    }
                }

                // Check if we're in character creation by looking for specific parent hierarchy
                var parent = uiObject.transform.parent;
                if (parent != null && parent.name == "Abilities")
                {
                    var grandparent = parent.parent;
                    if (grandparent != null && grandparent.name == "Leveling")
                    {
                        // This is a character creation attribute element
                        string attributeName = uiObject.name;
                        string speechText = TextExtractor.ExtractBestTextContent(uiObject);

                        if (!string.IsNullOrEmpty(speechText) && !string.IsNullOrEmpty(attributeName))
                        {
                            // Try to get displayed stat description first
                            string displayedDescription = FindDisplayedStatDescription(uiObject, attributeName);
                            if (!string.IsNullOrEmpty(displayedDescription))
                            {
                                if (speechText.Length <= 2) // Likely a number value
                                {
                                    string points = speechText == "1" ? "point" : "points";
                                    return $"{attributeName}: {speechText} {points} - {displayedDescription}";
                                }
                                else
                                {
                                    return $"{attributeName}: {speechText} - {displayedDescription}";
                                }
                            }

                            // Fallback to hardcoded descriptions
                            string fallbackDescription = GetAttributeDescription(attributeName);

                            if (speechText.Length <= 2) // Likely a number value
                            {
                                string points = speechText == "1" ? "point" : "points";
                                return $"{attributeName}: {speechText} {points}{fallbackDescription}";
                            }
                            else
                            {
                                return $"{attributeName}: {speechText}{fallbackDescription}";
                            }
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting character creation context: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Try to format skill selection context
        /// </summary>
        public static string GetSkillSelectionContext(GameObject uiObject)
        {
            try
            {
                if (uiObject == null) return null;

                // Check if this object is part of a SkillPortraitPanel
                var skillPanel = uiObject.GetComponentInParent<Il2Cpp.SkillPortraitPanel>();
                if (skillPanel != null && skillPanel.currentSkill != null)
                {
                    var skill = skillPanel.currentSkill;
                    var skillType = skillPanel.skill;

                    // Get the localized skill name
                    string skillName = Il2CppSunshine.Metric.Skill.SkillTypeToLocalizedName(skillType, true);

                    // Check if we're in gameplay (not character creation)
                    // In gameplay, we want rich tooltip information
                    if (IsInGameplayContext(uiObject))
                    {
                        try
                        {
                            // Try to get rich tooltip information
                            var modifiable = skillPanel.GetModifiable();
                            if (modifiable != null)
                            {
                                string tooltipData = Il2Cpp.CharacterSheetInfoPanel.GatherModifiableData(modifiable);
                                if (!string.IsNullOrEmpty(tooltipData))
                                {
                                    // Add the total value at the end for convenience
                                    int totalValue = modifiable.value;
                                    return $"{skillName}: {tooltipData}\nTotal: {totalValue}";
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MelonLogger.Warning($"Error getting tooltip data: {ex.Message}");
                        }

                        // Fallback to basic mechanical information using panel values
                        string result = $"{skillName}: {skillPanel.statValue}";

                        // Add rank if different from stat value
                        if (skillPanel.rankValue != skillPanel.statValue)
                        {
                            result += $" (Base: {skillPanel.rankValue}";
                            int modifier = skillPanel.statValue - skillPanel.rankValue;
                            if (modifier > 0)
                                result += $", +{modifier} bonus";
                            else if (modifier < 0)
                                result += $", {modifier} penalty";
                            result += ")";
                        }

                        return result;
                    }
                    else
                    {
                        // Character creation - provide descriptions
                        string description = FindDisplayedSkillDescription(skillPanel.gameObject, skillName);
                        if (!string.IsNullOrEmpty(description))
                        {
                            return description;
                        }

                        // Fallback to hardcoded descriptions
                        return GetHardcodedSkillDescription(skillType.ToString());
                    }
                }

                // Fallback: Check for skill components without SkillPortraitPanel
                // This handles other skill UI elements that might not have the panel
                var parent = uiObject.transform.parent;
                if (parent != null && parent.parent != null &&
                    parent.parent.name == "Skills")
                {
                    string skillName = parent.name;

                    // In gameplay, just return the skill name with any visible values
                    if (IsInGameplayContext(uiObject))
                    {
                        string basicText = TextExtractor.ExtractBestTextContent(uiObject);
                        if (!string.IsNullOrEmpty(basicText))
                        {
                            return $"{skillName.Replace('_', ' ')}: {basicText}";
                        }
                        return skillName.Replace('_', ' ');
                    }

                    // Character creation - try to get descriptions
                    string gameDescription = GetGameSkillDescription(parent.gameObject, skillName);
                    if (!string.IsNullOrEmpty(gameDescription))
                    {
                        return gameDescription;
                    }

                    return GetHardcodedSkillDescription(skillName);
                }

                // Check if this is a skill point allocation button (+ or - buttons)
                string pointAllocationContext = GetSkillPointAllocationContext(uiObject);
                if (!string.IsNullOrEmpty(pointAllocationContext))
                {
                    return pointAllocationContext;
                }

                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting skill selection context: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Try to format archetype information
        /// </summary>
        public static string GetArchetypeInformation(GameObject uiObject)
        {
            try
            {
                if (uiObject == null) return null;

                // Check for ArchetypeSelectMenuButton component first
                var archetypeButton = uiObject.GetComponent<ArchetypeSelectMenuButton>();
                if (archetypeButton != null)
                {
                    return FormatArchetypeButtonForSpeech(archetypeButton);
                }

                // Check if this might be archetype-related based on context or text content
                string basicText = TextExtractor.ExtractBestTextContent(uiObject);
                if (!string.IsNullOrEmpty(basicText) && IsArchetypeRelatedText(basicText, uiObject))
                {
                    // Try to find displayed archetype description
                    string displayedDescription = FindDisplayedArchetypeDescription(uiObject, basicText);
                    if (!string.IsNullOrEmpty(displayedDescription))
                    {
                        return displayedDescription;
                    }
                    
                    // Fallback: provide context based on common archetype names
                    return GetArchetypeContextFromText(basicText);
                }

                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting archetype information: {ex}");
                return null;
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Get attribute name from UI object context
        /// </summary>
        private static string GetAttributeNameFromObject(GameObject uiObject)
        {
            try
            {
                // Check object name first
                string objectName = uiObject.name.ToLower();
                if (objectName.Contains("intellect")) return "Intellect";
                if (objectName.Contains("psyche")) return "Psyche";
                if (objectName.Contains("physique")) return "Physique";
                if (objectName.Contains("motorics")) return "Motorics";

                // Check parent hierarchy
                Transform current = uiObject.transform;
                while (current != null)
                {
                    string name = current.name.ToLower();
                    if (name.Contains("intellect")) return "Intellect";
                    if (name.Contains("psyche")) return "Psyche";
                    if (name.Contains("physique")) return "Physique";
                    if (name.Contains("motorics")) return "Motorics";
                    current = current.parent;
                }

                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting attribute name from object: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Check if we're in gameplay context (not character creation)
        /// </summary>
        private static bool IsInGameplayContext(GameObject uiObject)
        {
            try
            {
                // First try to use World.RunningOrCollage which indicates if the game world is active
                // This is a static property that should be true during gameplay, false in menus
                if (Il2Cpp.World.RunningOrCollage)
                {
                    return true;
                }

                // Also check the ApplicationManager singleton as a secondary check
                var appManager = ApplicationManager.Singleton;
                if (appManager != null && appManager.IsGameArea)
                {
                    return true;
                }

                // Default to false (character creation) to ensure we show descriptions
                return false;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Error checking game context: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Find displayed stat description from UI
        /// </summary>
        private static string FindDisplayedStatDescription(GameObject statObject, string statName)
        {
            try
            {
                return TextExtractor.FindDisplayedDescription(
                    statObject, 
                    text => TextExtractor.IsLikelyStatDescriptionText(text, statName), 
                    4, 
                    $"stat {statName}"
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error finding displayed stat description: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Get hardcoded attribute descriptions as fallback
        /// </summary>
        private static string GetAttributeDescription(string attributeName)
        {
            return attributeName.ToLower() switch
            {
                "intellect" => " - affects logic, reasoning, and knowledge-based skills",
                "psyche" => " - affects empathy, composure, and social interaction skills",
                "physique" => " - affects physical strength, endurance, and combat abilities",
                "motorics" => " - affects dexterity, coordination, and perception skills",
                _ => ""
            };
        }

        /// <summary>
        /// Get skill description from displayed UI or fallback to hardcoded
        /// </summary>
        private static string GetGameSkillDescription(GameObject skillParent, string skillName)
        {
            try
            {
                if (skillParent == null) return null;
                
                // Try to find displayed skill description first
                string displayedDescription = FindDisplayedSkillDescription(skillParent, skillName);
                if (!string.IsNullOrEmpty(displayedDescription))
                {
                    return displayedDescription;
                }
                
                // Fallback to hardcoded descriptions
                return GetHardcodedSkillDescription(skillName);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting game skill description for {skillName}: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Find displayed skill description from UI
        /// </summary>
        private static string FindDisplayedSkillDescription(GameObject skillParent, string skillName)
        {
            try
            {
                string description = TextExtractor.FindDisplayedDescription(
                    skillParent,
                    text => TextExtractor.IsLikelySkillDescriptionText(text, skillName),
                    3,
                    $"skill {skillName}"
                );

                if (!string.IsNullOrEmpty(description))
                {
                    // Try to get localized skill name
                    string cleanedName = GetLocalizedSkillName(skillName);
                    if (string.IsNullOrEmpty(cleanedName))
                    {
                        cleanedName = skillName.Replace('_', ' ');
                    }

                    return $"{cleanedName}: {description}";
                }

                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error finding displayed skill description: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Get localized skill name from game data
        /// </summary>
        private static string GetLocalizedSkillName(string skillName)
        {
            try
            {
                if (TryGetSkillTypeFromName(skillName, out SkillType skillType))
                {
                    return Skill.SkillTypeToLocalizedName(skillType, true);
                }
                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting localized skill name for {skillName}: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Map skill name to SkillType enum
        /// </summary>
        private static bool TryGetSkillTypeFromName(string skillName, out SkillType skillType)
        {
            try
            {
                skillType = skillName.ToUpper() switch
                {
                    "LOGIC" => SkillType.LOGIC,
                    "ENCYCLOPEDIA" => SkillType.ENCYCLOPEDIA,
                    "RHETORIC" => SkillType.RHETORIC,
                    "DRAMA" => SkillType.DRAMA,
                    "CONCEPTUALIZATION" => SkillType.CONCEPTUALIZATION,
                    "VISUAL_CALCULUS" => SkillType.VISUAL_CALCULUS,
                    "VOLITION" => SkillType.VOLITION,
                    "INLAND_EMPIRE" => SkillType.INLAND_EMPIRE,
                    "EMPATHY" => SkillType.EMPATHY,
                    "AUTHORITY" => SkillType.AUTHORITY,
                    "SUGGESTION" => SkillType.SUGGESTION,
                    "ESPRIT_DE_CORPS" => SkillType.ESPRIT_DE_CORPS,
                    "PHYSICAL_INSTRUMENT" => SkillType.PHYSICAL_INSTRUMENT,
                    "ELECTROCHEMISTRY" => SkillType.ELECTROCHEMISTRY,
                    "ENDURANCE" => SkillType.ENDURANCE,
                    "HALF_LIGHT" => SkillType.HALF_LIGHT,
                    "PAIN_THRESHOLD" => SkillType.PAIN_THRESHOLD,
                    "SHIVERS" => SkillType.SHIVERS,
                    "HAND_EYE_COORDINATION" or "HE_COORDINATION" => SkillType.HE_COORDINATION,
                    "PERCEPTION" => SkillType.PERCEPTION,
                    "REACTION_SPEED" or "REACTION" => SkillType.REACTION,
                    "SAVOIR_FAIRE" => SkillType.SAVOIR_FAIRE,
                    "INTERFACING" => SkillType.INTERFACING,
                    "COMPOSURE" => SkillType.COMPOSURE,
                    _ => SkillType.LOGIC
                };
                return skillType != SkillType.LOGIC || skillName.ToUpper() == "LOGIC";
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error mapping skill name to type: {ex.Message}");
                skillType = SkillType.LOGIC;
                return false;
            }
        }

        /// <summary>
        /// Get hardcoded skill descriptions as fallback
        /// </summary>
        public static string GetHardcodedSkillDescription(string skillName)
        {
            return skillName.ToUpper() switch
            {
                // Intellect Skills
                "LOGIC" => "Logic Skill: Deductive reasoning and problem-solving. Helps with evidence analysis and logical conclusions",
                "ENCYCLOPEDIA" => "Encyclopedia Skill: General knowledge and trivia. Provides background information on various topics",
                "RHETORIC" => "Rhetoric Skill: Persuasion and argumentation. Useful for convincing others and debating",
                "DRAMA" => "Drama Skill: Acting and deception. Helps with lying and theatrical performance",
                "CONCEPTUALIZATION" => "Conceptualization Skill: Abstract thinking and creativity. Aids in artistic and philosophical insights",
                "VISUAL_CALCULUS" => "Visual Calculus Skill: Spatial reasoning and trajectory analysis. Useful for physics and geometry",

                // Psyche Skills
                "VOLITION" => "Volition Skill: Willpower and self-control. Resists mental influence and maintains composure",
                "INLAND_EMPIRE" => "Inland Empire Skill: Intuition and surreal thinking. Provides mystical and abstract insights",
                "EMPATHY" => "Empathy Skill: Understanding others' emotions. Helps read people and social situations",
                "AUTHORITY" => "Authority Skill: Leadership and intimidation. Commands respect and dominates conversations",
                "SUGGESTION" => "Suggestion Skill: Subtle influence and manipulation. Guides conversations indirectly",
                "ESPRIT_DE_CORPS" => "Esprit de Corps Skill: Police solidarity and institutional knowledge. Connects with law enforcement",

                // Physique Skills
                "PHYSICAL_INSTRUMENT" => "Physical Instrument Skill: Raw strength and intimidation. Useful for violence and physical threats",
                "ELECTROCHEMISTRY" => "Electrochemistry Skill: Drug knowledge and chemical effects. Understands substances and addiction",
                "ENDURANCE" => "Endurance Skill: Physical resilience and health. Withstands damage and fatigue",
                "HALF_LIGHT" => "Half Light Skill: Violence and aggression. Thrives in dangerous and confrontational situations",
                "PAIN_THRESHOLD" => "Pain Threshold Skill: Tolerance to injury. Ignores pain and physical discomfort",
                "SHIVERS" => "Shivers Skill: Environmental awareness. Senses the city's mood and atmosphere",

                // Motorics Skills
                "HAND_EYE_COORDINATION" => "Hand/Eye Coordination Skill: Fine motor skills and precision. Useful for delicate tasks",
                "PERCEPTION" => "Perception Skill: Noticing details and hidden things. Spots clues and environmental features",
                "REACTION_SPEED" => "Reaction Speed Skill: Quick reflexes and timing. Helps in fast-paced situations",
                "SAVOIR_FAIRE" => "Savoir Faire Skill: Style and panache. Performs actions with flair and sophistication",
                "INTERFACING" => "Interfacing Skill: Technology and electronics. Operates computers and technical equipment",
                "COMPOSURE" => "Composure Skill: Staying calm under pressure. Maintains dignity in stressful situations",

                _ => $"{skillName.Replace('_', ' ')} Skill: Select to view details and allocate points"
            };
        }

        /// <summary>
        /// Handle skill point allocation buttons
        /// </summary>
        private static string GetSkillPointAllocationContext(GameObject uiObject)
        {
            try
            {
                if (uiObject == null) return null;

                var button = uiObject.GetComponent<Button>();
                if (button == null) return null;

                string buttonText = TextExtractor.ExtractBestTextContent(uiObject);
                
                // Look for + or - buttons that are part of skill allocation
                if (string.IsNullOrEmpty(buttonText) || (!buttonText.Contains("+") && !buttonText.Contains("-")))
                    return null;

                // Check if we're in a skill context by looking at parent hierarchy
                var parent = uiObject.transform.parent;
                while (parent != null)
                {
                    if (parent.name.Contains("SKILL") || 
                        (parent.parent != null && parent.parent.name == "Skills"))
                    {
                        string skillName = GetSkillNameFromHierarchy(parent.gameObject);
                        if (!string.IsNullOrEmpty(skillName))
                        {
                            string actionText = buttonText.Contains("+") ? "Increase" : "Decrease";
                            return $"{actionText} {skillName.Replace('_', ' ')} skill";
                        }
                        break;
                    }
                    parent = parent.parent;
                }

                // Check for attribute point allocation
                parent = uiObject.transform.parent;
                if (parent != null && parent.name == "Abilities")
                {
                    var grandparent = parent.parent;
                    if (grandparent != null && grandparent.name == "Leveling")
                    {
                        string attributeName = GetAttributeNameFromButton(uiObject);
                        if (!string.IsNullOrEmpty(attributeName))
                        {
                            string actionText = buttonText.Contains("+") ? "Increase" : "Decrease";
                            return $"{actionText} {attributeName} attribute";
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting skill point allocation context: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Get skill name from hierarchy
        /// </summary>
        private static string GetSkillNameFromHierarchy(GameObject skillObject)
        {
            try
            {
                var current = skillObject.transform;
                while (current != null)
                {
                    string name = current.name;
                    if (name.Contains("LOGIC") || name.Contains("ENCYCLOPEDIA") || name.Contains("RHETORIC") ||
                        name.Contains("DRAMA") || name.Contains("CONCEPTUALIZATION") || name.Contains("VISUAL_CALCULUS") ||
                        name.Contains("VOLITION") || name.Contains("INLAND_EMPIRE") || name.Contains("EMPATHY") ||
                        name.Contains("AUTHORITY") || name.Contains("SUGGESTION") || name.Contains("ESPRIT_DE_CORPS") ||
                        name.Contains("PHYSICAL_INSTRUMENT") || name.Contains("ELECTROCHEMISTRY") || name.Contains("ENDURANCE") ||
                        name.Contains("HALF_LIGHT") || name.Contains("PAIN_THRESHOLD") || name.Contains("SHIVERS") ||
                        name.Contains("HE_COORDINATION") || name.Contains("PERCEPTION") || name.Contains("REACTION") ||
                        name.Contains("SAVOIR_FAIRE") || name.Contains("INTERFACING") || name.Contains("COMPOSURE"))
                    {
                        return name;
                    }
                    current = current.parent;
                }
                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting skill name from hierarchy: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Get attribute name from button context
        /// </summary>
        private static string GetAttributeNameFromButton(GameObject buttonObject)
        {
            try
            {
                var parent = buttonObject.transform.parent;
                if (parent != null)
                {
                    string parentName = parent.name.ToLower();
                    if (parentName.Contains("intellect")) return "Intellect";
                    if (parentName.Contains("psyche")) return "Psyche";
                    if (parentName.Contains("physique")) return "Physique";
                    if (parentName.Contains("motorics")) return "Motorics";
                }

                // Check siblings for attribute names
                if (parent != null)
                {
                    foreach (Transform sibling in parent)
                    {
                        if (sibling.gameObject != buttonObject)
                        {
                            string name = sibling.name.ToLower();
                            if (name.Contains("intellect")) return "Intellect";
                            if (name.Contains("psyche")) return "Psyche";
                            if (name.Contains("physique")) return "Physique";
                            if (name.Contains("motorics")) return "Motorics";
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting attribute name from button: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Find displayed archetype description
        /// </summary>
        private static string FindDisplayedArchetypeDescription(GameObject archetypeObject, string archetypeName)
        {
            try
            {
                string description = TextExtractor.FindDisplayedDescription(
                    archetypeObject, 
                    text => TextExtractor.IsLikelyArchetypeDescriptionText(text, archetypeName), 
                    4, 
                    $"archetype {archetypeName}"
                );

                if (!string.IsNullOrEmpty(description))
                {
                    return $"{archetypeName} Archetype: {description}";
                }

                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error finding displayed archetype description: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Format archetype button with full information
        /// </summary>
        private static string FormatArchetypeButtonForSpeech(ArchetypeSelectMenuButton archetypeButton)
        {
            try
            {
                if (archetypeButton == null) return null;

                // Handle custom character button
                if (archetypeButton.isCustomCharacterButton)
                {
                    return "Custom Character: Create your own archetype with customizable attributes and skills";
                }

                // Get archetype data
                var archetype = archetypeButton.Archetype;
                if (archetype == null) return null;

                string archetypeName = "";
                string description = "";
                string signatureSkill = "";

                // Try to get localized archetype name
                try 
                {
                    if (archetypeButton.nameLocalization != null)
                    {
                        var nameText = archetypeButton.nameLocalization.GetComponent<TextMeshProUGUI>();
                        if (nameText != null && !string.IsNullOrEmpty(nameText.text))
                        {
                            archetypeName = nameText.text.Trim();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"Could not access nameLocalization: {ex.Message}");
                }

                // Try to get archetype description
                try
                {
                    if (archetypeButton.descriptionLocalization != null)
                    {
                        var descText = archetypeButton.descriptionLocalization.GetComponent<TextMeshProUGUI>();
                        if (descText != null && !string.IsNullOrEmpty(descText.text))
                        {
                            description = descText.text.Trim();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"Could not access descriptionLocalization: {ex.Message}");
                }

                // Try to get signature skill
                try
                {
                    if (archetypeButton.signatureSkillLocalization != null)
                    {
                        var skillText = archetypeButton.signatureSkillLocalization.GetComponent<TextMeshProUGUI>();
                        if (skillText != null && !string.IsNullOrEmpty(skillText.text))
                        {
                            signatureSkill = skillText.text.Trim();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"Could not access signatureSkillLocalization: {ex.Message}");
                }

                // Build comprehensive archetype information
                string result = "";
                
                if (!string.IsNullOrEmpty(archetypeName))
                {
                    result = $"{archetypeName} Archetype";
                }
                else
                {
                    result = "Character Archetype";
                }

                // Add description if available
                if (!string.IsNullOrEmpty(description))
                {
                    result += $": {description}";
                }

                // Add attribute information from archetype template
                if (archetype != null)
                {
                    result += $". Attributes - Intellect: {archetype.Intellect}, Psyche: {archetype.Psyche}, Physique: {archetype.Fysique}, Motorics: {archetype.Motorics}";
                }

                // Add signature skill if available
                if (!string.IsNullOrEmpty(signatureSkill))
                {
                    result += $". Signature skill: {signatureSkill}";
                }

                return result;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error formatting archetype button: {ex}");
                return "Character Archetype";
            }
        }

        /// <summary>
        /// Check if text is archetype-related
        /// </summary>
        private static bool IsArchetypeRelatedText(string text, GameObject uiObject)
        {
            if (string.IsNullOrEmpty(text)) return false;
            
            // Check for known archetype names
            string[] archetypeNames = { "Thinker", "Sensitive", "Physical", "Custom Character", "Create Your Own" };
            foreach (string archetype in archetypeNames)
            {
                if (text.IndexOf(archetype, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            
            // Check if we're in an archetype selection context by looking at parent hierarchy
            Transform current = uiObject.transform;
            while (current != null)
            {
                if (current.name.IndexOf("archetype", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    current.name.IndexOf("template", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
                current = current.parent;
            }
            
            return false;
        }

        /// <summary>
        /// Get archetype context from text
        /// </summary>
        private static string GetArchetypeContextFromText(string text)
        {
            return text.ToLower() switch
            {
                "thinker" => "Thinker Archetype: Intellectual approach with high logic and reasoning. Focuses on problem-solving and knowledge-based skills",
                "sensitive" => "Sensitive Archetype: Empathetic approach with high social and emotional intelligence. Excels at understanding people and situations",
                "physical" => "Physical Archetype: Athletic approach with high strength and endurance. Specializes in physical challenges and direct action",
                "custom character" or "create your own" => "Custom Character: Create your own archetype with customizable attributes and skills",
                _ => $"Character Archetype: {text}"
            };
        }

        #endregion
    }
}