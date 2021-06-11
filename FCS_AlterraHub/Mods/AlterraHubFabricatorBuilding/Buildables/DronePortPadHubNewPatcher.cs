﻿
using System.Collections.Generic;
using FCS_AlterraHub.Enumerators;
using FCS_AlterraHub.Extensions;
using FCS_AlterraHub.Mods.AlterraHubFabricatorBuilding.Mono.DroneSystem;
using FCS_AlterraHub.Mods.Global.Spawnables;
using FCS_AlterraHub.Registration;
using FCS_AlterraHub.Structs;
using SMLHelper.V2.Crafting;
#if SUBNAUTICA
using System;
using System.IO;
using FCS_AlterraHub.Configuration;
using FCS_AlterraHub.Helpers;
using FCSCommon.Utilities;
using SMLHelper.V2.Utility;
using UnityEngine;
using RecipeData = SMLHelper.V2.Crafting.TechData;
using Sprite = Atlas.Sprite;
#endif


namespace FCS_AlterraHub.Mods.AlterraHubFabricatorBuilding.Buildables
{
    internal class DronePortPadHubNewPatcher : SMLHelper.V2.Assets.Buildable
    {
        public override TechGroup GroupForPDA => TechGroup.ExteriorModules;
        public override TechCategory CategoryForPDA => TechCategory.ExteriorModule;
        public override string AssetsFolder => Mod.GetAssetPath();
        
        public override TechType RequiredForUnlock => Mod.DronePortPadHubNewFragmentTechType;

        public override string DiscoverMessage => $"{this.FriendlyName} Unlocked!";

        public override bool AddScannerEntry => true;

        public override int FragmentsToScan => 3;

        public override float TimeToScanFragment => 5f;

        public override bool DestroyFragmentOnScan => true;

        public DronePortPadHubNewPatcher() : base(Mod.DronePortPadHubNewClassID, Mod.DronePortPadHubNewFriendly, Mod.DronePortPadHubNewDescription)
        {
            OnStartedPatching += () =>
            {
                var alterraGenKit = new FCSKit(Mod.DronePortPadHubNewKitClassID, Mod.DronePortPadHubNewFriendly,
                    Path.Combine(AssetsFolder, $"{ClassID}.png"));
                alterraGenKit.Patch();
            };

            OnFinishedPatching += () =>
            {
                FCSAlterraHubService.PublicAPI.CreateStoreEntry(TechType, Mod.DronePortPadHubNewKitClassID.ToTechType(), 90000, StoreCategory.Misc);
            };
        }
        
#if SUBNAUTICA_STABLE
        public override GameObject GetGameObject()
        {
            try
            {
                var prefab = GameObject.Instantiate(FCS_AlterraHub.Buildables.AlterraHub.DronePortPadHubNewPrefab);

                var size = new Vector3(6.70943f, 4.943072f, 8.8695f);
                var center = new Vector3(0f, 2.756582f, 0.6690737f);

                GameObjectHelpers.AddConstructableBounds(prefab, size, center);

                var model = prefab.FindChild("model");

                //========== Allows the building animation and material colors ==========// 
                Shader shader = Shader.Find("MarmosetUBER");
                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
                SkyApplier skyApplier = prefab.EnsureComponent<SkyApplier>();
                skyApplier.renderers = renderers;
                skyApplier.anchorSky = Skies.Auto;
                //========== Allows the building animation and material colors ==========// 

                var lw = prefab.AddComponent<LargeWorldEntity>();
                lw.cellLevel = LargeWorldEntity.CellLevel.VeryFar;

                // Add constructible
                var constructable = prefab.AddComponent<Constructable>();

                constructable.allowedOutside = true;
                constructable.allowedInBase = false;
                constructable.allowedOnGround = true;
                constructable.allowedOnWall = true;
                constructable.rotationEnabled = true;
                constructable.allowedOnCeiling = false;
                constructable.allowedInSub = false;
                constructable.allowedOnConstructables = false;
                constructable.model = model;
                constructable.placeMaxDistance = 10f; 
                constructable.placeDefaultDistance = 5f; 
                constructable.techType = TechType;

                PrefabIdentifier prefabID = prefab.AddComponent<PrefabIdentifier>();
                prefabID.ClassId = ClassID;

                prefab.AddComponent<TechTag>().type = TechType;
                prefab.AddComponent<AlterraDronePortController>();
                return prefab;

            }
            catch (Exception e)
            {
                QuickLogger.Error(e.Message);
            }

            return null;
        }
#else
        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
                var prefab = GameObject.Instantiate(AlterraHub.DronePortPadHubNewPrefab);

                var size = new Vector3(6.70943f, 4.943072f, 8.8695f);
                var center = new Vector3(0f, 2.756582f, 0.6690737f);

                GameObjectHelpers.AddConstructableBounds(prefab, size, center);

                var model = prefab.FindChild("model");

                //========== Allows the building animation and material colors ==========// 
                Shader shader = Shader.Find("MarmosetUBER");
                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
                SkyApplier skyApplier = prefab.EnsureComponent<SkyApplier>();
                skyApplier.renderers = renderers;
                skyApplier.anchorSky = Skies.Auto;
                //========== Allows the building animation and material colors ==========// 

                var lw = prefab.AddComponent<LargeWorldEntity>();
                lw.cellLevel = LargeWorldEntity.CellLevel.VeryFar;

                // Add constructible
                var constructable = prefab.AddComponent<Constructable>();

                constructable.allowedOutside = true;
                constructable.allowedInBase = false;
                constructable.allowedOnGround = true;
                constructable.allowedOnWall = true;
                constructable.rotationEnabled = true;
                constructable.allowedOnCeiling = false;
                constructable.allowedInSub = false;
                constructable.allowedOnConstructables = false;
                constructable.model = model;
                constructable.techType = TechType;

                PrefabIdentifier prefabID = prefab.AddComponent<PrefabIdentifier>();
                prefabID.ClassId = ClassID;

                prefab.AddComponent<TechTag>().type = TechType;
                prefab.AddComponent<AlterraDronePortController>();
                //prefab.AddComponent<FCSGameLoadUtil>();
            gameObject.Set(prefab);
            yield break;
        }
#endif


        protected override RecipeData GetBlueprintRecipe()
        {
            QuickLogger.Debug($"Creating recipe...");
            // Create and associate recipe to the new TechType
            var customFabRecipe = new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>()
                {
                    new Ingredient(Mod.DronePortPadHubNewKitClassID.ToTechType(),1)
                }
            };
            return customFabRecipe;
        }

        protected override Sprite GetItemSprite()
        {
            return ImageUtils.LoadSpriteFromFile(Path.Combine(AssetsFolder, $"{ClassID}.png"));
        }
    }
}
