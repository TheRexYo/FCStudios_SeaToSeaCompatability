﻿using System;
using FCS_AlterraHub.API;
using FCS_AlterraHub.Helpers;
using FCS_LifeSupportSolutions.Configuration;
using FCSCommon.Helpers;
using FCSCommon.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FCS_LifeSupportSolutions.Buildable
{
    internal static class ModelPrefab
    {
        private static bool _initialized;
        internal static string BodyMaterial { get; } = $"{Mod.ModName}_COL";
        internal static string SecondaryMaterial { get; } = $"{Mod.ModName}_COL_S";
        internal static string DecalMaterial { get; } = $"{Mod.ModName}_DECALS";
        internal static string DetailsMaterial { get; } = $"{Mod.ModName}_DETAILS";
        internal static string SpecTexture { get; } = $"{Mod.ModName}_S";
        internal static string LUMTexture { get; } = $"{Mod.ModName}_E";
        internal static string NormalTexture { get; } = $"{Mod.ModName}_N";
        internal static string DetailTexture => $"{Mod.ModName}_D";
        public static AssetBundle GlobalBundle { get; set; }
        public static AssetBundle ModBundle { get; set; }
        public static string EmissiveBControllerMaterial { get;} = $"{Mod.ModName}_B_Controller";
        public static string EmissiveControllerMaterial { get; } = $"{Mod.ModName}_E_Controller";
        public static GameObject EnergyPillVendingMachinePrefab { get; set; }
        public static GameObject RedEnergyPillPrefab { get; set; }

        public static GameObject BlueEnergyPillPrefab { get; set; }

        public static GameObject GreenEnergyPillPrefab { get; set; }
        public static GameObject PillHudPrefab { get; set; }
        public static GameObject MiniMedBayPrefab { get; set; }
        public static GameObject BaseUtilityUnitPrefab { get; set; }
        public static GameObject BaseOxygenTankPrefab { get; set; }


        internal static void Initialize()
        {
            if (GlobalBundle == null)
            {
                GlobalBundle = FCSAssetBundlesService.PublicAPI.GetAssetBundleByName(FCSAssetBundlesService.PublicAPI.GlobalBundleName);
            }

            if (ModBundle == null)
            {
                ModBundle = FCSAssetBundlesService.PublicAPI.GetAssetBundleByName(Mod.ModBundleName, Mod.GetModDirectory());
            }

            EnergyPillVendingMachinePrefab = GetPrefab(Mod.EnergyPillVendingMachinePrefabName);
            BaseOxygenTankPrefab = GetPrefab(Mod.BaseOxygenTankPrefabName);
            RedEnergyPillPrefab = GetPrefab("RedPill");
            GreenEnergyPillPrefab = GetPrefab("GreenPill");
            BlueEnergyPillPrefab = GetPrefab("BluePill");
            PillHudPrefab = GetPrefab("PillHUD");
            MiniMedBayPrefab = GetPrefab(Mod.MiniMedBayPrefabName);
            BaseUtilityUnitPrefab = GetPrefab(Mod.BaseUtilityUnityPrefabName);
        }


        internal static GameObject GetPrefab(string prefabName)
        {
            try
            {
                QuickLogger.Debug($"Getting Prefab: {prefabName}");
                if (!LoadAsset(prefabName, ModBundle, out var prefabGo)) return null;
                return prefabGo;
            }
            catch (Exception e)
            {
                QuickLogger.Error(e.Message);
                return null;
            }
        }

        private static bool LoadAsset(string prefabName, AssetBundle assetBundle, out GameObject go, bool applyShaders = true)
        {
            QuickLogger.Debug("Loading Asset");
            //We have found the asset bundle and now we are going to continue by looking for the model.
            GameObject prefab = assetBundle.LoadAsset<GameObject>(prefabName);
            QuickLogger.Debug($"Loaded Prefab {prefabName}");

            //If the prefab isn't null lets add the shader to the materials
            if (prefab != null)
            {
                if (applyShaders)
                {
                    //Lets apply the material shader
                    ApplyShaders(prefab, assetBundle);
                    QuickLogger.Debug($"Applied shaderes to prefab {prefabName}");
                }

                go = prefab;
                QuickLogger.Debug($"{prefabName} Prefab Found!");
                return true;
            }

            QuickLogger.Error($"{prefabName} Prefab Not Found!");

            go = null;
            return false;
        }

        /// <summary>
        /// Applies the shader to the materials of the reactor
        /// </summary>
        /// <param name="prefab">The prefab to apply shaders.</param>
        internal static void ApplyShaders(GameObject prefab, AssetBundle bundle = null)
        {
            #region BaseColor
            MaterialHelpers.ApplySpecShader(BodyMaterial, SpecTexture, prefab, 1, 3f, bundle);
            MaterialHelpers.ApplyEmissionShader(DecalMaterial, LUMTexture, prefab, bundle, Color.white);
            MaterialHelpers.ApplyEmissionShader(DetailsMaterial, LUMTexture, prefab, bundle, Color.white);
            MaterialHelpers.ApplyEmissionShader(EmissiveBControllerMaterial, LUMTexture, prefab, bundle, Color.white);
            MaterialHelpers.ApplyEmissionShader(EmissiveControllerMaterial, LUMTexture, prefab, bundle, Color.white);
            MaterialHelpers.ApplyAlphaShader(DecalMaterial, prefab);
            MaterialHelpers.ApplyAlphaShader(DetailsMaterial, prefab);
            MaterialHelpers.ApplyNormalShader(BodyMaterial,NormalTexture,prefab,bundle);
            #endregion
        }
    }
}
