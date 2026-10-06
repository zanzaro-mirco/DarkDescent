using System.Collections.Generic;
using System.IO;
using DarkDescent.Levels;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DarkDescent.Editor
{
    /// <summary>
    /// Genera e disegna una cripta o una caverna senza entrare in Play Mode (D10 della scheda M6, passo
    /// 7.2 della M7): seme della partita e profondità, gli stessi di <c>-seed</c> in build, quindi si vede
    /// proprio il livello del gioco. Un livello strano si salva come mappa di testo e diventa un livello di prova.
    /// </summary>
    public class DungeonGeneratorWindow : EditorWindow
    {
        public const string CryptSettingsPath = "Assets/_Project/Data/Levels/CryptSettings.asset";
        public const string CaveSettingsPath = "Assets/_Project/Data/Levels/CaveSettings.asset";
        private const string MapsFolder = "Assets/_Project/Levels";
        private const int PixelsPerCell = 14;

        private static readonly Vector2Int[] Steps = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        private ObjectField _settingsField;
        private LongField _seedField;
        private IntegerField _depthField;
        private Label _stats;
        private Image _image;
        private Texture2D _texture;

        /// <summary>La mappa disegnata adesso; null prima della prima generazione.</summary>
        public LevelMap Map { get; private set; }

        public Texture2D Preview => _texture;

        public string Stats => _stats.text;

        [MenuItem("DarkDescent/Generatore di dungeon")]
        public static DungeonGeneratorWindow Open()
        {
            var window = GetWindow<DungeonGeneratorWindow>();
            window.titleContent = new GUIContent("Generatore di dungeon");
            return window;
        }

        // CreateGUI può arrivare dopo GetWindow: chi chiama Generate subito (un test) costruisce prima
        private void CreateGUI()
        {
            EnsureGUI();
        }

        private void EnsureGUI()
        {
            if (_seedField != null)
            {
                return;
            }

            var root = rootVisualElement;
            root.style.paddingLeft = root.style.paddingRight = root.style.paddingTop = 6;

            _settingsField = new ObjectField("Numeri del livello")
            {
                objectType = typeof(DungeonSettings),
                value = AssetDatabase.LoadAssetAtPath<DungeonSettings>(CryptSettingsPath),
            };
            _seedField = new LongField("Seme della partita") { value = 4711 };
            _depthField = new IntegerField("Profondità") { value = 1 };
            _settingsField.RegisterValueChangedCallback(_ => Generate());
            _seedField.RegisterValueChangedCallback(_ => Generate());
            _depthField.RegisterValueChangedCallback(_ => Generate());

            var buttons = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 4, marginBottom = 4 } };
            buttons.Add(new Button(() => _seedField.value -= 1) { text = "◀ Seme precedente" });
            buttons.Add(new Button(() => _seedField.value += 1) { text = "Seme successivo ▶" });
            buttons.Add(new Button(Save) { text = "Salva come mappa" });

            var kinds = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 4 } };
            kinds.Add(new Button(() => Use(CryptSettingsPath)) { text = "Cripta (1–4)" });
            kinds.Add(new Button(() => Use(CaveSettingsPath)) { text = "Caverne (5–8)" });

            _stats = new Label { style = { marginBottom = 4, whiteSpace = WhiteSpace.Normal } };
            _image = new Image { scaleMode = ScaleMode.ScaleToFit };

            root.Add(kinds);
            root.Add(_settingsField);
            root.Add(_seedField);
            root.Add(_depthField);
            root.Add(buttons);
            root.Add(_stats);
            root.Add(new Label("Verde l'ingresso, giallo la scala, rosso gli scheletri, arancio le casse, chiaro le torce, marrone la scenografia."));
            root.Add(_image);
            Generate();
        }

        /// <summary>Le impostazioni di un tipo di livello, dalla prima profondità: per i pulsanti e per i test.</summary>
        public void Use(string settingsPath)
        {
            EnsureGUI();
            var settings = AssetDatabase.LoadAssetAtPath<DungeonSettings>(settingsPath);
            _settingsField.SetValueWithoutNotify(settings);
            _depthField.SetValueWithoutNotify(settings.FirstDepth);
            Generate();
        }

        /// <summary>Genera e disegna il livello di questo seme a questa profondità.</summary>
        public void Generate(long seed, int depth)
        {
            EnsureGUI();
            // i campi avvisano solo se il valore cambia: si imposta senza avviso e si genera una volta
            _seedField.SetValueWithoutNotify(seed);
            _depthField.SetValueWithoutNotify(depth);
            Generate();
        }

        private void Generate()
        {
            if (!(_settingsField.value is DungeonSettings settings))
            {
                _stats.text = "Manca un DungeonSettings.";
                return;
            }

            int depth = Mathf.Clamp(_depthField.value, settings.FirstDepth, settings.LastDepth);
            Map = DungeonLevel.CreateMap(settings, (ulong)_seedField.value, depth, out var layout);

            if (_texture == null || _texture.width != Map.Width || _texture.height != Map.Height)
            {
                DestroyImmediate(_texture);
                _texture = MapPainter.CreateTexture(Map);
            }

            MapPainter.Paint(Map, _texture);
            _image.image = _texture;
            _image.style.width = Map.Width * PixelsPerCell;
            _image.style.height = Map.Height * PixelsPerCell;
            _stats.text = Describe(Map, layout);
        }

        private static string Describe(LevelMap map, DungeonLayout layout)
        {
            int floor = 0, enemies = 0, swarm = 0, brutes = 0, chests = 0;
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    char c = map.GetSymbol(x, y);
                    floor += map.IsFloor(x, y) ? 1 : 0;
                    enemies += c == DungeonPopulator.EnemySymbol ? 1 : 0;
                    swarm += c == CavePopulator.SwarmSymbol ? 1 : 0;
                    brutes += c == CavePopulator.BruteSymbol ? 1 : 0;
                    chests += c == DungeonPopulator.ChestSymbol ? 1 : 0;
                }
            }

            int steps = StepsToStairs(map, layout.Entrance, layout.Stairs);
            string stairs = map.GetSymbol(layout.Stairs.x, layout.Stairs.y) == DungeonGenerator.StairsSymbol
                ? $"scala a {steps} passi dall'ingresso"
                : "nessuna scala (ultima profondità)";
            string rooms = layout.Rooms.Count > 0 ? $"{layout.Rooms.Count} stanze · " : "";
            string swarms = (swarm > 0 ? $" · {swarm} dello sciame" : "") + (brutes > 0 ? $" · {brutes} bruti" : "");
            return $"{rooms}{floor} celle di pavimento · {enemies} scheletri{swarms} · {chests} casse · {stairs}";
        }

        // passi a piedi, girando attorno a casse e scenografia
        private static int StepsToStairs(LevelMap map, Vector2Int from, Vector2Int stairs)
        {
            var distance = new Dictionary<Vector2Int, int> { [from] = 0 };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                if (cell == stairs)
                {
                    return distance[cell];
                }

                foreach (var step in Steps)
                {
                    var next = cell + step;
                    if (map.IsFloor(next.x, next.y) && !DungeonPopulator.IsObstacle(map.GetSymbol(next.x, next.y)) && !distance.ContainsKey(next))
                    {
                        distance[next] = distance[cell] + 1;
                        queue.Enqueue(next);
                    }
                }
            }

            return -1;
        }

        // Nella cartella delle mappe a mano: "Ricostruisci i livelli dalle mappe" ne fa una scena di prova.
        private void Save()
        {
            if (Map == null)
            {
                return;
            }

            string kind = _settingsField.value != null ? _settingsField.value.name.Replace("Settings", "") : "Level";
            string path = $"{MapsFolder}/{kind}_{_seedField.value}_{_depthField.value}.txt";
            File.WriteAllText(path, Map.ToText($"{kind} generato: seme della partita {_seedField.value}, profondità {_depthField.value}."));
            AssetDatabase.ImportAsset(path);
            Debug.Log($"Mappa salvata in {path}.");
        }

        private void OnDestroy()
        {
            DestroyImmediate(_texture);
        }
    }
}
