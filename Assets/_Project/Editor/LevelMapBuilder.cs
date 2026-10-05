using System.Collections.Generic;
using System.IO;
using DarkDescent.Items;
using DarkDescent.Levels;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DarkDescent.Editor
{
    /// <summary>
    /// Costruisce le scene dei livelli dalle mappe di testo in <c>Levels/</c> (D2 della scheda M3),
    /// con lo stesso <see cref="LevelBuilder"/> dei livelli generati, e ne cuoce il NavMesh.
    /// Ogni ricostruzione rifà la scena da zero: i ritocchi a mano vanno nella mappa, o si perdono.
    /// </summary>
    public static class LevelMapBuilder
    {
        private const string MapsFolder = "Assets/_Project/Levels";
        private const string ScenesFolder = "Assets/_Project/Scenes/Levels";
        private const string TilesetPath = "Assets/_Project/Data/Levels/DungeonTileset.asset";
        private const string ItemsFolder = "Assets/_Project/Data/Items";
        private const string CryptScenePath = "Assets/_Project/Scenes/Level_Crypt.unity";
        private const string CryptSettingsPath = "Assets/_Project/Data/Levels/CryptSettings.asset";

        [MenuItem("DarkDescent/Ricostruisci i livelli dalle mappe")]
        public static void BuildAllFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            BuildAll();
        }

        /// <summary>Per <c>-executeMethod</c>: costruisce tutte le mappe e scrive nel log.</summary>
        public static void BuildAll()
        {
            var tileset = AssetDatabase.LoadAssetAtPath<LevelTileset>(TilesetPath);
            var built = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:TextAsset", new[] { MapsFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = Path.GetFileNameWithoutExtension(path);
                var map = LevelMap.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(path).text);
                var scenePath = $"{ScenesFolder}/{name}.unity";
                Build(map, tileset, scenePath);
                built.Add(scenePath);
                Debug.Log($"Livello {name} costruito: {map.Width}×{map.Height} celle, {map.Markers.Count} marcatori.");
            }

            SyncBuildSettings(built);
        }

        [MenuItem("DarkDescent/Ricostruisci la scena della cripta")]
        public static void BuildCryptFromMenu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                BuildCrypt();
            }
        }

        /// <summary>
        /// La scena dei livelli generati (D9 della M6): vuota, con le impostazioni di luce della cripta e
        /// un DungeonLevel che costruisce il livello a runtime. Crea anche i numeri della cripta, se mancano.
        /// </summary>
        public static void BuildCrypt()
        {
            // la scena nuova prima degli asset: aprendola, Unity scarica gli oggetti caricati prima, e i
            // riferimenti salvati resterebbero vuoti
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var settings = AssetDatabase.LoadAssetAtPath<DungeonSettings>(CryptSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<DungeonSettings>();
                AssetDatabase.CreateAsset(settings, CryptSettingsPath);
            }

            var tileset = AssetDatabase.LoadAssetAtPath<LevelTileset>(TilesetPath);
            LevelBuilder.ApplyRenderSettings(tileset);
            var dungeon = new GameObject("Dungeon").AddComponent<DungeonLevel>();
            var so = new SerializedObject(dungeon);
            so.FindProperty("_settings").objectReferenceValue = settings;
            so.FindProperty("_tileset").objectReferenceValue = tileset;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, CryptScenePath);

            // subito dopo Core e la sandbox, prima dei livelli fatti a mano
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == CryptScenePath))
            {
                int index = scenes.FindIndex(s => s.path.StartsWith(ScenesFolder + "/"));
                scenes.Insert(index < 0 ? scenes.Count : index, new EditorBuildSettingsScene(CryptScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Scena della cripta costruita: {CryptScenePath}, numeri in {CryptSettingsPath}.");
        }

        public static void Build(LevelMap map, LevelTileset tileset, string scenePath)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // lo stesso costruttore dei livelli generati (D3 della M6); qui i prefab restano collegati
            var builder = new LevelBuilder(tileset,
                (prefab, parent) => (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent),
                name => AssetDatabase.LoadAssetAtPath<ItemDefinition>($"{ItemsFolder}/{name}.asset"),
                PrefabUtility.RecordPrefabInstancePropertyModifications);
            var context = builder.Build(map);

            var scene = context.gameObject.scene;
            Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
            EditorSceneManager.SaveScene(scene, scenePath);
            BakeNavMesh(context.GetComponentInChildren<NavMeshSurface>(), scenePath);
            EditorSceneManager.SaveScene(scene);
        }

        private static void BakeNavMesh(NavMeshSurface surface, string scenePath)
        {
            surface.BuildNavMesh();
            string folder = Path.Combine(Path.GetDirectoryName(scenePath), Path.GetFileNameWithoutExtension(scenePath)).Replace('\\', '/');
            Directory.CreateDirectory(folder);
            string assetPath = folder + "/NavMesh.asset";
            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(surface.navMeshData, assetPath);
            EditorUtility.SetDirty(surface);
        }

        // Le scene dei livelli nei Build Profiles sono esattamente quelle che hanno una mappa: una
        // mappa cancellata toglie anche la sua scena dalla build. Le altre scene restano in testa.
        private static void SyncBuildSettings(List<string> levelScenes)
        {
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (!s.path.StartsWith(ScenesFolder + "/"))
                {
                    scenes.Add(s);
                }
            }

            levelScenes.Sort(System.StringComparer.Ordinal);
            foreach (var path in levelScenes)
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
