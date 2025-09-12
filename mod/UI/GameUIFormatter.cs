using System;
using UnityEngine;
using UnityEngine.UI;
using Il2CppTMPro;
using Il2Cpp;
using Il2CppSunshine;
using Il2CppDiscoPages.Elements.MainMenu;
using Il2CppPages.MainMenu;
using AccessibilityMod.Utils;
using MelonLoader;

namespace AccessibilityMod.UI
{
    /// <summary>
    /// Handles formatting for game UI components like sliders, dropdowns, buttons, and toggles
    /// </summary>
    public static class GameUIFormatter
    {
        /// <summary>
        /// Format standard UI components (buttons, toggles, dropdowns, sliders)
        /// </summary>
        public static string FormatStandardUIComponent(GameObject uiObject, string speechText)
        {
            try
            {
                if (uiObject == null) return speechText;

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

                // Handle buttons
                var button = uiObject.GetComponent<Button>();
                if (button != null)
                {
                    return $"Button: {speechText}";
                }

                // Handle toggles
                var toggle = uiObject.GetComponent<Toggle>();
                if (toggle != null)
                {
                    return $"Toggle {(toggle.isOn ? "checked" : "unchecked")}: {speechText}";
                }

                // Handle sliders with enhanced information
                var slider = uiObject.GetComponent<Slider>();
                if (slider != null)
                {
                    string sliderInfo = GetEnhancedSliderInfo(slider, uiObject);
                    if (!string.IsNullOrEmpty(sliderInfo))
                    {
                        // If we have both slider info and text, combine them
                        return string.IsNullOrEmpty(speechText) ? sliderInfo : $"{sliderInfo} - {speechText}";
                    }
                    
                    // Fallback for sliders we couldn't identify
                    int percentage = Mathf.RoundToInt(slider.normalizedValue * 100);
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
                MelonLogger.Error($"Error formatting standard UI component: {ex}");
                return speechText;
            }
        }

        /// <summary>
        /// Get enhanced slider information with context detection
        /// </summary>
        public static string GetEnhancedSliderInfo(Slider slider, GameObject uiObject)
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

        #region Private Helper Methods

        /// <summary>
        /// Get dropdown name from OptionDropbox component
        /// </summary>
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

        /// <summary>
        /// Get TMP dropdown name
        /// </summary>
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

        /// <summary>
        /// Try to get slider name from singleton components
        /// </summary>
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

        /// <summary>
        /// Try to get slider name from parent object
        /// </summary>
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

        /// <summary>
        /// Try to get slider name from GameObject name
        /// </summary>
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

        /// <summary>
        /// Format slider value based on context
        /// </summary>
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

        #endregion
    }
}