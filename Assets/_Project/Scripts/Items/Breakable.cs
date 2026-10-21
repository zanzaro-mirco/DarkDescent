using System;
using DarkDescent.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Items
{
    /// <summary>
    /// Un barile da rompere (D5 della M13), come in Diablo 1: si clicca come un nemico e si rompe al
    /// primo colpo che arriva, anche a uno mancato, senza esperienza né barra in alto. Rompendosi mostra
    /// i pezzi, spegne il collider e l'ostacolo del NavMesh, suona e a volte lascia oro, un oggetto o
    /// una pozione dalla sua loot table, con i semi del barile (<c>SeedMixer.ForBarrel</c>): lo stesso
    /// barile lascia sempre le stesse cose. Una volta sola; rotto, entra nei ricordi del livello. Lo
    /// collega il CompositionRoot.
    /// </summary>
    [DisallowMultipleComponent]
    public class Breakable : MonoBehaviour, IDamageable
    {
        // la cella in decimetri, come la cassa: due barili dello stesso gruppo hanno semi diversi
        private const float CellsPerMeter = 10f;

        [SerializeField] private LootTable _lootTable;

        [SerializeField] private GroundItem _groundItemPrefab;

        [Tooltip("Il modello intero: si spegne alla rottura.")]
        [SerializeField] private GameObject _intact;

        [Tooltip("I pezzi a terra: si accendono alla rottura.")]
        [SerializeField] private GameObject _broken;

        [Tooltip("Il collider del click e dei colpi: si spegne alla rottura.")]
        [SerializeField] private Collider _hitArea;

        [Tooltip("Ritaglia il NavMesh attorno al barile; rotto, si spegne e il passaggio si apre.")]
        [SerializeField] private NavMeshObstacle _obstacle;

        [Tooltip("Le schegge che volano alla rottura.")]
        [SerializeField] private ParticleSystem _burst;

        [SerializeField] private AudioSource _audio;

        [Tooltip("Le varianti del suono, scelte a caso a ogni rottura (D14 della M13).")]
        [SerializeField] private AudioClip[] _breakClips;

        [Tooltip("Quanto lontano dal centro del barile cade l'oggetto, verso chi l'ha rotto.")]
        [SerializeField, Min(0f)] private float _dropDistance = 1.1f;

        [Tooltip("Quanto di lato cadono la pozione e l'oro.")]
        [SerializeField, Min(0f)] private float _sideOffset = 0.7f;

        [Tooltip("Da che altezza partono gli oggetti in volo.")]
        [SerializeField, Min(0f)] private float _dropHeight = 0.6f;

        private LootRoller _roller;
        private int _depth = 1;
        private int _cellX;
        private int _cellZ;

        /// <summary>Il barile si è rotto, con un colpo o nei test.</summary>
        public event Action<Breakable> Broken;

        public bool IsBroken { get; private set; }

        /// <summary>Per <see cref="IDamageable"/>: un barile rotto non si colpisce più.</summary>
        public bool IsDead => IsBroken;

        public LootTable LootTable => _lootTable;

        /// <summary>Il raggio del barile a terra, quello dell'ostacolo: chi lo colpisce si ferma sul bordo.</summary>
        public float Radius
        {
            get
            {
                if (_obstacle == null)
                {
                    return 0f;
                }

                Vector3 scale = _obstacle.transform.lossyScale;
                return _obstacle.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            }
        }

        public void Bind(LootRoller roller, int depth)
        {
            _roller = roller;
            _depth = depth;
        }

        /// <summary>L'oggetto che lascerà rompendosi, senza cambiare niente; null se non ne lascia.</summary>
        public ItemInstance Preview()
        {
            return _roller?.RollBarrel(_lootTable, _depth, _cellX, _cellZ);
        }

        /// <summary>La pozione che lascerà rompendosi, senza cambiare niente; null se non ne lascia.</summary>
        public ItemInstance PreviewPotion()
        {
            return _roller?.RollBarrelPotion(_lootTable, _depth, _cellX, _cellZ);
        }

        /// <summary>L'oro che lascerà rompendosi, senza cambiare niente; 0 se non ne lascia.</summary>
        public int PreviewGold()
        {
            return _roller?.RollBarrelGold(_lootTable, _depth, _cellX, _cellZ) ?? 0;
        }

        public void TakeDamage(in DamageInfo info)
        {
            Break(info.Source);
        }

        /// <summary>Un barile non schiva: anche il colpo mancato lo rompe.</summary>
        public void Evade(in DamageInfo info)
        {
            Break(info.Source);
        }

        /// <summary>
        /// Rotto in una visita di prima: i pezzi a terra, niente click e niente loot. Quello che aveva
        /// lasciato è tra gli oggetti a terra del ricordo del livello.
        /// </summary>
        public void RestoreBroken()
        {
            IsBroken = true;
            ShowBroken();
        }

        /// <summary>
        /// Si rompe e lascia il suo loot verso <paramref name="source"/>, chi l'ha colpito: dalla parte
        /// della stanza, mai dentro il muro dietro. Senza una sorgente cade davanti al barile.
        /// </summary>
        public void Break(GameObject source)
        {
            if (IsBroken)
            {
                return;
            }

            IsBroken = true;
            ShowBroken();
            if (_burst != null)
            {
                _burst.Play();
            }

            if (_audio != null && _breakClips.Length > 0)
            {
                _audio.PlayOneShot(_breakClips[UnityEngine.Random.Range(0, _breakClips.Length)]);
            }

            DropLoot(source);
            Broken?.Invoke(this);
        }

        private void DropLoot(GameObject source)
        {
            if (_groundItemPrefab == null)
            {
                return;
            }

            Vector3 toward = source != null ? source.transform.position - transform.position : transform.forward;
            toward.y = 0f;
            toward = toward.sqrMagnitude > 0.0001f ? toward.normalized : Vector3.forward;
            Vector3 side = Vector3.Cross(Vector3.up, toward);
            Vector3 front = transform.position + toward * _dropDistance;
            Vector3 from = transform.position + Vector3.up * _dropHeight;

            var item = Preview();
            if (item != null)
            {
                GroundItem.Spawn(_groundItemPrefab, item, front, from);
            }

            var potion = PreviewPotion();
            if (potion != null)
            {
                GroundItem.Spawn(_groundItemPrefab, potion, front + side * _sideOffset, from);
            }

            int gold = PreviewGold();
            if (gold > 0)
            {
                GroundItem.SpawnGold(_groundItemPrefab, _lootTable.Gold, gold, front - side * _sideOffset, from);
            }
        }

        private void ShowBroken()
        {
            if (_intact != null)
            {
                _intact.SetActive(false);
            }

            if (_broken != null)
            {
                _broken.SetActive(true);
            }

            if (_hitArea != null)
            {
                _hitArea.enabled = false;
            }

            if (_obstacle != null)
            {
                _obstacle.enabled = false;
            }
        }

        private void Awake()
        {
            _cellX = Mathf.RoundToInt(transform.position.x * CellsPerMeter);
            _cellZ = Mathf.RoundToInt(transform.position.z * CellsPerMeter);
        }
    }
}
