using System;
using HarmonyLib;
using MelonLoader;
using Il2CppSunshine.Journal;
using Il2CppPages.Gameplay.Journal;
using UnityEngine.EventSystems;
using AccessibilityMod.UI;

namespace AccessibilityMod.Patches
{
    /// <summary>
    /// Harmony patches for journal UI accessibility
    /// </summary>
    
    // Patch for journal task selection
    [HarmonyPatch(typeof(JournalTaskUI), "OnSelect")]
    public static class JournalTaskUI_OnSelect_Patch
    {
        private static JournalTaskUI lastSelectedTask = null;
        private static float lastSelectionTime = 0f;
        
        public static void Postfix(JournalTaskUI __instance, BaseEventData eventData)
        {
            try
            {
                // Prevent duplicate announcements within 0.5 seconds
                float currentTime = UnityEngine.Time.time;
                if (lastSelectedTask == __instance && currentTime - lastSelectionTime < 0.5f)
                {
                    MelonLogger.Msg($"[Journal] Skipping duplicate selection within 0.5s");
                    return;
                }
                
                lastSelectedTask = __instance;
                lastSelectionTime = currentTime;
                
                MelonLogger.Msg($"[Journal] Task selected: {__instance?.task?.Name} at time {currentTime}");
                
                // Format and announce the task
                string taskInfo = JournalFormatter.FormatJournalTask(__instance);
                if (!string.IsNullOrEmpty(taskInfo))
                {
                    MelonLogger.Msg($"[Journal] Announcing: {taskInfo}");
                    TolkScreenReader.Instance.Speak(taskInfo, false);
                    
                    // Check for copotype information
                    if (__instance.gameObject != null)
                    {
                        string copotypeInfo = JournalFormatter.FormatCopotypeInfo(__instance.gameObject);
                        if (!string.IsNullOrEmpty(copotypeInfo))
                        {
                            // Announce copotype info after a short delay
                            MelonCoroutines.Start(AnnounceCopotypeDelayed(copotypeInfo));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in JournalTaskUI_OnSelect_Patch: {ex}");
            }
        }
        
        private static System.Collections.IEnumerator AnnounceCopotypeDelayed(string info)
        {
            yield return new UnityEngine.WaitForSeconds(0.5f);
            TolkScreenReader.Instance.Speak(info, false);
        }
    }
    
    // Note: JournalSubtaskUI doesn't have OnSelect method since it extends MonoBehaviour, not Selectable
    // Subtasks might need to be handled differently or through the main task selection
    
    // Patch for journal page navigation (opening the journal)
    [HarmonyPatch(typeof(JournalPage), "OnNavigatedTo")]
    public static class JournalPage_OnNavigatedTo_Patch
    {
        public static void Postfix(JournalPage __instance)
        {
            try
            {
                MelonLogger.Msg("[Journal] Journal page opened");
                TolkScreenReader.Instance.Speak("Journal opened", false);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in JournalPage_OnNavigatedTo_Patch: {ex}");
            }
        }
    }
    
    // Patch for journal page closing
    [HarmonyPatch(typeof(JournalPage), "OnNavigatedFrom")]
    public static class JournalPage_OnNavigatedFrom_Patch
    {
        public static void Postfix(JournalPage __instance)
        {
            try
            {
                MelonLogger.Msg("[Journal] Journal page closed");
                TolkScreenReader.Instance.Speak("Journal closed", false);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in JournalPage_OnNavigatedFrom_Patch: {ex}");
            }
        }
    }
    
    // Patch for tab switching between Active and Done tasks
    [HarmonyPatch(typeof(JournalPage), "ToggleActiveDone")]
    public static class JournalPage_ToggleActiveDone_Patch
    {
        private static bool lastShowingActive = true;
        
        public static void Postfix(JournalPage __instance)
        {
            try
            {
                // Try to determine which tab is now active
                bool showingActive = !lastShowingActive; // Toggle assumption
                lastShowingActive = showingActive;
                
                string tabName = showingActive ? "Active tasks" : "Completed tasks";
                MelonLogger.Msg($"[Journal] Tab switched to: {tabName}");
                TolkScreenReader.Instance.Speak(tabName, false);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in JournalPage_ToggleActiveDone_Patch: {ex}");
            }
        }
    }
    
    // Patch for task completion
    [HarmonyPatch(typeof(JournalTask), "MarkDone")]
    public static class JournalTask_MarkDone_Patch
    {
        public static void Postfix(JournalTask __instance)
        {
            try
            {
                if (__instance == null) return;
                
                string taskName = __instance.LocalizedName;
                if (string.IsNullOrEmpty(taskName))
                {
                    taskName = __instance.Name;
                }
                
                if (!string.IsNullOrEmpty(taskName))
                {
                    MelonLogger.Msg($"[Journal] Task completed: {taskName}");
                    TolkScreenReader.Instance.Speak($"Task completed: {taskName}", true);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in JournalTask_MarkDone_Patch: {ex}");
            }
        }
    }
    
    // Patch for task cancellation
    [HarmonyPatch(typeof(JournalTask), "CancelTask")]
    public static class JournalTask_CancelTask_Patch
    {
        public static void Postfix(JournalTask __instance, bool __result)
        {
            try
            {
                if (__instance == null || !__result) return;
                
                string taskName = __instance.LocalizedName;
                if (string.IsNullOrEmpty(taskName))
                {
                    taskName = __instance.Name;
                }
                
                if (!string.IsNullOrEmpty(taskName))
                {
                    MelonLogger.Msg($"[Journal] Task canceled: {taskName}");
                    TolkScreenReader.Instance.Speak($"Task canceled: {taskName}", true);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in JournalTask_CancelTask_Patch: {ex}");
            }
        }
    }
    
    // Patch for new task reveal
    [HarmonyPatch(typeof(JournalTask), "Reveal")]
    public static class JournalTask_Reveal_Patch
    {
        public static void Postfix(JournalTask __instance)
        {
            try
            {
                if (__instance == null) return;
                
                string taskName = __instance.LocalizedName;
                if (string.IsNullOrEmpty(taskName))
                {
                    taskName = __instance.Name;
                }
                
                if (!string.IsNullOrEmpty(taskName))
                {
                    MelonLogger.Msg($"[Journal] New task revealed: {taskName}");
                    TolkScreenReader.Instance.Speak($"New task: {taskName}", true);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in JournalTask_Reveal_Patch: {ex}");
            }
        }
    }
}