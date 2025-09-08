using System;
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
        public static float lastSpeechTime = 0f;
        public static readonly float SPEECH_COOLDOWN = 0.1f; // 100ms cooldown to prevent spam

        public void UpdateUINavigation()
        {
            try
            {
                // Check for selected UI elements via Selectable components
                var selectables = UnityEngine.Object.FindObjectsOfType<Selectable>();
                foreach (var selectable in selectables)
                {
                    if (selectable != null && selectable.gameObject != null)
                    {
                        // Check if this selectable is in "highlighted" or "selected" state
                        var state = selectable.currentSelectionState;
                        if (state == Selectable.SelectionState.Highlighted || 
                            state == Selectable.SelectionState.Selected)
                        {
                            var name = selectable.gameObject.name;
                            
                            if (lastSelectedUIObject == null || lastSelectedUIObject != selectable.gameObject)
                            {
                                lastSelectedUIObject = selectable.gameObject;
                                
                                // Extract text and format for speech with UI context
                                string speechText = UIElementFormatter.FormatUIElementForSpeech(selectable.gameObject);
                                if (!string.IsNullOrEmpty(speechText) && speechText != lastSpokenText)
                                {
                                    TolkScreenReader.Instance.Speak(speechText, true); // Interrupt for menu navigation
                                    lastSpokenText = speechText;
                                    lastSpeechTime = Time.time;
                                }
                                
                                // Enhanced logging for dialog detection
                                if (speechText?.StartsWith("Dialog:") == true || speechText?.Contains("Check") == true)
                                {
                                    MelonLogger.Msg($"[DIALOG DEBUG] {name}: '{speechText}'");
                                }
                                else
                                {
                                    // Keep minimal logging for non-dialog UI
                                    MelonLogger.Msg($"[UI DEBUG] {name}: '{speechText}'");
                                }
                            }
                        }
                    }
                }

                // Also check current EventSystem selection as fallback
                CheckCurrentUISelection();
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
                        
                        // Extract text and format for speech with UI context  
                        string speechText = UIElementFormatter.FormatUIElementForSpeech(currentSelection);
                        if (!string.IsNullOrEmpty(speechText))
                        {
                            if (!string.IsNullOrEmpty(speechText) && speechText != lastSpokenText)
                            {
                                TolkScreenReader.Instance.Speak(speechText, true);
                                lastSpokenText = speechText;
                                lastSpeechTime = Time.time;
                            }
                        }
                        
                        // Enhanced logging for dialog detection via EventSystem
                        if (speechText?.StartsWith("Dialog:") == true || speechText?.Contains("Check") == true)
                        {
                            LogUISelectionInfo(currentSelection, "EventSystem-Dialog");
                        }
                        else
                        {
                            LogUISelectionInfo(currentSelection, "EventSystem");
                        }
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

                var tmpText = selectedObject.GetComponent<Il2CppTMPro.TextMeshProUGUI>();
                if (tmpText != null && !string.IsNullOrEmpty(tmpText.text))
                {
                    logMessage += $"TMPText: '{tmpText.text}', ";
                }

                var tmpTextPro = selectedObject.GetComponent<Il2CppTMPro.TextMeshPro>();
                if (tmpTextPro != null && !string.IsNullOrEmpty(tmpTextPro.text))
                {
                    logMessage += $"TMPPro: '{tmpTextPro.text}', ";
                }

                // Get button component if present
                var button = selectedObject.GetComponent<Button>();
                if (button != null)
                {
                    logMessage += "Type: Button, ";
                    
                    // Try to get text from button's children
                    var childText = selectedObject.GetComponentInChildren<UnityEngine.UI.Text>();
                    if (childText != null && !string.IsNullOrEmpty(childText.text))
                    {
                        logMessage += $"ButtonText: '{childText.text}', ";
                    }

                    var childTMP = selectedObject.GetComponentInChildren<Il2CppTMPro.TextMeshProUGUI>();
                    if (childTMP != null && !string.IsNullOrEmpty(childTMP.text))
                    {
                        logMessage += $"ButtonTMPText: '{childTMP.text}', ";
                    }
                }

                // Get toggle component if present
                var toggle = selectedObject.GetComponent<Toggle>();
                if (toggle != null)
                {
                    logMessage += $"Type: Toggle, State: {toggle.isOn}, ";
                }

                // Get slider component if present
                var slider = selectedObject.GetComponent<Slider>();
                if (slider != null)
                {
                    logMessage += $"Type: Slider, Value: {slider.value:F2}, ";
                }

                // Get dropdown component if present
                var dropdown = selectedObject.GetComponent<Dropdown>();
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