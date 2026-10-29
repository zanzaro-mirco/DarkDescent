using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace DarkDescent.Levels
{
    /// <summary>
    /// La scena di un livello fatto a mano che si costruisce a runtime dalla sua mappa di testo, con lo
    /// stesso builder dei livelli generati: la città (D3 della M10). Rispetto a una scena salvata
    /// dall'editor ha l'automappa, e ritoccare il livello vuol dire solo cambiare il testo.
    /// </summary>
    [DisallowMultipleComponent]
    public class MapLevel : BuiltLevel
    {
        [Tooltip("La mappa di testo del livello (formato di LevelMap). La profondità viene dalla sua direttiva @depth.")]
        [SerializeField] private TextAsset _map;

        [SerializeField] private LevelTileset _tileset;

        private LevelContext _context;

        public override LevelContext Build(ulong runSeed, int depth)
        {
            if (_context != null)
            {
                return _context;
            }

            // Instantiate e impostazioni di luce vanno nella scena attiva: dev'essere questa (trappola 4 della M6)
            SceneManager.SetActiveScene(gameObject.scene);
            var watch = Stopwatch.StartNew();
            var map = LevelMap.Parse(_map.text);
            long parsed = watch.ElapsedMilliseconds;

            _context = new LevelBuilder(_tileset).Build(map, activateEnemies: false);
            long built = watch.ElapsedMilliseconds;

            Bake(_context);
            long baked = watch.ElapsedMilliseconds;

            // il borgo deve stare sotto i 300 ms (D16 della M18)
            Timings = (parsed, built - parsed, baked - built);
            Debug.Log($"[DarkDescent] {gameObject.scene.name} costruito dalla mappa {_map.name}: "
                + $"lettura {Timings.generate} ms, costruzione {Timings.build} ms, NavMesh {Timings.bake} ms");
            return _context;
        }
    }
}
