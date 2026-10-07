namespace DarkDescent.Enemies
{
    /// <summary>
    /// Colpisce: il colpo si chiede a ogni frame, così finito uno ne parte un altro appena l'arma è
    /// pronta. Fuori portata e senza un colpo in volo torna a inseguire; con il bersaglio morto, fermo.
    /// Chase e Attack fanno la stessa richiesta, ma dicono cose diverse a chi guarda (test, animazioni).
    /// </summary>
    public sealed class AttackState : EnemyStateBase
    {
        private readonly bool _telegraphed;

        /// <param name="telegraphed">Vero per il bruto: partito il colpo forte, si passa alla carica (D6); i colpi leggeri restano qui.</param>
        public AttackState(bool telegraphed = false)
        {
            _telegraphed = telegraphed;
        }

        public override EnemyState Id => EnemyState.Attack;

        public override EnemyState Tick(IEnemyBody body, float deltaTime)
        {
            if (!body.IsTargetAlive)
            {
                body.Disengage();
                return EnemyState.Idle;
            }

            body.Engage();
            if (_telegraphed && body.IsTelegraphedSwing)
            {
                return EnemyState.WindUp;
            }

            return !body.IsTargetInRange && !body.IsSwinging ? EnemyState.Chase : Id;
        }
    }
}
