using System;
using System.IO;
using System.Linq;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Levels;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Editor
{
    /// <summary>
    /// Il prigioniero (D6 della M25): il prefab del cavaliere in catene (il barbaro di Adventurers senza il cappello, in posa seduta, con le catene della M13 ai piedi, il click e
    /// l'evidenziazione come gli altri), con la pozione e l'oro del ringraziamento presi dalla tabella dei bauli, e il suo posto nel set della cripta. Si rilancia:
    /// il prefab si rifà e niente si duplica. In batchmode: <c>-executeMethod DarkDescent.Editor.PrisonerTools.Build</c>.
    /// </summary>
    public static class PrisonerTools
    {
        private const string Root = "Assets/_Project";
        private const string PrefabsFolder = Root + "/Prefabs";
        private const string ModelsFolder = Root + "/Art/Models/DarkDescent";
        private const string Adventurers = KayKitImporter.ModelFolder;
        private const string Animations = Root + "/Art/Animations/KayKit/";
        private const string MaterialsFolder = PrefabsFolder + "/SecretMaterials";
        private const string PrefabPath = PrefabsFolder + "/Prisoner.prefab";
        private static readonly Color Dirt = new Color(0.74f, 0.66f, 0.6f, 1f);

        [MenuItem("DarkDescent/Segreti/Prigioniero: crea o aggiorna")]
        public static void Build()
        {
            Directory.CreateDirectory(MaterialsFolder);
            var prefab = PrisonerPrefab();
            var tileset = Load<LevelTileset>(Root + "/Data/Levels/DungeonTileset.asset");
            var so = new SerializedObject(tileset);
            so.FindProperty("_prisoner").objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tileset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DarkDescent] Prigioniero: prefab pronto nel set della cripta");
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

        // la prima clip che c'è tra i nomi dati, nel file dei movimenti
        private static AnimationClip Clip(string file, params string[] names)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(Animations + file + ".fbx").OfType<AnimationClip>().ToArray();
            foreach (string name in names)
            {
                var found = clips.FirstOrDefault(c => c.name == name);
                if (found != null)
                {
                    return found;
                }
            }

            throw new InvalidOperationException($"{file}.fbx non ha nessuna di {string.Join(", ", names)}; ha: {string.Join(", ", clips.Select(c => c.name))}");
        }

        // il barbaro con la tinta spenta del borgo, un po' più sporco
        private static Material Skin()
        {
            string path = MaterialsFolder + "/M_Prisoner.mat";
            var source = Load<Material>(Root + "/Art/Materials/M_KayKitBarbarian.mat");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(source) { name = "M_Prisoner" };
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = source.shader;
            material.CopyPropertiesFromMaterial(source);
            material.SetColor("_BaseColor", Dirt);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject PrisonerPrefab()
        {
            var chest = Load<GameObject>(PrefabsFolder + "/Dungeon/Chest.prefab");
            var chestSo = new SerializedObject(chest.GetComponent<Chest>());
            var table = chestSo.FindProperty("_lootTable").objectReferenceValue as LootTable;
            var groundItem = chestSo.FindProperty("_groundItemPrefab").objectReferenceValue as GroundItem;
            var highlight = new SerializedObject(chest.GetComponent<InteractableHighlight>()).FindProperty("_highlightMaterial").objectReferenceValue as Material;
            var stone = chest.GetComponentInChildren<Renderer>(true).sharedMaterials[0];

            var root = new GameObject("Prisoner");
            try
            {
                // il barbaro senza cappello, seduto, con la pelle sporca
                var model = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(Adventurers + "Barbarian.fbx"), root.transform);
                PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                model.name = "Model";
                var hat = model.transform.Find("Barbarian_BearHat");
                if (hat != null)
                {
                    UnityEngine.Object.DestroyImmediate(hat.gameObject);
                }

                var skin = Skin();
                foreach (var renderer in model.GetComponentsInChildren<Renderer>())
                {
                    renderer.sharedMaterial = skin;
                }

                var animator = model.GetComponent<Animator>();
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

                // le catene attorno a lui
                var chains = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(ModelsFolder + "/Chains.fbx"), root.transform);
                chains.name = "Chains";
                foreach (var renderer in chains.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.sharedMaterials = new[] { stone };
                }

                var click = new GameObject("ClickArea") { layer = LayerMask.NameToLayer("Interactable") };
                click.transform.SetParent(root.transform, false);
                var box = click.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0.7f, 0f);
                box.size = new Vector3(1.3f, 1.5f, 1.2f);

                var approach = new GameObject("ApproachPoint");
                approach.transform.SetParent(root.transform, false);
                approach.transform.localPosition = new Vector3(0f, 0f, 1.5f);

                var interactable = root.AddComponent<Interactable>();
                var interactableSo = new SerializedObject(interactable);
                interactableSo.FindProperty("_approachPoint").objectReferenceValue = approach.transform;
                interactableSo.FindProperty("_labelKey").stringValue = "prisoner.label";
                interactableSo.ApplyModifiedPropertiesWithoutUndo();

                var highlightComponent = root.AddComponent<InteractableHighlight>();
                var highlightSo = new SerializedObject(highlightComponent);
                var renderers = model.GetComponentsInChildren<Renderer>(true);
                var list = highlightSo.FindProperty("_renderers");
                list.arraySize = renderers.Length;
                for (int i = 0; i < renderers.Length; i++)
                {
                    list.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
                }

                highlightSo.FindProperty("_highlightMaterial").objectReferenceValue = highlight;
                highlightSo.ApplyModifiedPropertiesWithoutUndo();

                var prisoner = root.AddComponent<Prisoner>();
                var so = new SerializedObject(prisoner);
                so.FindProperty("_animator").objectReferenceValue = animator;
                so.FindProperty("_chains").objectReferenceValue = chains;
                so.FindProperty("_clickArea").objectReferenceValue = box;
                so.FindProperty("_sit").objectReferenceValue = Clip("Rig_Medium_Simulation", "Sit_Floor_Idle");
                so.FindProperty("_cheer").objectReferenceValue = Clip("Rig_Medium_Simulation", "Cheering", "Cheer", "Waving");
                so.FindProperty("_walk").objectReferenceValue = Clip("Rig_Medium_MovementBasic", "Walking_A");
                so.FindProperty("_groundItemPrefab").objectReferenceValue = groundItem;
                so.FindProperty("_potion").objectReferenceValue = table != null ? table.Potion : null;
                so.FindProperty("_gold").objectReferenceValue = table != null ? table.Gold : null;
                so.ApplyModifiedPropertiesWithoutUndo();

                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
