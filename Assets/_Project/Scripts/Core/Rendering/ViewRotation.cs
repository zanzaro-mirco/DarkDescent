using DarkDescent.Levels;
using UnityEngine;

namespace DarkDescent.Rendering
{
    /// <summary>
    /// La rotazione della visuale (D12 della M8): a scatti di 90° attorno al cavaliere, animata in
    /// 0,4 s. La camera parte guardando a nord-est; ogni scatto a destra aggiunge 90° all'imbardata.
    /// L'orientamento dei muri cambia a metà rotazione: dalla metà in poi la camera è più vicina alla
    /// posizione nuova. Logica pura.
    /// </summary>
    public sealed class ViewRotation
    {
        public const float BaseYaw = 45f;

        public const float StepDegrees = 90f;

        public const float Duration = 0.4f;

        // in scatti dall'inizio, senza riportarli tra 0 e 3: l'imbardata non salta da 315° a 45°
        private int _steps;
        private float _from = BaseYaw;
        private float _elapsed = Duration;

        /// <summary>L'imbardata di adesso, in gradi, senza riportarla tra 0 e 360.</summary>
        public float Yaw { get; private set; } = BaseYaw;

        /// <summary>
        /// Da quale dei quattro lati si guarda, da 0 a 3: con 0 la camera guarda a nord-est. Cambia a
        /// metà di uno scatto.
        /// </summary>
        public int Facing => Wrap(Mathf.FloorToInt((Yaw - BaseYaw) / StepDegrees + 0.5f));

        /// <summary>Il lato da cui si guarderà a rotazione finita.</summary>
        public int TargetFacing => Wrap(_steps);

        public bool IsTurning => _elapsed < Duration;

        /// <summary>
        /// Uno scatto: +1 a destra, -1 a sinistra. Durante una rotazione la prossima parte da dove si
        /// è, verso uno scatto più in là.
        /// </summary>
        public void Turn(int direction)
        {
            if (direction == 0)
            {
                return;
            }

            _steps += direction > 0 ? 1 : -1;
            _from = Yaw;
            _elapsed = 0f;
        }

        /// <summary>Fa passare il tempo della rotazione. False se la camera era già ferma.</summary>
        public bool Advance(float deltaTime)
        {
            if (!IsTurning)
            {
                return false;
            }

            _elapsed = Mathf.Min(_elapsed + deltaTime, Duration);
            float target = BaseYaw + _steps * StepDegrees;
            float t = _elapsed / Duration;
            Yaw = IsTurning ? Mathf.Lerp(_from, target, t * t * (3f - 2f * t)) : target;
            return true;
        }

        /// <summary>
        /// Se il lato è lontano dalla camera, dove vanno i muri alti. Con il lato 0 la camera guarda
        /// a nord-est e lontani sono nord ed est; ogni scatto a destra sposta la coppia di un lato.
        /// </summary>
        public static bool IsFar(MapDirection side, int facing)
        {
            return Wrap((int)side - facing) <= 1;
        }

        private static int Wrap(int quarter)
        {
            return ((quarter % 4) + 4) % 4;
        }
    }
}
