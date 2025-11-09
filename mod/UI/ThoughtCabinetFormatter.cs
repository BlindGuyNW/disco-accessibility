using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AccessibilityMod.Utils;
using Il2Cpp;
using Il2CppDiscoPages.Elements.THC;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppPages.Gameplay.THC;
using Il2CppSunshine;
using Il2CppSunshine.Metric;
using Il2CppSunshine.Views;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AccessibilityMod.UI
{
    /// <summary>
    /// Specialized formatter for Thought Cabinet UI elements to provide detailed thought information
    /// </summary>
    public static class ThoughtCabinetFormatter
    {
        private static Dictionary<string, string> thoughtStateDescriptions = new Dictionary<
            string,
            string
        >()
        {
            { "UNKNOWN", "Unknown thought - not yet discovered" },
            { "KNOWN", "Known thought - available for research" },
            { "COOKING", "In progress - thought is being researched" },
            { "DISCOVERED", "Discovered - thought research completed" },
            { "FIXED", "Equipped - thought is active and providing bonuses" },
            { "FORGOTTEN", "Forgotten thought - no longer available" },
        };

        /// <summary>
        /// Check if we're currently in the Thought Cabinet and format the element appropriately
        /// </summary>
        public static string GetThoughtCabinetElementInfo(GameObject uiObject)
        {
            try
            {
                if (uiObject == null)
                    return null;

                // Check if we're in the Thought Cabinet view
                bool inThoughtCabinet = IsInThoughtCabinetView();

                if (!inThoughtCabinet)
                    return null;

                // Try to format different types of thought cabinet elements
                string thoughtSlotInfo = GetThoughtSlotInfo(uiObject);
                if (!string.IsNullOrEmpty(thoughtSlotInfo))
                {
                    return thoughtSlotInfo;
                }

                string thoughtListInfo = GetThoughtListItemInfo(uiObject);
                if (!string.IsNullOrEmpty(thoughtListInfo))
                {
                    return thoughtListInfo;
                }

                string slotUnlockInfo = GetSlotUnlockInfo(uiObject);
                if (!string.IsNullOrEmpty(slotUnlockInfo))
                {
                    return slotUnlockInfo;
                }

                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting thought cabinet element info: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Check if we're currently in a Thought Cabinet view
        /// </summary>
        private static bool IsInThoughtCabinetView()
        {
            try
            {
                // Look for ThoughtCabinetView component in the scene
                var thoughtCabinetView =
                    UnityEngine.Object.FindObjectOfType<Il2CppSunshine.Views.ThoughtCabinetView>();
                if (thoughtCabinetView != null && thoughtCabinetView.gameObject.activeInHierarchy)
                {
                    return true;
                }

                // Also check for THC page components
                var thcPage = UnityEngine.Object.FindObjectOfType<THCPage>();
                if (thcPage != null && thcPage.gameObject.activeInHierarchy)
                {
                    return true;
                }

                var thcListPage = UnityEngine.Object.FindObjectOfType<THCListPage>();
                if (thcListPage != null && thcListPage.gameObject.activeInHierarchy)
                {
                    return true;
                }

                var thcDetailsPage = UnityEngine.Object.FindObjectOfType<THCDetailsPage>();
                if (thcDetailsPage != null && thcDetailsPage.gameObject.activeInHierarchy)
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error checking if in thought cabinet view: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Get information about a thought slot (equipped thought or empty slot)
        /// </summary>
        private static string GetThoughtSlotInfo(GameObject uiObject)
        {
            try
            {
                // Check for ThoughtSlot component
                var thoughtSlot = uiObject.GetComponent<ThoughtSlot>();
                if (thoughtSlot != null)
                {
                    return FormatThoughtSlot(thoughtSlot);
                }

                // Check for PageSystemThoughtSlot component
                var pageThoughtSlot = uiObject.GetComponent<PageSystemThoughtSlot>();
                if (pageThoughtSlot != null)
                {
                    return FormatPageThoughtSlot(pageThoughtSlot);
                }

                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting thought slot info: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Get information about thought list items (thoughts in inventory/selection)
        /// </summary>
        private static string GetThoughtListItemInfo(GameObject uiObject)
        {
            try
            {
                // Check for ThoughtOnList component (the actual UI component)
                var thoughtOnList = uiObject.GetComponent<ThoughtOnList>();
                if (thoughtOnList == null)
                {
                    // Check parent for the component
                    thoughtOnList = uiObject.GetComponentInParent<ThoughtOnList>();
                }

                if (thoughtOnList != null)
                {
                    return FormatThoughtOnList(thoughtOnList);
                }

                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting thought list item info: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Get information about slot unlock requirements
        /// </summary>
        private static string GetSlotUnlockInfo(GameObject uiObject)
        {
            try
            {
                string basicText = TextExtractor.ExtractBestTextContent(uiObject);

                // Look for slot unlock related text
                if (!string.IsNullOrEmpty(basicText))
                {
                    if (
                        basicText.ToLower().Contains("unlock")
                        || basicText.ToLower().Contains("skill point")
                        || basicText.ToLower().Contains("locked")
                    )
                    {
                        // Check if this is a buyable slot
                        var thoughtSlot = uiObject.GetComponentInParent<ThoughtSlot>();
                        if (thoughtSlot != null)
                        {
                            string slotContext = GetSlotUnlockContext(thoughtSlot);
                            if (!string.IsNullOrEmpty(slotContext))
                            {
                                return $"{basicText} - {slotContext}";
                            }
                        }

                        return $"Slot unlock: {basicText}";
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting slot unlock info: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Format a ThoughtSlot component
        /// </summary>
        private static string FormatThoughtSlot(ThoughtSlot thoughtSlot)
        {
            try
            {
                if (thoughtSlot == null)
                    return null;

                // Get the actual slot state from the component
                var slotState = thoughtSlot.State;
                var slotIndex = thoughtSlot.SlotIndex;

                switch (slotState)
                {
                    case ThoughtSlot.SlotState.LOCKED:
                        return $"Locked thought slot {slotIndex + 1} - unlock more slots by advancing skills";

                    case ThoughtSlot.SlotState.BUYABLE:
                        // Try to get the cost to unlock this slot
                        string unlockInfo = GetSlotUnlockCost(slotIndex);
                        return $"Unlockable thought slot {slotIndex + 1} - {unlockInfo}";

                    case ThoughtSlot.SlotState.OPEN:
                        return $"Empty thought slot {slotIndex + 1} - drag a thought here to equip it";

                    case ThoughtSlot.SlotState.FILLED:
                        // Get the equipped thought
                        var project = thoughtSlot.Project;
                        if (project != null)
                        {
                            string mechanicalEffects = GetMechanicalEffectsWithFlavor(project, project.state, thoughtSlot.gameObject);
                            string effects = !string.IsNullOrEmpty(mechanicalEffects)
                                ? $" - Effects: {mechanicalEffects}"
                                : "";
                            return $"Equipped thought: {project.displayName}{effects}";
                        }
                        else
                        {
                            return $"Thought slot {slotIndex + 1} - contains unknown thought";
                        }

                    case ThoughtSlot.SlotState.FIXTURE:
                        // Permanent thoughts that can't be removed
                        var fixtureProject = thoughtSlot.Project;
                        if (fixtureProject != null)
                        {
                            return $"Fixed thought: {fixtureProject.displayName} - permanent thought";
                        }
                        else
                        {
                            return $"Fixed thought slot {slotIndex + 1}";
                        }

                    default:
                        return $"Thought slot {slotIndex + 1} - {slotState}";
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error formatting thought slot: {ex}");
                return "Thought slot (error getting details)";
            }
        }

        /// <summary>
        /// Format a PageSystemThoughtSlot component
        /// </summary>
        private static string FormatPageThoughtSlot(PageSystemThoughtSlot pageThoughtSlot)
        {
            try
            {
                if (pageThoughtSlot == null)
                    return null;

                // Try to get slot state if available
                var slotState = pageThoughtSlot._State_k__BackingField;
                var project = pageThoughtSlot._Project_k__BackingField;

                if (project != null)
                {
                    string mechanicalEffects = GetMechanicalEffectsWithFlavor(project, project.state, pageThoughtSlot.gameObject);
                    string effects = !string.IsNullOrEmpty(mechanicalEffects)
                        ? $" - Effects: {mechanicalEffects}"
                        : "";
                    return $"Thought: {project.displayName}{effects}";
                }
                else
                {
                    switch (slotState)
                    {
                        case ThoughtSlot.SlotState.LOCKED:
                            return "Locked thought slot - advance skills to unlock more slots";
                        case ThoughtSlot.SlotState.BUYABLE:
                            return "Unlockable thought slot - use skill points to unlock";
                        case ThoughtSlot.SlotState.OPEN:
                            return "Empty thought slot - drag a thought here";
                        default:
                            return "Thought slot";
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error formatting page thought slot: {ex}");
                return "Thought slot (error getting details)";
            }
        }

        /// <summary>
        /// Format a ThoughtOnList component (actual thought UI element)
        /// </summary>
        private static string FormatThoughtOnList(ThoughtOnList thoughtOnList)
        {
            try
            {
                if (thoughtOnList == null)
                    return null;

                string announcement = "";

                // Get the thought project (like slot.item in inventory)
                var project = thoughtOnList._Project_k__BackingField;
                if (project != null)
                {
                    // Get thought name
                    string thoughtName = project.displayName;
                    if (string.IsNullOrEmpty(thoughtName))
                    {
                        thoughtName = "Unknown Thought";
                    }

                    // Get thought state
                    var state = project.state;
                    string stateDesc = GetThoughtStateDescription(state.ToString());

                    // Get thought description
                    string description = project.description;

                    // Build announcement like inventory does
                    announcement = $"Thought: {thoughtName}";

                    announcement += $" - Status: {stateDesc}";

                    // Add research time information if relevant
                    if (state == ThoughtState.COOKING || state == ThoughtState.KNOWN)
                    {
                        var timeLeft = project.ResearchTimeLeft;
                        var totalTime = project.ResearchTime;
                        if (timeLeft > 0 && totalTime > 0)
                        {
                            int progress = totalTime - timeLeft;
                            float percentage = (float)progress / totalTime * 100f;
                            announcement +=
                                $" - Research: {percentage:F0}% complete, {timeLeft} minutes remaining";
                        }
                        else if (timeLeft > 0)
                        {
                            announcement += $" - {timeLeft} minutes remaining";
                        }
                        else if (totalTime > 0)
                        {
                            announcement += $" - Research time: {totalTime} minutes";
                        }
                    }

                    // Add actual mechanical effects FIRST (most important info)
                    string mechanicalEffects = GetMechanicalEffectsWithFlavor(project, state, thoughtOnList.gameObject);
                    if (!string.IsNullOrEmpty(mechanicalEffects))
                    {
                        announcement += $" - Effects: {mechanicalEffects}";
                    }

                    // Add description last (it's usually long flavor text)
                    if (!string.IsNullOrEmpty(description) && description != thoughtName)
                    {
                        announcement += $" - Problem: {description}";
                    }

                    // Add solution if thought is fixed or discovered
                    if (
                        (state == ThoughtState.DISCOVERED || state == ThoughtState.FIXED)
                        && !string.IsNullOrEmpty(project.completionDescription)
                    )
                    {
                        announcement += $"\n- Solution: {project.completionDescription}";
                    }
                }
                else
                {
                    announcement = "Thought (no project data)";
                }

                return announcement;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error formatting thought on list: {ex}");
                return "Thought (error getting details)";
            }
        }

        /// <summary>
        /// Format a ThoughtListItem component (thought in list/inventory) - DEPRECATED
        /// </summary>
        private static string FormatThoughtListItem(ThoughtListItem thoughtListItem)
        {
            try
            {
                if (thoughtListItem == null)
                    return null;

                string announcement = "";

                // Get the thought project (like slot.item in inventory)
                var project = thoughtListItem.project;
                if (project != null)
                {
                    // Get thought name
                    string thoughtName = project.displayName;
                    if (string.IsNullOrEmpty(thoughtName))
                    {
                        thoughtName = thoughtListItem.name; // Fallback to ThoughtListItem name
                    }
                    if (string.IsNullOrEmpty(thoughtName))
                    {
                        thoughtName = "Unknown Thought";
                    }

                    // Get thought state
                    var state = project.state;
                    string stateDesc = GetThoughtStateDescription(state.ToString());

                    // Get thought description
                    string description = project.description;

                    // Build announcement like inventory does
                    announcement = $"Thought: {thoughtName}";

                    announcement += $" - Status: {stateDesc}";

                    // Add research time information if relevant
                    if (state == ThoughtState.COOKING)
                    {
                        var timeLeft = project.ResearchTimeLeft;
                        var totalTime = project.ResearchTime;
                        if (timeLeft > 0 && totalTime > 0)
                        {
                            int progress = totalTime - timeLeft;
                            float percentage = (float)progress / totalTime * 100f;
                            announcement +=
                                $" - Research: {percentage:F0}% complete, {timeLeft} minutes remaining";
                        }
                        else if (timeLeft > 0)
                        {
                            announcement += $" - {timeLeft} minutes remaining";
                        }
                    }

                    // Add actual mechanical effects FIRST (most important info)
                    string mechanicalEffects = GetMechanicalEffects(project, state);
                    if (!string.IsNullOrEmpty(mechanicalEffects))
                    {
                        announcement += $" - Effects: {mechanicalEffects}";
                    }

                    // Add description last (it's usually long flavor text)
                    if (!string.IsNullOrEmpty(description) && description != thoughtName)
                    {
                        announcement += $" - Description: {description}";
                    }
                }
                else
                {
                    // No project - just use the name from ThoughtListItem
                    string thoughtName = thoughtListItem.name;
                    if (!string.IsNullOrEmpty(thoughtName))
                    {
                        announcement = $"Thought: {thoughtName}";
                    }
                    else
                    {
                        announcement = "Unknown thought";
                    }
                }

                return announcement;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error formatting thought list item: {ex}");
                return "Thought (error getting details)";
            }
        }

        /// <summary>
        /// Find thought information from object hierarchy
        /// </summary>
        private static ThoughtCabinetProject FindThoughtInfoFromHierarchy(GameObject uiObject)
        {
            try
            {
                // Try to find ThoughtCabinetProject component in parents
                var transform = uiObject.transform;
                while (transform != null)
                {
                    var project = transform.GetComponent<ThoughtCabinetProject>();
                    if (project != null)
                    {
                        return project;
                    }
                    transform = transform.parent;
                }

                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error finding thought info from hierarchy: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Format thought with detailed information
        /// </summary>
        private static string FormatThoughtWithDetails(
            ThoughtCabinetProject thoughtProject,
            string basicText
        )
        {
            try
            {
                if (thoughtProject == null)
                    return basicText;

                string result = $"Thought: {basicText}";

                // Add thought state information
                var state = thoughtProject.state;
                string stateDesc = GetThoughtStateDescription(state.ToString());
                result += $" - Status: {stateDesc}";

                // Add description if available
                string description = thoughtProject.description;
                if (!string.IsNullOrEmpty(description))
                {
                    result += $" - {description}";
                }

                // Add research time information if relevant
                if (state == ThoughtState.COOKING)
                {
                    var timeLeft = thoughtProject.ResearchTimeLeft;
                    if (timeLeft > 0)
                    {
                        result += $" - {timeLeft} hours remaining";
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error formatting thought with details: {ex}");
                return basicText;
            }
        }

        /// <summary>
        /// Find thought description from UI hierarchy
        /// </summary>
        private static string FindThoughtDescription(GameObject uiObject)
        {
            try
            {
                // Look for text components that might contain descriptions
                var textComponents =
                    uiObject.GetComponentsInChildren<Il2CppTMPro.TextMeshProUGUI>();
                foreach (var textComp in textComponents)
                {
                    if (textComp != null && !string.IsNullOrEmpty(textComp.text))
                    {
                        string text = textComp.text.Trim();
                        // Look for longer text that might be descriptions
                        if (text.Length > 50 && text.Contains(" "))
                        {
                            return text;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error finding thought description: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Find thought state from UI
        /// </summary>
        private static string FindThoughtState(GameObject uiObject)
        {
            try
            {
                // This would require more investigation of the actual UI structure
                // For now, we'll return null and rely on other methods
                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error finding thought state: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Get human-readable description of thought state
        /// </summary>
        private static string GetThoughtStateDescription(string state)
        {
            if (string.IsNullOrEmpty(state))
                return "Unknown status";

            if (thoughtStateDescriptions.ContainsKey(state.ToUpper()))
            {
                return thoughtStateDescriptions[state.ToUpper()];
            }

            return state;
        }

        /// <summary>
        /// Extract mechanical effects from a thought project (without flavor text)
        /// </summary>
        private static string GetMechanicalEffects(
            ThoughtCabinetProject project,
            ThoughtState state
        )
        {
            try
            {
                var effects = new List<string>();

                // Get relevant effects based on thought state
                Il2CppReferenceArray<CharacterEffect> effectArray = null;

                if (state == ThoughtState.COOKING && project.researchEffects != null)
                {
                    effectArray = project.researchEffects;
                }
                else if (
                    (state == ThoughtState.DISCOVERED || state == ThoughtState.FIXED)
                    && project.completionEffects != null
                )
                {
                    effectArray = project.completionEffects;
                }

                if (effectArray != null && effectArray.Count > 0)
                {
                    for (int i = 0; i < effectArray.Count; i++)
                    {
                        var effect = effectArray[i];
                        if (effect != null)
                        {
                            string effectDesc = FormatCharacterEffect(effect);
                            if (!string.IsNullOrEmpty(effectDesc))
                            {
                                effects.Add(effectDesc);
                            }
                        }
                    }
                }

                return effects.Count > 0 ? string.Join(", ", effects) : null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting mechanical effects: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Extract mechanical effects with flavor text from UI
        /// </summary>
        private static string GetMechanicalEffectsWithFlavor(
            ThoughtCabinetProject project,
            ThoughtState state,
            GameObject uiObject
        )
        {
            try
            {
                var effects = new List<string>();

                // Get relevant effects based on thought state
                Il2CppReferenceArray<CharacterEffect> effectArray = null;

                if (state == ThoughtState.COOKING && project.researchEffects != null)
                {
                    effectArray = project.researchEffects;
                }
                else if (
                    (state == ThoughtState.DISCOVERED || state == ThoughtState.FIXED)
                    && project.completionEffects != null
                )
                {
                    effectArray = project.completionEffects;
                }

                if (effectArray != null && effectArray.Count > 0)
                {
                    // Try to extract effect-flavor mapping from tooltip
                    var effectFlavorMap = ExtractEffectFlavorTextFromTooltip();

                    for (int i = 0; i < effectArray.Count; i++)
                    {
                        var effect = effectArray[i];
                        if (effect != null)
                        {
                            string effectDesc = FormatCharacterEffect(effect);
                            if (!string.IsNullOrEmpty(effectDesc))
                            {
                                // Try to find matching flavor text from the tooltip
                                if (effectFlavorMap.ContainsKey(effectDesc))
                                {
                                    string flavorText = effectFlavorMap[effectDesc];
                                    effectDesc = $"{effectDesc}: {flavorText}";
                                }
                                effects.Add(effectDesc);
                            }
                        }
                    }
                }

                return effects.Count > 0 ? string.Join(", ", effects) : null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting mechanical effects with flavor: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Extract effect texts from the tooltip PropertiesText component
        /// </summary>
        private static Dictionary<string, string> ExtractEffectFlavorTextFromTooltip()
        {
            var effectFlavorMap = new Dictionary<string, string>();
            try
            {
                // Try to find ThoughtCabinetTooltip with PropertiesText
                var tooltip = UnityEngine.Object.FindObjectOfType<Il2CppSunshine.ThoughtCabinetTooltip>();
                if (tooltip != null && tooltip.gameObject.activeInHierarchy)
                {
                    var tooltipTexts = tooltip.gameObject.GetComponentsInChildren<TextMeshProUGUI>(true);
                    if (tooltipTexts != null)
                    {
                        foreach (var textComp in tooltipTexts)
                        {
                            if (textComp != null && !string.IsNullOrEmpty(textComp.text) &&
                                textComp.gameObject.name == "PropertiesText")
                            {
                                string text = textComp.text;

                                // Parse the properties text which contains lines like:
                                // "+2 Volition: Magnesium receptacle glands"
                                // "-1 Logic: No such thing, man"
                                var lines = text.Split('\n');
                                foreach (var line in lines)
                                {
                                    var trimmedLine = line.Trim();
                                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.Contains("Bonuses from"))
                                        continue;

                                    // Remove color tags
                                    var cleanLine = Regex.Replace(
                                        trimmedLine,
                                        @"</?color[^>]*>",
                                        ""
                                    );

                                    // Parse format: "+2 Volition: Magnesium receptacle glands"
                                    var colonIndex = cleanLine.IndexOf(':');
                                    if (colonIndex > 0)
                                    {
                                        var effectPart = cleanLine.Substring(0, colonIndex).Trim();
                                        var flavorPart = cleanLine.Substring(colonIndex + 1).Trim();
                                        effectFlavorMap[effectPart] = flavorPart;
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error extracting effect flavor text from tooltip: {ex}");
            }
            return effectFlavorMap;
        }

        /// <summary>
        /// Extract effect texts from the UI hierarchy (old method, kept for fallback)
        /// </summary>
        private static List<string> ExtractEffectTextsFromUI(GameObject uiObject)
        {
            var texts = new List<string>();
            try
            {
                if (uiObject == null)
                    return texts;

                // Search for text components in the UI object and its children
                var allTexts = uiObject.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (allTexts != null)
                {
                    foreach (var textComp in allTexts)
                    {
                        if (textComp != null && !string.IsNullOrEmpty(textComp.text))
                        {
                            string text = textComp.text.Trim();
                            if (text.Length > 5 && !text.StartsWith("+") && !text.StartsWith("-") &&
                                !text.All(c => char.IsDigit(c) || char.IsWhiteSpace(c) || c == '+' || c == '-'))
                            {
                                texts.Add(text);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error extracting effect texts from UI: {ex}");
            }
            return texts;
        }


        /// <summary>
        /// Format a single CharacterEffect into human-readable text
        /// </summary>
        private static string FormatCharacterEffect(CharacterEffect effect)
        {
            try
            {
                if (effect == null)
                    return null;

                // Use the game's own EffectName method - this gives us the mechanical effect
                string effectName = effect.EffectName(false, false, false, true);

                // Filter out empty, null, or technical effects
                if (
                    string.IsNullOrEmpty(effectName)
                    || effectName.Contains("LUA_")
                    || effectName.Contains("COMMAND")
                    || effectName.Trim() == "0"
                    || effectName.Contains("+0 ")
                    || effectName.Contains("-0 ")
                )
                {
                    return null;
                }

                return effectName.Trim();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error formatting character effect: {ex}");

                // Fallback to EffectFullName if EffectName fails
                try
                {
                    string fullName = effect.EffectFullName();
                    if (!string.IsNullOrEmpty(fullName) && !fullName.Contains("LUA_"))
                    {
                        return fullName.Trim();
                    }
                }
                catch
                {
                    // If both fail, return null
                }

                return null;
            }
        }

        /// <summary>
        /// Get the cost to unlock a specific slot
        /// </summary>
        private static string GetSlotUnlockCost(int slotIndex)
        {
            try
            {
                // Try to find the ThoughtManager to get unlock costs
                var thoughtManager = UnityEngine.Object.FindObjectOfType<ThoughtManager>();
                if (thoughtManager != null && thoughtManager.CostUnlockSlot != null)
                {
                    if (slotIndex >= 0 && slotIndex < thoughtManager.CostUnlockSlot.Count)
                    {
                        int cost = thoughtManager.CostUnlockSlot[slotIndex];
                        return $"costs {cost} skill point{(cost != 1 ? "s" : "")} to unlock";
                    }
                }

                // Fallback to general guidance
                return "use skill points to unlock - check character sheet for available points";
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting slot unlock cost: {ex}");
                return "use skill points to unlock";
            }
        }

        /// <summary>
        /// Get context for slot unlock requirements
        /// </summary>
        private static string GetSlotUnlockContext(ThoughtSlot thoughtSlot)
        {
            try
            {
                // This would need investigation of how slot unlock costs are determined
                // For now, provide general guidance
                return "Use skill points to unlock additional thought slots. Check your character sheet for available skill points.";
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting slot unlock context: {ex}");
                return null;
            }
        }
    }
}
