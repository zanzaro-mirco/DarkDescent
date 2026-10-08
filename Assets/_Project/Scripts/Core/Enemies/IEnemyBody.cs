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

        /// <summary>Il colpo in corso è quello forte, da caricare (D6 della M7); i colpi leggeri del bruto no.</summary>
        bool IsTelegraphedSwing { get; }

        /// <summary>Il bersaglio è entro la distanza di aggro e niente si mette in mezzo. Costa un raggio: si chiede di rado.</summary>
        bool CanSeeTarget();

        /// <summary>Chiede il colpo al bersaglio: se è lontano il nemico ci va, se è pronto colpisce.</summary>
        void Engage();

        /// <summary>Lascia il bersaglio: niente più avvicinamento né colpi.</summary>
        void Disengage();

        /// <summary>È più lontano da casa di quanto il suo archetipo gli permetta inseguendo.</summary>
        bool IsBeyondLeash { get; }

        /// <summary>È arrivato a casa, dove l'ha messo il livello.</summary>
        bool IsHome { get; }

        /// <summary>Si avvia verso casa.</summary>
        void GoHome();

        /// <summary>Arrivato: riprende la vita piena.</summary>
        void ArriveHome();
    }
}
