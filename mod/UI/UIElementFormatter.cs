using System;
using UnityEngine;
using UnityEngine.UI;
using Il2CppTMPro;
using Il2Cpp;
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
                
                // Check for confirmation dialog elements first (high priority)
                string confirmationText = GetConfirmationTextContext(uiObject);
                if (!string.IsNullOrEmpty(confirmationText))
                {
                    return confirmationText;
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
    }
}