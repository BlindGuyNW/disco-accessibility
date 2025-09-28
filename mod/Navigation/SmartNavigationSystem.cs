using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Il2Cpp;
using Il2CppFortressOccident;
using AccessibilityMod.Utils;
using MelonLoader;

namespace AccessibilityMod.Navigation
{
    public class SmartNavigationSystem
    {
        private readonly NavigationStateManager stateManager;
        private readonly MovementController movementController;

        public NavigationStateManager StateManager => stateManager;
        public MovementController MovementController => movementController;

        public SmartNavigationSystem()
        {
            stateManager = new NavigationStateManager();
            movementController = new MovementController();
        }

        public void SelectCategory(ObjectCategory category)
        {
            try
            {
                MelonLogger.Msg($"[SMART NAV] Selecting category: {category}");
                
                // Get current objects from registry
                var registry = MouseOverHighlight.registry;
                if (registry == null || registry.Count == 0)
                {
                    TolkScreenReader.Instance.Speak("No objects available for selection", true);
                    return;
                }
                
                // Find player position
                Vector3 playerPos = GameObjectUtils.GetPlayerPosition();
                if (playerPos == Vector3.zero)
                {
                    TolkScreenReader.Instance.Speak("Could not find player position", true);
                    return;
                }
                
                // Update categorized objects and switch to category
                stateManager.UpdateCategorizedObjects(playerPos, category);
                
                // Announce category contents
                var navInfo = stateManager.GetCurrentNavigationInfo(playerPos);
                string announcement = $"{category}: " +navInfo.FormatAnnouncement();
                
                MelonLogger.Msg($"[SMART NAV] {announcement}");
                TolkScreenReader.Instance.Speak(announcement, true);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[SMART NAV] Error selecting category: {ex}");
                TolkScreenReader.Instance.Speak($"Category selection failed: {ex.Message}", true);
            }
        }

        public void CycleWithinCategory()
        {
            try
            {
                if (!stateManager.HasSelection)
                {
                    TolkScreenReader.Instance.Speak("No objects in current category. Press [ for NPCs, ] for locations, \\ for containers, or = for everything.", true);
                    return;
                }
                
                // Cycle to next object
                stateManager.CycleToNextObject();
                
                // Find player position for announcement
                Vector3 playerPos = GameObjectUtils.GetPlayerPosition();
                if (playerPos == Vector3.zero) return;
                
                // Announce selected object
                var navInfo = stateManager.GetCurrentNavigationInfo(playerPos);
                string announcement = navInfo.FormatAnnouncement();
                
                MelonLogger.Msg($"[SMART NAV] {announcement}");
                TolkScreenReader.Instance.Speak(announcement, true);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[SMART NAV] Error cycling objects: {ex}");
                TolkScreenReader.Instance.Speak($"Cycling failed: {ex.Message}", true);
            }
        }

        public void NavigateToSelectedObject()
        {
            try
            {
                var selectedObject = stateManager.GetCurrentSelectedObject();
                if (selectedObject == null || selectedObject.transform == null)
                {
                    TolkScreenReader.Instance.Speak("No object selected. Select a category first, then use period to cycle.", true);
                    return;
                }
                
                Vector3 destination = selectedObject.transform.position;
                string objectName = ObjectNameCleaner.GetBetterObjectName(selectedObject);
                
                MelonLogger.Msg($"[SMART NAV] Attempting to navigate to {objectName}");
                TolkScreenReader.Instance.Speak($"Calculating path to {objectName}...", true);
                
                // Try automated movement
                movementController.TryNavigateToPosition(destination, objectName);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[SMART NAV] Navigation error: {ex}");
                TolkScreenReader.Instance.Speak($"Navigation failed: {ex.Message}", true);
            }
        }

        public void StopMovement()
        {
            movementController.StopMovement();
        }

        public void UpdateMovement()
        {
            movementController.UpdateMovementProgress();
        }

        public void ToggleSortingMode()
        {
            try
            {
                stateManager.ToggleSortingMode();

                // Re-sort current category with new mode
                Vector3 playerPos = GameObjectUtils.GetPlayerPosition();
                if (playerPos != Vector3.zero && stateManager.HasSelection)
                {
                    // Update objects with new sorting
                    stateManager.UpdateCategorizedObjects(playerPos, stateManager.CurrentCategory);
                }

                string modeName = stateManager.CurrentSortingMode == SortingMode.Directional
                    ? "directional (clockwise)"
                    : "distance";

                string announcement = $"Sorting mode changed to {modeName}";
                MelonLogger.Msg($"[SMART NAV] {announcement}");
                TolkScreenReader.Instance.Speak(announcement, true);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[SMART NAV] Error toggling sorting mode: {ex}");
            }
        }

        public void TestRegistryAccess()
        {
            try
            {
                MelonLogger.Msg("[REGISTRY TEST] Starting scene-wide object scan...");
                
                // Test 1: Access the MouseOverHighlight registry
                var registry = MouseOverHighlight.registry;
                if (registry == null)
                {
                    MelonLogger.Error("[REGISTRY TEST] Registry is null!");
                    TolkScreenReader.Instance.Speak("Registry test failed: registry is null", true);
                    return;
                }
                
                int totalObjects = registry.Count;
                MelonLogger.Msg($"[REGISTRY TEST] Found {totalObjects} objects in registry");
                
                // Test 2: Find player position
                Vector3 playerPos = GameObjectUtils.GetPlayerPosition();
                if (playerPos == Vector3.zero)
                {
                    MelonLogger.Error("[REGISTRY TEST] Could not find player character!");
                    TolkScreenReader.Instance.Speak("Registry test failed: no player found", true);
                    return;
                }
                
                MelonLogger.Msg($"[REGISTRY TEST] Player position: {playerPos}");
                
                // Test 3: Scan all objects for distances
                float maxDistance = 0;
                float minDistance = float.MaxValue;
                string furthestObject = "";
                string nearestObject = "";
                int objectsOver20m = 0;
                
                foreach (var obj in registry)
                {
                    if (obj == null || obj.transform == null) continue;
                    
                    float distance = Vector3.Distance(playerPos, obj.transform.position);
                    string objName = ObjectNameCleaner.GetBetterObjectName(obj);
                    
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                        furthestObject = objName;
                    }
                    
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestObject = objName;
                    }
                    
                    if (distance > 20.0f)
                    {
                        objectsOver20m++;
                    }
                    
                    MelonLogger.Msg($"[REGISTRY TEST] Object: {objName} at distance {distance:F1}m");
                }
                
                // Test 4: Compare to selection manager's limited range
                var selectionManager = UnityEngine.Object.FindObjectOfType<CharacterAnalogueControl>();
                int nearbyOnly = 0;
                if (selectionManager != null && selectionManager.m_interactableSelectionManager != null)
                {
                    nearbyOnly = selectionManager.m_interactableSelectionManager.m_availableInteractables.Count;
                }
                
                // Announce results
                string result = $"Registry scan complete! Found {totalObjects} total objects in scene. " +
                               $"Nearest: {nearestObject} at {minDistance:F1} meters. " +
                               $"Furthest: {furthestObject} at {maxDistance:F1} meters. " +
                               $"{objectsOver20m} objects beyond 20 meters. " +
                               $"Selection manager only sees {nearbyOnly} nearby objects.";
                
                MelonLogger.Msg($"[REGISTRY TEST] {result}");
                TolkScreenReader.Instance.Speak(result, true);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[REGISTRY TEST] Error: {ex}");
                TolkScreenReader.Instance.Speak($"Registry test failed with error: {ex.Message}", true);
            }
        }

        public void ScanSceneByDistance()
        {
            try
            {
                MelonLogger.Msg("[DISTANCE SCAN] Starting distance-based scene scan...");
                
                // Access the registry
                var registry = MouseOverHighlight.registry;
                if (registry == null || registry.Count == 0)
                {
                    TolkScreenReader.Instance.Speak("No objects found in scene", true);
                    return;
                }
                
                // Find player position
                Vector3 playerPos = GameObjectUtils.GetPlayerPosition();
                if (playerPos == Vector3.zero)
                {
                    TolkScreenReader.Instance.Speak("Could not find player position", true);
                    return;
                }
                
                // Group objects by distance
                var immediate = new List<string>();  // 0-5m
                var nearby = new List<string>();     // 5-15m
                var shortWalk = new List<string>();  // 15-30m
                var mediumDist = new List<string>(); // 30-50m
                int distantCount = 0;                // 50m+
                
                foreach (var obj in registry)
                {
                    if (obj == null || obj.transform == null) continue;
                    
                    float distance = Vector3.Distance(playerPos, obj.transform.position);
                    string name = ObjectNameCleaner.GetBetterObjectName(obj);
                    
                    if (distance <= 5f)
                        immediate.Add($"{name} ({distance:F0}m)");
                    else if (distance <= 15f)
                        nearby.Add($"{name} ({distance:F0}m)");
                    else if (distance <= 30f)
                        shortWalk.Add($"{name} ({distance:F0}m)");
                    else if (distance <= 50f)
                        mediumDist.Add($"{name} ({distance:F0}m)");
                    else
                        distantCount++;
                }
                
                // Build report
                string report = $"Scene scan: {registry.Count} objects found.";
                
                if (immediate.Count > 0)
                    report += $" Right here: {string.Join(", ", immediate.Take(5).ToArray())}" + 
                             (immediate.Count > 5 ? $" and {immediate.Count - 5} more." : ".");
                             
                if (nearby.Count > 0)
                    report += $" Nearby: {string.Join(", ", nearby.Take(8).ToArray())}" + 
                             (nearby.Count > 8 ? $" and {nearby.Count - 8} more." : ".");
                             
                if (shortWalk.Count > 0)
                    report += $" Short walk: {string.Join(", ", shortWalk.Take(5).ToArray())}" + 
                             (shortWalk.Count > 5 ? $" and {shortWalk.Count - 5} more." : ".");
                             
                if (mediumDist.Count > 0)
                    report += $" Medium distance: {string.Join(", ", mediumDist.Take(3).ToArray())}" + 
                             (mediumDist.Count > 3 ? $" and {mediumDist.Count - 3} more." : ".");
                             
                if (distantCount > 0)
                    report += $" {distantCount} distant objects beyond 50 meters.";
                
                MelonLogger.Msg($"[DISTANCE SCAN] {report}");
                TolkScreenReader.Instance.Speak(report, true);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[DISTANCE SCAN] Error: {ex}");
                TolkScreenReader.Instance.Speak($"Distance scan failed: {ex.Message}", true);
            }
        }
    }
}