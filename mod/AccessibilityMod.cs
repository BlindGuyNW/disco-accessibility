using System;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI.Extensions;
using Il2Cpp;
using Il2CppCollageMode;
using Il2CppFortressOccident;
using Il2CppSunshine;
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
            
            // Move mouse cursor to safe position to prevent UI interference
            try
            {
                // Move mouse to bottom-right corner where there's typically no UI
                Cursor.SetCursor(null, new Vector2(Screen.width - 10, Screen.height - 10), CursorMode.Auto);
                // Also try to set the actual mouse position if possible
                Input.mousePosition.Set(Screen.width - 10, Screen.height - 10, 0);
                LoggerInstance.Msg("Mouse cursor repositioned to safe area");
            }
            catch (Exception ex)
            {
                LoggerInstance.Warning($"Could not reposition mouse cursor: {ex.Message}");
            }
            
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
            
            // Reposition mouse cursor when scene loads to prevent UI interference
            try
            {
                // Move to corner where there's typically no UI
                Cursor.SetCursor(null, new Vector2(Screen.width - 10, Screen.height - 10), CursorMode.Auto);
                LoggerInstance.Msg("Mouse cursor repositioned after scene load");
            }
            catch (Exception ex)
            {
                LoggerInstance.Warning($"Could not reposition mouse after scene load: {ex.Message}");
            }
        }
        
        public override void OnUpdate()
        {
            // Test hotkey: Semicolon to scan entire scene registry
            if (Input.GetKeyDown(KeyCode.Semicolon))
            {
                TestRegistryAccess();
            }
            
            // Distance-based scene scanner: Quote (')
            if (Input.GetKeyDown(KeyCode.Quote))
            {
                ScanSceneByDistance();
            }
            
            // Category selection keys (safe punctuation)
            if (Input.GetKeyDown(KeyCode.LeftBracket))  // [
            {
                SelectCategory(ObjectCategory.NPCs);
            }
            else if (Input.GetKeyDown(KeyCode.RightBracket))  // ]
            {
                SelectCategory(ObjectCategory.Locations);
            }
            else if (Input.GetKeyDown(KeyCode.Backslash))  // \
            {
                SelectCategory(ObjectCategory.Loot);
            }
            else if (Input.GetKeyDown(KeyCode.Equals))  // =
            {
                SelectCategory(ObjectCategory.Everything);
            }
            
            // Cycle within current category: Period (.)
            if (Input.GetKeyDown(KeyCode.Period))
            {
                CycleWithinCategory();
            }
            
            // Navigate to selected object: Comma (,)
            if (Input.GetKeyDown(KeyCode.Comma))
            {
                NavigateToSelectedObject();
            }
            
            // Stop automated movement: Slash (/)
            if (Input.GetKeyDown(KeyCode.Slash))
            {
                StopAutomatedMovement();
            }
            
            // Monitor automated movement progress
            if (isMonitoringMovement && monitoredCharacter != null)
            {
                MonitorMovementProgress();
            }
            
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
        
        private void TestRegistryAccess()
        {
            try
            {
                LoggerInstance.Msg("[REGISTRY TEST] Starting scene-wide object scan...");
                
                // Test 1: Access the MouseOverHighlight registry
                var registry = MouseOverHighlight.registry;
                if (registry == null)
                {
                    LoggerInstance.Error("[REGISTRY TEST] Registry is null!");
                    TolkScreenReader.Instance.Speak("Registry test failed: registry is null", true);
                    return;
                }
                
                int totalObjects = registry.Count;
                LoggerInstance.Msg($"[REGISTRY TEST] Found {totalObjects} objects in registry");
                
                // Test 2: Find player position
                var character = UnityEngine.Object.FindObjectOfType<Character>();
                if (character == null)
                {
                    LoggerInstance.Error("[REGISTRY TEST] Could not find player character!");
                    TolkScreenReader.Instance.Speak("Registry test failed: no player found", true);
                    return;
                }
                
                Vector3 playerPos = character.transform.position;
                LoggerInstance.Msg($"[REGISTRY TEST] Player position: {playerPos}");
                
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
                    string objName = obj.gameObject != null ? obj.gameObject.name : "Unknown";
                    
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
                    
                    LoggerInstance.Msg($"[REGISTRY TEST] Object: {objName} at distance {distance:F1}m");
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
                
                LoggerInstance.Msg($"[REGISTRY TEST] {result}");
                TolkScreenReader.Instance.Speak(result, true);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"[REGISTRY TEST] Error: {ex}");
                TolkScreenReader.Instance.Speak($"Registry test failed with error: {ex.Message}", true);
            }
        }
        
        private void ScanSceneByDistance()
        {
            try
            {
                LoggerInstance.Msg("[DISTANCE SCAN] Starting distance-based scene scan...");
                
                // Access the registry
                var registry = MouseOverHighlight.registry;
                if (registry == null || registry.Count == 0)
                {
                    TolkScreenReader.Instance.Speak("No objects found in scene", true);
                    return;
                }
                
                // Find player position
                var character = UnityEngine.Object.FindObjectOfType<Character>();
                if (character == null)
                {
                    TolkScreenReader.Instance.Speak("Could not find player position", true);
                    return;
                }
                
                Vector3 playerPos = character.transform.position;
                
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
                    string name = GetBetterObjectName(obj);
                    
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
                
                LoggerInstance.Msg($"[DISTANCE SCAN] {report}");
                TolkScreenReader.Instance.Speak(report, true);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"[DISTANCE SCAN] Error: {ex}");
                TolkScreenReader.Instance.Speak($"Distance scan failed: {ex.Message}", true);
            }
        }
        
        private string GetBetterObjectName(MouseOverHighlight obj)
        {
            try
            {
                // Try to get GameEntity name first (more descriptive)
                var entity = obj.GetFirstActive();
                if (entity != null && !string.IsNullOrEmpty(entity.name))
                {
                    return CleanObjectName(entity.name);
                }
                
                // Fall back to GameObject name
                if (obj.gameObject != null && !string.IsNullOrEmpty(obj.gameObject.name))
                {
                    return CleanObjectName(obj.gameObject.name);
                }
                
                return "Unknown Object";
            }
            catch
            {
                return "Unknown Object";
            }
        }
        
        private string CleanObjectName(string rawName)
        {
            // Remove common Unity suffixes and prefixes
            string cleaned = rawName;
            
            // Remove (Clone) suffix
            if (cleaned.EndsWith("(Clone)"))
                cleaned = cleaned.Substring(0, cleaned.Length - 7);
            
            // Remove numbers and underscores at the end (e.g., "Container_01")
            while (cleaned.Length > 0 && (char.IsDigit(cleaned[cleaned.Length - 1]) || cleaned[cleaned.Length - 1] == '_'))
                cleaned = cleaned.Substring(0, cleaned.Length - 1);
            
            // Replace underscores with spaces
            cleaned = cleaned.Replace("_", " ");
            
            // Capitalize first letter
            if (cleaned.Length > 0)
                cleaned = char.ToUpper(cleaned[0]) + (cleaned.Length > 1 ? cleaned.Substring(1).ToLower() : "");
            
            return string.IsNullOrEmpty(cleaned) ? "Object" : cleaned;
        }
        
        private ObjectCategory CategorizeObject(MouseOverHighlight obj, Vector3 playerPos)
        {
            try
            {
                string name = GetBetterObjectName(obj).ToLower();
                string gameObjectName = obj.gameObject?.name?.ToLower() ?? "";
                float distance = Vector3.Distance(playerPos, obj.transform.position);
                
                // Check for NPCs - people you can talk to (but exclude Kim)
                if (IsInteractiveNPC(name, gameObjectName))
                {
                    return ObjectCategory.NPCs;
                }
                
                // Check for important locations - doors, exits, vehicles, story objects
                if (IsImportantLocation(name, gameObjectName))
                {
                    return ObjectCategory.Locations;
                }
                
                // Check for loot and containers - searchable items
                if (IsLootOrContainer(name, gameObjectName))
                {
                    return ObjectCategory.Loot;
                }
                
                // Everything else
                return ObjectCategory.Everything;
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"[CATEGORIZATION] Error categorizing object: {ex}");
                return ObjectCategory.Everything;
            }
        }
        
        private bool IsInteractiveNPC(string name, string gameObjectName)
        {
            // Exclude Kim who follows us around
            if (name.Contains("kim") || gameObjectName.Contains("kim"))
                return false;
            
            // Look for full names with capital letters indicating NPCs
            // Examples: "Tommy Lhomme", "Cuno", "Shop Owner"
            string[] npcPatterns = {
                "tommy", "cuno", "measurehead", "joyce", "evrart", "garte", "lena", 
                "plaisance", "soona", "idiot doom spiral", "annoying bird", "classical",
                "paledriver" // Actually an NPC despite the name
            };
            
            foreach (string pattern in npcPatterns)
            {
                if (name.Contains(pattern) || gameObjectName.Contains(pattern))
                    return true;
            }
            
            // Check for general NPC patterns
            return (name.Contains("person") || name.Contains("character") || name.Contains("npc") ||
                    gameObjectName.Contains("person") || gameObjectName.Contains("character"));
        }
        
        private bool IsImportantLocation(string name, string gameObjectName)
        {
            string[] locationPatterns = {
                "door", "entrance", "exit", "gate", "passage", "stairway", "stairs",
                "kineema", "car", "vehicle", "monument", "terminal", "phone", "booth",
                "building", "cabin", "shack", "harbor", "pier", "bridge"
            };
            
            foreach (string pattern in locationPatterns)
            {
                if (name.Contains(pattern) || gameObjectName.Contains(pattern))
                    return true;
            }
            
            return false;
        }
        
        private bool IsLootOrContainer(string name, string gameObjectName)
        {
            // Filter out obvious clutter first
            if (IsClutter(name, gameObjectName))
                return false;
            
            string[] containerPatterns = {
                "box", "crate", "container", "barrel", "chest", "bag", "pile",
                "money", "cash", "coin", "bottle", "item", "loot", "stash",
                "woodpile", "bagpile"
            };
            
            foreach (string pattern in containerPatterns)
            {
                if (name.Contains(pattern) || gameObjectName.Contains(pattern))
                    return true;
            }
            
            return false;
        }
        
        private bool IsClutter(string name, string gameObjectName)
        {
            string[] clutterPatterns = {
                "empty bottle", "trash", "broken", "debris", "rubble",
                "junk", "waste", "garbage"
            };
            
            foreach (string pattern in clutterPatterns)
            {
                if (name.Contains(pattern) || gameObjectName.Contains(pattern))
                    return true;
            }
            
            return false;
        }
        
        // Object categorization system
        public enum ObjectCategory
        {
            NPCs = 1,           // Interactive NPCs (excluding Kim)
            Locations = 2,      // Doors, exits, vehicles, story objects
            Loot = 3,          // Containers, money, pickuppable items
            Everything = 4      // Fallback category
        }
        
        private Dictionary<ObjectCategory, List<MouseOverHighlight>> categorizedObjects = new Dictionary<ObjectCategory, List<MouseOverHighlight>>();
        private ObjectCategory currentCategory = ObjectCategory.NPCs;
        private int selectedObjectIndex = -1;
        
        // Movement monitoring
        private bool isMonitoringMovement = false;
        private Character monitoredCharacter = null;
        private Vector3 movementDestination;
        private string movementTargetName = "";
        private float lastDistanceAnnouncement = 0f;
        
        private void SelectCategory(ObjectCategory category)
        {
            try
            {
                LoggerInstance.Msg($"[CATEGORY] Selecting category: {category}");
                
                // Get current objects from registry
                var registry = MouseOverHighlight.registry;
                if (registry == null || registry.Count == 0)
                {
                    TolkScreenReader.Instance.Speak("No objects available for selection", true);
                    return;
                }
                
                // Find player position
                var character = UnityEngine.Object.FindObjectOfType<Character>();
                if (character == null)
                {
                    TolkScreenReader.Instance.Speak("Could not find player position", true);
                    return;
                }
                
                Vector3 playerPos = character.transform.position;
                
                // Clear and populate categorized objects
                categorizedObjects.Clear();
                foreach (ObjectCategory cat in Enum.GetValues(typeof(ObjectCategory)))
                {
                    categorizedObjects[cat] = new List<MouseOverHighlight>();
                }
                
                // Categorize all objects within range
                foreach (var obj in registry)
                {
                    if (obj == null || obj.transform == null) continue;
                    
                    float distance = Vector3.Distance(playerPos, obj.transform.position);
                    
                    // Apply category-specific distance limits
                    float maxDistance = GetMaxDistanceForCategory(category);
                    if (distance > maxDistance) continue;
                    
                    ObjectCategory objCategory = CategorizeObject(obj, playerPos);
                    categorizedObjects[objCategory].Add(obj);
                }
                
                // Sort each category by distance
                foreach (var categoryList in categorizedObjects.Values)
                {
                    categoryList.Sort((a, b) => 
                        Vector3.Distance(playerPos, a.transform.position)
                        .CompareTo(Vector3.Distance(playerPos, b.transform.position)));
                }
                
                // Switch to selected category
                currentCategory = category;
                selectedObjectIndex = 0;
                
                // Announce category contents
                AnnounceCategory(category, playerPos);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"[CATEGORY] Error selecting category: {ex}");
                TolkScreenReader.Instance.Speak($"Category selection failed: {ex.Message}", true);
            }
        }
        
        private float GetMaxDistanceForCategory(ObjectCategory category)
        {
            return category switch
            {
                ObjectCategory.NPCs => 50f,         // Important people can be further
                ObjectCategory.Locations => 50f,    // Doors/exits can be further  
                ObjectCategory.Loot => 30f,         // Containers usually nearby
                ObjectCategory.Everything => 20f,   // Avoid overwhelming with distant clutter
                _ => 30f
            };
        }
        
        private void AnnounceCategory(ObjectCategory category, Vector3 playerPos)
        {
            var objects = categorizedObjects[category];
            string categoryName = GetCategoryDisplayName(category);
            
            if (objects.Count == 0)
            {
                TolkScreenReader.Instance.Speak($"No {categoryName} nearby", true);
                return;
            }
            
            // Announce first object in category
            var firstObj = objects[0];
            float distance = Vector3.Distance(playerPos, firstObj.transform.position);
            string name = GetBetterObjectName(firstObj);
            string direction = GetCardinalDirection(playerPos, firstObj.transform.position);
            
            string announcement = $"{categoryName} 1 of {objects.Count}: {name}, " +
                                $"{distance:F0} meters {direction}. Press period to cycle, comma to navigate.";
            
            LoggerInstance.Msg($"[CATEGORY] {announcement}");
            TolkScreenReader.Instance.Speak(announcement, true);
        }
        
        private string GetCategoryDisplayName(ObjectCategory category)
        {
            return category switch
            {
                ObjectCategory.NPCs => "NPC",
                ObjectCategory.Locations => "Location", 
                ObjectCategory.Loot => "Container",
                ObjectCategory.Everything => "Object",
                _ => "Item"
            };
        }
        
        private void CycleWithinCategory()
        {
            try
            {
                if (!categorizedObjects.ContainsKey(currentCategory) || 
                    categorizedObjects[currentCategory].Count == 0)
                {
                    TolkScreenReader.Instance.Speak("No objects in current category. Press [ for NPCs, ] for locations, \\ for containers, or = for everything.", true);
                    return;
                }
                
                var objects = categorizedObjects[currentCategory];
                selectedObjectIndex = (selectedObjectIndex + 1) % objects.Count;
                
                // Find player position for distance calculation
                var character = UnityEngine.Object.FindObjectOfType<Character>();
                if (character == null) return;
                Vector3 playerPos = character.transform.position;
                
                // Announce selected object
                var selectedObj = objects[selectedObjectIndex];
                float distance = Vector3.Distance(playerPos, selectedObj.transform.position);
                string name = GetBetterObjectName(selectedObj);
                string direction = GetCardinalDirection(playerPos, selectedObj.transform.position);
                string categoryName = GetCategoryDisplayName(currentCategory);
                
                string announcement = $"{categoryName} {selectedObjectIndex + 1} of {objects.Count}: {name}, " +
                                    $"{distance:F0} meters {direction}. Press comma to navigate.";
                
                LoggerInstance.Msg($"[CYCLE] {announcement}");
                TolkScreenReader.Instance.Speak(announcement, true);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"[CYCLE] Error cycling objects: {ex}");
                TolkScreenReader.Instance.Speak($"Cycling failed: {ex.Message}", true);
            }
        }
        
        private string GetCardinalDirection(Vector3 from, Vector3 to)
        {
            Vector3 direction = (to - from).normalized;
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            
            // Convert to cardinal directions
            if (angle < -157.5f || angle > 157.5f) return "south";
            if (angle >= -157.5f && angle < -112.5f) return "southwest";
            if (angle >= -112.5f && angle < -67.5f) return "west";
            if (angle >= -67.5f && angle < -22.5f) return "northwest";
            if (angle >= -22.5f && angle < 22.5f) return "north";
            if (angle >= 22.5f && angle < 67.5f) return "northeast";
            if (angle >= 67.5f && angle < 112.5f) return "east";
            if (angle >= 112.5f && angle < 157.5f) return "southeast";
            
            return "unknown direction";
        }
        
        private void NavigateToSelectedObject()
        {
            try
            {
                if (!categorizedObjects.ContainsKey(currentCategory) || 
                    categorizedObjects[currentCategory].Count == 0 ||
                    selectedObjectIndex < 0)
                {
                    TolkScreenReader.Instance.Speak("No object selected. Select a category first, then use period to cycle.", true);
                    return;
                }
                
                var objects = categorizedObjects[currentCategory];
                if (selectedObjectIndex >= objects.Count)
                {
                    TolkScreenReader.Instance.Speak("Selected object index out of range", true);
                    return;
                }
                
                var targetObject = objects[selectedObjectIndex];
                if (targetObject == null || targetObject.transform == null)
                {
                    TolkScreenReader.Instance.Speak("Selected object is no longer available", true);
                    return;
                }
                
                // Find player
                var character = UnityEngine.Object.FindObjectOfType<Character>();
                if (character == null)
                {
                    TolkScreenReader.Instance.Speak("Could not find player position", true);
                    return;
                }
                
                Vector3 playerPos = character.transform.position;
                Vector3 destination = targetObject.transform.position;
                string objectName = GetBetterObjectName(targetObject);
                
                LoggerInstance.Msg($"[NAVIGATION] Attempting to navigate to {objectName}");
                TolkScreenReader.Instance.Speak($"Calculating path to {objectName}...", true);
                
                // Try pathfinding with robust fallbacks
                if (TryNavigateWithPathfinding(playerPos, destination, objectName))
                {
                    // NavMesh pathfinding succeeded
                    return;
                }
                
                // Fallback to directional guidance
                GiveDirectionalGuidance(playerPos, destination, objectName);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"[NAVIGATION] Error: {ex}");
                TolkScreenReader.Instance.Speak($"Navigation failed: {ex.Message}", true);
            }
        }
        
        private Character GetPlayerCharacter()
        {
            try
            {
                LoggerInstance.Msg("[NAVIGATION] Searching for player character...");
                
                // Method 1: Try Character.Main static property
                try
                {
                    var mainChar = Character.Main;
                    if (mainChar != null)
                    {
                        LoggerInstance.Msg($"[NAVIGATION] Found Character.Main: {mainChar.name}");
                        // Test that the object is valid for Il2Cpp calls
                        var testStatus = mainChar.movementStatus;
                        LoggerInstance.Msg($"[NAVIGATION] Character.Main status test: {testStatus}");
                        return mainChar;
                    }
                    else
                    {
                        LoggerInstance.Msg("[NAVIGATION] Character.Main is null");
                    }
                }
                catch (Exception ex)
                {
                    LoggerInstance.Error($"[NAVIGATION] Error accessing Character.Main: {ex}");
                }
                
                // Method 2: Find by UnityEngine.Object
                try
                {
                    var foundChar = UnityEngine.Object.FindObjectOfType<Character>();
                    if (foundChar != null)
                    {
                        LoggerInstance.Msg($"[NAVIGATION] Found Character by type: {foundChar.name}");
                        // Test that the object is valid for Il2Cpp calls
                        var testStatus = foundChar.movementStatus;
                        LoggerInstance.Msg($"[NAVIGATION] Found Character status test: {testStatus}");
                        return foundChar;
                    }
                    else
                    {
                        LoggerInstance.Msg("[NAVIGATION] FindObjectOfType<Character>() returned null");
                    }
                }
                catch (Exception ex)
                {
                    LoggerInstance.Error($"[NAVIGATION] Error finding Character by type: {ex}");
                }
                
                LoggerInstance.Error("[NAVIGATION] Failed to find valid Character object");
                return null;
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"[NAVIGATION] Error in GetPlayerCharacter: {ex}");
                return null;
            }
        }
        
        private bool TryNavigateWithPathfinding(Vector3 start, Vector3 destination, string objectName)
        {
            try
            {
                LoggerInstance.Msg("[NAVIGATION] Attempting automated character movement...");
                
                // Get the player character with enhanced detection
                var character = GetPlayerCharacter();
                if (character == null)
                {
                    LoggerInstance.Error("[NAVIGATION] Could not find valid Character for automated movement");
                    return false;
                }
                
                // Check current movement status
                try
                {
                    var currentStatus = character.movementStatus;
                    LoggerInstance.Msg($"[NAVIGATION] Current movement status: {currentStatus}");
                    
                    if (currentStatus == Character.MovementStatus.MOVING)
                    {
                        TolkScreenReader.Instance.Speak("Character is already moving. Please wait.", true);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    LoggerInstance.Error($"[NAVIGATION] Error checking movement status: {ex}");
                    return false;
                }
                
                // Calculate distance for feedback
                float distance = Vector3.Distance(start, destination);
                
                // Create proper Il2Cpp nullable for heading parameter
                try
                {
                    LoggerInstance.Msg($"[NAVIGATION] Calling SetDestination for {objectName} at {destination}");
                    
                    // Create a proper Il2Cpp nullable float
                    var nullableHeading = new Il2CppSystem.Nullable<float>();
                    
                    // Try with WALK mode first (more reliable than AUTOMATIC)
                    character.SetDestination(destination, nullableHeading, MovementMode.WALK, false);
                    
                    LoggerInstance.Msg($"[NAVIGATION] SetDestination succeeded for {objectName}");
                    TolkScreenReader.Instance.Speak(
                        $"Walking to {objectName}, {distance:F1} meters away. Character will move automatically.", true);
                    
                    // Start monitoring movement progress
                    StartMovementMonitoring(character, destination, objectName);
                    
                    return true;
                }
                catch (Exception ex)
                {
                    LoggerInstance.Error($"[NAVIGATION] SetDestination failed: {ex}");
                    
                    // Try alternative approach with explicit heading
                    try
                    {
                        LoggerInstance.Msg("[NAVIGATION] Trying SetDestination with explicit heading...");
                        var heading = new Il2CppSystem.Nullable<float>(0f); // Face north
                        character.SetDestination(destination, heading, MovementMode.WALK, false);
                        
                        LoggerInstance.Msg($"[NAVIGATION] SetDestination with heading succeeded for {objectName}");
                        TolkScreenReader.Instance.Speak(
                            $"Walking to {objectName}, {distance:F1} meters away. Character will move automatically.", true);
                        
                        StartMovementMonitoring(character, destination, objectName);
                        return true;
                    }
                    catch (Exception ex2)
                    {
                        LoggerInstance.Error($"[NAVIGATION] SetDestination with heading also failed: {ex2}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"[NAVIGATION] Automated movement error: {ex}");
                return false;
            }
        }
        
        private void StartMovementMonitoring(Character character, Vector3 destination, string objectName)
        {
            isMonitoringMovement = true;
            monitoredCharacter = character;
            movementDestination = destination;
            movementTargetName = objectName;
            lastDistanceAnnouncement = Time.time;
            
            LoggerInstance.Msg($"[NAVIGATION] Started monitoring movement to {objectName}");
        }
        
        private void MonitorMovementProgress()
        {
            try
            {
                var status = monitoredCharacter.movementStatus;
                var currentPos = monitoredCharacter.transform.position;
                float currentDistance = Vector3.Distance(currentPos, movementDestination);
                
                // Check if movement completed
                if (status == Character.MovementStatus.IDLE || status == Character.MovementStatus.COMPLETED)
                {
                    if (currentDistance < 2.0f) // Successfully reached destination
                    {
                        TolkScreenReader.Instance.Speak($"Arrived at {movementTargetName}", true);
                        LoggerInstance.Msg($"[NAVIGATION] Successfully arrived at {movementTargetName}");
                    }
                    else
                    {
                        TolkScreenReader.Instance.Speak($"Movement stopped. {currentDistance:F1} meters from {movementTargetName}", true);
                        LoggerInstance.Msg($"[NAVIGATION] Movement stopped at {currentDistance:F1}m from target");
                    }
                    
                    isMonitoringMovement = false;
                    return;
                }
                
                // Check if movement failed or got stuck
                if (status == Character.MovementStatus.BROKEN)
                {
                    TolkScreenReader.Instance.Speak($"Movement failed. Unable to reach {movementTargetName}", true);
                    LoggerInstance.Msg("[NAVIGATION] Movement status is BROKEN");
                    isMonitoringMovement = false;
                    return;
                }
                
                // Provide periodic distance updates (every 3 seconds while moving)
                if (status == Character.MovementStatus.MOVING && Time.time - lastDistanceAnnouncement > 3.0f)
                {
                    if (currentDistance > 5.0f) // Only announce if still far away
                    {
                        TolkScreenReader.Instance.Speak($"{currentDistance:F0} meters to {movementTargetName}", true);
                        lastDistanceAnnouncement = Time.time;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"[NAVIGATION] Movement monitoring error: {ex}");
                isMonitoringMovement = false;
            }
        }
        
        private void StopAutomatedMovement()
        {
            if (!isMonitoringMovement)
            {
                TolkScreenReader.Instance.Speak("No active movement to stop", true);
                return;
            }
            
            try
            {
                if (monitoredCharacter != null)
                {
                    // Stop character movement by setting destination to current position
                    monitoredCharacter.SetDestination(monitoredCharacter.transform.position, null, MovementMode.AUTOMATIC, false);
                    LoggerInstance.Msg("[NAVIGATION] Automated movement stopped by user");
                }
                
                TolkScreenReader.Instance.Speak("Movement stopped", true);
                isMonitoringMovement = false;
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"[NAVIGATION] Error stopping movement: {ex}");
                TolkScreenReader.Instance.Speak("Error stopping movement", true);
            }
        }
        
        private void GiveDirectionalGuidance(Vector3 start, Vector3 destination, string objectName)
        {
            float distance = Vector3.Distance(start, destination);
            string direction = GetCardinalDirection(start, destination);
            
            string guidance = $"Direct path to {objectName}: {distance:F1} meters {direction}. " +
                            $"Face {direction} and walk forward.";
            
            LoggerInstance.Msg($"[NAVIGATION] Providing directional guidance: {guidance}");
            TolkScreenReader.Instance.Speak(guidance, true);
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
    [HarmonyPatch(typeof(InteractableSelectionManager), "Add", new System.Type[] { typeof(OrbUiElement), typeof(float) })]
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