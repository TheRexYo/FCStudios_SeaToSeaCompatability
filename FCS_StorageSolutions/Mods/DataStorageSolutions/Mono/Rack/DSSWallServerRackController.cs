﻿using System;
using System.Collections.Generic;
using System.Linq;
using FCS_AlterraHomeSolutions.Mono.PaintTool;
using FCS_AlterraHub.Extensions;
using FCS_AlterraHub.Helpers;
using FCS_AlterraHub.Interfaces;
using FCS_AlterraHub.Mono;
using FCS_AlterraHub.Registration;
using FCS_StorageSolutions.Configuration;
using FCS_StorageSolutions.Helpers;
using FCS_StorageSolutions.Mods.AlterraStorage.Buildable;
using FCSCommon.Helpers;
using FCSCommon.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace FCS_StorageSolutions.Mods.DataStorageSolutions.Mono.Rack
{
    internal class DSSWallServerRackController : FcsDevice, IFCSSave<SaveData>, IHandTarget, IDSSRack
    {
        private bool _runStartUpOnEnable;
        private bool _isFromSave;
        private bool _isBeingDestroyed;
        private DSSWallServerRackDataEntry _savedData;
        private float _targetPos;
        private float _speed = 3f;
        private GameObject _tray;
        private Dictionary<string, DSSSlotController> _slots;
        private List<GameObject> _meters;
        private Text _storageAmount;
        private Image _percentageBar;
        private GameObject _canvas;
        private bool _isVisible;
        private const float OpenPos = 0.382f;
        private const float ClosePos = 0f;
        public override bool IsVisible => _isVisible;
        public override bool IsRack { get; } = true;
        public override bool IsOperational => IsInitialized && IsConstructed;

        public override float GetPowerUsage()
        {
            if (Manager == null || Manager.GetBreakerState() || !IsConstructed) return 0f;

            return 0.01f + _slots.Count(x => x.Value != null && x.Value.IsOccupied) * 0.01f;
        }

        private void OnBreakerStateChanged(bool value)
        {
            UpdateScreenState();
        }

        private void OnPowerStateChanged(PowerSystem.Status obj)
        {
            UpdateScreenState();
        }

        private void UpdateScreenState()
        {
            if (Manager.GetBreakerState() || Manager.GetPowerState() == PowerSystem.Status.Offline)
            {
                TurnOffDevice();
            }
            else
            {
                TurnOnDevice();
            }
        }

        public bool IsOpen => _targetPos > 0;

        private void Start()
        {
            FCSAlterraHubService.PublicAPI.RegisterDevice(this, Mod.DSSTabID, Mod.ModName);
            Manager.OnPowerStateChanged += OnPowerStateChanged;
            Manager.OnBreakerStateChanged += OnBreakerStateChanged;
            UpdateStorageCount();
            UpdateScreenState();
        }

        private void RegisterServers()
        {
            foreach (KeyValuePair<string, DSSSlotController> controller in _slots)
            {
                if (controller.Value != null && controller.Value.IsOccupied && controller.Value.GetServer() != null)
                {
                    Manager.RegisterServerInBase(controller.Value.GetServer());
                }
            }
        }

        private void Update()
        {
            MoveTray();
        }

        private void OnEnable()
        {
            if (_runStartUpOnEnable)
            {
                if (!IsInitialized)
                {
                    Initialize();
                }

                if (_isFromSave)
                {
                    if (_savedData == null)
                    {
                        ReadySaveData();
                    }

                    if (_savedData != null)
                    {
                        _colorManager.ChangeColor(_savedData.BodyColor.Vector4ToColor());
                        _colorManager.ChangeColor(_savedData.SecondaryColor.Vector4ToColor(),
                            ColorTargetMode.Secondary);
                        if (_savedData.IsTrayOpen)
                        {
                            Open();
                        }
                    }
                }

                _runStartUpOnEnable = false;
            }
        }

        public override void OnDestroy()
        {
            if (Manager != null)
            {
                Manager.OnPowerStateChanged -= OnPowerStateChanged;
                Manager.OnBreakerStateChanged -= OnBreakerStateChanged;
            }

            base.OnDestroy();
            _isBeingDestroyed = true;
        }

        public override Vector3 GetPosition()
        {
            return transform.position;
        }

        public override void Initialize()
        {
            _storageAmount = gameObject.GetComponentInChildren<Text>();

            _canvas = gameObject.GetComponentInChildren<Canvas>()?.gameObject;

            _slots = new Dictionary<string, DSSSlotController>();
            _meters = new List<GameObject>();
            _percentageBar = GameObjectHelpers.FindGameObject(gameObject, "Preloader").GetComponent<Image>();

            var slotsLocation = GameObjectHelpers.FindGameObject(gameObject, "rack_door_mesh").transform;
            var meters = GameObjectHelpers.FindGameObject(gameObject, "Meters").transform;
            foreach (Transform meter in meters)
            {
                _meters.Add(meter.gameObject);
            }
            
            int i = 1;
            foreach (Transform slot in slotsLocation)
            {
                var meter = _meters[i - 1];
                var slotName = $"Slot {i++}";
                var slotController = slot.gameObject.AddComponent<DSSSlotController>();
                slotController.Initialize(slotName, this, meter);
                _slots.Add(slotName, slotController);
            }

            _tray = GameObjectHelpers.FindGameObject(gameObject, "anim_rack_door");

            if (_colorManager == null)
            {
                _colorManager = gameObject.EnsureComponent<ColorManager>();
                _colorManager.Initialize(gameObject, ModelPrefab.BodyMaterial, ModelPrefab.SecondaryMaterial);
            }

            var canvas = gameObject.GetComponentInChildren<Canvas>();

            InvokeRepeating(nameof(RegisterServers),1f,1f);
            InvokeRepeating(nameof(UpdateScreenState), 1, 1);
            IsInitialized = true;
        }

        public void UpdateStorageCount()
        {
            if (_slots == null) return;
            var storageTotal = 0;
            var storageAmount = 0;
            foreach (KeyValuePair<string, DSSSlotController> controller in _slots)
            {
                if (controller.Value != null && controller.Value.IsOccupied)
                {
                    storageTotal += 48;
                    storageAmount += controller.Value.GetStorageAmount();
                }
            }

            _storageAmount.text = AuxPatchers.AlterraStorageAmountFormat(storageAmount, storageTotal);
            _percentageBar.fillAmount = (float)storageAmount / (float)storageTotal;
        }

        public bool HasSpace(int amount)
        {
            //TODO Deal with filters
            return _slots.Any(x => x.Value.IsOccupied && x.Value.HasSpace(amount));
        }

        public bool AddItemToRack(InventoryItem item)
        {
            try
            {
                var result = TransferHelpers.AddItemToRack(this, item, 1);
                if (!result)
                {
                    PlayerInteractionHelper.GivePlayerItem(item);
                }

            }
            catch (Exception e)
            {
                QuickLogger.Error(e.Message);
                QuickLogger.Error(e.StackTrace);
                QuickLogger.Error(e.Source);
                return false;
            }

            return true;
        }

        public override void OnProtoSerialize(ProtobufSerializer serializer)
        {
            QuickLogger.Debug("In OnProtoSerialize");

            if (!Mod.IsSaving())
            {
                QuickLogger.Info($"Saving {GetPrefabID()}");
                Mod.Save(serializer);
                QuickLogger.Info($"Saved {GetPrefabID()}");
            }
        }

        public override void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            QuickLogger.Debug("In OnProtoDeserialize");

            if (_savedData == null)
            {
                ReadySaveData();
            }

            if (!IsInitialized)
            {
                Initialize();
            }

            _slots.ElementAt(0).Value.RestoreItems(serializer, _savedData.Slot1);
            _slots.ElementAt(1).Value.RestoreItems(serializer, _savedData.Slot2);
            _slots.ElementAt(2).Value.RestoreItems(serializer, _savedData.Slot3);
            _slots.ElementAt(3).Value.RestoreItems(serializer, _savedData.Slot4);
            _slots.ElementAt(4).Value.RestoreItems(serializer, _savedData.Slot5);
            _slots.ElementAt(5).Value.RestoreItems(serializer, _savedData.Slot6);

            _isFromSave = true;
        }

        public override bool CanDeconstruct(out string reason)
        {
            if (_slots != null)
            {
                if (_slots.Any(x => x.Value != null && x.Value.IsOccupied))
                {
                    reason = AuxPatchers.RackNotEmpty();
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        public override void OnConstructedChanged(bool constructed)
        {
            IsConstructed = constructed;
            if (constructed)
            {
                if (isActiveAndEnabled)
                {
                    if (!IsInitialized)
                    {
                        Initialize();
                    }

                    IsInitialized = true;
                }
                else
                {
                    _runStartUpOnEnable = true;
                }
            }
        }

        public void Save(SaveData newSaveData, ProtobufSerializer serializer = null)
        {
            if (!IsInitialized || !IsConstructed) return;

            if (_savedData == null)
            {
                _savedData = new DSSWallServerRackDataEntry();
            }

            _savedData.ID = GetPrefabID();
            _savedData.BodyColor = _colorManager.GetColor().ColorToVector4();
            _savedData.SecondaryColor = _colorManager.GetSecondaryColor().ColorToVector4();
            _savedData.IsTrayOpen = IsOpen;
            _savedData.Slot1 = _slots.ElementAt(0).Value.Save(serializer);
            _savedData.Slot2 = _slots.ElementAt(1).Value.Save(serializer);
            _savedData.Slot3 = _slots.ElementAt(2).Value.Save(serializer);
            _savedData.Slot4 = _slots.ElementAt(3).Value.Save(serializer);
            _savedData.Slot5 = _slots.ElementAt(4).Value.Save(serializer);
            _savedData.Slot6 = _slots.ElementAt(5).Value.Save(serializer);
            newSaveData.DSSWallServerRackDataEntries.Add(_savedData);
            QuickLogger.Debug($"Saving ID {_savedData.ID}", true);
        }

        private void ReadySaveData()
        {
            QuickLogger.Debug("In OnProtoDeserialize");
            _savedData = Mod.GetDSSWallServerRackSaveData(GetPrefabID());
        }

        private void MoveTray()
        {
            if (_tray == null) return;

            if(IsOpen)
            {
                if (_tray.transform.localPosition.z < OpenPos)
                {
                    _tray.transform.Translate(Vector3.forward * _speed * DayNightCycle.main.deltaTime);
                }

                if (_tray.transform.localPosition.z > OpenPos)
                {
                    _tray.transform.localPosition = new Vector3(_tray.transform.localPosition.x, _tray.transform.localPosition.y,OpenPos);
                }
            }
            else
            {
                if (_tray.transform.localPosition.z > ClosePos)
                {
                    _tray.transform.Translate(-Vector3.forward * _speed * DayNightCycle.main.deltaTime);
                }

                if (_tray.transform.localPosition.z < ClosePos)
                {
                    _tray.transform.localPosition = new Vector3(_tray.transform.localPosition.x, _tray.transform.localPosition.y, ClosePos);
                }
            }
        }

        private void Open()
        {
            _targetPos = OpenPos;
        }

        private void Close()
        {
            _targetPos = ClosePos;
        }

        public override bool ChangeBodyColor(Color color, ColorTargetMode mode)
        {
            return _colorManager.ChangeColor(color, mode);
        }

        public void OnHandHover(GUIHand hand)
        {
            if (!IsConstructed || !IsInitialized) return;
            HandReticle main = HandReticle.main;
            main.SetIcon(HandReticle.IconType.Hand);
            main.SetInteractText(IsOpen ? AuxPatchers.CloseWallServerRack() : AuxPatchers.OpenWallServerRack());
        }

        public void OnHandClick(GUIHand hand)
        {
            if (!IsConstructed || !IsInitialized) return;
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public override void TurnOnDevice()
        {
            _isVisible = true;
            if (!_canvas.activeSelf)
            {
                _canvas.SetActive(true);
            }
        }

        public override void TurnOffDevice()
        {
            _isVisible = false;
            if (_canvas.activeSelf)
            {
                _canvas.SetActive(false);
            }
        }

        public IEnumerable<KeyValuePair<string, DSSSlotController>> GetSlots()
        {
            return _slots;
        }

        public int GetFreeSpace()
        {
            int amount = 0;
            foreach (var controller in _slots)
            {
                if (controller.Value != null && controller.Value.IsOccupied)
                {
                    amount += controller.Value.GetFreeSpace();
                }
            }

            return amount;
        }

        public bool ItemAllowed(TechType techType, out ISlotController server)
        {
            server = null;

            foreach (KeyValuePair<string, DSSSlotController> controller in _slots)
            {
                if (controller.Value != null && controller.Value.IsOccupied && !controller.Value.IsFull && controller.Value.IsTechTypeAllowed(techType))
                {
                    server = controller.Value;
                    return true;
                }
            }

            return false;
        }

        public int GetItemCount(TechType techType)
        {
            int amount = 0;
            foreach (KeyValuePair<string, DSSSlotController> controller in _slots)
            {
                if (controller.Value != null && controller.Value.IsOccupied)
                {
                    amount += controller.Value.GetItemCount(techType);
                }
            }

            return amount;
        }

        public bool HasItem(TechType techType)
        {
            foreach (KeyValuePair<string, DSSSlotController> controller in _slots)
            {
                if (controller.Value != null && controller.Value.IsOccupied)
                {
                    if (controller.Value.HasItem(techType))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public Pickupable RemoveItemFromRack(TechType techType)
        {
            foreach (KeyValuePair<string, DSSSlotController> controller in _slots)
            {
                if (controller.Value != null && controller.Value.IsOccupied)
                {
                    if (controller.Value.HasItem(techType))
                    {
                        return controller.Value.RemoveItemFromServer(techType);
                    }
                }
                
            }

            return null;

        }

    }
}
