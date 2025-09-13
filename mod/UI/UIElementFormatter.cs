using System;
using UnityEngine;
using Il2Cpp;
using AccessibilityMod.Utils;
using MelonLoader;

namespace AccessibilityMod.UI
{
    /// <summary>
    /// Main UI element formatter that delegates to specialized formatters
    /// </summary>
    public static class UIElementFormatter
    {
        /// <summary>
        /// Format interactable for speech (for 3D world objects)
        /// </summary>
        public static string FormatInteractableForSpeech(Il2Cpp.CommonPadInteractable interactable)
        {
            try
            {
                if (interactable == null) return null;
                
                string speechText = "";
                
                // Get game entity name first (most descriptive)
                var gameEntity = interactable.GetGameEntity();
                if (gameEntity != null && !string.IsNullOrEmpty(gameEntity.name))
                {
                    speechText = ObjectNameCleaner.CleanObjectName(gameEntity.name);
                }
                
                // If no entity name, try to get object name
                if (string.IsNullOrEmpty(speechText))
                {
                    var orb = interactable.Orb;
                    var mouseOverHighlight = interactable.Interactable;
                    
                    if (orb != null)
                    {
                        var transform = orb.transform;
                        if (transform != null && !string.IsNullOrEmpty(transform.gameObject.name))
                        {
                            speechText = "Orb: " + ObjectNameCleaner.CleanObjectName(transform.gameObject.name);
                        }
                    }
                    else if (mouseOverHighlight != null)
                    {
                        var transform = mouseOverHighlight.transform;
                        if (transform != null && !string.IsNullOrEmpty(transform.gameObject.name))
                        {
                            speechText = ObjectNameCleaner.CleanObjectName(transform.gameObject.name);
                        }
                    }
                }
                
                // Add type information if we have something
                if (!string.IsNullOrEmpty(speechText))
                {
                    var interactableType = interactable.CurrentType();
                    string typeStr = interactableType.ToString();
                    if (!speechText.ToLower().Contains(typeStr.ToLower()) && !speechText.StartsWith("Orb:"))
                    {
                        if (interactableType == Il2Cpp.InteractableType.ORB)
                        {
                            speechText = $"Orb: {speechText}";
                        }
                    }
                }
                
                return speechText;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error formatting interactable for speech: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Format UI element for speech (main entry point for UI elements)
        /// </summary>
        public static string FormatUIElementForSpeech(GameObject uiObject)
        {
            try
            {
                if (uiObject == null) return null;
                
                // Try dialog-related formatting first (highest priority)
                string dialogText = DialogFormatter.GetDialogResponseText(uiObject);
                if (!string.IsNullOrEmpty(dialogText))
                {
                    return dialogText;
                }
                
                // Check for confirmation dialogs (high priority)
                string confirmationText = DialogFormatter.GetConfirmationTextContext(uiObject);
                if (!string.IsNullOrEmpty(confirmationText))
                {
                    return confirmationText;
                }
                
                // Check for Thought Cabinet elements (high priority)
                string thoughtCabinetText = ThoughtCabinetFormatter.GetThoughtCabinetElementInfo(uiObject);
                if (!string.IsNullOrEmpty(thoughtCabinetText))
                {
                    return thoughtCabinetText;
                }
                
                // Journal elements are filtered out at UINavigationHandler level
                
                // Check for character creation elements
                string archetypeText = CharacterCreationFormatter.GetArchetypeInformation(uiObject);
                if (!string.IsNullOrEmpty(archetypeText))
                {
                    return archetypeText;
                }
                
                string characterCreationText = CharacterCreationFormatter.GetCharacterCreationContext(uiObject);
                if (!string.IsNullOrEmpty(characterCreationText))
                {
                    return characterCreationText;
                }
                
                string skillText = CharacterCreationFormatter.GetSkillSelectionContext(uiObject);
                if (!string.IsNullOrEmpty(skillText))
                {
                    return skillText;
                }
                
                // Extract basic text content
                string speechText = TextExtractor.ExtractBestTextContent(uiObject);
                
                // If we have no text content, try specialized UI components that might not have text
                if (string.IsNullOrEmpty(speechText))
                {
                    // Check for sliders without text labels
                    var sliderComponent = uiObject.GetComponent<UnityEngine.UI.Slider>();
                    if (sliderComponent != null)
                    {
                        return GameUIFormatter.GetEnhancedSliderInfo(sliderComponent, uiObject);
                    }
                    
                    return null;
                }
                
                speechText = speechText.Trim();
                
                // Format standard UI components with context
                string formattedComponent = GameUIFormatter.FormatStandardUIComponent(uiObject, speechText);
                if (!formattedComponent.Equals(speechText))
                {
                    return formattedComponent;
                }
                
                // Check for confirmation button context
                var button = uiObject.GetComponent<UnityEngine.UI.Button>();
                if (button != null)
                {
                    string confirmationContext = DialogFormatter.GetConfirmationButtonContext(button, uiObject);
                    if (!string.IsNullOrEmpty(confirmationContext))
                    {
                        return confirmationContext;
                    }
                }
                
                // Default: return the basic text content
                return speechText;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error formatting UI element for speech: {ex}");
                return TextExtractor.ExtractBestTextContent(uiObject);
            }
        }

        /// <summary>
        /// Format dialog response text (delegated method for backward compatibility)
        /// </summary>
        public static string FormatDialogResponseText(Il2Cpp.SunshineResponseButton responseButton)
        {
            return DialogFormatter.FormatDialogResponseText(responseButton);
        }

        /// <summary>
        /// Extract text from any GameObject (delegated method for backward compatibility)
        /// </summary>
        public static string ExtractTextFromGameObject(GameObject obj)
        {
            return TextExtractor.ExtractBestTextContent(obj);
        }

        /// <summary>
        /// Extract best text content (delegated method for backward compatibility)
        /// </summary>
        public static string ExtractBestTextContent(GameObject uiObject)
        {
            return TextExtractor.ExtractBestTextContent(uiObject);
        }
    }
}