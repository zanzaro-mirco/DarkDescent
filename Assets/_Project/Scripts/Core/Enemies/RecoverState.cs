namespace DarkDescent.Enemies
{
    /// <summary>
    /// Dopo il colpo il bruto resta fermo (D6 della M7): non insegue e non colpisce, ed è la finestra
    /// per colpirlo. Finita, torna a inseguire se il bersaglio è vivo, altrimenti si ferma.
    /// </summary>
    public sealed class RecoverState : EnemyStateBase
    {
        private readonly float _duration;
        private float _timer;

        public RecoverState(float duration)
        {
            _duration = duration;
        }

        public override EnemyState Id => EnemyState.Recover;

        public override void Enter(IEnemyBody body)
        {
            _timer = _duration;
            body.Disengage();
        }

        public override EnemyState Tick(IEnemyBody body, float deltaTime)
        {
            _timer -= deltaTime;
            if (_timer > 0f)
            {
                return Id;
            }

            return body.IsTargetAlive ? EnemyState.Chase : EnemyState.Idle;
        }
    }
}
