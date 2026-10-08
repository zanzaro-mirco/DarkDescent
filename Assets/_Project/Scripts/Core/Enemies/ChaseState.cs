namespace DarkDescent.Enemies
{
    /// <summary>
    /// Insegue: chiede il colpo, e il corpo si avvicina da solo. A portata, o con un colpo già partito,
    /// passa all'attacco; con il bersaglio morto torna fermo; troppo lontano da casa ci torna.
    /// L'attacco non guarda la distanza da casa: un nemico che sta già colpendo finisce il duello.
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

            if (body.IsBeyondLeash)
            {
                return EnemyState.Return;
            }

            body.Engage();
            return body.IsTargetInRange || body.IsSwinging ? EnemyState.Attack : Id;
        }
    }
}
