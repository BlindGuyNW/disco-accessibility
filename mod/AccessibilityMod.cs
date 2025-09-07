using System;
using MelonLoader;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI.Extensions;
using Il2Cpp;
using Il2CppCollageMode;
using Il2CppFortressOccident;
using Il2CppTMPro;

[assembly: MelonInfo(typeof(AccessibilityMod.AccessibilityMod), "Disco Elysium Accessibility Mod", "1.0.0", "YourName")]
[assembly: MelonGame("ZAUM Studio", "Disco Elysium")]

namespace AccessibilityMod
{
    public class AccessibilityMod : MelonMod
    {
        public static InteractableSelectionManager lastSelectionManager = null;
        public static CommonPadInteractable lastSelectedInteractable = null;
        public static GameObject lastSelectedUIObject = null;
        public static string lastSpokenText = "";
        public static float lastSpeechTime = 0f;
        public static readonly float SPEECH_COOLDOWN = 0.1f; // 100ms cooldown to prevent spam

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Accessibility Mod initializing...");
            
            // Initialize Tolk screen reader
            if (TolkScreenReader.Instance.Initialize())
            {
                LoggerInstance.Msg("Tolk initialized successfully!");
                
                string detectedReader = TolkScreenReader.Instance.DetectScreenReader();
                if (!string.IsNullOrEmpty(detectedReader))
                {
                    LoggerInstance.Msg($"Detected screen reader: {detectedReader}");
                }
                else
                {
                    LoggerInstance.Msg("No screen reader detected, using SAPI fallback");
                }
                
                if (TolkScreenReader.Instance.HasSpeech())
                {
                    LoggerInstance.Msg("Speech output available");
                    TolkScreenReader.Instance.Speak("Disco Elysium Accessibility Mod loaded", true);
                }
                
                if (TolkScreenReader.Instance.HasBraille())
                {
                    LoggerInstance.Msg("Braille output available");
                }
            }
            else
            {
                LoggerInstance.Warning("Failed to initialize Tolk - falling back to console logging");
            }
        }
        
        public override void OnApplicationQuit()
        {
            // Clean up Tolk when the game exits
            TolkScreenReader.Instance.Cleanup();
            LoggerInstance.Msg("Tolk cleaned up");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            LoggerInstance.Msg($"Scene loaded: {sceneName} (Index: {buildIndex})");
        }
        
        public override void OnUpdate()
        {
            // Every frame, check for selected UI elements
            var selectables = UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Selectable>();
            foreach (var selectable in selectables)
            {
                if (selectable != null && selectable.gameObject != null)
                {
                    // Check if this selectable is in "highlighted" or "selected" state
                    var state = selectable.currentSelectionState;
                    if (state == UnityEngine.UI.Selectable.SelectionState.Highlighted || 
                        state == UnityEngine.UI.Selectable.SelectionState.Selected)
                    {
                        var name = selectable.gameObject.name;
                        
                        if (lastSelectedUIObject == null || lastSelectedUIObject != selectable.gameObject)
                        {
                            lastSelectedUIObject = selectable.gameObject;
                            
                            // Extract text and format for speech with UI context
                            string speechText = NavigationHelper.FormatUIElementForSpeech(selectable.gameObject);
                            if (!string.IsNullOrEmpty(speechText) && speechText != lastSpokenText)
                            {
                                TolkScreenReader.Instance.Speak(speechText, true); // Interrupt for menu navigation
                                lastSpokenText = speechText;
                                lastSpeechTime = Time.time;
                            }
                                
                            // Keep minimal logging for debugging
                            LoggerInstance.Msg($"[UI DEBUG] {name}: '{speechText}'");
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(InteractableSelectionManager), nameof(InteractableSelectionManager.OnUpdate))]
    public class InteractableSelectionManagerPatch
    {
        static void Postfix(InteractableSelectionManager __instance)
        {
            try
            {
                if (__instance?.CurrentSelected != null)
                {
                    var currentSelected = __instance.CurrentSelected;
                    
                    // Check if the selected interactable has changed
                    if (AccessibilityMod.lastSelectedInteractable == null || 
                        !currentSelected.IsTheSame(AccessibilityMod.lastSelectedInteractable))
                    {
                        AccessibilityMod.lastSelectedInteractable = currentSelected;
                        LogInteractableInfo(currentSelected);
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in InteractableSelectionManager patch: {ex}");
            }
        }

        private static void LogInteractableInfo(CommonPadInteractable interactable)
        {
            try
            {
                string speechText = NavigationHelper.FormatInteractableForSpeech(interactable);
                
                if (!string.IsNullOrEmpty(speechText))
                {
                    // Check for cooldown to prevent rapid speech
                    if (Time.time - AccessibilityMod.lastSpeechTime > AccessibilityMod.SPEECH_COOLDOWN)
                    {
                        // Don't repeat the same text
                        if (speechText != AccessibilityMod.lastSpokenText)
                        {
                            TolkScreenReader.Instance.Speak(speechText, false); // Don't interrupt for world objects
                            AccessibilityMod.lastSpokenText = speechText;
                            AccessibilityMod.lastSpeechTime = Time.time;
                        }
                    }
                    
                    // Keep minimal logging for debugging
                    MelonLogger.Msg($"[OBJECT DEBUG] {speechText}");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error announcing interactable: {ex}");
            }
        }
    }

    // Simple patch to monitor when interactables are added to the selection manager
    [HarmonyPatch(typeof(InteractableSelectionManager), "Add", new Type[] { typeof(OrbUiElement), typeof(float) })]
    public class InteractableAddedPatch
    {
        static void Postfix(InteractableSelectionManager __instance, OrbUiElement orb, float distance)
        {
            try
            {
                // Keep minimal debug logging
                MelonLogger.Msg($"[ORB DEBUG] {orb?.name ?? "Unknown"} at distance {distance:F2}");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in InteractableAdded patch: {ex}");
            }
        }
    }

    // Alternative patch to monitor interactable changes via events
    [HarmonyPatch(typeof(InteractableSelectionManager), "set_CurrentSelected")]
    public class InteractableSelectionManagerSetterPatch
    {
        static void Postfix(InteractableSelectionManager __instance, CommonPadInteractable value)
        {
            try
            {
                if (value != null)
                {
                    // Use Tolk to announce the object
                    string speechText = NavigationHelper.FormatInteractableForSpeech(value);
                    if (!string.IsNullOrEmpty(speechText) && speechText != AccessibilityMod.lastSpokenText)
                    {
                        if (Time.time - AccessibilityMod.lastSpeechTime > AccessibilityMod.SPEECH_COOLDOWN)
                        {
                            TolkScreenReader.Instance.Speak(speechText, false);
                            AccessibilityMod.lastSpokenText = speechText;
                            AccessibilityMod.lastSpeechTime = Time.time;
                        }
                    }
                    
                    // Keep minimal debug logging
                    MelonLogger.Msg($"[SETTER DEBUG] {speechText ?? "Unknown object"}");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in CurrentSelected setter patch: {ex}");
            }
        }
    }


    // Helper class for shared navigation methods
    public static class NavigationHelper
    {
        // Format interactable objects for speech output
        public static string FormatInteractableForSpeech(CommonPadInteractable interactable)
        {
            try
            {
                if (interactable == null) return null;
                
                string speechText = "";
                
                // Get game entity name first (most descriptive)
                var gameEntity = interactable.GetGameEntity();
                if (gameEntity != null && !string.IsNullOrEmpty(gameEntity.name))
                {
                    speechText = CleanObjectName(gameEntity.name);
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
                            speechText = "Orb: " + CleanObjectName(transform.gameObject.name);
                        }
                    }
                    else if (mouseOverHighlight != null)
                    {
                        var transform = mouseOverHighlight.transform;
                        if (transform != null && !string.IsNullOrEmpty(transform.gameObject.name))
                        {
                            speechText = CleanObjectName(transform.gameObject.name);
                        }
                    }
                }
                
                // Add type information if we have something
                if (!string.IsNullOrEmpty(speechText))
                {
                    var interactableType = interactable.CurrentType();
                    // InteractableType enum only has ORB and MOUSE_HIGHLIGHT, no None value
                    // Only add type prefix if not already in the text
                    string typeStr = interactableType.ToString();
                    if (!speechText.ToLower().Contains(typeStr.ToLower()) && !speechText.StartsWith("Orb:"))
                    {
                        if (interactableType == Il2Cpp.InteractableType.ORB)
                        {
                            speechText = $"Orb: {speechText}";
                        }
                        else if (interactableType == Il2Cpp.InteractableType.MOUSE_HIGHLIGHT)
                        {
                            // Don't add prefix for regular objects
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
        
        // Extract the best text content from a UI object using all available methods
        public static string ExtractBestTextContent(GameObject uiObject)
        {
            try
            {
                if (uiObject == null) return null;
                
                // Try direct text components first
                var textComponent = uiObject.GetComponent<UnityEngine.UI.Text>();
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
                var childText = uiObject.GetComponentInChildren<UnityEngine.UI.Text>();
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
        
        // Format UI elements for speech output (does its own text extraction + context)
        public static string FormatUIElementForSpeech(GameObject uiObject)
        {
            try
            {
                if (uiObject == null) return null;
                
                // TODO: Add support for slider components when needed
                // - LayoutProfileConfiguration for UI scaling sliders  
                // - AudioConfigurationSliders for volume sliders
                // - Individual *GraphicsOption components for graphics sliders
                
                // Extract text content for standard components
                string speechText = ExtractBestTextContent(uiObject);
                if (string.IsNullOrEmpty(speechText)) return null;
                
                speechText = speechText.Trim();
                
                // Check for Disco Elysium's OptionDropbox component
                var optionDropbox = uiObject.GetComponent<Il2Cpp.OptionDropbox>();
                if (optionDropbox != null)
                {
                    string settingName = optionDropbox.settingName;
                    if (!string.IsNullOrEmpty(settingName))
                    {
                        return $"{settingName}: {speechText}";
                    }
                    else
                    {
                        return $"Dropdown: {speechText}";
                    }
                }
                
                // Add UI element type context for standard components
                var button = uiObject.GetComponent<UnityEngine.UI.Button>();
                if (button != null)
                {
                    return $"Button: {speechText}";
                }
                
                var toggle = uiObject.GetComponent<UnityEngine.UI.Toggle>();
                if (toggle != null)
                {
                    return $"Toggle {(toggle.isOn ? "checked" : "unchecked")}: {speechText}";
                }
                
                var slider = uiObject.GetComponent<UnityEngine.UI.Slider>();
                if (slider != null)
                {
                    int percentage = Mathf.RoundToInt(slider.normalizedValue * 100);
                    return $"Slider {percentage} percent: {speechText}";
                }
                
                // Check for TextMesh Pro dropdown
                var tmpDropdown = uiObject.GetComponent<Il2CppTMPro.TMP_Dropdown>();
                if (tmpDropdown != null)
                {
                    if (tmpDropdown.options != null && tmpDropdown.value >= 0 && tmpDropdown.value < tmpDropdown.options.Count)
                    {
                        return $"Dropdown: {speechText}, selected {tmpDropdown.options[tmpDropdown.value].text}";
                    }
                    else
                    {
                        return $"Dropdown: {speechText}";
                    }
                }
                
                // Check for standard Unity dropdown (fallback)
                var dropdown = uiObject.GetComponent<UnityEngine.UI.Dropdown>();
                if (dropdown != null)
                {
                    if (dropdown.options != null && dropdown.value >= 0 && dropdown.value < dropdown.options.Count)
                    {
                        return $"Dropdown: {speechText}, selected {dropdown.options[dropdown.value].text}";
                    }
                    else
                    {
                        return $"Dropdown: {speechText}";
                    }
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
        
        // Clean up object names for better speech output
        private static string CleanObjectName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            
            // Remove common Unity prefixes/suffixes
            name = name.Replace("_", " ");
            name = name.Replace("(Clone)", "");
            name = name.Replace("GameObject", "");
            
            // Remove brackets and their contents
            name = System.Text.RegularExpressions.Regex.Replace(name, @"\([^)]*\)", "");
            name = System.Text.RegularExpressions.Regex.Replace(name, @"\[[^\]]*\]", "");
            
            // Clean up extra whitespace
            name = System.Text.RegularExpressions.Regex.Replace(name, @"\s+", " ").Trim();
            
            return name;
        }
        
        // Helper method to check current EventSystem selection as fallback
        public static void CheckCurrentUISelection()
        {
            try
            {
                var eventSystem = EventSystem.current;
                if (eventSystem != null)
                {
                    var currentSelection = eventSystem.currentSelectedGameObject;
                    if (currentSelection != AccessibilityMod.lastSelectedUIObject)
                    {
                        AccessibilityMod.lastSelectedUIObject = currentSelection;
                        
                        // Extract text and format for speech with UI context  
                        string speechText = NavigationHelper.FormatUIElementForSpeech(currentSelection);
                        if (!string.IsNullOrEmpty(speechText))
                        {
                            if (!string.IsNullOrEmpty(speechText) && speechText != AccessibilityMod.lastSpokenText)
                            {
                                TolkScreenReader.Instance.Speak(speechText, true);
                                AccessibilityMod.lastSpokenText = speechText;
                                AccessibilityMod.lastSpeechTime = Time.time;
                            }
                        }
                        
                        NavigationHelper.LogUISelectionInfo(currentSelection, "EventSystem");
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error checking EventSystem selection: {ex}");
            }
        }

        public static void LogUISelectionInfo(GameObject selectedObject, string source)
        {
            try
            {
                if (selectedObject == null)
                {
                    MelonLogger.Msg($"[{source}] UI Selection: None (deselected)");
                    return;
                }

                string logMessage = $"[{source}] UI Selection: ";
                
                // Get the object name
                logMessage += $"Object: {selectedObject.name}, ";

                // Try to get text content from various UI components
                var textComponent = selectedObject.GetComponent<UnityEngine.UI.Text>();
                if (textComponent != null && !string.IsNullOrEmpty(textComponent.text))
                {
                    logMessage += $"Text: '{textComponent.text}', ";
                }

                var tmpText = selectedObject.GetComponent<TextMeshProUGUI>();
                if (tmpText != null && !string.IsNullOrEmpty(tmpText.text))
                {
                    logMessage += $"TMPText: '{tmpText.text}', ";
                }

                var tmpTextPro = selectedObject.GetComponent<TextMeshPro>();
                if (tmpTextPro != null && !string.IsNullOrEmpty(tmpTextPro.text))
                {
                    logMessage += $"TMPPro: '{tmpTextPro.text}', ";
                }

                // Get button component if present
                var button = selectedObject.GetComponent<UnityEngine.UI.Button>();
                if (button != null)
                {
                    logMessage += "Type: Button, ";
                    
                    // Try to get text from button's children
                    var childText = selectedObject.GetComponentInChildren<UnityEngine.UI.Text>();
                    if (childText != null && !string.IsNullOrEmpty(childText.text))
                    {
                        logMessage += $"ButtonText: '{childText.text}', ";
                    }

                    var childTMP = selectedObject.GetComponentInChildren<TextMeshProUGUI>();
                    if (childTMP != null && !string.IsNullOrEmpty(childTMP.text))
                    {
                        logMessage += $"ButtonTMPText: '{childTMP.text}', ";
                    }
                }

                // Get toggle component if present
                var toggle = selectedObject.GetComponent<UnityEngine.UI.Toggle>();
                if (toggle != null)
                {
                    logMessage += $"Type: Toggle, State: {toggle.isOn}, ";
                }

                // Get slider component if present
                var slider = selectedObject.GetComponent<UnityEngine.UI.Slider>();
                if (slider != null)
                {
                    logMessage += $"Type: Slider, Value: {slider.value:F2}, ";
                }

                // Get dropdown component if present
                var dropdown = selectedObject.GetComponent<UnityEngine.UI.Dropdown>();
                if (dropdown != null)
                {
                    logMessage += $"Type: Dropdown, Selected: {dropdown.value}, ";
                    if (dropdown.options != null && dropdown.value < dropdown.options.Count)
                    {
                        logMessage += $"Option: '{dropdown.options[dropdown.value].text}', ";
                    }
                }

                // Get parent hierarchy for context
                var parent = selectedObject.transform.parent;
                if (parent != null)
                {
                    logMessage += $"Parent: {parent.name}, ";
                    
                    // Try to get grandparent for more context
                    var grandparent = parent.parent;
                    if (grandparent != null)
                    {
                        logMessage += $"Grandparent: {grandparent.name}, ";
                    }
                }

                // Get screen position if available
                var rectTransform = selectedObject.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    var worldPos = rectTransform.position;
                    logMessage += $"Position: ({worldPos.x:F0}, {worldPos.y:F0}, {worldPos.z:F0}), ";
                }

                MelonLogger.Msg(logMessage.TrimEnd(' ', ','));
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error logging UI selection info: {ex}");
            }
        }
    }
}