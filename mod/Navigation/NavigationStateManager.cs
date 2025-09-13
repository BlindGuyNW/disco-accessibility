using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Il2Cpp;
using Il2CppFortressOccident;
using AccessibilityMod.Utils;

namespace AccessibilityMod.Navigation
{
    public enum SortingMode
    {
        Distance,
        Directional
    }

    public class NavigationStateManager
    {
        private Dictionary<ObjectCategory, List<MouseOverHighlight>> categorizedObjects = new Dictionary<ObjectCategory, List<MouseOverHighlight>>();
        private ObjectCategory currentCategory = ObjectCategory.NPCs;
        private int selectedObjectIndex = -1;
        private SortingMode currentSortingMode = SortingMode.Directional;

        public ObjectCategory CurrentCategory => currentCategory;
        public int SelectedObjectIndex => selectedObjectIndex;
        public bool HasSelection => selectedObjectIndex >= 0 && HasObjectsInCategory(currentCategory);
        public SortingMode CurrentSortingMode => currentSortingMode;

        public NavigationStateManager()
        {
            // Initialize all categories
            foreach (ObjectCategory category in Enum.GetValues(typeof(ObjectCategory)))
            {
                categorizedObjects[category] = new List<MouseOverHighlight>();
            }
        }

        public void UpdateCategorizedObjects(Vector3 playerPos, ObjectCategory targetCategory)
        {
            try
            {
                // Get current objects from registry
                var registry = MouseOverHighlight.registry;
                if (registry == null || registry.Count == 0)
                {
                    ClearAllCategories();
                    return;
                }

                // Clear and populate categorized objects
                ClearAllCategories();

                // Special handling for Everything category - add ALL objects within range
                if (targetCategory == ObjectCategory.Everything)
                {
                    float maxDistance = ObjectCategorizer.GetMaxDistanceForCategory(ObjectCategory.Everything);
                    foreach (var obj in registry)
                    {
                        if (obj == null || obj.transform == null) continue;

                        float distance = Vector3.Distance(playerPos, obj.transform.position);
                        if (distance > maxDistance) continue;

                        // Add to Everything category regardless of what it is
                        categorizedObjects[ObjectCategory.Everything].Add(obj);
                    }
                }
                else
                {
                    // Normal categorization for specific categories
                    foreach (var obj in registry)
                    {
                        if (obj == null || obj.transform == null) continue;

                        float distance = Vector3.Distance(playerPos, obj.transform.position);

                        // Apply category-specific distance limits
                        float maxDistance = ObjectCategorizer.GetMaxDistanceForCategory(targetCategory);
                        if (distance > maxDistance) continue;

                        ObjectCategory objCategory = ObjectCategorizer.CategorizeObject(obj, playerPos);
                        categorizedObjects[objCategory].Add(obj);
                    }
                }
                
                // Sort each category based on current sorting mode
                foreach (var categoryList in categorizedObjects.Values)
                {
                    if (currentSortingMode == SortingMode.Directional)
                    {
                        // Sort by angular position (clockwise from North) with reachability weighting
                        // First group by reachability-weighted distance ranges, then sort by angle within each range
                        categoryList.Sort((a, b) =>
                        {
                            // Use reachability-weighted distance to maintain same-level priority
                            float weightedDistA = DirectionCalculator.CalculateReachabilityWeightedDistance(playerPos, a.transform.position);
                            float weightedDistB = DirectionCalculator.CalculateReachabilityWeightedDistance(playerPos, b.transform.position);

                            // Define distance ranges (0-10m, 10-20m, 20-30m, etc.) based on weighted distance
                            int rangeA = (int)(weightedDistA / 10);
                            int rangeB = (int)(weightedDistB / 10);

                            // Sort by weighted distance range first (maintains level priority)
                            if (rangeA != rangeB)
                                return rangeA.CompareTo(rangeB);

                            // Within same range, sort by angle (clockwise from North)
                            float angleA = DirectionCalculator.GetAngleToTarget(playerPos, a.transform.position);
                            float angleB = DirectionCalculator.GetAngleToTarget(playerPos, b.transform.position);
                            return angleA.CompareTo(angleB);
                        });
                    }
                    else
                    {
                        // Original distance-based sorting with reachability weighting
                        categoryList.Sort((a, b) =>
                            DirectionCalculator.CalculateReachabilityWeightedDistance(playerPos, a.transform.position)
                            .CompareTo(DirectionCalculator.CalculateReachabilityWeightedDistance(playerPos, b.transform.position)));
                    }
                }
                
                // Switch to selected category and reset selection
                currentCategory = targetCategory;
                selectedObjectIndex = HasObjectsInCategory(targetCategory) ? 0 : -1;
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Error($"[NAVIGATION STATE] Error updating categorized objects: {ex}");
            }
        }

        public MouseOverHighlight GetCurrentSelectedObject()
        {
            if (!HasSelection) return null;
            
            var objects = categorizedObjects[currentCategory];
            if (selectedObjectIndex >= objects.Count) return null;
            
            return objects[selectedObjectIndex];
        }

        public void CycleToNextObject()
        {
            if (!HasObjectsInCategory(currentCategory)) return;
            
            var objects = categorizedObjects[currentCategory];
            selectedObjectIndex = (selectedObjectIndex + 1) % objects.Count;
        }

        public int GetObjectCountForCategory(ObjectCategory category)
        {
            return categorizedObjects.ContainsKey(category) ? categorizedObjects[category].Count : 0;
        }

        public bool HasObjectsInCategory(ObjectCategory category)
        {
            return GetObjectCountForCategory(category) > 0;
        }

        public List<MouseOverHighlight> GetObjectsInCategory(ObjectCategory category)
        {
            return categorizedObjects.ContainsKey(category) ? categorizedObjects[category] : new List<MouseOverHighlight>();
        }

        private void ClearAllCategories()
        {
            foreach (var categoryList in categorizedObjects.Values)
            {
                categoryList.Clear();
            }
            selectedObjectIndex = -1;
        }

        public void ResetSelection()
        {
            selectedObjectIndex = -1;
        }

        public void ToggleSortingMode()
        {
            currentSortingMode = currentSortingMode == SortingMode.Distance
                ? SortingMode.Directional
                : SortingMode.Distance;
        }

        public void SetSortingMode(SortingMode mode)
        {
            currentSortingMode = mode;
        }

        public NavigationInfo GetCurrentNavigationInfo(Vector3 playerPos)
        {
            var selectedObj = GetCurrentSelectedObject();
            if (selectedObj == null)
            {
                return new NavigationInfo
                {
                    HasSelection = false,
                    ObjectName = "",
                    Distance = 0f,
                    Direction = "",
                    CurrentIndex = 0,
                    TotalCount = GetObjectCountForCategory(currentCategory),
                    CategoryName = ObjectCategorizer.GetCategoryDisplayName(currentCategory)
                };
            }

            float distance = Vector3.Distance(playerPos, selectedObj.transform.position);
            string name = ObjectNameCleaner.GetBetterObjectName(selectedObj);
            string direction = DirectionCalculator.GetCardinalDirection(playerPos, selectedObj.transform.position);

            return new NavigationInfo
            {
                HasSelection = true,
                ObjectName = name,
                Distance = distance,
                Direction = direction,
                CurrentIndex = selectedObjectIndex + 1,
                TotalCount = GetObjectCountForCategory(currentCategory),
                CategoryName = ObjectCategorizer.GetCategoryDisplayName(currentCategory),
                SortingMode = currentSortingMode
            };
        }
    }

    public class NavigationInfo
    {
        public bool HasSelection { get; set; }
        public string ObjectName { get; set; } = "";
        public float Distance { get; set; }
        public string Direction { get; set; } = "";
        public int CurrentIndex { get; set; }
        public int TotalCount { get; set; }
        public string CategoryName { get; set; } = "";
        public SortingMode SortingMode { get; set; } = SortingMode.Directional;

        public string FormatAnnouncement()
        {
            if (!HasSelection)
            {
                return $"No {CategoryName.ToLower()}s nearby";
            }

            string sortModeHint = SortingMode == SortingMode.Directional ? " (clockwise)" : " (by distance)";
            return $"{CategoryName} {CurrentIndex} of {TotalCount}{sortModeHint}: {ObjectName}, " +
                   $"{Distance:F0} meters {Direction}. Press period to cycle, comma to navigate.";
        }
    }
}