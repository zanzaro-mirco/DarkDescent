using System.Collections;
using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.Interaction;
using DarkDescent.Levels;
using DarkDescent.Player;
using DarkDescent.Progression;
using DarkDescent.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Il movimento con WASD e le frecce (M23): il cavaliere cammina dove dice lo schermo, i muri lo fermano senza fargli
    /// fare il giro, un tasto prende il posto del click, un click su un nemico a portata colpisce da fermo (D2–D8).
    /// </summary>
    public class KeyboardMovementTests : SandboxFixture
    {
        // uno spiazzo libero della sandbox (il terreno va da -50 a 50, gli ostacoli stanno attorno all'origine)
        private static readonly Vector3 OpenGround = new Vector3(20f, 0f, 20f);

        private Keyboard _keyboard;
        private PlayerController _controller;

        private IEnumerator Load()
        {
            yield return LoadSandbox();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _controller = Player.GetComponent<PlayerController>();
            Assert.IsTrue(PlayerAgent.Warp(OpenGround), "lo spiazzo sta sul NavMesh");
            yield return null;
        }

        private Vector3 World(Vector2 keys)
        {
            Transform view = Camera.transform;
            return KeyboardMove.WorldDirection(keys, view.forward, view.up, view.right);
        }

        // gli otto modi di premere i tasti, e quello che porta più vicino a una direzione del mondo
        private Vector2 KeysToward(Vector3 target)
        {
            target.y = 0f;
            Vector2 best = Vector2.zero;
            float bestDot = float.MinValue;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    var keys = new Vector2(x, y);
                    float dot = Vector3.Dot(World(keys), target.normalized);
                    if (dot > bestDot)
                    {
                        bestDot = dot;
                        best = keys;
                    }
                }
            }

            return best;
        }

        // un solo evento con tutti i tasti premuti: più Set di fila sullo stesso byte dei tasti si cancellano a vicenda,
        // ognuno scrive la copia del byte letta prima che gli altri siano elaborati
        private void Hold(Vector2 keys)
        {
            var pressed = new System.Collections.Generic.List<Key>();
            if (keys.y > 0.3f) pressed.Add(Key.W);
            if (keys.y < -0.3f) pressed.Add(Key.S);
            if (keys.x > 0.3f) pressed.Add(Key.D);
            if (keys.x < -0.3f) pressed.Add(Key.A);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(pressed.ToArray()));
        }

        private void ReleaseKeys()
        {
            Hold(Vector2.zero);
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        [UnityTest, Description("Tenendo premuto W, A, S, D e la diagonale, il cavaliere cammina verso l'alto, la sinistra, il basso, la destra e la diagonale dello schermo")]
        public IEnumerator Keys_WalkWhereTheScreenSays()
        {
            yield return Load();
            foreach (var keys in new[] { Vector2.up, Vector2.left, Vector2.down, Vector2.right, new Vector2(1f, 1f) })
            {
                PlayerAgent.Warp(OpenGround);
                yield return null;
                Vector3 start = Player.position;

                Hold(keys);
                yield return new WaitForSeconds(0.8f);
                Assert.IsTrue(_controller.IsKeyboardDriving, $"tasti {keys}: guidano il cavaliere");
                Vector3 moved = Flat(Player.position - start);
                ReleaseKeys();

                Assert.Greater(moved.magnitude, 1.2f, $"tasti {keys}: ha camminato");
                Assert.Less(Vector3.Angle(moved, World(keys)), 20f, $"tasti {keys}: nella direzione dello schermo");
                yield return new WaitForSeconds(0.5f);
            }
        }

        [UnityTest, Description("Rilasciati i tasti il cavaliere si ferma: niente percorso, niente corsa residua")]
        public IEnumerator ReleasingTheKeys_StopsTheKnight()
        {
            yield return Load();
            Hold(Vector2.up);
            yield return new WaitForSeconds(0.6f);
            ReleaseKeys();
            yield return new WaitForSeconds(0.5f);

            Assert.IsFalse(_controller.IsKeyboardDriving);
            Assert.IsFalse(PlayerAgent.hasPath, "nessun percorso residuo");
            Vector3 stopped = Player.position;
            yield return new WaitForSeconds(0.4f);
            Assert.Less(FlatDistance(stopped, Player.position), 0.05f, "fermo");
        }

        [UnityTest, Description("La diagonale non è più veloce: due tasti insieme fanno la stessa strada di uno solo")]
        public IEnumerator Diagonal_IsNotFaster()
        {
            yield return Load();
            Hold(Vector2.up);
            yield return new WaitForSeconds(1.2f);
            float straight = FlatDistance(Player.position, OpenGround);
            ReleaseKeys();
            yield return new WaitForSeconds(0.4f);

            PlayerAgent.Warp(OpenGround);
            yield return null;
            Hold(new Vector2(1f, 1f));
            yield return new WaitForSeconds(1.2f);
            float diagonal = FlatDistance(Player.position, OpenGround);
            ReleaseKeys();

            Assert.Greater(straight, 2f);
            Assert.AreEqual(straight, diagonal, straight * 0.2f, "la stessa strada, in diagonale");
        }

        [UnityTest, Description("Contro un muro il cavaliere ci arriva e resta lì: niente giro dall'altra parte, anche tenendo il tasto a lungo")]
        public IEnumerator KeyTowardsAWall_StopsAtIt()
        {
            yield return Load();
            // il muro sta in (0, 1, -6), largo 8 e profondo 1: la sua faccia verso il cavaliere è a z = -5,5
            PlayerAgent.Warp(new Vector3(0f, 0f, -3f));
            yield return null;

            Hold(KeysToward(Vector3.back));
            yield return new WaitForSeconds(3f);
            Vector3 atTheWall = Player.position;
            yield return new WaitForSeconds(1.5f);
            ReleaseKeys();

            Assert.Greater(atTheWall.z, -5.5f, $"il muro non si attraversa: {atTheWall}");
            Assert.Less(atTheWall.z, -4.5f, $"è arrivato al muro: {atTheWall}");
            Assert.Less(Mathf.Abs(atTheWall.x), 1f, "dritto contro il muro non scivola di lato");
            Assert.Less(FlatDistance(atTheWall, Player.position), 0.1f, "e non si sposta di più tenendo il tasto");
        }

        [UnityTest, Description("Contro un muro di sbieco il cavaliere scivola lungo il muro, senza oltrepassarlo")]
        public IEnumerator KeyAtAnAngleToAWall_SlidesAlongIt()
        {
            yield return Load();
            Vector3 start = new Vector3(-3.5f, 0f, -3f);
            PlayerAgent.Warp(start);
            yield return null;

            Hold(KeysToward(new Vector3(1f, 0f, -1f)));
            yield return new WaitForSeconds(1.4f);
            Vector3 end = Player.position;
            ReleaseKeys();

            Assert.Greater(end.z, -5.5f, $"il muro non si attraversa: {end}");
            Assert.Greater(end.x, start.x + 2f, $"ha scivolato lungo il muro: {end}");
        }

        [UnityTest, Description("Un tasto prende il posto del percorso del click; rilasciato, ci si ferma, e il click riprende com'è")]
        public IEnumerator Key_TakesOverFromAClick_AndTheClickComesBack()
        {
            yield return Load();
            Vector3 far = OpenGround + new Vector3(10f, 0f, 10f);
            ClickAt(far);
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(PlayerAgent.hasPath, "il click ha dato un percorso");

            Vector2 keys = KeysToward(OpenGround - far);
            Hold(keys);
            yield return new WaitForSeconds(0.4f);
            Assert.IsTrue(_controller.IsKeyboardDriving);
            Vector3 aim = Flat(PlayerAgent.destination - Player.position);
            Assert.Less(Vector3.Angle(aim, World(keys)), 25f, "la destinazione segue i tasti, non più il click");
            Assert.Less(aim.magnitude, 2.5f, "poco avanti, non il punto cliccato");

            ReleaseKeys();
            yield return new WaitForSeconds(0.6f);
            Assert.IsFalse(PlayerAgent.hasPath, "rilasciati i tasti ci si ferma, il click di prima non riprende");

            Vector3 target = Player.position + new Vector3(4f, 0f, 0f);
            ClickAt(target);
            float elapsed = 0f;
            while (elapsed < 4f && FlatDistance(Player.position, target) > 0.4f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.Less(FlatDistance(Player.position, target), 0.4f, "un click a tasti fermi cammina come sempre");
        }

        [UnityTest, Description("Un click sul pavimento con i tasti premuti non cambia strada: guidano i tasti")]
        public IEnumerator ClickOnTheFloor_WhileKeysAreDown_ChangesNothing()
        {
            yield return Load();
            Vector2 keys = Vector2.up;
            Hold(keys);
            yield return new WaitForSeconds(0.4f);
            ClickAt(OpenGround + new Vector3(-10f, 0f, 0f));
            yield return new WaitForSeconds(0.5f);

            Assert.IsTrue(_controller.IsKeyboardDriving);
            Vector3 aim = Flat(PlayerAgent.destination - Player.position);
            Assert.Less(Vector3.Angle(aim, World(keys)), 25f, "la destinazione è ancora quella dei tasti");
            ReleaseKeys();
        }

        [UnityTest, Description("Un blocco da fuori (una finestra a tutto schermo, un'abilità) ferma i tasti, e tolto riprendono")]
        public IEnumerator Blocker_StopsTheKeys()
        {
            yield return Load();
            bool blocked = true;
            _controller.SetKeyboardBlocker(() => blocked);
            Vector3 start = Player.position;

            Hold(Vector2.up);
            yield return new WaitForSeconds(0.6f);
            Assert.Less(FlatDistance(start, Player.position), 0.05f, "bloccato non si muove");
            Assert.IsFalse(_controller.IsKeyboardDriving);

            blocked = false;
            yield return new WaitForSeconds(0.6f);
            Assert.Greater(FlatDistance(start, Player.position), 0.8f, "sbloccato cammina, anche col tasto già premuto");
            ReleaseKeys();
        }

        // lo scheletro fermo a una certa distanza dal cavaliere: non insegue e non attacca, il test parla del colpo
        private (Health health, Transform transform) Skeleton(float away, Vector3 side)
        {
            var skeleton = GameObject.Find("Skeleton");
            skeleton.GetComponent<EnemyAI>().enabled = false;
            Assert.IsTrue(skeleton.GetComponent<NavMeshAgent>().Warp(Player.position + side.normalized * away), "lo scheletro sta sul NavMesh");
            // il collider segue la posizione solo al passo di fisica: il raggio del click lo vuole dov'è adesso
            Physics.SyncTransforms();
            return (skeleton.GetComponent<Health>(), skeleton.transform);
        }

        [UnityTest, Description("A tasti premuti, un click su un nemico a portata lo colpisce da fermo: il cavaliere non si sposta finché non ha colpito")]
        public IEnumerator ClickOnAnEnemyInReach_WhileKeysAreDown_StrikesStandingStill()
        {
            yield return Load();
            var (health, skeleton) = Skeleton(1.4f, Vector3.right);
            yield return null;

            // i tasti portano via dal nemico: se il click non fermasse il cavaliere, uscirebbe di portata
            Hold(KeysToward(Vector3.left));
            yield return null;
            yield return null;
            Vector3 atTheClick = Player.position;
            ClickAt(skeleton.position + Vector3.up);

            float elapsed = 0f;
            while (elapsed < 4f && health.Current >= health.Max)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Vector3 whenHit = Player.position;
            ReleaseKeys();
            Assert.Less(health.Current, health.Max, "il colpo è arrivato");
            Assert.Less(FlatDistance(atTheClick, whenHit), 0.5f, "da fermo: i tasti non hanno portato il cavaliere via");
        }

        [UnityTest, Description("Il Mago, a tasti premuti, lancia il dardo da fermo a un nemico a portata del dardo, che è più lontano di un colpo di spada")]
        public IEnumerator Mage_ThrowsTheBoltStandingStill_WhileKeysAreDown()
        {
            yield return Load();
            var classes = Player.GetComponent<PlayerClass>();
            classes.Apply(classes.Catalog.Find("mage"), "Merlino", true);
            yield return null;

            var (health, skeleton) = Skeleton(4f, Vector3.right);
            yield return null;
            Hold(KeysToward(Vector3.left));
            yield return null;
            yield return null;
            Vector3 atTheClick = Player.position;
            ClickAt(skeleton.position + Vector3.up);

            float elapsed = 0f;
            while (elapsed < 4f && health.Current >= health.Max)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Vector3 whenHit = Player.position;
            ReleaseKeys();
            Assert.Less(health.Current, health.Max, "il dardo è arrivato");
            Assert.Less(FlatDistance(atTheClick, whenHit), 0.5f, "da fermo: i tasti non hanno portato il Mago via");
        }

        [UnityTest, Description("A tasti premuti, un click su un nemico fuori portata non fa niente: non si insegue, e i tasti guidano")]
        public IEnumerator ClickOnAnEnemyOutOfReach_WhileKeysAreDown_DoesNothing()
        {
            yield return Load();
            var (health, skeleton) = Skeleton(8f, Vector3.right);
            yield return null;
            float before = FlatDistance(Player.position, skeleton.position);

            Vector2 keys = KeysToward(Vector3.left);
            Hold(keys);
            yield return new WaitForSeconds(0.2f);
            ClickAt(skeleton.position + Vector3.up);
            yield return new WaitForSeconds(1f);

            Assert.IsTrue(_controller.IsKeyboardDriving, "i tasti guidano ancora");
            Assert.AreEqual(health.Max, health.Current, "nessun colpo");
            Assert.Greater(FlatDistance(Player.position, skeleton.position), before, "non si è avvicinato al nemico");
            ReleaseKeys();
        }

        [UnityTest, Description("A tasti premuti, un click sulla cassa è un comando nuovo: i tasti tacciono, il cavaliere ci va e la apre, anche col tasto ancora giù")]
        public IEnumerator ClickOnAChest_WhileKeysAreDown_WalksAndOpensIt()
        {
            yield return LoadGeneratedCore();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _controller = Player.GetComponent<PlayerController>();
            var manager = Object.FindFirstObjectByType<LevelManager>();
            foreach (var enemy in manager.CurrentLevel.Enemies)
            {
                enemy.GetComponent<Health>().TakeDamage(new DamageInfo(1000f, DamageType.Physical, null));
            }

            yield return null;
            var chest = manager.CurrentLevel.Chests[0];
            Vector3 approach = chest.GetComponent<Interactable>().ApproachPoint;
            PlayerAgent.Warp(approach + chest.transform.forward * 3f);
            yield return new WaitForSeconds(0.3f);

            // i tasti portano via dalla cassa
            Hold(KeysToward(Player.position - chest.transform.position));
            yield return new WaitForSeconds(0.2f);
            ClickAt(chest.transform.position + Vector3.up * 0.7f);

            float elapsed = 0f;
            while (!chest.IsOpen && elapsed < 8f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(chest.IsOpen, "il click sulla cassa ha vinto sui tasti");
            Assert.IsFalse(_controller.IsKeyboardDriving, "i tasti tacciono finché non si rilasciano");
            ReleaseKeys();
        }

        [UnityTest, Description("A ogni scatto della visuale W porta verso l'alto dello schermo, con la camera di quel momento")]
        public IEnumerator EveryViewTurn_WStillGoesUp()
        {
            yield return LoadGeneratedCore();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            var rotation = Object.FindFirstObjectByType<CameraRotation>();

            for (int turn = 0; turn < 4; turn++)
            {
                if (turn > 0)
                {
                    rotation.Turn(1);
                    yield return new WaitForSecondsRealtime(0.6f);
                }

                Hold(Vector2.up);
                yield return new WaitForSeconds(0.1f);
                Vector3 aim = Flat(PlayerAgent.destination - Player.position);
                Vector3 expected = World(Vector2.up);
                ReleaseKeys();
                yield return new WaitForSeconds(0.3f);

                Assert.Greater(aim.magnitude, 0.3f, $"scatto {turn}: il cavaliere ha dove andare");
                Assert.Less(Vector3.Angle(aim, expected), 25f, $"scatto {turn}: verso l'alto dello schermo");
            }
        }
    }
}
