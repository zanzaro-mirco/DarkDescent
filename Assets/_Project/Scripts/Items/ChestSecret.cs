using System;
using System.Collections;
using System.Collections.Generic;
using DarkDescent.Enemies;
using DarkDescent.Levels;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Items
{
    /// <summary>
    /// Il segreto di una cassa normale (D3 e D4 della M25), sul prefab della cassa. Lo accende il CompositionRoot al caricamento del
    /// livello, scegliendo tra le casse che ci sono già (<see cref="SecretRules"/>): la mappa non cambia. Un click sulla cassa non la
    /// apre: il <b>mimic</b> salta fuori dal forziere e si combatte, e alla sua morte la cassa si apre con il suo loot di sempre; il
    /// <b>baule maledetto</b> chiama un'ondata di tre-cinque nemici, e a ondata finita si apre da solo con un premio migliore. I nemici
    /// nati qui li lega il CompositionRoot (<see cref="Spawned"/>), come quelli del Custode; non sono nel ricordo del livello, quindi
    /// non spostano i posti dei nemici veri. Se si lascia il livello a metà, la cassa riparte chiusa.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Chest))]
    public class ChestSecret : MonoBehaviour
    {
        /// <summary>
        /// Acceso nel gioco, spento dai test PlayMode (D14 della M25): i test che aprono la prima cassa non devono trovare un mimic o un baule
        /// maledetto. Solo i test dei segreti lo accendono, come gli interruttori dei nemici (M24).
        /// </summary>
        public static bool Enabled { get; set; } = true;

        /// <summary>Il premio del baule maledetto (D4 e D8 della M25): un oggetto in più, magici e rari più frequenti, almeno un magico e più oro, senza spostare gli unici.</summary>
        public static readonly LootBonus CursedBonus = new LootBonus(1, 2f, 2f, Rarity.Magic, 2f, false, 1f, 1.5f);

        [Tooltip("Il modello del forziere: si nasconde mentre il mimic è in piedi al suo posto.")]
        [SerializeField] private GameObject _model;

        [Tooltip("Le catene, il teschio e il cristallo del baule maledetto (ChestCurse): spento per le casse di sempre.")]
        [SerializeField] private GameObject _curseLook;

        [SerializeField] private EnemyAI _mimicPrefab;

        [Tooltip("I nemici dell'ondata del baule maledetto: lo scheletro e il bruto.")]
        [SerializeField] private EnemyAI _skeletonPrefab;

        [SerializeField] private EnemyAI _brutePrefab;

        [Tooltip("A che distanza dal baule nascono i nemici dell'ondata.")]
        [SerializeField, Min(1f)] private float _waveRadius = 4.5f;

        private Chest _chest;
        private ulong _runSeed;
        private int _depth;
        private readonly List<EnemyAI> _alive = new List<EnemyAI>();

        /// <summary>Un nemico è nato dal segreto: da legare (difficoltà, esperienza, barra) e da svegliare.</summary>
        public event Action<EnemyAI> Spawned;

        public Chest Chest => _chest != null ? _chest : _chest = GetComponent<Chest>();

        /// <summary>I nemici del segreto ancora vivi: uno per il mimic, tre-cinque per l'ondata.</summary>
        public IReadOnlyList<EnemyAI> Alive => _alive;

        /// <summary>Il segreto si è svegliato e i suoi nemici non sono ancora tutti morti.</summary>
        public bool IsFighting => _alive.Count > 0;

        /// <summary>Accende il segreto alla cassa, con il seme del mondo e la profondità da cui nascono i suoi nemici.</summary>
        public void Arm(ChestSecretKind kind, ulong runSeed, int depth)
        {
            _runSeed = runSeed;
            _depth = depth;
            if (_curseLook != null)
            {
                _curseLook.SetActive(kind == ChestSecretKind.Cursed);
            }
        }

        private void OnEnable()
        {
            Chest.Disturbed += HandleDisturbed;
        }

        private void OnDisable()
        {
            Chest.Disturbed -= HandleDisturbed;
        }

        private void HandleDisturbed(Chest chest)
        {
            if (chest.Secret == ChestSecretKind.Mimic)
            {
                WakeMimic();
            }
            else if (chest.Secret == ChestSecretKind.Cursed)
            {
                CallWave();
            }
        }

        // il mimic prende il posto del forziere, davanti a lui; alla sua morte la cassa si apre
        private void WakeMimic()
        {
            if (_mimicPrefab == null)
            {
                Chest.Open();
                return;
            }

            if (_model != null)
            {
                _model.SetActive(false);
            }

            Vector3 wanted = transform.position + transform.forward * 0.9f;
            Vector3 spot = NavMesh.SamplePosition(wanted, out var hit, 2.5f, NavMesh.AllAreas) ? hit.position : wanted;
            var mimic = Spawn(_mimicPrefab, spot, transform.rotation);
            mimic.Killed += HandleMimicKilled;
        }

        private void HandleMimicKilled(EnemyAI mimic)
        {
            mimic.Killed -= HandleMimicKilled;
            _alive.Remove(mimic);
            if (_model != null)
            {
                _model.SetActive(true);
            }

            // il mimic sparisce: la cassa vera si apre dove era, con il suo loot
            StartCoroutine(HideAndOpen(mimic));
        }

        private IEnumerator HideAndOpen(EnemyAI mimic)
        {
            yield return null;
            if (mimic != null)
            {
                mimic.gameObject.SetActive(false);
                Destroy(mimic.gameObject);
            }

            Chest.Open();
        }

        // tre-cinque nemici in un arco davanti al baule, sul NavMesh; a ondata battuta il baule si apre
        private void CallWave()
        {
            var cell = Chest.Cell;
            int size = SecretRules.WaveSize(_runSeed, _depth, cell.x, cell.y);
            int brutes = SecretRules.WaveBrutes(_depth, size);
            for (int i = 0; i < size; i++)
            {
                var prefab = i < brutes && _brutePrefab != null ? _brutePrefab : _skeletonPrefab;
                if (prefab == null)
                {
                    continue;
                }

                // un arco di 160° davanti alla cassa, da un lato all'altro
                float angle = size > 1 ? -80f + 160f * i / (size - 1) : 0f;
                Vector3 wanted = transform.position + Quaternion.Euler(0f, angle, 0f) * transform.forward * _waveRadius;
                Vector3 spot = NavMesh.SamplePosition(wanted, out var hit, 3f, NavMesh.AllAreas) ? hit.position : Chest.Front;
                Vector3 toward = Vector3.ProjectOnPlane(transform.position - spot, Vector3.up);
                var enemy = Spawn(prefab, spot, toward.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(toward) : transform.rotation);
                enemy.Killed += HandleWaveKilled;
            }

            if (_alive.Count == 0)
            {
                Chest.Open();
            }
        }

        private void HandleWaveKilled(EnemyAI enemy)
        {
            enemy.Killed -= HandleWaveKilled;
            _alive.Remove(enemy);
            if (_alive.Count == 0)
            {
                Chest.Open();
            }
        }

        private EnemyAI Spawn(EnemyAI prefab, Vector3 position, Quaternion rotation)
        {
            var enemy = Instantiate(prefab, position, rotation);
            _alive.Add(enemy);
            Spawned?.Invoke(enemy);
            return enemy;
        }
    }
}
