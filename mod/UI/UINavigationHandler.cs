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
                                
                                // Only log dialog selections for debugging
                                if (speechText?.StartsWith("Dialog:") == true || speechText?.Contains("Check") == true)
                                {
                                    MelonLogger.Msg($"[DIALOG] {name}: '{speechText}'");
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
                        
                        // Only log dialog selections
                        if (speechText?.StartsWith("Dialog:") == true || speechText?.Contains("Check") == true)
                        {
                            MelonLogger.Msg($"[DIALOG] EventSystem: {currentSelection?.name}: '{speechText}'");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error checking EventSystem selection: {ex}");
            }
        }

    }
}