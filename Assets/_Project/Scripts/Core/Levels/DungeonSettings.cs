using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// I numeri della cripta generata (D2 e D6 della scheda M6): misure del livello, delle zone del
    /// BSP e delle stanze, quanti nemici, casse e oggetti di scena. Dati immutabili: si ritarano
    /// qui, senza toccare il generatore.
    /// </summary>
    [CreateAssetMenu(menuName = "DarkDescent/Dungeon Settings", fileName = "DungeonSettings")]
    public class DungeonSettings : ScriptableObject
    {
        [Tooltip("Larghezza del livello in celle da 4 m, bordo di roccia compreso.")]
        [SerializeField, Min(12)] private int _width = 28;

        [Tooltip("Altezza del livello in celle da 4 m, bordo di roccia compreso.")]
        [SerializeField, Min(12)] private int _height = 28;

        [Tooltip("Lato minimo di una zona del BSP: deve contenere la stanza più piccola con una cella di roccia per parte.")]
        [SerializeField, Min(5)] private int _minLeaf = 6;

        [Tooltip("Una zona più larga o più alta di così si divide ancora.")]
        [SerializeField, Min(10)] private int _maxLeaf = 11;

        [Tooltip("Lato minimo di una stanza, in celle.")]
        [SerializeField, Min(3)] private int _minRoom = 3;

        [Tooltip("Lato massimo di una stanza, in celle.")]
        [SerializeField, Min(3)] private int _maxRoom = 7;

        [Header("Profondità (D3 della M7)")]
        [Tooltip("La scena che genera questi livelli: le scale tra due profondità di questo tipo riportano qui.")]
        [SerializeField] private string _sceneName = "Level_Crypt";

        [Tooltip("La prima profondità di questo tipo di livello.")]
        [SerializeField, Min(1)] private int _firstDepth = 1;

        [Tooltip("L'ultima profondità di questo tipo: da qui la scala porta alla scena dopo, o non c'è.")]
        [SerializeField, Min(1)] private int _lastDepth = 4;

        [Tooltip("Dove porta la scala dell'ultima profondità, alla profondità successiva. Vuoto: la scala non c'è.")]
        [SerializeField] private string _nextScene = "";

        [Header("Contenuto (D6)")]
        [Tooltip("Scheletri = base + per profondità × profondità: 5 al livello 1, 11 al livello 4.")]
        [SerializeField, Min(0)] private int _enemiesBase = 3;

        [SerializeField, Min(0)] private int _enemiesPerDepth = 2;

        [Tooltip("Gli scheletri arrivano a gruppi, uno per stanza: da quanti a quanti.")]
        [SerializeField, Min(1)] private int _minGroup = 1;

        [SerializeField, Min(1)] private int _maxGroup = 3;

        [Tooltip("Casse = base + profondità / ogni quante profondità: 1, 2, 2, 3.")]
        [SerializeField, Min(0)] private int _chestsBase = 1;

        [SerializeField, Min(1)] private int _depthsPerChest = 2;

        [Tooltip("Barili, casse di legno e pilastri: al più tanti per stanza, contro i muri.")]
        [SerializeField, Min(0)] private int _maxPropsPerRoom = 2;

        [Tooltip("Arredi (D4 della M11): quante composizioni di mobili al massimo per stanza; 0 toglie tutti gli arredi, punti d'interesse e mucchietti compresi.")]
        [SerializeField, Min(0)] private int _maxDecorPerRoom = 4;

        [Header("Punto d'interesse (D4 della M13)")]
        [Tooltip("Probabilità che il punto d'interesse di una stanza di almeno 5 × 5 celle sia un gruppo di barili. Quello che manca all'1 tra le quattro è una stanza senza.")]
        [SerializeField, Range(0f, 1f)] private float _barrelsChance = 0.35f;

        [Tooltip("Probabilità di un pozzo della vita o del mana: al più uno per livello, un secondo diventa barili.")]
        [SerializeField, Range(0f, 1f)] private float _wellChance = 0.15f;

        [Tooltip("Probabilità di un santuario: al più uno per livello, un secondo diventa barili.")]
        [SerializeField, Range(0f, 1f)] private float _shrineChance = 0.15f;

        [Tooltip("Probabilità di una zona di trappole; prima della profondità delle trappole diventano barili.")]
        [SerializeField, Range(0f, 1f)] private float _trapsChance = 0.10f;

        [Tooltip("La prima profondità con le trappole.")]
        [SerializeField, Min(1)] private int _trapsFirstDepth = 3;

        [Tooltip("Quanti spuntoni per livello dalla prima profondità con le trappole, da (x) a (y), contando quelli del punto d'interesse (D8 della M13).")]
        [SerializeField] private Vector2Int _spikesPerLevel = new Vector2Int(0, 2);

        [Header("Barili da rompere (D5 della M13)")]
        [Tooltip("Quanti barili per livello, da (x) a (y): i gruppi contro i muri arrivano dopo gli arredi finché il livello ne ha almeno quanti dice il tiro. Barili singoli e gruppi al centro contano.")]
        [SerializeField] private Vector2Int _barrelsPerLevel = new Vector2Int(4, 8);

        [Space]

        [Tooltip("Mucchietti a terra (ossa, monete e carte, armi rotte) al più per stanza: non sono ostacoli.")]
        [SerializeField, Min(0)] private int _maxScatterPerRoom = 2;

        [Tooltip("Una torcia in più ogni tante celle di larghezza della stanza, oltre la prima.")]
        [SerializeField, Min(2)] private int _cellsPerTorch = 4;

        [Header("Scheletri maghi (D6 della M9)")]
        [Tooltip("La prima profondità con i maghi, nella cripta. Le caverne li prendono dalla loro tabella.")]
        [SerializeField, Min(1)] private int _magesFirstDepth = 3;

        [Tooltip("Quanti maghi per livello, da (x) a (y), dalla prima profondità.")]
        [SerializeField] private Vector2Int _mages = new Vector2Int(1, 2);

        [Header("Nemici speciali (M12)")]
        [SerializeField] private SpecialEnemyRules _specials = new SpecialEnemyRules();

        [Tooltip("I modificatori tra cui si scelgono quelli dei campioni. Vuoto: campioni senza modificatori.")]
        [SerializeField] private Enemies.ChampionSettings _champions;

        public SpecialEnemyRules Specials => _specials;

        public Enemies.ChampionSettings Champions => _champions;

        public int Width => _width;

        public int Height => _height;

        public int MinLeaf => _minLeaf;

        public int MaxLeaf => _maxLeaf;

        public int MinRoom => _minRoom;

        public int MaxRoom => _maxRoom;

        public string SceneName => _sceneName;

        public int FirstDepth => _firstDepth;

        public int LastDepth => _lastDepth;

        /// <summary>Vero se le profondità di questa scena comprendono <paramref name="depth"/>: il portale della città ci riporta.</summary>
        public bool Covers(int depth)
        {
            return depth >= _firstDepth && depth <= _lastDepth;
        }

        public string NextScene => _nextScene;

        public int MinGroup => _minGroup;

        public int MaxGroup => _maxGroup;

        public int MaxPropsPerRoom => _maxPropsPerRoom;

        public int MaxDecorPerRoom => _maxDecorPerRoom;


        public int MaxScatterPerRoom => _maxScatterPerRoom;

        public int CellsPerTorch => _cellsPerTorch;

        public Vector2Int BarrelsPerLevel => _barrelsPerLevel;

        public int TrapsFirstDepth => _trapsFirstDepth;

        public Vector2Int SpikesPerLevel => _spikesPerLevel;

        /// <summary>
        /// Il punto d'interesse che dice il tiro <paramref name="roll"/> (da 0 a 1) a questa profondità,
        /// prima del limite di un pozzo e un santuario per livello: barili, pozzo, santuario, trappole, e
        /// oltre la loro somma niente. Le trappole prima della loro profondità diventano barili.
        /// </summary>
        public RoomFeatureKind FeatureFor(double roll, int depth)
        {
            double at = _barrelsChance;
            if (roll < at)
            {
                return RoomFeatureKind.Barrels;
            }

            if (roll < (at += _wellChance))
            {
                return RoomFeatureKind.Well;
            }

            if (roll < (at += _shrineChance))
            {
                return RoomFeatureKind.Shrine;
            }

            if (roll < at + _trapsChance)
            {
                return depth >= _trapsFirstDepth ? RoomFeatureKind.Traps : RoomFeatureKind.Barrels;
            }

            return RoomFeatureKind.None;
        }

        /// <summary>Quanti maghi, da quanti a quanti, a questa profondità della cripta: nessuno prima della prima.</summary>
        public Vector2Int MagesAt(int depth)
        {
            return depth >= _magesFirstDepth ? _mages : Vector2Int.zero;
        }

        public int EnemyCount(int depth)
        {
            return _enemiesBase + _enemiesPerDepth * depth;
        }

        public int ChestCount(int depth)
        {
            return _chestsBase + depth / _depthsPerChest;
        }

        /// <summary>
        /// La profondità dell'unico di questo tipo di livello in un mondo, o -1 se non c'è (D6 della
        /// M12): dalla prima profondità con i nemici speciali all'ultima, senza quella del re.
        /// </summary>
        public int UniqueDepth(ulong runSeed)
        {
            return SpecialEnemyPlanner.UniqueDepth(runSeed, Mathf.Max(_firstDepth, _specials.MinDepth), LastUniqueDepth, _specials.UniqueChance);
        }

        /// <summary>L'ultima profondità dove può stare l'unico: le caverne tolgono quella del re.</summary>
        protected virtual int LastUniqueDepth => _lastDepth;

        /// <summary>L'algoritmo di questi numeri: la cripta a BSP; le caverne lo cambiano.</summary>
        public virtual ILevelGenerator CreateGenerator()
        {
            return new DungeonGenerator(this);
        }
    }
}
