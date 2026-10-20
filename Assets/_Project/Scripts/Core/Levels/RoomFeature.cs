using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Il punto d'interesse di una stanza (D4 della M13): la stanza, che cosa e in quale cella. La cella
    /// è riservata anche quando l'oggetto arriva in un passo successivo della milestone, così gli arredi
    /// non la occupano.
    /// </summary>
    public readonly struct RoomFeature
    {
        public RoomFeature(int room, RoomFeatureKind kind, Vector2Int cell)
        {
            Room = room;
            Kind = kind;
            Cell = cell;
        }

        public int Room { get; }

        public RoomFeatureKind Kind { get; }

        public Vector2Int Cell { get; }
    }
}
