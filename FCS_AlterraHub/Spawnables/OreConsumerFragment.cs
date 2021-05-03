﻿using System;
using System.Collections;
using System.Collections.Generic;
using FCS_AlterraHub.Buildables;
using FCS_AlterraHub.Configuration;
using SMLHelper.V2.Assets;
using SMLHelper.V2.Utility;
using UnityEngine;
using UWE;

namespace FCS_AlterraHub.Spawnables
{
    internal class OreConsumerFragment : Spawnable
    {
        public OreConsumerFragment() : base("OreConsumerFragment","Ore Consumer Fragment","Fragment of an Ore Consumer Machine.")
        {
            OnFinishedPatching += () => { Mod.OreConsumerFragmentTechType = TechType; };
        }


        public override WorldEntityInfo EntityInfo => new WorldEntityInfo() { cellLevel = LargeWorldEntity.CellLevel.Medium, classId = ClassID, localScale = Vector3.one, prefabZUp = false, slotType = EntitySlot.Type.Medium, techType = TechType };

        public override GameObject GetGameObject()
        {
            try
            {
                var prefab = GameObject.Instantiate(AlterraHub.OreConsumerPrefab);

                PrefabIdentifier prefabIdentifier = prefab.AddComponent<PrefabIdentifier>();
                prefabIdentifier.ClassId = this.ClassID;
                prefab.AddComponent<LargeWorldEntity>().cellLevel = LargeWorldEntity.CellLevel.VeryFar;
                prefab.AddComponent<TechTag>().type = this.TechType;

                var rb = prefab.GetComponentInChildren<Rigidbody>();
                
                if (rb == null)
                {
                    rb = prefab.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                }
                
                Pickupable pickupable = prefab.AddComponent<Pickupable>();
                pickupable.isPickupable = false;

                ResourceTracker resourceTracker = prefab.AddComponent<ResourceTracker>();
                resourceTracker.prefabIdentifier = prefabIdentifier;
                resourceTracker.techType = this.TechType;
                resourceTracker.overrideTechType = TechType.Fragment;
                resourceTracker.rb = rb;
                resourceTracker.pickupable = pickupable;

                prefab.AddComponent<OreConsumerFragmentSpawn>();
                return prefab;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> oreConsumerFragment)
        {
            oreConsumerFragment.Set(GetGameObject());
            yield break;
        }


#if SUBNAUTICA
        protected override Atlas.Sprite GetItemSprite()
        {
            return new Atlas.Sprite(ImageUtils.LoadTextureFromFile(Mod.GetIconPath(ClassID)));
        }
#elif BELOWZERO
        protected override Sprite GetItemSprite()
        {
            return ImageUtils.LoadSpriteFromFile(Mod.GetIconPath(ClassID));
        }
#endif
    }

    internal class OreConsumerFragmentSpawn:MonoBehaviour
    {
    }
}
