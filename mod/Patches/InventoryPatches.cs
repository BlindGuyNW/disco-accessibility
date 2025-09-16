using System;
using HarmonyLib;
using MelonLoader;
using Il2CppSunshine.Views;
using Il2CppSunshine;
using Il2CppPages.Gameplay.Inventory;
using Il2CppDiscoPages.Elements.Inventory;
using Il2Cpp;
using Il2CppPagesSystem;
using AccessibilityMod.Inventory;
using UnityEngine.EventSystems;

namespace AccessibilityMod.Patches
{


    // Patch for inventory item slot pointer clicks (works for both mouse and controller input converted to clicks)
    [HarmonyPatch(typeof(InventoryItemSlot), "OnPointerClick")]
    public static class InventoryItemSlot_OnPointerClick_Patch
    {
        public static void Postfix(InventoryItemSlot __instance, PointerEventData eventData)
        {
            try
            {
                MelonLogger.Msg($"Inventory item slot clicked: {__instance.itemName}");
                // Item slot was clicked
                InventoryNavigationHandler.Instance.OnInventoryItemSelected(__instance);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in InventoryItemSlot_OnPointerClick_Patch: {ex}");
            }
        }
    }

    // Patch for tab panel changes
    [HarmonyPatch(typeof(PageSystemInventoryTabPanel), "ChangeGroup", new Type[] { typeof(ItemTabGroup), typeof(bool) })]
    public static class PageSystemInventoryTabPanel_ChangeGroup_Patch
    {
        public static void Postfix(PageSystemInventoryTabPanel __instance, ItemTabGroup group, bool immediate)
        {
            try
            {
                // Tab was changed
                InventoryNavigationHandler.Instance.OnTabChanged(group);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in PageSystemInventoryTabPanel_ChangeGroup_Patch: {ex}");
            }
        }
    }

    // Patch for equipment slot item docking (equipping)
    [HarmonyPatch(typeof(InventoryEquipmentSlot), "DockItem")]
    public static class InventoryEquipmentSlot_DockItem_Patch
    {
        public static void Postfix(InventoryEquipmentSlot __instance, string itemName)
        {
            try
            {
                MelonLogger.Msg($"Equipment slot docked item: {itemName}");
                string slotType = __instance.slotType.ToString().Replace("_", " ");
                TolkScreenReader.Instance.Speak($"Equipped {itemName} to {slotType}", false);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in InventoryEquipmentSlot_DockItem_Patch: {ex}");
            }
        }
    }

    // Patch for equipment slot item removal (unequipping)
    [HarmonyPatch(typeof(InventoryEquipmentSlot), "RemoveItem")]
    public static class InventoryEquipmentSlot_RemoveItem_Patch
    {
        public static void Postfix(InventoryEquipmentSlot __instance)
        {
            try
            {
                MelonLogger.Msg($"Equipment slot removed item");
                string slotType = __instance.slotType.ToString().Replace("_", " ");
                string itemName = __instance.prevItemName;
                if (!string.IsNullOrEmpty(itemName))
                {
                    TolkScreenReader.Instance.Speak($"Unequipped {itemName} from {slotType}", false);
                }
                else
                {
                    TolkScreenReader.Instance.Speak($"{slotType} slot is now empty", false);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in InventoryEquipmentSlot_RemoveItem_Patch: {ex}");
            }
        }
    }

    // REMOVED: Obsolete disabled patch - InventoryHighlighter handles all navigation now




    [HarmonyPatch(typeof(InventoryManager), "UpdateCurrentlySelectedTab")]
    public static class InventoryManager_UpdateCurrentlySelectedTab_Patch
    {
        private static int lastAnnouncedTab = -1;
        
        public static void Postfix(InventoryManager __instance)
        {
            try
            {
                MelonLogger.Msg($"[Inventory] InventoryManager.UpdateCurrentlySelectedTab called - CurrentTab: {__instance?.CurrentTab}");
                
                // Update InventoryNavigationHandler with the current tab (it will handle announcement)
                if (__instance != null && __instance.CurrentTab != lastAnnouncedTab)
                {
                    MelonLogger.Msg($"[Inventory] Tab change detected: {lastAnnouncedTab} -> {__instance.CurrentTab}");
                    lastAnnouncedTab = __instance.CurrentTab;
                    
                    ItemTabGroup tabGroup = (ItemTabGroup)__instance.CurrentTab;
                    InventoryNavigationHandler.Instance.OnTabChanged(tabGroup);
                }
                else
                {
                    MelonLogger.Msg($"[Inventory] No tab change: current={__instance?.CurrentTab}, lastAnnounced={lastAnnouncedTab}");
                }
                
                InventoryNavigationHandler.Instance?.Update();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in InventoryManager_UpdateCurrentlySelectedTab_Patch: {ex.Message}");
            }
        }
        
        private static string GetTabName(int tabIndex)
        {
            switch (tabIndex)
            {
                case 0: return "Tools";
                case 1: return "Clothes";
                case 2: return "Pawnables";
                case 3: return "Reading";
                default: return null;
            }
        }
    }

    // NEW: InventoryHighlighter patches - the REAL inventory navigation system
    [HarmonyPatch(typeof(Il2Cpp.InventoryHighlighter), "UnityEngine_EventSystems_ISelectHandler_OnSelect")]
    public static class InventoryHighlighter_OnSelect_Patch
    {
        public static void Postfix(Il2Cpp.InventoryHighlighter __instance, BaseEventData eventData)
        {
            try
            {
                MelonLogger.Msg($"[InventoryHighlighter] OnSelect called on: {__instance?.name}");
                
                // First, try to get InventoryItemSlot component directly on this GameObject
                var inventoryItemSlot = __instance.GetComponent<Il2CppDiscoPages.Elements.Inventory.InventoryItemSlot>();
                if (inventoryItemSlot != null)
                {
                    MelonLogger.Msg($"[InventoryHighlighter] Found InventoryItemSlot with itemName: '{inventoryItemSlot.itemName}'");
                    
                    // Check if this slot has an item
                    if (inventoryItemSlot.item != null && !string.IsNullOrEmpty(inventoryItemSlot.item.displayName))
                    {
                        MelonLogger.Msg($"[InventoryHighlighter] Slot has item: '{inventoryItemSlot.item.displayName}'");
                        TolkScreenReader.Instance.Speak(inventoryItemSlot.item.displayName, true);
                        return;
                    }
                    else if (!string.IsNullOrEmpty(inventoryItemSlot.itemName))
                    {
                        MelonLogger.Msg($"[InventoryHighlighter] Using slot itemName: '{inventoryItemSlot.itemName}'");
                        TolkScreenReader.Instance.Speak(inventoryItemSlot.itemName, true);
                        return;
                    }
                    else
                    {
                        MelonLogger.Msg($"[InventoryHighlighter] InventoryItemSlot is empty");
                        TolkScreenReader.Instance.Speak("Empty inventory slot", true);
                        return;
                    }
                }

                // Try to find InventoryItemSlot in children 
                var childInventoryItemSlot = __instance.GetComponentInChildren<Il2CppDiscoPages.Elements.Inventory.InventoryItemSlot>();
                if (childInventoryItemSlot != null)
                {
                    MelonLogger.Msg($"[InventoryHighlighter] Found child InventoryItemSlot with itemName: '{childInventoryItemSlot.itemName}'");
                    
                    if (childInventoryItemSlot.item != null && !string.IsNullOrEmpty(childInventoryItemSlot.item.displayName))
                    {
                        MelonLogger.Msg($"[InventoryHighlighter] Child slot has item: '{childInventoryItemSlot.item.displayName}'");
                        TolkScreenReader.Instance.Speak(childInventoryItemSlot.item.displayName, true);
                        return;
                    }
                    else if (!string.IsNullOrEmpty(childInventoryItemSlot.itemName))
                    {
                        MelonLogger.Msg($"[InventoryHighlighter] Using child slot itemName: '{childInventoryItemSlot.itemName}'");
                        TolkScreenReader.Instance.Speak(childInventoryItemSlot.itemName, true);
                        return;
                    }
                    else
                    {
                        MelonLogger.Msg($"[InventoryHighlighter] Child InventoryItemSlot is empty");
                        TolkScreenReader.Instance.Speak("Empty inventory slot", true);
                        return;
                    }
                }

                // If no InventoryItemSlot found, check numbered slots via InventoryViewData
                MelonLogger.Msg($"[InventoryHighlighter] No InventoryItemSlot found on: {__instance?.name}");
                
                // Check if this is a numbered slot (regular inventory)
                if (int.TryParse(__instance?.name, out int slotIndex))
                {
                    MelonLogger.Msg($"[InventoryHighlighter] Found numbered slot: {slotIndex}");
                    string itemName = InventoryHighlighterHelper.GetInventoryItemAtSlot(slotIndex);
                    if (!string.IsNullOrEmpty(itemName))
                    {
                        MelonLogger.Msg($"[InventoryHighlighter] Found item at slot {slotIndex}: {itemName}");
                        TolkScreenReader.Instance.Speak(itemName, true);
                    }
                    else
                    {
                        MelonLogger.Msg($"[InventoryHighlighter] Slot {slotIndex} is empty");
                        TolkScreenReader.Instance.Speak("Empty slot", true);
                    }
                }
                // Check if this is an equipment slot
                else
                {
                    string equippedItemName = InventoryHighlighterHelper.GetEquippedItemName(__instance?.name);
                    if (!string.IsNullOrEmpty(equippedItemName))
                    {
                        MelonLogger.Msg($"[InventoryHighlighter] Found equipped item via InventoryViewData: {equippedItemName}");
                        TolkScreenReader.Instance.Speak(equippedItemName, true);
                    }
                    else
                    {
                        // Announce slot name in a user-friendly way
                        string slotAnnouncement = InventoryHighlighterHelper.GetSlotAnnouncement(__instance?.name);
                        TolkScreenReader.Instance.Speak(slotAnnouncement, true);
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in InventoryHighlighter_OnSelect_Patch: {ex.Message}");
            }
        }
    }


    // Helper class for InventoryHighlighter operations
    public static class InventoryHighlighterHelper
    {
        public static string GetEquippedItemName(string slotName)
        {
            try
            {
                // Map slot GameObject name to EquipmentSlotType
                var slotType = GetEquipmentSlotType(slotName);
                if (slotType != null)
                {
                    // Get the InventoryViewData singleton and check for equipped item
                    var inventoryData = Il2CppSunshine.Metric.InventoryViewData.Singleton;
                    if (inventoryData != null)
                    {
                        bool isEquipped = inventoryData.IsEquipped(slotType.Value);
                        if (isEquipped)
                        {
                            string itemName = inventoryData.GetEquipped(slotType.Value);
                            // Get the full item details like inventory slots do
                            var library = inventoryData.GetLibrary();
                            string itemDetails = itemName; // fallback
                            if (library != null)
                            {
                                var inventoryItem = library.GetByName(itemName);
                                if (inventoryItem != null)
                                {
                                    itemDetails = FormatInventoryItemForSpeech(inventoryItem);
                                }
                            }
                            
                            // Add slot type prefix
                            string slotTypeName = GetSlotTypeName(slotName);
                            return $"{slotTypeName}: {itemDetails}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Msg($"Error getting equipped item for slot {slotName}: {ex.Message}");
            }
            return null;
        }

        public static Il2Cpp.EquipmentSlotType? GetEquipmentSlotType(string slotName)
        {
            switch (slotName?.ToLower())
            {
                case "armor": return Il2Cpp.EquipmentSlotType.ARMOR;
                case "coat": return Il2Cpp.EquipmentSlotType.COAT;
                case "glasses": return Il2Cpp.EquipmentSlotType.GLASSES;
                case "gloves": return Il2Cpp.EquipmentSlotType.GLOVES;
                case "hat": return Il2Cpp.EquipmentSlotType.HAT;
                case "jacket": return Il2Cpp.EquipmentSlotType.JACKET;
                case "neck": return Il2Cpp.EquipmentSlotType.NECK;
                case "pants": return Il2Cpp.EquipmentSlotType.PANTS;
                case "shirt": return Il2Cpp.EquipmentSlotType.SHIRT;
                case "shoes": return Il2Cpp.EquipmentSlotType.SHOES;
                case "heldleft": return Il2Cpp.EquipmentSlotType.HELDLEFT;
                case "heldright": return Il2Cpp.EquipmentSlotType.HELDRIGHT;
                default: return null;
            }
        }

        public static string GetSlotAnnouncement(string slotName)
        {
            string slotTypeName = GetSlotTypeName(slotName);
            return $"{slotTypeName}: empty";
        }
        
        public static string GetSlotTypeName(string slotName)
        {
            switch (slotName?.ToLower())
            {
                case "pants": return "Pants";
                case "shirt": return "Shirt";
                case "gloves": return "Gloves";
                case "shoes": return "Shoes";
                case "glasses": return "Glasses";
                case "hat": return "Hat";
                case "jacket": return "Jacket";
                case "coat": return "Coat";
                case "armor": return "Armor";
                case "neck": return "Neck";
                case "heldleft": return "Left hand";
                case "heldright": return "Right hand";
                default:
                    // For numbered slots or unknown slots
                    if (slotName != null && char.IsDigit(slotName[0]))
                    {
                        return "Empty inventory slot";
                    }
                    return slotName ?? "Unknown slot";
            }
        }

        public static string GetInventoryItemAtSlot(int slotIndex)
        {
            try
            {
                var inventoryData = Il2CppSunshine.Metric.InventoryViewData.Singleton;
                if (inventoryData != null)
                {
                    var tabContents = inventoryData.tabContents;
                    if (tabContents == null) return null;

                    // Check if we're in pawn shop by looking at active view type
                    var currentView = UnityEngine.Object.FindObjectOfType<Il2CppSunshine.Views.View>();
                    bool isInPawnShop = currentView != null &&
                                       currentView.GetViewType() == Il2CppSunshine.Views.ViewType.INVENTORY_PAWN;

                    if (isInPawnShop)
                    {
                        // In pawn shop, look directly in PAWNABLES tab data
                        if (tabContents.ContainsKey(Il2Cpp.ItemTabGroup.PAWNABLES))
                        {
                            var pawnablesItems = tabContents[Il2Cpp.ItemTabGroup.PAWNABLES];
                            if (pawnablesItems != null && pawnablesItems.ContainsKey(slotIndex))
                            {
                                return GetFormattedItemName(pawnablesItems[slotIndex], inventoryData);
                            }
                        }
                    }
                    else
                    {
                        // Normal tabbed inventory - read current tab from game's state
                        Il2Cpp.ItemTabGroup currentTab = Il2Cpp.ItemTabGroup.TOOLS; // fallback

                        // Try PageSystem first (the active inventory system)
                        var pageSystemPanel = UnityEngine.Object.FindObjectOfType<Il2CppDiscoPages.Elements.Inventory.PageSystemInventoryTabPanel>();
                        if (pageSystemPanel != null)
                        {
                            currentTab = pageSystemPanel.CurrentItemTabGroup;
                            MelonLogger.Msg($"[InventoryHighlighter] PageSystem current tab: {currentTab}");
                        }
                        else
                        {
                            // Fallback to singleton if PageSystem not available
                            var inventoryTabPanel = Il2Cpp.InventoryTabPanel.Singleton;
                            if (inventoryTabPanel != null)
                            {
                                currentTab = inventoryTabPanel.CurrentItemTabGroup;
                                MelonLogger.Msg($"[InventoryHighlighter] Singleton current tab: {currentTab}");
                            }
                        }

                        if (tabContents.ContainsKey(currentTab))
                        {
                            var currentTabItems = tabContents[currentTab];
                            if (currentTabItems != null && currentTabItems.ContainsKey(slotIndex))
                            {
                                return GetFormattedItemName(currentTabItems[slotIndex], inventoryData);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Msg($"Error getting inventory item at slot {slotIndex}: {ex.Message}");
            }
            return null;
        }

        private static string GetFormattedItemName(string itemName, Il2CppSunshine.Metric.InventoryViewData inventoryData)
        {
            if (string.IsNullOrEmpty(itemName)) return null;

            var library = inventoryData.GetLibrary();
            if (library != null)
            {
                var inventoryItem = library.GetByName(itemName);
                if (inventoryItem != null)
                {
                    return FormatInventoryItemForSpeech(inventoryItem);
                }
            }
            return itemName;
        }

        private static string FormatInventoryItemForSpeech(Il2CppSunshine.Metric.InventoryItem item)
        {
            try
            {
                System.Text.StringBuilder result = new System.Text.StringBuilder();
                
                // Add the display name
                if (!string.IsNullOrEmpty(item.displayName))
                {
                    result.Append(item.displayName);
                }
                else if (!string.IsNullOrEmpty(item.listName))
                {
                    result.Append(item.listName);
                }
                
                // Add equipment effects/bonuses
                if (item.equipEffects != null && item.equipEffects.Count > 0)
                {
                    result.Append(". Bonuses: ");
                    foreach (var effect in item.equipEffects)
                    {
                        if (effect != null)
                        {
                            // Format the effect properly with stat name and value
                            string effectText = FormatCharacterEffect(effect);
                            if (!string.IsNullOrEmpty(effectText))
                            {
                                result.Append($"{effectText}, ");
                            }
                        }
                    }
                }
                
                // Add item description if it exists
                if (!string.IsNullOrEmpty(item.description))
                {
                    result.Append($". {item.description}");
                }
                
                // Add item value if it exists
                if (item.itemValue > 0)
                {
                    // itemValue is stored in cents
                    if (item.itemValue < 100)
                    {
                        result.Append($". Value: {item.itemValue} cents");
                    }
                    else
                    {
                        decimal valueInReal = item.itemValue / 100m;
                        result.Append($". Value: {valueInReal:F2} reál");
                    }
                }
                
                return result.ToString();
            }
            catch (Exception ex)
            {
                MelonLogger.Msg($"Error formatting inventory item: {ex.Message}");
                return item.displayName ?? item.listName ?? "Unknown item";
            }
        }
        
        private static string FormatCharacterEffect(Il2CppSunshine.Metric.CharacterEffect effect)
        {
            try
            {
                if (effect == null) return null;
                
                // Try using the EffectName method which should format it properly
                string effectName = effect.EffectName(editor: false, withColor: false, revertTagsForRTL: false, revertFormatForRTL: false);
                if (!string.IsNullOrEmpty(effectName))
                {
                    return effectName;
                }
                
                // Fallback: construct manually from properties
                string sign = effect.Sign ?? "";
                int value = effect.parameter;
                string statName = "";
                
                // Try to get skill name first
                if (effect.skillType != Il2CppSunshine.Metric.SkillType.NONE)
                {
                    statName = effect.skillType.ToString();
                }
                // Otherwise try ability name
                else if (effect.abilityType != Il2CppSunshine.Metric.AbilityType.Error)
                {
                    statName = effect.abilityType.ToString();
                }
                
                if (!string.IsNullOrEmpty(statName))
                {
                    // Clean up the stat name (remove underscores, etc)
                    statName = statName.Replace("_", " ");
                    return $"{sign}{value} {statName}";
                }
                
                return null;
            }
            catch (Exception ex)
            {
                MelonLogger.Msg($"Error formatting character effect: {ex.Message}");
                return null;
            }
        }
    }
}