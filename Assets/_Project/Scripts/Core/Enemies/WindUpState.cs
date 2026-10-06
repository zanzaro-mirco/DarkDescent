namespace DarkDescent.Enemies
{
    /// <summary>
    /// Il bruto carica il colpo (D6 della M7): fermo, girato dov'era, con il settore rosso a terra. La
    /// durata è il ritardo dell'arma, che MeleeAttack conta da solo; qui si aspetta che il colpo finisca,
    /// arrivato, a vuoto o annullato, e poi si resta fermi.
    /// </summary>
    public sealed class WindUpState : EnemyStateBase
    {
        public override EnemyState Id => EnemyState.WindUp;

        public override EnemyState Tick(IEnemyBody body, float deltaTime)
        {
            return body.IsSwinging ? Id : EnemyState.Recover;
        }
    }
}
