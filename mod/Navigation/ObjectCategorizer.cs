using System;
using UnityEngine;
using Il2Cpp;
using Il2CppFortressOccident;
using AccessibilityMod.Utils;
using MelonLoader;

namespace AccessibilityMod.Navigation
{
    public enum ObjectCategory
    {
        NPCs = 1,           // Interactive NPCs (excluding Kim)
        Locations = 2,      // Doors, exits, vehicles, story objects
        Loot = 3,          // Containers, money, pickuppable items
        Everything = 4      // Fallback category
    }

    public static class ObjectCategorizer
    {
        public static ObjectCategory CategorizeObject(MouseOverHighlight obj, Vector3 playerPos)
        {
            try
            {
                string name = ObjectNameCleaner.GetBetterObjectName(obj).ToLower();
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
                MelonLogger.Error($"[CATEGORIZATION] Error categorizing object: {ex}");
                return ObjectCategory.Everything;
            }
        }

        public static float GetMaxDistanceForCategory(ObjectCategory category)
        {
            return category switch
            {
                ObjectCategory.NPCs => 50f,         // Important people can be further
                ObjectCategory.Locations => 50f,    // Doors/exits can be further  
                ObjectCategory.Loot => 30f,         // Containers usually nearby
                ObjectCategory.Everything => 50f,   // Same range as NPCs and locations
                _ => 30f
            };
        }

        public static string GetCategoryDisplayName(ObjectCategory category)
        {
            return category switch
            {
                ObjectCategory.NPCs => "NPC",
                ObjectCategory.Locations => "Location", 
                ObjectCategory.Loot => "Container",
                ObjectCategory.Everything => "Everything",
                _ => "Item"
            };
        }

        private static bool IsInteractiveNPC(string name, string gameObjectName)
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

        private static bool IsImportantLocation(string name, string gameObjectName)
        {
            string[] locationPatterns = {
                "door", "entrance", "exit", "gate", "passage", "stairway", "stairs",
                "kineema", "car", "vehicle", "monument", "terminal", "phone", "booth",
                "building", "cabin", "shack", "harbor", "pier", "bridge",
                "kiosque", "outline", "rooftop", "balcony", "doorway"
            };
            
            foreach (string pattern in locationPatterns)
            {
                if (name.Contains(pattern) || gameObjectName.Contains(pattern))
                    return true;
            }
            
            return false;
        }

        private static bool IsLootOrContainer(string name, string gameObjectName)
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

        private static bool IsClutter(string name, string gameObjectName)
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
    }
}