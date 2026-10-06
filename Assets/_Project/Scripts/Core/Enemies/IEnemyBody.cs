namespace DarkDescent.Enemies
{
    /// <summary>
    /// Quello che uno stato può chiedere al nemico (D8 della M7): vedere il bersaglio, sapere se è a
    /// portata, chiedere il colpo. Il componente lo fa con NavMesh, fisica e MeleeAttack; i test con un
    /// corpo finto, senza scene.
    /// </summary>
    public interface IEnemyBody
    {
        /// <summary>C'è un bersaglio, e non è morto.</summary>
        bool IsTargetAlive { get; }

        /// <summary>Il bersaglio è a portata del colpo.</summary>
        bool IsTargetInRange { get; }

        /// <summary>Un colpo è partito e non è ancora finito.</summary>
        bool IsSwinging { get; }

        /// <summary>Il bersaglio è entro la distanza di aggro e niente si mette in mezzo. Costa un raggio: si chiede di rado.</summary>
        bool CanSeeTarget();

        /// <summary>Chiede il colpo al bersaglio: se è lontano il nemico ci va, se è pronto colpisce.</summary>
        void Engage();

        /// <summary>Lascia il bersaglio: niente più avvicinamento né colpi.</summary>
        void Disengage();
    }
}
