using System;
using System.Text;
using UnityEngine;
using Il2CppSunshine.Journal;
using Il2Cpp;
using MelonLoader;
using AccessibilityMod.Utils;

namespace AccessibilityMod.UI
{
    /// <summary>
    /// Formats journal UI elements for accessibility
    /// </summary>
    public static class JournalFormatter
    {
        /// <summary>
        /// Format a journal task UI element for screen reader
        /// </summary>
        public static string FormatJournalTask(JournalTaskUI taskUI)
        {
            try
            {
                if (taskUI == null) return null;
                
                var sb = new StringBuilder();
                
                // Get the task object
                var task = taskUI.task;
                if (task == null)
                {
                    // Try to get text from the UI element itself
                    if (taskUI.taskNameTextField != null)
                    {
                        string uiText = taskUI.taskNameTextField.text;
                        if (!string.IsNullOrEmpty(uiText))
                        {
                            return ObjectNameCleaner.CleanObjectName(uiText);
                        }
                    }
                    return "Unknown task";
                }
                
                // Get task name
                string taskName = task.LocalizedName;
                if (string.IsNullOrEmpty(taskName))
                {
                    taskName = task.Name;
                }
                
                if (string.IsNullOrEmpty(taskName))
                {
                    taskName = "Unknown task";
                }
                else
                {
                    taskName = ObjectNameCleaner.CleanObjectName(taskName);
                }
                
                // Add status prefix
                if (task.IsCanceled)
                {
                    sb.Append("Canceled: ");
                }
                else if (task.IsDone)
                {
                    sb.Append("Completed: ");
                }
                else if (task is JournalTask journalTaskForNew && journalTaskForNew.IsNew)
                {
                    sb.Append("New: ");
                }
                
                sb.Append(taskName);
                
                // Add time information if available
                if (taskUI.aquisitionTime != null)
                {
                    try
                    {
                        string timeStr = FormatClockTime(taskUI.aquisitionTime);
                        if (!string.IsNullOrEmpty(timeStr))
                        {
                            sb.Append($" - acquired {timeStr}");
                        }
                    }
                    catch
                    {
                        // Ignore time formatting errors
                    }
                }
                
                // Add subtask information if this is a JournalTask with subtasks
                if (task is JournalTask journalTask)
                {
                    MelonLogger.Msg($"[Journal] Task is JournalTask, checking subtasks");
                    var subtasks = journalTask.GainedSubtasks;
                    if (subtasks != null)
                    {
                        MelonLogger.Msg($"[Journal] Found {subtasks.Count} subtasks");
                        if (subtasks.Count > 0)
                        {
                            sb.Append($" ({subtasks.Count} subtask");
                            if (subtasks.Count != 1) sb.Append("s");
                            
                            // Count completed subtasks
                            int completed = 0;
                            foreach (var subtask in subtasks)
                            {
                                if (subtask.IsDone) completed++;
                            }
                            
                            if (completed > 0)
                            {
                                sb.Append($", {completed} completed");
                            }
                            sb.Append(")");
                        }
                    }
                    else
                    {
                        MelonLogger.Msg($"[Journal] GainedSubtasks is null");
                    }
                }
                else
                {
                    MelonLogger.Msg($"[Journal] Task is not a JournalTask, it's type: {task?.GetType()?.Name}");
                }
                
                // Add description
                string description = task.LocalizedDescription;
                if (string.IsNullOrEmpty(description))
                {
                    description = task.Description;
                }
                
                if (!string.IsNullOrEmpty(description))
                {
                    sb.Append(". Description: ");
                    sb.Append(ObjectNameCleaner.CleanObjectName(description));
                }
                else
                {
                    MelonLogger.Msg($"[Journal] No description found for task {taskName}");
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error formatting journal task: {ex}");
                return "Error reading task";
            }
        }
        
        /// <summary>
        /// Format subtask information
        /// </summary>
        public static string FormatSubtask(JournalSubtask subtask, int index)
        {
            try
            {
                if (subtask == null) return null;
                
                var sb = new StringBuilder();
                
                // Add indentation/number
                sb.Append($"  {index}. ");
                
                // Add status
                if (subtask.IsCanceled)
                {
                    sb.Append("[Canceled] ");
                }
                else if (subtask.IsDone)
                {
                    sb.Append("[Done] ");
                }
                
                // Add name
                string name = subtask.LocalizedName;
                if (string.IsNullOrEmpty(name))
                {
                    name = subtask.Name;
                }
                
                if (!string.IsNullOrEmpty(name))
                {
                    sb.Append(ObjectNameCleaner.CleanObjectName(name));
                }
                else
                {
                    sb.Append("Unknown subtask");
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error formatting subtask: {ex}");
                return null;
            }
        }
        
        /// <summary>
        /// Format copotype information if present
        /// </summary>
        public static string FormatCopotypeInfo(GameObject taskUIObject)
        {
            try
            {
                if (taskUIObject == null) return null;
                
                // Look for copotype info blocks in the hierarchy
                var copotypeBlock = taskUIObject.GetComponentInChildren<CopotypeInfoBlock>();
                if (copotypeBlock == null)
                {
                    // Try parent hierarchy
                    var parent = taskUIObject.transform.parent;
                    if (parent != null)
                    {
                        copotypeBlock = parent.GetComponentInChildren<CopotypeInfoBlock>();
                    }
                }
                
                if (copotypeBlock == null) return null;
                
                var sb = new StringBuilder();
                sb.Append("Copotype information: ");
                
                // Try to find value blocks
                var valueBlocks = copotypeBlock.GetComponentsInChildren<CopotypeValueBlock>();
                if (valueBlocks != null && valueBlocks.Length > 0)
                {
                    foreach (var valueBlock in valueBlocks)
                    {
                        if (valueBlock._descriptionText != null && valueBlock._valueText != null)
                        {
                            string desc = valueBlock._descriptionText.text;
                            string val = valueBlock._valueText.text;
                            
                            if (!string.IsNullOrEmpty(desc) && !string.IsNullOrEmpty(val))
                            {
                                sb.Append($"{desc}: {val}, ");
                            }
                        }
                    }
                }
                
                string result = sb.ToString();
                if (result.EndsWith(", "))
                {
                    result = result.Substring(0, result.Length - 2);
                }
                
                return result == "Copotype information: " ? null : result;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error formatting copotype info: {ex}");
                return null;
            }
        }
        
        /// <summary>
        /// Check if a GameObject is a journal task UI element
        /// </summary>
        public static bool IsJournalTaskUI(GameObject obj)
        {
            if (obj == null) return false;
            return obj.GetComponent<JournalTaskUI>() != null;
        }
        
        /// <summary>
        /// Get journal task information from a GameObject
        /// </summary>
        public static string GetJournalTaskInfo(GameObject obj)
        {
            try
            {
                if (obj == null) return null;
                
                var taskUI = obj.GetComponent<JournalTaskUI>();
                if (taskUI == null) return null;
                
                return FormatJournalTask(taskUI);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting journal task info: {ex}");
                return null;
            }
        }
        
        /// <summary>
        /// Format clock time for display
        /// </summary>
        private static string FormatClockTime(SunshineClockTime clockTime)
        {
            try
            {
                if (clockTime == null) return null;

                var sb = new StringBuilder();

                // Get day of week
                try
                {
                    var dayOfWeek = clockTime.GetDayOfWeek();
                    sb.Append(dayOfWeek.ToString());
                    sb.Append(" at ");
                }
                catch
                {
                    // If we can't get day of week, try day counter
                    try
                    {
                        int dayCounter = clockTime.DayCounter;
                        sb.Append($"Day {dayCounter}, ");
                    }
                    catch
                    {
                        // Ignore if we can't get day info
                    }
                }

                // Get time string
                string timeStr = clockTime.ToString();
                if (!string.IsNullOrEmpty(timeStr))
                {
                    sb.Append(timeStr);
                }
                else
                {
                    // Try to build time manually
                    try
                    {
                        int hours = clockTime.Hours;
                        int minutes = clockTime.Minutes;
                        sb.Append($"{hours:D2}:{minutes:D2}");
                    }
                    catch
                    {
                        // If we can't get time components, just return what we have
                    }
                }

                string result = sb.ToString();
                return string.IsNullOrEmpty(result) ? null : result;
            }
            catch
            {
                return null;
            }
        }
    }
}