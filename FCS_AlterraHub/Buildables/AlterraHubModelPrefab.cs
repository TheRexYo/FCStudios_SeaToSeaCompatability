﻿using System;
using System.Collections.Generic;
using FCS_AlterraHub.API;
using FCS_AlterraHub.Configuration;
using FCSCommon.Helpers;
using FCSCommon.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FCS_AlterraHub.Buildables
{
    public partial class AlterraHub
    {
        private static bool _initialized;
        private static Dictionary<string,Material> _v2Materials = new Dictionary<string, Material>();
        private static bool _v2MaterialsLoaded;
        internal static GameObject ColorItemPrefab { get; set; }
        internal static GameObject ItemPrefab { get; set; }

        /// <summary>
        /// Material for fcs01_BP
        /// </summary>
        public const string BasePrimaryCol = "fcs01_BP";
        /// <summary>
        /// Material for fcs01_BS
        /// </summary>
        public const string BaseSecondaryCol = "fcs01_BS";
        /// <summary>
        /// Material for fcs01_BD
        /// </summary>
        public const string BaseDefaultDecals = "fcs01_BD";
        /// <summary>
        /// Material for fcs01_BTD
        /// </summary>
        public const string BaseTexDecals = "fcs01_BTD";
        /// <summary>
        /// Material for fcs01_BDED
        /// </summary>
        public const string BaseEmissiveDecals = "fcs01_BDED";
        /// <summary>
        /// Material for fcs01_BEDC
        /// </summary>
        public const string BaseEmissiveDecalsController = "fcs01_BEDC";

        public const string BaseDetail = "fcs01_D";
        public const string BaseNormal = "fcs01_N";
        public const string BaseEmission = "fcs01_E";
        public const string BaseSpec = "fcs01_s";
        
        internal static string BodyMaterial => $"fcs{Mod.ModName}_COL";
        internal static string DecalMaterial => $"fcs{Mod.ModName}_DECALS";
        internal static string DetailsMaterial => $"fcs{Mod.ModName}_DETAILS";
        internal static string SpecTexture => $"fcs{Mod.ModName}_S";
        internal static string LUMTexture => $"fcs{Mod.ModName}_E";
        internal static string NormalTexture => $"fcs{Mod.ModName}_N";
        internal static string DetailTexture => $"fcs{Mod.ModName}_D";
        internal static GameObject OilPrefab { get; set; }
        internal static GameObject OreConsumerPrefab { get; set; }
        internal static GameObject AlterraHubPrefab { get; private set; }
        internal static GameObject DebitCardPrefab { get; set; }
        internal static GameObject BioFuelPrefab { get; set; }
        internal static GameObject KitPrefab { get; set; }
        internal static GameObject CartItemPrefab { get; set; }
        internal static GameObject FcsPDAPrefab { get; set; }
        internal static GameObject FCSPDADecoPrefab { get; set; }
        internal static GameObject ColorPickerDialogPrefab { get; set; }
        internal static GameObject MissionObjectiveItemPrefab { get; set; }
        public static GameObject MissionItemPrefab { get; set; }
        public static GameObject PDAEntryPrefab { get; set; }
        public static GameObject MissionMessageBoxPrefab { get; set; }

        public static bool GetPrefabs()
        {
            try
            {
                if (!_initialized)
                {
                    QuickLogger.Debug($"AssetBundle Set");

                    QuickLogger.Debug("GetPrefabs");
                    
                    if (!LoadAsset("TestItem", QPatch.GlobalBundle, out var colorPickerCanvasGo)) return false;
                    ColorPickerDialogPrefab = colorPickerCanvasGo;

                    if (!LoadAssetV2("fcsPDA", QPatch.GlobalBundle, out var fcsPDAPrefab)) return false;
                    FcsPDAPrefab = fcsPDAPrefab;

                    if (!LoadAsset(Mod.AlterraHubPrefabName, QPatch.GlobalBundle, out var prefabGo)) return false;
                    AlterraHubPrefab = prefabGo;

                    if (!LoadAsset(Mod.CardPrefabName, QPatch.GlobalBundle,out var cardPrefabGo)) return false;
                    DebitCardPrefab = cardPrefabGo;

                    if (!LoadAsset(Mod.BioFuelPrefabName, QPatch.GlobalBundle, out var bioFuelPrefabGo)) return false;
                    BioFuelPrefab = bioFuelPrefabGo;

                    if (!LoadAsset(Mod.KitPrefabName, QPatch.GlobalBundle, out var kitPrefabGo)) return false; 
                    KitPrefab = kitPrefabGo;

                    if (!LoadAsset(Mod.OreConsumerPrefabName, QPatch.GlobalBundle, out var oreConsumerPrefabGo)) return false;
                    OreConsumerPrefab = oreConsumerPrefabGo;
                    
                    if (!LoadAsset("CartItem", QPatch.GlobalBundle, out var cartItemPrefabGo)) return false;
                    CartItemPrefab = cartItemPrefabGo;

                    if (!LoadAsset("StoreItem", QPatch.GlobalBundle, out var itemPrefabGo)) return false;
                    ItemPrefab = itemPrefabGo;

                    if (!LoadAssetV2("DecoFCSPDA", QPatch.GlobalBundle, out var fcsPDADecoGo)) return false;
                    FCSPDADecoPrefab = fcsPDADecoGo;

                    if (!LoadAsset("MissionObjectItem", QPatch.GlobalBundle, out var missionObjectiveItemPrefabGo, false)) return false;
                    MissionObjectiveItemPrefab = missionObjectiveItemPrefabGo;

                    if (!LoadAsset("MissionItem", QPatch.GlobalBundle, out var missionItemPrefabGo, false)) return false;
                    MissionItemPrefab = missionItemPrefabGo;                    
                    
                    if (!LoadAsset("PDAEntry", QPatch.GlobalBundle, out var pdaEntryPrefabGo, false)) return false;
                    PDAEntryPrefab = pdaEntryPrefabGo;

                    if (!LoadAsset("MissionMessageBox", QPatch.GlobalBundle, out var missionMessageBox, false)) return false;
                    MissionMessageBoxPrefab = missionMessageBox;

                    _initialized = true;
                }

                return true;
            }
            catch (Exception e)
            {
                QuickLogger.Error(e.Message);
                return false;
            }
        }

        private static bool LoadAsset(string prefabName,AssetBundle assetBundle,out GameObject go,bool applyShaders = true)
        {
            //We have found the asset bundle and now we are going to continue by looking for the model.
            GameObject prefab = assetBundle.LoadAsset<GameObject>(prefabName);
            
            //If the prefab isn't null lets add the shader to the materials
            if (prefab != null)
            {
                if (applyShaders)
                {
                    //Lets apply the material shader
                    ApplyShaders(prefab, assetBundle);
                }

                go = prefab;
                QuickLogger.Debug($"{prefabName} Prefab Found!");
                return true;
            }

            QuickLogger.Error($"{prefabName} Prefab Not Found!");

            go = null;
            return false;
        }

        private static bool LoadAssetV2(string prefabName, AssetBundle assetBundle, out GameObject go, bool applyShaders = true)
        {
            //We have found the asset bundle and now we are going to continue by looking for the model.
            GameObject prefab = assetBundle.LoadAsset<GameObject>(prefabName);

            //If the prefab isn't null lets add the shader to the materials
            if (prefab != null)
            {
                if (applyShaders)
                {
                    //Lets apply the material shader
                    ApplyShadersV2(prefab, assetBundle);
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
            MaterialHelpers.ApplyAlphaShader(DecalMaterial, prefab);
            MaterialHelpers.ApplyAlphaShader(DetailsMaterial, prefab);
            #endregion
        }

        public static void ApplyShadersV2(GameObject prefab, AssetBundle bundle)
        {
            #region BasePrimaryCol
            MaterialHelpers.ApplyNormalShader(BasePrimaryCol, BaseNormal, prefab, bundle);
            #endregion

            #region BaseSecondaryCol
            MaterialHelpers.ApplyNormalShader(BaseSecondaryCol, BaseNormal, prefab, bundle);
            #endregion

            #region BaseDefaultDecals
            MaterialHelpers.ApplyNormalShader(BaseDefaultDecals, BaseNormal, prefab, bundle);
            MaterialHelpers.ApplyAlphaShader(BaseDefaultDecals, prefab);
            #endregion

            #region BaseTexDecals
            MaterialHelpers.ApplyNormalShader(BaseTexDecals, BaseNormal, prefab, bundle);
            MaterialHelpers.ApplyAlphaShader(BaseTexDecals, prefab);
            #endregion

            #region BaseEmissiveDecals
            MaterialHelpers.ApplyNormalShader(BaseEmissiveDecals, BaseNormal, prefab, bundle);
            MaterialHelpers.ApplyAlphaShader(BaseEmissiveDecals, prefab);
            MaterialHelpers.ApplyEmissionShader(BaseEmissiveDecals, BaseEmission, prefab, bundle, Color.white);
            #endregion

            #region BaseEmissiveDecals
            MaterialHelpers.ApplyNormalShader(BaseEmissiveDecalsController, BaseNormal, prefab, bundle);
            MaterialHelpers.ApplyAlphaShader(BaseEmissiveDecalsController, prefab);
            MaterialHelpers.ApplyEmissionShader(BaseEmissiveDecalsController, BaseEmission, prefab, bundle, Color.white);
            #endregion
        }

        public static void LoadV2Materials()
        {
            if (_v2MaterialsLoaded) return;

            if (QPatch.GlobalBundle == null)
            {
                QPatch.GlobalBundle = FCSAssetBundlesService.PublicAPI.GetAssetBundleByName(Mod.AssetBundleName);
            }

            if (QPatch.GlobalBundle == null)
            {
                QuickLogger.Error($"[LoadV2Materials] GlobalBundle returned null stopping process");
                return;
            }

            #region BasePrimaryCol
            Material basePrimaryCol = QPatch.GlobalBundle.LoadAsset<Material>(BasePrimaryCol);
            MaterialHelpers.CreateV2NormalMaterial(basePrimaryCol, BaseNormal, QPatch.GlobalBundle);
            _v2Materials.Add(BasePrimaryCol, basePrimaryCol);
            #endregion

            #region BaseSecondaryCol
            Material baseSecondaryCol = QPatch.GlobalBundle.LoadAsset<Material>(BaseSecondaryCol);
            MaterialHelpers.CreateV2NormalMaterial(baseSecondaryCol, BaseNormal, QPatch.GlobalBundle);
            _v2Materials.Add(BaseSecondaryCol, baseSecondaryCol);
            #endregion

            #region BaseDefaultDecals
            Material baseDefaultDecals = QPatch.GlobalBundle.LoadAsset<Material>(BaseDefaultDecals);
            MaterialHelpers.CreateV2NormalMaterial(baseDefaultDecals, BaseNormal, QPatch.GlobalBundle);
            MaterialHelpers.CreateV2ApplyAlphaMaterial(baseDefaultDecals, QPatch.GlobalBundle);
            MaterialHelpers.CreateV2Specular(baseDefaultDecals,BaseSpec,1,3,QPatch.GlobalBundle);
            _v2Materials.Add(BaseDefaultDecals, baseDefaultDecals);
            #endregion

            #region BaseTexDecals
            Material baseTexDecals = QPatch.GlobalBundle.LoadAsset<Material>(BaseTexDecals);
            MaterialHelpers.CreateV2NormalMaterial(baseTexDecals, BaseNormal, QPatch.GlobalBundle);
            MaterialHelpers.CreateV2ApplyAlphaMaterial(baseTexDecals, QPatch.GlobalBundle);
            _v2Materials.Add(BaseTexDecals, baseTexDecals);
            #endregion

            #region BaseEmissiveDecals
            Material baseEmissiveDecals = QPatch.GlobalBundle.LoadAsset<Material>(BaseEmissiveDecals);
            MaterialHelpers.CreateV2NormalMaterial(baseEmissiveDecals, BaseNormal, QPatch.GlobalBundle);
            MaterialHelpers.CreateV2ApplyAlphaMaterial(baseEmissiveDecals, QPatch.GlobalBundle);
            MaterialHelpers.CreateV2EmissionMaterial(baseEmissiveDecals, BaseEmission, QPatch.GlobalBundle, Color.white);
            _v2Materials.Add(BaseEmissiveDecals, baseEmissiveDecals);
            #endregion

            #region BaseEmissiveDecals
            Material baseEmissiveDecalsController = QPatch.GlobalBundle.LoadAsset<Material>(BaseEmissiveDecalsController);
            MaterialHelpers.CreateV2NormalMaterial(baseEmissiveDecalsController, BaseNormal, QPatch.GlobalBundle);
            MaterialHelpers.CreateV2ApplyAlphaMaterial(baseEmissiveDecalsController, QPatch.GlobalBundle);
            MaterialHelpers.CreateV2EmissionMaterial(baseEmissiveDecalsController, BaseEmission, QPatch.GlobalBundle, Color.white);
            _v2Materials.Add(BaseEmissiveDecalsController, baseEmissiveDecalsController);
            #endregion

            _v2MaterialsLoaded = true;

        }


        public static void ReplaceShadersV2(GameObject prefab,string materialName)
        {
            LoadV2Materials();
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                for (var index = 0; index < renderer.materials.Length; index++)
                {
                    Material material = renderer.materials[index];
                    if (material.name.StartsWith(materialName, StringComparison.OrdinalIgnoreCase))
                    {
                        renderer.materials[index] = _v2Materials[materialName];
                    }
                }
            }
        }
    }
}