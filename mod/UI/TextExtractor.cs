using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Il2CppTMPro;
using MelonLoader;

namespace AccessibilityMod.UI
{
    /// <summary>
    /// Utility class for extracting text content from UI GameObjects
    /// </summary>
    public static class TextExtractor
    {
        /// <summary>
        /// Extract the best text content from a GameObject, checking multiple text component types
        /// </summary>
        public static string ExtractBestTextContent(GameObject uiObject)
        {
            try
            {
                if (uiObject == null) return null;
                
                // Try direct text components first
                var textComponent = uiObject.GetComponent<Text>();
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
                var childText = uiObject.GetComponentInChildren<Text>();
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

        /// <summary>
        /// Search for descriptive text within a GameObject hierarchy, with filtering for likely descriptions
        /// </summary>
        public static string SearchForDescriptiveText(GameObject parent, Func<string, bool> textValidator, string contextName = "")
        {
            try
            {
                if (parent == null) return null;

                // Get all text components in this object and its children
                var allTextComponents = new List<Component>();
                
                // Add TextMeshProUGUI components
                var tmpComponents = parent.GetComponentsInChildren<TextMeshProUGUI>();
                if (tmpComponents != null)
                {
                    allTextComponents.AddRange(tmpComponents);
                }
                
                // Add regular Text components  
                var textComponents = parent.GetComponentsInChildren<Text>();
                if (textComponents != null)
                {
                    allTextComponents.AddRange(textComponents);
                }
                
                // Search text components for descriptive content
                foreach (var component in allTextComponents)
                {
                    string text = null;
                    
                    if (component is TextMeshProUGUI tmpText && tmpText != null)
                    {
                        text = tmpText.text;
                    }
                    else if (component is Text regularText && regularText != null)
                    {
                        text = regularText.text;
                    }
                    
                    if (!string.IsNullOrEmpty(text) && textValidator(text))
                    {
                        return text.Trim();
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error searching for descriptive text in {contextName}: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Find displayed text by searching up the hierarchy from a starting object
        /// </summary>
        public static string FindDisplayedDescription(GameObject startObject, Func<string, bool> textValidator, int maxLevels = 4, string contextName = "")
        {
            try
            {
                // Search in the starting object and its broader hierarchy for description text
                var candidates = new List<GameObject> { startObject };
                
                // Add parent objects that might contain the description area
                var current = startObject.transform;
                for (int i = 0; i < maxLevels && current != null; i++)
                {
                    current = current.parent;
                    if (current != null)
                    {
                        candidates.Add(current.gameObject);
                    }
                }
                
                // Search each candidate for descriptive text
                foreach (var candidate in candidates)
                {
                    string description = SearchForDescriptiveText(candidate, textValidator, contextName);
                    if (!string.IsNullOrEmpty(description))
                    {
                        return description;
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error finding displayed description for {contextName}: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Common text validation for skill descriptions
        /// </summary>
        public static bool IsLikelySkillDescriptionText(string text, string skillName = "")
        {
            if (string.IsNullOrEmpty(text) || text.Length < 20)
                return false;
                
            // Skip if it's just the skill name itself
            if (!string.IsNullOrEmpty(skillName) && text.Trim().Equals(skillName.Replace('_', ' '), StringComparison.OrdinalIgnoreCase))
                return false;
                
            // Skip pure numbers or very short text
            if (text.Trim().Length < 15 || text.All(c => char.IsDigit(c) || char.IsWhiteSpace(c)))
                return false;
                
            // Skip common UI text that's not descriptions  
            string lowerText = text.ToLower();
            if (lowerText.Contains("select button") || lowerText.Contains("level up") || 
                (lowerText.Contains("point") && lowerText.Length < 25))
                return false;
                
            // Look for description-like content - complete sentences or skill-related keywords
            return lowerText.Contains("skill") || lowerText.Contains("ability") ||
                   lowerText.Contains("helps") || lowerText.Contains("allows") ||
                   lowerText.Contains("used") || lowerText.Contains("affects") ||
                   lowerText.Contains("reasoning") || lowerText.Contains("knowledge") ||
                   lowerText.Contains("social") || lowerText.Contains("physical") ||
                   (text.Contains(".") && text.Split('.').Length > 1); // Multiple sentences
        }

        /// <summary>
        /// Common text validation for archetype descriptions
        /// </summary>
        public static bool IsLikelyArchetypeDescriptionText(string text, string archetypeName = "")
        {
            if (string.IsNullOrEmpty(text) || text.Length < 20)
                return false;
                
            // Skip if it's just the archetype name itself
            if (!string.IsNullOrEmpty(archetypeName) && text.Trim().Equals(archetypeName, StringComparison.OrdinalIgnoreCase))
                return false;
                
            // Skip pure numbers or very short text
            if (text.Trim().Length < 15 || text.All(c => char.IsDigit(c) || char.IsWhiteSpace(c)))
                return false;
                
            // Skip common UI text that's not descriptions  
            string lowerText = text.ToLower();
            if (lowerText.Contains("button") || lowerText.Contains("select") || 
                lowerText.Contains("click") || (lowerText.Contains("archetype") && lowerText.Length < 30))
                return false;
                
            // Look for description-like content - complete sentences or archetype-related keywords
            return lowerText.Contains("approach") || lowerText.Contains("focuses") ||
                   lowerText.Contains("specializes") || lowerText.Contains("excels") ||
                   lowerText.Contains("strength") || lowerText.Contains("abilities") ||
                   lowerText.Contains("intellect") || lowerText.Contains("empathy") ||
                   lowerText.Contains("physical") || lowerText.Contains("reasoning") ||
                   (text.Contains(".") && text.Split('.').Length > 1); // Multiple sentences
        }

        /// <summary>
        /// Common text validation for stat descriptions
        /// </summary>
        public static bool IsLikelyStatDescriptionText(string text, string statName = "")
        {
            if (string.IsNullOrEmpty(text) || text.Length < 15)
                return false;
                
            // Skip if it's just the stat name itself
            if (!string.IsNullOrEmpty(statName) && text.Trim().Equals(statName, StringComparison.OrdinalIgnoreCase))
                return false;
                
            // Skip pure numbers or very short text
            if (text.All(c => char.IsDigit(c) || char.IsWhiteSpace(c)))
                return false;
                
            // Skip common UI text that's not descriptions  
            string lowerText = text.ToLower();
            if (lowerText.Contains("button") || lowerText.Contains("select") || 
                (lowerText.Contains("point") && lowerText.Length < 20))
                return false;
                
            // Look for description-like content - stat-related keywords
            return lowerText.Contains("affects") || lowerText.Contains("determines") ||
                   lowerText.Contains("governs") || lowerText.Contains("influences") ||
                   lowerText.Contains("skills") || lowerText.Contains("abilities") ||
                   lowerText.Contains("logic") || lowerText.Contains("reasoning") ||
                   lowerText.Contains("empathy") || lowerText.Contains("physical") ||
                   lowerText.Contains("coordination") || lowerText.Contains("dexterity") ||
                   (text.Contains(".") && text.Split('.').Length > 1); // Multiple sentences
        }

        /// <summary>
        /// Extract text from a UI component using reflection (for complex components)
        /// </summary>
        public static string ExtractTooltipDescription(object tooltipData)
        {
            try
            {
                if (tooltipData == null) return null;
                
                // Use reflection to look for common description fields
                var type = tooltipData.GetType();
                var fields = type.GetFields();
                var properties = type.GetProperties();
                
                // Look for fields that might contain description
                foreach (var field in fields)
                {
                    if (field.Name.ToLower().Contains("description") || 
                        field.Name.ToLower().Contains("text") ||
                        field.Name.ToLower().Contains("content"))
                    {
                        var value = field.GetValue(tooltipData);
                        if (value != null && value is string str && !string.IsNullOrEmpty(str))
                        {
                            return str.Trim();
                        }
                    }
                }
                
                // Look for properties that might contain description
                foreach (var prop in properties)
                {
                    if (prop.CanRead && (prop.Name.ToLower().Contains("description") || 
                        prop.Name.ToLower().Contains("text") ||
                        prop.Name.ToLower().Contains("content")))
                    {
                        try
                        {
                            var value = prop.GetValue(tooltipData);
                            if (value != null && value is string str && !string.IsNullOrEmpty(str))
                            {
                                return str.Trim();
                            }
                        }
                        catch { /* Property might not be accessible */ }
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Error extracting tooltip description: {ex.Message}");
                return null;
            }
        }
    }
}