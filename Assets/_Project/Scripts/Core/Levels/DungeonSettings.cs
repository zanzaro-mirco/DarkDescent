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

        [Tooltip("Una torcia in più ogni tante celle di larghezza della stanza, oltre la prima.")]
        [SerializeField, Min(2)] private int _cellsPerTorch = 4;

        public int Width => _width;

        public int Height => _height;

        public int MinLeaf => _minLeaf;

        public int MaxLeaf => _maxLeaf;

        public int MinRoom => _minRoom;

        public int MaxRoom => _maxRoom;

        public string SceneName => _sceneName;

        public int FirstDepth => _firstDepth;

        public int LastDepth => _lastDepth;

        public string NextScene => _nextScene;

        public int MinGroup => _minGroup;

        public int MaxGroup => _maxGroup;

        public int MaxPropsPerRoom => _maxPropsPerRoom;

        public int CellsPerTorch => _cellsPerTorch;

        public int EnemyCount(int depth)
        {
            return _enemiesBase + _enemiesPerDepth * depth;
        }

        public int ChestCount(int depth)
        {
            return _chestsBase + depth / _depthsPerChest;
        }

        /// <summary>L'algoritmo di questi numeri: la cripta a BSP; le caverne lo cambiano.</summary>
        public virtual ILevelGenerator CreateGenerator()
        {
            return new DungeonGenerator(this);
        }
    }
}
