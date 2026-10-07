using System.Collections;
using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.Interaction;
using DarkDescent.Items;
using DarkDescent.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Dalla prova della build M7: una cassa dietro una colonna e uno scheletro dietro una colonna non si
    /// riuscivano a cliccare. Colonne, barili e muri bassi si attraversano; i muri alti no.
    /// </summary>
    public class CursorThroughObstaclesTests : SandboxFixture
    {
        private static T Load<T>(string path) where T : Object
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
        }

        // Mette il pezzo sulla linea tra la camera e il punto, un metro e mezzo prima del punto,
        // girato verso la camera: il raggio del cursore lo incontra prima del punto.
        private GameObject PutInFront(string prefabPath, Vector3 point)
        {
            Vector3 toCamera = Camera.transform.position - point;
            Vector3 flat = new Vector3(toCamera.x, 0f, toCamera.z).normalized;
            var piece = Object.Instantiate(Load<GameObject>(prefabPath), new Vector3(point.x, 0f, point.z) + flat * 1.5f, Quaternion.LookRotation(flat));
            Physics.SyncTransforms();
            var collider = piece.GetComponent<Collider>();
            Ray ray = new Ray(Camera.transform.position, point - Camera.transform.position);
            Assert.IsTrue(collider.Raycast(ray, out var hit, 100f), "il pezzo deve coprire il punto");
            Assert.Less(hit.distance, Vector3.Distance(Camera.transform.position, point));
            return piece;
        }

        private IEnumerator StillSkeleton()
        {
            yield return LoadSandbox();
            Object.FindFirstObjectByType<EnemyAI>().enabled = false;
        }

        [UnityTest, Description("Uno scheletro dietro una colonna si clicca: il cavaliere lo prende come bersaglio")]
        public IEnumerator EnemyBehindPillar_IsClickable()
        {
            yield return StillSkeleton();
            var skeleton = Object.FindFirstObjectByType<EnemyAI>();
            Vector3 chest = skeleton.transform.position + Vector3.up;
            PutInFront("Assets/_Project/Prefabs/Dungeon/Pillar.prefab", chest);

            ClickAt(chest);
            yield return null;
            yield return null;

            Assert.IsTrue(Player.GetComponent<MeleeAttack>().HasTarget, "il click deve arrivare allo scheletro");
        }

        [UnityTest, Description("Un oggetto a terra dietro una colonna si evidenzia sotto il cursore; dietro un muro alto no")]
        public IEnumerator ItemBehindPillar_IsHovered_ButNotBehindWall()
        {
            yield return StillSkeleton();
            var prefab = Load<GroundItem>("Assets/_Project/Prefabs/GroundItem.prefab");
            var sword = Load<ItemDefinition>("Assets/_Project/Data/Items/ShortSword.asset");
            // davanti al cavaliere, visto dalla camera: lui non copre l'oggetto
            var item = GroundItem.Spawn(prefab, new ItemInstance(sword), Player.position + new Vector3(-2f, 0f, -2f));
            yield return null;
            Vector3 point = item.GetComponent<BoxCollider>().bounds.center;
            var controller = Player.GetComponent<PlayerController>();

            var pillar = PutInFront("Assets/_Project/Prefabs/Dungeon/Pillar.prefab", point);
            PointAt(point);
            yield return null;
            yield return null;
            Assert.AreSame(item.GetComponent<Interactable>(), controller.Hovered, "dietro la colonna si vede e si clicca");

            Object.Destroy(pillar);
            yield return null;
            var wall = PutInFront("Assets/_Project/Prefabs/Dungeon/Wall.prefab", point);
            Assert.IsTrue(wall.CompareTag(PlayerController.WallTag));
            yield return null;
            yield return null;
            Assert.IsNull(controller.Hovered, "un muro alto ferma il cursore");
        }
    }
}
