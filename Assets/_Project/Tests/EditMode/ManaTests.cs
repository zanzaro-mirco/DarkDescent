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

        [Test, Description("Il libro (D18 della M20): quattro caselle vuote; un'abilità che costa va nella prima casella libera dopo il sinistro, che vuole solo l'Attacco; una seconda volta no; piene le tre, resta nota ma fuori dalle caselle")]
        public void Book_LearnsIntoTheFirstFreeSlot()
        {
            var book = new SpellBook();
            Assert.AreEqual(4, book.Count);
            Assert.IsNull(book[0]);

            int learned = -2;
            book.Learned += slot => learned = slot;
            Assert.IsTrue(book.Learn(Spell("Firebolt")));
            Assert.AreEqual((1, Spell("Firebolt")), (learned, book[SpellBook.Secondary]), "il sinistro non è per chi costa mana");
            Assert.IsTrue(book.Learn(Spell("FireRing")));
            Assert.AreEqual(2, learned);
            Assert.IsFalse(book.Learn(Spell("FireRing")), "già nota");
            Assert.IsTrue(book.Learn(Spell("StoneSkin")));
            Assert.AreEqual(3, learned);
            CollectionAssert.AreEqual(new[] { Spell("Firebolt").Id, Spell("FireRing").Id, Spell("StoneSkin").Id }, book.KnownIds());

            var fourth = AssetDatabase.FindAssets("t:SpellDefinition", new[] { "Assets/_Project/Data/Spells" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<SpellDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                .First(spell => !book.Knows(spell) && !SpellBook.IsPrimary(spell));
            Assert.IsTrue(book.Learn(fourth));
            Assert.AreEqual(-1, learned, "le tre caselle sono piene: nota ma fuori");
            Assert.AreEqual(-1, book.SlotOf(fourth));
            Assert.IsTrue(book.Knows(fourth));
        }

        [Test, Description("Il sinistro vuole solo abilità senza mana e senza ricarica, le altre caselle solo quelle che costano: l'Attacco entra solo nel sinistro e il menu offre a ogni casella solo le sue")]
        public void Book_PrimaryTakesOnlyTheAttack()
        {
            var attack = Spell("Attack");
            Assert.IsTrue(SpellBook.IsPrimary(attack));
            Assert.IsFalse(SpellBook.IsPrimary(Spell("Firebolt")));
            Assert.IsFalse(SpellBook.IsPrimary(Spell("Whirlwind")), "il Turbine ha mana e ricarica");

            var book = new SpellBook(new[] { Spell("Firebolt"), attack, Spell("FireRing") });
            Assert.AreEqual((attack, Spell("Firebolt"), Spell("FireRing")), (book[0], book[1], book[2]), "l'Attacco prende il sinistro anche arrivato dopo");
            CollectionAssert.AreEqual(new[] { attack }, book.Choices(SpellBook.Primary));
            CollectionAssert.AreEqual(new[] { Spell("Firebolt"), Spell("FireRing") }, book.Choices(2));

            Assert.IsFalse(book.Assign(SpellBook.Primary, Spell("Firebolt")), "chi costa non va nel sinistro");
            Assert.IsFalse(book.Assign(2, attack), "l'Attacco non va negli altri");
            Assert.IsFalse(book.Assign(1, Spell("StoneSkin")), "non nota");
            Assert.IsFalse(book.Assign(4, Spell("Firebolt")), "la quinta casella non c'è");
        }

        [Test, Description("Assegnare: l'abilità va sull'altra casella e si scambia con quella che c'era; togliere: la casella resta vuota e l'abilità torna nel primo posto libero")]
        public void Book_AssignsAndForgets()
        {
            // destro il Dardo, Q l'Anello, E la Pelle
            var book = new SpellBook(AllSpells());
            int rearranged = 0;
            book.Rearranged += () => rearranged++;
            Assert.IsTrue(book.Assign(3, Spell("Firebolt")), "lo scambio con la Pelle");
            Assert.AreEqual((Spell("StoneSkin"), Spell("FireRing"), Spell("Firebolt")), (book[1], book[2], book[3]));
            Assert.IsTrue(book.Assign(3, Spell("Firebolt")), "già lì: niente da fare");
            Assert.AreEqual(1, rearranged);

            Assert.IsTrue(book.Forget(Spell("StoneSkin")));
            Assert.IsFalse(book.Knows(Spell("StoneSkin")));
            Assert.IsNull(book[1]);
            Assert.IsFalse(book.Forget(Spell("StoneSkin")), "non c'è più");
            Assert.IsTrue(book.Learn(Spell("StoneSkin")));
            Assert.AreEqual(1, book.SlotOf(Spell("StoneSkin")), "torna nel primo posto libero");
        }

        [Test, Description("Dal salvataggio: le quattro caselle posto per posto, vuote comprese; gli id che non ci sono si ignorano; il sinistro vuoto riprende l'Attacco; la barra a otto posti del 20.3 si rimette per ordine")]
        public void Book_ArrangesFromSave()
        {
            var attack = Spell("Attack");
            var book = new SpellBook(new[] { attack, Spell("Firebolt"), Spell("FireRing"), Spell("StoneSkin") });
            book.Assign(3, Spell("Firebolt"));
            var saved = book.Export();
            CollectionAssert.AreEqual(new[] { attack.Id, Spell("StoneSkin").Id, Spell("FireRing").Id, Spell("Firebolt").Id }, saved);

            // come la mette l'albero: senza l'Anello, in un altro ordine
            var again = new SpellBook(new[] { Spell("StoneSkin"), Spell("Firebolt"), attack });
            saved[2] = "sconosciuto";
            again.Arrange(saved);
            Assert.AreEqual((attack, Spell("StoneSkin"), (SpellDefinition)null, Spell("Firebolt")), (again[0], again[1], again[2], again[3]));

            again.Arrange(new[] { "", "", "", "" });
            Assert.AreEqual(attack, again[0], "il sinistro vuoto riprende l'Attacco");
            Assert.IsNull(again[1]);

            // il 20.3 aveva otto posti: contano l'ordine, non il posto
            var legacy = new SpellBook(new[] { attack, Spell("FireRing"), Spell("Firebolt"), Spell("StoneSkin") });
            legacy.Arrange(new[] { "", Spell("StoneSkin").Id, "", "", Spell("Firebolt").Id, "", Spell("FireRing").Id, "" });
            Assert.AreEqual((attack, Spell("StoneSkin"), Spell("Firebolt"), Spell("FireRing")), (legacy[0], legacy[1], legacy[2], legacy[3]));
        }

        [Test, Description("Il lancio: casella vuota, senza mana o in ricarica non parte e non spende; pronto spende il costo e avvia la ricarica, che il tempo consuma; la ricarica è dell'abilità e la segue quando si sposta")]
        public void Caster_ChecksKnownManaCooldown()
        {
            var book = new SpellBook(new[] { Spell("Firebolt") });
            var caster = new SpellCaster(book, new ManaPool(30f));
            Assert.AreEqual(CastResult.NotKnown, caster.TryCast(2));

            book.Learn(Spell("FireRing"));
            Assert.AreEqual(CastResult.Ready, caster.TryCast(2));
            Assert.AreEqual(15f, caster.Mana.Current);
            Assert.AreEqual(1f, caster.CooldownFraction(2));
            Assert.AreEqual(CastResult.Cooldown, caster.TryCast(2), "in ricarica");
            Assert.AreEqual(15f, caster.Mana.Current, "senza spendere");

            book.Assign(3, Spell("FireRing"));
            Assert.AreEqual((0f, 1f), (caster.CooldownFraction(2), caster.CooldownFraction(3)), "la ricarica va con l'Anello");
            Assert.AreEqual(CastResult.Cooldown, caster.Check(3));

            caster.Advance(1.5f);
            Assert.AreEqual(0.5f, caster.CooldownFraction(3), 0.0001f);
            caster.Advance(1.5f);
            Assert.AreEqual(CastResult.Ready, caster.Check(3));

            Assert.AreEqual(CastResult.Ready, caster.TryCast(1));
            Assert.AreEqual(CastResult.Cooldown, caster.TryCast(1), "anche il Dardo ha una ricarica breve");
            caster.Advance(0.8f);
            Assert.AreEqual(CastResult.Ready, caster.TryCast(1));
            caster.Advance(0.8f);
            Assert.AreEqual(3f, caster.Mana.Current);
            Assert.AreEqual(CastResult.NoMana, caster.TryCast(1), "3 di mana non bastano per 6");
        }
    }
}
