namespace DarkDescent.Items
{
    /// <summary>
    /// Chi apre il portale di una pergamena del ritorno (D6 della M10): l'inventario sa solo che la
    /// pergamena è stata letta, i livelli li conosce chi apre il portale.
    /// </summary>
    public interface IPortalOpener
    {
        /// <summary>Apre il portale davanti al cavaliere; false se qui non si può, e la pergamena resta.</summary>
        bool TryOpenPortal();
    }
}
