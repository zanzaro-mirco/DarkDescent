using System;
using System.Collections.Generic;

namespace DarkDescent.Films
{
    /// <summary>
    /// La lista delle inquadrature di un filmato (D2 della M19), in fila: lo strumento dell'editor ne
    /// fa la Timeline, il lettore ne legge le didascalie. Logica pura, che si prova senza scene: le
    /// inquadrature non lasciano buchi e non si sovrappongono, e la durata sta nei limiti del filmato.
    /// </summary>
    public sealed class FilmScript
    {
        // la tolleranza sui secondi: i tempi si scrivono a mano, con i decimali
        private const float Epsilon = 0.001f;

        private readonly List<FilmShot> _shots;

        public FilmScript(string id, string titleKey, float minDuration, float maxDuration, IEnumerable<FilmShot> shots)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            TitleKey = titleKey;
            MinDuration = minDuration;
            MaxDuration = maxDuration;
            _shots = new List<FilmShot>(shots ?? throw new ArgumentNullException(nameof(shots)));
        }

        /// <summary>Il nome del filmato, che è anche quello della sua scena e della sua Timeline.</summary>
        public string Id { get; }

        /// <summary>Il nome nell'elenco del menu (D18), nella tabella delle stringhe.</summary>
        public string TitleKey { get; }

        public float MinDuration { get; }

        public float MaxDuration { get; }

        public IReadOnlyList<FilmShot> Shots => _shots;

        /// <summary>Dove finisce l'ultima inquadratura; 0 senza inquadrature.</summary>
        public float Duration => _shots.Count > 0 ? _shots[_shots.Count - 1].End : 0f;

        /// <summary>L'inquadratura al secondo <paramref name="time"/>; null prima dell'inizio e dopo la fine.</summary>
        public FilmShot ShotAt(float time)
        {
            foreach (var shot in _shots)
            {
                if (shot.Contains(time))
                {
                    return shot;
                }
            }

            return null;
        }

        /// <summary>La didascalia al secondo <paramref name="time"/>; null se l'inquadratura non ne ha.</summary>
        public string CaptionAt(float time) => ShotAt(time)?.CaptionKey;

        /// <summary>Il titolo al secondo <paramref name="time"/>; null se l'inquadratura non ne ha.</summary>
        public string TitleAt(float time) => ShotAt(time)?.TitleKey;

        /// <summary>
        /// Il tratto di una camera attorno al secondo <paramref name="time"/>: le inquadrature di fila con
        /// la stessa camera sono un movimento solo, che va da <paramref name="start"/> a
        /// <paramref name="end"/>, anche se la didascalia cambia. Falso prima dell'inizio e dopo la fine.
        /// </summary>
        public bool CameraSpan(float time, out string camera, out float start, out float end)
        {
            camera = null;
            start = end = 0f;
            int index = _shots.FindIndex(shot => shot.Contains(time));
            if (index < 0)
            {
                return false;
            }

            camera = _shots[index].Camera;
            int first = index;
            while (first > 0 && _shots[first - 1].Camera == camera)
            {
                first--;
            }

            int last = index;
            while (last < _shots.Count - 1 && _shots[last + 1].Camera == camera)
            {
                last++;
            }

            start = _shots[first].Start;
            end = _shots[last].End;
            return true;
        }

        /// <summary>
        /// Cosa non torna, una riga per errore; vuota se il filmato si può montare: la prima
        /// inquadratura comincia a 0, ognuna dove finisce la precedente, nessuna è vuota o senza
        /// camera, e la durata sta tra il minimo e il massimo.
        /// </summary>
        public IReadOnlyList<string> Problems()
        {
            var problems = new List<string>();
            if (_shots.Count == 0)
            {
                problems.Add($"{Id}: nessuna inquadratura");
                return problems;
            }

            float expected = 0f;
            for (int i = 0; i < _shots.Count; i++)
            {
                var shot = _shots[i];
                if (Math.Abs(shot.Start - expected) > Epsilon)
                {
                    problems.Add(shot.Start > expected
                        ? $"{Id}: buco prima dell'inquadratura {i + 1}, da {expected:0.##} a {shot.Start:0.##} s"
                        : $"{Id}: l'inquadratura {i + 1} comincia a {shot.Start:0.##} s, sopra la precedente che finisce a {expected:0.##}");
                }

                if (shot.Duration <= 0f)
                {
                    problems.Add($"{Id}: l'inquadratura {i + 1} dura {shot.Duration:0.##} s");
                }

                if (string.IsNullOrEmpty(shot.Camera))
                {
                    problems.Add($"{Id}: l'inquadratura {i + 1} senza camera");
                }

                expected = shot.End;
            }

            if (Duration < MinDuration - Epsilon || Duration > MaxDuration + Epsilon)
            {
                problems.Add($"{Id}: dura {Duration:0.##} s, fuori da {MinDuration:0.##}–{MaxDuration:0.##}");
            }

            return problems;
        }
    }
}
