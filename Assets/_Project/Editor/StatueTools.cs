using System;
using System.Collections.Generic;
using System.IO;
using DarkDescent.Enemies;
using DarkDescent.Interaction;
using DarkDescent.Levels;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Editor
{
    /// <summary>
    /// La statua che si anima (D7 della M25): l'archetipo e il prefab del nemico <c>Statue</c> (il bruto, più duro e più lento, con la pelle di pietra),
    /// il prefab della statua che sta accanto alla cassa (modello di pietra, macerie, polvere, suoni) e il suo posto nel set della cripta. Si rilancia:
    /// gli asset che ci sono già si aggiornano e niente si duplica. In batchmode: <c>-executeMethod DarkDescent.Editor.StatueTools.Build</c>.
    /// </summary>
    public static class StatueTools
    {
        private const string Root = "Assets/_Project";
        private const string EnemiesFolder = Root + "/Data/Enemies";
        private const string PrefabsFolder = Root + "/Prefabs";
        private const string MaterialsFolder = PrefabsFolder + "/SecretMaterials";
        private const string ModelsFolder = Root + "/Art/Models/DarkDescent";
        private const string EnemyPath = PrefabsFolder + "/StatueEnemy.prefab";
        private const string GuardPath = PrefabsFolder + "/StatueGuard.prefab";
        private static readonly Color Stone = new Color(0.52f, 0.56f, 0.64f, 1f);

        [MenuItem("DarkDescent/Segreti/Statua: crea o aggiorna")]
        public static void Build()
        {
            Directory.CreateDirectory(MaterialsFolder);
            var archetype = Archetype();
            var enemy = EnemyPrefab(archetype);
            var guard = GuardPrefab(enemy.GetComponent<EnemyAI>());

            var tileset = Load<LevelTileset>(Root + "/Data/Levels/DungeonTileset.asset");
            var so = new SerializedObject(tileset);
            so.FindProperty("_statue").objectReferenceValue = guard;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tileset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DarkDescent] Statua: archetipo, nemico e statua pronti nel set della cripta");
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

        // il bruto con un altro nome e il doppio dell'esperienza: un nemico che si incontra una volta e che pesa
        private static EnemyArchetype Archetype()
        {
            string path = EnemiesFolder + "/Statue.asset";
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
            so.FindProperty("_nameKey").stringValue = "enemy.statue";

            // nasce già addosso al cavaliere e non lo lascia: insegue da lontano e non torna a casa
            so.FindProperty("_aggroRange").floatValue = 14f;
            so.FindProperty("_leashRange").floatValue = 40f;
            so.FindProperty("_experience").intValue = 460;
            so.FindProperty("_patrolShare").floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(archetype);
            return archetype;
        }

        // una copia della pelle con la tinta di pietra: stessa texture, più fredda e più scura
        private static Material[] StoneSkin(Material[] source, string prefix)
        {
            var result = new Material[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                string path = $"{MaterialsFolder}/{prefix}_{i}.mat";
                var copy = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (copy == null)
                {
                    copy = new Material(source[i]) { name = $"{prefix}_{i}" };
                    AssetDatabase.CreateAsset(copy, path);
                }

                copy.shader = source[i].shader;
                copy.CopyPropertiesFromMaterial(source[i]);
                copy.SetColor("_BaseColor", Stone);
                if (copy.HasProperty("_Smoothness"))
                {
                    copy.SetFloat("_Smoothness", 0.12f);
                }

                EditorUtility.SetDirty(copy);
                result[i] = copy;
            }

            return result;
        }

        private static GameObject EnemyPrefab(EnemyArchetype archetype)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath) != null)
            {
                AssetDatabase.DeleteAsset(EnemyPath);
            }

            if (!AssetDatabase.CopyAsset(PrefabsFolder + "/Brute.prefab", EnemyPath))
            {
                throw new InvalidOperationException("Copia del prefab del bruto fallita");
            }

            var contents = PrefabUtility.LoadPrefabContents(EnemyPath);
            try
            {
                contents.name = "StatueEnemy";
                var aiSo = new SerializedObject(contents.GetComponent<EnemyAI>());
                aiSo.FindProperty("_archetype").objectReferenceValue = archetype;
                aiSo.ApplyModifiedPropertiesWithoutUndo();

                // più vita e più armatura, un po' più lento: il bruto è già lento, la statua lo è un poco di più
                var healthSo = new SerializedObject(contents.GetComponent<DarkDescent.Combat.Health>());
                healthSo.FindProperty("_maxHealth").floatValue = 220f;
                healthSo.ApplyModifiedPropertiesWithoutUndo();
                foreach (var component in contents.GetComponents<MonoBehaviour>())
                {
                    var stats = new SerializedObject(component);
                    var armor = stats.FindProperty("_armor");
                    if (armor != null && armor.propertyType == SerializedPropertyType.Integer)
                    {
                        armor.intValue = 55;
                        stats.ApplyModifiedPropertiesWithoutUndo();
                    }
                    else if (armor != null && armor.propertyType == SerializedPropertyType.Float)
                    {
                        armor.floatValue = 55f;
                        stats.ApplyModifiedPropertiesWithoutUndo();
                    }
                }

                contents.GetComponent<NavMeshAgent>().speed = 1.45f;

                // la pelle di pietra su ogni mesh del modello; il lampo dei colpi resta quello del bruto
                int index = 0;
                foreach (var renderer in contents.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer is ParticleSystemRenderer || renderer is LineRenderer || renderer is TrailRenderer)
                    {
                        continue;
                    }

                    renderer.sharedMaterials = StoneSkin(renderer.sharedMaterials, "StatueSkin" + index++);
                }

                // un po' più grosso di un bruto: il modello, non la radice (la radice porta il raggio dell'agente)
                var model = contents.GetComponentInChildren<DarkDescent.Characters.CharacterAnimatorDriver>(true);
                if (model != null)
                {
                    model.transform.localScale *= 1.12f;
                }

                PrefabUtility.SaveAsPrefabAsset(contents, EnemyPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            return Load<GameObject>(EnemyPath);
        }

        private static GameObject GuardPrefab(EnemyAI enemy)
        {
            var chest = Load<GameObject>(PrefabsFolder + "/Dungeon/Chest.prefab");
            var stone = chest.GetComponentInChildren<Renderer>(true).sharedMaterials[0];
            var root = new GameObject("StatueGuard");
            try
            {
                var statue = Model("Statue", "Statue", root.transform, stone);
                statue.transform.localScale = Vector3.one * 1.5f;
                var rubble = Model("Rubble", "Rubble", root.transform, stone);
                rubble.SetActive(false);

                // il velo di polvere dei campioni, fermo finché non serve
                var dustAsset = Load<GameObject>(PrefabsFolder + "/VFX/Fx_ChampionDust.prefab");
                var dustObject = (GameObject)PrefabUtility.InstantiatePrefab(dustAsset, root.transform);
                dustObject.name = "Dust";
                dustObject.transform.localPosition = new Vector3(0f, 0.2f, 0f);
                var dust = dustObject.GetComponentInChildren<ParticleSystem>(true);
                if (dust != null)
                {
                    var main = dust.main;
                    main.playOnAwake = false;
                    main.loop = false;
                    main.duration = 1.2f;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.4f);
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.3f);
                    main.startColor = new Color(0.78f, 0.74f, 0.66f, 0.55f);
                    main.maxParticles = 60;
                    var emission = dust.emission;
                    emission.rateOverTime = 40f;
                    var shape = dust.shape;
                    shape.shapeType = ParticleSystemShapeType.Circle;
                    shape.radius = 0.7f;
                    dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }

                var source = root.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 1f;
                source.maxDistance = 22f;
                source.volume = 0.8f;
                var chestSource = chest.GetComponentInChildren<AudioSource>(true);
                source.outputAudioMixerGroup = chestSource != null ? chestSource.outputAudioMixerGroup : null;

                var guard = root.AddComponent<StatueGuard>();
                var so = new SerializedObject(guard);
                so.FindProperty("_statue").objectReferenceValue = statue;
                so.FindProperty("_rubble").objectReferenceValue = rubble;
                so.FindProperty("_dust").objectReferenceValue = dust;
                so.FindProperty("_audio").objectReferenceValue = source;
                so.FindProperty("_grind").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/SFX/Kenney/impactPunch_heavy_001.ogg");
                so.FindProperty("_roar").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/SFX/Creatures/grunt_03.ogg");
                so.FindProperty("_enemyPrefab").objectReferenceValue = enemy;
                so.ApplyModifiedPropertiesWithoutUndo();

                return PrefabUtility.SaveAsPrefabAsset(root, GuardPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject Model(string name, string fbx, Transform parent, Material material)
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>($"{ModelsFolder}/{fbx}.fbx"), parent);
            model.name = name;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterials = new[] { material };
            }

            return model;
        }
    }
}
