namespace DarkDescent.Enemies
{
    /// <summary>
    /// Quello che in più chiede la fuga (D2–D5 della M24): se il nemico ha motivo di scappare, partire per un punto lontano
    /// dal bersaglio, sapere se è arrivato, e finire richiamando i compagni. Come <see cref="IRangedBody"/>, un nemico che
    /// non scappa non lo implementa, e gli stati lo chiedono solo se c'è.
    /// </summary>
    public interface IFleeBody : IEnemyBody
    {
        /// <summary>
        /// Ha motivo di scappare ora: vita sotto la soglia o compagni morti vicino, una volta sola per vita, e non appena
        /// dopo un tentativo senza posto dove andare.
        /// </summary>
        bool WantsToFlee { get; }

        /// <summary>
        /// Parte per un punto lontano dal bersaglio e non troppo lontano da casa, più veloce del solito, e non si cura. False se
        /// non c'è un posto che lo allontani (un angolo non è una fuga): resta a combattere, e non riprova subito.
        /// </summary>
        bool BeginFlee();

        /// <summary>È arrivato dove scappava, o non sta andando da nessuna parte.</summary>
        bool IsAtDestination { get; }

        /// <summary>Finita la fuga: la velocità di prima, e i compagni vicini accorrono.</summary>
        void EndFlee();
    }
}
