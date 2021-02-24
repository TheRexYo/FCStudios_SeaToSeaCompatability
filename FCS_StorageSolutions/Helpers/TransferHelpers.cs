﻿using System.Collections.Generic;
using FCS_StorageSolutions.Mods.DataStorageSolutions.Mono.Rack;
using FCSCommon.Utilities;


namespace FCS_StorageSolutions.Helpers
{
    internal static class TransferHelpers
    {
        internal static bool AddItemToRack(DSSRackBase rack, InventoryItem item, int amount)
        {
            foreach (KeyValuePair<string, DSSSlotController> slotController in rack.GetSlots())
            {
                //TODO Check filter
                if (slotController.Value.IsOccupied && slotController.Value.HasSpace(amount))
                {
                    var result = slotController.Value.AddItemToMountedServer(item);

                    if (!result)
                    {
                        QuickLogger.Debug(
                            $"Failed to add item to server: {slotController.Value.GetSlotName()} in rack {rack.GetPrefabID()}",
                            true);
                        return false;
                    }

                    QuickLogger.Debug(
                        $"Added item to server: {slotController.Value.GetSlotName()} in rack {rack.GetPrefabID()}",
                        true);
                    return true;
                }
            }

            return false;
        }
    }
}
