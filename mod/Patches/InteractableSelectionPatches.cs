using System;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using AccessibilityMod.UI;
using MelonLoader;

namespace AccessibilityMod.Patches
{
    [HarmonyPatch(typeof(InteractableSelectionManager), nameof(InteractableSelectionManager.OnUpdate))]
    public class InteractableSelectionManagerPatch
    {
        public static CommonPadInteractable lastSelectedInteractable = null;
        
        static void Postfix(InteractableSelectionManager __instance)
        {
            try
            {
                if (__instance?.CurrentSelected != null)
                {
                    var currentSelected = __instance.CurrentSelected;
                    
                    // Check if the selected interactable has changed
                    if (lastSelectedInteractable == null || 
                        !currentSelected.IsTheSame(lastSelectedInteractable))
                    {
                        lastSelectedInteractable = currentSelected;
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
                string speechText = UIElementFormatter.FormatInteractableForSpeech(interactable);
                
                if (!string.IsNullOrEmpty(speechText))
                {
                    // Don't repeat the same text
                    if (speechText != UINavigationHandler.lastSpokenText)
                    {
                        TolkScreenReader.Instance.Speak(speechText, false); // Don't interrupt for world objects
                        UINavigationHandler.lastSpokenText = speechText;
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error announcing interactable: {ex}");
            }
        }
    }

    // Simple patch to monitor when interactables are added to the selection manager
    [HarmonyPatch(typeof(InteractableSelectionManager), "Add", new System.Type[] { typeof(OrbUiElement), typeof(float) })]
    public class InteractableAddedPatch
    {
        static void Postfix(InteractableSelectionManager __instance, OrbUiElement orb, float distance)
        {
            // Orb added to selection manager - no logging needed
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
                    string speechText = UIElementFormatter.FormatInteractableForSpeech(value);
                    if (!string.IsNullOrEmpty(speechText) && speechText != UINavigationHandler.lastSpokenText)
                    {
                        TolkScreenReader.Instance.Speak(speechText, false);
                        UINavigationHandler.lastSpokenText = speechText;
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in CurrentSelected setter patch: {ex}");
            }
        }
    }
}