using System.Collections.Generic;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Magic;
using NUnit.Framework;
using UnityEditor;

namespace DarkDescent.Tests
{
    public class ManaTests
    {
        private static SpellDefinition Spell(string name) => AssetDatabase.LoadAssetAtPath<SpellDefinition>($"Assets/_Project/Data/Spells/{name}.asset");

        private static List<SpellDefinition> AllSpells() => new List<SpellDefinition> { Spell("Firebolt"), Spell("FireRing"), Spell("StoneSkin") };

        [Test, Description("Mana massimo (D2 della M9): 10 + 2 × Magia, +1 per livello oltre il primo, più gli affissi; con la Magia 10 di partenza, 30")]
        public void MaxMana_FollowsMagicAndLevel()
        {
            Assert.AreEqual(30f, CombatFormulas.MaxMana(10f, 1f));
            Assert.AreEqual(34f, CombatFormulas.MaxMana(10f, 5f), "+1 a livello");
            Assert.AreEqual(50f, CombatFormulas.MaxMana(20f, 1f), "+2 per punto di Magia");
            Assert.AreEqual(38f, CombatFormulas.MaxMana(10f, 1f, 8f), "il mana degli affissi");
            Assert.AreEqual(30f, CombatFormulas.MaxMana(10f, 0f), "il livello 0 dei nemici non toglie niente");
        }

        [Test, Description("Spendere: solo se basta; restituire: fino al massimo; rigenerare: il 2% del massimo al secondo, senza superarlo")]
        public void Pool_SpendsRestoresRegenerates()
        {
            var pool = new ManaPool(30f);
            Assert.IsTrue(pool.TrySpend(6f));
            Assert.AreEqual(24f, pool.Current);
            Assert.IsFalse(pool.TrySpend(25f), "non basta");
            Assert.AreEqual(24f, pool.Current, "e non spende niente");

            pool.Regenerate(1f, CombatFormulas.ManaRegenPerSecond);
            Assert.AreEqual(24.6f, pool.Current, 0.0001f, "il 2% di 30 in un secondo");
            Assert.AreEqual(5.4f, pool.Restore(10f), 0.0001f, "la pozione rende solo quello che manca");
            Assert.AreEqual(30f, pool.Current);
            pool.Regenerate(10f, CombatFormulas.ManaRegenPerSecond);
            Assert.AreEqual(30f, pool.Current, "pieno resta pieno");
        }

        [Test, Description("Il massimo che sale non riempie, quello che scende taglia; Fill riempie")]
        public void Pool_MaxChangesKeepCurrent()
        {
            var pool = new ManaPool(30f);
            pool.TrySpend(10f);
            pool.SetMax(40f);
            Assert.AreEqual(20f, pool.Current);
            pool.SetMax(15f);
            Assert.AreEqual(15f, pool.Current);
            pool.Fill();
            Assert.AreEqual(15f, pool.Max);
            Assert.AreEqual(15f, pool.Current);
        }

        [Test, Description("La rigenerazione a piccoli passi annuncia solo quando cambia il valore intero (trappola 7): sessanta frame da un secondo fanno pochi eventi")]
        public void Pool_AnnouncesOnlyWholeChanges()
        {
            var pool = new ManaPool(30f);
            pool.TrySpend(10f);
            int changes = 0;
            pool.Changed += (current, max) => changes++;
            for (int i = 0; i < 60; i++)
            {
                pool.Regenerate(1f / 60f, CombatFormulas.ManaRegenPerSecond);
            }

            Assert.AreEqual(20.6f, pool.Current, 0.001f);
            Assert.AreEqual(0, changes, "da 20 a 20,6: il valore intero non è cambiato");
            for (int i = 0; i < 60; i++)
            {
                pool.Regenerate(1f / 60f, CombatFormulas.ManaRegenPerSecond);
            }

            Assert.AreEqual(1, changes, "passando 21, un evento solo");
        }

        [Test, Description("I tre incantesimi della scheda (D3): costo, ricarica, danno con la Magia")]
        public void Spells_HaveTheNumbersOfTheCard()
        {
            var firebolt = Spell("Firebolt");
            var ring = Spell("FireRing");
            var skin = Spell("StoneSkin");
            Assert.AreEqual(SpellKind.Projectile, firebolt.Kind);
            Assert.AreEqual(6f, firebolt.ManaCost);
            Assert.AreEqual(0.8f, firebolt.Cooldown, "non a raffica: la prova della build lo trovava una mitragliatrice");
            Assert.AreEqual((9, 15), firebolt.DamageRange(10f), "6–12 + 10 / 4, arrotondato");
            Assert.AreEqual(SpellKind.Area, ring.Kind);
            Assert.AreEqual(15f, ring.ManaCost);
            Assert.AreEqual(3f, ring.Cooldown);
            Assert.AreEqual(4f, ring.Radius);
            Assert.AreEqual((11, 17), ring.DamageRange(10f), "8–14 + 10 / 3");
            Assert.AreEqual(SpellKind.Buff, skin.Kind);
            Assert.AreEqual(50, skin.ArmorPercent);
            Assert.AreEqual(20f, skin.Duration);
            Assert.AreEqual(30f, skin.Cooldown);

            Assert.AreEqual(6 + 3, firebolt.RollDamage(12f, new FixedRandomSource(0.0)), "il tiro più basso con la Magia 12");
            Assert.AreEqual(12 + 3, firebolt.RollDamage(12f, new FixedRandomSource(0.9999)), "il più alto");
            Assert.AreNotEqual(firebolt.Id, ring.Id);
            Assert.IsFalse(string.IsNullOrEmpty(firebolt.Id));
        }

        [Test, Description("La barra (D9 della M20): otto posti vuoti; un'abilità imparata va nel primo libero e la prima è scelta; una seconda volta no; si sceglie solo un posto pieno; piena, non entra più niente")]
        public void Book_LearnsIntoTheFirstFreeSlot()
        {
            var book = new SpellBook();
            Assert.AreEqual((8, -1), (book.Count, book.Selected));
            Assert.IsNull(book.SelectedSpell);

            int learned = -1;
            book.Learned += slot => learned = slot;
            Assert.IsTrue(book.Learn(Spell("Firebolt")));
            Assert.AreEqual((0, 0), (learned, book.Selected));
            Assert.IsTrue(book.Learn(Spell("FireRing")));
            Assert.AreEqual((1, 0), (learned, book.Selected), "la scelta resta");
            Assert.IsFalse(book.Learn(Spell("FireRing")), "già nella barra");
            Assert.IsFalse(book.TrySelect(2), "un posto vuoto");
            Assert.IsTrue(book.TrySelect(1));
            Assert.AreEqual(Spell("FireRing"), book.SelectedSpell);
            CollectionAssert.AreEqual(new[] { Spell("Firebolt").Id, Spell("FireRing").Id }, book.KnownIds());

            var others = AssetDatabase.FindAssets("t:SpellDefinition", new[] { "Assets/_Project/Data/Spells" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<SpellDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(spell => !book.Knows(spell)).Take(7).ToList();
            Assert.AreEqual(7, others.Count);
            for (int i = 0; i < 6; i++)
            {
                Assert.IsTrue(book.Learn(others[i]));
            }

            Assert.IsFalse(book.Learn(others[6]), "otto posti pieni");
        }

        [Test, Description("Spostare: l'abilità va sull'altro posto e si scambia con quella che c'era, la scelta la segue; togliere: se era la scelta, si sceglie la prima che resta")]
        public void Book_MovesAndForgets()
        {
            // F1 il Dardo, F2 l'Anello, F3 la Pelle
            var book = new SpellBook(AllSpells());
            int rearranged = 0;
            book.Rearranged += () => rearranged++;
            Assert.IsTrue(book.Move(0, 5));
            Assert.AreEqual((5, Spell("Firebolt")), (book.Selected, book.SelectedSpell));
            Assert.IsNull(book[0]);
            Assert.IsTrue(book.Move(1, 5), "lo scambio");
            Assert.AreEqual((Spell("Firebolt"), Spell("FireRing")), (book[1], book[5]));
            Assert.AreEqual(1, book.Selected, "la scelta segue il Dardo");
            Assert.IsFalse(book.Move(0, 3), "da un posto vuoto");
            Assert.AreEqual(2, rearranged);

            Assert.IsTrue(book.Forget(Spell("Firebolt")));
            Assert.IsFalse(book.Knows(Spell("Firebolt")));
            Assert.AreEqual(2, book.Selected, "tolto lo scelto, il primo che resta: la Pelle in F3");
            Assert.IsFalse(book.Forget(Spell("Firebolt")), "non c'è più");
            Assert.IsTrue(book.Learn(Spell("Firebolt")));
            Assert.AreEqual(0, book.SlotOf(Spell("Firebolt")), "torna nel primo posto libero");
        }

        [Test, Description("Dal salvataggio: la barra posto per posto, vuoti compresi; rimessa su un libro che ha le stesse abilità in un altro ordine, ognuna torna al suo posto, gli id che non ci sono si ignorano; un posto scelto vuoto passa al primo pieno")]
        public void Book_ArrangesFromSave()
        {
            var book = new SpellBook(AllSpells());
            book.Move(0, 4);
            var saved = book.Export();
            Assert.AreEqual(8, saved.Count);
            CollectionAssert.AreEqual(new[] { "", Spell("FireRing").Id, Spell("StoneSkin").Id, "", Spell("Firebolt").Id, "", "", "" }, saved);

            // come la mette l'albero: senza l'Anello, in un altro ordine
            var again = new SpellBook(new[] { Spell("StoneSkin"), Spell("Firebolt") });
            saved[7] = "sconosciuto";
            again.Arrange(saved, 4);
            Assert.AreEqual((Spell("StoneSkin"), Spell("Firebolt")), (again[2], again[4]));
            Assert.AreEqual(2, again.KnownIds().Count);
            Assert.AreEqual(4, again.Selected);

            again.Arrange(new string[0], 7);
            Assert.AreEqual(2, again.Selected, "F8 è vuoto: il primo pieno");
        }

        [Test, Description("Il lancio: posto vuoto, senza mana o in ricarica non parte e non spende; pronto spende il costo e avvia la ricarica, che il tempo consuma; la ricarica è dell'abilità e la segue quando si sposta")]
        public void Caster_ChecksKnownManaCooldown()
        {
            var book = new SpellBook(new[] { Spell("Firebolt") });
            var caster = new SpellCaster(book, new ManaPool(30f));
            Assert.AreEqual(CastResult.NotKnown, caster.TryCast(1));

            book.Learn(Spell("FireRing"));
            Assert.AreEqual(CastResult.Ready, caster.TryCast(1));
            Assert.AreEqual(15f, caster.Mana.Current);
            Assert.AreEqual(1f, caster.CooldownFraction(1));
            Assert.AreEqual(CastResult.Cooldown, caster.TryCast(1), "in ricarica");
            Assert.AreEqual(15f, caster.Mana.Current, "senza spendere");

            book.Move(1, 6);
            Assert.AreEqual((0f, 1f), (caster.CooldownFraction(1), caster.CooldownFraction(6)), "la ricarica va con l'Anello");
            Assert.AreEqual(CastResult.Cooldown, caster.Check(6));

            caster.Advance(1.5f);
            Assert.AreEqual(0.5f, caster.CooldownFraction(6), 0.0001f);
            caster.Advance(1.5f);
            Assert.AreEqual(CastResult.Ready, caster.Check(6));

            Assert.AreEqual(CastResult.Ready, caster.TryCast(0));
            Assert.AreEqual(CastResult.Cooldown, caster.TryCast(0), "anche il Dardo ha una ricarica breve");
            caster.Advance(0.8f);
            Assert.AreEqual(CastResult.Ready, caster.TryCast(0));
            caster.Advance(0.8f);
            Assert.AreEqual(3f, caster.Mana.Current);
            Assert.AreEqual(CastResult.NoMana, caster.TryCast(0), "3 di mana non bastano per 6");
        }
    }
}
