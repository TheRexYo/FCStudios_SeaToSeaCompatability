﻿using FCSCommon.Exceptions;
using FCSCommon.Utilities;
using FCSTechFabricator.Mono;
using Oculus.Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using FCSTechFabricator.Helpers;
using FCSTechFabricator.Models;
using QModManager.API.ModLoading;
using UnityEngine;

namespace FCSTechFabricator
{
    [QModCore]
    public class QPatch
    {
        #region Private Members
        private static bool _continue = true;
        #endregion

        #region Public Properties
        public static GameObject ColorItem { get; internal set; }

        public static GameObject SeaAlienGasTank { get; internal set; }

        public static GameObject SeaGasTank { get; internal set; }

        public static GameObject BatteryModule { get; internal set; }

        public static GameObject FocusModule { get; internal set; }

        public static GameObject SolarModule { get; internal set; }

        public static GameObject Kit { get; internal set; }

        public static AssetBundle Bundle { get; internal set; }
        public static GameObject Freon { get; internal set; }
        #endregion

        #region Internal Properties
        internal static Configuration ModConfiguration { get; set; }
        internal static FCSTechFabricatorBuildable Fabricator;
        #endregion

        #region Patch Methods

        [QModPrePatch]
        public static void Initialization()
        {
            try
            {
                if (PatchHelpers.GetPrefabs())
                {
                    QuickLogger.Debug("Initializing TechFabricator PrePatch");
                    // == Load Configuration == //
                    string configJson = File.ReadAllText(Mod.ConfigurationFile().Trim());

                    //LoadData
                    ModConfiguration = JsonConvert.DeserializeObject<Configuration>(configJson);
                }
                else
                {
                    _continue = false;
                }

            }
            catch (Exception ex)
            {
                QuickLogger.Error(ex);
            }
        }

        
        [QModPatch]
        public static void Patch()
        {
            var assembly = Assembly.GetExecutingAssembly();
            QuickLogger.Info("Started patching. Version: " + QuickLogger.GetAssemblyVersion(assembly));

#if DEBUG
            QuickLogger.DebugLogsEnabled = true;
            QuickLogger.Debug("Debug logs enabled");
#endif

            try
            {
                if (_continue)
                {
                    Fabricator = new FCSTechFabricatorBuildable();
                    Fabricator.Patch();
                    QuickLogger.Info("Finished patching");
                }
                else
                {
                    throw new PatchTerminatedException();
                }
            }
            catch (Exception ex)
            {
                QuickLogger.Error(ex);
            }
        }

        [QModPostPatch]
        public static void PostInitializationMethod()
        {
            try
            {
                PatchHelpers.RegisterItems();
            }
            catch (Exception ex)
            {
                QuickLogger.Error(ex);
            }
        }

        #endregion

    }
}