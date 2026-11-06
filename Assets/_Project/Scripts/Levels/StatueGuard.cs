using System;
using System.Collections;
using DarkDescent.Enemies;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Levels
{
    /// <summary>
    /// La statua che si anima (D7 della M25): una statua di pietra vicino a una cassa. Quando il cavaliere arriva a pochi metri dalla cassa si scrolla
    /// (un velo di polvere e un verso, un secondo di avviso) e prende vita: la statua sparisce e al suo posto nasce il nemico <c>Statue</c>, lento e duro,
    /// che il CompositionRoot lega come gli altri (<see cref="Spawned"/>). Morto, resta un mucchio di macerie e il ricordo del livello (ADR-079) lo
    /// sa. Se si lascia il livello a metà, la statua torna in piedi, come il mimic. Un segreto del livello (<see cref="ILevelSecret"/>).
    /// </summary>
    [DisallowMultipleComponent]
    public class StatueGuard : MonoBehaviour, ILevelSecret
    {
        /// <summary>Acceso nel gioco, spento dai test PlayMode (D14 della M25): una statua che si anima cambia i combattimenti dei test. Lo accendono solo i suoi test.</summary>
        public static bool Enabled { get; set; } = true;

        public enum Phase
        {
            Still,
            Shaking,
            Awake,
            Done,
        }

        [Tooltip("La statua di pietra: sparisce quando prende vita.")]
        [SerializeField] private GameObject _statue;

        [Tooltip("Le macerie che restano quando il nemico muore (o in un ricordo di prima).")]
        [SerializeField] private GameObject _rubble;

        [Tooltip("Il velo di polvere dello scrollarsi.")]
        [SerializeField] private ParticleSystem _dust;

        [SerializeField] private AudioSource _audio;

        [Tooltip("Il grattare della pietra mentre si scrolla.")]
        [SerializeField] private AudioClip _grind;

        [Tooltip("Il verso quando prende vita.")]
        [SerializeField] private AudioClip _roar;

        [SerializeField] private EnemyAI _enemyPrefab;

        [Tooltip("A che distanza dalla cassa la statua si scrolla, in metri.")]
        [SerializeField, Min(1f)] private float _wakeRange = 3.2f;

        [Tooltip("Quanto dura l'avviso, in secondi: il cavaliere ha il tempo di allontanarsi.")]
        [SerializeField, Min(0.2f)] private float _shakeTime = 1.0f;

        private Transform _player;
        private Vector3 _chestPosition;
        private float _nextCheck;
        private Quaternion _statueRest;
        private Vector3 _statueHome;

        public Phase State { get; private set; } = Phase.Still;

        public bool IsDone => State == Phase.Done;

        /// <summary>La cassa a cui fa la guardia: la statua si scrolla quando il cavaliere le arriva vicino.</summary>
        public Vector3 ChestPosition => _chestPosition;

        /// <summary>Il nemico nato dalla statua, se è in piedi.</summary>
        public EnemyAI Enemy { get; private set; }

        /// <summary>Il nemico è nato: da legare (difficoltà, esperienza, barra) e da svegliare.</summary>
        public event Action<EnemyAI> Spawned;

        /// <summary>Mette la statua al suo posto, voltata verso la cassa.</summary>
        public static StatueGuard Build(LevelContext level, GameObject prefab, Vector3 position, Vector3 chestPosition)
        {
            Vector3 toward = Vector3.ProjectOnPlane(chestPosition - position, Vector3.up);
            var instance = Instantiate(prefab, position, toward.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(toward) : Quaternion.identity, level.transform);
            instance.name = "StatueGuard";
            var guard = instance.GetComponent<StatueGuard>();
            guard._chestPosition = chestPosition;
            return guard;
        }

        /// <summary>Il cavaliere: chi tiene la partita lo dà alla statua perché lo guardi (controllo quattro volte al secondo).</summary>
        public void BindPlayer(Transform player)
        {
            _player = player;
        }

        private void Awake()
        {
            if (_statue != null)
            {
                _statueRest = _statue.transform.localRotation;
                _statueHome = _statue.transform.localPosition;
            }

            if (_rubble != null)
            {
                _rubble.SetActive(false);
            }
        }

        private void Update()
        {
            if (State != Phase.Still || _player == null || Time.time < _nextCheck)
            {
                return;
            }

            _nextCheck = Time.time + 0.25f;
            Vector3 away = Vector3.ProjectOnPlane(_player.position - _chestPosition, Vector3.up);
            if (away.sqrMagnitude < _wakeRange * _wakeRange)
            {
                StartCoroutine(Wake());
            }
        }

        /// <summary>Già caduta in una visita di prima (ADR-079): macerie, e non si anima più.</summary>
        public void RestoreDone()
        {
            State = Phase.Done;
            if (_statue != null)
            {
                _statue.SetActive(false);
            }

            if (_rubble != null)
            {
                _rubble.SetActive(true);
            }
        }

        private IEnumerator Wake()
        {
            State = Phase.Shaking;
            if (_dust != null)
            {
                _dust.Play();
            }

            if (_audio != null && _grind != null)
            {
                _audio.PlayOneShot(_grind);
            }

            // si scrolla: un tremito che cresce, la pietra che si sposta di pochi centimetri
            for (float t = 0f; t < _shakeTime; t += Time.deltaTime)
            {
                if (_statue != null)
                {
                    float strength = Mathf.Lerp(0.4f, 1f, t / _shakeTime);
                    float shake = Mathf.Sin(t * 70f) * strength;
                    _statue.transform.localRotation = _statueRest * Quaternion.Euler(0f, shake * 4f, shake * 2f);
                    _statue.transform.localPosition = _statueHome + new Vector3(shake * 0.03f, 0f, 0f);
                }

                yield return null;
            }

            if (_statue != null)
            {
                _statue.SetActive(false);
            }

            Vector3 spot = NavMesh.SamplePosition(transform.position, out var hit, 2.5f, NavMesh.AllAreas) ? hit.position : transform.position;
            if (_enemyPrefab == null)
            {
                RestoreDone();
                yield break;
            }

            Enemy = Instantiate(_enemyPrefab, spot, transform.rotation);
            Enemy.Killed += HandleKilled;
            State = Phase.Awake;
            if (_audio != null && _roar != null)
            {
                _audio.PlayOneShot(_roar);
            }

            Spawned?.Invoke(Enemy);
        }

        private void HandleKilled(EnemyAI enemy)
        {
            enemy.Killed -= HandleKilled;
            State = Phase.Done;
            if (_rubble != null)
            {
                _rubble.SetActive(true);
            }
        }
    }
}
