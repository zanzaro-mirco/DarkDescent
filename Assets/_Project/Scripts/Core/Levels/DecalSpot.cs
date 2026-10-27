using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Una macchia a terra (D5 della M17): la cella, la casella dell'atlante, l'angolo, lo scarto dal
    /// centro e il lato. Nella direttiva <c>@decals</c> è una parola sola di interi,
    /// "x,y,casella,angolo,scartoX,scartoZ,lato", con scarti e lato in decimetri: il builder la legge e
    /// mette il quadrato sul pavimento. Non entra nel salvataggio: rinasce dal seme con la mappa.
    /// </summary>
    public readonly struct DecalSpot
    {
        public const string Directive = "decals";

        public DecalSpot(int x, int y, int tile, int angle, int offsetX, int offsetZ, int size)
        {
            X = x;
            Y = y;
            Tile = tile;
            Angle = angle;
            OffsetX = offsetX;
            OffsetZ = offsetZ;
            Size = size;
        }

        public int X { get; }

        public int Y { get; }

        /// <summary>La casella dell'atlante, riga per riga dall'alto (<see cref="DecalPlanner"/>).</summary>
        public int Tile { get; }

        /// <summary>In gradi, attorno all'asse verticale.</summary>
        public int Angle { get; }

        /// <summary>Lo scarto dal centro della cella lungo X del mondo, in decimetri.</summary>
        public int OffsetX { get; }

        /// <summary>Lo scarto dal centro della cella lungo Z del mondo, in decimetri.</summary>
        public int OffsetZ { get; }

        /// <summary>Il lato del quadrato, in decimetri.</summary>
        public int Size { get; }

        /// <summary>Il centro nel mondo, all'altezza del pavimento.</summary>
        public Vector3 Center => LevelMap.CellCenter(X, Y) + new Vector3(OffsetX * 0.1f, 0f, OffsetZ * 0.1f);

        /// <summary>
        /// Fin dove si vede la macchia dal centro della cella, lungo un asse: lo scarto più il mezzo lato.
        /// L'immagine sfuma fino a sparire sul cerchio inscritto nella casella (<c>fade</c> in
        /// <c>imagelib.py</c>), così gli angoli del quadrato, anche ruotato, sono trasparenti.
        /// </summary>
        public float Reach => Mathf.Max(Mathf.Abs(OffsetX), Mathf.Abs(OffsetZ)) * 0.1f + Size * 0.05f;

        public string ToToken()
        {
            return X + "," + Y + "," + Tile + "," + Angle + "," + OffsetX + "," + OffsetZ + "," + Size;
        }

        /// <summary>Legge una parola della direttiva; false se non sono sette interi.</summary>
        public static bool TryParse(string token, out DecalSpot spot)
        {
            spot = default;
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            var parts = token.Split(',');
            if (parts.Length != 7)
            {
                return false;
            }

            var values = new int[7];
            for (int i = 0; i < values.Length; i++)
            {
                if (!int.TryParse(parts[i], out values[i]))
                {
                    return false;
                }
            }

            spot = new DecalSpot(values[0], values[1], values[2], values[3], values[4], values[5], values[6]);
            return true;
        }

        /// <summary>Le parole della direttiva, nello stesso ordine.</summary>
        public static string[] Tokens(IReadOnlyList<DecalSpot> spots)
        {
            var tokens = new string[spots.Count];
            for (int i = 0; i < tokens.Length; i++)
            {
                tokens[i] = spots[i].ToToken();
            }

            return tokens;
        }

        /// <summary>Tutte le macchie di una mappa, dalla sua direttiva; le parole sbagliate si saltano.</summary>
        public static List<DecalSpot> FromMap(LevelMap map)
        {
            var spots = new List<DecalSpot>();
            foreach (var token in map.GetDirective(Directive))
            {
                if (TryParse(token, out var spot))
                {
                    spots.Add(spot);
                }
            }

            return spots;
        }

        /// <summary>
        /// Il quadrato della macchia nelle liste di una mesh: quattro vertici alzati di
        /// <paramref name="lift"/> sul pavimento, le coordinate della sua casella in un atlante di
        /// <paramref name="columns"/> × <paramref name="rows"/>, due triangoli rivolti in alto.
        /// </summary>
        public void AddQuad(List<Vector3> vertices, List<Vector2> uvs, List<int> triangles, float lift, int columns, int rows)
        {
            int first = vertices.Count;
            Vector3 center = Center + new Vector3(0f, lift, 0f);
            var rotation = Quaternion.Euler(0f, Angle, 0f);
            float half = Size * 0.05f;
            vertices.Add(center + rotation * new Vector3(-half, 0f, -half));
            vertices.Add(center + rotation * new Vector3(-half, 0f, half));
            vertices.Add(center + rotation * new Vector3(half, 0f, half));
            vertices.Add(center + rotation * new Vector3(half, 0f, -half));

            // la casella 0 è in alto a sinistra; le coordinate delle texture partono dal basso
            float u0 = (float)(Tile % columns) / columns;
            float u1 = u0 + 1f / columns;
            float v1 = 1f - (float)(Tile / columns) / rows;
            float v0 = v1 - 1f / rows;
            uvs.Add(new Vector2(u0, v0));
            uvs.Add(new Vector2(u0, v1));
            uvs.Add(new Vector2(u1, v1));
            uvs.Add(new Vector2(u1, v0));

            triangles.Add(first);
            triangles.Add(first + 1);
            triangles.Add(first + 2);
            triangles.Add(first);
            triangles.Add(first + 2);
            triangles.Add(first + 3);
        }
    }
}
