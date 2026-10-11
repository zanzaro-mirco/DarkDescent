using UnityEngine;
using UnityEngine.SceneManagement;

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
            _context = new LevelBuilder(_tileset).Build(LevelMap.Parse(_map.text), activateEnemies: false);
            Bake(_context);
            return _context;
        }
    }
}
