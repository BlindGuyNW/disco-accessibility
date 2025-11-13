using System;
using System.Text;
using AccessibilityMod.UI;
using Il2Cpp;
using Il2CppPages.Gameplay.Journal;
using MelonLoader;
using UnityEngine;

namespace AccessibilityMod.Patches
{
    /// <summary>
    /// Handles officer profile announcements
    /// </summary>
    public static class OfficerProfileAnnouncement
    {
        /// <summary>
        /// Announce the officer profile information from the badge page
        /// </summary>
        public static void AnnounceOfficerProfile()
        {
            try
            {
                // Optimized search: look specifically for "Copotype Profile Line" or "OfficerProfile Console"
                // This is much faster than searching all GameObjects
                var allTransforms = UnityEngine.Object.FindObjectsOfType<Transform>();
                GameObject profileObject = null;

                if (allTransforms != null)
                {
                    foreach (var transform in allTransforms)
                    {
                        if (transform == null || transform.gameObject == null)
                            continue;
                        if (!transform.gameObject.activeInHierarchy)
                            continue;

                        string name = transform.gameObject.name;

                        // Look for the parent container directly
                        if (
                            name.Equals(
                                "OfficerProfile Console",
                                StringComparison.OrdinalIgnoreCase
                            )
                            || name.Equals(
                                "Officer Profile Missing",
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        {
                            profileObject = transform.gameObject;
                            break;
                        }

                        // Or look for a profile line and walk up to find the container
                        if (name.Contains("Copotype Profile Line"))
                        {
                            // Walk up the parent hierarchy to find the container
                            Transform parent = transform.parent;
                            while (parent != null)
                            {
                                var parentTexts =
                                    parent.GetComponentsInChildren<Il2CppTMPro.TextMeshProUGUI>();

                                // Look for a parent with text components (profile lines or incomplete message)
                                // Complete profiles have 10+ components, incomplete profiles have 2-3
                                if (parentTexts != null && parentTexts.Length > 1)
                                {
                                    profileObject = parent.gameObject;
                                    break;
                                }
                                parent = parent.parent;
                            }

                            if (profileObject != null)
                                break;
                        }
                    }
                }

                if (profileObject == null)
                {
                    TolkScreenReader.Instance.Speak(
                        "Officer profile not visible. Make sure the journal is open.",
                        true
                    );
                    return;
                }

                // Format and announce the profile information
                string profileInfo = FormatOfficerProfile(profileObject);

                if (!string.IsNullOrEmpty(profileInfo))
                {
                    TolkScreenReader.Instance.Speak(profileInfo, true);
                }
                else
                {
                    TolkScreenReader.Instance.Speak(
                        "Officer profile has no information available.",
                        true
                    );
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error announcing officer profile: {ex}");
                TolkScreenReader.Instance.Speak("Error reading officer profile.", true);
            }
        }

        /// <summary>
        /// Format officer profile information from the profile GameObject
        /// </summary>
        private static string FormatOfficerProfile(GameObject profileObject)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append("Officer Profile.\n\n");

                // First check if profile is incomplete
                var allTextComponents =
                    profileObject.GetComponentsInChildren<Il2CppTMPro.TextMeshProUGUI>();
                bool isIncomplete = false;

                if (allTextComponents != null)
                {
                    foreach (var textComp in allTextComponents)
                    {
                        if (
                            textComp != null
                            && !string.IsNullOrEmpty(textComp.text)
                            && textComp
                                .text.Trim()
                                .Equals("INCOMPLETE", StringComparison.OrdinalIgnoreCase)
                        )
                        {
                            isIncomplete = true;
                            break;
                        }
                    }
                }

                // If incomplete, just read all text in order
                if (isIncomplete)
                {
                    if (allTextComponents != null)
                    {
                        foreach (var textComp in allTextComponents)
                        {
                            if (textComp != null && !string.IsNullOrEmpty(textComp.text))
                            {
                                string text = textComp.text.Trim();
                                if (text.Length > 0)
                                {
                                    sb.AppendLine($"{text}.");
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Look for CopotypeProfileLine components - each line is label + value
                    var profileLines = profileObject.GetComponentsInChildren<CopotypeValueBlock>();

                    if (profileLines != null && profileLines.Length > 0)
                    {
                        foreach (var line in profileLines)
                        {
                            if (
                                line != null
                                && line._descriptionText != null
                                && line._valueText != null
                            )
                            {
                                string desc = line._descriptionText.text?.Trim();
                                string val = line._valueText.text?.Trim();

                                if (!string.IsNullOrEmpty(desc) && !string.IsNullOrEmpty(val))
                                {
                                    sb.AppendLine($"{desc}: {val}.");
                                }
                            }
                        }
                    }
                    else
                    {
                        // Fallback: collect all text and try to pair them
                        var textList = new System.Collections.Generic.List<string>();

                        if (allTextComponents != null)
                        {
                            foreach (var textComp in allTextComponents)
                            {
                                if (textComp != null && !string.IsNullOrEmpty(textComp.text))
                                {
                                    string text = textComp.text.Trim();
                                    if (text.Length > 0)
                                    {
                                        textList.Add(text);
                                    }
                                }
                            }
                        }

                        // Try to pair them up: assume they alternate label, value, label, value
                        for (int i = 0; i < textList.Count - 1; i += 2)
                        {
                            string label = textList[i];
                            string value = textList[i + 1];

                            // Check if this looks like a label/value pair
                            // Labels are typically longer text, values are short (numbers or short text)
                            if (
                                value.Length <= 5
                                || System.Text.RegularExpressions.Regex.IsMatch(value, @"^\d+y?$")
                            )
                            {
                                sb.AppendLine($"{label}: {value}.");
                            }
                            else
                            {
                                // Doesn't look like a pair, just output the label
                                sb.AppendLine(label);
                                i--; // Adjust index since we didn't consume the "value"
                            }
                        }
                    }
                }

                string result = sb.ToString().Trim();
                return string.IsNullOrEmpty(result) || result == "Officer Profile." ? null : result;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error formatting officer profile: {ex}");
                return null;
            }
        }
    }
}
