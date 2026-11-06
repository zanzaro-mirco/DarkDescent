using System.IO;
using System.Text;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Quanti livelli della cripta hanno un fondo cieco adatto alla stanza sigillata (25.4 della M25, D5): la misura che dice se «un livello su tre» è
    /// possibile. Scrive il risultato in Temp/secretroom_measure.txt.
    /// </summary>
    public class SealedRoomMeasureTests
    {
        private const int Seeds = 200;

        [Test, Description("Misura: la frazione di livelli a profondità 2, 3 e 4 con un fondo cieco adatto, con e senza il requisito della cassa")]
        public void MeasureHowOftenALevelHasADeadEnd()
        {
            var settings = ScriptableObject.CreateInstance<DungeonSettings>();
            try
            {
                var report = new StringBuilder();
                for (int depth = 2; depth <= 4; depth++)
                {
                    int any = 0, withChest = 0, regionSum = 0, distanceSum = 0, chestSum = 0, enemySum = 0;
                    for (ulong seed = 1; seed <= Seeds; seed++)
                    {
                        var map = new DungeonGenerator(settings).Generate(seed, depth).ToMap(null);
                        var plain = SealedRoomPlanner.Find(map, seed, depth, requireChest: false);
                        var treasure = SealedRoomPlanner.Find(map, seed, depth, requireChest: true);
                        any += plain != null ? 1 : 0;
                        if (treasure != null)
                        {
                            withChest++;
                            regionSum += treasure.Region.Count;
                            distanceSum += treasure.LeverDistance;
                            chestSum += treasure.Chests;
                            enemySum += treasure.Enemies;
                        }
                    }

                    Assert.GreaterOrEqual(withChest, Seeds / 2, $"profondità {depth}: meno di un livello su due ha un fondo cieco con una cassa: «un livello su tre» (D5) non è possibile");
                    report.AppendLine($"profondità {depth}: fondo cieco {any}/{Seeds}, con una cassa {withChest}/{Seeds}"
                        + (withChest > 0 ? $", celle {regionSum / (double)withChest:0.0}, casse {chestSum / (double)withChest:0.0}, nemici {enemySum / (double)withChest:0.0}, leva a {distanceSum / (double)withChest:0.0} passi" : string.Empty));
                }

                Directory.CreateDirectory("Temp");
                File.WriteAllText("Temp/secretroom_measure.txt", report.ToString());
                Assert.Pass(report.ToString());
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }
    }
}
