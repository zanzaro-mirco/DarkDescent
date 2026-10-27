using System.Collections.Generic;
using System.IO;
using DarkDescent.Levels;
using DarkDescent.Rendering;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Editor
{
    /// <summary>
    /// I prefab del borgo (M18, 18.2) e il suo tileset, fatti da una tabella: ogni pezzo è uno o più
    /// modelli KayKit, scalati, girati e posati con il fondo a terra e il centro dove dice la tabella.
    /// Le case di Medieval Hexagon sono in miniatura e vanno ×6. Si rifà tutto con un comando, anche in
    /// batchmode: <c>-executeMethod DarkDescent.Editor.VillagePrefabs.Build</c>.
    /// </summary>
    public static class VillagePrefabs
    {
        public const string PrefabFolder = "Assets/_Project/Prefabs/Village/";
        public const string TilesetPath = "Assets/_Project/Data/Levels/VillageTileset.asset";
        public const string DayLightingPath = "Assets/_Project/Data/Levels/VillageDayLighting.asset";
        public const string CryptTilesetPath = "Assets/_Project/Data/Levels/DungeonTileset.asset";
        public const string TunnelMouthPath = "Assets/_Project/Prefabs/Dungeon/TunnelMouth.prefab";

        private const string Village = KayKitImporter.ModelFolder + "Village/";
        private const string Graveyard = KayKitImporter.ModelFolder + "Graveyard/";
        private const string Nature = KayKitImporter.ModelFolder + "Nature/";
        private const string GeneratedFolder = "Assets/_Project/Art/Models/DarkDescent/";
        private const string TownPrefabs = "Assets/_Project/Prefabs/Town/";
        private const string Dungeon = KayKitImporter.ModelFolder + "Dungeon/";

        // le case di Medieval Hexagon: la porta alta quanto il cavaliere (18.1)
        private const float HexScale = 6f;

        // le luci che si accendono dal tramonto all'alba: davanti alla porta delle case, sotto le lanterne
        private static readonly Color WindowColor = new Color(1f, 0.68f, 0.36f);
        private static readonly Color LanternColor = new Color(1f, 0.78f, 0.45f);

        private enum Glow
        {
            None,
            Window,
            Lantern
        }

        private static readonly Dictionary<string, Glow> Glows = new Dictionary<string, Glow>
        {
            ["HouseA_Red"] = Glow.Window,
            ["HouseA_Blue"] = Glow.Window,
            ["HouseB_Yellow"] = Glow.Window,
            ["HealerHouse"] = Glow.Window,
            ["Tavern"] = Glow.Window,
            ["Forge"] = Glow.Window,
            ["Chapel"] = Glow.Window,
            ["LanternPost"] = Glow.Lantern,
        };

        private const int DefaultLayer = 0;
        private const int GroundLayer = 6;
        private const int ObstacleLayer = 7;

        // La bocca della grotta (D6 della M18): imbocco e buio di Tools/Blender/models/cave.py, con
        // l'apertura nel loro centro e il davanti verso +Z. Sta sul bordo nord della cella dell'uscita,
        // che ha il +Z verso la cella da cui si arriva, girata verso la camera di partenza
        private const float CaveFront = -2f;
        private const float CaveYaw = 30f;
        private const float CaveHalfWidth = 1.5f;
        private static readonly Color CaveGlowColor = new Color(1f, 0.55f, 0.2f);
        private static readonly Color FireColor = new Color(1f, 0.58f, 0.26f);

        // attorno all'imbocco, dalle immagini di Mirco: i drappi rossi fuori dai pali (si accendono con
        // l'imbocco), i bracieri ai lati e uno in fondo, i pali con il teschio più in là
        private static readonly Part[] CaveBanners =
        {
            new Part(Dungeon + "banner_red.fbx", 0.55f, -CaveHalfWidth - 0.75f, 0.35f, lift: 1f),
            new Part(Dungeon + "banner_red.fbx", 0.55f, CaveHalfWidth + 0.75f, 0.35f, lift: 1f),
        };

        private static readonly Part[] CaveSkullPosts =
        {
            Grave("post_skull", -CaveHalfWidth - 2.6f, 1.3f), Grave("post_skull", CaveHalfWidth + 2.6f, 1.3f),
        };

        private static readonly Vector3[] CaveBraziers =
        {
            new Vector3(-CaveHalfWidth - 0.9f, 0f, 1f), new Vector3(CaveHalfWidth + 0.9f, 0f, 1f), new Vector3(0.5f, 0f, -3f),
        };

        // Lo sbocco del cunicolo (D7 della M18) nel muro della cella scelta da TunnelSpot: il builder
        // mette il prefab al centro della cella con il +Z verso la stanza, e il muro dietro manca. Il
        // modello ha l'origine sulla linea del muro, a metà cella. Attorno, come la grotta del borgo: i
        // drappi appesi alla traversa e due bracieri davanti agli stipiti
        private static readonly Vector3 TunnelWallLine = new Vector3(0f, 0f, -LevelMap.CellSize * 0.5f);
        private static readonly Color TunnelLightColor = new Color(0.55f, 0.66f, 0.92f);

        private static readonly Part[] TunnelBanners =
        {
            new Part(Dungeon + "banner_red.fbx", 0.55f, -1.75f, 1.05f, lift: 0.5f),
            new Part(Dungeon + "banner_red.fbx", 0.55f, 1.75f, 1.05f, lift: 0.5f),
        };

        private static readonly Vector3[] TunnelBraziers = { new Vector3(-1.55f, 0f, 1.9f), new Vector3(1.55f, 0f, 1.9f) };

        // le texture del terreno, da Tools/Textures/ground.py: senza cuciture, stese in coordinate del mondo
        private const string TextureFolder = "Assets/_Project/Art/Textures/Village/";
        private const float GrassRepeat = 32f;
        private const float RoadRepeat = 12f;

        /// <summary>Un modello del pezzo: dove sta il centro della sua base, quanto è grande, come è girato.</summary>
        private readonly struct Part
        {
            public Part(string model, float scale, float x = 0f, float z = 0f, float yaw = 0f, float lift = 0f)
            {
                Model = model;
                Scale = scale;
                Position = new Vector3(x, lift, z);
                Yaw = yaw;
            }

            public string Model { get; }

            public float Scale { get; }

            public Vector3 Position { get; }

            public float Yaw { get; }
        }

        /// <summary>
        /// Un prefab del borgo. Gli ostacoli stanno sul layer Obstacle, che il NavMesh ritaglia, con un
        /// collider che ferma i click; il paesaggio oltre il bordo sta su Default, fuori dal NavMesh.
        /// </summary>
        private readonly struct Piece
        {
            public Piece(string name, int layer, params Part[] parts)
            {
                Name = name;
                Layer = layer;
                Parts = parts;
            }

            public string Name { get; }

            public int Layer { get; }

            public Part[] Parts { get; }
        }

        private static Piece Obstacle(string name, params Part[] parts) => new Piece(name, ObstacleLayer, parts);

        private static Piece Scenery(string name, params Part[] parts) => new Piece(name, DefaultLayer, parts);

        private static Part Hex(string model, float yaw = 0f, float x = 0f, float z = 0f) => new Part(Village + model + ".fbx", HexScale, x, z, yaw);

        private static Part Grave(string model, float x = 0f, float z = 0f, float yaw = 0f, float scale = 1f) => new Part(Graveyard + model + ".fbx", scale, x, z, yaw);

        private static Part Wild(string model, float scale, float x = 0f, float z = 0f, float yaw = 0f) => new Part(Nature + model + "_Color1.fbx", scale, x, z, yaw);

        /// <summary>I pezzi, nell'ordine in cui si costruiscono.</summary>
        private static readonly Piece[] Pieces =
        {
            // il bordo: muretti di pietra e staccionate lunghi una cella, lungo X; alto sui lati lontani,
            // solo il muretto su quelli vicini alla camera, come i muri della cripta
            Obstacle("VillageEdge",
                Hex("fence_stone_straight", 90f), Wild("Bush_1_A", 2.5f, 1.3f, 0.7f), Wild("Rock_3_D", 1.4f, -1.4f, 0.8f, 70f)),
            Obstacle("VillageEdgeFence",
                Hex("fence_wood_straight", 90f), Wild("Bush_2_B", 2.5f, -1.2f, 0.7f), Wild("Grass_1_A", 2.5f, 0.8f, 0.6f)),
            Obstacle("VillageEdgeLow", Hex("fence_stone_straight", 90f)),

            // le case: un pezzo della mappa ciascuna, con la porta verso +Z
            Obstacle("HouseA_Red", Hex("building_home_A_red")),
            Obstacle("HouseA_Blue", Hex("building_home_A_blue")),
            Obstacle("HouseB_Yellow", Hex("building_home_B_yellow")),
            Obstacle("HealerHouse", Hex("building_home_B_green")),
            Obstacle("Tavern", Hex("building_tavern_blue")),
            Obstacle("Ruin", Hex("building_destroyed")),
            Obstacle("Forge", Hex("building_blacksmith_red")),
            Obstacle("Chapel", Hex("building_church_yellow")),
            Obstacle("Well", Hex("building_well_blue")),
            Obstacle("Crypt", Grave("crypt")),
            // il monte della grotta: la bocca è l'uscita, sulla cella davanti (CaveExit)
            Obstacle("CaveMouth", Hex("mountain_C_grass", 0f, 0f, -1.5f)),

            // il cimitero e la piazza: oggetti sul pavimento, che il NavMesh aggira
            Obstacle("Grave", Grave("grave_A")),
            Obstacle("Gravestones", Grave("gravestone", -0.9f), Grave("gravemarker_A", 0.9f, 0.2f)),
            Obstacle("GraveFence", Grave("fence")),
            Obstacle("GraveArch", Grave("arch")),
            Obstacle("LanternPost", Grave("post_lantern")),
            Obstacle("DeadTree", Grave("tree_dead_large_decorated")),
            Obstacle("Bench", Grave("bench")),
            Obstacle("Crates", Hex("crate_A_big", 0f, -0.5f), Hex("crate_A_small", 30f, 0.9f, 0.4f)),
            Obstacle("Barrels", Hex("barrel", 0f, -0.5f), Hex("barrel", 0f, 0.6f, 0.3f), Hex("sack", 0f, 0.2f, -0.9f)),
            Obstacle("WeaponRack", Hex("weaponrack")),
            Obstacle("Lumber", Hex("resource_lumber")),
            Obstacle("Wheelbarrow", Hex("wheelbarrow")),

            // l'erba e i cespugli bassi sul pavimento: si attraversano, fuori dal NavMesh
            Scenery("GrassTufts",
                Wild("Grass_1_A", 1.9f, -1.2f, -0.9f), Wild("Grass_2_B", 1.9f, 0.8f, -1.3f, 70f), Wild("Grass_1_D", 1.9f, 1.3f, 0.9f, 140f),
                Wild("Grass_2_C", 1.9f, -0.6f, 1.2f, 210f), Wild("Grass_1_B", 1.6f, 0.2f, 0.1f, 300f)),
            Scenery("ScatterGrass", Wild("Grass_1_A", 1.7f, -0.6f, -0.3f), Wild("Grass_2_B", 1.6f, 0.5f, 0.4f, 80f), Wild("Grass_1_C", 1.4f, 0.1f, -0.9f, 200f)),
            Scenery("ScatterGrassBush", Wild("Grass_2_A", 1.7f, -0.4f, 0.2f), Wild("Bush_1_E", 1.6f, 0.6f, -0.3f, 30f)),
            Scenery("ScatterGrassTall", Wild("Grass_1_D", 1.9f, 0f, 0f), Wild("Grass_2_C", 1.5f, 0.7f, 0.6f, 120f)),
            Scenery("ScatterPebbles", Wild("Rock_1_D", 0.7f, -0.4f, 0f), Wild("Rock_1_G", 0.6f, 0.4f, 0.3f, 60f), Wild("Rock_1_J", 0.5f, 0.1f, -0.5f, 140f)),
            Scenery("ScatterStone", Wild("Rock_2_B", 1.1f, 0f, 0f), Wild("Grass_1_B", 1.5f, 0.7f, 0.3f, 90f)),
            Scenery("Shrub", Wild("Bush_3_B", 2.4f, -0.4f, -0.3f), Wild("Grass_2_D", 2.5f, 0.9f, 0.7f), Wild("Grass_1_C", 2.2f, -1.1f, 1f, 90f)),

            // il paesaggio oltre il bordo: niente NavMesh, niente collider
            Scenery("Pines", Grave("tree_pine_orange_large", -1f, -0.8f), Grave("tree_pine_yellow_medium", 1.4f, 0.9f, 40f), Grave("tree_pine_orange_small", -0.8f, 1.6f, 80f)),
            Scenery("PinesTall", Grave("tree_pine_yellow_large", 0.8f, -0.6f, 20f), Grave("tree_pine_orange_large", -1.2f, 1f, 70f)),
            Scenery("DeadTrees", Grave("tree_dead_large", -0.6f, 0f), Grave("tree_dead_medium", 1.3f, 1f, 110f)),
            Scenery("Rocks", Wild("Rock_3_C", 2.6f, -0.8f), Wild("Rock_3_H", 2f, 1.1f, 0.7f, 60f), Wild("Bush_1_B", 2.5f, 0.4f, -1.3f)),
            Scenery("Forest",
                Grave("tree_pine_orange_large", -1.1f, -1.1f), Grave("tree_pine_yellow_large", 1.2f, -0.6f, 50f),
                Grave("tree_pine_orange_medium", -0.6f, 1.3f, 100f), Grave("tree_pine_yellow_medium", 1.5f, 1.5f, 160f),
                Wild("Bush_2_C", 2.5f, 0.2f, 0.2f)),
            Scenery("Boulders", Wild("Rock_3_B", 4f, -0.6f, -0.4f), Wild("Rock_3_F", 3.4f, 1.1f, 0.8f, 80f), Wild("Rock_2_C", 3f, -1.2f, 1.3f, 150f)),
            Scenery("Bushes",
                Wild("Bush_1_D", 2.6f, -1f, -0.6f), Wild("Bush_2_D", 2.6f, 0.9f, 0.3f, 60f), Wild("Rock_1_B", 1.6f, -0.2f, 1.2f),
                Wild("Grass_2_A", 2.5f, 1.4f, -1.2f), Wild("Grass_1_C", 2.5f, -1.5f, 1.4f)),
            Scenery("Knoll", Hex("hill_single_A")),
            Scenery("Hill", Hex("hills_A_trees")),
            Scenery("Mountain", Hex("mountain_A_grass_trees")),
        };

        [MenuItem("DarkDescent/Borgo/Prefab e tileset")]
        public static void Build()
        {
            Directory.CreateDirectory(PrefabFolder);
            var grass = GroundMaterial("M_VillageGrass", "ground_grass.png");
            var dirt = GroundMaterial("M_VillageDirt", "ground_road.png");

            SaveTile("VillageGrass", grass, GrassRepeat);
            SaveTile("VillageRoad", dirt, RoadRepeat);
            SaveBackdrop(grass, DayLighting());
            foreach (var piece in Pieces)
            {
                SavePiece(piece);
            }

            SaveCaveExit(dirt);
            SaveTunnelMouth();
            AssetDatabase.SaveAssets();
            BuildTileset();
            Debug.Log($"[DarkDescent] borgo: {Pieces.Length + 4} prefab in {PrefabFolder}, più {TunnelMouthPath}");
        }

        /// <summary>Il prefab del borgo con questo nome.</summary>
        public static GameObject Load(string name)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + name + ".prefab");
        }

        // Un materiale del terreno, copiato da quello della cripta perché prenda la luce come il resto
        private static Material GroundMaterial(string name, string texture)
        {
            string path = VillageTools.MaterialFolder + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var source = AssetDatabase.LoadAssetAtPath<Material>(VillageTools.DungeonMaterial);
            if (material == null)
            {
                material = new Material(source) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.CopyPropertiesFromMaterial(source);
            }

            var map = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + texture);
            if (map == null)
            {
                throw new FileNotFoundException($"manca {TextureFolder + texture}: si fa con Tools/Textures/ground.py");
            }

            material.SetTexture("_BaseMap", map);
            material.SetTexture("_MainTex", map);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_Color", Color.white);
            EditorUtility.SetDirty(material);
            return material;
        }

        // Una piastrella da 4 m con la cima a 0, come il pavimento della cripta, con la texture stesa nel mondo
        private static void SaveTile(string name, Material material, float repeat)
        {
            var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.layer = GroundLayer;
            tile.transform.localScale = new Vector3(LevelMap.CellSize, 0.2f, LevelMap.CellSize);
            tile.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            tile.GetComponent<MeshRenderer>().sharedMaterial = material;
            WorldTexture(tile, repeat);

            var root = new GameObject(name) { layer = GroundLayer };
            tile.transform.SetParent(root.transform, false);
            tile.name = "Tile";
            Save(root, name);
        }

        // Il prato sotto tutta la mappa e oltre il bordo, appena sotto i pavimenti, con il sole: fuori
        // dal NavMesh e senza collider. Il giorno che passa (18.3) muoverà questo sole.
        private static void SaveBackdrop(Material grass, DayLighting lighting)
        {
            var root = new GameObject("VillageBackdrop");
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            Object.DestroyImmediate(ground.GetComponent<Collider>());
            ground.transform.SetParent(root.transform, false);
            ground.transform.localScale = new Vector3(400f, 0.2f, 400f);
            ground.transform.localPosition = new Vector3(0f, -0.13f, 0f);
            var renderer = ground.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = grass;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            WorldTexture(ground, GrassRepeat);

            var sun = new GameObject("Sun");
            sun.transform.SetParent(root.transform, false);
            sun.transform.localRotation = Quaternion.Euler(48f, -35f, 0f);
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.86f, 0.84f, 0.8f);
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.75f;

            var view = new SerializedObject(root.AddComponent<DayCycleView>());
            view.FindProperty("_lighting").objectReferenceValue = lighting;
            view.FindProperty("_sun").objectReferenceValue = light;
            view.ApplyModifiedPropertiesWithoutUndo();
            Save(root, root.name);
        }

        private static void WorldTexture(GameObject go, float repeat)
        {
            var so = new SerializedObject(go.AddComponent<WorldUv>());
            so.FindProperty("_metersPerRepeat").floatValue = repeat;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SavePiece(Piece piece)
        {
            var root = new GameObject(piece.Name) { layer = piece.Layer };
            AddParts(root.transform, piece.Parts);
            SetLayer(root, piece.Layer);

            if (piece.Layer == ObstacleLayer)
            {
                var all = Bounds(root);
                var box = root.AddComponent<BoxCollider>();
                box.center = all.center;
                box.size = all.size;
            }

            if (Glows.TryGetValue(piece.Name, out var glow))
            {
                AddGlow(root, glow);
            }

            Save(root, piece.Name);
        }

        private static void AddParts(Transform parent, Part[] parts)
        {
            foreach (var part in parts)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(part.Model);
                if (model == null)
                {
                    throw new FileNotFoundException($"manca {part.Model}");
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
                instance.transform.localScale = Vector3.one * part.Scale;
                instance.transform.localRotation = Quaternion.Euler(0f, part.Yaw, 0f);
                var material = MaterialFor(part.Model);
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                {
                    var materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        materials[i] = material;
                    }

                    renderer.sharedMaterials = materials;
                }

                // il fondo a terra e il centro della base dove dice la tabella
                var bounds = Bounds(instance);
                instance.transform.localPosition += part.Position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            }
        }

        private static void SetLayer(GameObject root, int layer)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = layer;
            }
        }

        private static Transform Group(Transform parent, string name, Vector3 position = default, float yaw = 0f)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            group.localPosition = position;
            group.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return group;
        }

        // Pezzi della tabella in un gruppo già spostato e girato: AddParts misura in coordinate del
        // mondo, quindi si montano su una radice ferma nell'origine e poi passano al gruppo
        private static void AddPartsTo(Transform group, Part[] parts)
        {
            var temp = new GameObject("Temp");
            AddParts(temp.transform, parts);
            while (temp.transform.childCount > 0)
            {
                temp.transform.GetChild(0).SetParent(group, false);
            }

            Object.DestroyImmediate(temp);
        }

        // Il braciere su treppiede, con la luce sopra le fiamme che trema come le torce
        private static void AddBrazier(Transform parent, Vector3 position, float intensity, float range)
        {
            var brazier = Generated(parent, "Brazier");
            brazier.transform.localPosition = position;
            var light = PointLight(brazier.transform, "Flame", new Vector3(0f, 1.6f, 0f), FireColor, intensity, range);
            light.gameObject.AddComponent<TorchFlicker>();
        }

        // Un modello di Tools/Blender con il suo centro: l'apertura degli imbocchi è lì
        private static GameObject Generated(Transform parent, string name)
        {
            string path = GeneratedFolder + name + ".fbx";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null)
            {
                throw new FileNotFoundException($"manca {path}: si fa con Tools/Blender/make.py");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            var material = MaterialFor(path);
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = material;
            }

            return instance;
        }

        private static Light PointLight(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.localPosition = position;
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }

        // L'uscita del borgo verso la profondità 1 (D6 della M18), al posto della scala della cripta: il
        // builder la mette sulla cella '>' con il +Z verso la cella da cui si arriva. L'imbocco (pali,
        // teschio cornuto, massi e drappi) sta sul bordo opposto, sotto il monte, e si accende sotto il
        // cursore con il bagliore dentro; il buio, i bracieri, i pali con il teschio e la terra battuta
        // della cella stanno in "Unlit". L'area del click copre anche l'imbocco: il builder la passa all'uscita. Niente NavMesh:
        // si scende dal bordo della cella, come dalla scala.
        private static void SaveCaveExit(Material dirt)
        {
            var root = new GameObject("CaveExit");
            var opening = new Vector3(0f, 0f, CaveFront);

            // la prima luce del prefab: quella che si alza sotto il cursore
            PointLight(root.transform, "Glow", opening + new Vector3(0f, 1.2f, -0.6f), CaveGlowColor, 3f, 6f);

            var mouth = Group(root.transform, "Mouth", opening, CaveYaw);
            Generated(mouth, "CaveEntrance");
            AddPartsTo(mouth, CaveBanners);

            var unlit = Group(root.transform, "Unlit");
            var inside = Group(unlit, "Inside", opening, CaveYaw);
            Generated(inside, "CaveHole");
            AddPartsTo(inside, CaveSkullPosts);
            for (int i = 0; i < CaveBraziers.Length; i++)
            {
                // l'ultimo è in fondo alla galleria: più debole, dice quanto è profonda
                bool deep = i == CaveBraziers.Length - 1;
                AddBrazier(inside, CaveBraziers[i], deep ? 2.5f : 4f, deep ? 4f : 6f);
            }

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            Object.DestroyImmediate(ground.GetComponent<Collider>());
            ground.transform.SetParent(unlit, false);
            ground.transform.localScale = new Vector3(LevelMap.CellSize, 0.2f, LevelMap.CellSize);
            ground.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = dirt;
            WorldTexture(ground, RoadRepeat);

            SetLayer(root, DefaultLayer);

            var click = new GameObject("ClickArea") { layer = LayerMask.NameToLayer("Interactable") };
            click.transform.SetParent(root.transform, false);
            click.transform.localPosition = opening;
            click.transform.localRotation = Quaternion.Euler(0f, CaveYaw, 0f);
            var box = click.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 1.9f, 0f);
            box.size = new Vector3(CaveHalfWidth * 2f + 1.6f, 3.8f, 1.6f);
            Save(root, root.name);
        }

        // Lo sbocco del cunicolo alla profondità 1 della cripta (D7 della M18): un ostacolo, che il NavMesh
        // aggira, con una luce fredda in fondo, come se di là ci fosse il cielo. Il collider stringe
        // massi, galleria, drappi e bracieri.
        private static void SaveTunnelMouth()
        {
            var root = new GameObject("TunnelMouth");
            var mouth = Group(root.transform, "Mouth", TunnelWallLine);
            Generated(mouth, "TunnelEntrance");
            Generated(mouth, "TunnelHole");
            AddPartsTo(mouth, TunnelBanners);
            foreach (var position in TunnelBraziers)
            {
                AddBrazier(mouth, position, 4f, 6f);
            }

            SetLayer(root, ObstacleLayer);

            var local = new Bounds();
            bool first = true;
            foreach (var filter in mouth.GetComponentsInChildren<MeshFilter>())
            {
                foreach (var vertex in filter.sharedMesh.vertices)
                {
                    Vector3 point = mouth.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                    if (first)
                    {
                        local = new Bounds(point, Vector3.zero);
                        first = false;
                    }
                    else
                    {
                        local.Encapsulate(point);
                    }
                }
            }

            var box = mouth.gameObject.AddComponent<BoxCollider>();
            box.center = local.center;
            box.size = local.size;

            PointLight(mouth, "Daylight", new Vector3(0f, 1.4f, -1f), TunnelLightColor, 3f, 5f);
            PrefabUtility.SaveAsPrefabAsset(root, TunnelMouthPath);
            Object.DestroyImmediate(root);

            var tileset = AssetDatabase.LoadAssetAtPath<LevelTileset>(CryptTilesetPath);
            var so = new SerializedObject(tileset);
            so.FindProperty("_entranceMouth").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(TunnelMouthPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tileset);
        }

        // Una luce calda senza ombre, che la vista del giorno accende al tramonto: davanti alla porta,
        // all'altezza delle finestre, o sotto il lume della lanterna
        private static void AddGlow(GameObject root, Glow glow)
        {
            var bounds = Bounds(root);
            var go = new GameObject("NightLight");
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = glow == Glow.Window
                ? new Vector3(bounds.center.x, 1.8f, bounds.max.z + 0.6f)
                : new Vector3(bounds.center.x, bounds.max.y - 0.8f, bounds.center.z);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = glow == Glow.Window ? WindowColor : LanternColor;
            light.range = glow == Glow.Window ? 7f : 8f;
            light.intensity = glow == Glow.Window ? 2.2f : 2.6f;
            light.shadows = LightShadows.None;
            light.enabled = false;
            go.AddComponent<NightLight>();
        }

        // Le luci dei quattro momenti (D5 della M18): un asset da ritoccare dall'Inspector. Si scrive
        // solo la prima volta; dopo comanda l'asset, non questa tabella.
        private static DayLighting DayLighting()
        {
            var lighting = AssetDatabase.LoadAssetAtPath<DayLighting>(DayLightingPath);
            if (lighting != null)
            {
                return lighting;
            }

            lighting = ScriptableObject.CreateInstance<DayLighting>();
            AssetDatabase.CreateAsset(lighting, DayLightingPath);
            var so = new SerializedObject(lighting);
            SetLook(so, "_dawn", new DayLook(new Vector2(25f, 95f), new Color(1f, 0.8f, 0.65f), 1f, new Color(0.42f, 0.38f, 0.4f),
                new Color(0.7f, 0.62f, 0.6f), new Vector2(18f, 70f), 0.15f, new Color(1f, 0.95f, 0.93f), -5f, 12f));
            SetLook(so, "_day", new DayLook(new Vector2(52f, -35f), new Color(0.95f, 0.93f, 0.88f), 1.2f, new Color(0.48f, 0.47f, 0.45f),
                new Color(0.66f, 0.69f, 0.72f), new Vector2(35f, 120f), 0.2f, Color.white, -6f, 4f));
            SetLook(so, "_dusk", new DayLook(new Vector2(22f, 255f), new Color(1f, 0.6f, 0.38f), 1.1f, new Color(0.4f, 0.3f, 0.3f),
                new Color(0.6f, 0.42f, 0.36f), new Vector2(25f, 95f), 0.1f, new Color(1f, 0.92f, 0.85f), 2f, 20f));
            SetLook(so, "_night", new DayLook(new Vector2(45f, 200f), new Color(0.6f, 0.7f, 0.95f), 0.45f, new Color(0.16f, 0.18f, 0.26f),
                new Color(0.08f, 0.1f, 0.16f), new Vector2(20f, 85f), 0f, new Color(0.88f, 0.92f, 1.05f), -15f, -20f));
            so.FindProperty("_minimumAmbient").colorValue = new Color(0.16f, 0.18f, 0.25f);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(lighting);
            return lighting;
        }

        private static void SetLook(SerializedObject so, string name, DayLook look)
        {
            var property = so.FindProperty(name);
            property.FindPropertyRelative("_sunAngles").vector2Value = look.SunAngles;
            property.FindPropertyRelative("_sunColor").colorValue = look.SunColor;
            property.FindPropertyRelative("_sunIntensity").floatValue = look.SunIntensity;
            property.FindPropertyRelative("_ambient").colorValue = look.Ambient;
            property.FindPropertyRelative("_fog").colorValue = look.Fog;
            property.FindPropertyRelative("_fogRange").vector2Value = look.FogRange;
            property.FindPropertyRelative("_exposure").floatValue = look.Exposure;
            property.FindPropertyRelative("_colorFilter").colorValue = look.ColorFilter;
            property.FindPropertyRelative("_saturation").floatValue = look.Saturation;
            property.FindPropertyRelative("_temperature").floatValue = look.Temperature;
        }

        private static Material MaterialFor(string model)
        {
            string name = model.StartsWith(Village) ? "M_Village"
                : model.StartsWith(Graveyard) ? "M_Graveyard"
                : model.StartsWith(Nature) ? "M_Nature"
                : "M_Dungeon";
            return AssetDatabase.LoadAssetAtPath<Material>(VillageTools.MaterialFolder + name + ".mat");
        }

        private static Bounds Bounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            // in coordinate della radice, che sta nell'origine senza giri
            return bounds;
        }

        private static void Save(GameObject root, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + name + ".prefab");
            Object.DestroyImmediate(root);
        }

        // Il tileset del borgo (nato dal tileset dell'accampamento, di cui tiene scala, uscita, oggetti a
        // terra, suoni e nome): pavimento, bordo, terreno e simboli vengono da qui.
        private static void BuildTileset()
        {
            var tileset = AssetDatabase.LoadAssetAtPath<LevelTileset>(TilesetPath);
            if (tileset == null)
            {
                throw new FileNotFoundException($"manca {TilesetPath}: scala, uscita, suoni e nome si mettono lì una volta");
            }

            var so = new SerializedObject(tileset);
            so.FindProperty("_floor").objectReferenceValue = Load("VillageGrass");
            so.FindProperty("_backdrop").objectReferenceValue = Load("VillageBackdrop");
            so.FindProperty("_wall").objectReferenceValue = Load("VillageEdge");
            so.FindProperty("_wallVariant").objectReferenceValue = Load("VillageEdgeFence");
            so.FindProperty("_lowWall").objectReferenceValue = Load("VillageEdgeLow");
            so.FindProperty("_variantChance").floatValue = 0.4f;
            so.FindProperty("_floorVariant").objectReferenceValue = null;
            so.FindProperty("_wallTorch").objectReferenceValue = null;
            so.FindProperty("_ambientColor").colorValue = new Color(0.4f, 0.38f, 0.35f);
            so.FindProperty("_stairsDown").objectReferenceValue = Load("CaveExit");
            so.FindProperty("_exitBanner").objectReferenceValue = null;

            var markers = new List<(char Symbol, GameObject Prefab, bool ReplacesFloor, bool KeepsRotation)>
            {
                (',', Load("VillageRoad"), true, true),
                ('H', Load("HouseA_Red"), false, false),
                ('I', Load("HouseA_Blue"), false, false),
                ('J', Load("HouseB_Yellow"), false, false),
                ('G', Load("HealerHouse"), false, false),
                ('V', Load("Tavern"), false, false),
                ('D', Load("Ruin"), false, false),
                ('B', Load("Forge"), false, false),
                ('C', Load("Chapel"), false, false),
                ('W', Load("Well"), false, false),
                ('Q', Load("Crypt"), false, false),
                ('O', Load("CaveMouth"), false, false),
                ('Y', Load("Pines"), false, false),
                ('Z', Load("PinesTall"), false, false),
                ('X', Load("DeadTrees"), false, false),
                ('R', Load("Rocks"), false, false),
                ('h', Load("Hill"), false, false),
                ('F', Load("Forest"), false, false),
                ('S', Load("Boulders"), false, false),
                ('U', Load("Bushes"), false, false),
                ('N', Load("Knoll"), false, false),
                ('v', Load("GrassTufts"), false, false),
                ('o', Load("Shrub"), false, false),
                ('m', Load("Mountain"), false, false),
                ('g', Load("Grave"), false, true),
                ('q', Load("Gravestones"), false, true),
                ('n', Load("GraveFence"), false, true),
                ('a', Load("GraveArch"), false, true),
                ('l', Load("LanternPost"), false, false),
                ('d', Load("DeadTree"), false, false),
                ('e', Load("Bench"), false, false),
                ('x', Load("Crates"), false, false),
                ('b', Load("Barrels"), false, false),
                ('r', Load("WeaponRack"), false, false),
                ('u', Load("Lumber"), false, false),
                ('k', Load("Wheelbarrow"), false, false),
                ('f', AssetDatabase.LoadAssetAtPath<GameObject>(TownPrefabs + "Campfire.prefab"), false, false),
                ('M', AssetDatabase.LoadAssetAtPath<GameObject>(TownPrefabs + "MerchantStall.prefab"), false, false),
                ('A', AssetDatabase.LoadAssetAtPath<GameObject>(TownPrefabs + "DifficultyAltar.prefab"), false, false),
                ('P', AssetDatabase.LoadAssetAtPath<GameObject>(TownPrefabs + "PortalSpot.prefab"), false, false),
            };

            // l'erba conta il doppio dei sassi: quattro varianti d'erba e due di sassi
            string[] scatter = { "ScatterGrass", "ScatterGrassBush", "ScatterGrassTall", "ScatterGrass", "ScatterPebbles", "ScatterStone" };
            var scatterList = so.FindProperty("_scatter");
            scatterList.arraySize = scatter.Length;
            for (int i = 0; i < scatter.Length; i++)
            {
                scatterList.GetArrayElementAtIndex(i).objectReferenceValue = Load(scatter[i]);
            }

            so.FindProperty("_scatterChance").floatValue = 0.6f;
            so.FindProperty("_outsideScatterChance").floatValue = 0.85f;

            var list = so.FindProperty("_markers");
            list.arraySize = markers.Count;
            for (int i = 0; i < markers.Count; i++)
            {
                var element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_symbol").intValue = markers[i].Symbol;
                element.FindPropertyRelative("_prefab").objectReferenceValue = markers[i].Prefab;
                element.FindPropertyRelative("_replacesFloor").boolValue = markers[i].ReplacesFloor;
                element.FindPropertyRelative("_keepsRotation").boolValue = markers[i].KeepsRotation;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tileset);
            AssetDatabase.SaveAssets();
        }
    }
}
