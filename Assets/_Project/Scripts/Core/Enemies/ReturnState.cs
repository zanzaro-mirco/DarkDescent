namespace DarkDescent.Enemies
{
    /// <summary>
    /// Torna a casa (seconda prova della build M7): inseguendo si è allontanato troppo da dove l'ha
    /// messo il livello. Lascia il bersaglio e cammina fino a casa senza guardarsi attorno; arrivato,
    /// riprende la vita piena e torna fermo, pronto a vedere di nuovo il cavaliere.
    /// </summary>
    public sealed class ReturnState : EnemyStateBase
    {
        public override EnemyState Id => EnemyState.Return;

        public override void Enter(IEnemyBody body)
        {
            body.Disengage();
            body.GoHome();
        }

        public override EnemyState Tick(IEnemyBody body, float deltaTime)
        {
            if (!body.IsHome)
            {
                return Id;
            }

            body.ArriveHome();
            return EnemyState.Idle;
        }
    }
}
