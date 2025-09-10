using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Il2CppTMPro;
using Il2Cpp;
using Il2CppSunshine;
using Il2CppSunshine.Metric;
using Il2CppDiscoPages.Elements.MainMenu;
using Il2CppPages.MainMenu;
using Il2CppI2.Loc;
using Il2CppCollageMode.Scripts.Localization;
using AccessibilityMod.Utils;
using MelonLoader;

namespace AccessibilityMod.UI
{
    public static class UIElementFormatter
    {
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

        public static string ExtractBestTextContent(GameObject uiObject)
        {
            try
            {
                if (uiObject == null) return null;
                
                // Try direct text components first
                var textComponent = uiObject.GetComponent<Text>();
                if (textComponent != null && !string.IsNullOrEmpty(textComponent.text))
                {
                    return textComponent.text.Trim();
                }
                
                var tmpText = uiObject.GetComponent<TextMeshProUGUI>();
                if (tmpText != null && !string.IsNullOrEmpty(tmpText.text))
                {
                    return tmpText.text.Trim();
                }
                
                var tmpTextPro = uiObject.GetComponent<TextMeshPro>();
                if (tmpTextPro != null && !string.IsNullOrEmpty(tmpTextPro.text))
                {
                    return tmpTextPro.text.Trim();
                }
                
                // Try child text components (for buttons, etc.)
                var childText = uiObject.GetComponentInChildren<Text>();
                if (childText != null && !string.IsNullOrEmpty(childText.text))
                {
                    return childText.text.Trim();
                }
                
                var childTMP = uiObject.GetComponentInChildren<TextMeshProUGUI>();
                if (childTMP != null && !string.IsNullOrEmpty(childTMP.text))
                {
                    return childTMP.text.Trim();
                }
                
                var childTMPPro = uiObject.GetComponentInChildren<TextMeshPro>();
                if (childTMPPro != null && !string.IsNullOrEmpty(childTMPPro.text))
                {
                    return childTMPPro.text.Trim();
                }
                
                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error extracting text content: {ex}");
                return null;
            }
        }

        public static string FormatUIElementForSpeech(GameObject uiObject)
        {
            try
            {
                if (uiObject == null) return null;
                
                // Check for Disco Elysium dialog response buttons first (highest priority)
                string dialogText = GetDialogResponseText(uiObject);
                if (!string.IsNullOrEmpty(dialogText))
                {
                    return dialogText;
                }
                
                // Check for confirmation dialog elements (high priority)
                string confirmationText = GetConfirmationTextContext(uiObject);
                if (!string.IsNullOrEmpty(confirmationText))
                {
                    return confirmationText;
                }
                
                // Check for archetype selection buttons (character creation context)
                string archetypeText = GetArchetypeInformation(uiObject);
                if (!string.IsNullOrEmpty(archetypeText))
                {
                    return archetypeText;
                }
                
                // Check for character creation attribute context
                string characterCreationText = GetCharacterCreationContext(uiObject);
                if (!string.IsNullOrEmpty(characterCreationText))
                {
                    return characterCreationText;
                }
                
                // Check for skill selection context
                string skillText = GetSkillSelectionContext(uiObject);
                if (!string.IsNullOrEmpty(skillText))
                {
                    return skillText;
                }
                
                // Enhanced slider support with context detection
                var slider = uiObject.GetComponent<Slider>();
                if (slider != null)
                {
                    string sliderInfo = GetEnhancedSliderInfo(slider, uiObject);
                    if (!string.IsNullOrEmpty(sliderInfo))
                    {
                        return sliderInfo;
                    }
                }
                
                // Extract text content for standard components
                string speechText = ExtractBestTextContent(uiObject);
                
                // Handle sliders that don't have text content (common case)
                if (string.IsNullOrEmpty(speechText))
                {
                    var sliderComponent = uiObject.GetComponent<Slider>();
                    if (sliderComponent != null)
                    {
                        string sliderInfo = GetEnhancedSliderInfo(sliderComponent, uiObject);
                        if (!string.IsNullOrEmpty(sliderInfo))
                        {
                            return sliderInfo;
                        }
                    }
                    return null;
                }
                
                speechText = speechText.Trim();
                
                // Check for Disco Elysium's OptionDropbox component (base class for many dropdowns)
                var optionDropbox = uiObject.GetComponent<OptionDropbox>();
                if (optionDropbox != null)
                {
                    string dropdownName = GetDropdownName(optionDropbox, uiObject);
                    if (!string.IsNullOrEmpty(dropdownName))
                    {
                        return $"{dropdownName}: {speechText}";
                    }
                    else
                    {
                        return $"Dropdown: {speechText}";
                    }
                }
                
                // Add UI element type context for standard components
                var button = uiObject.GetComponent<Button>();
                if (button != null)
                {
                    // Check if this button is part of a confirmation dialog
                    string confirmationContext = GetConfirmationButtonContext(button, uiObject);
                    if (!string.IsNullOrEmpty(confirmationContext))
                    {
                        return confirmationContext;
                    }
                    
                    return $"Button: {speechText}";
                }
                
                var toggle = uiObject.GetComponent<Toggle>();
                if (toggle != null)
                {
                    return $"Toggle {(toggle.isOn ? "checked" : "unchecked")}: {speechText}";
                }
                
                // Enhanced slider handling with text context
                var sliderWithText = uiObject.GetComponent<Slider>();
                if (sliderWithText != null)
                {
                    string sliderInfo = GetEnhancedSliderInfo(sliderWithText, uiObject);
                    if (!string.IsNullOrEmpty(sliderInfo))
                    {
                        // If we have both slider info and text, combine them
                        return string.IsNullOrEmpty(speechText) ? sliderInfo : $"{sliderInfo} - {speechText}";
                    }
                    
                    // Fallback for sliders we couldn't identify
                    int percentage = Mathf.RoundToInt(sliderWithText.normalizedValue * 100);
                    return $"Slider {percentage}%: {speechText}";
                }
                
                // Check for TextMesh Pro dropdown (for dropdowns that don't use OptionDropbox)
                var tmpDropdown = uiObject.GetComponent<TMP_Dropdown>();
                if (tmpDropdown != null)
                {
                    string dropdownName = GetTMPDropdownName(tmpDropdown, uiObject);
                    string prefix = !string.IsNullOrEmpty(dropdownName) ? dropdownName : "Dropdown";
                    
                    // For dropdowns, speechText should already be the selected value
                    return $"{prefix}: {speechText}";
                }
                
                // Check for standard Unity dropdown (fallback)
                var dropdown = uiObject.GetComponent<Dropdown>();
                if (dropdown != null)
                {
                    return $"Dropdown: {speechText}";
                }
                
                // Default: just return the text without additional context
                return speechText;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error formatting UI element for speech: {ex}");
                return ExtractBestTextContent(uiObject);
            }
        }

        private static string GetDropdownName(OptionDropbox optionDropbox, GameObject uiObject)
        {
            try
            {
                // First check if settingName is already meaningful
                string settingName = optionDropbox.settingName;
                if (!string.IsNullOrEmpty(settingName))
                {
                    // Fix spacing in "VoiceOver Mode" if needed
                    if (settingName.Equals("VoiceOverMode", StringComparison.OrdinalIgnoreCase))
                    {
                        settingName = "Voice Over Mode";
                    }
                    
                    // Some settings already have good names
                    string lowerSetting = settingName.ToLower();
                    if (lowerSetting.Contains("mode") || lowerSetting.Contains("option") || 
                        lowerSetting.Contains("setting") || lowerSetting.Contains("language"))
                    {
                        var voModeOption = optionDropbox as VoiceOverModeOption;
                        if (voModeOption != null)
                        {
                            return "Voice Over Mode";
                        }
                        
                        return settingName;
                    }
                }
                
                // Check specific dropdown types
                var displayMode = optionDropbox as DisplayModeOption;
                if (displayMode != null)
                {
                    return "Display Mode";
                }
                
                // Check parent name for context
                var parent = uiObject.transform.parent;
                if (parent != null)
                {
                    string parentName = parent.name.ToLower();
                    if (parentName.Contains("language")) return "Language";
                    if (parentName.Contains("resolution")) return "Resolution";
                    if (parentName.Contains("display")) return "Display Mode";
                    if (parentName.Contains("voiceover")) return "VoiceOver Mode";
                    if (parentName.Contains("graphics")) return "Graphics Option";
                }
                
                return !string.IsNullOrEmpty(settingName) ? settingName : null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting dropdown name: {ex}");
                return null;
            }
        }

        private static string GetConfirmationButtonContext(Button button, GameObject uiObject)
        {
            try
            {
                var confirmationController = UnityEngine.Object.FindObjectOfType<ConfirmationController>();
                if (confirmationController == null || !confirmationController.IsVisible)
                {
                    return null;
                }
                
                // Check if this button is the Confirm button
                if (confirmationController.Confirm == button)
                {
                    string message = "";
                    if (confirmationController.Text != null)
                    {
                        message = confirmationController.Text.text;
                    }
                    
                    return !string.IsNullOrEmpty(message) ? $"Confirm: {message}" : "Confirm Button";
                }
                
                // Check if this button is the Cancel button
                if (confirmationController.Cancel == button)
                {
                    string message = "";
                    if (confirmationController.Text != null)
                    {
                        message = confirmationController.Text.text;
                    }
                    
                    return !string.IsNullOrEmpty(message) ? $"Cancel: {message}" : "Cancel Button";
                }
                
                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting confirmation button context: {ex}");
                return null;
            }
        }

        private static string GetConfirmationTextContext(GameObject uiObject)
        {
            try
            {
                var confirmationController = UnityEngine.Object.FindObjectOfType<ConfirmationController>();
                if (confirmationController == null || !confirmationController.IsVisible)
                {
                    return null;
                }
                
                // Check if this is the main text of the confirmation dialog
                var textComponent = uiObject.GetComponent<Text>();
                if (textComponent != null && confirmationController.Text == textComponent)
                {
                    return $"Confirmation: {textComponent.text}";
                }
                
                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting confirmation text context: {ex}");
                return null;
            }
        }

        private static string GetTMPDropdownName(TMP_Dropdown dropdown, GameObject uiObject)
        {
            try
            {
                // Check if this is the Resolution dropdown
                var resolutionSwitcher = UnityEngine.Object.FindObjectOfType<ResolutionSwitcher>();
                if (resolutionSwitcher != null && resolutionSwitcher._dd == dropdown)
                {
                    return "Resolution";
                }
                
                // Check GameObject name and parent for context
                string objName = uiObject.name.ToLower();
                if (objName.Contains("resolution")) return "Resolution";
                if (objName.Contains("language")) return "Language";
                if (objName.Contains("display")) return "Display Mode";
                
                var parent = uiObject.transform.parent;
                if (parent != null)
                {
                    string parentName = parent.name.ToLower();
                    if (parentName.Contains("resolution")) return "Resolution";
                    if (parentName.Contains("language")) return "Language";
                    if (parentName.Contains("display")) return "Display Mode";
                }
                
                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting TMP dropdown name: {ex}");
                return null;
            }
        }

        private static string GetEnhancedSliderInfo(Slider slider, GameObject uiObject)
        {
            try
            {
                if (slider == null || uiObject == null) return null;
                
                string sliderName = "Slider";
                string value = "";
                
                // Try to get slider name from various sources
                sliderName = TryGetSliderNameFromSingletons(slider, uiObject) ?? 
                            TryGetSliderNameFromParent(uiObject) ?? 
                            TryGetSliderNameFromGameObject(uiObject) ?? 
                            "Slider";
                
                // Format the value contextually
                value = FormatSliderValue(slider, sliderName);
                
                return $"{sliderName}: {value}";
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting enhanced slider info: {ex}");
                // Fallback to basic slider info
                int percentage = Mathf.RoundToInt(slider.normalizedValue * 100);
                return $"Slider: {percentage}%";
            }
        }

        private static string TryGetSliderNameFromSingletons(Slider slider, GameObject uiObject)
        {
            try
            {
                // Check AudioConfigurationSliders singleton
                var audioConfig = UnityEngine.Object.FindObjectOfType<AudioConfigurationSliders>();
                if (audioConfig != null)
                {
                    if (audioConfig.spatialsourceSlider == slider) return "Spatial Volume";
                    if (audioConfig.uiSlider == slider) return "UI Volume"; 
                    if (audioConfig.musicSlider == slider) return "Music Volume";
                    if (audioConfig.voiceoverSlider == slider) return "Voiceover Volume";
                    if (audioConfig.weatherSlider == slider) return "Weather Volume";
                }
                
                // Check other configuration singletons
                var layoutConfig = UnityEngine.Object.FindObjectOfType<LayoutProfileConfiguration>();
                if (layoutConfig != null && layoutConfig.slider == slider)
                {
                    return "UI Layout Scale";
                }
                
                var textConfig = UnityEngine.Object.FindObjectOfType<TextSizeConfiguration>();
                if (textConfig != null && textConfig.slider == slider)
                {
                    return "Text Size";
                }
                
                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error checking slider singletons: {ex}");
                return null;
            }
        }

        private static string TryGetSliderNameFromParent(GameObject uiObject)
        {
            try
            {
                var parent = uiObject.transform.parent;
                if (parent != null)
                {
                    string parentName = parent.name.ToLower();
                    
                    // Common slider parent patterns in options screens
                    if (parentName.Contains("volume")) return "Volume";
                    if (parentName.Contains("audio")) return "Audio";
                    if (parentName.Contains("sound")) return "Sound";
                    if (parentName.Contains("music")) return "Music";
                    if (parentName.Contains("gamma")) return "Gamma";
                    if (parentName.Contains("brightness")) return "Brightness";
                    if (parentName.Contains("scale") || parentName.Contains("size")) return "UI Scale";
                    if (parentName.Contains("graphics")) return "Graphics Setting";
                    
                    // Clean up and return parent name as fallback
                    return ObjectNameCleaner.CleanObjectName(parent.name) + " Slider";
                }
                
                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting slider name from parent: {ex}");
                return null;
            }
        }

        private static string TryGetSliderNameFromGameObject(GameObject uiObject)
        {
            try
            {
                string objName = uiObject.name.ToLower();
                
                // Check for common slider naming patterns
                if (objName.Contains("volume")) return "Volume Slider";
                if (objName.Contains("audio")) return "Audio Slider";
                if (objName.Contains("music")) return "Music Slider";
                if (objName.Contains("gamma")) return "Gamma Slider";
                if (objName.Contains("brightness")) return "Brightness Slider";
                if (objName.Contains("scale")) return "Scale Slider";
                
                // Generic fallback with cleaned name
                if (objName.Contains("slider"))
                {
                    return ObjectNameCleaner.CleanObjectName(uiObject.name);
                }
                
                return ObjectNameCleaner.CleanObjectName(uiObject.name) + " Slider";
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting slider name from GameObject: {ex}");
                return "Slider";
            }
        }

        private static string FormatSliderValue(Slider slider, string sliderName)
        {
            try
            {
                float value = slider.value;
                float normalizedValue = slider.normalizedValue;
                int percentage = Mathf.RoundToInt(normalizedValue * 100);
                
                string lowerName = sliderName.ToLower();
                
                // Volume sliders: show percentage
                if (lowerName.Contains("volume"))
                {
                    return $"{percentage}%";
                }
                
                // UI Layout Scale: show step-based value
                if (lowerName.Contains("ui layout scale") || lowerName.Contains("layout"))
                {
                    int step = Mathf.RoundToInt(value) + 1;
                    int maxSteps = Mathf.RoundToInt(slider.maxValue) + 1;
                    return $"{step} of {maxSteps}";
                }
                
                // Text Size: show named sizes
                if (lowerName.Contains("text size"))
                {
                    try
                    {
                        var textConfig = UnityEngine.Object.FindObjectOfType<TextSizeConfiguration>();
                        if (textConfig != null)
                        {
                            var currentTextSize = TextSizeConfiguration.CurrTextSize;
                            string sizeName = currentTextSize.ToString();
                            
                            if (!string.IsNullOrEmpty(sizeName))
                            {
                                sizeName = char.ToUpper(sizeName[0]) + sizeName.Substring(1).ToLower();
                            }
                            
                            int textStep = Mathf.RoundToInt(value) + 1;
                            int textMaxSteps = Mathf.RoundToInt(slider.maxValue) + 1;
                            return $"{sizeName} ({textStep} of {textMaxSteps})";
                        }
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Error($"Error getting text size name: {ex}");
                    }
                    
                    // Fallback to step-based
                    int textFallbackStep = Mathf.RoundToInt(value) + 1;
                    int textFallbackMaxSteps = Mathf.RoundToInt(slider.maxValue) + 1;
                    return $"{textFallbackStep} of {textFallbackMaxSteps}";
                }
                
                // Graphics settings: try to show meaningful ranges
                if (lowerName.Contains("gamma") || lowerName.Contains("brightness"))
                {
                    return $"{value:F1}";
                }
                
                // Default: show both percentage and raw value if they're different
                if (Mathf.Abs(value - percentage) > 0.1f)
                {
                    return $"{value:F1} ({percentage}%)";
                }
                
                return $"{percentage}%";
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error formatting slider value: {ex}");
                return $"{Mathf.RoundToInt(slider.normalizedValue * 100)}%";
            }
        }

        private static string GetDialogResponseText(GameObject uiObject)
        {
            try
            {
                if (uiObject == null) return null;

                // Check for SunshineResponseButton component (Disco Elysium's dialog choices)
                var responseButton = uiObject.GetComponent<Il2Cpp.SunshineResponseButton>();
                if (responseButton != null)
                {
                    return FormatDialogResponseText(responseButton);
                }

                // Also check if this might be a child of a response button
                var parentResponseButton = uiObject.GetComponentInParent<Il2Cpp.SunshineResponseButton>();
                if (parentResponseButton != null)
                {
                    return FormatDialogResponseText(parentResponseButton);
                }

                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting dialog response text: {ex}");
                return null;
            }
        }

        public static string FormatDialogResponseText(Il2Cpp.SunshineResponseButton responseButton)
        {
            try
            {
                if (responseButton == null) return null;

                string dialogText = "";
                string skillCheckInfo = "";

                // Extract the main dialog text from optionText
                if (responseButton.optionText != null)
                {
                    // Try to get text from the textField component
                    if (responseButton.optionText.textField != null && !string.IsNullOrEmpty(responseButton.optionText.textField.text))
                    {
                        dialogText = responseButton.optionText.textField.text.Trim();
                    }
                    // Fallback to originalText property
                    else if (!string.IsNullOrEmpty(responseButton.optionText.originalText))
                    {
                        dialogText = responseButton.optionText.originalText.Trim();
                    }
                }

                // Check for skill check information
                bool isWhiteCheck = responseButton.whiteCheck;
                bool isRedCheck = responseButton.redCheck;

                if (isWhiteCheck || isRedCheck)
                {
                    // Try to extract skill check percentage and details
                    string checkType = isWhiteCheck ? "White Check" : "Red Check";
                    string skillDetails = "";
                    
                    // Look for skill check details in the response button
                    try
                    {
                        // Check if there are percentage or difficulty indicators in the UI
                        var parent = responseButton.transform.parent;
                        if (parent != null)
                        {
                            // Look for text components that might contain percentage or difficulty
                            var textComponents = parent.GetComponentsInChildren<Il2CppTMPro.TextMeshProUGUI>();
                            foreach (var textComp in textComponents)
                            {
                                if (textComp != null && !string.IsNullOrEmpty(textComp.text))
                                {
                                    string text = textComp.text.Trim();
                                    // Look for percentage patterns like "75%" or difficulty like "Very Easy"
                                    if (text.Contains("%") || 
                                        text.Contains("Easy") || text.Contains("Medium") || text.Contains("Hard") ||
                                        text.Contains("Impossible") || text.Contains("Trivial") ||
                                        text.Contains("Challenging") || text.Contains("Legendary"))
                                    {
                                        skillDetails = text;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Fallback to basic check type
                    }
                    
                    // Format skill check info
                    if (!string.IsNullOrEmpty(skillDetails))
                    {
                        skillCheckInfo = $"{checkType} ({skillDetails})";
                    }
                    else
                    {
                        skillCheckInfo = checkType;
                    }
                }

                // Check if we should skip dialog text to avoid interrupting dialog reading
                bool skipDialogText = DialogStateManager.IsInConversation() && DialogStateManager.IsDialogReadingEnabled;
                
                // Combine skill check info with dialog text
                if (!string.IsNullOrEmpty(skillCheckInfo) && !string.IsNullOrEmpty(dialogText))
                {
                    if (skipDialogText)
                    {
                        // Only announce skill check info, skip dialog text to avoid interruption
                        return skillCheckInfo;
                    }
                    else
                    {
                        // Check if dialog text already contains skill check details to avoid duplication
                        if (dialogText.Contains("[") && dialogText.Contains("]") && dialogText.Contains("-"))
                        {
                            // Dialog text already has skill check details, just prefix with check type
                            string checkType = isWhiteCheck ? "White Check" : "Red Check";
                            return $"{checkType}: {dialogText}";
                        }
                        else
                        {
                            return $"{skillCheckInfo}: {dialogText}";
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(skillCheckInfo))
                {
                    return skillCheckInfo;
                }
                else if (!string.IsNullOrEmpty(dialogText))
                {
                    if (skipDialogText)
                    {
                        // Skip dialog text entirely when dialog reading is active
                        return null;
                    }
                    else
                    {
                        return $"Dialog: {dialogText}";
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error formatting dialog response text: {ex}");
                return null;
            }
        }

        private static string GetArchetypeInformation(GameObject uiObject)
        {
            try
            {
                if (uiObject == null) return null;

                // Check if this might be archetype-related based on context or text content
                string basicText = ExtractBestTextContent(uiObject);
                if (!string.IsNullOrEmpty(basicText) && IsArchetypeRelatedText(basicText, uiObject))
                {
                    // Found archetype-related text
                    
                    // Use the same approach as skill descriptions - read what's displayed on screen
                    string displayedDescription = FindDisplayedArchetypeDescription(uiObject, basicText);
                    if (!string.IsNullOrEmpty(displayedDescription))
                    {
                        // Found displayed archetype description
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

        private static string FindDisplayedArchetypeDescription(GameObject archetypeObject, string archetypeName)
        {
            try
            {
                // Looking for displayed archetype description
                
                // Search in the archetype object and its broader hierarchy for description text
                var candidates = new List<GameObject> { archetypeObject };
                
                // Add parent objects that might contain the description area
                var current = archetypeObject.transform;
                for (int i = 0; i < 4 && current != null; i++) // Go up 4 levels for archetype descriptions
                {
                    current = current.parent;
                    if (current != null)
                    {
                        candidates.Add(current.gameObject);
                    }
                }
                
                // Search each candidate for descriptive text
                foreach (var candidate in candidates)
                {
                    string description = SearchForArchetypeDescriptionText(candidate, archetypeName);
                    if (!string.IsNullOrEmpty(description))
                    {
                        return description;
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error finding displayed archetype description: {ex}");
                return null;
            }
        }
        
        private static string SearchForArchetypeDescriptionText(GameObject parent, string archetypeName)
        {
            try
            {
                // Get all text components in this object and its children
                var allTextComponents = new List<Component>();
                
                // Add TextMeshProUGUI components
                var tmpComponents = parent.GetComponentsInChildren<TextMeshProUGUI>();
                if (tmpComponents != null)
                {
                    allTextComponents.AddRange(tmpComponents);
                }
                
                // Add regular Text components  
                var textComponents = parent.GetComponentsInChildren<Text>();
                if (textComponents != null)
                {
                    allTextComponents.AddRange(textComponents);
                }
                
                // Searching text components for archetype description
                
                foreach (var component in allTextComponents)
                {
                    string text = null;
                    
                    if (component is TextMeshProUGUI tmpText && tmpText != null)
                    {
                        text = tmpText.text;
                    }
                    else if (component is Text regularText && regularText != null)
                    {
                        text = regularText.text;
                    }
                    
                    if (!string.IsNullOrEmpty(text))
                    {
                        // Checking text for archetype description pattern
                        
                        // Look for archetype description patterns
                        if (IsLikelyArchetypeDescriptionText(text, archetypeName))
                        {
                            return $"{archetypeName} Archetype: {text.Trim()}";
                        }
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error searching for archetype description text: {ex}");
                return null;
            }
        }
        
        private static bool IsLikelyArchetypeDescriptionText(string text, string archetypeName)
        {
            if (string.IsNullOrEmpty(text) || text.Length < 20)
                return false;
                
            // Skip if it's just the archetype name itself
            if (text.Trim().Equals(archetypeName, StringComparison.OrdinalIgnoreCase))
                return false;
                
            // Skip pure numbers or very short text
            if (text.Trim().Length < 15 || text.All(c => char.IsDigit(c) || char.IsWhiteSpace(c)))
                return false;
                
            // Skip common UI text that's not descriptions  
            string lowerText = text.ToLower();
            if (lowerText.Contains("button") || lowerText.Contains("select") || 
                lowerText.Contains("click") || lowerText.Contains("archetype") && lowerText.Length < 30)
                return false;
                
            // Look for description-like content - complete sentences or archetype-related keywords
            return lowerText.Contains("approach") || lowerText.Contains("focuses") ||
                   lowerText.Contains("specializes") || lowerText.Contains("excels") ||
                   lowerText.Contains("strength") || lowerText.Contains("abilities") ||
                   lowerText.Contains("intellect") || lowerText.Contains("empathy") ||
                   lowerText.Contains("physical") || lowerText.Contains("reasoning") ||
                   text.Contains(".") && text.Split('.').Length > 1; // Multiple sentences
        }

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

        private static string GetCharacterCreationContext(GameObject uiObject)
        {
            try
            {
                if (uiObject == null) return null;

                // Check if we're in character creation by looking for specific parent hierarchy
                var parent = uiObject.transform.parent;
                if (parent != null && parent.name == "Abilities")
                {
                    var grandparent = parent.parent;
                    if (grandparent != null && grandparent.name == "Leveling")
                    {
                        // This is a character creation attribute element
                        string attributeName = uiObject.name;
                        string speechText = ExtractBestTextContent(uiObject);
                        
                        if (!string.IsNullOrEmpty(speechText) && !string.IsNullOrEmpty(attributeName))
                        {
                            // Try to get displayed stat description first
                            string displayedDescription = FindDisplayedStatDescription(uiObject, attributeName);
                            if (!string.IsNullOrEmpty(displayedDescription))
                            {
                                // Found displayed stat description
                                
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

        private static string FindDisplayedStatDescription(GameObject statObject, string statName)
        {
            try
            {
                // Looking for displayed stat description
                
                // Search in the stat object and its broader hierarchy for description text
                var candidates = new List<GameObject> { statObject };
                
                // Add parent objects that might contain the description area
                var current = statObject.transform;
                for (int i = 0; i < 4 && current != null; i++) // Go up 4 levels for stat descriptions
                {
                    current = current.parent;
                    if (current != null)
                    {
                        candidates.Add(current.gameObject);
                    }
                }
                
                // Search each candidate for descriptive text
                foreach (var candidate in candidates)
                {
                    string description = SearchForStatDescriptionText(candidate, statName);
                    if (!string.IsNullOrEmpty(description))
                    {
                        return description;
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error finding displayed stat description: {ex}");
                return null;
            }
        }
        
        private static string SearchForStatDescriptionText(GameObject parent, string statName)
        {
            try
            {
                // Get all text components in this object and its children
                var allTextComponents = new List<Component>();
                
                // Add TextMeshProUGUI components
                var tmpComponents = parent.GetComponentsInChildren<TextMeshProUGUI>();
                if (tmpComponents != null)
                {
                    allTextComponents.AddRange(tmpComponents);
                }
                
                // Add regular Text components  
                var textComponents = parent.GetComponentsInChildren<Text>();
                if (textComponents != null)
                {
                    allTextComponents.AddRange(textComponents);
                }
                
                // Searching text components for stat description
                
                foreach (var component in allTextComponents)
                {
                    string text = null;
                    
                    if (component is TextMeshProUGUI tmpText && tmpText != null)
                    {
                        text = tmpText.text;
                    }
                    else if (component is Text regularText && regularText != null)
                    {
                        text = regularText.text;
                    }
                    
                    if (!string.IsNullOrEmpty(text))
                    {
                        // Checking text for stat description pattern
                        
                        // Look for stat description patterns
                        if (IsLikelyStatDescriptionText(text, statName))
                        {
                            return text.Trim();
                        }
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error searching for stat description text: {ex}");
                return null;
            }
        }
        
        private static bool IsLikelyStatDescriptionText(string text, string statName)
        {
            if (string.IsNullOrEmpty(text) || text.Length < 15)
                return false;
                
            // Skip if it's just the stat name itself
            if (text.Trim().Equals(statName, StringComparison.OrdinalIgnoreCase))
                return false;
                
            // Skip pure numbers or very short text
            if (text.All(c => char.IsDigit(c) || char.IsWhiteSpace(c)))
                return false;
                
            // Skip common UI text that's not descriptions  
            string lowerText = text.ToLower();
            if (lowerText.Contains("button") || lowerText.Contains("select") || 
                lowerText.Contains("point") && lowerText.Length < 20)
                return false;
                
            // Look for description-like content - stat-related keywords
            return lowerText.Contains("affects") || lowerText.Contains("determines") ||
                   lowerText.Contains("governs") || lowerText.Contains("influences") ||
                   lowerText.Contains("skills") || lowerText.Contains("abilities") ||
                   lowerText.Contains("logic") || lowerText.Contains("reasoning") ||
                   lowerText.Contains("empathy") || lowerText.Contains("physical") ||
                   lowerText.Contains("coordination") || lowerText.Contains("dexterity") ||
                   text.Contains(".") && text.Split('.').Length > 1; // Multiple sentences
        }

        private static string GetAttributeDescription(string attributeName)
        {
            switch (attributeName.ToLower())
            {
                case "intellect":
                    return " - affects logic, reasoning, and knowledge-based skills";
                case "psyche":
                    return " - affects empathy, composure, and social interaction skills";
                case "physique":
                    return " - affects physical strength, endurance, and combat abilities";
                case "motorics":
                    return " - affects dexterity, coordination, and perception skills";
                default:
                    return "";
            }
        }

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

        private static string GetArchetypeContextFromText(string text)
        {
            switch (text.ToLower())
            {
                case "thinker":
                    return "Thinker Archetype: Intellectual approach with high logic and reasoning. Focuses on problem-solving and knowledge-based skills";
                case "sensitive":
                    return "Sensitive Archetype: Empathetic approach with high social and emotional intelligence. Excels at understanding people and situations";  
                case "physical":
                    return "Physical Archetype: Athletic approach with high strength and endurance. Specializes in physical challenges and direct action";
                case "custom character":
                case "create your own":
                    return "Custom Character: Create your own archetype with customizable attributes and skills";
                default:
                    return $"Character Archetype: {text}";
            }
        }

        private static string GetSkillSelectionContext(GameObject uiObject)
        {
            try
            {
                if (uiObject == null) return null;

                // Check if this is a skill selection button (Parent: SKILL_NAME, Grandparent: Skills)
                var parent = uiObject.transform.parent;
                if (parent != null && parent.parent != null && 
                    parent.parent.name == "Skills" && uiObject.name == "Select Button")
                {
                    string skillName = parent.name;
                    // Detected skill selection
                    
                    // Try to get rich description from game data first
                    string gameDescription = GetGameSkillDescription(parent.gameObject, skillName);
                    if (!string.IsNullOrEmpty(gameDescription))
                    {
                        return gameDescription;
                    }
                    
                    // Fallback to our descriptions if game data not available
                    return GetSkillDescription(skillName);
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

        private static string GetSkillPointAllocationContext(GameObject uiObject)
        {
            try
            {
                if (uiObject == null) return null;

                // Check if this is a skill point allocation button by checking button text and hierarchy
                var button = uiObject.GetComponent<Button>();
                if (button == null) return null;

                string buttonText = ExtractBestTextContent(uiObject);
                
                // Look for + or - buttons that are part of skill allocation
                if (string.IsNullOrEmpty(buttonText) || (!buttonText.Contains("+") && !buttonText.Contains("-")))
                    return null;

                // Check if we're in a skill context by looking at parent hierarchy
                var parent = uiObject.transform.parent;
                while (parent != null)
                {
                    // Look for skill-related parent objects
                    if (parent.name.Contains("SKILL") || 
                        (parent.parent != null && parent.parent.name == "Skills"))
                    {
                        string skillName = GetSkillNameFromHierarchy(parent.gameObject);
                        if (!string.IsNullOrEmpty(skillName))
                        {
                            // Try to get current skill level and points available
                            string pointInfo = GetSkillPointInfo(parent.gameObject, skillName);
                            
                            string actionText = buttonText.Contains("+") ? "Increase" : "Decrease";
                            string result = $"{actionText} {skillName.Replace('_', ' ')} skill";
                            
                            if (!string.IsNullOrEmpty(pointInfo))
                            {
                                result += $" - {pointInfo}";
                            }
                            
                            return result;
                        }
                        break;
                    }
                    parent = parent.parent;
                }

                // Check if we're in character creation attribute allocation
                parent = uiObject.transform.parent;
                if (parent != null && parent.name == "Abilities")
                {
                    var grandparent = parent.parent;
                    if (grandparent != null && grandparent.name == "Leveling")
                    {
                        // This might be an attribute point allocation button
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

        private static string GetSkillNameFromHierarchy(GameObject skillObject)
        {
            try
            {
                // Try to find the skill name in the hierarchy
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

        private static string GetSkillPointInfo(GameObject skillObject, string skillName)
        {
            try
            {
                // Try to find current skill level and available points
                // Look for text components that might show current values
                var textComponents = skillObject.GetComponentsInChildren<TextMeshProUGUI>();
                foreach (var text in textComponents)
                {
                    if (text != null && !string.IsNullOrEmpty(text.text))
                    {
                        string textContent = text.text.Trim();
                        
                        // Look for numeric values that might represent skill points
                        if (System.Text.RegularExpressions.Regex.IsMatch(textContent, @"^\d+$"))
                        {
                            return $"Current level: {textContent}";
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting skill point info: {ex}");
                return null;
            }
        }

        private static string GetAttributeNameFromButton(GameObject buttonObject)
        {
            try
            {
                // Try to find attribute name by looking at siblings or parent names
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

        private static string GetGameSkillDescription(GameObject skillParent, string skillName)
        {
            // Getting game skill description
            try
            {
                if (skillParent == null)
                {
                    MelonLogger.Warning($"skillParent is null for {skillName}");
                    return null;
                }
                
                // The descriptions are already displayed on screen! Just find and read them.
                string displayedDescription = FindDisplayedSkillDescription(skillParent, skillName);
                if (!string.IsNullOrEmpty(displayedDescription))
                {
                    // Found displayed skill description
                    return displayedDescription;
                }
                
                // Fallback to hardcoded descriptions if we can't find the displayed ones
                return GetSkillDescription(skillName);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"EXCEPTION in GetGameSkillDescription for {skillName}: {ex.GetType().Name} - {ex.Message}");
                MelonLogger.Error($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }
        
        private static string FindDisplayedSkillDescription(GameObject skillParent, string skillName)
        {
            try
            {
                // Looking for displayed skill description
                
                // Search in the skill parent and its broader hierarchy for description text
                var candidates = new List<GameObject> { skillParent };
                
                // Add parent objects that might contain the description area
                var current = skillParent.transform;
                for (int i = 0; i < 3 && current != null; i++) // Go up 3 levels
                {
                    current = current.parent;
                    if (current != null)
                    {
                        candidates.Add(current.gameObject);
                    }
                }
                
                // Search each candidate for descriptive text
                foreach (var candidate in candidates)
                {
                    string description = SearchForSkillDescriptionText(candidate, skillName);
                    if (!string.IsNullOrEmpty(description))
                    {
                        return description;
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error finding displayed skill description: {ex}");
                return null;
            }
        }
        
        private static string SearchForSkillDescriptionText(GameObject parent, string skillName)
        {
            try
            {
                // Get all text components in this object and its children
                var allTextComponents = new List<Component>();
                
                // Add TextMeshProUGUI components
                var tmpComponents = parent.GetComponentsInChildren<TextMeshProUGUI>();
                if (tmpComponents != null)
                {
                    allTextComponents.AddRange(tmpComponents);
                }
                
                // Add regular Text components  
                var textComponents = parent.GetComponentsInChildren<Text>();
                if (textComponents != null)
                {
                    allTextComponents.AddRange(textComponents);
                }
                
                // Searching text components for skill description
                
                foreach (var component in allTextComponents)
                {
                    string text = null;
                    
                    if (component is TextMeshProUGUI tmpText && tmpText != null)
                    {
                        text = tmpText.text;
                    }
                    else if (component is Text regularText && regularText != null)
                    {
                        text = regularText.text;
                    }
                    
                    if (!string.IsNullOrEmpty(text))
                    {
                        // Checking text for skill description pattern
                        
                        // Look for skill description patterns
                        if (IsLikelySkillDescriptionText(text, skillName))
                        {
                            string cleanedName = GetLocalizedSkillName(TryGetSkillTypeFromName(skillName, out SkillType skillType) ? skillType : SkillType.LOGIC);
                            if (string.IsNullOrEmpty(cleanedName))
                            {
                                cleanedName = skillName.Replace('_', ' ');
                            }
                            
                            return $"{cleanedName}: {text.Trim()}";
                        }
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error searching for skill description text: {ex}");
                return null;
            }
        }
        
        private static bool IsLikelySkillDescriptionText(string text, string skillName)
        {
            if (string.IsNullOrEmpty(text) || text.Length < 20)
                return false;
                
            // Skip if it's just the skill name itself
            if (text.Trim().Equals(skillName.Replace('_', ' '), StringComparison.OrdinalIgnoreCase))
                return false;
                
            // Skip pure numbers or very short text
            if (text.Trim().Length < 15 || text.All(c => char.IsDigit(c) || char.IsWhiteSpace(c)))
                return false;
                
            // Skip common UI text that's not descriptions  
            string lowerText = text.ToLower();
            if (lowerText.Contains("select button") || lowerText.Contains("level up") || 
                lowerText.Contains("point") && lowerText.Length < 25)
                return false;
                
            // Look for description-like content - complete sentences or skill-related keywords
            return lowerText.Contains("skill") || lowerText.Contains("ability") ||
                   lowerText.Contains("helps") || lowerText.Contains("allows") ||
                   lowerText.Contains("used") || lowerText.Contains("affects") ||
                   lowerText.Contains("reasoning") || lowerText.Contains("knowledge") ||
                   lowerText.Contains("social") || lowerText.Contains("physical") ||
                   text.Contains(".") && text.Split('.').Length > 1; // Multiple sentences
        }

        private static string ExtractTooltipDescription(object tooltipData)
        {
            try
            {
                if (tooltipData == null) return null;
                
                // Use reflection to look for common description fields
                var type = tooltipData.GetType();
                var fields = type.GetFields();
                var properties = type.GetProperties();
                
                // Look for fields that might contain description
                foreach (var field in fields)
                {
                    if (field.Name.ToLower().Contains("description") || 
                        field.Name.ToLower().Contains("text") ||
                        field.Name.ToLower().Contains("content"))
                    {
                        var value = field.GetValue(tooltipData);
                        if (value != null && value is string str && !string.IsNullOrEmpty(str))
                        {
                            return str.Trim();
                        }
                    }
                }
                
                // Look for properties that might contain description
                foreach (var prop in properties)
                {
                    if (prop.CanRead && (prop.Name.ToLower().Contains("description") || 
                        prop.Name.ToLower().Contains("text") ||
                        prop.Name.ToLower().Contains("content")))
                    {
                        try
                        {
                            var value = prop.GetValue(tooltipData);
                            if (value != null && value is string str && !string.IsNullOrEmpty(str))
                            {
                                return str.Trim();
                            }
                        }
                        catch { /* Property might not be accessible */ }
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Error extracting tooltip description: {ex.Message}");
                return null;
            }
        }

        private static string ExtractSkillObjectDescription(object skill, string skillName)
        {
            try
            {
                if (skill == null) return null;
                
                var type = skill.GetType();
                // Look for description-related fields and properties
                var fields = type.GetFields();
                var properties = type.GetProperties();
                
                // Try common field names for descriptions
                foreach (var field in fields)
                {
                    if (field.Name.ToLower().Contains("description") || 
                        field.Name.ToLower().Contains("tooltip") ||
                        field.Name.ToLower().Contains("info"))
                    {
                        var value = field.GetValue(skill);
                        if (value != null && value is string str && !string.IsNullOrEmpty(str))
                        {
                            return $"{skillName.Replace('_', ' ')} Skill: {str.Trim()}";
                        }
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error extracting skill object description: {ex}");
                return null;
            }
        }

        private static bool IsLikelySkillDescription(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length < 10)
                return false;
                
            // Skip single words, numbers, or very short text
            if (text.Split(' ').Length < 3)
                return false;
                
            // Skip common UI text that's not descriptions
            string lowerText = text.ToLower();
            if (lowerText.Contains("select") || lowerText.Contains("button") || 
                lowerText.Contains("click") || lowerText == "background" ||
                lowerText.Length < 15)
                return false;
                
            // Look for description-like content
            return lowerText.Contains("skill") || lowerText.Contains("ability") ||
                   lowerText.Contains("helps") || lowerText.Contains("allows") ||
                   lowerText.Contains("used") || lowerText.Contains("affects");
        }
        
        private static void TryFindSpecificGameComponents(GameObject skillParent, string skillName)
        {
            try
            {
                // Looking for specific Il2Cpp components
                
                // Try to find various game-specific components (for debug purposes)
                var statLevelers = skillParent.GetComponentsInChildren<Il2Cpp.StatLeveler>();
                // Could potentially extract skill descriptions from StatLeveler components here
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Error finding specific components for {skillName}: {ex.Message}");
            }
        }

        private static bool TryGetSkillTypeFromName(string skillName, out SkillType skillType)
        {
            try
            {
                // Map common skill names to SkillType enum values (using actual enum names from SkillType.cs)
                switch (skillName.ToUpper())
                {
                    case "LOGIC":
                        skillType = SkillType.LOGIC;
                        return true;
                    case "ENCYCLOPEDIA":
                        skillType = SkillType.ENCYCLOPEDIA;
                        return true;
                    case "RHETORIC":
                        skillType = SkillType.RHETORIC;
                        return true;
                    case "DRAMA":
                        skillType = SkillType.DRAMA;
                        return true;
                    case "CONCEPTUALIZATION":
                        skillType = SkillType.CONCEPTUALIZATION;
                        return true;
                    case "VISUAL_CALCULUS":
                        skillType = SkillType.VISUAL_CALCULUS;
                        return true;
                    case "VOLITION":
                        skillType = SkillType.VOLITION;
                        return true;
                    case "INLAND_EMPIRE":
                        skillType = SkillType.INLAND_EMPIRE;
                        return true;
                    case "EMPATHY":
                        skillType = SkillType.EMPATHY;
                        return true;
                    case "AUTHORITY":
                        skillType = SkillType.AUTHORITY;
                        return true;
                    case "SUGGESTION":
                        skillType = SkillType.SUGGESTION;
                        return true;
                    case "ESPRIT_DE_CORPS":
                        skillType = SkillType.ESPRIT_DE_CORPS;
                        return true;
                    case "PHYSICAL_INSTRUMENT":
                        skillType = SkillType.PHYSICAL_INSTRUMENT;
                        return true;
                    case "ELECTROCHEMISTRY":
                        skillType = SkillType.ELECTROCHEMISTRY;
                        return true;
                    case "ENDURANCE":
                        skillType = SkillType.ENDURANCE;
                        return true;
                    case "HALF_LIGHT":
                        skillType = SkillType.HALF_LIGHT;
                        return true;
                    case "PAIN_THRESHOLD":
                        skillType = SkillType.PAIN_THRESHOLD;
                        return true;
                    case "SHIVERS":
                        skillType = SkillType.SHIVERS;
                        return true;
                    case "HAND_EYE_COORDINATION":
                    case "HE_COORDINATION":  // The actual enum name is different
                        skillType = SkillType.HE_COORDINATION;
                        return true;
                    case "PERCEPTION":
                        skillType = SkillType.PERCEPTION;
                        return true;
                    case "REACTION_SPEED":
                    case "REACTION":
                        skillType = SkillType.REACTION;
                        return true;
                    case "SAVOIR_FAIRE":
                        skillType = SkillType.SAVOIR_FAIRE;
                        return true;
                    case "INTERFACING":
                        skillType = SkillType.INTERFACING;
                        return true;
                    case "COMPOSURE":
                        skillType = SkillType.COMPOSURE;
                        return true;
                    default:
                        skillType = SkillType.LOGIC; // Default fallback
                        return false;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error mapping skill name to type: {ex.Message}");
                skillType = SkillType.LOGIC;
                return false;
            }
        }

        private static string GetLocalizedSkillName(SkillType skillType)
        {
            try
            {
                // Use the game's built-in method to get localized skill name
                return Skill.SkillTypeToLocalizedName(skillType, true);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting localized skill name for {skillType}: {ex.Message}");
                return null;
            }
        }

        private static string GetLocalizedSkillDescription(SkillType skillType)
        {
            try
            {
                string skillName = skillType.ToString();
                
                // Try various localization key patterns that might be used in the game
                string[] keyPatterns = {
                    $"Skills/{skillName}/Description",
                    $"Skill_{skillName}_Description", 
                    $"SKILL_{skillName.ToUpper()}_DESC",
                    $"CharacterCreation/Skills/{skillName}/Description",
                    $"Skills.{skillName}.Description",
                    $"UI/Skills/{skillName}/Desc",
                    $"skill.{skillName.ToLower()}.description",
                    $"{skillName.ToLower()}.description", 
                    $"skills.{skillName.ToLower()}.desc",
                    $"character.skill.{skillName.ToLower()}.description",
                    $"UI.skill.{skillName.ToLower()}.description",
                    skillName + "_Description",
                    skillName + "_Desc",
                    skillName.ToLower() + "_description"
                };
                
                foreach (string key in keyPatterns)
                {
                    try
                    {
                        string description = CollageLocalization.GetTranslation(key);
                        if (!string.IsNullOrEmpty(description) && description != key && !description.StartsWith("Missing"))
                        {
                            MelonLogger.Msg($"Found skill description for {skillType} using key '{key}': {description}");
                            return description;
                        }
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Warning($"Failed to get translation for key '{key}': {ex.Message}");
                    }
                }
                
                // Also try getting English translation as fallback
                foreach (string key in keyPatterns)
                {
                    try
                    {
                        string description = CollageLocalization.GetEnglishTranslation(key);
                        if (!string.IsNullOrEmpty(description) && description != key && !description.StartsWith("Missing"))
                        {
                            MelonLogger.Msg($"Found English skill description for {skillType} using key '{key}': {description}");
                            return description;
                        }
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Warning($"Failed to get English translation for key '{key}': {ex.Message}");
                    }
                }
                
                MelonLogger.Warning($"No skill description found for {skillType} using any localization key");
                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting localized skill description for {skillType}: {ex.Message}");
                return null;
            }
        }

        private static string GetSkillDescription(string skillName)
        {
            switch (skillName.ToUpper())
            {
                // Intellect Skills
                case "LOGIC":
                    return "Logic Skill: Deductive reasoning and problem-solving. Helps with evidence analysis and logical conclusions";
                case "ENCYCLOPEDIA":
                    return "Encyclopedia Skill: General knowledge and trivia. Provides background information on various topics";
                case "RHETORIC":
                    return "Rhetoric Skill: Persuasion and argumentation. Useful for convincing others and debating";
                case "DRAMA":
                    return "Drama Skill: Acting and deception. Helps with lying and theatrical performance";
                case "CONCEPTUALIZATION":
                    return "Conceptualization Skill: Abstract thinking and creativity. Aids in artistic and philosophical insights";
                case "VISUAL_CALCULUS":
                    return "Visual Calculus Skill: Spatial reasoning and trajectory analysis. Useful for physics and geometry";

                // Psyche Skills
                case "VOLITION":
                    return "Volition Skill: Willpower and self-control. Resists mental influence and maintains composure";
                case "INLAND_EMPIRE":
                    return "Inland Empire Skill: Intuition and surreal thinking. Provides mystical and abstract insights";
                case "EMPATHY":
                    return "Empathy Skill: Understanding others' emotions. Helps read people and social situations";
                case "AUTHORITY":
                    return "Authority Skill: Leadership and intimidation. Commands respect and dominates conversations";
                case "SUGGESTION":
                    return "Suggestion Skill: Subtle influence and manipulation. Guides conversations indirectly";
                case "ESPRIT_DE_CORPS":
                    return "Esprit de Corps Skill: Police solidarity and institutional knowledge. Connects with law enforcement";

                // Physique Skills
                case "PHYSICAL_INSTRUMENT":
                    return "Physical Instrument Skill: Raw strength and intimidation. Useful for violence and physical threats";
                case "ELECTROCHEMISTRY":
                    return "Electrochemistry Skill: Drug knowledge and chemical effects. Understands substances and addiction";
                case "ENDURANCE":
                    return "Endurance Skill: Physical resilience and health. Withstands damage and fatigue";
                case "HALF_LIGHT":
                    return "Half Light Skill: Violence and aggression. Thrives in dangerous and confrontational situations";
                case "PAIN_THRESHOLD":
                    return "Pain Threshold Skill: Tolerance to injury. Ignores pain and physical discomfort";
                case "SHIVERS":
                    return "Shivers Skill: Environmental awareness. Senses the city's mood and atmosphere";

                // Motorics Skills
                case "HAND_EYE_COORDINATION":
                    return "Hand/Eye Coordination Skill: Fine motor skills and precision. Useful for delicate tasks";
                case "PERCEPTION":
                    return "Perception Skill: Noticing details and hidden things. Spots clues and environmental features";
                case "REACTION_SPEED":
                    return "Reaction Speed Skill: Quick reflexes and timing. Helps in fast-paced situations";
                case "SAVOIR_FAIRE":
                    return "Savoir Faire Skill: Style and panache. Performs actions with flair and sophistication";
                case "INTERFACING":
                    return "Interfacing Skill: Technology and electronics. Operates computers and technical equipment";
                case "COMPOSURE":
                    return "Composure Skill: Staying calm under pressure. Maintains dignity in stressful situations";

                default:
                    return $"{skillName.Replace('_', ' ')} Skill: Select to view details and allocate points";
            }
        }

        // Helper method to extract text from any GameObject
        public static string ExtractTextFromGameObject(GameObject obj)
        {
            return ExtractBestTextContent(obj);
        }
    }
}