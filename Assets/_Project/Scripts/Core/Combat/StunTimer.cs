using System;

namespace DarkDescent.Combat
{
    /// <summary>
    /// Lo stordimento di un nemico e la sua regola (M20, dal disegno dell'albero): il re, i campioni e
    /// gli unici ne prendono metà; finito uno stordimento, per <see cref="Immunity"/> secondi non se
    /// ne prende un altro, e nemmeno mentre dura; gli Inamovibili non si stordiscono mai. Logica pura:
    /// il tempo lo fa passare chi la usa.
    /// </summary>
    public sealed class StunTimer
    {
        /// <summary>I secondi dopo uno stordimento in cui non se ne prende un altro.</summary>
        public const float Immunity = 3f;

        /// <summary>I secondi di stordimento che restano; 0 se non è stordito.</summary>
        public float Remaining { get; private set; }

        /// <summary>I secondi in cui non si stordisce di nuovo, dopo lo stordimento.</summary>
        public float ImmuneFor { get; private set; }

        public bool IsStunned => Remaining > 0f;

        /// <summary>Uno stordimento è finito: il nemico riparte.</summary>
        public event Action Ended;

        /// <summary>I secondi che prende davvero: metà per il re, i campioni e gli unici, niente per un Inamovibile.</summary>
        public static float Scaled(float duration, bool reduced, bool unstoppable)
        {
            if (unstoppable || duration <= 0f)
            {
                return 0f;
            }

            return reduced ? duration * 0.5f : duration;
        }

        /// <summary>Stordisce per tanti secondi, già passati per <see cref="Scaled"/>; false se non si può.</summary>
        public bool TryStun(float duration)
        {
            if (duration <= 0f || IsStunned || ImmuneFor > 0f)
            {
                return false;
            }

            Remaining = duration;
            return true;
        }

        public void Advance(float deltaTime)
        {
            if (Remaining > 0f)
            {
                Remaining -= deltaTime;
                if (Remaining <= 0f)
                {
                    Remaining = 0f;
                    ImmuneFor = Immunity;
                    Ended?.Invoke();
                }

                return;
            }

            ImmuneFor = Math.Max(0f, ImmuneFor - deltaTime);
        }

        /// <summary>Senza stordimento né immunità: il nemico è tornato a casa o è morto.</summary>
        public void Clear()
        {
            Remaining = 0f;
            ImmuneFor = 0f;
        }
    }
}
