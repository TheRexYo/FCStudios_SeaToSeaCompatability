﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FCS_AlterraHub.Interfaces;
using FCSCommon.Utilities;
using UnityEngine;

namespace FCS_AlterraHub.Mono
{
    public class FCSStorage : MonoBehaviour, IFCSStorage
    {
        private GameObject _storageRoot;
        private byte[] _storageRootBytes;
        private int _slots;
        public Action<int, int> OnContainerUpdate { get; set; }
        public Action<FcsDevice, TechType> OnContainerAddItem { get; set; }
        public Action<FcsDevice, TechType> OnContainerRemoveItem { get; set; }
        
        public ItemsContainer ItemsContainer { get; set; }
        public int StorageCount()
        {
            return GetCount();
        }

        public byte[] Save(ProtobufSerializer serializer)
        {
            if (serializer == null || _storageRoot == null)
            {
                QuickLogger.DebugError($"Failed to save: Serializer: {serializer} || Root {_storageRoot?.name}",true);
                return null;
            }

            _storageRootBytes = StorageHelper.Save(serializer, _storageRoot);
            return _storageRootBytes;
        }

#if SUBNAUTICA_STABLE
        public void RestoreItems(ProtobufSerializer serializer, byte[] serialData)
        {
            QuickLogger.Debug("RestoreItems");
            StorageHelper.RenewIdentifier(_storageRoot);
            QuickLogger.Debug("RenewIdentifier Called");
            if (serialData == null)
            {
                return;
            }

            QuickLogger.Debug($"Storage root Position: {_storageRoot.transform.position}");

            using (MemoryStream memoryStream = new MemoryStream(serialData))
            {
                QuickLogger.Debug("Getting Data from memory stream");
                GameObject gObj = serializer.DeserializeObjectTree(memoryStream, 0);
                QuickLogger.Debug($"De-serialized Object Stream. {gObj} | {gObj.name}");
                TransferItems(gObj);
                QuickLogger.Debug("Items Transferred");
                QuickLogger.Debug($"Object location: {gObj.transform.position}");
                GameObject.Destroy(gObj);
                QuickLogger.Debug("Item destroyed");
            }
        }

        public void RestoreItems(ProtobufSerializer serializer, List<byte[]> serialData)
        {
            QuickLogger.Debug("RestoreItems");
            
            StorageHelper.RenewIdentifier(_storageRoot);
            
            QuickLogger.Debug("RenewIdentifier Called");
            
            if (serialData == null) return;

            QuickLogger.Debug($"Storage root Position: {_storageRoot.transform.position}");

            for (var i = 0; i < serialData.Count; i++)
            {
                byte[] bytes = serialData[i];

                QuickLogger.Debug($"Loading Save data: {i}/{serialData.Count}");

                if (bytes == null) continue;

                using (MemoryStream memoryStream = new MemoryStream(bytes))
                {
                    QuickLogger.Debug("Getting Data from memory stream");
                    GameObject gObj = serializer.DeserializeObjectTree(memoryStream, 0);
                    QuickLogger.Debug($"De-serialized Object Stream. {gObj} | {gObj.name}");
                    TransferItems(gObj);
                    QuickLogger.Debug("Items Transferred");
                    QuickLogger.Debug($"Object location: {gObj.transform.position}");
                    GameObject.Destroy(gObj);
                    QuickLogger.Debug("Item destroyed");
                }
            }
        }
#else
public IEnumerator RestoreItems(ProtobufSerializer serializer, byte[] serialData)
        {
            QuickLogger.Debug("RestoreItems");
            StorageHelper.RenewIdentifier(_storageRoot);
            QuickLogger.Debug("RenewIdentifier Called");
            if (serialData == null)
            {
                yield break;
            }
            
            QuickLogger.Debug($"Storage root Position: {_storageRoot.transform.position}");

            using (MemoryStream memoryStream = new MemoryStream(serialData))
            {
                QuickLogger.Debug("Getting Data from memory stream");
                CoroutineTask<GameObject> task = serializer.DeserializeObjectTreeAsync(memoryStream, false,false,0);
                yield return task;
                var gObj = task.GetResult();
                QuickLogger.Debug($"De-serialized Object Stream. {gObj} | {gObj.name}");
                TransferItems(gObj);
                QuickLogger.Debug("Items Transferred");
                QuickLogger.Debug($"Object location: {gObj.transform.position}");
                Destroy(gObj);
                QuickLogger.Debug("Item destroyed");
            }
            yield break;
        }
#endif

        private void TransferItems(GameObject source)
        {
            QuickLogger.Debug("Attempting to transfer items");

            foreach (UniqueIdentifier uniqueIdentifier in source.GetComponentsInChildren<UniqueIdentifier>(true))
            {
                if (!(uniqueIdentifier.transform.parent != source.transform))
                {
                    Pickupable pickupable = uniqueIdentifier.gameObject.EnsureComponent<Pickupable>();
                    if (!ItemsContainer.Contains(pickupable))
                    {
                        InventoryItem item = new InventoryItem(pickupable);
                        ChangePrefabId(item);
                        item.item.transform.parent = _storageRoot.transform;
                        ItemsContainer.UnsafeAdd(item);
                    }
                }
            }

            CleanUpDuplicatedStorageNoneRoutine();
        }

        private static void ChangePrefabId(InventoryItem item)
        {
            var uniqueIdentifiers = item.item.gameObject.GetComponentsInChildren<UniqueIdentifier>(true);
            
            foreach (var identifier in uniqueIdentifiers)
            {
                identifier.Id = Guid.NewGuid().ToString();
            }
        }

        public void CleanUpDuplicatedStorageNoneRoutine()
        {
            QuickLogger.Debug("Cleaning Duplicates", true);

            StoreInformationIdentifier[] sids = gameObject.GetComponentsInChildren<StoreInformationIdentifier>(true);
#if DEBUG
            QuickLogger.Debug($"SIDS: {sids.Length}", true);
#endif

            int num;
            for (int i = sids.Length - 1; i >= 0; i = num - 1)
            {
                StoreInformationIdentifier storeInformationIdentifier = sids[i];
                if (storeInformationIdentifier != null && storeInformationIdentifier.name.StartsWith("SerializerEmptyGameObject", StringComparison.OrdinalIgnoreCase))
                {
                    QuickLogger.Debug($"Duplicate: {storeInformationIdentifier.gameObject.transform.position} | {storeInformationIdentifier.gameObject.name}");
                    GameObject.Destroy(storeInformationIdentifier.gameObject);
                    QuickLogger.Debug($"Destroyed Duplicate");
                }
                num = i;
            }
        }

        public int GetCount()
        {
            int i = 0;
            foreach (TechType techType in ItemsContainer.GetItemTypes())
            {
                foreach (var invItem in ItemsContainer.GetItems(techType))
                {
                    i++;
                }
            }

            return i;
        }

        public int GetFreeSpace()
        {
            return _slots - GetCount();
        }

        public Dictionary<TechType,int> GetItemsWithin()
        {
            List<TechType> keys = ItemsContainer.GetItemTypes();
            var lookup = keys?.Where(x => x != TechType.None).ToLookup(x => x).ToArray();
            return lookup?.ToDictionary(count => count.Key, count => ItemsContainer.GetCount(count.Key));
        }

        public bool AddItemToContainer(InventoryItem item)
        {
            ItemsContainer.UnsafeAdd(item);
          return true;
        }
        
        public void Initialize(int slots, GameObject storageRoot = null)
        {
            if (storageRoot == null)
            {
                _storageRoot = new GameObject("FCSStorage");
                _storageRoot.transform.parent = transform;
                UWE.Utils.ZeroTransform(_storageRoot);
            }
            else
            {
                _storageRoot = storageRoot;
            }

            _slots = slots;
            _storageRoot.AddComponent<StoreInformationIdentifier>();
            ItemsContainer = new ItemsContainer(slots, slots, _storageRoot.transform, "FCSStorage", null);
            ItemsContainer.isAllowedToAdd += IsAllowedToAdd;
            ItemsContainer.isAllowedToRemove += IsAllowedToRemoveItems;
        }

        private bool IsAllowedToRemoveItems(Pickupable pickupable, bool verbose)
        {
            return true;
        }

        public void Initialize(int slots, int width, int height,string name, GameObject go = null)
        {
            if (go == null)
            {
                _storageRoot = new GameObject("FCSStorage");
                _storageRoot.transform.parent = transform;
                UWE.Utils.ZeroTransform(_storageRoot);
            }
            else
            {
                _storageRoot = go;
            }

            _slots = slots;
            _storageRoot.AddComponent<StoreInformationIdentifier>();
            ItemsContainer = new ItemsContainer(width, height, _storageRoot.transform, name, null);
            ItemsContainer.isAllowedToAdd += IsAllowedToAdd;
            ItemsContainer.isAllowedToRemove += IsAllowedToRemoveItems;
        }

        public int GetContainerFreeSpace => _slots - GetCount();
        public bool IsFull => GetCount() >= _slots;

        public List<TechType> InvalidTechTypes = new List<TechType>();
        public virtual bool CanBeStored(int amount, TechType techType)
        {
            QuickLogger.Debug($"GetCount: {GetCount()} | Amount {amount} | Slots: {_slots}", true);

            if (InvalidTechTypes.Contains(techType) || IsFull) return false;
            
            return GetCount() + amount <= _slots;
        }

        public virtual bool IsAllowedToAdd(Pickupable pickupable, bool verbose)
        {
            return CanBeStored(1,pickupable.GetTechType());
        }

        public virtual bool IsAllowedToRemoveItems()
        {
            return true;
        }

        public virtual Pickupable RemoveItemFromContainer(TechType techType)
        {
           return ItemsContainer.RemoveItem(techType);
        }

        public bool ContainsItem(TechType techType)
        {
            return ItemsContainer.Contains(techType);
        }
    }
}
