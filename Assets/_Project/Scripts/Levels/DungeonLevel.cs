using System.Collections.Generic;
using System.Diagnostics;
using DarkDescent.Core;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace DarkDescent.Levels
{
    /// <summary>
    /// La scena di un livello generato (D9 della scheda M6): vuota finché il LevelManager non chiede
    /// una profondità. Allora genera la cripta dal seme della partita, la costruisce con lo stesso
    /// builder delle mappe a mano, cuoce il NavMesh dai collider e accende i nemici.
    /// </summary>
    [DisallowMultipleComponent]
    public class DungeonLevel : MonoBehaviour
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
        public static LevelMap CreateMap(DungeonSettings settings, ulong runSeed, int depth, string sceneName, out DungeonLayout layout)
        {
            layout = settings.CreateGenerator().Generate(SeedMixer.ForLevel(runSeed, depth), depth);
            var directives = new Dictionary<string, string[]>
            {
                ["depth"] = new[] { depth.ToString() },
                ["entrance"] = new[] { depth == 1 ? FirstEntrance : EntranceFromAbove },
            };

            // l'ultima profondità della cripta non ha la scala: le caverne arrivano alla M7
            if (depth < settings.LastDepth)
            {
                directives["exit"] = new[] { sceneName, EntranceFromAbove, (depth + 1).ToString() };
            }
            else
            {
                layout.Remove(layout.Stairs.x, layout.Stairs.y);
            }

            return layout.ToMap(directives);
        }

        /// <summary>Costruisce il livello alla profondità chiesta, una volta sola; le chiamate dopo la prima restituiscono quello.</summary>
        public LevelContext Build(ulong runSeed, int depth)
        {
            if (_context != null)
            {
                return _context;
            }

            // Instantiate e impostazioni di luce vanno nella scena attiva: dev'essere questa (trappola 4)
            SceneManager.SetActiveScene(gameObject.scene);
            var watch = Stopwatch.StartNew();

            ulong seed = SeedMixer.ForLevel(runSeed, depth);
            var map = CreateMap(_settings, runSeed, depth, gameObject.scene.name, out var layout);
            long generated = watch.ElapsedMilliseconds;

            _context = new LevelBuilder(_tileset).Build(map, activateEnemies: false);
            long built = watch.ElapsedMilliseconds;

            // dai collider: le mesh dei modelli non sono leggibili, e in build il NavMesh verrebbe vuoto (D5)
            var surface = _context.GetComponentInChildren<NavMeshSurface>();
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            long baked = watch.ElapsedMilliseconds;

            // i nemici si accendono sopra il NavMesh appena cotto: i loro agent ci si agganciano
            _context.transform.Find(LevelBuilder.EnemiesGroup).gameObject.SetActive(true);

            Timings = (generated, built - generated, baked - built);
            Debug.Log($"[DarkDescent] livello {depth} generato (seme {seed}): {layout.Rooms.Count} stanze, "
                + $"generazione {Timings.generate} ms, costruzione {Timings.build} ms, NavMesh {Timings.bake} ms");
            return _context;
        }
    }
}
