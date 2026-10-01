namespace DarkDescent.Combat
{
    /// <summary>Qualsiasi cosa possa ricevere un colpo: player, nemici, più avanti barili e porte.</summary>
    public interface IDamageable
    {
        // "in": la struct passa per riferimento in sola lettura, senza copia
        void TakeDamage(in DamageInfo info);

        bool IsDead { get; }
    }
}
