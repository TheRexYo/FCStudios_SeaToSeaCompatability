﻿using System;
using System.Collections.Generic;
using FCS_AlterraHub.Helpers;
using FCS_AlterraHub.Interfaces;
using FCS_AlterraHub.Mono;
using FCS_StorageSolutions.Configuration;
using FCS_StorageSolutions.Mods.AlterraStorage.Buildable;
using FCS_StorageSolutions.Mods.DataStorageSolutions.Buildable;
using FCS_StorageSolutions.Mods.DataStorageSolutions.Mono.Server;
using FCS_StorageSolutions.Mods.DataStorageSolutions.Mono.Transceiver;
using FCSCommon.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace FCS_StorageSolutions.Mods.DataStorageSolutions.Mono.Rack
{
    internal class DSSSlotController : MonoBehaviour, IFCSDumpContainer, IHandTarget,ISlotController
    {
        private DumpContainerSimplified _dumpContainer;
        private InventoryItem _inventoryItem;
        private IDSSRack _controller;
        private string _slotName;
        private BoxCollider _collider;
        private FCSStorage _storage;
        private DSSServerController _mountedServer;
        private DSSTransceiverController _transceiver;
        private Image _preloader;
        private Text _preloaderPercentage;
        public bool IsOccupied => _mountedServer != null || _transceiver != null;
        public bool IsFull => _mountedServer?.IsFull() ?? true;

        private void Start()
        {
            QuickLogger.Debug($"Storage Status: {_storage}");
            _storage?.CleanUpDuplicatedStorageNoneRoutine();
            Mod.CleanDummyServers();
        }
        
        private void Update()
        {
            if (_collider == null || _controller == null || _preloader == null || _preloaderPercentage == null) return;
            _collider.isTrigger = !_controller.IsOpen;

            if (_mountedServer != null)
            {
                _preloader.fillAmount = _mountedServer.GetPercentage();
                _preloaderPercentage.text = $"{_mountedServer.GetPercentage():P0}";
            }
            
        }

        internal void Initialize(string slotName, IDSSRack controller,GameObject preloader)
        {
            _preloader = preloader.GetComponent<Image>();
            _preloaderPercentage = preloader.GetComponentInChildren<Text>();
            _collider = gameObject.GetComponent<BoxCollider>();
            _controller = controller;
            _slotName = slotName;
            if (_dumpContainer == null)
            {
                _dumpContainer = gameObject.EnsureComponent<DumpContainerSimplified>();
                _dumpContainer.Initialize(transform,$"Add server to {_slotName}",this,1,1,gameObject.name);
            }


            if (_storage == null)
            {
                _storage = gameObject.AddComponent<FCSStorage>();
                _storage.Initialize(1,gameObject);
                _storage.ItemsContainer.onAddItem += OnServerAddedToStorage;
                _storage.ItemsContainer.onRemoveItem += OnServerRemovedFromStorage;
            }
        }

        private void OnServerRemovedFromStorage(InventoryItem item)
        {
            var server = item.item.gameObject.GetComponent<DSSServerController>();
            var transceiver = item.item.gameObject.GetComponent<DSSTransceiverController>();

            if (server == null && transceiver == null)
            {
                return;
            }


            if (server != null)
            {
                server.UnDockServer();
                _controller.Manager.RemoveServerFromBase(_mountedServer);
                server.GetStorage().ItemsContainer.onAddItem -= OnMountedServerUpdate;
                server.GetStorage().ItemsContainer.onRemoveItem -= OnMountedServerUpdate;
            }

            _mountedServer = null;
            _transceiver = null;
            _inventoryItem = null;
        }

        private void OnServerAddedToStorage(InventoryItem item)
        {
            if(item == null) return;
            ModelPrefab.ApplyShaders(item.item.gameObject);
            MountServerToRack(item);
        }

        private void OnMountedServerUpdate(InventoryItem item)
        {
            _controller.UpdateStorageCount();
        }

        public void OnHandHover(GUIHand hand)
        {
            if(_controller == null) return;
            if(_controller.IsOpen)
            {
                HandReticle main = HandReticle.main;
                main.SetIcon(HandReticle.IconType.Hand);
                main.SetInteractText(IsOccupied ? $"Remove server from {_slotName}" : $"Add server to {_slotName}");
            }
        }
        
        public void OnHandClick(GUIHand hand)
        {
            if (_controller == null || !_controller.IsOpen) return;

            FindServer();

            if (IsOccupied)
            {
                if (_mountedServer != null)
                {
                    var pickup = _inventoryItem.item;
                    _storage.ItemsContainer.RemoveItem(pickup);
                    PlayerInteractionHelper.GivePlayerItem(pickup);
                    _preloader.fillAmount = 0;
                    _preloaderPercentage.text = $"{0:P0}";
                    _controller.UpdateStorageCount();
                }

                if(_transceiver != null)
                {
                    PlayerInteractionHelper.GivePlayerItem(_inventoryItem.item);
                }

            }
            else
            {
                _dumpContainer.OpenStorage();
            }
        }

        #region Dump Container

        /// <summary>
        /// Event when the DumpContainer has an item added
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool AddItemToContainer(InventoryItem item)
        {
           return  _storage.AddItemToContainer(item);
        }

        private bool MountServerToRack(InventoryItem item)
        {
            QuickLogger.Debug("Adding Server", true);
            try
            {
                if (item.item.GetTechType() != Mod.GetDSSServerTechType() && item.item.GetTechType() != Mod.GetTransceiverTechType() || _controller == null) return false;

                QuickLogger.Debug($"Valid Server, {Language.main.Get(item.item.GetTechType())}", true); 

                if (item.item.GetTechType() == Mod.GetDSSServerTechType())
                {
                    _mountedServer = item.item.gameObject.GetComponentInChildren<DSSServerController>();
                    if (_mountedServer != null)
                    {
                        _mountedServer.DockServer(this, _controller);
                        _mountedServer.GetStorage().ItemsContainer.onAddItem += OnMountedServerUpdate;
                        _mountedServer.GetStorage().ItemsContainer.onRemoveItem += OnMountedServerUpdate;
                        _controller.Manager?.RegisterServerInBase(_mountedServer);
                        _controller.UpdateStorageCount();
                    }
                    else
                    {
                        QuickLogger.Debug("Mounted Server was null");
                    }

                    if (_controller == null)
                    {
                        QuickLogger.Debug("CONTROLLER IS NULL");
                    }
                }

                if (item.item.GetTechType() == Mod.GetTransceiverTechType())
                {
                    //Do something
                    _transceiver = item.item.gameObject.GetComponentInChildren<DSSTransceiverController>();
                    _transceiver.DockTransceiver(this,_controller);
                    QuickLogger.Debug($"Mounting a transmitter in {_slotName} in serverRack {_controller.UnitID}");
                }

                _inventoryItem = item;
            }
            catch (Exception e)
            {
                QuickLogger.Error("Please contact FCStudios about this fail", true);
                QuickLogger.DebugError(e.Message);
                QuickLogger.DebugError(e.StackTrace);
                PlayerInteractionHelper.GivePlayerItem(item);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Dump Container calls this method to see if this item can be added to the container.
        /// </summary>
        /// <param name="pickupable"></param>
        /// <param name="verbose"></param>
        /// <returns></returns>
        public bool IsAllowedToAdd(TechType techType, bool verbose)
        {
            return techType == Mod.GetDSSServerTechType() || techType == Mod.GetTransceiverTechType();
        }

        public bool IsAllowedToAdd(Pickupable pickupable, bool verbose)
        {
            return IsAllowedToAdd(pickupable.GetTechType(), verbose);
        }

        #endregion

        public bool AddItemToMountedServer(InventoryItem item)
        {
            return _mountedServer.AddItemToContainer(item);
        }

        public string GetSlotName()
        {
            return _slotName;
        }

        public byte[] Save(ProtobufSerializer serializer)
        {
            return _storage.Save(serializer);
        }

        public void RestoreItems(ProtobufSerializer serializer,byte[] data)
        {
            _storage.RestoreItems(serializer,data);
        }

        public int GetStorageAmount()
        {
            FindServer();

            if (_mountedServer == null)
            {
                return 0;
            }

            return _mountedServer.GetCount();
        }

        private void FindServer()
        {
            if (_mountedServer == null && _transceiver == null)
            {
                if (_inventoryItem != null)
                {
                    _mountedServer = _inventoryItem.item.gameObject.GetComponentInChildren<DSSServerController>();
                    _transceiver = _inventoryItem.item.gameObject.GetComponentInChildren<DSSTransceiverController>();
                }
            }
        }

        public bool HasSpace(int amount)
        {
            return _mountedServer.HasSpace(amount);
        }

        public int GetFreeSpace()
        {
            if (_mountedServer == null) return 0;

            return _mountedServer.GetFreeSpace();
        }

        public bool IsTechTypeAllowed(TechType techType)
        {
            if (_mountedServer == null) return false;
            return _mountedServer.IsTechTypeAllowed(techType);
        }

        public int GetItemCount(TechType techType)
        {
            return _mountedServer.GetItemCount(techType);
        }

        public bool HasItem(TechType techType)
        {
            if (_mountedServer == null) return false;
            return _mountedServer.HasItem(techType);
        }

        public Pickupable RemoveItemFromServer(TechType techType)
        {
            if (_mountedServer == null) return null;
            return _mountedServer.RemoveItemFromContainer(techType,1);
        }

        public FcsDevice GetServer()
        {
            return _mountedServer;
        }
    }
}