using System;
using System.Collections.Generic;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Un campione nella mappa (M12): la cella del nemico promosso e i suoi modificatori. Nella
    /// direttiva <c>@champions</c> è una parola sola, "x,y,id,id": il builder la legge e rende campione
    /// il nemico che mette in quella cella.
    /// </summary>
    public readonly struct ChampionSpawn
    {
        public const string Directive = "champions";

        private readonly string[] _modifiers;

        public ChampionSpawn(int x, int y, IReadOnlyList<string> modifiers)
        {
            X = x;
            Y = y;
            _modifiers = new string[modifiers != null ? modifiers.Count : 0];
            for (int i = 0; i < _modifiers.Length; i++)
            {
                _modifiers[i] = modifiers[i];
            }
        }

        public int X { get; }

        public int Y { get; }

        public IReadOnlyList<string> Modifiers => _modifiers ?? Array.Empty<string>();

        /// <summary>La parola della direttiva: "x,y,id,id".</summary>
        public string ToToken()
        {
            return X + "," + Y + (Modifiers.Count > 0 ? "," + string.Join(",", Modifiers) : string.Empty);
        }

        /// <summary>Legge una parola della direttiva; false se non è "x,y" seguito da id.</summary>
        public static bool TryParse(string token, out ChampionSpawn spawn)
        {
            spawn = default;
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            var parts = token.Split(',');
            if (parts.Length < 2 || !int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y))
            {
                return false;
            }

            var modifiers = new List<string>(parts.Length - 2);
            for (int i = 2; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    modifiers.Add(parts[i]);
                }
            }

            spawn = new ChampionSpawn(x, y, modifiers);
            return true;
        }

        /// <summary>Tutti i campioni di una mappa, dalla sua direttiva; le parole sbagliate si saltano.</summary>
        public static List<ChampionSpawn> FromMap(LevelMap map)
        {
            var spawns = new List<ChampionSpawn>();
            foreach (var token in map.GetDirective(Directive))
            {
                if (TryParse(token, out var spawn))
                {
                    spawns.Add(spawn);
                }
            }

            return spawns;
        }
    }
}
