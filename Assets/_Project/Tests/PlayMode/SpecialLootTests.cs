using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Town;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>Il loot dei nemici speciali, i forzieri e il tesoro del re (12.4 della M12).</summary>
    public class SpecialLootTests : SandboxFixture
    {
        private static LevelManager Manager => Object.FindFirstObjectByType<LevelManager>();

        private static DungeonSettings Crypt => AssetDatabase.LoadAssetAtPath<DungeonSettings>("Assets/_Project/Data/Levels/CryptSettings.asset");

        private static DamageInfo Damage(float amount) => new DamageInfo(amount, DamageType.Physical, null);

        private static int GroundItems => Object.FindObjectsByType<GroundItem>(FindObjectsSortMode.None).Length;

        private static IEnumerator Enter(string scene, int depth)
        {
            Manager.LoadLevel(scene, "FromAbove", depth);
            yield return null;
            for (float t = 0f; Manager.IsTransitioning || Manager.CurrentLevel == null || Manager.CurrentLevel.Depth != depth; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 15f, $"la profondità {depth} non arriva");
                yield return null;
            }

            yield return null;
            yield return null;
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
            yield return Enter("Level_Crypt", 3);
        }

        // quanti oggetti a terra lascia, contati prima: oggetto, pozione, pergamena, oro e quelli in più
        private static int Expected(LootDrop drop, List<ItemInstance> extra)
        {
            drop.PreviewExtra(extra);
            return (drop.Preview() != null ? 1 : 0) + (drop.PreviewPotion() != null ? 1 : 0) + (drop.Carried != null ? 1 : 0)
                + (drop.PreviewGold() > 0 ? 1 : 0) + extra.Count;
        }

        private static int Expected(Chest chest, List<ItemInstance> extra)
        {
            chest.PreviewExtra(extra);
            return (chest.Preview() != null ? 1 : 0) + (chest.PreviewPotion() != null ? 1 : 0) + (chest.Carried != null ? 1 : 0)
                + (chest.PreviewGold() > 0 ? 1 : 0) + extra.Count;
        }

        [UnityTest, Description("Un campione lascia un oggetto in più e più oro, Morvak due in più con almeno un raro; i nemici normali niente in più; morendo cadono tutti")]
        public IEnumerator Specials_DropTheirExtraLoot()
        {
            yield return EnterCryptWithMorvak();
            var level = Manager.CurrentLevel;
            var extra = new List<ItemInstance>();
            foreach (var enemy in level.Enemies)
            {
                var drop = enemy.GetComponent<LootDrop>();
                var special = enemy.GetComponent<SpecialEnemy>();
                if (special == null)
                {
                    Assert.IsNull(drop.Bonus, enemy.name);
                    drop.PreviewExtra(extra);
                    Assert.IsEmpty(extra, enemy.name);
                    continue;
                }

                Assert.AreSame(special.Numbers.Loot, drop.Bonus, enemy.name);
                drop.PreviewExtra(extra);
                if (special.Rank == EnemyRank.Unique)
                {
                    Assert.AreEqual(2, extra.Count, enemy.name);
                }
                else
                {
                    // l'oggetto del campione cade una volta su due (D13 della M13)
                    Assert.LessOrEqual(extra.Count, 1, enemy.name);
                }
            }

            var morvak = level.Enemies.Single(e => e.GetComponent<SpecialEnemy>()?.Rank == EnemyRank.Unique);
            var loot = morvak.GetComponent<LootDrop>();
            loot.PreviewExtra(extra);
            Assert.IsTrue(extra.Append(loot.Preview()).Any(i => i != null && i.Rarity == Rarity.Rare), "Morvak lascia un raro");

            int expected = Expected(loot, extra);
            int before = GroundItems;
            morvak.GetComponent<Health>().TakeDamage(Damage(100000f));
            yield return null;
            Assert.AreEqual(before + expected, GroundItems, "cade tutto, anche gli oggetti in più");
        }

        [UnityTest, Description("Accanto a Morvak c'è un forziere: si apre come una cassa e lascia un oggetto sicuro, almeno magico, e più oro di una cassa")]
        public IEnumerator GoldChest_BesideMorvakOpens()
        {
            yield return EnterCryptWithMorvak();
            var level = Manager.CurrentLevel;
            var morvak = level.Enemies.Single(e => e.GetComponent<SpecialEnemy>()?.Rank == EnemyRank.Unique);
            var chest = level.Chests.Where(c => c.Bonus.ExtraRolls == 1).OrderBy(c => FlatDistance(c.transform.position, morvak.transform.position)).First();
            Assert.LessOrEqual(FlatDistance(chest.transform.position, morvak.transform.position), LevelMap.CellSize * 6f, "vicino all'unico");
            Assert.AreEqual("Gold chest", chest.GetComponent<Interactable>().GetLabel(Localizer));
            Assert.IsFalse(chest.IsSealed);

            var extra = new List<ItemInstance>();
            chest.PreviewExtra(extra);
            Assert.AreEqual(1, extra.Count);
            Assert.IsNull(chest.Preview(), "solo l'oggetto sicuro");
            Assert.GreaterOrEqual(extra[0].Rarity, Rarity.Magic, "almeno magico");
            Assert.Greater(chest.PreviewGold(), 0);

            int expected = Expected(chest, extra);
            int before = GroundItems;
            chest.GetComponent<Interactable>().Use(Player.gameObject);
            yield return null;
            Assert.IsTrue(chest.IsOpen);
            Assert.AreEqual(before + expected, GroundItems);
        }

        [UnityTest, Description("Il tesoro del re sta in fondo alla sala, sigillato: finché il re è vivo non si apre; morto il re si apre e lascia tre oggetti con un raro; il portale della vittoria è dall'altra parte, verso la porta")]
        public IEnumerator KingsTreasure_OpensWhenTheKingDies()
        {
            yield return LoadGeneratedCore();
            for (ulong seed = 1; seed <= 10; seed++)
            {
                Manager.RunSeed = seed;
                yield return Enter("Level_Caves", 8);
                if (Manager.CurrentLevel.BossGate != null)
                {
                    break;
                }
            }

            var level = Manager.CurrentLevel;
            Assert.IsNotNull(level.BossGate, "un seme con la porta");

            // anche il loot dal seme del livello: con il seme a caso del CompositionRoot ogni tanto i tre
            // oggetti non avevano un raro (PlayMode del 27 ott 2026)
            Object.FindFirstObjectByType<DarkDescent.Core.CompositionRoot>().UseLootSeed(Manager.RunSeed);
            var treasure = level.Chests.Single(c => c.OpensWithBoss);
            Assert.AreEqual("The King's Hoard", treasure.GetComponent<Interactable>().GetLabel(Localizer));
            Assert.IsTrue(treasure.IsSealed);
            treasure.GetComponent<Interactable>().Use(Player.gameObject);
            Assert.IsFalse(treasure.IsOpen, "sigillato, con il re vivo");

            var boss = level.Boss;
            Vector3 home = boss.GetComponent<EnemyAI>().HomePosition;
            Assert.AreEqual(LevelMap.CellSize * 2f, FlatDistance(treasure.transform.position, home), 0.1f, "contro la parete in fondo");
            Assert.Greater(FlatDistance(treasure.transform.position, level.BossGate.transform.position), FlatDistance(home, level.BossGate.transform.position),
                "dall'altra parte della porta");

            boss.Health.TakeDamage(Damage(boss.Health.Max));
            yield return null;
            Assert.IsFalse(treasure.IsSealed, "morto il re si apre");
            var portal = Object.FindFirstObjectByType<TownPortals>().VictoryPortal;
            Assert.Greater(FlatDistance(portal.transform.position, treasure.transform.position), LevelMap.CellSize * 2.5f, "il portale non sta davanti al tesoro");

            var extra = new List<ItemInstance>();
            treasure.PreviewExtra(extra);
            Assert.AreEqual(2, extra.Count);
            Assert.IsTrue(extra.Append(treasure.Preview()).Any(i => i.Rarity == Rarity.Rare), "almeno un raro");
            int expected = Expected(treasure, extra);
            int before = GroundItems;
            treasure.GetComponent<Interactable>().Use(Player.gameObject);
            yield return null;
            Assert.IsTrue(treasure.IsOpen);
            Assert.AreEqual(before + expected, GroundItems);
        }
    }
}
