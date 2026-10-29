using System;
using System.Collections.Generic;
using DarkDescent.Audio;
using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Town;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Il "chi c'è" di un livello: ingressi, uscite e nemici della sua scena. Sta su un oggetto alla
    /// radice della scena del livello, dove il LevelManager lo cerca dopo il caricamento, e gli
    /// riporta le uscite usate: il LevelManager sta in Core e le uscite non lo conoscono.
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelContext : MonoBehaviour
    {
        [Tooltip("La profondità del livello, dalla direttiva @depth della mappa: è il livello degli oggetti che ci cadono. 0 è la città.")]
        [SerializeField, Min(0)] private int _depth = 1;

        [Tooltip("I moduli da cui è costruito: suono, passi e nome del livello. Vuoto in una scena di prova: suono predefinito di Core, niente passi né nome.")]
        [SerializeField] private LevelTileset _tileset;

        private readonly List<LevelEntrance> _entrances = new List<LevelEntrance>();
        private readonly List<LevelExit> _exits = new List<LevelExit>();
        private readonly List<EnemyAI> _enemies = new List<EnemyAI>();
        private readonly List<Chest> _chests = new List<Chest>();
        private readonly List<Breakable> _breakables = new List<Breakable>();
        private readonly List<OneUseFeature> _features = new List<OneUseFeature>();
        private readonly List<SpikeTrap> _traps = new List<SpikeTrap>();
        private readonly List<Merchant> _merchants = new List<Merchant>();

        /// <summary>Il player ha raggiunto un'uscita.</summary>
        public event Action<LevelExit> ExitRequested;

        /// <summary>I nemici presenti al caricamento. Quelli morti e spariti restano nella lista come null di Unity.</summary>
        public IReadOnlyList<EnemyAI> Enemies => _enemies;

        public IReadOnlyList<LevelExit> Exits => _exits;

        /// <summary>Le casse da aprire (dalla M6): il loot le collega come i nemici.</summary>
        public IReadOnlyList<Chest> Chests => _chests;

        /// <summary>I barili da rompere (D5 della M13), nell'ordine in cui il livello li costruisce.</summary>
        public IReadOnlyList<Breakable> Breakables => _breakables;

        /// <summary>I pozzi e i santuari (D6 e D7 della M13), nell'ordine in cui il livello li costruisce.</summary>
        public IReadOnlyList<OneUseFeature> Features => _features;

        /// <summary>Gli spuntoni (D8 della M13).</summary>
        public IReadOnlyList<SpikeTrap> Traps => _traps;

        /// <summary>Il mercante della città, quello di anelli e amuleti (D5 della M10); null negli altri livelli.</summary>
        public Merchant Merchant => Trading(ShopGoods.Jewelry);

        /// <summary>I banchi del borgo, mercante, fabbro e guaritrice (D18 della M18); vuoto negli altri livelli.</summary>
        public IReadOnlyList<Merchant> Merchants => _merchants;

        /// <summary>Il banco che tratta <paramref name="goods"/>: il fabbro le armi, la guaritrice le pozioni; null se non c'è.</summary>
        public Merchant Trading(ShopGoods goods)
        {
            foreach (var merchant in _merchants)
            {
                if ((merchant.Goods & goods) == goods)
                {
                    return merchant;
                }
            }

            return null;
        }

        /// <summary>L'altare delle difficoltà della città (D3 della M14); null negli altri livelli.</summary>
        public DifficultyAltar Altar { get; private set; }

        /// <summary>Il boss della sala in fondo alle caverne (D8 della M10); null negli altri livelli.</summary>
        public BossController Boss { get; private set; }

        /// <summary>La porta della sala del boss; null se non c'è.</summary>
        public BossGate BossGate { get; private set; }

        /// <summary>Il giorno che passa (M18): solo nel borgo, altrimenti null.</summary>
        public Rendering.DayCycleView Daylight { get; private set; }

        public int EntranceCount => _entrances.Count;

        public int Depth => _depth;

        /// <summary>La città (D3 della M10): profondità 0, senza nemici, da dove si scende alla cripta.</summary>
        public bool IsTown => _depth == 0;

        public LevelTileset Tileset => _tileset;

        public AmbienceProfile Ambience => _tileset != null ? _tileset.Ambience : null;

        /// <summary>
        /// La mappa da cui è nato il livello, per l'automappa (D15 della M6). C'è solo per i livelli
        /// costruiti a runtime: in quelli salvati come scena non si serializza, e l'automappa resta spenta.
        /// </summary>
        public LevelMap Map { get; private set; }

        /// <summary>Per chi costruisce il livello (<see cref="LevelBuilder"/>), prima che si accenda.</summary>
        public void Configure(int depth, LevelMap map = null, LevelTileset tileset = null)
        {
            _depth = Mathf.Max(0, depth);
            Map = map;
            _tileset = tileset;
        }

        private void Awake()
        {
            // tutta la scena, non solo i figli: nemici e ingressi non devono stare sotto questo oggetto
            var entrances = new List<LevelEntrance>();
            var exits = new List<LevelExit>();
            var enemies = new List<EnemyAI>();
            var chests = new List<Chest>();
            var breakables = new List<Breakable>();
            var features = new List<OneUseFeature>();
            var traps = new List<SpikeTrap>();
            var merchants = new List<Merchant>();
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                root.GetComponentsInChildren(true, entrances);
                _entrances.AddRange(entrances);
                root.GetComponentsInChildren(true, exits);
                _exits.AddRange(exits);
                root.GetComponentsInChildren(true, enemies);
                _enemies.AddRange(enemies);
                root.GetComponentsInChildren(true, chests);
                _chests.AddRange(chests);
                root.GetComponentsInChildren(true, breakables);
                _breakables.AddRange(breakables);
                root.GetComponentsInChildren(true, features);
                _features.AddRange(features);
                root.GetComponentsInChildren(true, traps);
                _traps.AddRange(traps);
                root.GetComponentsInChildren(true, merchants);
                _merchants.AddRange(merchants);

                if (Altar == null)
                {
                    Altar = root.GetComponentInChildren<DifficultyAltar>(true);
                }

                if (Boss == null)
                {
                    Boss = root.GetComponentInChildren<BossController>(true);
                }

                if (BossGate == null)
                {
                    BossGate = root.GetComponentInChildren<BossGate>(true);
                }

                if (Daylight == null)
                {
                    Daylight = root.GetComponentInChildren<Rendering.DayCycleView>(true);
                }
            }
        }

        private void OnEnable()
        {
            foreach (var exit in _exits)
            {
                exit.Triggered += HandleExitTriggered;
            }
        }

        private void OnDisable()
        {
            foreach (var exit in _exits)
            {
                exit.Triggered -= HandleExitTriggered;
            }
        }

        private void HandleExitTriggered(LevelExit exit)
        {
            ExitRequested?.Invoke(exit);
        }

        public bool HasEntrance(string id)
        {
            return FindEntrance(id) != null;
        }

        /// <summary>
        /// L'ingresso con quell'id; se non c'è, il primo del livello. Succede nell'editor quando si
        /// preme Play con un livello aperto che non ha l'ingresso iniziale.
        /// </summary>
        public Transform GetEntrance(string id)
        {
            var entrance = FindEntrance(id);
            if (entrance != null)
            {
                return entrance.transform;
            }

            if (_entrances.Count == 0)
            {
                Debug.LogError($"Il livello {gameObject.scene.name} non ha ingressi.", this);
                return transform;
            }

            Debug.LogWarning($"Ingresso \"{id}\" assente in {gameObject.scene.name}: uso \"{_entrances[0].Id}\".", this);
            return _entrances[0].transform;
        }

        private LevelEntrance FindEntrance(string id)
        {
            foreach (var entrance in _entrances)
            {
                if (entrance.Id == id)
                {
                    return entrance;
                }
            }

            return null;
        }
    }
}
