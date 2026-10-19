using System.Collections;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.Levels;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>Campioni e unici nella barra in alto e sull'automappa (12.5 della M12).</summary>
    public class SpecialHudTests : SandboxFixture
    {
        private static LevelManager Manager => Object.FindFirstObjectByType<LevelManager>();

        private static EnemyBar Bar => Object.FindFirstObjectByType<EnemyBar>();

        private static AutomapView Automap => Object.FindFirstObjectByType<AutomapView>();

        private static DungeonSettings Crypt => AssetDatabase.LoadAssetAtPath<DungeonSettings>("Assets/_Project/Data/Levels/CryptSettings.asset");

        private static float PanelHeight => ((RectTransform)Bar.transform.Find("Panel")).sizeDelta.y;

        [TearDown]
        public void RestoreLanguage()
        {
            Localizer?.SetLanguage("en");
        }

        private IEnumerator EnterCryptWithMorvak()
        {
            yield return LoadGeneratedCore();
            ulong seed = 1;
            while (Crypt.UniqueDepth(seed) != 3)
            {
                seed++;
            }

            Manager.RunSeed = seed;
            Manager.LoadLevel("Level_Crypt", "FromAbove", 3);
            yield return null;
            for (float t = 0f; Manager.IsTransitioning || Manager.CurrentLevel == null || Manager.CurrentLevel.Depth != 3; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 15f, "la profondità 3 non arriva");
                yield return null;
            }

            yield return null;
            yield return null;

            // fermi: il test parla di quello che si vede, non dello scontro
            foreach (var enemy in Manager.CurrentLevel.Enemies)
            {
                enemy.enabled = false;
            }
        }

        [UnityTest, Description("Nella barra in alto un campione ha il nome blu e sotto i suoi modificatori, Morvak il nome d'oro e i suoi due; un nemico normale resta com'era, senza la riga; i modificatori seguono la lingua")]
        public IEnumerator Bar_ShowsRankColorAndModifiers()
        {
            yield return EnterCryptWithMorvak();
            var enemies = Manager.CurrentLevel.Enemies;
            var normal = enemies.First(e => e.GetComponent<SpecialEnemy>() == null);
            var champion = enemies.First(e => e.GetComponent<SpecialEnemy>()?.Rank == EnemyRank.Champion).GetComponent<SpecialEnemy>();
            var morvak = enemies.First(e => e.GetComponent<SpecialEnemy>()?.Rank == EnemyRank.Unique).GetComponent<SpecialEnemy>();

            Bar.Pin(normal.GetComponent<Health>());
            Color baseColor = Bar.NameColor;
            float baseHeight = PanelHeight;
            Assert.AreEqual(string.Empty, Bar.ModifiersText);

            Bar.Pin(champion.GetComponent<Health>());
            Assert.AreEqual(champion.Numbers.Color, Bar.NameColor, "il blu dei campioni");
            string expected = string.Join(" · ", champion.ActiveModifiers.Select(m => Localizer.Get(m.NameKey)));
            Assert.AreEqual(expected, Bar.ModifiersText);
            Assert.Greater(PanelHeight, baseHeight, "il pannello si allunga per la riga");

            Bar.Pin(morvak.GetComponent<Health>());
            Assert.AreEqual("Morvak the Jailer", Bar.NameText);
            Assert.AreEqual(morvak.Numbers.Color, Bar.NameColor, "l'oro degli unici");
            Assert.AreEqual("Frozen · Unstoppable", Bar.ModifiersText);
            Localizer.SetLanguage("it");
            Assert.AreEqual("Morvak il Carceriere", Bar.NameText);
            Assert.AreEqual("Gelido · Inamovibile", Bar.ModifiersText);

            Bar.Pin(normal.GetComponent<Health>());
            Assert.AreEqual(baseColor, Bar.NameColor, "tornato a un nemico normale");
            Assert.AreEqual(string.Empty, Bar.ModifiersText);
            Assert.AreEqual(baseHeight, PanelHeight);
            Bar.Unpin();
        }

        [UnityTest, Description("Sull'automappa Morvak compare solo quando la sua cella è scoperta, con un punto d'oro; morto, il punto si spegne")]
        public IEnumerator Automap_ShowsSpecialsOnceSeen()
        {
            yield return EnterCryptWithMorvak();
            var morvak = Manager.CurrentLevel.Enemies.First(e => e.GetComponent<SpecialEnemy>()?.Rank == EnemyRank.Unique);
            var frame = Automap.Corner;
            Assert.IsTrue(frame.IsShowing);
            int before = frame.VisibleMarkers;
            var cell = DarkDescent.Levels.Exploration.CellAt(morvak.transform.position);
            Assert.IsFalse(Manager.CurrentLevel.Map != null && IsExplored(cell), "Morvak sta lontano dall'ingresso");

            Assert.IsTrue(NavMesh.SamplePosition(morvak.transform.position + Vector3.back * 4f, out var hit, 3f, NavMesh.AllAreas));
            PlayerAgent.Warp(hit.position);
            for (float t = 0f; !IsExplored(cell); t += Time.deltaTime)
            {
                Assert.Less(t, 3f, "la cella di Morvak non si scopre");
                yield return null;
            }

            yield return null;
            Assert.Greater(frame.VisibleMarkers, before, "visto, compare il suo punto");
            var color = morvak.GetComponent<SpecialEnemy>().Numbers.Color;
            Assert.IsTrue(Enumerable.Range(0, frame.VisibleMarkers).Any(i => frame.Marker(i).color == color), "un punto d'oro");

            int shown = frame.VisibleMarkers;
            morvak.GetComponent<Health>().TakeDamage(new DamageInfo(100000f, DamageType.Physical, null));
            yield return null;
            Assert.AreEqual(shown - 1, frame.VisibleMarkers, "morto, il punto si spegne");
        }

        private static bool IsExplored(Vector2Int cell)
        {
            var tracker = Object.FindFirstObjectByType<ExplorationTracker>();
            return tracker.Exploration.IsExplored(cell.x, cell.y);
        }
    }
}
