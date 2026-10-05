namespace DarkDescent.Levels
{
    /// <summary>
    /// Un algoritmo di livello: dal seme e dalla profondità, la griglia con ingresso e scala. Gioco,
    /// finestra dell'editor e test non sanno quale l'ha fatta (D2 della scheda M7).
    /// </summary>
    public interface ILevelGenerator
    {
        DungeonLayout Generate(ulong seed, int depth = 1);
    }
}
