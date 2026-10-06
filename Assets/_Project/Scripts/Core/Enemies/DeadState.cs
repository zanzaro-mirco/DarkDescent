namespace DarkDescent.Enemies
{
    /// <summary>Morto: non decide più niente. Corpo, collider e sparizione li gestisce il componente.</summary>
    public sealed class DeadState : EnemyStateBase
    {
        public override EnemyState Id => EnemyState.Dead;

        public override void Enter(IEnemyBody body)
        {
            body.Disengage();
        }

        public override EnemyState Tick(IEnemyBody body, float deltaTime)
        {
            return Id;
        }
    }
}
