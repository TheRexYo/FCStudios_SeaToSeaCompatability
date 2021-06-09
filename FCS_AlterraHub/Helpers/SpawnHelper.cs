﻿using System;
using System.Collections;
using System.Collections.Generic;
using FCS_AlterraHub.Mono;
using FCSCommon.Helpers;
using UnityEngine;
using UWE;
using Object = UnityEngine.Object;

namespace FCS_AlterraHub.Helpers
{
    public static class SpawnHelper
    {

        private static Dictionary<string, string> PlantResourceDictionary = new Dictionary<string, string>
        {
            { "[CORAL_REEF_PLANT_MIDDLE]","WorldEntities/Doodads/Coral_reef/coral_reef_plant_middle_03"},
            { "[CORAL_REEF_G3]","WorldEntities/Doodads/Coral_reef/coral_reef_grass_03"},
            { "[CORAL_REEF_SMALL_DECO]","WorldEntities/Doodads/Coral_reef/coral_reef_small_deco_14"},
            { "[PURPLE_FAN]","WorldEntities/Doodads/Coral_reef/Coral_reef_purple_fan"},
        };

        private static Dictionary<UWEPrefabID, string> UWEClassIDDictionary = new Dictionary<UWEPrefabID, string>
        {
            {UWEPrefabID.UnderwaterElecSourceMedium,"ff8e782e-e6f3-40a6-9837-d5b6dcce92bc"},
            {UWEPrefabID.FloatingPapers,"b4ec5044-5519-4743-b61b-92a8b6fe4a32"},
            {UWEPrefabID.BubbleColumnSmall,"5ec8b8a6-b9b1-412b-9048-62701346cca2"},
            {UWEPrefabID.BubbleColumnBig,"0dbd3431-62cc-4dd2-82d5-7d60c71a9edf"},
            {UWEPrefabID.StarshipGirder10,"99c0da07-a612-4cb7-9e16-e2e6bd3d6207"},
        };

        public static GameObject SpawnAtPoint(string location, Transform trans, float scale = 0.179f)
        {
            var obj = GameObject.Instantiate(Resources.Load<GameObject>(PlantResourceDictionary[location]));
            MaterialHelpers.ChangeWavingSpeed(obj,new Vector4(0f,0f,0.10f,0f));
            obj.transform.SetParent(trans,false);
            obj.transform.position = trans.position;
            obj.transform.localScale *= scale;
            Object.Destroy(obj.GetComponent<Rigidbody>());
            Object.Destroy(obj.GetComponent<WorldForces>());
            return obj;
        }

        public static bool ContainsPlant(string plantKey)
        {
            return PlantResourceDictionary.ContainsKey(plantKey);
        }

        public static IEnumerator SpawnPrefabByClassID(string classId,Transform transform)
        {
            IPrefabRequest request = PrefabDatabase.GetPrefabAsync(classId);
            yield return request;
            GameObject prefab;

            if (!request.TryGetPrefab(out prefab))
            {
                Debug.LogErrorFormat("FindPrefab", "Failed to request prefab for '{0}'", new object[]
                {
                    classId
                });
                //Destroy(base.gameObject);
                yield break;
            }

            DeferredSpawner.Task deferredTask = DeferredSpawner.instance.InstantiateAsync(prefab, transform.localPosition, transform.localRotation, true);
            yield return deferredTask;
            GameObject result = deferredTask.result;
            DeferredSpawner.instance.ReturnTask(deferredTask);
            result.transform.SetParent(transform.parent, false);
            result.transform.localScale = transform.localScale;
            result.SetActive(true);
            //Destroy(base.gameObject);
            yield break;
        }

        public static IEnumerator SpawnUWEPrefab(UWEPrefabID uwePrefab, Transform transform,Action<GameObject> callBack = null,bool removeComponents = true)
        {
            IPrefabRequest request = PrefabDatabase.GetPrefabAsync(UWEClassIDDictionary[uwePrefab]);
            yield return request;
            GameObject prefab;

            if (!request.TryGetPrefab(out prefab))
            {
                Debug.LogErrorFormat("FindPrefab", "Failed to request prefab for '{0}'", new object[]
                {
                    UWEClassIDDictionary[uwePrefab]
                });
                //Destroy(base.gameObject);
                yield break;
            }

            DeferredSpawner.Task deferredTask = DeferredSpawner.instance.InstantiateAsync(prefab, transform.localPosition, transform.localRotation, true);
            yield return deferredTask;
            GameObject result = deferredTask.result;
            DeferredSpawner.instance.ReturnTask(deferredTask);
            result.transform.SetParent(transform.parent, false);
            result.transform.localScale = transform.localScale;
            result.SetActive(true);
            if(removeComponents)
            {
                GameObject.DestroyImmediate(result.GetComponent<PrefabIdentifier>());
                GameObject.DestroyImmediate(result.GetComponent<LargeWorldEntity>());
            }

            callBack?.Invoke(result);
            yield break;
        }

        public static IEnumerator SpawnUWEPrefab(UWEPrefabID uwePrefab, Vector3 position, Quaternion rotation, bool removeComponents = true)
        {
            IPrefabRequest request = PrefabDatabase.GetPrefabAsync(UWEClassIDDictionary[uwePrefab]);
            yield return request;
            GameObject prefab;

            if (!request.TryGetPrefab(out prefab))
            {
                Debug.LogErrorFormat("FindPrefab", "Failed to request prefab for '{0}'", new object[]
                {
                    UWEClassIDDictionary[uwePrefab]
                });
                //Destroy(base.gameObject);
                yield break;
            }

            DeferredSpawner.Task deferredTask = DeferredSpawner.instance.InstantiateAsync(prefab, position, rotation, true);
            yield return deferredTask;
            GameObject result = deferredTask.result;
            DeferredSpawner.instance.ReturnTask(deferredTask);
            result.SetActive(true);
            if (removeComponents)
            {
                GameObject.DestroyImmediate(result.GetComponent<PrefabIdentifier>());
                GameObject.DestroyImmediate(result.GetComponent<LargeWorldEntity>());
            }
            yield break;
        }
    }

    public enum UWEPrefabID
    {
        UnderwaterElecSourceMedium,
        FloatingPapers,
        BubbleColumnSmall,
        BubbleColumnBig,
        StarshipGirder10
    }
}
