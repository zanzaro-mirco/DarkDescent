using System.Collections.Generic;
using System.IO;
using System.Linq;
using DarkDescent.Editor;
using DarkDescent.Talents;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Le icone dei talenti del Guerriero (D12 della M20): ogni abilità attiva e ogni opzione dei passivi ha la
    /// sua, sono quadrate e sono tutte e solo quelle usate; la tabella dello strumento copre tutti i nodi.
    /// </summary>
    public class TalentIconTests
    {
        private static readonly string[] Names = { "Warrior", "WarriorDefense", "WarriorArms" };

        private static IEnumerable<TalentTree> Trees => Names.Select(n => AssetDatabase.LoadAssetAtPath<TalentTree>($"Assets/_Project/Data/Talents/{n}.asset"));

        private static IEnumerable<TalentOption> Options => Trees.SelectMany(t => t.Nodes).SelectMany(n => n.Options);

        private static string IdOf(TalentOption option) => option.NameKey.Replace("talent.", "");

        [Test, Description("La tabella dello strumento ha un'icona per ogni passivo e per ogni opzione dei nodi a scelta, e per niente altro; le undici abilità attive hanno la loro")]
        public void Table_CoversEveryTalent()
        {
            var passives = Options.Where(o => o.Ability == null).Select(IdOf).ToList();
            CollectionAssert.AreEquivalent(passives, TalentIconTools.Passives.Keys, "una voce per ogni passivo e opzione");
            var actives = Options.Where(o => o.Ability != null).Select(o => o.Ability.name).Distinct().ToList();
            CollectionAssert.AreEquivalent(actives.Where(a => a != "Attack"), TalentIconTools.Skills.Select(s => s.Spell), "una per ogni abilità attiva dell'albero");
            Assert.AreEqual(11, TalentIconTools.Skills.Length);
            Assert.AreEqual(33, TalentIconTools.Passives.Values.Distinct().Count(), "trentatré tipi di passivo");
        }

        [Test, Description("Ogni abilità attiva e ogni opzione dei passivi ha la sua icona, presa dalla cartella Talents")]
        public void EveryTalentHasItsIcon()
        {
            foreach (var option in Options)
            {
                var icon = option.Ability != null ? option.Ability.Icon : option.Icon;
                Assert.IsNotNull(icon, IdOf(option));
                if (option.Ability == null || option.Ability.name != "Attack")
                {
                    StringAssert.StartsWith(TalentIconTools.Folder, AssetDatabase.GetAssetPath(icon), IdOf(option));
                }
            }
        }

        [Test, Description("Le icone sono quadrate, grandi abbastanza per la casella della barra, e nella cartella ci sono tutte e solo quelle usate")]
        public void Icons_AreSquareAndAllUsed()
        {
            var used = TalentIconTools.Skills.Select(s => s.Icon).Concat(TalentIconTools.Passives.Values).Distinct().OrderBy(n => n).ToList();
            var files = Directory.GetFiles(TalentIconTools.Folder, "*.png").Select(Path.GetFileNameWithoutExtension).OrderBy(n => n).ToList();
            CollectionAssert.AreEqual(used, files, "tutte e solo quelle usate");
            foreach (string name in used)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{TalentIconTools.Folder}/{name}.png");
                Assert.IsNotNull(sprite, name);
                Assert.AreEqual(sprite.rect.width, sprite.rect.height, name);
                Assert.GreaterOrEqual(sprite.rect.width, 128f, name);
            }
        }
    }
}
