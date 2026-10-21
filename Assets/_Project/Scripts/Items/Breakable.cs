using System;
using System.Collections;
using System.Collections.Generic;
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
    /// collega il CompositionRoot. Dalla profondità 4 uno su cinque dei barili dei gruppi è esplosivo
    /// (D8): cerchiato di rosso, rotto accende la miccia e dopo un secondo esplode, ferisce tutti nel
    /// raggio, nemici compresi, e rompe i barili vicini.
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

        [Header("Barile esplosivo (D8 della M13)")]
        [Tooltip("Può essere esplosivo: i barili dei gruppi sì, quello singolo contro il muro no.")]
        [SerializeField] private bool _canExplode;

        [Tooltip("Il modello del barile esplosivo, al posto di quello intero.")]
        [SerializeField] private GameObject _explosiveModel;

        [SerializeField] private ParticleSystem _fuseSparks;

        [SerializeField] private ParticleSystem _explosion;

        [Tooltip("Il lampo dell'esplosione: si accende e si spegne in fretta.")]
        [SerializeField] private Light _flash;

        [SerializeField] private AudioClip _fuseClip;

        [SerializeField] private AudioClip _explosionClip;

        [SerializeField, Min(0f)] private float _fuseDuration = 1f;

        [SerializeField, Min(0.5f)] private float _explosionRadius = 3f;

        [SerializeField, Min(0.05f)] private float _flashDuration = 0.4f;

        // riusati a ogni esplosione: niente liste nuove
        private static readonly Collider[] Hits = new Collider[64];
        private readonly HashSet<Health> _hurt = new HashSet<Health>();
        private float _flashIntensity;

        private LootRoller _roller;
        private int _depth = 1;
        private int _cellX;
        private int _cellZ;

        /// <summary>Il barile si è rotto, con un colpo o nei test; quello esplosivo quando si accende la miccia.</summary>
        public event Action<Breakable> Broken;

        /// <summary>Il barile esplosivo è esploso, dopo la miccia.</summary>
        public event Action<Breakable> Exploded;

        /// <summary>Esplosivo in questo livello: deciso a <see cref="Bind"/> dal seme della sua cella.</summary>
        public bool IsExplosive { get; private set; }

        public bool CanExplode => _canExplode;

        public float ExplosionRadius => _explosionRadius;

        public bool IsBroken { get; private set; }

        /// <summary>
        /// La miccia è accesa e il barile non è ancora esploso: lasciando il livello adesso il ricordo lo
        /// tiene intero, perché né il danno né il loot sono arrivati.
        /// </summary>
        public bool IsFusing { get; private set; }

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
            IsExplosive = _canExplode && roller != null && TrapRules.IsExplosiveBarrel(roller.RunSeed, depth, _cellX, _cellZ);
            if (!IsBroken)
            {
                ShowIntact();
            }
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
            if (IsExplosive)
            {
                // la miccia: niente più click, il barile resta intero finché non esplode
                if (_hitArea != null)
                {
                    _hitArea.enabled = false;
                }

                IsFusing = true;
                Broken?.Invoke(this);
                StartCoroutine(Fuse(source));
                return;
            }

            Shatter(source);
            Broken?.Invoke(this);
        }

        // i pezzi a terra, le schegge, il suono e il loot
        private void Shatter(GameObject source)
        {
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
        }

        private IEnumerator Fuse(GameObject source)
        {
            if (_fuseSparks != null)
            {
                _fuseSparks.Play();
            }

            if (_audio != null && _fuseClip != null)
            {
                _audio.PlayOneShot(_fuseClip);
            }

            yield return new WaitForSeconds(_fuseDuration);
            if (_fuseSparks != null)
            {
                _fuseSparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            IsFusing = false;
            Shatter(source);
            if (_explosion != null)
            {
                _explosion.Play();
            }

            if (_audio != null && _explosionClip != null)
            {
                _audio.PlayOneShot(_explosionClip);
            }

            Explode();
            Exploded?.Invoke(this);
            if (_flash != null)
            {
                yield return Flash();
            }
        }

        // tutti nel raggio, una volta sola anche con più collider: il cavaliere, i nemici e gli altri barili
        private void Explode()
        {
            Vector3 center = transform.position + Vector3.up * 0.8f;
            int count = Physics.OverlapSphereNonAlloc(center, _explosionRadius, Hits, ~0, QueryTriggerInteraction.Ignore);
            _hurt.Clear();
            float damage = TrapRules.ExplosionDamage(_depth);
            for (int i = 0; i < count; i++)
            {
                var health = Hits[i].GetComponentInParent<Health>();
                if (health != null && !health.IsDead && _hurt.Add(health))
                {
                    health.TakeDamage(new DamageInfo(damage, DamageType.Fire, gameObject));
                }

                var barrel = Hits[i].GetComponentInParent<Breakable>();
                if (barrel != null && barrel != this)
                {
                    barrel.Break(gameObject);
                }
            }

            _hurt.Clear();
        }

        private IEnumerator Flash()
        {
            _flash.enabled = true;
            for (float t = 0f; t < _flashDuration; t += Time.deltaTime)
            {
                _flash.intensity = _flashIntensity * (1f - t / _flashDuration);
                yield return null;
            }

            _flash.enabled = false;
        }

        private void ShowIntact()
        {
            if (_intact != null)
            {
                _intact.SetActive(!IsExplosive);
            }

            if (_explosiveModel != null)
            {
                _explosiveModel.SetActive(IsExplosive);
            }
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

            if (_explosiveModel != null)
            {
                _explosiveModel.SetActive(false);
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
            if (_flash != null)
            {
                _flashIntensity = _flash.intensity;
                _flash.enabled = false;
            }

            _cellX = Mathf.RoundToInt(transform.position.x * CellsPerMeter);
            _cellZ = Mathf.RoundToInt(transform.position.z * CellsPerMeter);
        }
    }
}
