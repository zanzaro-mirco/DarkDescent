namespace DarkDescent.Combat
{
    /// <summary>Qualsiasi cosa possa ricevere un colpo: player, nemici, più avanti barili e porte.</summary>
    public interface IDamageable
    {
        // "in": la struct passa per riferimento in sola lettura, senza copia
        void TakeDamage(in DamageInfo info);

        /// <summary>Un colpo diretto qui è andato a vuoto: niente danno, ma chi lo mostra ("Mancato") deve saperlo.</summary>
        void Evade(in DamageInfo info);

        bool IsDead { get; }
    }
}
