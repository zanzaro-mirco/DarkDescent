using UnityEngine;

namespace DarkDescent.Town
{
    /// <summary>
    /// Il saluto di un abitante del borgo (D9 della M18): quando arriva il cavaliere saluta per un po',
    /// poi torna alla sua posa. Il peso del saluto sale e scende in <c>blend</c> secondi, senza salti:
    /// un nuovo arrivo a metà della discesa riparte dal peso che c'è. Logica pura.
    /// </summary>
    public sealed class Greeting
    {
        private readonly float _duration;
        private readonly float _blend;
        private float _elapsed;
        private bool _active;

        public Greeting(float duration, float blend)
        {
            _blend = Mathf.Max(0.01f, blend);
            _duration = Mathf.Max(2f * _blend, duration);
        }

        /// <summary>Quanto pesa il saluto sulla posa: 0 fermo, 1 saluta.</summary>
        public float Weight
        {
            get
            {
                if (!_active)
                {
                    return 0f;
                }

                float rise = _elapsed / _blend;
                float fall = (_duration - _elapsed) / _blend;
                return Mathf.Clamp01(Mathf.Min(rise, fall));
            }
        }

        public bool IsWaving => _active;

        /// <summary>Il cavaliere è arrivato: si saluta da capo.</summary>
        public void Begin()
        {
            _elapsed = Weight * _blend;
            _active = true;
        }

        public void Tick(float deltaTime)
        {
            if (!_active)
            {
                return;
            }

            _elapsed += Mathf.Max(0f, deltaTime);
            if (_elapsed >= _duration)
            {
                _active = false;
            }
        }
    }
}
