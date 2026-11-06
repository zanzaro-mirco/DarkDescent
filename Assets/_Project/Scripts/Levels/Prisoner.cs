using System;
using System.Collections;
using System.Collections.Generic;
using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.Interaction;
using DarkDescent.Items;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Il prigioniero (D6 della M25): un cavaliere in catene, seduto contro un muro tra dei nemici che già c'erano e sono la sua scorta. Un click lo libera
    /// solo se la scorta è morta («prima la scorta» altrimenti): esulta, lascia una pozione e un po' d'oro, dice il suo consiglio (chi tiene la partita apre
    /// il pannello di lettura con <see cref="Freed"/>) e sale a piedi verso la scala in su, dove sparisce. Le catene restano. Un segreto del livello
    /// (<see cref="ILevelSecret"/>): liberato, il ricordo (ADR-079) lo sa e al ritorno c'è solo la catena vuota.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Interactable))]
    public class Prisoner : MonoBehaviour, ILevelSecret
    {
        /// <summary>Acceso nel gioco, spento dai test PlayMode (D14 della M25): un prigioniero e la sua scorta cambiano i percorsi e i conti dei test. Lo accendono solo i suoi test.</summary>
        public static bool Enabled { get; set; } = true;

        public enum Phase
        {
            Captive,
            Thanking,
            Leaving,
            Gone,
        }

        [Tooltip("Il modello del cavaliere, con il suo Animator.")]
        [SerializeField] private Animator _animator;

        [Tooltip("Le catene: restano quando se ne va.")]
        [SerializeField] private GameObject _chains;

        [Tooltip("Il collider del click: si spegne quando è libero.")]
        [SerializeField] private Collider _clickArea;

        [SerializeField] private AnimationClip _sit;

        [SerializeField] private AnimationClip _cheer;

        [SerializeField] private AnimationClip _walk;

        [Tooltip("Come i bauli: l'oggetto a terra, la pozione e l'oro del ringraziamento.")]
        [SerializeField] private GroundItem _groundItemPrefab;

        [SerializeField] private PotionDefinition _potion;

        [SerializeField] private GoldDefinition _gold;

        [SerializeField, Min(0.5f)] private float _walkSpeed = 2.4f;

        [Tooltip("Per quanto esulta prima di andare, in secondi.")]
        [SerializeField, Min(0.5f)] private float _thankSeconds = 2.4f;

        private readonly List<EnemyAI> _escort = new List<EnemyAI>();
        private Interactable _interactable;
        private PlayableGraph _graph;
        private AnimationMixerPlayable _mixer;
        private Vector3 _exit;
        private int _depth;
        private int _advice;

        public Phase State { get; private set; } = Phase.Captive;

        public bool IsDone => State != Phase.Captive;

        /// <summary>Quale consiglio dà (1–4).</summary>
        public int Advice => _advice;

        public int Depth => _depth;

        /// <summary>La cella del livello dove sta: gli altri segreti stanno lontano.</summary>
        public Vector2Int Cell { get; private set; }

        /// <summary>La scorta: i nemici che erano già in quella stanza. Quelli morti restano nella lista come null di Unity.</summary>
        public IReadOnlyList<EnemyAI> Escort => _escort;

        /// <summary>È liberato: chi tiene la partita apre il pannello con il suo consiglio.</summary>
        public event Action<Prisoner> Freed;

        /// <summary>Un click con la scorta ancora viva: chi tiene la partita lo dice.</summary>
        public event Action Refused;

        /// <summary>Ha lasciato il livello, o la sua strada non c'era e sparisce lì dov'è.</summary>
        public event Action Left;

        /// <summary>
        /// Lo mette contro il muro del suo posto, voltato verso lo spazio libero, con la scorta dei nemici di casa nelle <see cref="PrisonerRules.EscortCells"/> celle attorno
        /// (le stesse che ha contato il piano, fuori dal fondo sigillato <paramref name="exclude"/>) e la scala in su dove va.
        /// </summary>
        public static Prisoner Build(LevelContext level, GameObject prefab, SecretSpots.Spot spot, int depth, int advice, Vector3 exit, ISet<Vector2Int> exclude = null)
        {
            Vector3 facing = new Vector3(spot.Facing.x, 0f, -spot.Facing.y);
            var instance = Instantiate(prefab, LevelMap.CellCenter(spot.Cell.x, spot.Cell.y) - facing * 1.1f, Quaternion.LookRotation(facing), level.transform);
            instance.name = "Prisoner";
            var prisoner = instance.GetComponent<Prisoner>();
            prisoner._depth = depth;
            prisoner._advice = advice;
            prisoner._exit = exit;
            prisoner.Cell = spot.Cell;
            foreach (var enemy in level.Enemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                var home = new Vector2Int(Mathf.RoundToInt(enemy.HomePosition.x / LevelMap.CellSize), Mathf.RoundToInt(-enemy.HomePosition.z / LevelMap.CellSize));
                if (Mathf.Abs(home.x - spot.Cell.x) <= PrisonerRules.EscortCells && Mathf.Abs(home.y - spot.Cell.y) <= PrisonerRules.EscortCells && (exclude == null || !exclude.Contains(home)))
                {
                    prisoner._escort.Add(enemy);
                }
            }

            return prisoner;
        }

        /// <summary>Quanti della scorta sono ancora in piedi.</summary>
        public int EscortAlive
        {
            get
            {
                int alive = 0;
                foreach (var enemy in _escort)
                {
                    if (enemy != null && enemy.gameObject.activeInHierarchy && !enemy.GetComponent<Health>().IsDead)
                    {
                        alive++;
                    }
                }

                return alive;
            }
        }

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
        }

        private void OnEnable()
        {
            _interactable.Used += HandleUsed;
            PlayPose();
        }

        private void OnDisable()
        {
            _interactable.Used -= HandleUsed;
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }
        }

        /// <summary>Già liberato in una visita di prima (ADR-079): è salito, restano le catene vuote.</summary>
        public void RestoreDone()
        {
            State = Phase.Gone;
            Disarm();
            if (_animator != null)
            {
                _animator.gameObject.SetActive(false);
            }
        }

        // un grafo suo con le tre pose, senza Animator Controller: come gli abitanti del borgo
        private void PlayPose()
        {
            if (_animator == null || _sit == null || State != Phase.Captive)
            {
                return;
            }

            _graph = PlayableGraph.Create(name);
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            _mixer = AnimationMixerPlayable.Create(_graph, 3);
            Connect(0, _sit);
            Connect(1, _cheer);
            Connect(2, _walk);
            _mixer.SetInputWeight(0, 1f);
            AnimationPlayableOutput.Create(_graph, "Pose", _animator).SetSourcePlayable(_mixer);
            _graph.Play();
        }

        private void Connect(int input, AnimationClip clip)
        {
            if (clip == null)
            {
                return;
            }

            var playable = AnimationClipPlayable.Create(_graph, clip);
            _graph.Connect(playable, 0, _mixer, input);
        }

        // passa a un'altra posa in poco meno di mezzo secondo
        private IEnumerator BlendTo(int input)
        {
            if (!_graph.IsValid())
            {
                yield break;
            }

            float[] from = new float[3];
            for (int i = 0; i < 3; i++)
            {
                from[i] = _mixer.GetInputWeight(i);
            }

            for (float t = 0f; t < 0.4f; t += Time.deltaTime)
            {
                if (!_graph.IsValid())
                {
                    yield break;
                }

                for (int i = 0; i < 3; i++)
                {
                    _mixer.SetInputWeight(i, Mathf.Lerp(from[i], i == input ? 1f : 0f, t / 0.4f));
                }

                yield return null;
            }

            for (int i = 0; i < 3; i++)
            {
                _mixer.SetInputWeight(i, i == input ? 1f : 0f);
            }
        }

        private void Disarm()
        {
            _interactable.SetHighlighted(false);
            if (_clickArea != null)
            {
                _clickArea.enabled = false;
            }
        }

        private void HandleUsed(GameObject user)
        {
            if (State != Phase.Captive)
            {
                return;
            }

            if (EscortAlive > 0)
            {
                Refused?.Invoke();
                return;
            }

            State = Phase.Thanking;
            Disarm();

            // le catene restano dove sono: staccate da lui, che se ne va
            if (_chains != null)
            {
                _chains.transform.SetParent(transform.parent, true);
            }

            DropReward();
            Freed?.Invoke(this);
            StartCoroutine(ThankThenLeave());
        }

        // una pozione e un po' d'oro davanti a lui, senza tirare niente: la pozione della tabella dei bauli e un oro che dipende dalla profondità
        private void DropReward()
        {
            if (_groundItemPrefab == null)
            {
                return;
            }

            Vector3 front = transform.position + transform.forward * 1.4f;
            Vector3 from = transform.position + Vector3.up * 1f;
            if (_potion != null)
            {
                GroundItem.Spawn(_groundItemPrefab, new ItemInstance(_potion), front + transform.right * 0.7f, from);
            }

            if (_gold != null)
            {
                GroundItem.SpawnGold(_groundItemPrefab, _gold, PrisonerRules.Gold(_depth), front - transform.right * 0.7f, from);
            }
        }

        private IEnumerator ThankThenLeave()
        {
            yield return BlendTo(1);
            yield return new WaitForSeconds(_thankSeconds);
            State = Phase.Leaving;
            yield return BlendTo(2);
            yield return Walk();
            State = Phase.Gone;
            if (_animator != null)
            {
                _animator.gameObject.SetActive(false);
            }

            Left?.Invoke();
        }

        // a piedi lungo il cammino sul NavMesh verso la scala in su; se la strada non c'è sparisce dov'è
        private IEnumerator Walk()
        {
            var path = new NavMeshPath();
            Vector3 start = NavMesh.SamplePosition(transform.position, out var from, 2.5f, NavMesh.AllAreas) ? from.position : transform.position;
            Vector3 goal = NavMesh.SamplePosition(_exit, out var to, 3f, NavMesh.AllAreas) ? to.position : _exit;
            if (!NavMesh.CalculatePath(start, goal, NavMesh.AllAreas, path) || path.corners.Length == 0)
            {
                yield break;
            }

            foreach (var corner in path.corners)
            {
                Vector3 target = new Vector3(corner.x, transform.position.y, corner.z);
                while (Vector3.ProjectOnPlane(target - transform.position, Vector3.up).sqrMagnitude > 0.04f)
                {
                    Vector3 step = Vector3.ProjectOnPlane(target - transform.position, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(step), 8f * Time.deltaTime);
                    transform.position = Vector3.MoveTowards(transform.position, target, _walkSpeed * Time.deltaTime);
                    yield return null;
                }
            }
        }
    }
}
