namespace DarkDescent.Enemies
{
    /// <summary>
    /// Quello che in più chiede la ronda (D7 della M24): sapere se questo nemico gira, andare al punto dopo, sapere se è
    /// arrivato, restare fermo e finire. Come <see cref="IFleeBody"/>, chi non gira non lo implementa.
    /// </summary>
    public interface IPatrolBody : IEnemyBody
    {
        /// <summary>Questo nemico fa la ronda (uno su quattro, e non coi test né con i grandi): se no resta fermo.</summary>
        bool IsPatroller { get; }

        /// <summary>I secondi che sta fermo al punto dove è arrivato (il primo vale anche per l'inizio).</summary>
        float PatrolPause { get; }

        /// <summary>Parte per il punto dopo, al passo della ronda (più lento di quando insegue).</summary>
        void WalkToNextPatrolPoint();

        /// <summary>È arrivato al punto, o non sta andando da nessuna parte.</summary>
        bool IsAtPatrolPoint { get; }

        /// <summary>Si ferma dov'è.</summary>
        void HoldAtPatrolPoint();

        /// <summary>Fine della ronda (ha visto il cavaliere, o torna fermo): la velocità di prima.</summary>
        void EndPatrol();
    }
}
