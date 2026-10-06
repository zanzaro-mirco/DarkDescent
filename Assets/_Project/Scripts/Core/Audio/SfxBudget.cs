using System;

namespace DarkDescent.Audio
{
    /// <summary>
    /// Quali suoni dei personaggi si sentono (D9 della M7). Al più uno per tipo per fotogramma:
    /// dieci colpi dello sciame nello stesso istante suonano come uno. E al più <see cref="MaxVoices"/>
    /// insieme: oltre, un suono nuovo passa solo se è più vicino del più lontano tra quelli in corso,
    /// che lascia il posto. Logica pura, con fotogramma, tempo e distanza passati da fuori; niente
    /// allocazioni dopo la costruzione.
    /// </summary>
    public sealed class SfxBudget
    {
        private readonly float[] _endTimes;
        private readonly float[] _distances;
        private readonly int[] _kindFrames;
        private int _count;

        public SfxBudget(int maxVoices)
        {
            if (maxVoices < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxVoices));
            }

            _endTimes = new float[maxVoices];
            _distances = new float[maxVoices];
            _kindFrames = new int[Enum.GetValues(typeof(SfxKind)).Length];
            for (int i = 0; i < _kindFrames.Length; i++)
            {
                _kindFrames[i] = -1;
            }
        }

        public int MaxVoices => _endTimes.Length;

        /// <summary>I suoni ancora in corso all'ultima richiesta.</summary>
        public int ActiveVoices => _count;

        /// <summary>
        /// Chiede di suonare: vero se il suono passa, e da quel momento conta tra le voci fino a
        /// <paramref name="now"/> + <paramref name="duration"/>.
        /// </summary>
        public bool Request(SfxKind kind, int frame, float now, float distance, float duration)
        {
            if (_kindFrames[(int)kind] == frame)
            {
                return false;
            }

            Expire(now);
            int slot = _count;
            if (_count == MaxVoices)
            {
                slot = Farthest();
                if (distance >= _distances[slot])
                {
                    return false;
                }
            }
            else
            {
                _count++;
            }

            _endTimes[slot] = now + duration;
            _distances[slot] = distance;
            _kindFrames[(int)kind] = frame;
            return true;
        }

        /// <summary>
        /// La priorità di Unity per un suono a questa distanza: 0 è la più alta. Vicino al cavaliere
        /// 64, ogni metro un po' meno: se Unity deve togliere una voce, toglie quella lontana.
        /// </summary>
        public static int Priority(float distance)
        {
            return Math.Max(0, Math.Min(255, 64 + (int)(distance * 6f)));
        }

        // i suoni finiti liberano il posto: l'ultimo prende il posto di quello tolto
        private void Expire(float now)
        {
            for (int i = _count - 1; i >= 0; i--)
            {
                if (_endTimes[i] <= now)
                {
                    _count--;
                    _endTimes[i] = _endTimes[_count];
                    _distances[i] = _distances[_count];
                }
            }
        }

        private int Farthest()
        {
            int farthest = 0;
            for (int i = 1; i < _count; i++)
            {
                if (_distances[i] > _distances[farthest])
                {
                    farthest = i;
                }
            }

            return farthest;
        }
    }
}
