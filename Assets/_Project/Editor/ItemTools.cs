using System.Collections.Generic;
using System.IO;
using System.Linq;
using DarkDescent.Items;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DarkDescent.Editor
{
    /// <summary>
    /// Strumenti per gli oggetti: tiene aggiornato il database e fa le icone dai modelli 3D
    /// (D6 della M4), così un oggetto nuovo ha la sua icona con un click e lo stesso stile degli
    /// altri. Resta nel repo, come LevelMapBuilder.
    /// </summary>
    public static class ItemTools
    {
        public const string ItemsFolder = "Assets/_Project/Data/Items";
        public const string DatabasePath = "Assets/_Project/Data/ItemDatabase.asset";
        public const string AffixesFolder = "Assets/_Project/Data/Affixes";
        public const string AffixDatabasePath = "Assets/_Project/Data/AffixDatabase.asset";
        public const string IconsFolder = "Assets/_Project/Art/Icons";

        /// <summary>Pixel per cella dell'inventario: il doppio di come si vede a 1920×1080, per restare nitide.</summary>
        public const int PixelsPerCell = 128;

        // margine attorno al modello, in frazione del lato
        private const float Padding = 0.08f;

        // lontano da tutto il resto della scena: la camera dell'icona vede solo il modello
        private static readonly Vector3 StagePosition = new Vector3(0f, -1000f, 0f);

        [MenuItem("DarkDescent/Oggetti/Aggiorna il database")]
        public static void UpdateDatabase()
        {
            var database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<ItemDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            var items = FindItems();
            var serialized = new SerializedObject(database);
            var list = serialized.FindProperty("_items");
            list.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log($"[ItemTools] database: {items.Count} oggetti");
            UpdateAffixDatabase();
        }

        /// <summary>Il database degli affissi: le definizioni in Data/Affixes, in ordine di nome.</summary>
        public static void UpdateAffixDatabase()
        {
            var database = AssetDatabase.LoadAssetAtPath<AffixDatabase>(AffixDatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<AffixDatabase>();
                AssetDatabase.CreateAsset(database, AffixDatabasePath);
            }

            var affixes = AssetDatabase.FindAssets("t:AffixDefinition", new[] { AffixesFolder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<AffixDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(affix => affix != null)
                .OrderBy(affix => affix.name)
                .ToList();
            var serialized = new SerializedObject(database);
            var list = serialized.FindProperty("_affixes");
            list.arraySize = affixes.Count;
            for (int i = 0; i < affixes.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = affixes[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log($"[ItemTools] database: {affixes.Count} affissi");
        }

        [MenuItem("DarkDescent/Oggetti/Rigenera le icone")]
        public static void RenderIcons()
        {
            Directory.CreateDirectory(IconsFolder);
            foreach (var item in FindItems())
            {
                if (item.Model == null)
                {
                    Debug.LogWarning($"[ItemTools] {item.name}: senza modello, niente icona");
                    continue;
                }

                string path = $"{IconsFolder}/{item.name}.png";
                File.WriteAllBytes(path, Render(item));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                ConfigureSprite(path);

                var serialized = new SerializedObject(item);
                serialized.FindProperty("_icon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.SaveAssets();
            UpdateDatabase();
        }

        /// <summary>Gli oggetti che si possono trovare: le definizioni in Data/Items, in ordine di nome.</summary>
        public static List<ItemDefinition> FindItems()
        {
            return AssetDatabase.FindAssets("t:ItemDefinition", new[] { ItemsFolder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(item => item != null)
                .OrderBy(item => item.name)
                .ToList();
        }

        private static byte[] Render(ItemDefinition item)
        {
            int width = item.Size.x * PixelsPerCell;
            int height = item.Size.y * PixelsPerCell;

            // una scena di anteprima, separata da quelle aperte: lo strumento non tocca il lavoro in corso
            var stage = EditorSceneManager.NewPreviewScene();
            var model = (GameObject)PrefabUtility.InstantiatePrefab(item.Model, stage);
            var cameraObject = new GameObject("IconCamera");
            var keyLight = new GameObject("KeyLight");
            var fillLight = new GameObject("FillLight");
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            var previousAmbient = RenderSettings.ambientLight;
            var previousMode = RenderSettings.ambientMode;
            try
            {
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, stage);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(keyLight, stage);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fillLight, stage);

                model.transform.SetPositionAndRotation(StagePosition, Quaternion.Euler(item.IconRotation));
                Bounds bounds = BoundsOf(model);

                // luce da in alto a sinistra, come in molte icone, più una luce di riempimento davanti
                AddLight(keyLight, Quaternion.Euler(35f, 25f, 0f), 1.6f);
                AddLight(fillLight, Quaternion.Euler(-10f, 180f + 20f, 0f), 0.6f);

                var camera = cameraObject.AddComponent<Camera>();
                camera.scene = stage;
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.targetTexture = target;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                var cameraData = camera.GetUniversalAdditionalCameraData();
                cameraData.renderPostProcessing = false;
                cameraData.antialiasing = AntialiasingMode.None;

                // la camera guarda il modello di fronte (lungo +Z); l'inquadratura rispetta la forma delle celle
                float aspect = (float)width / height;
                float halfHeight = Mathf.Max(bounds.extents.y, bounds.extents.x / aspect) * (1f + Padding * 2f);
                camera.orthographicSize = halfHeight;
                camera.aspect = aspect;
                cameraObject.transform.SetPositionAndRotation(bounds.center - Vector3.forward * (bounds.extents.z + 5f), Quaternion.identity);

                // l'ambiente della scena aperta (un livello buio) non deve scurire le icone
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);

                // due render: il primo può uscire con gli shader ancora da compilare
                camera.Render();
                camera.Render();

                var previous = RenderTexture.active;
                RenderTexture.active = target;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                RenderTexture.active = previous;
                byte[] png = texture.EncodeToPNG();
                Object.DestroyImmediate(texture);
                return png;
            }
            finally
            {
                RenderSettings.ambientMode = previousMode;
                RenderSettings.ambientLight = previousAmbient;
                target.Release();
                Object.DestroyImmediate(target);
                EditorSceneManager.ClosePreviewScene(stage);
            }
        }

        private static void AddLight(GameObject owner, Quaternion rotation, float intensity)
        {
            owner.transform.rotation = rotation;
            var light = owner.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.shadows = LightShadows.None;
        }

        private static Bounds BoundsOf(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        private static void ConfigureSprite(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
