using System;
using System.Collections.Generic;
using DarkDescent.Characters;
using DarkDescent.Enemies;
using DarkDescent.Items;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Editor
{
    /// <summary>
    /// I segreti delle casse (D3 e D4 della M25): l'archetipo e il prefab del mimic (il forziere dei KayKit con la fisica e i colpi del
    /// bruto, senza il loot suo: lascia quello della cassa) e, sul prefab della cassa normale, il segreto con le sue catene
    /// (<c>ChestCurse</c>), il modello da nascondere e i prefab dei nemici dell'ondata. Si rilancia: gli asset che ci sono già si
    /// aggiornano e niente si duplica.
    /// </summary>
    public static class SecretTools
    {
        private const string Root = "Assets/_Project";
        private const string EnemiesFolder = Root + "/Data/Enemies";
        private const string PrefabsFolder = Root + "/Prefabs";
        private const string ChestPath = PrefabsFolder + "/Dungeon/Chest.prefab";
        private const string MimicPath = PrefabsFolder + "/Mimic.prefab";
        private const string CursePath = Root + "/Art/Models/DarkDescent/ChestCurse.fbx";
        private const string CurseName = "CurseLook";

        [MenuItem("DarkDescent/Segreti/Mimic e baule maledetto: crea o aggiorna")]
        public static void Build()
        {
            var archetype = Archetype();
            var mimic = MimicPrefab(archetype);
            ChestPrefab(mimic);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DarkDescent] Segreti: archetipo e prefab del mimic, segreto sulla cassa normale pronti");
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException("Manca " + path);
            }

            return asset;
        }

        // il bruto con un altro nome: stessa vita, stessi colpi, stessa esperienza (D3: un bruto che salta fuori dal forziere)
        private static EnemyArchetype Archetype()
        {
            string path = EnemiesFolder + "/Mimic.asset";
            var archetype = AssetDatabase.LoadAssetAtPath<EnemyArchetype>(path);
            if (archetype == null)
            {
                if (!AssetDatabase.CopyAsset(EnemiesFolder + "/Brute.asset", path))
                {
                    throw new InvalidOperationException("Copia dell'archetipo del bruto fallita");
                }

                archetype = Load<EnemyArchetype>(path);
            }

            var so = new SerializedObject(archetype);
            so.FindProperty("_nameKey").stringValue = "enemy.mimic";

            // da sveglio non aspetta: insegue da lontano e non torna a casa
            so.FindProperty("_aggroRange").floatValue = 14f;
            so.FindProperty("_leashRange").floatValue = 40f;
            so.FindProperty("_corpseLifetime").floatValue = 1.5f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(archetype);
            return archetype;
        }

        // la radice del modello del forziere dentro il prefab della cassa: il figlio diretto che contiene il coperchio
        private static Transform ChestModelRoot(GameObject chest)
        {
            var lid = new SerializedObject(chest.GetComponent<Chest>()).FindProperty("_lid").objectReferenceValue as Transform;
            if (lid == null)
            {
                throw new InvalidOperationException("La cassa normale non ha il coperchio");
            }

            var node = lid;
            while (node.parent != null && node.parent != chest.transform)
            {
                node = node.parent;
            }

            return node;
        }

        private static string RelativePath(Transform root, Transform node)
        {
            var parts = new List<string>();
            for (var t = node; t != null && t != root; t = t.parent)
            {
                parts.Insert(0, t.name);
            }

            return string.Join("/", parts);
        }

        private static GameObject MimicPrefab(EnemyArchetype archetype)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(MimicPath) != null)
            {
                AssetDatabase.DeleteAsset(MimicPath);
            }

            if (!AssetDatabase.CopyAsset(PrefabsFolder + "/Brute.prefab", MimicPath))
            {
                throw new InvalidOperationException("Copia del prefab del bruto fallita");
            }

            var chestAsset = Load<GameObject>(ChestPath);
            var sourceModel = ChestModelRoot(chestAsset);
            var lidPath = RelativePath(sourceModel, new SerializedObject(chestAsset.GetComponent<Chest>()).FindProperty("_lid").objectReferenceValue as Transform);
            var fbx = PrefabUtility.GetCorrespondingObjectFromSource(sourceModel.gameObject);

            var contents = PrefabUtility.LoadPrefabContents(MimicPath);
            try
            {
                contents.name = "Mimic";

                // via il modello dello scheletro; il lampo dei colpi si porta il suo materiale sul forziere
                var skeletonModel = contents.GetComponentInChildren<CharacterAnimatorDriver>(true);
                Material flash = null;
                if (skeletonModel != null)
                {
                    var oldFlash = skeletonModel.GetComponent<HitFlash>();
                    if (oldFlash != null)
                    {
                        flash = new SerializedObject(oldFlash).FindProperty("_flashMaterial").objectReferenceValue as Material;
                    }

                    UnityEngine.Object.DestroyImmediate(skeletonModel.gameObject);
                }

                // il forziere dei KayKit, con i materiali di quello della cassa
                var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx, contents.transform);
                model.name = "Model";
                model.transform.localPosition = sourceModel.localPosition;
                model.transform.localRotation = sourceModel.localRotation;
                model.transform.localScale = sourceModel.localScale;
                var sourceRenderers = sourceModel.GetComponentsInChildren<Renderer>(true);
                var renderers = model.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length && i < sourceRenderers.Length; i++)
                {
                    renderers[i].sharedMaterials = sourceRenderers[i].sharedMaterials;
                }

                var flashComponent = model.AddComponent<HitFlash>();
                if (flash != null)
                {
                    var flashSo = new SerializedObject(flashComponent);
                    flashSo.FindProperty("_flashMaterial").objectReferenceValue = flash;
                    flashSo.ApplyModifiedPropertiesWithoutUndo();
                }

                var lid = string.IsNullOrEmpty(lidPath) ? null : model.transform.Find(lidPath);
                var hop = contents.AddComponent<MimicHop>();
                hop.Configure(model.transform, lid, contents.GetComponent<NavMeshAgent>());

                var aiSo = new SerializedObject(contents.GetComponent<EnemyAI>());
                aiSo.FindProperty("_archetype").objectReferenceValue = archetype;
                aiSo.ApplyModifiedPropertiesWithoutUndo();

                // il premio del mimic è la cassa: niente loot suo
                var loot = contents.GetComponent<LootDrop>();
                if (loot != null)
                {
                    UnityEngine.Object.DestroyImmediate(loot);
                }

                PrefabUtility.SaveAsPrefabAsset(contents, MimicPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            return Load<GameObject>(MimicPath);
        }

        // il segreto sulla cassa normale: il modello, le catene (spente), il mimic e i nemici dell'ondata
        private static void ChestPrefab(GameObject mimic)
        {
            var contents = PrefabUtility.LoadPrefabContents(ChestPath);
            try
            {
                var model = ChestModelRoot(contents);
                var secret = contents.GetComponent<ChestSecret>();
                if (secret == null)
                {
                    secret = contents.AddComponent<ChestSecret>();
                }

                // le catene: figlie della cassa, con lo stesso posto del forziere, spente finché non servono
                var existing = contents.transform.Find(CurseName);
                if (existing != null)
                {
                    UnityEngine.Object.DestroyImmediate(existing.gameObject);
                }

                var curse = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(CursePath), contents.transform);
                curse.name = CurseName;
                curse.transform.localPosition = model.localPosition;
                curse.transform.localRotation = model.localRotation;
                curse.transform.localScale = model.localScale;
                var sourceRenderers = model.GetComponentsInChildren<Renderer>(true);
                var curseRenderers = curse.GetComponentsInChildren<Renderer>(true);
                if (sourceRenderers.Length > 0)
                {
                    foreach (var renderer in curseRenderers)
                    {
                        renderer.sharedMaterials = new[] { sourceRenderers[0].sharedMaterials[0] };
                    }
                }

                // un bagliore viola: dalla camera del gioco le catene sole si perdono nel buio della stanza
                var glow = new GameObject("CurseGlow");
                glow.transform.SetParent(curse.transform, false);
                glow.transform.localPosition = new Vector3(0f, 1.4f, 0f);
                var light = glow.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(0.72f, 0.32f, 1f);
                light.intensity = 2.2f;
                light.range = 3.4f;
                light.shadows = LightShadows.None;

                curse.SetActive(false);

                var so = new SerializedObject(secret);
                so.FindProperty("_model").objectReferenceValue = model.gameObject;
                so.FindProperty("_curseLook").objectReferenceValue = curse;
                so.FindProperty("_mimicPrefab").objectReferenceValue = mimic.GetComponent<EnemyAI>();
                so.FindProperty("_skeletonPrefab").objectReferenceValue = Load<GameObject>(PrefabsFolder + "/Skeleton.prefab").GetComponent<EnemyAI>();
                so.FindProperty("_brutePrefab").objectReferenceValue = Load<GameObject>(PrefabsFolder + "/Brute.prefab").GetComponent<EnemyAI>();
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, ChestPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
    }
}
