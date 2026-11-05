namespace DarkDescent.Enemies
{
    /// <summary>
    /// Uno stato del nemico (D8 della M7): a ogni frame guarda il corpo e dice in quale stato andare.
    /// Restituire il proprio <see cref="Id"/> vuol dire restare. Il cambio vale dal frame dopo, come
    /// nello switch che c'era prima (trappola 4).
    /// </summary>
    public abstract class EnemyStateBase
    {
        public abstract EnemyState Id { get; }

        /// <summary>Appena entrati nello stato, prima del primo Tick.</summary>
        public virtual void Enter(IEnemyBody body)
        {
        }

        /// <summary>Appena prima di lasciare lo stato, qualunque sia il motivo (anche la morte o il ritorno a casa).</summary>
        public virtual void Exit(IEnemyBody body)
        {
        }

        public abstract EnemyState Tick(IEnemyBody body, float deltaTime);
    }
}
