using System.Collections.Generic;
using System.Diagnostics;
using DarkDescent.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace DarkDescent.Levels
{
    /// <summary>
    /// La scena di un livello generato (D9 della scheda M6): vuota finché il LevelManager non chiede
    /// una profondità. Allora genera la cripta o la caverna delle sue impostazioni dal seme della partita, la costruisce con lo stesso
    /// builder delle mappe a mano, cuoce il NavMesh dai collider e accende i nemici.
    /// </summary>
    [DisallowMultipleComponent]
    public class DungeonLevel : BuiltLevel
    {
        private const string FirstEntrance = "Start";
        private const string EntranceFromAbove = "FromAbove";

        [SerializeField] private DungeonSettings _settings;

        [SerializeField] private LevelTileset _tileset;

        private LevelContext _context;

        /// <summary>Quanto è costato l'ultimo livello, in millisecondi: generazione, costruzione, NavMesh (D4).</summary>
        public (long generate, long build, long bake) Timings { get; private set; }

        /// <summary>L'ingresso da cui si entra in un livello generato senza scale: quello della profondità 1 o quello in cima.</summary>
        public static string EntranceAt(int depth)
        {
            return depth == 1 ? FirstEntrance : EntranceFromAbove;
        }

        /// <summary>
        /// La mappa del livello di una partita a una profondità, con le direttive: la stessa che il gioco
        /// costruisce. La usa anche la finestra del generatore nell'editor, per vedere il livello di un seme.
        /// </summary>
        public static LevelMap CreateMap(DungeonSettings settings, ulong runSeed, int depth, out DungeonLayout layout)
        {
            ulong levelSeed = SeedMixer.ForLevel(runSeed, depth);
            layout = settings.CreateGenerator().Generate(levelSeed, depth);
            var directives = new Dictionary<string, string[]>
            {
                ["depth"] = new[] { depth.ToString() },
                ["entrance"] = new[] { EntranceAt(depth) },
            };

            // i nemici speciali (M12) dopo tutto il resto, con un seme loro: il livello di prima resta com'era
            var modifiers = settings.Champions != null ? settings.Champions.CollectModifierIds() : new List<string>();
            var escorts = new List<Vector2Int>();
            var champions = SpecialEnemyPlanner.Place(layout, depth, settings.Specials, modifiers, settings.UniqueDepth(runSeed) == depth,
                new SplitMix64Source(SeedMixer.ForSpecials(levelSeed)), escorts);
            if (escorts.Count > 0)
            {
                directives[SpecialEnemyPlanner.EscortDirective] = SpecialEnemyPlanner.EscortTokens(escorts);
            }

            if (champions.Count > 0)
            {
                var tokens = new string[champions.Count];
                for (int i = 0; i < tokens.Length; i++)
                {
                    tokens[i] = champions[i].ToToken();
                }

                directives[ChampionSpawn.Directive] = tokens;
            }

            // il tesoro del re in fondo alla sua sala (D8 della M12): una cella fissa, nessun tiro
            SpecialEnemyPlanner.PlaceKingsTreasure(layout);

            // le macchie a terra (D5 della M17) per ultime, con un seme loro e solo in una direttiva:
            // la griglia resta quella di prima
            var decals = DecalPlanner.Plan(layout, settings.DecalThemes, new SplitMix64Source(SeedMixer.ForDecals(levelSeed)));
            if (decals.Count > 0)
            {
                directives[DecalSpot.Directive] = DecalSpot.Tokens(decals);
            }

            // Prima dell'ultima profondità la scala riporta in questa scena, all'ultima porta alla scena
            // dopo (dalla cripta alle caverne, D3 della M7); senza una scena dopo la scala non c'è.
            string target = depth < settings.LastDepth ? settings.SceneName : settings.NextScene;
            if (!string.IsNullOrEmpty(target))
            {
                directives["exit"] = new[] { target, EntranceFromAbove, (depth + 1).ToString() };
            }
            else
            {
                layout.Remove(layout.Stairs.x, layout.Stairs.y);
            }

            return layout.ToMap(directives);
        }

        public override LevelContext Build(ulong runSeed, int depth)
        {
            if (_context != null)
            {
                return _context;
            }

            // Instantiate e impostazioni di luce vanno nella scena attiva: dev'essere questa (trappola 4)
            SceneManager.SetActiveScene(gameObject.scene);
            var watch = Stopwatch.StartNew();

            ulong seed = SeedMixer.ForLevel(runSeed, depth);
            var map = CreateMap(_settings, runSeed, depth, out _);
            long generated = watch.ElapsedMilliseconds;

            _context = new LevelBuilder(_tileset).Build(map, activateEnemies: false);
            long built = watch.ElapsedMilliseconds;

            Bake(_context);
            long baked = watch.ElapsedMilliseconds;

            Timings = (generated, built - generated, baked - built);
            Debug.Log($"[DarkDescent] livello {depth} generato da {_settings.name} (seme {seed}): "
                + $"generazione {Timings.generate} ms, costruzione {Timings.build} ms, NavMesh {Timings.bake} ms");
            return _context;
        }
    }
}
