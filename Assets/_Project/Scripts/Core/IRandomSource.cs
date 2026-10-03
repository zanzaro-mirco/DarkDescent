namespace DarkDescent.Core
{
    /// <summary>
    /// Da dove vengono i numeri casuali del gioco (D3 della M4). La crea il
    /// CompositionRoot e la passa a chi tira: in build parte da un seme qualsiasi, nei test è
    /// fissa. <c>UnityEngine.Random</c> no: è globale, e lo usano anche i suoni.
    /// </summary>
    public interface IRandomSource
    {
        /// <summary>Un numero in [0, 1).</summary>
        double NextDouble();
    }
}
