﻿using FCSCommon.Utilities;
using FCSTechFabricator.Abstract_Classes;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace FCSTechFabricator.Mono.DeepDriller
{
    public partial class BatteryAttachmentBuildable : FCSTechFabricatorItem
    {
        private TechGroup GroupForPDA = TechGroup.Resources;
        private TechCategory CategoryForPDA = TechCategory.AdvancedMaterials;

        public BatteryAttachmentBuildable() : base("BatteryAttachment_DD", "Battery Attachment")
        {

        }

        public override GameObject GetGameObject()
        {
            GameObject prefab = GameObject.Instantiate<GameObject>(QPatch.BatteryModule);
            prefab.name = this.PrefabFileName;

            // Make the object drop slowly in water
            var wf = prefab.AddComponent<WorldForces>();
            wf.underwaterGravity = 0;
            wf.underwaterDrag = 20f;
            wf.enabled = true;

            // Add fabricating animation
            var fabricatingA = prefab.AddComponent<VFXFabricating>();
            fabricatingA.localMinY = -0.1f;
            fabricatingA.localMaxY = 0.6f;
            fabricatingA.posOffset = new Vector3(0f, 0f, 0f);
            fabricatingA.eulerOffset = new Vector3(0f, 0f, 0f);
            fabricatingA.scaleFactor = 1.0f;

            // Set proper shaders (for crafting animation)
            Shader marmosetUber = Shader.Find("MarmosetUBER");
            var renderer = prefab.GetComponentInChildren<Renderer>();
            renderer.material.shader = marmosetUber;

            // Update sky applier
            var applier = prefab.GetComponent<SkyApplier>();
            if (applier == null)
                applier = prefab.AddComponent<SkyApplier>();
            applier.renderers = new Renderer[] { renderer };
            applier.anchorSky = Skies.Auto;

            // We can pick this item
            var pickupable = prefab.AddComponent<Pickupable>();
            pickupable.isPickupable = true;
            pickupable.randomizeRotationWhenDropped = true;

            PrefabIdentifier prefabID = prefab.AddComponent<PrefabIdentifier>();
            prefabID.ClassId = this.ClassID;

            var techTag = prefab.AddComponent<TechTag>();
            techTag.type = this.TechType;

            prefab.AddComponent<FCSTechFabricatorTag>();
            return prefab;
        }

        private TechData GetBlueprintRecipe()
        {
            // Create and associate recipe to the new TechType
            var customFabRecipe = new TechData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>()
                {
                    new Ingredient(TechType.Copper, 1),
                    new Ingredient(TechType.AcidMushroom, 2),
                    new Ingredient(TechType.Silicone, 1),
                    new Ingredient(TechType.Titanium, 1),
                }
            };
            return customFabRecipe;
        }

        public override void Register()
        {
            if (QPatch.BatteryModule != null)
            {
                if (this.IsRegistered == false)
                {
                    //Create a new TechType
                    this.TechType = TechTypeHandler.AddTechType(ClassID, PrefabFileName, "This specially made attachment allows you to run your deep driller off battery power", new Atlas.Sprite(ImageUtils.LoadTextureFromFile($"./QMods/FCSTechFabricator/Assets/{ClassID}.png")));

                    FriendlyName_I = this.PrefabFileName;

                    TechTypeID = TechType;

                    ClassID_I = this.ClassID;
                    QuickLogger.Debug($"Class Id = {ClassID_I}");

                    CraftDataHandler.SetTechData(TechType, GetBlueprintRecipe());

                    CraftDataHandler.AddToGroup(this.GroupForPDA, this.CategoryForPDA, this.TechType);

                    // Add the new TechType to Hand Equipment type.
                    CraftDataHandler.SetEquipmentType(TechType, EquipmentType.PowerCellCharger);

                    PrefabHandler.RegisterPrefab(this);

                    this.IsRegistered = true;
                }
            }
            else
            {
                QuickLogger.Error("Failed to get the BatteryModulePrefab");
            }
        }

    }
}