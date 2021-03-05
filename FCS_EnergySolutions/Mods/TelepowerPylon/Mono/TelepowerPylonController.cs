﻿using System;
using System.Collections.Generic;
using System.Linq;
using FCS_AlterraHomeSolutions.Mono.PaintTool;
using FCS_AlterraHub.Buildables;
using FCS_AlterraHub.Extensions;
using FCS_AlterraHub.Helpers;
using FCS_AlterraHub.Interfaces;
using FCS_AlterraHub.Model;
using FCS_AlterraHub.Mono;
using FCS_AlterraHub.Mono.Controllers;
using FCS_AlterraHub.Registration;
using FCS_EnergySolutions.Buildable;
using FCS_EnergySolutions.Configuration;
using FCS_EnergySolutions.Mods.TelepowerPylon.Model;
using FCSCommon.Extensions;
using FCSCommon.Helpers;
using FCSCommon.Utilities;
using SMLHelper.V2.Utility;
using UnityEngine;
using UnityEngine.UI;
using WorldHelpers = FCS_AlterraHub.Helpers.WorldHelpers;

namespace FCS_EnergySolutions.Mods.TelepowerPylon.Mono
{
    internal class TelepowerPylonController : FcsDevice,IFCSSave<SaveData>, IHandTarget, IFCSDumpContainer
    {
        private TelepowerPylonDataEntry _savedData;
        internal bool IsFromSave { get; private set; }
        private bool _runStartUpOnEnable;
        private TelepowerPylonPowerManager _powerManager;
        private GameObject _canvas;
        private NameController _nameController;
        private int _maxConnectionLimit;
        private readonly Dictionary<string, TelepowerPylonController> _currentConnections = new Dictionary<string, TelepowerPylonController>();
        private readonly Dictionary<string, GameObject> _trackedFrequencyItem = new Dictionary<string, GameObject>();
        private GameObject _connectionsGrid;
        private Text _status;
        private TelepowerPylonUpgrade _currentUpgrade = TelepowerPylonUpgrade.MK1;
        private TelepowerPylonMode _mode = TelepowerPylonMode.PUSH;
        private Button _addBTN;
        private bool _attemptedToLoadConnections;
        private Toggle _pullToggle;
        private Toggle _pushToggle;
        private bool _loadingFromSave;
        private FCSMessageBox _messageBox;
        private bool _cursorLockCached;
        private bool _isInRange ;
        private bool _isInUse;
        private GameObject _inputDummy;
        private GameObject _cameraPosition;
        private GameObject _screenBlock;
        private GameObject _playerBody;
        private DumpContainerSimplified _dumpContainer;
        private TechType _mk2UpgradeTechType;
        private TechType _mk3UpgradeTechType;
        private Button _upgradeBTN;
        private ParticleSystem[] _particles;

        private const int DEFAULT_CONNECTIONS_LIMIT = 6;
        private GameObject inputDummy
        {
            get
            {
                if (this._inputDummy == null)
                {
                    this._inputDummy = new GameObject("InputDummy");
                    this._inputDummy.SetActive(false);
                }
                return this._inputDummy;
            }
        }
        public override bool IsOperational => Manager != null && IsConstructed;
        public Action<TelepowerPylonController> OnDestroyCalledAction { get; set; }
        public TelepowerPylonTrigger _telepowerPylonTrigger { get; private set; }

        #region Unity Methods

        private void Start()
        {
            FCSAlterraHubService.PublicAPI.RegisterDevice(this, Mod.TelepowerPylonTabID, Mod.ModName);
            FCS_AlterraHub.Patches.Player_Update_Patch.OnWorldSettled += OnWorldSettled;
            //InvokeRepeating(nameof(MakeConnection),1f,1f);
        }
        
        private void OnEnable()
        {
            if (_runStartUpOnEnable)
            {
                if (!IsInitialized)
                {
                    Initialize();
                }

                if (IsFromSave)
                {
                    if (_savedData == null)
                    {
                        ReadySaveData();
                    }

                    if (!string.IsNullOrEmpty(_savedData.BaseId))
                    {
                        BaseId = _savedData.BaseId;
                    }
                    _colorManager.ChangeColor(_savedData.Body.Vector4ToColor());
                    _colorManager.ChangeColor(_savedData.SecondaryBody.Vector4ToColor(),ColorTargetMode.Secondary);

                    switch (_savedData.PylonMode)
                    {
                        case TelepowerPylonMode.PULL:
                            _pullToggle.isOn = true;
                            break;
                        case TelepowerPylonMode.PUSH:
                            _pushToggle.isOn = true;
                            break;
                    }

                    if (_savedData.Upgrade == TelepowerPylonUpgrade.MK2)
                    {
                        AttemptUpgrade(_mk2UpgradeTechType);
                    }

                    if (_savedData.Upgrade == TelepowerPylonUpgrade.MK3)
                    {
                        AttemptUpgrade(_mk3UpgradeTechType);
                    }
                }

                _runStartUpOnEnable = false;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && _isInRange)
            {
                ExitDisplay();
            }
        }

        public override void OnDestroy()
        {
            OnDestroyCalledAction?.Invoke(this);
            FCS_AlterraHub.Patches.Player_Update_Patch.OnWorldSettled -= OnWorldSettled;
        }

        #endregion

        #region Public Methods

        public override float GetPowerUsage()
        {
            if (!IsConstructed || Manager == null) return 0f;
            return CalculatePowerUsage();
        }

        private float CalculatePowerUsage()
        {
            float amount = 0f;

            if (_mode == TelepowerPylonMode.PUSH)
            {
                foreach (KeyValuePair<string, TelepowerPylonController> connection in _currentConnections)
                {
                    var distance = WorldHelpers.GetDistance(this, connection.Value);
                    amount += distance * QPatch.Configuration.TelepowerPylonPowerUsagePerMeter;
                }
            }
            return amount;
        }

        public override Vector3 GetPosition()
        {
            return transform.position;
        }

        public override void Initialize()
        {
            if (_canvas == null)
            {
                _canvas = GameObjectHelpers.FindGameObject(gameObject,"Canvas");
                _canvas?.SetActive(IsConstructed);
            }

            if (_messageBox == null)
            {
                _messageBox = GameObjectHelpers.FindGameObject(gameObject, "MessageBox").AddComponent<FCSMessageBox>();
            }

            if (_powerManager == null)
            {
                _powerManager = gameObject.EnsureComponent<TelepowerPylonPowerManager>();
                _powerManager.Initialize(this);
            }

            if (_colorManager == null)
            {
                _colorManager = gameObject.AddComponent<ColorManager>();
                _colorManager.Initialize(gameObject, AlterraHub.BasePrimaryCol,AlterraHub.BaseSecondaryCol);
            }

            if (_nameController == null)
            {
                _nameController = gameObject.AddComponent<NameController>();
                _nameController.Initialize("Connect", "Telepower Pylon Search");
                _nameController.SetCurrentName("TP");
                _nameController.OnLabelChanged += OnSearchConnection;
                _nameController.SetMaxChar(20);
            }

            _connectionsGrid = GameObjectHelpers.FindGameObject(gameObject, "Content");

            _status = GameObjectHelpers.FindGameObject(gameObject, "Status")?.GetComponent<Text>();

            _addBTN = GameObjectHelpers.FindGameObject(gameObject, "AddBTN")?.GetComponent<Button>();
            _addBTN.onClick.AddListener(() =>
            {
                if (_currentConnections.Count == _maxConnectionLimit)
                {
                    _messageBox?.Show(AuxPatchers.MaximumConnectionsReached(),FCSMessageButton.OK,null);
                    return;
                }
                _nameController.Show();
            });            
            
            _upgradeBTN = GameObjectHelpers.FindGameObject(gameObject, "UpgradeButton")?.GetComponent<Button>();
            _upgradeBTN.onClick.AddListener(() =>
            {
                ExitDisplay();
                _dumpContainer?.OpenStorage();
            });


            _maxConnectionLimit = DEFAULT_CONNECTIONS_LIMIT;

            MaterialHelpers.ChangeEmissionColor(AlterraHub.BaseEmissiveDecalsController, gameObject, Color.cyan);
            MaterialHelpers.ChangeSpecSettings(AlterraHub.BaseDefaultDecals, AlterraHub.BaseSpec, gameObject, 2.61f, 8f);

            _pushToggle = GameObjectHelpers.FindGameObject(gameObject, "PushToggle")?.GetComponent<Toggle>();
            if (_pushToggle != null)
                
                _pushToggle.onValueChanged.AddListener((value =>
                {
                    if (_powerManager == null || _addBTN == null || _messageBox == null || _pullToggle == null)
                    {
                        return;
                    }
                    
                    if (_powerManager.HasConnections())
                    {
                        _messageBox?.Show(AuxPatchers.RemoveAllTelepowerConnectionsPush(), FCSMessageButton.OK, null);
                        _pullToggle.SetIsOnWithoutNotify(true);
                        return;
                    }

                    if (value)
                    {
                        _mode = TelepowerPylonMode.PUSH;
                    }
                    _addBTN.interactable = false;
                }));

            _pullToggle = GameObjectHelpers.FindGameObject(gameObject, "PullToggle")?.GetComponent<Toggle>();
            if (_pullToggle != null)
                _pullToggle.onValueChanged.AddListener((value =>
                {
                    if (_trackedFrequencyItem == null || _addBTN == null || _messageBox == null || _pushToggle == null)
                    {
                        return;
                    }

                    QuickLogger.Debug($"Has Frequency Item: {_trackedFrequencyItem.Any()}",true);

                    if (_trackedFrequencyItem.Any())
                    {
                        _messageBox?.Show(AuxPatchers.RemoveAllTelepowerConnectionsPull(), FCSMessageButton.OK, null);
                        _pushToggle.SetIsOnWithoutNotify(true);
                        return;
                    }

                    if (value)
                    {
                        _mode = TelepowerPylonMode.PULL;
                    }

                    _addBTN.interactable = true;

                }));

            if (_telepowerPylonTrigger == null)
            {
                _telepowerPylonTrigger = GameObjectHelpers.FindGameObject(gameObject, "Trigger").AddComponent<TelepowerPylonTrigger>();
            }

            _telepowerPylonTrigger.onTriggered += value =>
            {
                _isInRange = true;
                if (value) return;
                _isInRange = false;
                ExitDisplay();
            };

            if (_dumpContainer == null)
            {
                _dumpContainer = gameObject.AddComponent<DumpContainerSimplified>();
                _dumpContainer.Initialize(transform,"Add Upgrade", this,1,1);
            }


            _particles = gameObject.GetComponentsInChildren<ParticleSystem>();

            _mk2UpgradeTechType = "TelepowerMk2Upgrade".ToTechType();
            _mk3UpgradeTechType = "TelepowerMk3Upgrade".ToTechType();

            _cameraPosition = GameObjectHelpers.FindGameObject(gameObject, "CameraPosition");
            _screenBlock = GameObjectHelpers.FindGameObject(gameObject, "MainBlocker");

            _playerBody = Player.main.playerController.gameObject.FindChild("body");
            
            UpdateStatus();

            IPCMessage += message =>
            {
                if (message.Equals("UpdateEffects"))
                {
                    ChangeTrailColor();
                }
            };

            IsInitialized = true;
        }
        
        public override bool ChangeBodyColor(Color color, ColorTargetMode mode)
        {
            return _colorManager.ChangeColor(color, mode);
        }

        public void DeleteFrequencyItemAndDisconnectRelay(string unitID)
        {
            DeleteFrequencyItem(unitID.ToLower());
        }
        
        public TelepowerPylonMode GetCurrentMode()
        {
            return _mode;
        }

        public IPowerInterface GetPowerRelay()
        {
            return _powerManager?.GetPowerRelay();
        }

        public bool IsPlayerInRange()
        {
            return _telepowerPylonTrigger.IsPlayerInRange;
        }

        public override bool AddItemToContainer(InventoryItem item)
        {
            var result = AttemptUpgrade(item.item.GetTechType());
            if(result)
            {
                Destroy(item.item.gameObject);
                return true;
            }

            PlayerInteractionHelper.GivePlayerItem(item);
            return false;
        }

        internal void ChangeTrailColor()
        {
            foreach (ParticleSystem system in _particles)
            {
                var index = QPatch.Configuration.TelepowerPylonTrailBrightness;
                var h = system.trails;
                h.colorOverLifetime = new Color(index, index, index);
            }

        }

        public bool IsAllowedToAdd(TechType techType, bool verbose)
        {
            var result = techType == _mk2UpgradeTechType || techType == _mk3UpgradeTechType;
            if (!result)
            {
                QuickLogger.ModMessage("Only Telepower Pylon Upgrade MK2 and MK3 Allowed.");
            }

            return result;
        }

        public bool IsAllowedToAdd(Pickupable pickupable, bool verbose)
        {
            var result = pickupable.GetTechType() == _mk2UpgradeTechType || pickupable.GetTechType() == _mk3UpgradeTechType;
            if (!result)
            {
                QuickLogger.ModMessage("Only Telepower Pylon Upgrade MK2 and MK3 Allowed.");
            }

            return result;
        }

        #endregion

        #region Private Methods
        private void OnSearchConnection(string text, NameController arg2)
        {
            var unit = FCSAlterraHubService.PublicAPI.FindDevice(text);
            
            var idToLower = text.ToLower();

            if (FindOtherPylonWithConnection(unit))
            {
                _messageBox.Show( $"Cannot add {text} because another pylon has this connection.",FCSMessageButton.OK,null);
                return;
            }


            
            if (unit.Value == null)
            {
                QuickLogger.Message($"Failed to find pylon with unit ID: {text}", true);
                return;
            }

            var pylon = (TelepowerPylonController) unit.Value;

            if (pylon == null)
            {
                QuickLogger.DebugError("Failed to cast object to Pylon",true);
                return;
            }

            if (pylon.GetCurrentMode() != TelepowerPylonMode.PUSH && !_loadingFromSave)
            {
                _messageBox.Show($"Pylon {pylon.UnitID} is not in push mode and cannot be added as a connection.", FCSMessageButton.OK,null);
                return;
            }
            
            if (_currentConnections.ContainsKey(idToLower)) return;

            if (_currentConnections.Count < _maxConnectionLimit) // && WorldHelpers.CheckIfInRange(this, unit.Value, 1000)
            {
                AddConnection(idToLower, unit.Value);
                _powerManager.AddConnection(pylon);
                pylon.AddPullPylon(this);
            }
        }

        private bool FindOtherPylonWithConnection(KeyValuePair<string, FcsDevice> unit)
        {
            var devices = Manager.GetDevices(Mod.TelepowerPylonTabID).ToArray();

            QuickLogger.Debug($"Devices Found: {devices.Length}",true);

            foreach (FcsDevice device in devices)
            {
                var pylon = (TelepowerPylonController) device;

                QuickLogger.Debug($"Pylon {pylon.UnitID}: Device to check: {unit.Key} Result: {pylon.HasConnection(unit.Key)}",true);

                if (pylon.HasConnection(unit.Key))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasConnection(string unitKey)
        {
            return _currentConnections.Any(x => x.Key.ToLower().Equals(unitKey.ToLower()));
        }

        private void UpdateStatus()
        {
            var upgrade = GetCurrentUpgrade();

            _status.text = $"Mark: {upgrade} | Frequency Slots: {_currentConnections.Count}/{_maxConnectionLimit}";
        }

        private TelepowerPylonUpgrade GetCurrentUpgrade()
        {
            return _currentUpgrade;
        }
        
        private void AddConnection(string text, FcsDevice unit)
        {
            var controller = (TelepowerPylonController) unit;
            _currentConnections.Add(text.ToLower(), controller);
            AddConnectionItemToGrid(controller);
            controller.OnDestroyCalledAction += OnDestroyCalled;
            UpdateStatus();
        }

        private void OnDestroyCalled(TelepowerPylonController obj)
        {
            DeleteFrequencyItem(obj.UnitID.ToLower());
        }

        private void AddConnectionItemToGrid(TelepowerPylonController targetController)
        {
            var prefab = Instantiate(ModelPrefab.FrequencyItemPrefab);
            var freqItem = prefab.AddComponent<FrequencyItemController>();
            freqItem.Initialize(targetController,this);
            _trackedFrequencyItem.Add(targetController.UnitID.ToLower(), prefab);
            prefab.transform.SetParent(_connectionsGrid.transform, false);
        }

        private void DeleteFrequencyItem(string id)
        {
            if (_currentConnections.ContainsKey(id))
            {
                _currentConnections.Remove(id);
            }
            else
            {
                QuickLogger.Debug($"Failed to find connection in the list: {id}");
            }

            if (_trackedFrequencyItem.ContainsKey(id))
            {
                Destroy(_trackedFrequencyItem[id]);
                _trackedFrequencyItem.Remove(id);
            }
            
            UpdateStatus();
            _powerManager.RemoveConnection(id);
        }

        private void OnWorldSettled()
        {
            if (_attemptedToLoadConnections) return;
            if (_savedData?.CurrentConnections != null)
            {
                //_loadingFromSave = true;
                foreach (string connection in _savedData.CurrentConnections)
                {
                    OnSearchConnection(connection,null);
                }
                
            }

            _attemptedToLoadConnections = true;
            _loadingFromSave = false;

        }

        private void ExitDisplay()
        {
            _isInUse = false;
            SNCameraRoot.main.transform.localPosition = Vector3.zero;
            SNCameraRoot.main.transform.localRotation = Quaternion.identity;
            ExitLockedMode();
            _playerBody.SetActive(true);
        }

        private void ExitLockedMode()
        {
            InterceptInput(false);
        }

        private void InterceptInput(bool state)
        {
            if (inputDummy.activeSelf == state)
            {
                return;
            }
            if (state)
            {
                _screenBlock.SetActive(false);
                MainCameraControl.main.enabled = false;
                InputHandlerStack.main.Push(inputDummy);
                _cursorLockCached = UWE.Utils.lockCursor;
                UWE.Utils.lockCursor = false;
                return;
            }

            UWE.Utils.lockCursor = _cursorLockCached;
            InputHandlerStack.main.Pop(inputDummy);
            MainCameraControl.main.enabled = true;
            _screenBlock.SetActive(true);
        }
        
        private bool AttemptUpgrade(TechType techType)
        {
            if (techType == _mk2UpgradeTechType && _currentUpgrade == TelepowerPylonUpgrade.MK1)
            {
                _currentUpgrade = TelepowerPylonUpgrade.MK2;
                _maxConnectionLimit = 8;
                ChangeEffectColor(Color.cyan);
                UpdateStatus();
                return true;
            }

            if (techType == _mk3UpgradeTechType && _currentUpgrade != TelepowerPylonUpgrade.MK3)
            {
                _currentUpgrade = TelepowerPylonUpgrade.MK3;
                _maxConnectionLimit = 10;
                ChangeEffectColor(Color.green);
                UpdateStatus();
                return true;
            }

            return false;
        }

        private void ChangeEffectColor(Color color)
        {
            foreach (ParticleSystem system in _particles)
            {
                var main = system.main;
                main.startColor = color;
            }
        }

        #endregion

        #region IConstructable

        public override void OnConstructedChanged(bool constructed)
        {
            IsConstructed = constructed;
            _canvas?.SetActive(constructed);
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

        public override bool CanDeconstruct(out string reason)
        {
            reason = string.Empty;
            if ((_powerManager != null && _powerManager.HasConnections()) || _trackedFrequencyItem.Any())
            {
                reason = AuxPatchers.RemoveAllTelepowerConnections();
                return false;
            }

            if (_currentUpgrade != TelepowerPylonUpgrade.MK1 &&
                !PlayerInteractionHelper.CanPlayerHold(_mk2UpgradeTechType) ||
                !PlayerInteractionHelper.CanPlayerHold(_mk3UpgradeTechType))
            {
                reason = AlterraHub.InventoryFull();
                return false;
            }

            switch (_currentUpgrade)
            {
                case TelepowerPylonUpgrade.MK2:
                    PlayerInteractionHelper.GivePlayerItem(_mk2UpgradeTechType);
                    break;
                case TelepowerPylonUpgrade.MK3:
                    PlayerInteractionHelper.GivePlayerItem(_mk3UpgradeTechType);
                    break;
            }

            return true;
        }

        #endregion

        #region IProtoEventListener

        private void ReadySaveData()
        {
            QuickLogger.Debug("In OnProtoDeserialize");
            _savedData = Mod.GetTelepowerPylonSaveData(GetPrefabID());
        }

        public void Save(SaveData newSaveData, ProtobufSerializer serializer)
        {
            if (!IsInitialized
                || !IsConstructed) return;

            if (_savedData == null)
            {
                _savedData = new TelepowerPylonDataEntry();
            }

            _savedData.Id = GetPrefabID();

            QuickLogger.Debug($"Saving ID {_savedData.Id}", true);
            _savedData.Body = _colorManager.GetColor().ColorToVector4();
            _savedData.SecondaryBody = _colorManager.GetSecondaryColor().ColorToVector4();
            _savedData.BaseId = BaseId;
            _savedData.PylonMode = GetCurrentMode();
            _savedData.CurrentConnections = GetCurrentConnectionIDs().ToList();
            _savedData.Upgrade = _currentUpgrade;
            newSaveData.TelepowerPylonEntries.Add(_savedData);
        }

        private IEnumerable<string> GetCurrentConnectionIDs()
        {
            foreach (KeyValuePair<string, TelepowerPylonController> connection in _currentConnections)
            {
                yield return connection.Key;
            }
        }

        public override void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            QuickLogger.Debug("In OnProtoDeserialize");

            if (_savedData == null)
            {
                ReadySaveData();
            }

            IsFromSave = true;
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

        public void AddPullPylon(TelepowerPylonController pylon)
        {
            AddConnectionItemToGrid(pylon);
            if(_currentConnections.ContainsKey(pylon.UnitID.ToLower())) return;
            _currentConnections.Add(pylon.UnitID.ToLower(),pylon);
        }

        #endregion

        #region IHand Target

        public void OnHandHover(GUIHand hand)
        {
            HandReticle main = HandReticle.main;

            if (_isInRange)
            {
                main.SetInteractText($"Unit ID: {UnitID} Click to use configure Telepower Pylon", $"For more information press {FCS_AlterraHub.QPatch.Configuration.PDAInfoKeyCode} | Power Usage: {CalculatePowerUsage()}");
                main.SetIcon(HandReticle.IconType.Info);
            }
            
            if (Input.GetKeyDown(FCS_AlterraHub.QPatch.Configuration.PDAInfoKeyCode))
            {

            }
        }
        
        public void OnHandClick(GUIHand hand)
        {
            if (_isInRange)
            {
                InterceptInput(true);
                _isInUse = true;
                var hudCameraPos = _cameraPosition.transform.position;
                var hudCameraRot = _cameraPosition.transform.rotation;
                Player.main.SetPosition(new Vector3(hudCameraPos.x, Player.main.transform.position.y, hudCameraPos.z), hudCameraRot);
                _playerBody.SetActive(false);
                SNCameraRoot.main.transform.position = hudCameraPos;
                SNCameraRoot.main.transform.rotation = hudCameraRot;
            }
        }
        
        #endregion
    }

    internal enum TelepowerPylonUpgrade
    {
        MK1 = 1,
        MK2 = 2,
        MK3 = 3
    }
}
