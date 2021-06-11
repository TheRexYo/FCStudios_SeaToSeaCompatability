﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FCS_AlterraHub.API;
using FCS_AlterraHub.Configuration;
using FCS_AlterraHub.Enumerators;
using FCS_AlterraHub.Extensions;
using FCS_AlterraHub.Mono;
using FCS_AlterraHub.Structs;
using FCS_AlterraHub.Systems;
using FCSCommon.Utilities;
using Oculus.Newtonsoft.Json;
using QModManager.API;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;
using UnityEngine;

namespace FCS_AlterraHub.Registration
{
    public interface IFCSAlterraHubService
    {
        bool IsRegisteringEncyclopedia { get; set; }
        CardSystem AccountSystem { get; }

        void RegisterDevice(FcsDevice device, string tabID, string packageId);
        void RemoveDeviceFromGlobal(string unitID);
        void CreateStoreEntry(TechType techType, TechType receiveTechType, decimal cost, StoreCategory category);
        Dictionary<TechType, FCSStoreEntry> GetRegisteredKits();
        TechType GetRegisteredKit(TechType techType);
        Dictionary<string, FcsDevice> GetRegisteredDevices();
        bool IsModPatched(string mod);
        void RegisterPatchedMod(string mod);
        int GetRegisterModCount(string tabID);
        bool IsTechTypeRegistered(TechType techType);
        void UnRegisterDevice(FcsDevice device);
        void RegisterTechLight(TechLight techLight);
        bool ChargeAccount(decimal amount);
        bool IsCreditAvailable(decimal amount);
        KeyValuePair<string, FcsDevice> FindDevice(string unitID);
        KeyValuePair<string, FcsDevice> FindDeviceWithPreFabID(string prefabID);
        void AddBuiltTech(TechType techType);
        int GetTechBuiltCount(TechType techType);
        void RemoveBuiltTech(TechType techType);
        TechType GetDeviceTechType(string deviceId);
        Dictionary<string, FcsDevice> GetRegisteredDevicesOfId(string id);
        void RegisterBase(BaseManager manager);
        void UnRegisterBase(BaseManager manager);
        void RegisterEncyclopediaEntry(string modID);
        void RegisterModPack(string modID, Assembly assembly);
        void CreateStoreEntry(TechType techType, TechType receiveTechType,int returnAmount, decimal cost, StoreCategory category,bool forceUnlocked = false);
        FCSGamePlaySettings GetGamePlaySettings();
    }

    internal interface IFCSAlterraHubServiceInternal
    {
        List<Dictionary<string, List<EncyclopediaEntryData>>> EncyclopediaEntries { get; set; }
        void RegisterEncyclopediaEntries(List<Dictionary<string, List<EncyclopediaEntryData>>> encyclopediaEntries);

    }

    public class FCSAlterraHubService : IFCSAlterraHubService, IFCSAlterraHubServiceInternal
    {
        private static readonly FCSAlterraHubService singleton = new FCSAlterraHubService();

        public static List<KnownDevice> knownDevices = new List<KnownDevice>();
        private static readonly Dictionary<string, FcsDevice> GlobalDevices = new Dictionary<string, FcsDevice>();
        private static Dictionary<TechType, FCSStoreEntry> _storeItems = new Dictionary<TechType, FCSStoreEntry>();
        private static HashSet<TechType> _registeredTechTypes = new HashSet<TechType>();
        private List<string> _patchedMods = new List<string>();
        private Dictionary<TechType, int> _globallyBuiltTech = new Dictionary<TechType, int>();
        private Dictionary<string, string> _registeredModPacks = new Dictionary<string, string>();
        public static IFCSAlterraHubService PublicAPI => singleton;
        internal static IFCSAlterraHubServiceInternal InternalAPI => singleton;

        public List<Dictionary<string, List<EncyclopediaEntryData>>> EncyclopediaEntries { get; set; } =
            new List<Dictionary<string, List<EncyclopediaEntryData>>>();
        private FCSAlterraHubService()
        {
            Mod.OnDevicesDataLoaded += OnDataLoaded;
        }

        private void OnDataLoaded(List<KnownDevice> obj)
        {
            if (obj != null)
                knownDevices = obj;

            //QModServices.Main.AddCriticalMessage($"Alterra Service Loaded: {knownDevices.Count} Devices");
        }

        public CardSystem AccountSystem => CardSystem.main;

        public void RegisterDevice(FcsDevice device, string tabID, string packageId)
        {
            var prefabID = device.GetPrefabID();

            QuickLogger.Debug($"Attempting to Register: {prefabID} : Constructed {device.IsConstructed}");

            if (!device.BypassRegisterCheck)
            {
                if (string.IsNullOrWhiteSpace(prefabID) || !device.IsConstructed) return;
            }

            if (!knownDevices.Any(x => x.PrefabID.Equals(prefabID)))
            {
                QuickLogger.Debug($"Creating new ID: Known Count{knownDevices.Count}");
                var unitID = GenerateNewID(tabID, prefabID);
                device.TabID = tabID;
                device.UnitID = unitID;
                device.PackageId = packageId;
                AddToGlobalDevices(device, unitID);
                Mod.SaveDevices(knownDevices);
            }
            else
            {
                device.TabID = tabID;
                device.PackageId = packageId;
                device.UnitID = knownDevices.FirstOrDefault(x => x.PrefabID.Equals(prefabID)).ToString();
                AddToGlobalDevices(device, device.UnitID);
            }
            
            QuickLogger.Debug($"Registering Device: {device.UnitID}");

            _registeredTechTypes.Add(device.GetTechType());

            BaseManagerSetup(device);
        }

        private string GenerateNewID(string tabId, string prefabID)
        {
            var newEntry = new KnownDevice();
            var id = 0;
            if (knownDevices.Any())
            {
                id = knownDevices.Where(x => x.DeviceTabId.Equals(tabId)).DefaultIfEmpty().Max(x => x.ID);
                id++;
            }

            newEntry.ID = id;
            newEntry.PrefabID = prefabID;
            newEntry.DeviceTabId = tabId;
            knownDevices.Add(newEntry);
            return newEntry.ToString();
        }

        private static void AddToGlobalDevices(FcsDevice device, string unitID)
        {
            if (!GlobalDevices.ContainsKey(unitID))
            {
                GlobalDevices.Add(unitID, device);
            }
        }

        private static void BaseManagerSetup(FcsDevice device)
        {
            if (string.IsNullOrEmpty(device.BaseId))
            {
                var subRoot = device.gameObject.GetComponentInParent<SubRoot>();
                if (subRoot != null)
                {
                    device.BaseId = subRoot.gameObject.GetComponent<PrefabIdentifier>().Id;
                }
                else
                {
                    QuickLogger.Error($"Failed to Setup the Base Manager for device {device.UnitID} with prefab id {device.GetPrefabID()}");
                    return;
                }
            }

            var manager = BaseManager.FindManager(device.BaseId);
            if (manager != null)
            {
                device.Manager = manager;
                manager.RegisterDevice(device);
            }
        }

        private static void BaseManagerSetup(TechLight device)
        {
            string baseId = string.Empty;
            var subRoot = device.gameObject.GetComponentInParent<SubRoot>();
            if (subRoot != null)
            {
                baseId = subRoot.gameObject.GetComponent<PrefabIdentifier>().Id;
            }
            else
            {
                QuickLogger.Debug(
                    $"Failed to Setup the Base Manager for TechLight {device.gameObject.GetComponent<PrefabIdentifier>().Id}");
                return;
            }

            var manager = BaseManager.FindManager(baseId);
            if (manager != null)
            {
                manager.RegisterDevice(device);
            }
        }

        public void RegisterNewCard(string prefabID)
        {
            CardSystem.main.GenerateNewCard(prefabID);
        }

        public void UnRegisterCard(string cardNumber)
        {
            CardSystem.main.DeleteCard(cardNumber);
        }

        public void RemoveDeviceFromGlobal(string unitID)
        {
            if (string.IsNullOrEmpty(unitID)) return;
            GlobalDevices.Remove(unitID);
        }

        public void CreateStoreEntry(TechType techType, TechType recieveTechType, decimal cost, StoreCategory category)
        {
            CreateStoreEntry(techType,recieveTechType,1,cost,category);   
        }

        public void RegisterModPack(string modID, Assembly assembly)
        {
            if (string.IsNullOrWhiteSpace(modID))
            {
                QuickLogger.Error($"Failed to register mod {Assembly.GetExecutingAssembly().FullName} because mod is empty");
                return;
            }

            if (assembly == null)
            {
                QuickLogger.Error($"Failed to register mod {modID} because assembly is null");
                return;
            }
            QuickLogger.Info($"Started patching {modID} Version: {QuickLogger.GetAssemblyVersion(assembly)}");
            
            var path = Path.GetDirectoryName(assembly.Location);
            
            if (!_registeredModPacks.ContainsKey(modID))
            {
                _registeredModPacks.Add(modID,path);
                QuickLogger.Debug($"Registered mod pack {modID} with at path {path}");
            }
            else
            {
                QuickLogger.Warning($"Mod Pack: {modID} has already been registered! Ignoring replacing entry with new data.");
                _registeredModPacks[modID] = path;
            }
        }

        public void CreateStoreEntry(TechType techType, TechType receiveTechType, int returnAmount, decimal cost, StoreCategory category,bool forceUnlocked = false)
        {
            if (!_storeItems.ContainsKey(techType))
            {
                _storeItems.Add(techType,
                    new FCSStoreEntry
                    {
                        TechType = techType,
                        ReceiveTechType = receiveTechType,
                        Cost = cost,
                        ReturnAmount = returnAmount,
                        StoreCategory = category,
                        ForcedUnlock = forceUnlocked
                    });
            }
        }

        public FCSGamePlaySettings GetGamePlaySettings()
        {
            return Mod.GamePlaySettings;
        }

        public Dictionary<TechType, FCSStoreEntry> GetRegisteredKits()
        {
            return _storeItems;
        }

        public TechType GetRegisteredKit(TechType techType)
        {
            return _storeItems.Any(x => x.Value.ReceiveTechType == techType)
                ? _storeItems.FirstOrDefault(x => x.Value.ReceiveTechType == techType).Key
                : TechType.None;
        }

        public Dictionary<string, FcsDevice> GetRegisteredDevices()
        {
            return GlobalDevices;
        }

        public bool IsModPatched(string mod)
        {
            return _patchedMods.Contains(mod);
        }

        public void RegisterPatchedMod(string mod)
        {
            if (!_patchedMods.Contains(mod))
            {
                _patchedMods.Add(mod);
            }
        }

        public int GetRegisterModCount(string tabID)
        {
            return knownDevices.Count(x => x.DeviceTabId.Equals(tabID));
        }

        public bool IsTechTypeRegistered(TechType techType)
        {
            return _registeredTechTypes.Contains(techType);
        }
        
        public void RegisterEncyclopediaEntries(List<Dictionary<string, List<EncyclopediaEntryData>>> encyclopediaEntries)
        {
            
            LanguageHandler.SetLanguageLine($"EncyPath_fcs", "Field Creators Studios");

            foreach (Dictionary<string, List<EncyclopediaEntryData>> entry in encyclopediaEntries)
            {
                foreach (KeyValuePair<string, List<EncyclopediaEntryData>> data in entry)
                {
                    foreach (EncyclopediaEntryData entryData in data.Value)
                    {
                        LanguageHandler.SetLanguageLine($"Ency_{data.Key}", entryData.Title);
                        LanguageHandler.SetLanguageLine($"EncyDesc_{data.Key}", entryData.Body);
                        LanguageHandler.SetLanguageLine($"EncyPath_{entryData.Path}", entryData.TabTitle);

                        FMODAsset fModAsset = null;

                        if (!string.IsNullOrWhiteSpace(entryData.AudioName))
                        {
                            if (_registeredModPacks.TryGetValue(entryData.ModPackID, out string directory))
                            {
                                var audioPath = Path.Combine(directory, "Audio", $"{entryData.AudioName}.mp3");
                                if (File.Exists(audioPath))
                                {
                                    fModAsset = ScriptableObject.CreateInstance<FMODAsset>();
                                    fModAsset.id = entryData.AudioName;
                                    fModAsset.name = "";
                                    fModAsset.path = audioPath;

                                    CustomSoundHandler.Main.RegisterCustomSound(fModAsset.id, fModAsset.path);
                                }
                                else
                                {
                                    QuickLogger.Error($"Failed to located audio path: {audioPath} for entry {entryData.Path}");
                                }
                            }
                        }

                        PDAEncyclopedia.mapping.Add(data.Key, new PDAEncyclopedia.EntryData
                        {
                            audio = fModAsset,
                            image = FCSAssetBundlesService.PublicAPI.GetEncyclopediaTexture2D(entryData.ImageName),
                            key = data.Key,
                            nodes = PDAEncyclopedia.ParsePath(entryData.Path),
                            path = entryData.Path,
                            timeCapsule = false,
                            unlocked = entryData.Unlocked
                        });

                        if (entryData.Unlocked)
                        {
                            PDAEncyclopedia.Add(data.Key, false);
                        }
                        else
                        {
                            if (TechTypeExtensions.FromString(entryData.UnlockedBy, out TechType techType, true))
                            {
                                TechType bluePrintTechType = TechType.None;
                                if (!string.IsNullOrWhiteSpace(entryData.Blueprint))
                                {
                                    TechTypeExtensions.FromString(entryData.Blueprint, out bluePrintTechType, true);
                                }

                                PDAHandler.AddCustomScannerEntry(new PDAScanner.EntryData()
                                {
                                    encyclopedia = data.Key,
                                    locked = true,
                                    key = techType,
                                    blueprint = bluePrintTechType,
                                    destroyAfterScan = entryData.DestroyFragmentAfterScan,
                                    isFragment = entryData.HasFragment,
                                    totalFragments = entryData.TotalFragmentsToUnlock,
                                    scanTime = entryData.ScanTime
                                });
                            }
                            else
                            {
                                QuickLogger.Error($"Failed to Parse TechType to unlock Ency entry: {data.Key}", true);
                            }
                        }
                    }
                }
            }
        }

        public bool IsRegisteringEncyclopedia { get; set; }

        public void RegisterEncyclopediaEntry(string modID)
        {
            try
            {
                if (_registeredModPacks.TryGetValue(modID, out string path))
                {
                    var encyclopediaJson = Path.Combine(path, "Assets", "Encyclopedia", "EncyclopediaEntries.json");

                    if (File.Exists(encyclopediaJson))
                    {
                        var jsonData = JsonConvert.DeserializeObject<Dictionary<string, List<EncyclopediaEntryData>>>(File.ReadAllText(encyclopediaJson));

                        QuickLogger.Debug(jsonData.ToString());

                        InternalAPI.EncyclopediaEntries.Insert(0,jsonData);
                    }
                    else
                    {
                        QuickLogger.Warning($"No encyclopedia file found for mod pack {modID}");
                    }
                }
                else
                {
                    QuickLogger.Error($"Failed to find {modID}! Registering Encyclopedia Data failed");
                }
            }
            catch (Exception e)
            {
                QuickLogger.Error(e.Message);
                QuickLogger.Error(e.StackTrace);
            }
        }


        public void UnRegisterDevice(FcsDevice device)
        {
            foreach (KnownDevice knownDevice in knownDevices)
            {
                if (!string.IsNullOrEmpty(knownDevice.PrefabID) && knownDevice.PrefabID.Equals(device.GetPrefabID()))
                {
                    knownDevices.Remove(knownDevice);
                    _registeredTechTypes.Remove(device.GetTechType());
                    RemoveDeviceFromGlobal(device.UnitID);
                    break;
                }
            }
        }

        public Dictionary<string, FcsDevice> GetRegisteredDevicesOfId(string id)
        {
            return GlobalDevices.Where(x => x.Value.TabID.Equals(id, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(x => x.Key, x => x.Value);
        }

        public void RegisterTechLight(TechLight techLight)
        {
            BaseManagerSetup(techLight);
        }

        public bool ChargeAccount(decimal amount)
        {
            if (CardSystem.main.HasBeenRegistered())
            {
                CardSystem.main.RemoveFinances(amount);
                return true;
            }

            return false;
        }

        public bool IsCreditAvailable(decimal amount)
        {
            return CardSystem.main.HasEnough(amount);
        }

        public KeyValuePair<string, FcsDevice> FindDevice(string unitID)
        {
            return GlobalDevices.FirstOrDefault(x => x.Key.Equals(unitID, StringComparison.OrdinalIgnoreCase));
        }

        public KeyValuePair<string, FcsDevice> FindDeviceWithPreFabID(string prefabID)
        {
            return GlobalDevices.FirstOrDefault(x => x.Value.GetPrefabID().Equals(prefabID, StringComparison.OrdinalIgnoreCase));
        }

        public void AddBuiltTech(TechType techType)
        {
            if (_globallyBuiltTech.ContainsKey(techType))
            {
                _globallyBuiltTech[techType] += 1;
            }
            else
            {
                _globallyBuiltTech.Add(techType, 1);
            }
        }

        public int GetTechBuiltCount(TechType techType)
        {
            if (_globallyBuiltTech.ContainsKey(techType))
            {
                return _globallyBuiltTech[techType];
            }

            return 0;
        }

        public void RemoveBuiltTech(TechType techType)
        {
            if (_globallyBuiltTech.ContainsKey(techType))
            {
                _globallyBuiltTech[techType] -= 1;
                if (_globallyBuiltTech[techType] <= 0)
                {
                    _globallyBuiltTech.Remove(techType);
                }
            }
        }

        public TechType GetDeviceTechType(string deviceId)
        {
            return FindDevice(deviceId).Value.GetTechType();
        }

        public void RegisterBase(BaseManager manager)
        {
            QuickLogger.Debug($"Attempting to Register: {manager.BaseID}");

            if (!knownDevices.Any(x => x.PrefabID.Equals(manager.BaseID)))
            {
                manager.BaseFriendlyID = GenerateNewID("BS", manager.BaseID);
                Mod.SaveDevices(knownDevices);
            }
            else
            {
                manager.BaseFriendlyID = knownDevices.FirstOrDefault(x => x.PrefabID.Equals(manager.BaseID)).ToString();
            }

            QuickLogger.Debug($"Registering Device: {manager.BaseID}");
        }

        public void UnRegisterBase(BaseManager manager)
        {
            foreach (KnownDevice knownDevice in knownDevices)
            {
                if (!string.IsNullOrEmpty(knownDevice.PrefabID) && knownDevice.PrefabID.Equals(manager.BaseID))
                {
                    knownDevices.Remove(knownDevice);
                    break;
                }
            }
        }
    }
}