using System;
using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using Il2CppCollageMode;
using Il2CppFortressOccident;

[assembly: MelonInfo(typeof(AccessibilityMod.AccessibilityMod), "Disco Elysium Accessibility Mod", "1.0.0", "YourName")]
[assembly: MelonGame("ZAUM Studio", "Disco Elysium")]

namespace AccessibilityMod
{
    public class AccessibilityMod : MelonMod
    {
        public static InteractableSelectionManager lastSelectionManager = null;
        public static CommonPadInteractable lastSelectedInteractable = null;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Accessibility Mod initialized!");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            LoggerInstance.Msg($"Scene loaded: {sceneName} (Index: {buildIndex})");
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
}