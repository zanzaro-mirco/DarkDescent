namespace DarkDescent.Enemies
{
    /// <summary>
    /// La ronda (D7 della M24): sta fermo qualche secondo a un punto, va al successivo, e intanto guarda attorno come chi è
    /// fermo. Se vede il cavaliere insegue come gli altri. Finito l'inseguimento (o tornato a casa) il nemico torna fermo e
    /// da fermo riprende la ronda: <see cref="IdleState"/> lo manda qui. Se un punto non si raggiunge entro
    /// <see cref="MaxWalk"/> secondi (un compagno sulla strada) lo dà per raggiunto, invece di restare lì a spingere.
    /// </summary>
    public sealed class PatrolState : EnemyStateBase
    {
        /// <summary>Oltre tanti secondi di cammino un punto vale come raggiunto.</summary>
        public const float MaxWalk = 12f;

        private readonly float _perceptionInterval;
        private readonly EnemyState _engage;
        private float _timer;
        private float _wait;
        private float _walked;
        private bool _walking;

        public PatrolState(float perceptionInterval, EnemyState engage = EnemyState.Chase)
        {
            _perceptionInterval = perceptionInterval;
            _engage = engage;
        }

        public override EnemyState Id => EnemyState.Patrol;

        public override void Enter(IEnemyBody body)
        {
            _walking = false;
            _walked = 0f;
            _wait = body is IPatrolBody patrol ? patrol.PatrolPause : 0f;
        }

        public override void Exit(IEnemyBody body)
        {
            if (body is IPatrolBody patrol)
            {
                patrol.EndPatrol();
            }
        }

        public override EnemyState Tick(IEnemyBody body, float deltaTime)
        {
            var patrol = (IPatrolBody)body;
            if (!patrol.IsPatroller)
            {
                return EnemyState.Idle;
            }

            _timer -= deltaTime;
            if (_timer <= 0f)
            {
                _timer = _perceptionInterval;
                if (body.IsTargetAlive && body.CanSeeTarget())
                {
                    return _engage;
                }
            }

            if (!_walking)
            {
                _wait -= deltaTime;
                if (_wait <= 0f)
                {
                    patrol.WalkToNextPatrolPoint();
                    _walking = true;
                    _walked = 0f;
                }

                return Id;
            }

            _walked += deltaTime;
            if (patrol.IsAtPatrolPoint || _walked >= MaxWalk)
            {
                patrol.HoldAtPatrolPoint();
                _walking = false;
                _wait = patrol.PatrolPause;
            }

            return Id;
        }
    }
}
