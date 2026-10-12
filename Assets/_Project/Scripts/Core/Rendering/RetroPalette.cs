using UnityEngine;

namespace DarkDescent.Rendering
{
    /// <summary>
    /// I colori del filtro retrò (D2 della M11): 32, nello spazio sRGB come si scelgono a occhio. Tanti
    /// scuri per il buio, caldi per fuochi e oro, freddi per le ombre, il verde delle caverne, il rosso
    /// del sangue e il viola della magia. Lo shader porta ogni pixel al più vicino.
    /// </summary>
    [CreateAssetMenu(menuName = "DarkDescent/Retro Palette", fileName = "RetroPalette")]
    public sealed class RetroPalette : ScriptableObject
    {
        /// <summary>Quanti colori legge lo shader: la palette deve averne esattamente tanti.</summary>
        public const int Size = 32;

        [SerializeField] private Color[] _colors = new Color[Size];

        public int Count => _colors != null ? _colors.Length : 0;

        public Color this[int index] => _colors[index];

        /// <summary>Il colore della palette più vicino, con i pesi della luminosità: per i test e per l'editor.</summary>
        public Color Nearest(Color color)
        {
            Color best = _colors[0];
            float bestDistance = float.MaxValue;
            foreach (var candidate in _colors)
            {
                float distance = Distance(color, candidate);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best;
        }

        /// <summary>La distanza che usa lo shader: il verde pesa di più, il blu di meno.</summary>
        public static float Distance(Color a, Color b)
        {
            float r = a.r - b.r;
            float g = a.g - b.g;
            float bl = a.b - b.b;
            return 0.3f * r * r + 0.59f * g * g + 0.11f * bl * bl;
        }
    }
}
