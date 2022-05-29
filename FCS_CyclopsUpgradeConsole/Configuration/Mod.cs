﻿using System;
using System.IO;

namespace CyclopsUpgradeConsole.Configuration
{
    internal static class Mod
    {
        internal const string BundleName = "cyclopsupgradeconsolebundle";
        internal const string ModTabID = "CUC";
        internal const string ModFriendlyName = "Cyclops Upgrade Console";
        internal const string ModName = "CyclopsUpgradeConsole";
        internal static string CyclopsUpgradeConsoleKitClassID => $"{ModName}_Kit";
        internal static string ModClassName => ModName;
        internal static string ModPrefabName => ModName;
        internal static string ModFolderName => $"FCS_{ModName}";
        
        internal const string ModDescription = "A wall mountable upgrade console to connect a greater number of upgrades to your Cyclops.";


        private static string GetModPath()
        {
            return Path.Combine(GetQModsPath(), ModFolderName);
        }

        private static string GetQModsPath()
        {
            return Path.Combine(Environment.CurrentDirectory, "QMods");
        }

        internal static string GetAssetFolder()
        {
            return Path.Combine(GetModPath(), "Assets");
        }
    }
}