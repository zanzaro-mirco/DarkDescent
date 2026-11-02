using System.Linq;
using DarkDescent.Editor;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DarkDescent.Tests
{
    /// <summary>
    /// La scena Core com'è dopo <c>HudTools.BuildConsole</c> (D18 della M20): console, provette, cintura,
    /// quattro caselle e menu delle abilità tutti collegati. I test PlayMode ci passano da dentro; questi
    /// girano in un secondo e dicono subito quale riferimento è saltato.
    /// </summary>
    public class HudSceneTests
    {
        private Scene _scene;
        private Transform _hud;

        [OneTimeSetUp]
        public void OpenCore()
        {
            _scene = EditorSceneManager.OpenScene(TalentTools.CoreScene, OpenSceneMode.Additive);
            _hud = _scene.GetRootGameObjects().Single(root => root.name == "HUD").transform;
        }

        [OneTimeTearDown]
        public void CloseCore()
        {
            EditorSceneManager.CloseScene(_scene, true);
        }

        private int Index(string name) => _hud.Find(name).GetSiblingIndex();

        private static SerializedProperty Property(Object target, string name) => new SerializedObject(target).FindProperty(name);

        [Test, Description("L'ordine nell'HUD: la console sotto le provette e le caselle, il menu delle abilità sotto la schermata della morte che copre tutto")]
        public void Hud_HasTheRightOrder()
        {
            foreach (string above in new[] { "HealthOrb", "ManaOrb", "Belt", "SpellBar", "PortalSlot", "ExperienceBar" })
            {
                Assert.Less(Index("Console"), Index(above), $"la console sotto {above}");
            }

            Assert.Less(Index("SkillsUI"), Index("DeathScreen"), "il menu sotto la morte");
            Assert.Less(Index("SkillsUI"), Index("ScreenFader"));
        }

        [Test, Description("La barra ha le quattro caselle in ordine (sinistro, destro, Q, E), ognuna con icona, nome, tasto, cornice accesa, velo e barra; il menu è collegato")]
        public void SpellBar_HasFourWiredSlots()
        {
            var bar = _hud.Find("SpellBar").GetComponent<SpellBarView>();
            var slots = Property(bar, "_slots");
            Assert.AreEqual(4, slots.arraySize);
            Assert.AreEqual(4, _hud.Find("SpellBar").childCount, "niente caselle in più");
            for (int i = 0; i < 4; i++)
            {
                var slot = (SpellSlotView)slots.GetArrayElementAtIndex(i).objectReferenceValue;
                Assert.IsNotNull(slot, $"casella {i}");
                Assert.AreEqual(i, Property(slot, "_slot").intValue);
                foreach (string field in new[] { "_icon", "_name", "_key", "_selectedFrame", "_cooldown", "_bar" })
                {
                    Assert.IsNotNull(Property(slot, field).objectReferenceValue, $"casella {i}, {field}");
                }

                Assert.IsFalse(((Image)Property(slot, "_selectedFrame").objectReferenceValue).enabled, "la cornice accesa a riposo è spenta");
            }

            Assert.IsNotNull(Property(bar, "_menu").objectReferenceValue);
        }

        [Test, Description("Il menu delle abilità: finestra spenta, titolo, quattro righe con titolo, scelte e scritta vuota, e la scelta modello spenta con le sue parti")]
        public void SkillMenu_IsWired()
        {
            var root = _hud.Find("SkillsUI");
            var menu = root.GetComponent<SkillMenu>();
            Assert.IsNotNull(menu);
            var window = (GameObject)Property(menu, "_window").objectReferenceValue;
            Assert.IsNotNull(window);
            Assert.IsFalse(window.activeSelf, "chiuso all'avvio");
            Assert.IsNotNull(Property(menu, "_title").objectReferenceValue);
            foreach (string array in new[] { "_rows", "_rowTitles", "_rowChoices", "_rowEmpty" })
            {
                var list = Property(menu, array);
                Assert.AreEqual(4, list.arraySize, array);
                for (int i = 0; i < 4; i++)
                {
                    Assert.IsNotNull(list.GetArrayElementAtIndex(i).objectReferenceValue, $"{array}[{i}]");
                }
            }

            var template = (SkillChoiceView)Property(menu, "_choiceTemplate").objectReferenceValue;
            Assert.IsNotNull(template);
            Assert.IsFalse(template.gameObject.activeSelf);
            foreach (string field in new[] { "_icon", "_inner", "_name", "_current" })
            {
                Assert.IsNotNull(Property(template, field).objectReferenceValue, field);
            }

            Assert.AreEqual(8, _hud.Find("Belt").childCount, "otto pozioni nella cintura");
        }

        [Test, Description("Le provette di vita e mana: vetro, liquido a riempimento verticale dal basso e cornice con il loro sprite, e due liquidi diversi")]
        public void Gauges_HaveTheirSprites()
        {
            foreach (string orb in new[] { "HealthOrb", "ManaOrb" })
            {
                var fill = _hud.Find(orb + "/Fill").GetComponent<Image>();
                Assert.AreEqual(Image.Type.Filled, fill.type, orb);
                Assert.AreEqual(Image.FillMethod.Vertical, fill.fillMethod, orb);
                Assert.AreEqual((int)Image.OriginVertical.Bottom, fill.fillOrigin, orb);
                Assert.IsNotNull(fill.sprite, orb);
                Assert.IsNotNull(_hud.Find(orb + "/Back").GetComponent<Image>().sprite, orb);
                Assert.IsNotNull(_hud.Find(orb + "/Ring").GetComponent<Image>().sprite, orb);
            }

            Assert.AreNotSame(_hud.Find("HealthOrb/Fill").GetComponent<Image>().sprite, _hud.Find("ManaOrb/Fill").GetComponent<Image>().sprite);
        }
    }
}
