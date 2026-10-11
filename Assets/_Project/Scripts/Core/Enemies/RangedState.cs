namespace DarkDescent.Enemies
{
    /// <summary>
    /// Combatte a distanza (D6 della M9): troppo lontano, o con un muro in mezzo, si avvicina; entro
    /// la portata si ferma e lancia appena la ricarica lo permette; se il bersaglio arriva troppo
    /// vicino si allontana. Appena entrato non scappa per un poco: un mago chiuso in un angolo resta e
    /// lancia invece di provare a fuggire a ogni frame (trappola 5).
    /// </summary>
    public sealed class RangedState : EnemyStateBase
    {
        private readonly float _retreatBelow;
        private readonly float _castRange;
        private readonly float _retreatPause;
        private readonly float _shotCheckInterval;
        private float _retreatWait;
        private float _shotTimer;
        private bool _clearShot;

        /// <param name="retreatBelow">Più vicino di così si allontana.</param>
        /// <param name="castRange">Entro questa distanza, con il tiro libero, lancia.</param>
        /// <param name="retreatPause">Secondi dopo l'ingresso in cui non si allontana.</param>
        /// <param name="shotCheckInterval">Ogni quanto controlla se ha il tiro libero.</param>
        public RangedState(float retreatBelow, float castRange, float retreatPause, float shotCheckInterval = 0.25f)
        {
            _retreatBelow = retreatBelow;
            _castRange = castRange;
            _retreatPause = retreatPause;
            _shotCheckInterval = shotCheckInterval;
        }

        public override EnemyState Id => EnemyState.Ranged;

        public override void Enter(IEnemyBody body)
        {
            _retreatWait = _retreatPause;
            _shotTimer = 0f;
        }

        public override EnemyState Tick(IEnemyBody body, float deltaTime)
        {
            var ranged = (IRangedBody)body;
            if (!body.IsTargetAlive)
            {
                body.Disengage();
                return EnemyState.Idle;
            }

            // un lancio partito si finisce, fermo
            if (ranged.IsCasting)
            {
                ranged.HoldPosition();
                return Id;
            }

            if (body.IsBeyondLeash)
            {
                return EnemyState.Return;
            }

            _retreatWait -= deltaTime;
            float distance = ranged.TargetDistance;
            if (distance < _retreatBelow && _retreatWait <= 0f)
            {
                return EnemyState.Retreat;
            }

            _shotTimer -= deltaTime;
            if (_shotTimer <= 0f)
            {
                _shotTimer = _shotCheckInterval;
                _clearShot = distance <= _castRange && ranged.HasClearShot();
            }

            if (distance > _castRange || !_clearShot)
            {
                ranged.Approach();
                return Id;
            }

            ranged.HoldPosition();
            if (ranged.IsCastReady)
            {
                ranged.Cast();
            }

            return Id;
        }
    }
}
