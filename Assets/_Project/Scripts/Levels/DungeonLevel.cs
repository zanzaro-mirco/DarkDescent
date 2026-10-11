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

        /// <summary>
        /// La mappa del livello di una partita a una profondità, con le direttive: la stessa che il gioco
        /// costruisce. La usa anche la finestra del generatore nell'editor, per vedere il livello di un seme.
        /// </summary>
        public static LevelMap CreateMap(DungeonSettings settings, ulong runSeed, int depth, out DungeonLayout layout)
        {
            layout = settings.CreateGenerator().Generate(SeedMixer.ForLevel(runSeed, depth), depth);
            var directives = new Dictionary<string, string[]>
            {
                ["depth"] = new[] { depth.ToString() },
                ["entrance"] = new[] { depth == 1 ? FirstEntrance : EntranceFromAbove },
            };

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
