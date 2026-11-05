namespace DarkDescent.Enemies
{
    /// <summary>
    /// Scappa (D2–D5 della M24): vita bassa o compagni morti vicino. Corre per qualche secondo verso un punto lontano dal
    /// cavaliere, senza colpire e senza guardarsi attorno, poi torna all'inseguimento e chiama i compagni. Non si cura: non è
    /// il ritorno a casa (ADR-050), e il punto è scelto entro un raggio da casa proprio per non uscirne. Se non c'è un punto
    /// che lo allontani torna subito all'inseguimento, e il corpo non riprova per un poco.
    /// </summary>
    public sealed class FleeState : EnemyStateBase
    {
        private readonly float _duration;
        private float _elapsed;
        private bool _started;

        /// <param name="duration">Secondi di fuga, anche se arriva prima: aspetta lì.</param>
        public FleeState(float duration)
        {
            _duration = duration;
        }

        public override EnemyState Id => EnemyState.Flee;

        public override void Enter(IEnemyBody body)
        {
            _elapsed = 0f;
            body.Disengage();
            _started = body is IFleeBody flee && flee.BeginFlee();
        }

        public override EnemyState Tick(IEnemyBody body, float deltaTime)
        {
            var flee = (IFleeBody)body;
            if (!_started)
            {
                return Resume(body);
            }

            _elapsed += deltaTime;
            if (_elapsed < _duration)
            {
                return Id;
            }

            flee.EndFlee();
            return Resume(body);
        }

        // il bersaglio può essere morto nel frattempo: allora torna fermo
        private static EnemyState Resume(IEnemyBody body)
        {
            return body.IsTargetAlive ? EnemyState.Chase : EnemyState.Idle;
        }
    }
}
