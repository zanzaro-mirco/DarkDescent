using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// I numeri della cripta generata (D2 e D6 della scheda M6): misure del livello, delle zone del
    /// BSP e delle stanze. Dati immutabili: si ritarano qui, senza toccare il generatore.
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

        public int Width => _width;

        public int Height => _height;

        public int MinLeaf => _minLeaf;

        public int MaxLeaf => _maxLeaf;

        public int MinRoom => _minRoom;

        public int MaxRoom => _maxRoom;
    }
}
