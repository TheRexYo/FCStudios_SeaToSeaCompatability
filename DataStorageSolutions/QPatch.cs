﻿using System;
using System.IO;
using System.Reflection;
using DataStorageSolutions.Buildables;
using DataStorageSolutions.Buildables.Antenna;
using DataStorageSolutions.Buildables.FilterMachine;
using DataStorageSolutions.Buildables.Racks;
using DataStorageSolutions.Buildables.Terminal;
using DataStorageSolutions.Configuration;
using DataStorageSolutions.Craftables;
using DataStorageSolutions.Patches;
using FCSCommon.Utilities;
using FCSTechFabricator;
using FCSTechFabricator.Components;
using FCSTechFabricator.Craftables;
using HarmonyLib;
using QModManager.API;
using QModManager.API.ModLoading;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;
using UnityEngine;

namespace DataStorageSolutions
{
    [QModCore]
    public class QPatch
    {
        internal static ConfigFile Configuration { get; private set; }
        internal static AssetBundle GlobalBundle { get; set; }
        internal static object EasyCraftSettingsInstance { get; set; }
        internal static FieldInfo UseStorage { get; set; }
        internal static bool IsDockedVehicleStorageAccessInstalled { get; set; }

        private static void AddTechFabricatorItems()
        {
            var icon = ImageUtils.LoadSpriteFromFile(Path.Combine(Mod.GetAssetFolder(), $"{Mod.ModName}.png"));
            var craftingTab = new CraftingTab(Mod.DSSTabID, Mod.ModFriendlyName, icon);

            //Floor Mounted Rack Has Been Cut
            //var floorMountedRack = new FCSKit(Mod.FloorMountedRackKitClassID, Mod.FloorMountedRackFriendlyName, craftingTab , Mod.FloorMountedRackIngredients);
            //floorMountedRack.Patch(FcTechFabricatorService.PublicAPI, FcAssetBundlesService.PublicAPI);

            var wallMountedRack = new FCSKit(Mod.WallMountedRackKitClassID, Mod.WallMountedRackFriendlyName, craftingTab, Mod.WallMountedRackIngredients);
            wallMountedRack.Patch(FcTechFabricatorService.PublicAPI, FcAssetBundlesService.PublicAPI);

            var terminal = new FCSKit(Mod.TerminalKitClassID, Mod.TerminalFriendlyName, craftingTab, Mod.TerminalIngredients);
            terminal.Patch(FcTechFabricatorService.PublicAPI, FcAssetBundlesService.PublicAPI);

            var antenna = new FCSKit(Mod.AntennaKitClassID, Mod.AntennaFriendlyName, craftingTab, Mod.AntennaIngredients);
            antenna.Patch(FcTechFabricatorService.PublicAPI, FcAssetBundlesService.PublicAPI);

            var serverFormattingStation = new FCSKit(Mod.ServerFormattingStationKitClassID, Mod.ServerFormattingStationFriendlyName, craftingTab, Mod.ServerFormattingStationIngredients);
            serverFormattingStation.Patch(FcTechFabricatorService.PublicAPI, FcAssetBundlesService.PublicAPI);

            Server = new ServerCraftable(Mod.ServerClassID, Mod.ServerFriendlyName, Mod.ServerDescription, craftingTab);
            Server.Patch(FcTechFabricatorService.PublicAPI, FcAssetBundlesService.PublicAPI);
        }

        internal static ServerCraftable Server { get; set; }
        
        [QModPatch]
        public static void Patch()
        {
            try
            {
                QuickLogger.Info($"Started patching. Version: {QuickLogger.GetAssemblyVersion(Assembly.GetExecutingAssembly())}");

#if DEBUG
                QuickLogger.DebugLogsEnabled = true;
                QuickLogger.Debug("Debug logs enabled");
#endif

                GlobalBundle = FcAssetBundlesService.PublicAPI.GetAssetBundleByName(FcAssetBundlesService.PublicAPI.GlobalBundleName);

                Configuration = Mod.LoadConfiguration();

                OptionsPanelHandler.RegisterModOptions(new Options());

                AuxPatchers.AdditionalPatching();

                DSSModelPrefab.GetPrefabs();

                AddTechFabricatorItems();

                var antenna = new AntennaBuildable();
                antenna.Patch();

                //Floor Mounted Rack Has Been Cut
                //var floorMountedRack = new FloorMountedRackBuildable();
                //floorMountedRack.Patch();

                var wallMountedRack = new WallMountedRackBuildable();
                wallMountedRack.Patch();

                var terminal = new DSSTerminalC48Buildable();
                terminal.Patch();

                var serverFormattingStation = new ServerFormattingStationBuildable();
                serverFormattingStation.Patch();

                var harmony = new Harmony("com.datastoragesolutions.fstudios");

                harmony.PatchAll(Assembly.GetExecutingAssembly());

                PatchEasyCraft(harmony);

                IsDockedVehicleStorageAccessInstalled = QModServices.Main.ModPresent("DockedVehicleStorageAccess");
                
                QuickLogger.Info("Finished patching");
            }
            catch (Exception ex)
            {
                QuickLogger.Error(ex);
            }
        }
        
        private static void PatchEasyCraft(Harmony harmony)
        {
            var isEasyCraftInstalled = QModServices.Main.ModPresent("EasyCraft");

            if (isEasyCraftInstalled)
            {
                QuickLogger.Debug("EasyCraft is installed");

                var easyCraftClosestItemContainersType = Type.GetType("EasyCraft.ClosestItemContainers, EasyCraft");
                var easyCraftMainType = Type.GetType("EasyCraft.Main, EasyCraft");
                var easyCraftSettingsType = Type.GetType("EasyCraft.Settings, EasyCraft");

                if (easyCraftMainType != null)
                {
                    QuickLogger.Debug("Got EasyCraft Main Type");
                    EasyCraftSettingsInstance = easyCraftMainType
                        .GetField("settings", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                    if (EasyCraftSettingsInstance != null)
                    {
                        QuickLogger.Debug("Got EasyCraft Settings Field Info");
                        if (easyCraftSettingsType != null)
                        {
                            QuickLogger.Debug("Got EasyCraft Settings type");

                            QuickLogger.Debug($"Got EasyCraft Settings type: {easyCraftSettingsType.Name}");
                            var autoCraft = easyCraftSettingsType.GetField("autoCraft").GetValue(EasyCraftSettingsInstance);
                            UseStorage = easyCraftSettingsType.GetField("useStorage");
                            var returnSurplus = easyCraftSettingsType.GetField("returnSurplus")
                                .GetValue(EasyCraftSettingsInstance);
                        }
                    }
                }


                if (easyCraftClosestItemContainersType != null)
                {
                    QuickLogger.Debug("Got EasyCraft Type");
                    var destroyItemMethodInfo = easyCraftClosestItemContainersType.GetMethod("DestroyItem");
                    var getPickupCountMethodInfo = easyCraftClosestItemContainersType.GetMethod("GetPickupCount");

                    if (destroyItemMethodInfo != null)
                    {
                        QuickLogger.Debug("Got EasyCraft DestroyItem Method");
                        var postfix = typeof(EasyCraft_Patch).GetMethod("DestroyItem");
                        harmony.Patch(destroyItemMethodInfo, null, new HarmonyMethod(postfix));
                    }

                    if (getPickupCountMethodInfo != null)
                    {
                        QuickLogger.Debug("Got EasyCraft GetPickupCount Method");
                        var postfix = typeof(EasyCraft_Patch).GetMethod("GetPickupCount");
                        harmony.Patch(getPickupCountMethodInfo, null, new HarmonyMethod(postfix));
                    }
                }
                else
                {
                    QuickLogger.Error("Failed to get EasyCraft Type");
                }
            }
            else
            {
                QuickLogger.Debug("EasyCraft  not installed");
            }
        }
    }
}