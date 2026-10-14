using System.Linq;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Localization;
using DarkDescent.Magic;
using DarkDescent.Town;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>La magia del portale e i portali (D6 della M10, passo 11.7 della M11).</summary>
    public class PortalSpellTests
    {
        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>("Assets/_Project/" + path);

        [Test, Description("La magia del portale: tipo suo, icona, costo, nome nelle due lingue; il cavaliere la ha, fuori dai posti F1–F3")]
        public void PortalSpell_IsWired()
        {
            var spell = Asset<SpellDefinition>("Data/Spells/TownPortal.asset");
            Assert.AreEqual(SpellKind.Portal, spell.Kind);
            Assert.IsNotNull(spell.Icon);
            Assert.IsFalse(string.IsNullOrEmpty(spell.Id));
            Assert.AreEqual(10f, spell.ManaCost);
            Assert.AreEqual(0f, spell.Cooldown);

            var localizer = new Localizer(StringTable.Parse(Asset<TextAsset>("Data/Localization/Strings.csv").text));
            Assert.AreEqual("Town Portal", localizer.Get(spell.NameKey));
            localizer.SetLanguage("it");
            Assert.AreEqual("Portale", localizer.Get(spell.NameKey));
            Assert.AreEqual("Il Re delle ossa non ti lascia fuggire", localizer.Get(TextKeys.PortalBossRoom));
            Assert.AreEqual("Sei già all'accampamento", localizer.Get(TextKeys.PortalInTown));

            var magic = Asset<GameObject>("Prefabs/Player.prefab").GetComponent<PlayerMagic>();
            Assert.AreSame(spell, magic.PortalSpell);
            Assert.AreEqual(-1, magic.Book.SlotOf(spell), "non è tra F1, F2 e F3");
        }

        [Test, Description("La pergamena del ritorno non c'è più: né oggetto, né nel database, né dal mercante, né tra le stringhe")]
        public void ReturnScroll_IsGone()
        {
            Assert.IsNull(Asset<Object>("Data/Items/ReturnScroll.asset"));
            foreach (var item in Asset<ItemDatabase>("Data/ItemDatabase.asset").Items)
            {
                Assert.AreNotEqual("item.scroll_return", item.NameKey);
            }

            foreach (var item in Asset<MerchantTable>("Data/Loot/MerchantTable.asset").Supplies)
            {
                Assert.AreNotEqual("item.scroll_return", item.NameKey);
            }

            StringAssert.DoesNotContain("scroll_return", Asset<TextAsset>("Data/Localization/Strings.csv").text);
        }

        [Test, Description("Il portale della città riporta all'ingresso della profondità: la cripta fino alla 4, poi le caverne")]
        public void Depths_MapToTheirScenes()
        {
            var crypt = Asset<DungeonSettings>("Data/Levels/CryptSettings.asset");
            var caves = Asset<DungeonSettings>("Data/Levels/CaveSettings.asset");
            Assert.IsTrue(crypt.Covers(1) && crypt.Covers(4) && !crypt.Covers(5) && !crypt.Covers(0));
            Assert.IsTrue(caves.Covers(5) && caves.Covers(8) && !caves.Covers(4));
            Assert.AreEqual("Start", DungeonLevel.EntranceAt(1));
            Assert.AreEqual("FromAbove", DungeonLevel.EntranceAt(3));
            Assert.AreEqual("FromAbove", DungeonLevel.EntranceAt(5));
        }

        [Test, Description("La città ha l'arrivo dei portali accanto all'ingresso: il simbolo P è un ingresso Portal")]
        public void Town_HasThePortalArrival()
        {
            var map = LevelMap.Parse(Asset<TextAsset>("Data/Levels/Town.txt").text);
            var arrival = map.Markers.Single(m => m.Symbol == 'P');
            var start = map.Markers.Single(m => m.Symbol == '<');
            Assert.LessOrEqual(Mathf.Abs(arrival.X - start.X) + Mathf.Abs(arrival.Y - start.Y), 3, "accanto all'ingresso");
            Assert.IsTrue(map.IsFloor(arrival.X, arrival.Y - 1), "il portale sta a nord, sul pavimento");

            var prefab = Asset<LevelTileset>("Data/Levels/TownTileset.asset").GetMarkerPrefab('P');
            Assert.AreEqual(TownPortals.PortalEntrance, prefab.GetComponent<LevelEntrance>().Id);
        }
    }
}
