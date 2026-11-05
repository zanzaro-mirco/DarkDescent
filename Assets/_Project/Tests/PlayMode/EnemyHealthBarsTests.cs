using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Enemies;
using DarkDescent.Levels;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>Le barre sopra i nemici feriti (23.3 della M23): un'opzione spenta di default, solo sui feriti, al più 24.</summary>
    public class EnemyHealthBarsTests : SandboxFixture
    {
        private static EnemyHealthBars Bars => Object.FindFirstObjectByType<EnemyHealthBars>();

        private static LevelManager Manager => Object.FindFirstObjectByType<LevelManager>();

        private static DungeonSettings Crypt => AssetDatabase.LoadAssetAtPath<DungeonSettings>("Assets/_Project/Data/Levels/CryptSettings.asset");

        private readonly List<GameObject> _dummies = new List<GameObject>();

        [TearDown]
        public void Restore()
        {
            foreach (var dummy in _dummies)
            {
                if (dummy != null)
                {
                    Bars?.Untrack(dummy.GetComponent<Health>());
                    Object.Destroy(dummy);
                }
            }

            _dummies.Clear();
            PlayerPrefs.DeleteKey(OptionsPanel.EnemyBarsPreference);
            Localizer?.SetLanguage("en");
        }

        private Health Skeleton()
        {
            var enemy = Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None).First(e => e.gameObject.activeSelf);

            // fermo: il test parla delle barre, non dello scontro
            enemy.enabled = false;
            return enemy.GetComponent<Health>();
        }

        private void Hurt(Health enemy, float share)
        {
            enemy.TakeDamage(new DamageInfo(enemy.Max * share, DamageType.Physical, Player.gameObject));
        }

        // una Health senza corpo, seguita come un nemico del livello, a una distanza dal cavaliere
        private Health Dummy(string name, float away)
        {
            var go = new GameObject(name);
            go.transform.position = Player.position + Vector3.right * away;
            var health = go.AddComponent<Health>();
            Bars.Track(health);
            _dummies.Add(go);
            return health;
        }

        [UnityTest, Description("Spente di partenza: un nemico ferito non ha la barra e il canvas non si costruisce nemmeno")]
        public IEnumerator Bars_AreOffByDefault()
        {
            yield return LoadSandbox();
            var skeleton = Skeleton();
            Assert.IsFalse(Bars.IsOn, "spente di partenza");
            Assert.AreEqual(0, PlayerPrefs.GetInt(OptionsPanel.EnemyBarsPreference, 0));

            Hurt(skeleton, 0.4f);
            yield return null;
            yield return null;

            Assert.AreEqual(0, Bars.ShownCount);
            Assert.AreEqual(0, Bars.PoolSize, "senza l'opzione nessuna barra costruita");
        }

        [UnityTest, Description("Accese: lo scheletro intatto non ha la barra, ferito sì, con la parte piena giusta e sopra la testa; guarito del tutto la perde, morto pure")]
        public IEnumerator Bars_ShowOnlyOnWoundedEnemies()
        {
            yield return LoadSandbox();
            Bars.SetOn(true);
            var skeleton = Skeleton();
            yield return null;
            yield return null;
            Assert.AreEqual(0, Bars.ShownCount, "intatto: niente barra");

            Hurt(skeleton, 0.38f);
            yield return null;
            Assert.IsTrue(Bars.IsShowing(skeleton), "ferito: la barra");
            Assert.AreEqual(skeleton.Current / skeleton.Max, Bars.FillOf(skeleton), 0.001f);
            Assert.AreEqual(0.62f, Bars.FillOf(skeleton), 0.01f);
            Vector3 head = skeleton.transform.position + Vector3.up * 2.35f * skeleton.transform.lossyScale.y;
            Assert.Less(Vector3.Distance(head, Bars.PositionOf(skeleton)), 0.01f, "sopra la testa");

            Hurt(skeleton, 0.3f);
            Assert.AreEqual(0.32f, Bars.FillOf(skeleton), 0.01f, "si accorcia al colpo, senza aspettare un frame");

            skeleton.Heal(skeleton.Max);
            Assert.IsFalse(Bars.IsShowing(skeleton), "guarito del tutto la barra se ne va");
            Assert.AreEqual(0, Bars.WoundedCount);

            Hurt(skeleton, 0.5f);
            yield return null;
            Assert.IsTrue(Bars.IsShowing(skeleton));
            Hurt(skeleton, 2f);
            Assert.IsTrue(skeleton.IsDead);
            Assert.IsFalse(Bars.IsShowing(skeleton), "morto, la barra sparisce");
            Assert.AreEqual(0, Bars.WoundedCount);
            Assert.AreEqual(0, Bars.ShownCount);
        }

        [UnityTest, Description("Un nemico che torna in vita (Revive) è di nuovo intatto: niente barra")]
        public IEnumerator Bars_ARevivedEnemy_HasNoBar()
        {
            yield return LoadSandbox();
            Bars.SetOn(true);
            var skeleton = Skeleton();
            Hurt(skeleton, 2f);
            yield return null;
            skeleton.Revive();
            yield return null;
            Assert.IsFalse(Bars.IsShowing(skeleton));
            Assert.AreEqual(0, Bars.WoundedCount);
        }

        [UnityTest, Description("Trenta feriti intorno al cavaliere: restano ventiquattro barre, quelle dei più vicini, e uno oltre il raggio non ne ha")]
        public IEnumerator Bars_StayWithinTwentyFour_TheNearest()
        {
            yield return LoadSandbox();
            Bars.SetOn(true);
            var all = new List<Health>();
            for (int i = 0; i < 30; i++)
            {
                var dummy = Dummy("Dummy" + i, 2f + i * 0.5f);
                Hurt(dummy, 0.5f);
                all.Add(dummy);
            }

            var far = Dummy("Far", 60f);
            Hurt(far, 0.5f);
            yield return null;
            yield return null;

            Assert.AreEqual(24, Bars.ShownCount);
            Assert.AreEqual(24, Bars.PoolSize, "il pool non cresce oltre il tetto");
            for (int i = 0; i < 30; i++)
            {
                Assert.AreEqual(i < 24, Bars.IsShowing(all[i]), "il dummy " + i);
            }

            Assert.IsFalse(Bars.IsShowing(far), "oltre il raggio");

            // uno dei vicini muore: la sua barra passa al venticinquesimo, senza crescere
            Hurt(all[0], 3f);
            yield return null;
            Assert.AreEqual(24, Bars.ShownCount);
            Assert.AreEqual(24, Bars.PoolSize);
            Assert.IsFalse(Bars.IsShowing(all[0]));
            Assert.IsTrue(Bars.IsShowing(all[24]), "il venticinquesimo prende il posto");
        }

        [UnityTest, Description("Spegnere l'opzione toglie tutte le barre; riaccesa tornano sui feriti")]
        public IEnumerator Bars_TurnedOff_AreAllGone()
        {
            yield return LoadSandbox();
            Bars.SetOn(true);
            var a = Dummy("A", 3f);
            var b = Dummy("B", 4f);
            Hurt(a, 0.3f);
            Hurt(b, 0.6f);
            yield return null;
            Assert.AreEqual(2, Bars.ShownCount);

            Bars.SetOn(false);
            Assert.AreEqual(0, Bars.ShownCount);
            yield return null;
            Assert.AreEqual(0, Bars.ShownCount, "spente restano spente");

            Bars.SetOn(true);
            yield return null;
            Assert.IsTrue(Bars.IsShowing(a));
            Assert.IsTrue(Bars.IsShowing(b));
        }

        [UnityTest, Description("Un nemico lasciato (livello scaricato) perde la barra e non resta tra i feriti")]
        public IEnumerator Bars_AnUntrackedEnemy_LosesItsBar()
        {
            yield return LoadSandbox();
            Bars.SetOn(true);
            var a = Dummy("A", 3f);
            Hurt(a, 0.3f);
            yield return null;
            Assert.IsTrue(Bars.IsShowing(a));

            Bars.Untrack(a);
            Assert.IsFalse(Bars.IsShowing(a));
            Assert.AreEqual(0, Bars.WoundedCount);
            Hurt(a, 0.3f);
            yield return null;
            Assert.IsFalse(Bars.IsShowing(a), "non più seguito: i suoi colpi non la riaccendono");
        }

        [UnityTest, Description("La casella nelle opzioni accende e spegne le barre, ricorda la scelta tra le preferenze e dopo aver riaperto il gioco le trova accese; la spunta è la stessa delle altre")]
        public IEnumerator Option_TurnsTheBarsOnAndOff_AndIsRemembered()
        {
            yield return LoadCore();
            var options = Object.FindFirstObjectByType<OptionsPanel>();
            Assert.IsFalse(options.EnemyBars.isOn, "spenta di partenza");
            Assert.IsFalse(Bars.IsOn);
            var check = (UnityEngine.UI.Image)options.EnemyBars.graphic;
            Assert.AreEqual("check", check.sprite.texture.name, "la spunta d'oro come le altre");

            var enemy = Manager.CurrentLevel.Enemies
                .Where(e => e.gameObject.activeInHierarchy)
                .OrderBy(e => FlatDistance(e.transform.position, Player.position))
                .First();
            enemy.enabled = false;
            var health = enemy.GetComponent<Health>();
            Hurt(health, 0.4f);
            yield return null;
            Assert.IsFalse(Bars.IsShowing(health), "spenta, niente barra");

            options.Open();
            options.EnemyBars.isOn = true;
            Assert.IsTrue(Bars.IsOn);
            Assert.AreEqual(1, PlayerPrefs.GetInt(OptionsPanel.EnemyBarsPreference));
            yield return null;
            Assert.IsTrue(Bars.IsShowing(health), "accesa, il ferito ha la barra");
            options.Back.onClick.Invoke();

            // riaperto il gioco la scelta resta, come per i numeri delle sfere
            SceneManager.LoadScene("Core");
            SceneManager.LoadScene("Level_01", LoadSceneMode.Additive);
            yield return WaitForLevel();
            yield return null;
            options = Object.FindFirstObjectByType<OptionsPanel>();
            Assert.IsTrue(options.EnemyBars.isOn);
            Assert.IsTrue(Bars.IsOn, "le barre partono accese");

            options.Open();
            options.EnemyBars.isOn = false;
            Assert.IsFalse(Bars.IsOn);
            Assert.AreEqual(0, PlayerPrefs.GetInt(OptionsPanel.EnemyBarsPreference, 1));
            Assert.AreEqual(0, Bars.ShownCount);
            options.Back.onClick.Invoke();
        }

        [UnityTest, Description("Un campione ha la barra nel colore del suo grado, l'unico anche il nome, nella lingua attiva; un nemico normale la barra rossa e nessun nome")]
        public IEnumerator Bars_TakeTheColorOfTheRank_AndUniquesTheirName()
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
            foreach (var enemy in Manager.CurrentLevel.Enemies)
            {
                enemy.enabled = false;
            }

            var enemies = Manager.CurrentLevel.Enemies;
            var normal = enemies.First(e => e.GetComponent<SpecialEnemy>() == null).GetComponent<Health>();
            var champion = enemies.First(e => e.GetComponent<SpecialEnemy>()?.Rank == EnemyRank.Champion).GetComponent<SpecialEnemy>();
            var morvak = enemies.First(e => e.GetComponent<SpecialEnemy>()?.Rank == EnemyRank.Unique).GetComponent<SpecialEnemy>();

            // tutti vicini al cavaliere, ognuno ferito
            Bars.SetOn(true);
            var spots = new[] { Vector3.right * 3f, Vector3.right * 5f, Vector3.right * 7f };
            var health = new[] { normal, champion.GetComponent<Health>(), morvak.GetComponent<Health>() };
            for (int i = 0; i < 3; i++)
            {
                health[i].transform.position = Player.position + spots[i];
                health[i].SetCurrent(health[i].Max * 0.5f);
            }

            yield return null;
            yield return null;

            Assert.IsTrue(Bars.IsShowing(normal));
            Assert.AreNotEqual(champion.Numbers.Color, Bars.ColorOf(normal), "il rosso, non un grado");
            Assert.AreEqual(string.Empty, Bars.NameOf(normal), "un normale non ha il nome");
            Assert.AreEqual(champion.Numbers.Color, Bars.ColorOf(champion.GetComponent<Health>()), "il colore dei campioni");
            Assert.AreEqual(string.Empty, Bars.NameOf(champion.GetComponent<Health>()), "un campione non ha il nome");
            Assert.AreEqual(morvak.Numbers.Color, Bars.ColorOf(morvak.GetComponent<Health>()), "il colore degli unici");
            Assert.AreEqual("Morvak the Jailer", Bars.NameOf(morvak.GetComponent<Health>()));
            Localizer.SetLanguage("it");
            Assert.AreEqual("Morvak il Carceriere", Bars.NameOf(morvak.GetComponent<Health>()), "il nome segue la lingua");
        }

        [UnityTest, Description("Il re delle ossa ha la sua barra in alto: ferito non ne ha una sopra la testa")]
        public IEnumerator Bars_TheBossHasNone()
        {
            yield return LoadGeneratedCore();
            BossController boss = null;
            for (ulong seed = 1; seed <= 10 && boss == null; seed++)
            {
                Manager.RunSeed = seed;
                Manager.LoadLevel("Level_Caves", "FromAbove", 8);
                yield return null;
                for (float t = 0f; Manager.IsTransitioning || Manager.CurrentLevel == null || Manager.CurrentLevel.Depth != 8; t += Time.unscaledDeltaTime)
                {
                    Assert.Less(t, 15f, "la profondità 8 non arriva");
                    yield return null;
                }

                yield return null;
                yield return null;
                boss = Manager.CurrentLevel.Boss;
            }

            Assert.IsNotNull(boss, "il re c'è");
            Bars.SetOn(true);
            boss.Health.TakeDamage(new DamageInfo(boss.Health.Max * 0.2f, DamageType.Physical, Player.gameObject));
            yield return null;
            Assert.IsFalse(Bars.IsShowing(boss.Health));
            Assert.AreEqual(0, Bars.WoundedCount, "il re non conta tra i feriti con la barra");
        }
    }
}
