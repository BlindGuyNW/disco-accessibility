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

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Accessibility Mod initialized!");
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
                        
                        // Get text from the button if it has any
                        var text = selectable.GetComponentInChildren<UnityEngine.UI.Text>();
                        var tmpText = selectable.GetComponentInChildren<TextMeshProUGUI>();
                        
                        string textContent = "";
                        if (text != null) textContent = text.text;
                        else if (tmpText != null) textContent = tmpText.text;
                        
                        if (!string.IsNullOrEmpty(textContent))
                        {
                            if (lastSelectedUIObject == null || lastSelectedUIObject != selectable.gameObject)
                            {
                                lastSelectedUIObject = selectable.gameObject;
                                LoggerInstance.Msg($"[UI SELECTION] Selected: {name} - Text: '{textContent}' - State: {state}");
                            }
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
                string logMessage = "Selected interactable: ";
                
                // Try to get information about the object
                var interactableType = interactable.CurrentType();
                logMessage += $"Type: {interactableType}, ";

                // Get world position
                var worldPos = interactable.GetWorldPosition();
                logMessage += $"Position: ({worldPos.x:F2}, {worldPos.y:F2}, {worldPos.z:F2}), ";

                // Check if it's an orb or regular interactable
                var orb = interactable.Orb;
                var mouseOverHighlight = interactable.Interactable;

                if (orb != null)
                {
                    logMessage += "Object: Orb, ";
                    
                    // Try to get the world orb for more information
                    var worldOrb = orb.WorldOrb;
                    if (worldOrb != null)
                    {
                        // Try to get entity information
                        var transform = orb.transform;
                        if (transform != null)
                        {
                            logMessage += $"GameObject: {transform.gameObject.name}, ";
                        }
                    }
                }
                else if (mouseOverHighlight != null)
                {
                    logMessage += "Object: MouseOverHighlight, ";
                    
                    // Try to get the game object name
                    var transform = mouseOverHighlight.transform;
                    if (transform != null)
                    {
                        logMessage += $"GameObject: {transform.gameObject.name}, ";
                    }
                }

                // Get game entity if available
                var gameEntity = interactable.GetGameEntity();
                if (gameEntity != null)
                {
                    logMessage += $"Entity: {gameEntity.name}, ";
                }

                MelonLogger.Msg(logMessage);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error logging interactable info: {ex}");
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
                MelonLogger.Msg($"Orb added to selection: {orb?.name ?? "Unknown"} at distance {distance:F2}");
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
                    MelonLogger.Msg($"Interactable selection changed via setter");
                    
                    // Get basic information
                    var toString = value.ToString();
                    if (!string.IsNullOrEmpty(toString))
                    {
                        MelonLogger.Msg($"Selected object: {toString}");
                    }
                    
                    // Try to get world position for spatial context
                    if (value.HaveAnyWorldInteractableObject())
                    {
                        var worldPos = value.GetWorldPosition();
                        MelonLogger.Msg($"Object world position: ({worldPos.x:F2}, {worldPos.y:F2}, {worldPos.z:F2})");
                    }
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