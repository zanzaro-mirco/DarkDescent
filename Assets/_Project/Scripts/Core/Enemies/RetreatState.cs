namespace DarkDescent.Enemies
{
    /// <summary>
    /// Si allontana dal bersaglio troppo vicino (D6 della M9), verso un punto che sceglie il corpo.
    /// Arrivato, o dopo un tempo massimo, torna a combattere a distanza; se non c'è un punto dove
    /// andare, ci torna subito e lancia da dov'è.
    /// </summary>
    public sealed class RetreatState : EnemyStateBase
    {
        private readonly float _maxDuration;
        private float _timer;
        private bool _moving;

        public RetreatState(float maxDuration)
        {
            _maxDuration = maxDuration;
        }

        public override EnemyState Id => EnemyState.Retreat;

        public override void Enter(IEnemyBody body)
        {
            _timer = _maxDuration;
            _moving = ((IRangedBody)body).TryRetreat();
        }

        public override EnemyState Tick(IEnemyBody body, float deltaTime)
        {
            if (!body.IsTargetAlive)
            {
                body.Disengage();
                return EnemyState.Idle;
            }

            _timer -= deltaTime;
            if (!_moving || _timer <= 0f || ((IRangedBody)body).IsAtDestination)
            {
                return EnemyState.Ranged;
            }

            return Id;
        }
    }
}
