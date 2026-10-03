using System.Collections.Generic;
using System.IO;
using DarkDescent.Interaction;
using DarkDescent.Levels;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace DarkDescent.Editor
{
    /// <summary>
    /// Costruisce le scene dei livelli dalle mappe di testo in <c>Levels/</c> (D2 della scheda M3).
    /// Ogni ricostruzione rifà la scena da zero: i ritocchi a mano vanno nella mappa, o si perdono.
    /// </summary>
    public static class LevelMapBuilder
    {
        private const string MapsFolder = "Assets/_Project/Levels";
        private const string ScenesFolder = "Assets/_Project/Scenes/Levels";
        private const string TilesetPath = "Assets/_Project/Data/Levels/DungeonTileset.asset";

        private const char EntranceSymbol = '<';
        private const char StairsDownSymbol = '>';
        private const char TorchSymbol = 'T';

        // la cima della scala sta appena sopra il pavimento (0,05 m), il resto scende nel buio
        private const float StairsHeight = 5.1f;
        private const float FloorTop = 0.05f;

        private const float WallHalfThickness = 0.5f;
        private const float TorchHeight = 2.2f;

        // Il trigger dell'uscita copre la cella della scala e sconfina nella cella da cui si arriva:
        // il NavMesh finisce mezzo metro prima del bordo (raggio dell'agent), il player deve entrarci
        // fermandosi lì. Il punto d'arrivo del click sta appena dentro il NavMesh.
        private const float ExitTriggerReach = 1.5f;
        private const float ApproachDistance = 2.8f;

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

        public static void Build(LevelMap map, LevelTileset tileset, string scenePath)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var level = new GameObject("Level");
            var context = level.AddComponent<LevelContext>();

            var floors = new GameObject("Floors").transform;
            var walls = CreateNotWalkableGroup("Walls");
            var props = CreateNotWalkableGroup("Props");
            var torches = new GameObject("Torches").transform;
            var enemies = new GameObject("Enemies").transform;
            foreach (var group in new[] { floors, walls, props, torches, enemies })
            {
                group.SetParent(level.transform, false);
            }

            BuildFloors(map, tileset, floors);
            BuildWalls(map, tileset, walls);
            BuildMarkers(map, tileset, level.transform, props, torches, enemies);

            var navMesh = new GameObject("NavMesh");
            navMesh.transform.SetParent(level.transform, false);
            var surface = navMesh.AddComponent<NavMeshSurface>();
            ConfigureSurface(surface);

            // provvisoria fino al passo 3.6, che porta il buio e le torce
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
            EditorSceneManager.SaveScene(scene, scenePath);

            BakeNavMesh(surface, scenePath);
            EditorSceneManager.SaveScene(scene);
        }

        private static Transform CreateNotWalkableGroup(string name)
        {
            var go = new GameObject(name);
            var modifier = go.AddComponent<NavMeshModifier>();
            modifier.overrideArea = true;
            modifier.area = NavMesh.GetAreaFromName("Not Walkable");
            modifier.applyToChildren = true;
            return go.transform;
        }

        private static void BuildFloors(LevelMap map, LevelTileset tileset, Transform parent)
        {
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    // dove c'è la scala che scende il pavimento manca: si scende nel buio
                    if (map.IsFloor(x, y) && map.GetSymbol(x, y) != StairsDownSymbol)
                    {
                        Place(tileset.Floor, parent, LevelMap.CellCenter(x, y), Quaternion.identity);
                    }
                }
            }
        }

        // Nord ed est sono lontani dalla camera, che guarda da sud-ovest: lì i muri alti. A sud e a
        // ovest i muri bassi, che non coprono il cavaliere e non fermano i click (D5, ADR-006).
        private static void BuildWalls(LevelMap map, LevelTileset tileset, Transform parent)
        {
            foreach (var (x, y, side) in map.BoundaryEdges())
            {
                bool far = side == MapDirection.North || side == MapDirection.East;
                var prefab = far ? tileset.Wall : tileset.LowWall;
                Vector3 position = LevelMap.CellCenter(x, y) + LevelMap.ToWorld(side) * (LevelMap.CellSize * 0.5f);

                // i moduli di muro sono lunghi lungo X: ruotati di 90° per i lati est e ovest
                var rotation = side == MapDirection.North || side == MapDirection.South
                    ? Quaternion.identity
                    : Quaternion.Euler(0f, 90f, 0f);
                Place(prefab, parent, position, rotation);
            }
        }

        private static void BuildMarkers(LevelMap map, LevelTileset tileset, Transform level, Transform props, Transform torches, Transform enemies)
        {
            int enemyCount = 0;
            foreach (var marker in map.Markers)
            {
                Vector3 center = LevelMap.CellCenter(marker.X, marker.Y);
                switch (marker.Symbol)
                {
                    case EntranceSymbol:
                    {
                        var entranceId = map.GetDirective("entrance");
                        var go = new GameObject("Entrance");
                        go.transform.SetParent(level, false);
                        go.transform.SetPositionAndRotation(center, Quaternion.LookRotation(Vector3.back));
                        var entrance = go.AddComponent<LevelEntrance>();
                        SetString(entrance, "_id", entranceId.Count > 0 ? entranceId[0] : "Start");
                        go.name = "Entrance_" + entrance.Id;
                        break;
                    }
                    case StairsDownSymbol:
                        PlaceStairsDown(map, tileset, level, marker);
                        break;
                    case TorchSymbol:
                        PlaceTorch(map, tileset, torches, marker);
                        break;
                    default:
                    {
                        var prefab = tileset.GetMarkerPrefab(marker.Symbol);
                        if (prefab == null)
                        {
                            Debug.LogError($"Simbolo '{marker.Symbol}' in ({marker.X}, {marker.Y}) senza prefab nel tileset.");
                            break;
                        }

                        bool isEnemy = prefab.GetComponentInChildren<Enemies.EnemyAI>() != null;
                        var placed = Place(prefab, isEnemy ? enemies : props, center, Quaternion.LookRotation(Vector3.back));
                        if (isEnemy)
                        {
                            placed.name = $"{prefab.name}_{++enemyCount:00}";
                        }

                        break;
                    }
                }
            }
        }

        // da dove si preferisce arrivare: da sud la scala scende verso nord, lontano dalla camera, e
        // la parte sotto il pavimento finisce dietro il muro alto invece di spuntare nel vuoto
        private static readonly MapDirection[] StairsEntryOrder = { MapDirection.South, MapDirection.West, MapDirection.East, MapDirection.North };

        // La scala sale verso +Z del modello: la cima va verso il pavimento da cui si arriva, e tutto
        // il resto scende sotto il livello del pavimento.
        private static void PlaceStairsDown(LevelMap map, LevelTileset tileset, Transform level, MapMarker marker)
        {
            Vector3 toEntry = Vector3.back;
            foreach (var side in StairsEntryOrder)
            {
                Vector3 dir = LevelMap.ToWorld(side);
                if (map.IsFloor(marker.X + Mathf.RoundToInt(dir.x), marker.Y - Mathf.RoundToInt(dir.z)))
                {
                    toEntry = dir;
                    break;
                }
            }

            Vector3 position = LevelMap.CellCenter(marker.X, marker.Y) - toEntry * (LevelMap.CellSize * 0.5f);
            position.y = FloorTop - StairsHeight;
            var stairs = Place(tileset.StairsDown, level, position, Quaternion.LookRotation(toEntry));
            stairs.name = "StairsDown";

            var exitInfo = map.GetDirective("exit");
            if (exitInfo.Count < 2)
            {
                Debug.LogError("Scala senza direttiva @exit <scena> <ingresso>: non porta da nessuna parte.");
                return;
            }

            CreateExit(level, LevelMap.CellCenter(marker.X, marker.Y), toEntry, exitInfo[0], exitInfo[1]);
        }

        // L'uscita guarda verso la cella da cui si arriva (+Z locale = verso l'ingresso della scala).
        private static void CreateExit(Transform level, Vector3 cellCenter, Vector3 toEntry, string targetScene, string targetEntrance)
        {
            var go = new GameObject("Exit");
            go.transform.SetParent(level, false);
            go.transform.SetPositionAndRotation(cellCenter, Quaternion.LookRotation(toEntry));

            float half = LevelMap.CellSize * 0.5f;
            var trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1f, ExitTriggerReach * 0.5f);
            trigger.size = new Vector3(LevelMap.CellSize, 2f, LevelMap.CellSize + ExitTriggerReach);
            go.AddComponent<Rigidbody>().isKinematic = true;

            var exit = go.AddComponent<LevelExit>();
            SetString(exit, "_targetScene", targetScene);
            SetString(exit, "_targetEntrance", targetEntrance);

            // il collider del click è separato dal trigger: il raggio del click ignora i trigger
            var click = new GameObject("ClickArea");
            click.layer = LayerMask.NameToLayer("Interactable");
            click.transform.SetParent(go.transform, false);
            var clickBox = click.AddComponent<BoxCollider>();
            clickBox.center = new Vector3(0f, -0.05f, 0f);
            clickBox.size = new Vector3(LevelMap.CellSize, 0.2f, LevelMap.CellSize);

            var approach = new GameObject("ApproachPoint").transform;
            approach.SetParent(go.transform, false);
            approach.localPosition = new Vector3(0f, 0f, ApproachDistance);
            var interactable = go.AddComponent<Interactable>();
            var so = new SerializedObject(interactable);
            so.FindProperty("_approachPoint").objectReferenceValue = approach;
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Assert(ApproachDistance > half && ApproachDistance < half + ExitTriggerReach, "il punto d'arrivo deve stare nel trigger");
        }

        // La torcia va sul muro alto della sua cella, a nord o a est: sui muri bassi non c'è dove appenderla.
        private static void PlaceTorch(LevelMap map, LevelTileset tileset, Transform parent, MapMarker marker)
        {
            MapDirection side;
            if (!map.IsFloor(marker.X, marker.Y - 1))
            {
                side = MapDirection.North;
            }
            else if (!map.IsFloor(marker.X + 1, marker.Y))
            {
                side = MapDirection.East;
            }
            else
            {
                Debug.LogError($"Torcia in ({marker.X}, {marker.Y}) senza muro alto a nord o a est.");
                return;
            }

            Vector3 outward = LevelMap.ToWorld(side);
            Vector3 position = LevelMap.CellCenter(marker.X, marker.Y)
                + outward * (LevelMap.CellSize * 0.5f - WallHalfThickness)
                + Vector3.up * TorchHeight;

            // il modello sporge lungo il suo +Z: verso l'interno della stanza
            Place(tileset.WallTorch, parent, position, Quaternion.LookRotation(-outward));
        }

        private static GameObject Place(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.SetPositionAndRotation(position, rotation);
            return go;
        }

        private static void SetString(Object target, string property, string value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Le stesse impostazioni della sandbox (M1): solo i layer dell'ambiente, height mesh per non
        // galleggiare, gli agent ignorati perché il player non si ritagli un buco.
        private static void ConfigureSurface(NavMeshSurface surface)
        {
            surface.collectObjects = CollectObjects.All;
            surface.layerMask = LayerMask.GetMask("Ground", "Obstacle");
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.ignoreNavMeshAgent = true;
            surface.ignoreNavMeshObstacle = true;
            surface.minRegionArea = 2f;
            surface.buildHeightMesh = true;
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
