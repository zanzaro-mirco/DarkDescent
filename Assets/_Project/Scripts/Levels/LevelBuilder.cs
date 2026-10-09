using System;
using DarkDescent.Enemies;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Localization;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Costruisce un livello da una <see cref="LevelMap"/> nella scena attiva (D3 della scheda M6):
    /// pavimenti, muri, marcatori, atmosfera e la superficie del NavMesh. È lo stesso per le mappe a
    /// mano, costruite dall'editor, e per i livelli generati a runtime. Non cuoce il NavMesh: lo fa
    /// chi lo chiama, salvandolo come asset nell'editor o in memoria a runtime.
    /// </summary>
    public sealed class LevelBuilder
    {
        /// <summary>Il gruppo dei nemici, figlio della radice del livello.</summary>
        public const string EnemiesGroup = "Enemies";

        private const char EntranceSymbol = '<';
        private const char StairsDownSymbol = '>';
        private const char TorchSymbol = 'T';
        private const char ItemSymbol = 'i';
        private const string StairsOccluder = "Occluder";

        private const float WallHalfThickness = 0.5f;
        private const float TorchHeight = 2.2f;

        // la torcia di un muro che diventa basso, quando sul muro di fronte non trova posto: sotto
        // la cima del muro basso, che è sotto 1,2 m
        private const float LowTorchHeight = 0.75f;

        // quante celle una torcia cerca il muro di fronte, nella stessa stanza
        private const int MirrorReach = 12;

        // Il trigger dell'uscita copre la cella della scala e sconfina nella cella da cui si arriva:
        // il NavMesh finisce mezzo metro prima del bordo (raggio dell'agent), il player deve entrarci
        // fermandosi lì. Il punto d'arrivo del click sta appena dentro il NavMesh.
        private const float ExitTriggerReach = 1.5f;
        private const float ApproachDistance = 2.8f;

        // l'area del click va da appena sotto il pavimento alla cima delle balaustre
        private const float ClickAreaBottom = -0.15f;
        private const float ClickAreaTop = 1.15f;

        // da dove si preferisce arrivare: da sud la scala scende verso nord, lontano dalla camera di
        // partenza, e la parte sotto il pavimento finisce dietro il muro alto invece di spuntare nel vuoto
        private static readonly MapDirection[] StairsEntryOrder = { MapDirection.South, MapDirection.West, MapDirection.East, MapDirection.North };

        private readonly LevelTileset _tileset;
        private readonly Func<GameObject, Transform, GameObject> _instantiate;
        private readonly Func<string, ItemDefinition> _findItem;
        private readonly Action<Object> _modified;

        /// <param name="instantiate">Come si istanzia un prefab sotto un padre: l'editor tiene il legame con il prefab, a runtime basta <c>Instantiate</c>.</param>
        /// <param name="findItem">Dal nome della direttiva @items alla definizione; null se il livello non ha oggetti a terra.</param>
        /// <param name="modified">Avvisato quando cambia un campo di un'istanza di prefab: l'editor deve registrarlo, o al salvataggio si perde.</param>
        public LevelBuilder(LevelTileset tileset, Func<GameObject, Transform, GameObject> instantiate = null,
            Func<string, ItemDefinition> findItem = null, Action<Object> modified = null)
        {
            _tileset = tileset;
            _instantiate = instantiate ?? ((prefab, parent) => Object.Instantiate(prefab, parent));
            _findItem = findItem;
            _modified = modified;
        }

        /// <summary>
        /// Costruisce il livello e restituisce il suo contesto, alla radice. Tutto nasce sotto una radice
        /// spenta e si accende alla fine: a runtime gli Awake partono a livello finito, e il contesto
        /// trova ingressi, uscite e nemici (trappola 1 della M6).
        /// </summary>
        /// <param name="activateEnemies">False a runtime: il gruppo dei nemici resta spento finché non c'è
        /// il NavMesh, altrimenti i loro agent nascono senza appoggio.</param>
        public LevelContext Build(LevelMap map, bool activateEnemies = true)
        {
            var level = new GameObject("Level");
            level.SetActive(false);
            var context = level.AddComponent<LevelContext>();
            var depth = map.GetDirective("depth");
            context.Configure(depth.Count > 0 ? int.Parse(depth[0]) : 1, map, _tileset);

            var floors = new GameObject("Floors").transform;
            var walls = CreateNotWalkableGroup("Walls");
            var props = CreateNotWalkableGroup("Props");
            var torches = new GameObject("Torches").transform;
            var enemies = new GameObject(EnemiesGroup).transform;
            enemies.gameObject.SetActive(activateEnemies);
            foreach (var group in new[] { floors, walls, props, torches, enemies })
            {
                group.SetParent(level.transform, false);
            }

            // ogni lato ha la versione alta e quella bassa di muri e torce; WallView accende quella
            // giusta per la camera (D12 della M8)
            var view = level.AddComponent<WallView>();
            var wallSides = new SideGroups(walls, view);
            var torchSides = new SideGroups(torches, view);

            BuildFloors(map, floors);
            BuildWalls(map, wallSides);
            BuildMarkers(map, level.transform, props, torchSides, enemies);

            var navMesh = new GameObject("NavMesh");
            navMesh.transform.SetParent(level.transform, false);
            ConfigureSurface(navMesh.AddComponent<NavMeshSurface>());

            ApplyAtmosphere(level.transform);
            level.SetActive(true);
            return context;
        }

        // Niente sole e niente cielo: ambiente quasi nero a colore unico, il resto lo fanno torce,
        // scala e cavaliere (D4 della M3). Valgono solo se il livello è la scena attiva: lo fa il LevelManager.
        /// <summary>Le impostazioni di luce della scena attiva: anche per la scena della cripta, costruita vuota.</summary>
        public static void ApplyRenderSettings(LevelTileset tileset)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = tileset.AmbientColor;
            RenderSettings.skybox = null;
            RenderSettings.sun = null;
            RenderSettings.fog = false;

            // senza cielo anche i riflessi devono essere neri, o i materiali luccicano nel buio
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = null;
        }

        private void ApplyAtmosphere(Transform level)
        {
            ApplyRenderSettings(_tileset);

            var post = new GameObject("PostProcessing");
            post.transform.SetParent(level, false);
            var volume = post.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = _tileset.PostProcessing;
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

        private void BuildFloors(LevelMap map, Transform parent)
        {
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    // dove c'è la scala che scende il pavimento manca: si scende nel buio
                    if (map.IsFloor(x, y) && map.GetSymbol(x, y) != StairsDownSymbol)
                    {
                        Place(Pick(_tileset.Floor, _tileset.FloorVariant, x, y, 0), parent, LevelMap.CellCenter(x, y), Quaternion.identity);
                    }
                }
            }
        }

        // Su ogni lato sia il muro alto sia quello basso: alti i lati lontani dalla camera, bassi
        // quelli vicini, che non coprono il cavaliere e non fermano i click (ADR-017, ADR-006). Quali
        // siano lo decide la rotazione della camera (D12 della M8).
        private void BuildWalls(LevelMap map, SideGroups sides)
        {
            foreach (var (x, y, side) in map.BoundaryEdges())
            {
                Vector3 position = LevelMap.CellCenter(x, y) + LevelMap.ToWorld(side) * (LevelMap.CellSize * 0.5f);

                // i moduli di muro sono lunghi lungo X: ruotati di 90° per i lati est e ovest
                var rotation = side == MapDirection.North || side == MapDirection.South
                    ? Quaternion.identity
                    : Quaternion.Euler(0f, 90f, 0f);
                Place(Pick(_tileset.Wall, _tileset.WallVariant, x, y, 1 + (int)side), sides.High(side), position, rotation);
                Place(_tileset.LowWall, sides.Low(side), position, rotation);
            }
        }

        // La variante su una parte dei pezzi, scelta da una mescola della cella e del lato: niente
        // generatore di numeri, quindi lo stesso livello ha gli stessi pezzi anche costruito dall'editor.
        private GameObject Pick(GameObject basePrefab, GameObject variant, int x, int y, int salt)
        {
            if (variant == null)
            {
                return basePrefab;
            }

            uint hash = unchecked((uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(salt * 83492791));
            hash ^= hash >> 13;
            hash = unchecked(hash * 0x5bd1e995);
            hash ^= hash >> 15;
            return (hash % 1000) < _tileset.VariantChance * 1000f ? variant : basePrefab;
        }

        private void BuildMarkers(LevelMap map, Transform level, Transform props, SideGroups torches, Transform enemies)
        {
            int enemyCount = 0;

            // gli oggetti a terra prendono, in ordine di lettura della mappa, i nomi di @items
            var itemNames = map.GetDirective("items");
            int itemCount = 0;
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
                        entrance.Configure(entranceId.Count > 0 ? entranceId[0] : "Start");
                        go.name = "Entrance_" + entrance.Id;
                        break;
                    }
                    case StairsDownSymbol:
                        PlaceStairsDown(map, level, torches, marker);
                        break;
                    case TorchSymbol:
                        PlaceTorch(map, torches, marker);
                        break;
                    case ItemSymbol:
                        PlaceGroundItem(props, center, itemCount < itemNames.Count ? itemNames[itemCount] : null);
                        itemCount++;
                        break;
                    default:
                    {
                        var prefab = _tileset.GetMarkerPrefab(marker.Symbol);
                        if (prefab == null)
                        {
                            Debug.LogError($"Simbolo '{marker.Symbol}' in ({marker.X}, {marker.Y}) senza prefab nel tileset.");
                            break;
                        }

                        bool isEnemy = prefab.GetComponentInChildren<EnemyAI>() != null;
                        var facing = isEnemy ? Vector3.back : AwayFromWall(map, marker);
                        var placed = Place(prefab, isEnemy ? enemies : props, center, Quaternion.LookRotation(facing));
                        if (isEnemy)
                        {
                            placed.name = $"{prefab.name}_{++enemyCount:00}";
                        }

                        break;
                    }
                }
            }
        }

        // Un oggetto di scena contro un muro guarda la stanza: una cassa si apre dal davanti, e lì il
        // cavaliere deve poter arrivare. Senza muri accanto guarda la camera, come prima della M6.
        private static Vector3 AwayFromWall(LevelMap map, MapMarker marker)
        {
            if (!map.IsFloor(marker.X, marker.Y - 1)) return Vector3.back;
            if (!map.IsFloor(marker.X, marker.Y + 1)) return Vector3.forward;
            if (!map.IsFloor(marker.X - 1, marker.Y)) return Vector3.right;
            if (!map.IsFloor(marker.X + 1, marker.Y)) return Vector3.left;
            return Vector3.back;
        }

        // Il prefab della scala ha l'origine al centro della cella, all'altezza del pavimento, e il +Z
        // verso il pavimento da cui si arriva: lì c'è la cima, il resto scende sotto il livello del
        // pavimento. Balaustre, pilastrini e bagliore sono già nel prefab.
        private void PlaceStairsDown(LevelMap map, Transform level, SideGroups decor, MapMarker marker)
        {
            Vector3 toEntry = Vector3.back;
            var entrySide = MapDirection.South;
            foreach (var side in StairsEntryOrder)
            {
                Vector3 dir = LevelMap.ToWorld(side);
                if (IsFloorToward(map, marker, dir))
                {
                    toEntry = dir;
                    entrySide = side;
                    break;
                }
            }

            Vector3 cellCenter = LevelMap.CellCenter(marker.X, marker.Y);
            var stairs = Place(_tileset.StairsDown, level, cellCenter, Quaternion.LookRotation(toEntry));
            stairs.name = "StairsDown";

            // lo stendardo va sul muro oltre la scala e si vede quando quel muro è alto: sul muro
            // basso non c'è dove appenderlo, e la scala resta senza
            Vector3 far = -toEntry;
            if (!IsFloorToward(map, marker, far))
            {
                Vector3 wallFace = cellCenter + far * (LevelMap.CellSize * 0.5f - WallHalfThickness);
                Place(_tileset.ExitBanner, decor.High(Opposite(entrySide)), wallFace, Quaternion.LookRotation(toEntry)).name = "ExitBanner";
            }

            var exitInfo = map.GetDirective("exit");
            if (exitInfo.Count < 2)
            {
                Debug.LogError("Scala senza direttiva @exit <scena> <ingresso>: non porta da nessuna parte.");
                return;
            }

            // la profondità di arrivo: dalla direttiva se c'è (livelli generati), altrimenti la successiva
            var depth = map.GetDirective("depth");
            int targetDepth = exitInfo.Count > 2 ? int.Parse(exitInfo[2]) : (depth.Count > 0 ? int.Parse(depth[0]) : 1) + 1;
            var exit = CreateExit(level, cellCenter, toEntry, exitInfo[0], exitInfo[1], targetDepth);
            AddHighlight(exit, stairs);
        }

        private static bool IsFloorToward(LevelMap map, MapMarker marker, Vector3 dir)
        {
            return map.IsFloor(marker.X + Mathf.RoundToInt(dir.x), marker.Y - Mathf.RoundToInt(dir.z));
        }

        // Sotto il cursore si accendono la scala e le balaustre, e il bagliore si alza. Lo stendardo
        // resta com'è: acceso d'ambra perderebbe il rosso che lo fa riconoscere da lontano. L'anello
        // nero sotto i pavimenti resta nero: nasconde la parte sepolta del pozzo, che accesa si
        // vedrebbe attraverso le celle di roccia.
        private void AddHighlight(GameObject exit, GameObject stairs)
        {
            var occluder = stairs.transform.Find(StairsOccluder);
            var renderers = Array.FindAll(stairs.GetComponentsInChildren<Renderer>(), r => occluder == null || !r.transform.IsChildOf(occluder));
            exit.AddComponent<InteractableHighlight>().Configure(renderers, _tileset.HighlightMaterial, stairs.GetComponentInChildren<Light>());
        }

        // L'uscita guarda verso la cella da cui si arriva (+Z locale = verso l'ingresso della scala).
        private static GameObject CreateExit(Transform level, Vector3 cellCenter, Vector3 toEntry, string targetScene, string targetEntrance, int targetDepth)
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

            go.AddComponent<LevelExit>().Configure(targetScene, targetEntrance, targetDepth);

            // il collider del click è separato dal trigger: il raggio del click ignora i trigger.
            // Alto quanto le balaustre, così il cursore prende la scala anche passando sopra di loro.
            var click = new GameObject("ClickArea");
            click.layer = LayerMask.NameToLayer("Interactable");
            click.transform.SetParent(go.transform, false);
            var clickBox = click.AddComponent<BoxCollider>();
            clickBox.center = new Vector3(0f, (ClickAreaTop + ClickAreaBottom) * 0.5f, 0f);
            clickBox.size = new Vector3(LevelMap.CellSize, ClickAreaTop - ClickAreaBottom, LevelMap.CellSize);

            var approach = new GameObject("ApproachPoint").transform;
            approach.SetParent(go.transform, false);
            approach.localPosition = new Vector3(0f, 0f, ApproachDistance);
            // "Descend to level {0}": il numero è la profondità di arrivo
            go.AddComponent<Interactable>().Configure(approach, TextKeys.ExitDescend, targetDepth.ToString());

            Debug.Assert(ApproachDistance > half && ApproachDistance < half + ExitTriggerReach, "il punto d'arrivo deve stare nel trigger");
            return go;
        }

        // La torcia va sul muro della sua cella, a nord, a est, a sud o a ovest, il primo che c'è.
        // Quando la camera gira e quel muro diventa basso, la stanza resta illuminata da una torcia
        // sul muro di fronte, nella stessa colonna; se di fronte non c'è un muro della stessa stanza,
        // la torcia scende sul muro basso (D12 della M8).
        private void PlaceTorch(LevelMap map, SideGroups sides, MapMarker marker)
        {
            if (!TryWallSide(map, marker.X, marker.Y, out var home))
            {
                Debug.LogError($"Torcia in ({marker.X}, {marker.Y}) senza un muro a cui appenderla.");
                return;
            }

            PlaceTorchOn(sides.High(home), marker.X, marker.Y, home, TorchHeight);
            var across = Opposite(home);
            if (TryFindFacingWall(map, marker.X, marker.Y, across, out var cell))
            {
                PlaceTorchOn(sides.High(across), cell.x, cell.y, across, TorchHeight);
            }
            else
            {
                PlaceTorchOn(sides.Low(home), marker.X, marker.Y, home, LowTorchHeight);
            }
        }

        private void PlaceTorchOn(Transform parent, int x, int y, MapDirection side, float height)
        {
            Vector3 outward = LevelMap.ToWorld(side);
            Vector3 position = LevelMap.CellCenter(x, y)
                + outward * (LevelMap.CellSize * 0.5f - WallHalfThickness)
                + Vector3.up * height;

            // il modello sporge lungo il suo +Z: verso l'interno della stanza
            Place(_tileset.WallTorch, parent, position, Quaternion.LookRotation(-outward));
        }

        private static bool TryWallSide(LevelMap map, int x, int y, out MapDirection side)
        {
            for (int i = 0; i < 4; i++)
            {
                side = (MapDirection)i;
                var step = Step(side);
                if (!map.IsFloor(x + step.x, y + step.y))
                {
                    return true;
                }
            }

            side = MapDirection.North;
            return false;
        }

        // Dalla cella della torcia verso il lato opposto, finché c'è un muro: la cella prima del muro
        // è quella della torcia di fronte. Un passaggio stretto vuol dire che la stanza è finita, e
        // la torcia non va a illuminare un corridoio; neanche sulla scala, dove c'è lo stendardo.
        private static bool TryFindFacingWall(LevelMap map, int x, int y, MapDirection side, out Vector2Int cell)
        {
            var step = Step(side);
            var across = new Vector2Int(step.y, step.x);
            cell = new Vector2Int(x, y);
            for (int i = 0; i < MirrorReach; i++)
            {
                var next = cell + step;
                if (!map.IsFloor(next.x, next.y))
                {
                    return map.GetSymbol(cell.x, cell.y) != StairsDownSymbol;
                }

                if (!map.IsFloor(next.x + across.x, next.y + across.y) && !map.IsFloor(next.x - across.x, next.y - across.y))
                {
                    return false;
                }

                cell = next;
            }

            return false;
        }

        // nord è la riga sopra nel file di testo: y diminuisce
        private static Vector2Int Step(MapDirection side)
        {
            switch (side)
            {
                case MapDirection.North: return new Vector2Int(0, -1);
                case MapDirection.East: return new Vector2Int(1, 0);
                case MapDirection.South: return new Vector2Int(0, 1);
                default: return new Vector2Int(-1, 0);
            }
        }

        private static MapDirection Opposite(MapDirection side)
        {
            return (MapDirection)(((int)side + 2) % 4);
        }

        private void PlaceGroundItem(Transform parent, Vector3 center, string itemName)
        {
            var definition = itemName != null && _findItem != null ? _findItem(itemName) : null;
            if (definition == null)
            {
                Debug.LogError($"Oggetto a terra in {center} senza un oggetto valido in @items ('{itemName}').");
                return;
            }

            var placed = Place(_tileset.GroundItem, parent, center, Quaternion.identity);
            placed.name = "Item_" + itemName;
            var ground = placed.GetComponent<GroundItem>();
            ground.Configure(definition);
            _modified?.Invoke(ground);
        }

        private GameObject Place(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
        {
            var go = _instantiate(prefab, parent);
            go.transform.SetPositionAndRotation(position, rotation);
            return go;
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
    }
}
