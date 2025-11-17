using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Il2CppTMPro;
using Il2Cpp;
using Il2CppSunshine;
using Il2CppSunshine.Views;
using Il2CppSunshine.Metric;
using Il2CppDiscoPages.Elements.THC;
using Il2CppPages.Gameplay.THC;
using AccessibilityMod.Utils;
using MelonLoader;

namespace AccessibilityMod.UI
{
    /// <summary>
    /// Specialized handler for Thought Cabinet navigation and detailed announcements
    /// </summary>
    public class ThoughtCabinetNavigationHandler
    {
        private static Dictionary<GameObject, ThoughtInfo> thoughtCache = new Dictionary<GameObject, ThoughtInfo>();
        private static float lastCacheUpdate = 0f;
        private static readonly float CACHE_UPDATE_INTERVAL = 2f; // Update cache every 2 seconds

        private struct ThoughtInfo
        {
            public string name;
            public string description;
            public string state;
            public int researchTimeLeft;
            public bool isSlot;
            public bool isLocked;
        }

        /// <summary>
        /// Handle Thought Cabinet specific keyboard shortcuts
        /// </summary>
        public static void HandleThoughtCabinetInput()
        {
            try
            {
                if (!IsInThoughtCabinetView()) return;

                // Tab key - Read full thought description
                if (UnityEngine.Input.GetKeyDown(KeyCode.Tab))
                {
                    AnnounceFullThoughtDetails();
                }

                // F2 key - List all available thoughts
                if (UnityEngine.Input.GetKeyDown(KeyCode.F2))
                {
                    ListAvailableThoughts();
                }

                // F3 key - List equipped thoughts
                if (UnityEngine.Input.GetKeyDown(KeyCode.F3))
                {
                    ListEquippedThoughts();
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error handling thought cabinet input: {ex}");
            }
        }

        /// <summary>
        /// Update the thought cache if needed
        /// </summary>
        public static void UpdateThoughtCache()
        {
            try
            {
                if (Time.time - lastCacheUpdate < CACHE_UPDATE_INTERVAL) return;
                if (!IsInThoughtCabinetView()) return;

                thoughtCache.Clear();

                // Cache thought slots
                var thoughtSlots = UnityEngine.Object.FindObjectsOfType<ThoughtSlot>();
                foreach (var slot in thoughtSlots)
                {
                    if (slot != null && slot.gameObject.activeInHierarchy)
                    {
                        var info = ExtractThoughtSlotInfo(slot.gameObject);
                        thoughtCache[slot.gameObject] = info;
                    }
                }

                // Cache thought list items
                var oceanSlots = UnityEngine.Object.FindObjectsOfType<PageSystemThoughtOceanSlot>();
                foreach (var oceanSlot in oceanSlots)
                {
                    if (oceanSlot != null && oceanSlot.gameObject.activeInHierarchy)
                    {
                        var info = ExtractThoughtItemInfo(oceanSlot.gameObject);
                        thoughtCache[oceanSlot.gameObject] = info;
                    }
                }

                lastCacheUpdate = Time.time;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error updating thought cache: {ex}");
            }
        }

        /// <summary>
        /// Check if we're currently in a Thought Cabinet view
        /// </summary>
        private static bool IsInThoughtCabinetView()
        {
            try
            {
                var thoughtCabinetView = UnityEngine.Object.FindObjectOfType<Il2CppSunshine.Views.ThoughtCabinetView>();
                if (thoughtCabinetView != null && thoughtCabinetView.gameObject.activeInHierarchy)
                {
                    return true;
                }

                var thcPage = UnityEngine.Object.FindObjectOfType<THCPage>();
                return thcPage != null && thcPage.gameObject.activeInHierarchy;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error checking if in thought cabinet view: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Announce full details of currently selected thought
        /// </summary>
        private static void AnnounceFullThoughtDetails()
        {
            try
            {
                var eventSystem = EventSystem.current;
                if (eventSystem == null) return;

                var currentSelection = eventSystem.currentSelectedGameObject;
                if (currentSelection == null)
                {
                    TolkScreenReader.Instance.Speak("No thought selected", true);
                    return;
                }

                // Try to get detailed information
                string details = GetFullThoughtDetails(currentSelection);
                if (!string.IsNullOrEmpty(details))
                {
                    TolkScreenReader.Instance.Speak(details, true);
                }
                else
                {
                    TolkScreenReader.Instance.Speak("No detailed information available for this selection", true);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error announcing full thought details: {ex}");
            }
        }

        /// <summary>
        /// Get full detailed information about a thought
        /// </summary>
        private static string GetFullThoughtDetails(GameObject thoughtObject)
        {
            try
            {
                // Check cache first
                if (thoughtCache.ContainsKey(thoughtObject))
                {
                    var info = thoughtCache[thoughtObject];
                    return FormatFullThoughtInfo(info);
                }

                // Try to extract information directly
                var thoughtProject = thoughtObject.GetComponent<ThoughtCabinetProject>();
                if (thoughtProject != null)
                {
                    return FormatThoughtProjectDetails(thoughtProject);
                }

                // Try to find in parent hierarchy
                thoughtProject = thoughtObject.GetComponentInParent<ThoughtCabinetProject>();
                if (thoughtProject != null)
                {
                    return FormatThoughtProjectDetails(thoughtProject);
                }

                // Fallback to basic text extraction with context
                string basicText = TextExtractor.ExtractBestTextContent(thoughtObject);
                if (!string.IsNullOrEmpty(basicText))
                {
                    string context = GetThoughtContextFromHierarchy(thoughtObject);
                    if (!string.IsNullOrEmpty(context))
                    {
                        return $"{basicText} - {context}";
                    }
                    return basicText;
                }

                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting full thought details: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Format complete thought project details
        /// </summary>
        private static string FormatThoughtProjectDetails(ThoughtCabinetProject thoughtProject)
        {
            try
            {
                var details = new List<string>();

                // Add display name
                string displayName = thoughtProject.displayName;
                if (!string.IsNullOrEmpty(displayName))
                {
                    details.Add($"Thought: {displayName}");
                }

                // Add state information
                var state = thoughtProject.state;
                string stateDesc = GetThoughtStateDescription(state);
                details.Add($"Status: {stateDesc}");

                // Add description
                string description = thoughtProject.description;
                if (!string.IsNullOrEmpty(description))
                {
                    details.Add($"Description: {description}");
                }

                // Add research time information
                if (state == ThoughtState.COOKING)
                {
                    var timeLeft = thoughtProject.ResearchTimeLeft;
                    var totalTime = thoughtProject.ResearchTime;
                    if (timeLeft > 0 && totalTime > 0)
                    {
                        int progress = totalTime - timeLeft;
                        details.Add($"Research progress: {progress} of {totalTime} hours completed, {timeLeft} hours remaining");
                    }
                    else if (timeLeft > 0)
                    {
                        details.Add($"Research time remaining: {timeLeft} hours");
                    }
                }

                // Add completion description if available and relevant
                if (state == ThoughtState.DISCOVERED || state == ThoughtState.FIXED)
                {
                    string completionDesc = thoughtProject.completionDescription;
                    if (!string.IsNullOrEmpty(completionDesc))
                    {
                        details.Add($"Effect: {completionDesc}");
                    }
                }

                return string.Join(". ", details);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error formatting thought project details: {ex}");
                return "Error retrieving thought details";
            }
        }

        /// <summary>
        /// Get human-readable description of thought state
        /// </summary>
        private static string GetThoughtStateDescription(ThoughtState state)
        {
            switch (state)
            {
                case ThoughtState.UNKNOWN:
                    return "Unknown - not yet discovered";
                case ThoughtState.KNOWN:
                    return "Known - available for research";
                case ThoughtState.COOKING:
                    return "In progress - currently being researched";
                case ThoughtState.DISCOVERED:
                    return "Discovered - research completed, ready to equip";
                case ThoughtState.FIXED:
                    return "Equipped - active and providing bonuses";
                case ThoughtState.FORGOTTEN:
                    return "Forgotten - no longer available";
                default:
                    return state.ToString();
            }
        }


        /// <summary>
        /// List all available thoughts
        /// </summary>
        private static void ListAvailableThoughts()
        {
            try
            {
                UpdateThoughtCache();
                
                var availableThoughts = thoughtCache.Values
                    .Where(info => !info.isSlot && (info.state == "KNOWN" || info.state == "DISCOVERED"))
                    .Select(info => $"{info.name} - {GetThoughtStateDescription(ParseThoughtState(info.state))}")
                    .ToList();

                if (availableThoughts.Count == 0)
                {
                    TolkScreenReader.Instance.Speak("No available thoughts found", true);
                    return;
                }

                string announcement = $"Available thoughts: {availableThoughts.Count} found. " + 
                                    string.Join(". ", availableThoughts);
                
                TolkScreenReader.Instance.Speak(announcement, true);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error listing available thoughts: {ex}");
            }
        }

        /// <summary>
        /// List all equipped thoughts
        /// </summary>
        private static void ListEquippedThoughts()
        {
            try
            {
                UpdateThoughtCache();
                
                var equippedThoughts = thoughtCache.Values
                    .Where(info => info.state == "FIXED")
                    .Select(info => info.name)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToList();

                if (equippedThoughts.Count == 0)
                {
                    TolkScreenReader.Instance.Speak("No thoughts currently equipped", true);
                    return;
                }

                string announcement = $"Equipped thoughts: {equippedThoughts.Count} found. " + 
                                    string.Join(". ", equippedThoughts);
                
                TolkScreenReader.Instance.Speak(announcement, true);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error listing equipped thoughts: {ex}");
            }
        }


        /// <summary>
        /// Extract thought slot information
        /// </summary>
        private static ThoughtInfo ExtractThoughtSlotInfo(GameObject slotObject)
        {
            var info = new ThoughtInfo();
            try
            {
                info.isSlot = true;
                
                string text = TextExtractor.ExtractBestTextContent(slotObject);
                if (string.IsNullOrEmpty(text))
                {
                    info.name = "Empty slot";
                    info.isLocked = CheckIfSlotLocked(slotObject);
                }
                else
                {
                    info.name = text;
                    info.state = "FIXED"; // Assume equipped if has content
                }

                return info;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error extracting thought slot info: {ex}");
                return info;
            }
        }

        /// <summary>
        /// Extract thought item information
        /// </summary>
        private static ThoughtInfo ExtractThoughtItemInfo(GameObject itemObject)
        {
            var info = new ThoughtInfo();
            try
            {
                info.isSlot = false;
                info.name = TextExtractor.ExtractBestTextContent(itemObject);
                
                // Try to get more details from ThoughtCabinetProject
                var thoughtProject = itemObject.GetComponentInParent<ThoughtCabinetProject>();
                if (thoughtProject != null)
                {
                    info.name = thoughtProject.displayName;
                    info.description = thoughtProject.description;
                    info.state = thoughtProject.state.ToString();
                    info.researchTimeLeft = thoughtProject.ResearchTimeLeft;
                }

                return info;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error extracting thought item info: {ex}");
                return info;
            }
        }

        /// <summary>
        /// Check if a slot is locked
        /// </summary>
        private static bool CheckIfSlotLocked(GameObject slotObject)
        {
            try
            {
                // Look for lock indicators in the UI
                var textComponents = slotObject.GetComponentsInChildren<Il2CppTMPro.TextMeshProUGUI>();
                foreach (var textComp in textComponents)
                {
                    if (textComp != null && !string.IsNullOrEmpty(textComp.text))
                    {
                        string text = textComp.text.ToLower();
                        if (text.Contains("lock") || text.Contains("unlock") || text.Contains("skill point"))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error checking if slot locked: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Get additional context from object hierarchy
        /// </summary>
        private static string GetThoughtContextFromHierarchy(GameObject thoughtObject)
        {
            try
            {
                // Look for description text in siblings or nearby components
                var parent = thoughtObject.transform.parent;
                if (parent != null)
                {
                    var textComponents = parent.GetComponentsInChildren<Il2CppTMPro.TextMeshProUGUI>();
                    foreach (var textComp in textComponents)
                    {
                        if (textComp != null && textComp.gameObject != thoughtObject && !string.IsNullOrEmpty(textComp.text))
                        {
                            string text = textComp.text.Trim();
                            if (text.Length > 30 && text.Contains(" "))
                            {
                                return text;
                            }
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting thought context: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Format full thought information from cached data
        /// </summary>
        private static string FormatFullThoughtInfo(ThoughtInfo info)
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(info.name))
            {
                parts.Add(info.isSlot ? $"Thought slot: {info.name}" : $"Thought: {info.name}");
            }

            if (!string.IsNullOrEmpty(info.state))
            {
                string stateDesc = GetThoughtStateDescription(ParseThoughtState(info.state));
                parts.Add($"Status: {stateDesc}");
            }

            if (!string.IsNullOrEmpty(info.description))
            {
                parts.Add($"Description: {info.description}");
            }

            if (info.researchTimeLeft > 0)
            {
                parts.Add($"Research time remaining: {info.researchTimeLeft} hours");
            }

            if (info.isLocked)
            {
                parts.Add("This slot is locked - use skill points to unlock");
            }

            return parts.Count > 0 ? string.Join(". ", parts) : "No information available";
        }

        /// <summary>
        /// Parse string to ThoughtState enum
        /// </summary>
        private static ThoughtState ParseThoughtState(string state)
        {
            if (string.IsNullOrEmpty(state)) return ThoughtState.UNKNOWN;
            
            if (Enum.TryParse<ThoughtState>(state.ToUpper(), out ThoughtState result))
            {
                return result;
            }
            
            return ThoughtState.UNKNOWN;
        }
    }
}