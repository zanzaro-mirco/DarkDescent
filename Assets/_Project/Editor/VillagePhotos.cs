using System;
using System.IO;
using System.Linq;
using DarkDescent.Core;
using DarkDescent.Levels;
using DarkDescent.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace DarkDescent.Editor
{
    /// <summary>
    /// Le foto del borgo per il sì di Mirco (D14 della M18): la mappa costruita in una scena vuota e
    /// fotografata dall'alto e con l'angolo della camera di gioco, più la fila dei pezzi visti dal lato
    /// della porta. In batchmode, a Unity chiuso:
    /// <c>-executeMethod DarkDescent.Editor.VillagePhotos.Layout -photos-out &lt;cartella&gt;</c>.
    /// </summary>
    public static class VillagePhotos
    {
        public const string MapPath = "Assets/_Project/Data/Levels/Town.txt";

        private const string FolderOption = "-photos-out";

        // la camera di gioco: ortografica, 30° sull'orizzonte, da sud-ovest (Core.unity)
        private const float GamePitch = 30f;
        private const float GameYaw = 45f;
        private const float GameZoom = 9f;

        // la casa della foto della sagoma: la prima 'I', sul lato sud con la porta a nord
        private const char SilhouetteHouse = 'I';

        /// <summary>Prefab e tileset rifatti, poi tutte le foto: il giro intero dopo un ritocco a tabella o mappa.</summary>
        [MenuItem("DarkDescent/Borgo/Prefab e foto")]
        public static void All()
        {
            VillagePrefabs.Build();
            Lineup();
            Layout();
        }

        [MenuItem("DarkDescent/Borgo/Foto della disposizione")]
        public static void Layout()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var tileset = AssetDatabase.LoadAssetAtPath<LevelTileset>(VillagePrefabs.TilesetPath);
            var map = LevelMap.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(MapPath).text);
            new LevelBuilder(tileset).Build(map, activateEnemies: false);

            // nell'editor Awake non parte: le coordinate del terreno si stendono a mano
            foreach (var ground in Object.FindObjectsByType<WorldUv>(FindObjectsSortMode.None))
            {
                ground.Apply();
            }

            var min = LevelMap.CellCenter(0, map.Height - 1) - new Vector3(2f, 0f, 2f);
            var max = LevelMap.CellCenter(map.Width - 1, 0) + new Vector3(2f, 0f, 2f);
            var center = (min + max) * 0.5f;
            float width = max.x - min.x;
            float depth = max.z - min.z;

            // dall'alto, una cella ogni 48 pixel: le etichette si mettono dopo, sulle celle
            Snap("layout_top", 48 * map.Width, 48 * map.Height, center + Vector3.up * 200f, Quaternion.Euler(90f, 0f, 0f), depth * 0.5f);

            // con l'angolo della camera di gioco, tutto il borgo nell'inquadratura
            var rotation = Quaternion.Euler(GamePitch, GameYaw, 0f);
            Snap("layout_game", 1920, 1080, center - rotation * Vector3.forward * 300f, rotation, Mathf.Max(width, depth) * 0.42f);

            // lo stesso, girato di 180°: i lati che la camera di partenza vede da dietro
            var back = Quaternion.Euler(GamePitch, GameYaw + 180f, 0f);
            Snap("layout_game_back", 1920, 1080, center - back * Vector3.forward * 300f, back, Mathf.Max(width, depth) * 0.42f);

            // il cavaliere davanti alla porta di una casa del lato sud, con la camera di gioco: la casa
            // lo copre e si vede la sagoma
            var house = map.Markers.First(m => m.Symbol == SilhouetteHouse);
            var hero = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HeroSilhouette.PlayerPath));
            var spot = LevelMap.CellCenter(house.X, house.Y) + new Vector3(2.2f, 0f, 2.2f);
            hero.transform.SetPositionAndRotation(spot, Quaternion.Euler(0f, 200f, 0f));
            var game = Quaternion.Euler(GamePitch, GameYaw, 0f);
            Snap("silhouette", 1920, 1080, spot + Vector3.up * 1.2f - game * Vector3.forward * 60f, game, GameZoom);
        }

        /// <summary>I pezzi in fila, visti dal loro +Z: il lato della porta, che nella mappa guarda la strada.</summary>
        [MenuItem("DarkDescent/Borgo/Foto dei pezzi")]
        public static void Lineup()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var backdrop = (GameObject)PrefabUtility.InstantiatePrefab(VillagePrefabs.Load("VillageBackdrop"));
            backdrop.transform.position = Vector3.zero;
            backdrop.GetComponentInChildren<WorldUv>().Apply();
            string[] names = { "HouseA_Red", "HouseB_Yellow", "HealerHouse", "Tavern", "Ruin", "Forge", "Chapel", "Well", "Crypt", "CaveMouth" };
            float x = 0f;
            foreach (string name in names)
            {
                var piece = (GameObject)PrefabUtility.InstantiatePrefab(VillagePrefabs.Load(name));
                piece.transform.position = new Vector3(x, 0f, 0f);
                x += 14f;
            }

            var rotation = Quaternion.Euler(20f, 180f, 0f);
            var center = new Vector3((x - 14f) * 0.5f, 3f, 0f);
            Snap("pieces_front", 2400, 600, center - rotation * Vector3.forward * 200f, rotation, 15f);
        }

        private static void Snap(string name, int width, int height, Vector3 position, Quaternion rotation, float halfHeight)
        {
            var cameraObject = new GameObject("PhotoCamera");
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            try
            {
                cameraObject.transform.SetPositionAndRotation(position, rotation);
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = halfHeight;
                camera.aspect = (float)width / height;
                camera.nearClipPlane = 1f;
                camera.farClipPlane = 1000f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
                camera.targetTexture = target;
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;

                // due render: il primo può uscire con gli shader ancora da compilare
                camera.Render();
                camera.Render();

                var previous = RenderTexture.active;
                RenderTexture.active = target;
                var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                RenderTexture.active = previous;
                string path = Path.Combine(Folder(), name + ".png");
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                Debug.Log($"[DarkDescent] foto del borgo: {path}");
            }
            finally
            {
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(cameraObject);
            }
        }

        private static string Folder()
        {
            string folder = CommandLine.TryGetValue(Environment.GetCommandLineArgs(), FolderOption, out string value)
                ? value
                : Path.Combine(Application.dataPath, "..", "Recordings", "Photos", "Village");
            Directory.CreateDirectory(folder);
            return folder;
        }
    }
}
