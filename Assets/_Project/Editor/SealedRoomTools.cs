using System;
using System.IO;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Levels;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Editor
{
    /// <summary>
    /// La stanza sigillata (D5 della M25): il prefab della porta di pietra (con le rune che brillano, un ostacolo che ritaglia il NavMesh e un collider che
    /// ferma vista e incantesimi) e quello della leva (il piedistallo, l'asta e il click), e i due posti nel set della cripta. Si rilancia: i prefab si
    /// rifanno e niente si duplica.
    /// </summary>
    public static class SealedRoomTools
    {
        private const string Root = "Assets/_Project";
        private const string ModelsFolder = Root + "/Art/Models/DarkDescent";
        private const string PrefabsFolder = Root + "/Prefabs";
        private const string MaterialsFolder = PrefabsFolder + "/SecretMaterials";
        private const string DoorPath = PrefabsFolder + "/SealedDoor.prefab";
        private const string LeverPath = PrefabsFolder + "/SecretLever.prefab";
        private const string ChestPath = PrefabsFolder + "/Dungeon/Chest.prefab";
        private static readonly Color RuneColor = new Color(0.62f, 0.28f, 1f);

        [MenuItem("DarkDescent/Segreti/Stanza sigillata: crea o aggiorna")]
        public static void Build()
        {
            Directory.CreateDirectory(MaterialsFolder);
            var chest = Load<GameObject>(ChestPath);
            var stone = StoneMaterial(chest);
            var highlight = new SerializedObject(chest.GetComponent<InteractableHighlight>()).FindProperty("_highlightMaterial").objectReferenceValue as Material;
            var runes = RunesMaterial();
            var door = DoorPrefab(stone, runes);
            var lever = LeverPrefab(stone, highlight, chest.GetComponentInChildren<AudioSource>(true));

            var tileset = Load<LevelTileset>(Root + "/Data/Levels/DungeonTileset.asset");
            var so = new SerializedObject(tileset);
            so.FindProperty("_sealedDoor").objectReferenceValue = door;
            so.FindProperty("_lever").objectReferenceValue = lever;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tileset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DarkDescent] Stanza sigillata: porta, leva e set della cripta pronti");
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

        // il materiale dei modelli dei KayKit: quello del forziere
        private static Material StoneMaterial(GameObject chest)
        {
            var renderer = chest.GetComponentInChildren<Renderer>(true);
            return renderer.sharedMaterials[0];
        }

        // le rune brillano: URP Lit con una tinta viola e l'emissione accesa, salvato come asset (l'emissione a runtime nelle build non c'è, trappola 8 della M2)
        private static Material RunesMaterial()
        {
            string path = MaterialsFolder + "/SealedRunes.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "SealedRunes" };
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", RuneColor);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", RuneColor * 2.4f);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject Model(string name, Transform parent, Material material)
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>($"{ModelsFolder}/{name}.fbx"), parent);
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterials = new[] { material };
            }

            return model;
        }

        private static GameObject DoorPrefab(Material stone, Material runeMaterial)
        {
            var root = new GameObject("SealedDoor");
            try
            {
                int obstacleLayer = LayerMask.NameToLayer("Obstacle");
                var leaf = new GameObject("Leaf");
                leaf.transform.SetParent(root.transform, false);
                Model("SealedDoor", leaf.transform, stone).name = "Stone";
                var runes = Model("SealedDoor_Runes", leaf.transform, runeMaterial);
                runes.name = "Runes";
                foreach (var renderer in runes.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }

                var glowObject = new GameObject("Glow");
                glowObject.transform.SetParent(root.transform, false);
                glowObject.transform.localPosition = new Vector3(0f, 2.1f, 1.1f);
                var glow = glowObject.AddComponent<Light>();
                glow.type = LightType.Point;
                glow.color = RuneColor;
                glow.intensity = 2.6f;
                glow.range = 5.5f;
                glow.shadows = LightShadows.None;

                // la porta ferma la vista dei nemici e gli incantesimi: un collider sul layer degli ostacoli, come i muri
                var blocker = new GameObject("Blocker") { layer = obstacleLayer };
                blocker.transform.SetParent(root.transform, false);
                var box = blocker.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 2f, 0f);
                box.size = new Vector3(4f, 4f, 0.7f);

                // e il passo: un ostacolo che ritaglia il NavMesh nella cella, un po' più largo del corridoio perché non resti uno spiraglio
                var obstacle = root.AddComponent<NavMeshObstacle>();
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.center = new Vector3(0f, 1.5f, 0f);
                obstacle.size = new Vector3(4.6f, 3f, 1.4f);
                obstacle.carving = true;
                obstacle.carveOnlyStationary = false;

                var door = root.AddComponent<SealedDoor>();
                var so = new SerializedObject(door);
                so.FindProperty("_leaf").objectReferenceValue = leaf.transform;
                so.FindProperty("_runes").objectReferenceValue = runes;
                so.FindProperty("_glow").objectReferenceValue = glow;
                so.FindProperty("_obstacle").objectReferenceValue = obstacle;
                var blockers = so.FindProperty("_blockers");
                blockers.arraySize = 1;
                blockers.GetArrayElementAtIndex(0).objectReferenceValue = box;
                so.ApplyModifiedPropertiesWithoutUndo();

                return PrefabUtility.SaveAsPrefabAsset(root, DoorPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject LeverPrefab(Material stone, Material highlight, AudioSource chestSource)
        {
            var root = new GameObject("SecretLever");
            try
            {
                var pedestal = Model("Lever", root.transform, stone);
                pedestal.name = "Pedestal";

                // l'asta gira nel perno in cima al piedistallo, a 1,05 m: il suo piede è l'origine del suo modello
                var handle = Model("Lever_Handle", root.transform, stone);
                handle.name = "Handle";
                handle.transform.localPosition = new Vector3(0f, 1.05f, 0f);

                var click = new GameObject("ClickArea") { layer = LayerMask.NameToLayer("Interactable") };
                click.transform.SetParent(root.transform, false);
                var box = click.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0.8f, 0f);
                box.size = new Vector3(1.3f, 1.8f, 1.1f);

                var approach = new GameObject("ApproachPoint");
                approach.transform.SetParent(root.transform, false);
                approach.transform.localPosition = new Vector3(0f, 0f, 1.4f);

                var interactable = root.AddComponent<Interactable>();
                var interactableSo = new SerializedObject(interactable);
                interactableSo.FindProperty("_approachPoint").objectReferenceValue = approach.transform;
                interactableSo.FindProperty("_labelKey").stringValue = "lever.label";
                interactableSo.ApplyModifiedPropertiesWithoutUndo();

                var highlightComponent = root.AddComponent<InteractableHighlight>();
                var highlightSo = new SerializedObject(highlightComponent);
                var renderers = pedestal.GetComponentsInChildren<Renderer>(true);
                var list = highlightSo.FindProperty("_renderers");
                list.arraySize = renderers.Length;
                for (int i = 0; i < renderers.Length; i++)
                {
                    list.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
                }

                highlightSo.FindProperty("_highlightMaterial").objectReferenceValue = highlight;
                highlightSo.ApplyModifiedPropertiesWithoutUndo();

                // il tintinnio a 6 m (D12): una sorgente nello spazio, con l'uscita della cassa e un colpo di metallo leggero
                var source = root.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 1f;
                source.maxDistance = 16f;
                source.volume = 0.55f;
                source.outputAudioMixerGroup = chestSource != null ? chestSource.outputAudioMixerGroup : null;

                var lever = root.AddComponent<SecretLever>();
                var leverSo = new SerializedObject(lever);
                leverSo.FindProperty("_audio").objectReferenceValue = source;
                leverSo.FindProperty("_tinkle").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/SFX/Kenney/impactMetal_light_001.ogg");
                leverSo.FindProperty("_handle").objectReferenceValue = handle.transform;
                leverSo.FindProperty("_clickArea").objectReferenceValue = box;
                leverSo.ApplyModifiedPropertiesWithoutUndo();

                return PrefabUtility.SaveAsPrefabAsset(root, LeverPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
