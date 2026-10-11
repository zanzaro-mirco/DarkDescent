namespace DarkDescent.Enemies
{
    public enum EnemyState
    {
        Idle,
        Chase,
        Attack,

        /// <summary>Carica un colpo telegrafato: fermo, il settore a terra (il bruto, D6).</summary>
        WindUp,

        /// <summary>Dopo il colpo resta fermo: la finestra per colpirlo (D6).</summary>
        Recover,

        /// <summary>Si è allontanato troppo da casa: ci torna e guarisce (seconda prova della build M7).</summary>
        Return,
        Dead,

        /// <summary>A distanza (lo scheletro mago, D6 della M9): si avvicina finché ha il tiro, poi lancia.</summary>
        Ranged,

        /// <summary>Il cavaliere è troppo vicino: si allontana, poi torna a lanciare.</summary>
        Retreat,
    }
}
