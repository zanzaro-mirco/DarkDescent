using System.Collections.Generic;
using System.IO;
using DarkDescent.Levels;
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

        private const string Village = KayKitImporter.ModelFolder + "Village/";
        private const string Graveyard = KayKitImporter.ModelFolder + "Graveyard/";
        private const string Nature = KayKitImporter.ModelFolder + "Nature/";
        private const string Generated = "Assets/_Project/Art/Models/DarkDescent/";
        private const string TownPrefabs = "Assets/_Project/Prefabs/Town/";

        // le case di Medieval Hexagon: la porta alta quanto il cavaliere (18.1)
        private const float HexScale = 6f;

        private const int DefaultLayer = 0;
        private const int GroundLayer = 6;
        private const int ObstacleLayer = 7;

        private static readonly Color GrassColor = new Color(0.34f, 0.42f, 0.24f);
        private static readonly Color DirtColor = new Color(0.42f, 0.33f, 0.24f);

        /// <summary>Un modello del pezzo: dove sta il centro della sua base, quanto è grande, come è girato.</summary>
        private readonly struct Part
        {
            public Part(string model, float scale, float x = 0f, float z = 0f, float yaw = 0f)
            {
                Model = model;
                Scale = scale;
                Position = new Vector3(x, 0f, z);
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
            Obstacle("CaveMouth",
                Hex("mountain_C_grass", 0f, 0f, -1.5f), new Part(Generated + "CaveHole.fbx", 1f, 0f, 4.2f),
                Wild("Rock_3_B", 3f, -2.7f, 4.6f), Wild("Rock_3_E", 2.6f, 2.8f, 4.8f, 120f), Wild("Rock_2_A", 2.4f, -3.8f, 3.4f)),

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

            // il paesaggio oltre il bordo: niente NavMesh, niente collider
            Scenery("Pines", Grave("tree_pine_orange_large", -1f, -0.8f), Grave("tree_pine_yellow_medium", 1.4f, 0.9f, 40f), Grave("tree_pine_orange_small", -0.8f, 1.6f, 80f)),
            Scenery("PinesTall", Grave("tree_pine_yellow_large", 0.8f, -0.6f, 20f), Grave("tree_pine_orange_large", -1.2f, 1f, 70f)),
            Scenery("DeadTrees", Grave("tree_dead_large", -0.6f, 0f), Grave("tree_dead_medium", 1.3f, 1f, 110f)),
            Scenery("Rocks", Wild("Rock_3_C", 2.6f, -0.8f), Wild("Rock_3_H", 2f, 1.1f, 0.7f, 60f), Wild("Bush_1_B", 2.5f, 0.4f, -1.3f)),
            Scenery("Hill", Hex("hills_A_trees")),
            Scenery("Mountain", Hex("mountain_A_grass_trees")),
        };

        [MenuItem("DarkDescent/Borgo/Prefab e tileset")]
        public static void Build()
        {
            Directory.CreateDirectory(PrefabFolder);
            var grass = FlatMaterial("M_VillageGrass", GrassColor);
            var dirt = FlatMaterial("M_VillageDirt", DirtColor);

            SaveTile("VillageGrass", grass, new Vector3(LevelMap.CellSize, 0.2f, LevelMap.CellSize), GroundLayer, true);
            SaveTile("VillageRoad", dirt, new Vector3(LevelMap.CellSize, 0.2f, LevelMap.CellSize), GroundLayer, true);
            SaveBackdrop(grass);
            foreach (var piece in Pieces)
            {
                SavePiece(piece);
            }

            AssetDatabase.SaveAssets();
            BuildTileset();
            Debug.Log($"[DarkDescent] borgo: {Pieces.Length + 3} prefab in {PrefabFolder}");
        }

        /// <summary>Il prefab del borgo con questo nome.</summary>
        public static GameObject Load(string name)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + name + ".prefab");
        }

        // Un materiale a tinta unita, copiato da quello della cripta perché prenda la luce come il resto
        private static Material FlatMaterial(string name, Color color)
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

            material.SetTexture("_BaseMap", null);
            material.SetTexture("_MainTex", null);
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        // Una piastrella da 4 m con la cima a 0, come il pavimento della cripta
        private static void SaveTile(string name, Material material, Vector3 size, int layer, bool collider)
        {
            var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = name;
            tile.layer = layer;
            tile.transform.localScale = size;
            tile.transform.localPosition = new Vector3(0f, -size.y * 0.5f, 0f);
            tile.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!collider)
            {
                Object.DestroyImmediate(tile.GetComponent<Collider>());
            }

            var root = new GameObject(name) { layer = layer };
            tile.transform.SetParent(root.transform, false);
            tile.name = "Tile";
            Save(root, name);
        }

        // Il prato sotto tutta la mappa e oltre il bordo, appena sotto i pavimenti, con il sole: fuori
        // dal NavMesh e senza collider. Il giorno che passa (18.3) muoverà questo sole.
        private static void SaveBackdrop(Material grass)
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

            var sun = new GameObject("Sun");
            sun.transform.SetParent(root.transform, false);
            sun.transform.localRotation = Quaternion.Euler(48f, -35f, 0f);
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.86f, 0.84f, 0.8f);
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.75f;
            Save(root, root.name);
        }

        private static void SavePiece(Piece piece)
        {
            var root = new GameObject(piece.Name) { layer = piece.Layer };
            foreach (var part in piece.Parts)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(part.Model);
                if (model == null)
                {
                    throw new FileNotFoundException($"manca {part.Model}");
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
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

            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = piece.Layer;
            }

            if (piece.Layer == ObstacleLayer)
            {
                var all = Bounds(root);
                var box = root.AddComponent<BoxCollider>();
                box.center = all.center;
                box.size = all.size;
            }

            Save(root, piece.Name);
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
