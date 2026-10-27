using UnityEngine;

namespace DarkDescent.Rendering
{
    /// <summary>
    /// Il giorno che passa nel borgo (D5 della M18): l'ora viene dal tempo di gioco della partita, già
    /// nel salvataggio, e un giro intero dura <see cref="CycleSeconds"/>. Ogni momento resta per un po',
    /// poi sfuma nel successivo in <see cref="TransitionSeconds"/>. La partita nuova comincia di giorno.
    /// Logica pura: stesso tempo, stesso momento.
    /// </summary>
    public static class DayCycle
    {
        public const float CycleSeconds = 1200f;
        public const float TransitionSeconds = 60f;

        // quanto dura ogni momento pieno, dal giorno: 8 + 2 + 4 + 2 minuti, più i quattro passaggi
        private static readonly DayMoment[] Order = { DayMoment.Day, DayMoment.Dusk, DayMoment.Night, DayMoment.Dawn };
        private static readonly float[] Hold = { 480f, 120f, 240f, 120f };

        public static DayPhase At(float playTime)
        {
            float t = Mathf.Repeat(playTime, CycleSeconds);
            for (int i = 0; i < Order.Length; i++)
            {
                var next = Order[(i + 1) % Order.Length];
                if (t < Hold[i])
                {
                    return new DayPhase(Order[i], next, 0f);
                }

                t -= Hold[i];
                if (t < TransitionSeconds)
                {
                    return new DayPhase(Order[i], next, t / TransitionSeconds);
                }

                t -= TransitionSeconds;
            }

            // solo per arrotondamento all'ultimo istante del giro
            return new DayPhase(DayMoment.Day, DayMoment.Dusk, 0f);
        }

        /// <summary>Quanto sono accese lanterne e finestre: spente di giorno, accese dal tramonto all'alba.</summary>
        public static float Lights(float playTime)
        {
            var phase = At(playTime);
            if (phase.From == DayMoment.Day)
            {
                return phase.Blend;
            }

            return phase.From == DayMoment.Dawn ? 1f - phase.Blend : 1f;
        }

        /// <summary>Il primo istante del giro in cui il momento è pieno: per le foto e per i test.</summary>
        public static float Start(DayMoment moment)
        {
            float t = 0f;
            for (int i = 0; i < Order.Length; i++)
            {
                if (Order[i] == moment)
                {
                    return t;
                }

                t += Hold[i] + TransitionSeconds;
            }

            return 0f;
        }
    }
}
