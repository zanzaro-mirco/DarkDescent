namespace DarkDescent.Enemies
{
    /// <summary>
    /// Insegue: chiede il colpo, e il corpo si avvicina da solo. A portata, o con un colpo già partito,
    /// passa all'attacco; con il bersaglio morto torna fermo.
    /// </summary>
    public sealed class ChaseState : EnemyStateBase
    {
        public override EnemyState Id => EnemyState.Chase;

        public override EnemyState Tick(IEnemyBody body, float deltaTime)
        {
            if (!body.IsTargetAlive)
            {
                body.Disengage();
                return EnemyState.Idle;
            }

            body.Engage();
            return body.IsTargetInRange || body.IsSwinging ? EnemyState.Attack : Id;
        }
    }
}
