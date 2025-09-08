using System;
using System.Text.RegularExpressions;
using Il2Cpp;
using Il2CppFortressOccident;

namespace AccessibilityMod.Utils
{
    public static class ObjectNameCleaner
    {
        public static string CleanObjectName(string rawName)
        {
            if (string.IsNullOrEmpty(rawName)) return rawName;
            
            // Remove common Unity prefixes/suffixes
            string cleaned = rawName;
            cleaned = cleaned.Replace("_", " ");
            cleaned = cleaned.Replace("(Clone)", "");
            cleaned = cleaned.Replace("GameObject", "");
            
            // Remove brackets and their contents
            cleaned = Regex.Replace(cleaned, @"\([^)]*\)", "");
            cleaned = Regex.Replace(cleaned, @"\[[^\]]*\]", "");
            
            // Clean up extra whitespace
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
            
            return cleaned;
        }

        public static string GetBetterObjectName(MouseOverHighlight obj)
        {
            try
            {
                // Try to get GameEntity name first (more descriptive)
                var entity = obj.GetFirstActive();
                if (entity != null && !string.IsNullOrEmpty(entity.name))
                {
                    return FormatObjectName(entity.name);
                }
                
                // Fall back to GameObject name
                if (obj.gameObject != null && !string.IsNullOrEmpty(obj.gameObject.name))
                {
                    return FormatObjectName(obj.gameObject.name);
                }
                
                return "Unknown Object";
            }
            catch
            {
                return "Unknown Object";
            }
        }

        private static string FormatObjectName(string rawName)
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
    }
}