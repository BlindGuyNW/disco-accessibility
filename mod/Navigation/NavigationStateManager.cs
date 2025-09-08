using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Il2Cpp;
using Il2CppFortressOccident;
using AccessibilityMod.Utils;

namespace AccessibilityMod.Navigation
{
    public class NavigationStateManager
    {
        private Dictionary<ObjectCategory, List<MouseOverHighlight>> categorizedObjects = new Dictionary<ObjectCategory, List<MouseOverHighlight>>();
        private ObjectCategory currentCategory = ObjectCategory.NPCs;
        private int selectedObjectIndex = -1;

        public ObjectCategory CurrentCategory => currentCategory;
        public int SelectedObjectIndex => selectedObjectIndex;
        public bool HasSelection => selectedObjectIndex >= 0 && HasObjectsInCategory(currentCategory);

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
                
                // Categorize all objects within range
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
                
                // Sort each category by distance
                foreach (var categoryList in categorizedObjects.Values)
                {
                    categoryList.Sort((a, b) => 
                        Vector3.Distance(playerPos, a.transform.position)
                        .CompareTo(Vector3.Distance(playerPos, b.transform.position)));
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
                CategoryName = ObjectCategorizer.GetCategoryDisplayName(currentCategory)
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

        public string FormatAnnouncement()
        {
            if (!HasSelection)
            {
                return $"No {CategoryName.ToLower()}s nearby";
            }

            return $"{CategoryName} {CurrentIndex} of {TotalCount}: {ObjectName}, " +
                   $"{Distance:F0} meters {Direction}. Press period to cycle, comma to navigate.";
        }
    }
}