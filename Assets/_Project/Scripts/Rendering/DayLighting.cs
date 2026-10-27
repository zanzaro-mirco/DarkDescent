using UnityEngine;

namespace DarkDescent.Rendering
{
    /// <summary>
    /// Le luci dei quattro momenti del giorno nel borgo (D5 della M18), e il minimo della notte: anche
    /// al buio strade, banchi e grotta restano leggibili. Dati immutabili: la vista legge, non scrive.
    /// </summary>
    [CreateAssetMenu(fileName = "DayLighting", menuName = "DarkDescent/Day Lighting")]
    public class DayLighting : ScriptableObject
    {
        [SerializeField] private DayLook _dawn;
        [SerializeField] private DayLook _day;
        [SerializeField] private DayLook _dusk;
        [SerializeField] private DayLook _night;

        [Tooltip("La luce ambiente non scende mai sotto questo colore, canale per canale.")]
        [SerializeField] private Color _minimumAmbient = new Color(0.1f, 0.11f, 0.15f);

        public Color MinimumAmbient => _minimumAmbient;

        public DayLook Look(DayMoment moment)
        {
            switch (moment)
            {
                case DayMoment.Dawn: return _dawn;
                case DayMoment.Day: return _day;
                case DayMoment.Dusk: return _dusk;
                default: return _night;
            }
        }

        /// <summary>La luce del giorno a questo punto, con il passaggio addolcito e il minimo della notte.</summary>
        public DayLook Look(DayPhase phase)
        {
            float t = phase.Blend * phase.Blend * (3f - 2f * phase.Blend);
            return DayLook.Lerp(Look(phase.From), Look(phase.To), t);
        }

        /// <summary>La luce ambiente con il minimo della notte.</summary>
        public Color Ambient(DayLook look)
        {
            var ambient = look.Ambient;
            return new Color(Mathf.Max(ambient.r, _minimumAmbient.r), Mathf.Max(ambient.g, _minimumAmbient.g), Mathf.Max(ambient.b, _minimumAmbient.b));
        }
    }
}
