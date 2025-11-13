using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using MelonLoader;
using Il2CppDiscoPages.Elements.Inventory;
using Il2CppSunshine.Metric;
using Il2CppSunshine.Views;
using Il2CppPages.Gameplay.Inventory;
using Il2Cpp;
using AccessibilityMod.UI;

namespace AccessibilityMod.Inventory
{
    public class InventoryNavigationHandler
    {
        private static InventoryNavigationHandler _instance;
        public static InventoryNavigationHandler Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new InventoryNavigationHandler();
                }
                return _instance;
            }
        }

        private bool isInventoryOpen = false;
        private InventoryItemSlot lastSelectedSlot = null;
        private string lastAnnounced = "";
        private float lastAnnouncementTime = 0f;
        private const float ANNOUNCEMENT_COOLDOWN = 0.2f;

        // Track current tab
        private ItemTabGroup currentTab = ItemTabGroup.TOOLS;
        private PageSystemInventoryTabPanel lastTabPanel = null;

        public void Initialize()
        {
            MelonLogger.Msg("InventoryNavigationHandler initialized");
        }

        public void Update()
        {
            try
            {
                // Check if inventory is open
                CheckInventoryState();

                if (isInventoryOpen)
                {
                    // Check for selected inventory items
                    CheckSelectedInventoryItem();
                    
                    // Check for tab changes
                    CheckTabChanges();
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in InventoryNavigationHandler.Update: {ex}");
            }
        }

        private void CheckInventoryState()
        {
            try
            {
                // Check for InventoryView or InventoryPage active in scene
                var inventoryView = UnityEngine.Object.FindObjectOfType<InventoryView>();
                var inventoryPage = UnityEngine.Object.FindObjectOfType<InventoryPage>();
                var inventoryItemsPage = UnityEngine.Object.FindObjectOfType<InventoryItemsPage>();

                bool wasOpen = isInventoryOpen;
                isInventoryOpen = (inventoryView != null && inventoryView.gameObject.activeInHierarchy) ||
                                 (inventoryPage != null && inventoryPage.gameObject.activeInHierarchy) ||
                                 (inventoryItemsPage != null && inventoryItemsPage.gameObject.activeInHierarchy);

                // Announce state changes
                if (isInventoryOpen && !wasOpen)
                {
                    OnInventoryOpened();
                }
                else if (!isInventoryOpen && wasOpen)
                {
                    OnInventoryClosed();
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error checking inventory state: {ex}");
            }
        }

        private void CheckSelectedInventoryItem()
        {
            try
            {
                // Check EventSystem for selected GameObject
                var eventSystem = EventSystem.current;
                if (eventSystem == null) return;

                var selected = eventSystem.currentSelectedGameObject;
                if (selected == null) return;

                // Check if it's an inventory slot
                var inventorySlot = selected.GetComponent<InventoryItemSlot>();
                if (inventorySlot == null)
                {
                    // Check parent for inventory slot
                    inventorySlot = selected.GetComponentInParent<InventoryItemSlot>();
                }

                if (inventorySlot != null && inventorySlot != lastSelectedSlot)
                {
                    lastSelectedSlot = inventorySlot;
                    AnnounceInventoryItem(inventorySlot);
                }

                // Also check for equipment slots
                var equipmentSlot = selected.GetComponent<InventoryEquipmentSlot>();
                if (equipmentSlot == null)
                {
                    equipmentSlot = selected.GetComponentInParent<InventoryEquipmentSlot>();
                }

                if (equipmentSlot != null)
                {
                    AnnounceEquipmentSlot(equipmentSlot);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error checking selected inventory item: {ex}");
            }
        }

        private void CheckTabChanges()
        {
            try
            {
                // Find active tab panel
                var tabPanel = UnityEngine.Object.FindObjectOfType<PageSystemInventoryTabPanel>();
                if (tabPanel == null) return;

                if (tabPanel != lastTabPanel)
                {
                    lastTabPanel = tabPanel;
                    // Tab panel changed, check current tab
                    CheckCurrentTab(tabPanel);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error checking tab changes: {ex}");
            }
        }

        private void CheckCurrentTab(PageSystemInventoryTabPanel tabPanel)
        {
            try
            {
                // Check for active tab buttons
                var tabButtons = tabPanel.GetComponentsInChildren<PageSystemInventoryTabButton>();
                foreach (var button in tabButtons)
                {
                    if (button != null && button.gameObject.activeInHierarchy)
                    {
                        // Check if this button is selected
                        var selectable = button.GetComponent<UnityEngine.UI.Selectable>();
                        if (selectable != null && EventSystem.current != null && 
                            EventSystem.current.currentSelectedGameObject == button.gameObject)
                        {
                            // Get tab name from button
                            var text = UIElementFormatter.ExtractTextFromGameObject(button.gameObject);
                            if (!string.IsNullOrEmpty(text))
                            {
                                AnnounceText($"Tab: {text}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error checking current tab: {ex}");
            }
        }

        private void AnnounceInventoryItem(InventoryItemSlot slot)
        {
            try
            {
                if (slot == null) return;

                string announcement = "";

                // Get item information
                if (slot.item != null)
                {
                    var item = slot.item;
                    
                    // Get item name
                    string itemName = GetItemDisplayName(item);
                    if (string.IsNullOrEmpty(itemName))
                    {
                        itemName = slot.itemName;
                    }

                    // Get item description
                    string description = GetItemDescription(item);

                    // Get item type and group
                    string itemType = GetItemTypeString(item);

                    // Build announcement
                    announcement = itemName;
                    
                    if (!string.IsNullOrEmpty(itemType))
                    {
                        announcement += $", {itemType}";
                    }

                    if (item.substance)
                    {
                        announcement += ", Consumable";
                        if (item.substanceActive)
                        {
                            announcement += " (Active)";
                        }

                        // Add number of uses for multi-use items
                        if (item.substanceUses > 0)
                        {
                            announcement += $", {item.substanceUses} use{(item.substanceUses != 1 ? "s" : "")} remaining";
                        }

                        // Add substance effects (bonuses/penalties when consumed)
                        string substanceEffects = GetSubstanceEffects(item);
                        if (!string.IsNullOrEmpty(substanceEffects))
                        {
                            announcement += $" - Effects: {substanceEffects}";
                        }
                    }

                    if (item.cursed)
                    {
                        announcement += ", Cursed";
                    }

                    // Add description if available
                    if (!string.IsNullOrEmpty(description) && description != itemName)
                    {
                        announcement += $". {description}";
                    }
                }
                else if (!string.IsNullOrEmpty(slot.itemName))
                {
                    // Slot has name but no item
                    announcement = slot.itemName;
                }
                else
                {
                    // Empty slot
                    announcement = "Empty slot";
                }

                AnnounceText(announcement);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error announcing inventory item: {ex}");
            }
        }

        private void AnnounceEquipmentSlot(InventoryEquipmentSlot slot)
        {
            try
            {
                if (slot == null) return;

                string announcement = "";

                // Get slot type
                string slotType = GetEquipmentSlotType(slot);
                
                // Check if slot has an item by looking at the item name
                if (!string.IsNullOrEmpty(slot.prevItemName))
                {
                    announcement = $"{slotType}: {slot.prevItemName}";
                }
                else
                {
                    announcement = $"{slotType}: Empty";
                }

                AnnounceText(announcement);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error announcing equipment slot: {ex}");
            }
        }

        private string GetItemDisplayName(InventoryItem item)
        {
            try
            {
                if (item == null) return "";

                // Try display name property
                string name = item.displayName;
                if (!string.IsNullOrEmpty(name)) return name;

                // Try list name
                name = item.listName;
                if (!string.IsNullOrEmpty(name)) return name;

                // Try getting from game object name
                return item.gameObject.name.Replace("_", " ");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting item display name: {ex}");
                return "";
            }
        }

        private string GetItemDescription(InventoryItem item)
        {
            try
            {
                if (item == null) return "";
                return item.description;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting item description: {ex}");
                return "";
            }
        }

        private string GetSubstanceEffects(InventoryItem item)
        {
            try
            {
                if (item == null || item.substanceBuffs == null || item.substanceBuffs.Count == 0)
                    return null;

                var effectStrings = new List<string>();

                // Iterate through substance buffs
                foreach (var buff in item.substanceBuffs)
                {
                    if (buff == null || buff.effects == null)
                        continue;

                    // Each buff contains an array of CharacterEffects
                    foreach (var effect in buff.effects)
                    {
                        if (effect == null)
                            continue;

                        // Format the effect using the same method as thoughts
                        string effectDesc = FormatCharacterEffect(effect);
                        if (!string.IsNullOrEmpty(effectDesc))
                        {
                            effectStrings.Add(effectDesc);
                        }
                    }
                }

                return effectStrings.Count > 0 ? string.Join(", ", effectStrings) : null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting substance effects: {ex}");
                return null;
            }
        }

        private string FormatCharacterEffect(CharacterEffect effect)
        {
            try
            {
                if (effect == null)
                    return null;

                // Use the game's own EffectName method to get the formatted effect string
                string effectName = effect.EffectName(false, false, false, true);

                // Filter out empty, null, or technical effects
                if (
                    string.IsNullOrEmpty(effectName)
                    || effectName.Contains("LUA_")
                    || effectName.Contains("COMMAND")
                    || effectName.Trim() == "0"
                    || effectName.Contains("+0 ")
                    || effectName.Contains("-0 ")
                )
                {
                    return null;
                }

                return effectName.Trim();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error formatting character effect: {ex}");
                return null;
            }
        }

        private string GetItemTypeString(InventoryItem item)
        {
            try
            {
                if (item == null) return "";

                // Get item type
                string typeStr = item.type.ToString();
                
                // Get item group
                string groupStr = item.group.ToString();

                // Combine if different
                if (typeStr != groupStr && !string.IsNullOrEmpty(groupStr))
                {
                    return $"{typeStr}, {groupStr}";
                }
                
                return typeStr;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error getting item type: {ex}");
                return "";
            }
        }

        private string GetEquipmentSlotType(InventoryEquipmentSlot slot)
        {
            try
            {
                // Try to get slot type from the slot itself
                return slot.slotType.ToString().Replace("_", " ");
            }
            catch (Exception ex)
            {
                try
                {
                    // Try to get from UI text as fallback
                    var text = UIElementFormatter.ExtractTextFromGameObject(slot.gameObject);
                    if (!string.IsNullOrEmpty(text))
                    {
                        return text;
                    }
                }
                catch (Exception ex2)
                {
                    MelonLogger.Error($"Error getting equipment slot text: {ex2}");
                }

                MelonLogger.Error($"Error getting equipment slot type: {ex}");
                return "Equipment slot";
            }
        }

        private void OnInventoryOpened()
        {
            // No announcement needed - it's obvious from context
            lastSelectedSlot = null;
        }

        private void OnInventoryClosed()
        {
            // No announcement needed - it's obvious from context
            lastSelectedSlot = null;
            lastTabPanel = null;
        }

        private void AnnounceText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (text == lastAnnounced && Time.time - lastAnnouncementTime < ANNOUNCEMENT_COOLDOWN) return;

            TolkScreenReader.Instance.Speak(text, false);
            lastAnnounced = text;
            lastAnnouncementTime = Time.time;
        }

        public void OnInventoryViewOpened(InventoryView view)
        {
            // Called from patch
            isInventoryOpen = true;
            OnInventoryOpened();
        }

        public void OnInventoryViewClosed()
        {
            // Called from patch
            isInventoryOpen = false;
            OnInventoryClosed();
        }

        public void OnInventoryItemSelected(InventoryItemSlot slot)
        {
            // Called from patch
            if (slot != lastSelectedSlot)
            {
                lastSelectedSlot = slot;
                AnnounceInventoryItem(slot);
            }
        }

        public void OnTabChanged(ItemTabGroup newTab)
        {
            // Called from patch
            if (newTab != currentTab)
            {
                currentTab = newTab;
                string tabName = newTab.ToString().Replace("_", " ");
                AnnounceText($"Tab: {tabName}");
            }
        }

        public ItemTabGroup GetCurrentTab()
        {
            // Read current tab from game's state instead of tracking it ourselves
            Il2Cpp.ItemTabGroup gameTab = Il2Cpp.ItemTabGroup.TOOLS; // fallback

            // Try PageSystem first (the active inventory system)
            var pageSystemPanel = UnityEngine.Object.FindObjectOfType<Il2CppDiscoPages.Elements.Inventory.PageSystemInventoryTabPanel>();
            if (pageSystemPanel != null)
            {
                gameTab = pageSystemPanel.CurrentItemTabGroup;
            }
            else
            {
                // Fallback to singleton if PageSystem not available
                var inventoryTabPanel = Il2Cpp.InventoryTabPanel.Singleton;
                if (inventoryTabPanel != null)
                {
                    gameTab = inventoryTabPanel.CurrentItemTabGroup;
                }
            }

            return gameTab;
        }
    }
}