using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MelonLoader;

namespace AccessibilityMod.UI
{
    public class UINavigationHandler
    {
        public static GameObject lastSelectedUIObject = null;
        public static string lastSpokenText = "";
        
        // Track dialog responses for better single option detection
        private static List<Il2Cpp.SunshineResponseButton> currentResponseButtons = new List<Il2Cpp.SunshineResponseButton>();
        private static float lastResponseCheckTime = 0f;
        private static readonly float RESPONSE_CHECK_INTERVAL = 0.5f; // Check for responses every 500ms
        
        // Dialog text scanning removed - now using OnConversationLine patch instead

        public void UpdateUINavigation()
        {
            try
            {
                // Only check EventSystem selection (controller/keyboard navigation)
                // Removed Selectable.Highlighted checking to prevent mouse hover announcements
                CheckCurrentUISelection();
                
                // Check for dialog response buttons periodically
                if (Time.time - lastResponseCheckTime > RESPONSE_CHECK_INTERVAL)
                {
                    CheckDialogResponses();
                    lastResponseCheckTime = Time.time;
                }
                
                // TODO: Proper dialog detection instead of scanning
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error updating UI navigation: {ex}");
            }
        }

        private static void CheckCurrentUISelection()
        {
            try
            {
                var eventSystem = EventSystem.current;
                if (eventSystem != null)
                {
                    var currentSelection = eventSystem.currentSelectedGameObject;
                    
                    if (currentSelection != lastSelectedUIObject)
                    {
                        lastSelectedUIObject = currentSelection;


                        // Check if this is a dialog response button selection
                        CheckForDialogSelection(currentSelection);

                        // Skip skill check buttons as they're handled by SkillCheckTooltipPatches
                        if (currentSelection != null)
                        {
                            var responseButton = currentSelection.GetComponent<Il2Cpp.SunshineResponseButton>();
                            if (responseButton != null && (responseButton.whiteCheck || responseButton.redCheck))
                            {
                                return; // Skill check buttons are handled by SkillCheckTooltipPatches
                            }
                        }

                        // Skip journal elements as they're handled by JournalPatches
                        if (currentSelection != null && currentSelection.GetComponent<Il2CppSunshine.Journal.JournalTaskUI>() != null)
                        {
                            return; // Journal elements are handled by their own patches
                        }

                        // Handle character sheet skill elements with delay
                        if (currentSelection != null)
                        {
                            var skillPanel = currentSelection.GetComponentInParent<Il2Cpp.SkillPortraitPanel>();
                            if (skillPanel != null)
                            {

                                // Schedule delayed skill announcement to allow game to update descriptions
                                MelonCoroutines.Start(DelayedSkillAnnouncement(currentSelection));
                                return;
                            }
                        }

                        // Extract text and format for speech with UI context
                        string speechText = UIElementFormatter.FormatUIElementForSpeech(currentSelection);

                        if (!string.IsNullOrEmpty(speechText))
                        {
                            TolkScreenReader.Instance.Speak(speechText, false);
                            lastSpokenText = speechText;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error checking EventSystem selection: {ex}");
            }
        }
        
        /// <summary>
        /// Check for dialog response buttons state changes and announce single response scenarios
        /// </summary>
        private static void CheckDialogResponses()
        {
            try
            {
                // Find all SunshineResponseButton objects in the scene
                var responseButtons = UnityEngine.Object.FindObjectsOfType<Il2Cpp.SunshineResponseButton>();
                
                if (responseButtons == null || responseButtons.Length == 0)
                {
                    // No responses, clear our tracking
                    if (currentResponseButtons.Count > 0)
                    {
                        currentResponseButtons.Clear();
                        DialogStateManager.OnConversationEnd();
                    }
                    return;
                }
                
                // Check if response buttons have changed
                bool hasChanged = responseButtons.Length != currentResponseButtons.Count;
                
                if (!hasChanged)
                {
                    // Check if any buttons are different
                    for (int i = 0; i < responseButtons.Length; i++)
                    {
                        if (!currentResponseButtons.Contains(responseButtons[i]))
                        {
                            hasChanged = true;
                            break;
                        }
                    }
                }
                
                if (hasChanged)
                {
                    // Update our tracking
                    currentResponseButtons.Clear();
                    List<string> responseTexts = new List<string>();
                    
                    foreach (var button in responseButtons)
                    {
                        if (button != null && button.gameObject.activeInHierarchy)
                        {
                            currentResponseButtons.Add(button);
                            
                            // Extract response text
                            string responseText = UIElementFormatter.FormatDialogResponseText(button);
                            if (!string.IsNullOrEmpty(responseText))
                            {
                                responseTexts.Add(responseText);
                            }
                        }
                    }
                    
                    // Notify DialogStateManager of available responses
                    DialogStateManager.OnResponsesUpdated(responseTexts);
                    
                    // Special handling for single response
                    if (responseTexts.Count == 1)
                    {
                        string singleResponse = responseTexts[0];
                        
                        // Check if this is a "Continue" type response
                        if (singleResponse.IndexOf("continue", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            singleResponse.IndexOf("next", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            singleResponse.Length < 50) // Short responses are often continue prompts
                        {
                            // Make sure it's announced even if not selected yet
                            if (singleResponse != lastSpokenText)
                            {
                                TolkScreenReader.Instance.Speak($"Single option: {singleResponse}", false);
                                lastSpokenText = singleResponse;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error checking dialog responses: {ex}");
            }
        }
        
        /// <summary>
        /// Check if the currently selected UI element is a dialog response button and notify DialogStateManager
        /// </summary>
        private static void CheckForDialogSelection(GameObject selectedObject)
        {
            if (selectedObject == null) return;
            
            try
            {
                // Check if the selected object is a SunshineResponseButton
                var responseButton = selectedObject.GetComponent<Il2Cpp.SunshineResponseButton>();
                if (responseButton != null)
                {
                    // Find this button's index in our current response buttons list
                    int index = currentResponseButtons.IndexOf(responseButton);
                    if (index >= 0)
                    {
                        DialogStateManager.OnResponseSelected(index);
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error checking dialog selection: {ex}");
            }
        }
        
        // Dialog text scanning removed - now using OnConversationLine patch in DialogSystemPatches instead
        
        /// <summary>
        /// Check if text component likely contains dialog
        /// </summary>
        private static bool IsLikelyDialogText(Il2CppTMPro.TextMeshProUGUI tmpText, string text)
        {
            // Skip response button text (we handle those separately)
            var responseButton = tmpText.GetComponentInParent<Il2Cpp.SunshineResponseButton>();
            if (responseButton != null) return false;
            
            // Skip very long text (likely descriptions or UI text)
            if (text.Length > 500) return false;
            
            // Look for dialog characteristics
            bool hasDialogLength = text.Length > 15 && text.Length < 300;
            bool hasQuotes = text.Contains("\"") || text.Contains(""") || text.Contains(""");
            bool hasPunctuation = text.Contains(".") || text.Contains("!") || text.Contains("?");
            bool isConversational = text.Contains(" you ") || text.Contains(" I ") || text.Contains(" we ");
            
            return hasDialogLength && (hasQuotes || hasPunctuation || isConversational);
        }
        
        /// <summary>
        /// Try to extract speaker name from text component context
        /// </summary>
        private static string ExtractSpeakerFromContext(Il2CppTMPro.TextMeshProUGUI tmpText)
        {
            try
            {
                // Look for speaker name in nearby text components
                var parent = tmpText.transform.parent;
                if (parent != null)
                {
                    // Check siblings for speaker name
                    var siblingTexts = parent.GetComponentsInChildren<Il2CppTMPro.TextMeshProUGUI>();
                    foreach (var sibling in siblingTexts)
                    {
                        if (sibling != tmpText && sibling != null && !string.IsNullOrEmpty(sibling.text))
                        {
                            string siblingText = sibling.text.Trim();
                            
                            // Look for speaker-like text (short, proper nouns)
                            if (siblingText.Length > 2 && siblingText.Length < 30 && 
                                char.IsUpper(siblingText[0]) && !siblingText.Contains(" "))
                            {
                                return siblingText;
                            }
                        }
                    }
                }
                
                return null; // No speaker found
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error extracting speaker from context: {ex}");
                return null;
            }
        }
        
        /// <summary>
        /// Format dialog text with speaker identification
        /// </summary>
        private static string FormatDialogWithSpeaker(string speakerName, string dialogText)
        {
            if (string.IsNullOrEmpty(speakerName))
            {
                return dialogText;
            }
            
            // Clean up speaker name and identify type
            string cleanSpeaker = speakerName.Replace("_", " ");
            
            // Check if it's a skill name
            if (IsSkillName(cleanSpeaker))
            {
                return $"{cleanSpeaker} skill: {dialogText}";
            }
            else if (cleanSpeaker.Equals("You", StringComparison.OrdinalIgnoreCase))
            {
                return $"You: {dialogText}";
            }
            else
            {
                return $"{cleanSpeaker} says: {dialogText}";
            }
        }
        
        /// <summary>
        /// Check if the speaker name is a skill
        /// </summary>
        private static bool IsSkillName(string speakerName)
        {
            string[] skillNames = {
                "Logic", "Encyclopedia", "Rhetoric", "Drama", "Conceptualization", "Visual Calculus",
                "Volition", "Inland Empire", "Empathy", "Authority", "Suggestion", "Esprit de Corps",
                "Physical Instrument", "Electrochemistry", "Endurance", "Half Light", "Pain Threshold", "Shivers",
                "Hand Eye Coordination", "Perception", "Reaction Speed", "Savoir Faire", "Interfacing", "Composure"
            };
            
            foreach (string skill in skillNames)
            {
                if (speakerName.IndexOf(skill, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Delayed skill announcement to allow game time to update descriptions
        /// </summary>
        private static IEnumerator DelayedSkillAnnouncement(GameObject skillSelection)
        {
            // Wait a bit for the game to update skill descriptions
            yield return new WaitForSeconds(0.1f);

            // Extract text and format for speech with UI context
            string speechText = UIElementFormatter.FormatUIElementForSpeech(skillSelection);

            if (!string.IsNullOrEmpty(speechText))
            {
                TolkScreenReader.Instance.Speak(speechText, false);
            }
        }

    }
}