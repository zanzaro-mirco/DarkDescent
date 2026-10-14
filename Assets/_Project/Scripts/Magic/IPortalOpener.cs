namespace DarkDescent.Magic
{
    /// <summary>
    /// Chi apre il portale per l'accampamento (D6 della M10; dal passo 11.7 della M11 con la magia del
    /// portale): la magia sa solo quanto costa, i livelli li conosce chi apre il portale.
    /// </summary>
    public interface IPortalOpener
    {
        /// <summary>Se qui il portale si può aprire: <see cref="PortalResult.Opened"/>, o il perché no.</summary>
        PortalResult CanOpenTownPortal();

        /// <summary>Apre il portale davanti al cavaliere; si chiama solo dopo un <see cref="CanOpenTownPortal"/> riuscito.</summary>
        void OpenTownPortal();
    }
}
