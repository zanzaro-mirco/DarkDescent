using System;
using System.Collections;
using System.IO;
using DarkDescent.Core;
using DarkDescent.Enemies;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>Le ronde nei livelli veri (24.3 della M24, D7): quanti nemici girano, e che i loro punti stiano sul NavMesh.</summary>
    public class PatrolLevelTests : SandboxFixture
    {
        private string _folder;

        [SetUp]
        public void UseTemporarySaves()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentPatrol_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, _folder);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            EnemyBehaviours.PatrolEnabled = false;
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, null);
            yield return null;
            Directory.Delete(_folder, true);
        }

        private static LevelManager Manager => UnityEngine.Object.FindFirstObjectByType<LevelManager>();

        private static IEnumerator Travel(string scene, string entrance, int depth)
        {
            var before = Manager.CurrentLevel;
            Manager.LoadLevel(scene, entrance, depth);
            yield return null;
            for (float t = 0f; Manager.IsTransitioning || Manager.CurrentLevel == null || Manager.CurrentLevel == before; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 15f, scene + " non arriva");
                yield return null;
            }

            yield return null;
            yield return null;
        }

        [UnityTest, Description("Nei livelli veri, a ogni profondità, un nemico normale su quattro (scheletri e bruti) ha la ronda, con due o tre punti sul NavMesh entro 8 m da casa; sciame, maghi, campioni e re nessuno")]
        public IEnumerator InRealLevels_AboutAQuarterPatrol_WithPointsOnTheNavMesh()
        {
            yield return LoadGeneratedCore();
            EnemyBehaviours.PatrolEnabled = true;

            int candidates = 0;
            int patrollers = 0;
            int planned = 0;
            foreach (var spot in new[] { ("Level_Crypt", "Start", 1), ("Level_Crypt", "FromAbove", 2), ("Level_Crypt", "FromAbove", 3), ("Level_Crypt", "FromAbove", 4), ("Level_Caves", "FromAbove", 5), ("Level_Caves", "FromAbove", 7) })
            {
                Manager.RunSeed = 11;
                yield return Travel(spot.Item1, spot.Item2, spot.Item3);
                foreach (var enemy in Manager.CurrentLevel.Enemies)
                {
                    if (enemy == null)
                    {
                        continue;
                    }

                    var points = enemy.PatrolPoints;
                    bool special = enemy.TryGetComponent(out SpecialEnemy rank) && rank.Rank != EnemyRank.Normal;
                    if (!enemy.Archetype.CanPatrol || special || enemy.TryGetComponent(out BossController _))
                    {
                        Assert.AreEqual(0, points.Count, $"{enemy.name} ({enemy.Archetype.name}) non fa la ronda");
                        continue;
                    }

                    candidates++;
                    int cellX = Mathf.RoundToInt(enemy.HomePosition.x / LevelMap.CellSize);
                    int cellZ = Mathf.RoundToInt(-enemy.HomePosition.z / LevelMap.CellSize);
                    if (PatrolPlan.For(spot.Item3, cellX, cellZ, enemy.Archetype.PatrolShare, enemy.Archetype.PatrolRadius).Patrols)
                    {
                        planned++;
                    }

                    if (points.Count == 0)
                    {
                        continue;
                    }

                    patrollers++;
                    Assert.That(points.Count, Is.InRange(PatrolPlan.MinPoints, PatrolPlan.MaxPoints), enemy.name);
                    foreach (var point in points)
                    {
                        Assert.IsTrue(NavMesh.SamplePosition(point, out _, 0.2f, NavMesh.AllAreas), $"{enemy.name}: un punto fuori dal NavMesh");
                        Assert.LessOrEqual(Vector3.Distance(new Vector3(point.x, 0f, point.z), new Vector3(enemy.HomePosition.x, 0f, enemy.HomePosition.z)), 8.01f, $"{enemy.name}: un punto oltre gli 8 m");
                    }
                }
            }

            Assert.GreaterOrEqual(candidates, 30, "abbastanza nemici normali da contare");
            Debug.Log($"[DarkDescent] ronde: {patrollers} su {candidates} candidati ({planned} dall'hash)");
            Assert.That(planned / (double)candidates, Is.InRange(0.12, 0.40), "uno su quattro, più o meno");
            Assert.GreaterOrEqual(patrollers, planned * 0.75, "la gran parte delle ronde trova i suoi punti sul NavMesh");
        }
    }
}
