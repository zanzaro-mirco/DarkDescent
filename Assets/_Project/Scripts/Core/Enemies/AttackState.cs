namespace DarkDescent.Enemies
{
    /// <summary>
    /// Colpisce: il colpo si chiede a ogni frame, così finito uno ne parte un altro appena l'arma è
    /// pronta. Fuori portata e senza un colpo in volo torna a inseguire; con il bersaglio morto, fermo.
    /// Chase e Attack fanno la stessa richiesta, ma dicono cose diverse a chi guarda (test, animazioni).
    /// </summary>
    public sealed class AttackState : EnemyStateBase
    {
        public override EnemyState Id => EnemyState.Attack;

        public override EnemyState Tick(IEnemyBody body, float deltaTime)
        {
            if (!body.IsTargetAlive)
            {
                body.Disengage();
                return EnemyState.Idle;
            }

            body.Engage();
            return !body.IsTargetInRange && !body.IsSwinging ? EnemyState.Chase : Id;
        }
    }
}
