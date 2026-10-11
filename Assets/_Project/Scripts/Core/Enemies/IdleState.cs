namespace DarkDescent.Enemies
{
    /// <summary>
    /// Fermo, guarda attorno ogni tanto: un raggio a ogni frame per ogni nemico costerebbe troppo. Il
    /// conto alla rovescia resta tra un'entrata e l'altra, come prima: tornato fermo, il nemico guarda
    /// subito se il conto è già scaduto.
    /// </summary>
    public sealed class IdleState : EnemyStateBase
    {
        private readonly float _perceptionInterval;
        private readonly EnemyState _engage;
        private float _timer;

        /// <param name="engage">Dove va quando vede il bersaglio: l'inseguimento, o il combattimento a distanza del mago.</param>
        public IdleState(float perceptionInterval, EnemyState engage = EnemyState.Chase)
        {
            _perceptionInterval = perceptionInterval;
            _engage = engage;
        }

        public override EnemyState Id => EnemyState.Idle;

        public override EnemyState Tick(IEnemyBody body, float deltaTime)
        {
            _timer -= deltaTime;
            if (_timer > 0f)
            {
                return Id;
            }

            _timer = _perceptionInterval;
            return body.IsTargetAlive && body.CanSeeTarget() ? _engage : Id;
        }
    }
}
