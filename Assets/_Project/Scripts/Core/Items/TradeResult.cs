namespace DarkDescent.Items
{
    /// <summary>Com'è andato un acquisto o una vendita a un banco del borgo.</summary>
    public enum TradeResult
    {
        Done,
        Nothing,
        NotEnoughGold,
        NoRoom,

        /// <summary>Il banco non tratta quel tipo di oggetto (D18 della M18).</summary>
        NotWanted,
    }
}
