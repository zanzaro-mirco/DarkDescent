using System;
using System.IO;
using DarkDescent.Core;
using DarkDescent.Interaction;
using DarkDescent.Levels;
using DarkDescent.Localization;
using DarkDescent.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.Editor
{
    /// <summary>
    /// La lore (D9 della M25): i tre arredi dei testi (leggio, lapide, carte) con il bagliore tenue di un testo non letto e il click, messi nel set della
    /// cripta e in quello delle caverne; e il pannello di lettura nella scena <c>Core</c>, fatto con la cornice delle opzioni come il pannello dei comandi.
    /// Si rilancia: i prefab si rifanno e il pannello non si duplica. Prima i prefab (<see cref="BuildProps"/>), poi il pannello (<see cref="InstallPanel"/>,
    /// con Unity chiuso: <c>-executeMethod DarkDescent.Editor.LoreTools.InstallPanel</c>).
    /// </summary>
    public static class LoreTools
    {
        private const string Root = "Assets/_Project";
        private const string ModelsFolder = Root + "/Art/Models/DarkDescent";
        private const string PrefabsFolder = Root + "/Prefabs";
        private const string CoreScene = Root + "/Scenes/Core.unity";
        private const string PanelName = "LorePanel";
        private static readonly Color Candle = new Color(1f, 0.72f, 0.38f);

        /// <summary>Tutto insieme, per il batchmode: la leva con il suo tintinnio, gli arredi e il pannello.</summary>
        public static void InstallAll()
        {
            SealedRoomTools.Build();
            BuildProps();
            InstallPanel();
        }

        [MenuItem("DarkDescent/Segreti/Lore: crea gli arredi")]
        public static void BuildProps()
        {
            var chest = Load<GameObject>(PrefabsFolder + "/Dungeon/Chest.prefab");
            var stone = chest.GetComponentInChildren<Renderer>(true).sharedMaterials[0];
            var highlight = new SerializedObject(chest.GetComponent<InteractableHighlight>()).FindProperty("_highlightMaterial").objectReferenceValue as Material;

            var lectern = Prop("Lectern", "Lectern", "lore.lectern", stone, highlight, new Vector3(0f, 0.8f, 0f), new Vector3(1.3f, 1.7f, 1.2f));
            var tablet = Prop("Tablet", "Tablet", "lore.tablet", stone, highlight, new Vector3(0f, 0.9f, 0f), new Vector3(1.5f, 1.9f, 0.9f));
            var book = Prop("Book", "Books", "lore.book", stone, highlight, new Vector3(0f, 0.3f, 0f), new Vector3(1.4f, 0.9f, 1.2f));
            foreach (string tilesetName in new[] { "DungeonTileset", "CaveTileset" })
            {
                var tileset = Load<LevelTileset>($"{Root}/Data/Levels/{tilesetName}.asset");
                var so = new SerializedObject(tileset);
                so.FindProperty("_lectern").objectReferenceValue = lectern;
                so.FindProperty("_tablet").objectReferenceValue = tablet;
                so.FindProperty("_book").objectReferenceValue = book;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(tileset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DarkDescent] Lore: leggio, lapide e carte pronti nei set della cripta e delle caverne");
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

        // un arredo: il modello (il materiale dei KayKit), il click davanti, un bagliore di candela e il componente che lo legge
        private static GameObject Prop(string prefabName, string modelName, string labelKey, Material stone, Material highlight, Vector3 clickCenter, Vector3 clickSize)
        {
            var root = new GameObject("Lore" + prefabName);
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>($"{ModelsFolder}/{modelName}.fbx"), root.transform);
                model.name = "Model";
                foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.sharedMaterials = new[] { stone };
                }

                var click = new GameObject("ClickArea") { layer = LayerMask.NameToLayer("Interactable") };
                click.transform.SetParent(root.transform, false);
                var box = click.AddComponent<BoxCollider>();
                box.center = clickCenter;
                box.size = clickSize;

                var approach = new GameObject("ApproachPoint");
                approach.transform.SetParent(root.transform, false);
                approach.transform.localPosition = new Vector3(0f, 0f, 1.6f);

                var glowObject = new GameObject("Glow");
                glowObject.transform.SetParent(root.transform, false);
                glowObject.transform.localPosition = new Vector3(0f, 1.3f, 0.4f);
                var glow = glowObject.AddComponent<Light>();
                glow.type = LightType.Point;
                glow.color = Candle;
                glow.intensity = 1.6f;
                glow.range = 4.2f;
                glow.shadows = LightShadows.None;

                var interactable = root.AddComponent<Interactable>();
                var interactableSo = new SerializedObject(interactable);
                interactableSo.FindProperty("_approachPoint").objectReferenceValue = approach.transform;
                interactableSo.FindProperty("_labelKey").stringValue = labelKey;
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

                var lore = root.AddComponent<LoreSpot>();
                var loreSo = new SerializedObject(lore);
                loreSo.FindProperty("_glow").objectReferenceValue = glow;
                loreSo.ApplyModifiedPropertiesWithoutUndo();

                return PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabsFolder}/Lore{prefabName}.prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [MenuItem("DarkDescent/UI/Metti il pannello di lettura")]
        public static void InstallPanel()
        {
            var scene = EditorSceneManager.OpenScene(CoreScene, OpenSceneMode.Single);
            var hud = HudTools.FindHud(scene);
            var panel = BuildPanel(hud);
            var root = UnityEngine.Object.FindFirstObjectByType<CompositionRoot>(FindObjectsInactive.Include);
            var rootSo = new SerializedObject(root);
            rootSo.FindProperty("_lorePanel").objectReferenceValue = panel;
            rootSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("LoreTools: pannello di lettura installato nella scena Core.");
        }

        // la cornice delle opzioni, copiata e ripulita: la pelle, il titolo e Indietro (che diventa Chiudi); in mezzo il testo
        private static LorePanel BuildPanel(Transform hud)
        {
            var old = hud.Find(PanelName);
            if (old != null)
            {
                UnityEngine.Object.DestroyImmediate(old.gameObject);
            }

            var options = UnityEngine.Object.FindFirstObjectByType<OptionsPanel>(FindObjectsInactive.Include);
            var optionsRoot = (GameObject)new SerializedObject(options).FindProperty("_root").objectReferenceValue;
            var enemyBar = UnityEngine.Object.FindFirstObjectByType<EnemyBar>(FindObjectsInactive.Include);
            var font = ((TMP_Text)new SerializedObject(enemyBar).FindProperty("_name").objectReferenceValue).font;

            var holder = HudTools.Node(hud, PanelName);
            HudTools.Stretch(holder, Vector2.zero, Vector2.zero);
            var death = hud.Find("DeathScreen");
            if (death != null)
            {
                holder.SetSiblingIndex(death.GetSiblingIndex());
            }

            var copy = UnityEngine.Object.Instantiate(optionsRoot, holder);
            copy.name = "Root";
            copy.SetActive(true);
            HudTools.Stretch((RectTransform)copy.transform, Vector2.zero, Vector2.zero);

            var box = (RectTransform)copy.transform.Find("Box");
            var children = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in box)
            {
                children.Add(child);
            }

            foreach (var child in children)
            {
                if (child.name != "SkinFrame" && child.name != "Title" && child.name != "Back")
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }

            box.sizeDelta = new Vector2(1000f, 640f);
            var title = (RectTransform)box.Find("Title");
            title.anchoredPosition = new Vector2(0f, 250f);
            title.sizeDelta = new Vector2(860f, 56f);
            var titleText = title.GetComponent<TMP_Text>();
            DestroyKey(title.gameObject);

            var close = (RectTransform)box.Find("Back");
            close.name = "Close";
            close.anchoredPosition = new Vector2(0f, -258f);
            close.sizeDelta = new Vector2(320f, 50f);
            var closeText = close.GetComponentInChildren<TMP_Text>(true);
            DestroyKey(closeText.gameObject);
            // «Chiudi»: la sua chiave
            var closeKey = closeText.gameObject.AddComponent<LocalizedText>();
            var closeKeySo = new SerializedObject(closeKey);
            closeKeySo.FindProperty("_key").stringValue = "lore.close";
            closeKeySo.ApplyModifiedPropertiesWithoutUndo();

            var body = HudTools.Text(box, "Body", font, 30f, new Color(0.91f, 0.89f, 0.83f, 1f), TextAlignmentOptions.TopLeft, 340f);
            var layout = body.GetComponent<LayoutElement>();
            if (layout != null)
            {
                UnityEngine.Object.DestroyImmediate(layout);
            }

            HudTools.Place((RectTransform)body.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, 190f), new Vector2(860f, 380f));
            body.lineSpacing = 8f;

            var view = holder.gameObject.AddComponent<LorePanel>();
            var so = new SerializedObject(view);
            so.FindProperty("_root").objectReferenceValue = copy;
            so.FindProperty("_title").objectReferenceValue = titleText;
            so.FindProperty("_body").objectReferenceValue = body;
            so.FindProperty("_close").objectReferenceValue = close.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static void DestroyKey(GameObject go)
        {
            var key = go.GetComponent<LocalizedText>();
            if (key != null)
            {
                UnityEngine.Object.DestroyImmediate(key);
            }
        }
    }
}
