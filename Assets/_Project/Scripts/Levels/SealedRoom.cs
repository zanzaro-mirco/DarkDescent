using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// La stanza sigillata di un livello (D5 della M25): la porta di pietra sul corridoio e la leva che la apre, messe al caricamento sopra il livello
    /// generato secondo il piano di <see cref="SealedRoomPlanner"/>. Il fondo chiuso ha già le sue casse e i suoi nemici: sono il tesoro e la scorta.
    /// Si registra tra i segreti del livello prima che il ricordo venga rimesso: tirata la leva, il ricordo la ritrova tirata e la porta aperta.
    /// </summary>
    [DisallowMultipleComponent]
    public class SealedRoom : MonoBehaviour, ILevelSecret
    {
        /// <summary>
        /// Acceso nel gioco, spento dai test PlayMode (D14 della M25): una porta di pietra chiusa cambia i percorsi che molti test contano di fare.
        /// Solo i test della stanza sigillata lo accendono.
        /// </summary>
        public static bool Enabled { get; set; } = true;

        private readonly HashSet<Vector2Int> _region = new HashSet<Vector2Int>();

        public SealedDoor Door { get; private set; }

        public SecretLever Lever { get; private set; }

        public SealedRoomPlanner.Plan Plan { get; private set; }

        public bool IsDone => Door != null && Door.IsOpen;

        /// <summary>
        /// Mette la porta e la leva del piano sotto il livello. La porta sta di traverso al corridoio, con la faccia a rune dal lato opposto al fondo;
        /// la leva sta contro il muro, voltata verso lo spazio libero.
        /// </summary>
        public static SealedRoom Build(LevelContext level, SealedRoomPlanner.Plan plan, GameObject doorPrefab, GameObject leverPrefab)
        {
            var room = new GameObject("SealedRoom").AddComponent<SealedRoom>();
            room.transform.SetParent(level.transform, false);
            room.Plan = plan;
            foreach (var cell in plan.Region)
            {
                room._region.Add(cell);
            }

            // la faccia con le rune guarda chi arriva, cioè il lato opposto al fondo
            Vector3 toRoom = Vector3.forward;
            foreach (var step in new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left })
            {
                if (room._region.Contains(plan.Door + step))
                {
                    toRoom = new Vector3(step.x, 0f, -step.y);
                }
            }

            var door = Instantiate(doorPrefab, LevelMap.CellCenter(plan.Door.x, plan.Door.y), Quaternion.LookRotation(-toRoom), room.transform);
            door.name = "SealedDoor";
            room.Door = door.GetComponent<SealedDoor>();

            // la leva contro il muro, un metro dentro la cella, girata verso lo spazio libero
            Vector3 facing = new Vector3(plan.LeverFacing.x, 0f, -plan.LeverFacing.y);
            var lever = Instantiate(leverPrefab, LevelMap.CellCenter(plan.Lever.x, plan.Lever.y) - facing * 1.1f, Quaternion.LookRotation(facing), room.transform);
            lever.name = "SecretLever";
            room.Lever = lever.GetComponent<SecretLever>();
            room.Lever.Pulled += room.HandlePulled;
            return room;
        }

        /// <summary>Il punto del mondo sta nel fondo chiuso dalla porta: gli abitanti di lì non fanno la ronda, che li porterebbe oltre la porta.</summary>
        public bool Holds(Vector3 world)
        {
            int x = Mathf.RoundToInt(world.x / LevelMap.CellSize);
            int y = Mathf.RoundToInt(-world.z / LevelMap.CellSize);
            return _region.Contains(new Vector2Int(x, y));
        }

        /// <summary>Il punto sta nel fondo o lo tocca: una cassa contro il muro del fondo sta sulla cella di un ostacolo, accanto al pavimento del fondo.</summary>
        public bool Touches(Vector3 world)
        {
            int x = Mathf.RoundToInt(world.x / LevelMap.CellSize);
            int y = Mathf.RoundToInt(-world.z / LevelMap.CellSize);
            return _region.Contains(new Vector2Int(x, y)) || _region.Contains(new Vector2Int(x + 1, y)) || _region.Contains(new Vector2Int(x - 1, y))
                || _region.Contains(new Vector2Int(x, y + 1)) || _region.Contains(new Vector2Int(x, y - 1));
        }

        public void RestoreDone()
        {
            Lever.RestorePulled();
            Door.RestoreOpen();
        }

        private void HandlePulled()
        {
            Door.Open();
        }
    }
}
