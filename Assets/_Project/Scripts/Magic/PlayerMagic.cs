using System;
using System.Collections.Generic;
using DarkDescent.Combat;
using DarkDescent.Player;
using DarkDescent.Progression;
using DarkDescent.Stats;
using DarkDescent.Talents;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DarkDescent.Magic
{
    /// <summary>
    /// La magia del cavaliere (M9): mana, incantesimi noti e lancio. Il mana massimo segue Magia,
    /// livello e affissi dalla scheda delle statistiche; si riempie salendo di livello e tornando in
    /// vita. Il tasto destro, Q ed E usano l'abilità della loro casella verso il cursore e annunciano
    /// <see cref="Cast"/>: che cosa succede nel mondo lo decidono le abilità che ascoltano. Dal
    /// passo 11.7 della M11 c'è anche la magia del portale, con il suo tasto: sempre nota, apre il
    /// portale per l'accampamento dove chi lo apre lo permette. Dalla M20 le abilità le dà l'albero: a
    /// ogni *Applica* entrano le nuove ed escono quelle tolte. Con il 20.4b (D18) le caselle sono
    /// quattro, alla Diablo 3, e nella prima, del sinistro, c'è l'Attacco: il colpo dell'arma, che
    /// resta del controller e non passa da qui.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterStats), typeof(Health))]
    public class PlayerMagic : MonoBehaviour
    {
        [Tooltip("La magia del portale per l'accampamento, fuori dalla barra (passo 11.7 della M11).")]
        [SerializeField] private SpellDefinition _portalSpell;

        [Tooltip("L'Attacco, il colpo dell'arma: sempre noto, nella casella del sinistro (D18 della M20).")]
        [SerializeField] private SpellDefinition _attackSpell;

        [Tooltip("Il suono sordo di un lancio che non parte: niente mana, ricarica, incantesimo non noto.")]
        [SerializeField] private AudioClip _failClip;

        [Tooltip("Il suono di un'abilità imparata fuori dall'albero.")]
        [SerializeField] private AudioClip _learnClip;

        [SerializeField] private AudioSource _audio;

        private CharacterStats _stats;
        private Health _health;
        private PlayerProgress _progress;
        private PlayerTalents _talents;
        private PlayerInputReader _reader;
        private PlayerController _controller;
        private ManaPool _mana;
        private SpellBook _book;
        private SpellCaster _caster;
        private IPortalOpener _portalOpener;
        private IAbilityTargeting _targeting;
        private bool _subscribed;

        // le abilità che l'albero ha messo nella barra: le sole che toglie
        private readonly HashSet<SpellDefinition> _fromTree = new HashSet<SpellDefinition>();
        private readonly List<SpellDefinition> _dropped = new List<SpellDefinition>();

        // la pressione arriva da una callback dell'input, dove la UI risponde con il frame prima:
        // si esegue nel primo Update, come il click del movimento. -1 se non c'è niente
        private int _skillPending = -1;
        private bool _portalPending;

        /// <summary>Un incantesimo è partito verso un punto del mondo: il mana è già speso.</summary>
        public event Action<SpellDefinition, Vector3> Cast;

        /// <summary>Un lancio non è partito, e perché.</summary>
        public event Action<CastResult> CastFailed;

        /// <summary>La magia del portale non è partita, e perché: l'HUD lo dice per un momento.</summary>
        public event Action<PortalResult> PortalRefused;

        public SpellDefinition PortalSpell => _portalSpell;

        public SpellDefinition AttackSpell => _attackSpell;

        public ManaPool Mana => _mana ??= new ManaPool(MaxMana());

        /// <summary>Il mana che torna ogni secondo, in parte del massimo: quello della classe (D7 della M21).</summary>
        public float ManaRegenPerSecond { get; set; } = CombatFormulas.ManaRegenPerSecond;

        public SpellBook Book => _book ??= CreateBook();

        public SpellCaster Caster => _caster ??= CreateCaster();

        // l'albero può arrivare prima dell'Awake: chi si collega lo fa dal composition root
        private PlayerTalents Talents => _talents != null ? _talents : _talents = GetComponent<PlayerTalents>();

        private void Awake()
        {
            _stats = GetComponent<CharacterStats>();
            _health = GetComponent<Health>();
            _progress = GetComponent<PlayerProgress>();
            _talents = GetComponent<PlayerTalents>();
        }

        /// <summary>Un numero di un'abilità con i nodi dell'albero applicati (M20); senza albero, quello dell'asset.</summary>
        public float AbilityValue(SpellDefinition ability, AbilityParam param)
        {
            return Talents != null ? Talents.Build.AbilityValue(ability, param) : TalentBuild.BaseValue(ability, param);
        }

        /// <summary>Chi controlla il bersaglio delle abilità del Guerriero: senza, partono sempre.</summary>
        public void SetTargeting(IAbilityTargeting targeting)
        {
            _targeting = targeting;
        }

        private SpellBook CreateBook()
        {
            var book = new SpellBook();
            book.Learn(_attackSpell);
            return book;
        }

        /// <summary>
        /// Il colpo del sinistro della classe (M21): per il Guerriero l'Attacco, il colpo dell'arma; per il Mago il
        /// Dardo arcano, che parte da lontano dalla bacchetta o dal bastone. Prende il posto di quello di prima nella
        /// casella del sinistro; le abilità note e le altre caselle restano.
        /// </summary>
        public void SetAttackSpell(SpellDefinition attack)
        {
            if (attack == null || attack == _attackSpell && Book.Knows(attack))
            {
                return;
            }

            var previous = _attackSpell;
            _attackSpell = attack;
            if (previous != null && previous != attack)
            {
                Book.Forget(previous);
            }

            Book.Learn(attack);
            Book.Assign(SpellBook.Primary, attack);

            // il dardo parte dal colpo dell'arma: stessa scelta del bersaglio, stesso ritmo, ma da lontano
            var bolt = attack.Kind == SpellKind.ArcaneBolt;
            var spells = GetComponent<PlayerSpells>();
            if (spells != null)
            {
                spells.SetBolt(bolt ? attack : null);
            }

            if (TryGetComponent(out MeleeAttack melee))
            {
                melee.SetBolt(bolt ? spells : null, attack.Range, attack.Delay, attack.MagicDivisor);
            }
        }

        private SpellCaster CreateCaster()
        {
            var caster = new SpellCaster(Book, Mana);
            caster.SetNumbers(spell => AbilityValue(spell, AbilityParam.ManaCost), spell => AbilityValue(spell, AbilityParam.Cooldown));
            return caster;
        }

        /// <summary>Collega tasti e mira. Bind e OnEnable arrivano in ordine qualsiasi: si iscrive chi arriva per secondo.</summary>
        public void Bind(PlayerInputReader reader, PlayerController controller)
        {
            Unsubscribe();
            _reader = reader;
            _controller = controller;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        /// <summary>Chi apre il portale per l'accampamento; senza, la magia del portale non parte.</summary>
        public void SetPortalOpener(IPortalOpener opener)
        {
            _portalOpener = opener;
        }

        /// <summary>
        /// La magia del portale (T o un click sul suo posto): se chi lo apre lo permette e il mana basta,
        /// lo spende, annuncia <see cref="Cast"/> per gesto e suono, e apre il portale.
        /// </summary>
        public PortalResult TryCastPortal()
        {
            var result = CheckPortal();
            if (result == PortalResult.Opened)
            {
                Mana.TrySpend(_portalSpell.ManaCost);
                Cast?.Invoke(_portalSpell, transform.position);
                _portalOpener.OpenTownPortal();
                return result;
            }

            if (result != PortalResult.Unavailable && _audio != null && _failClip != null)
            {
                _audio.PlayOneShot(_failClip);
            }

            PortalRefused?.Invoke(result);
            return result;
        }

        private PortalResult CheckPortal()
        {
            if (_portalSpell == null || _portalOpener == null || _health.IsDead)
            {
                return PortalResult.Unavailable;
            }

            var where = _portalOpener.CanOpenTownPortal();
            if (where != PortalResult.Opened)
            {
                return where;
            }

            return Mana.Current >= _portalSpell.ManaCost ? PortalResult.Opened : PortalResult.NoMana;
        }

        /// <summary>Mette un'abilità nota in una casella: il menu delle abilità. False se non ci può stare.</summary>
        public bool Assign(int slot, SpellDefinition spell) => Book.Assign(slot, spell);

        /// <summary>
        /// Dà un incantesimo fuori dall'albero e lo mette in una casella: la prima libera, o quella del
        /// destro; false se era già noto. Dalla M20 il Dardo e l'Anello di fuoco non sono del Guerriero:
        /// i test li danno con questo, come un libro di prova (D3), finché il Mago della M21 non li
        /// riporta in un albero suo.
        /// </summary>
        public bool Learn(SpellDefinition spell)
        {
            if (!Book.Learn(spell))
            {
                return false;
            }

            if (Book.SlotOf(spell) < 0)
            {
                Book.Assign(SpellBook.Secondary, spell);
            }
            if (_audio != null && _learnClip != null)
            {
                _audio.PlayOneShot(_learnClip);
            }

            return true;
        }

        /// <summary>Usa un'abilità verso <paramref name="target"/>, se è in una casella: i test.</summary>
        public CastResult TryCastAt(SpellDefinition spell, Vector3 target) => TryCastAt(Book.SlotOf(spell), target);

        /// <summary>
        /// Usa l'abilità di una casella verso <paramref name="target"/>: il destro, Q, E, o un test. La
        /// casella del sinistro non passa da qui: l'Attacco è il colpo del controller.
        /// </summary>
        public CastResult TryCastAt(int slot, Vector3 target)
        {
            if (_health.IsDead || slot <= SpellBook.Primary)
            {
                return CastResult.NotKnown;
            }

            // un'abilità del Guerriero senza niente da colpire non spende il mana (M20)
            var result = Caster.Check(slot);
            if (result == CastResult.Ready && _targeting != null && Book[slot].IsWarriorAbility)
            {
                result = _targeting.CanUse(Book[slot], target);
            }

            if (result == CastResult.Ready)
            {
                result = Caster.TryCast(slot);
            }

            if (result == CastResult.Ready)
            {
                Cast?.Invoke(Book[slot], target);
            }
            else
            {
                if (_audio != null && _failClip != null)
                {
                    _audio.PlayOneShot(_failClip);
                }

                CastFailed?.Invoke(result);
            }

            return result;
        }

        private void OnEnable()
        {
            if (_talents != null)
            {
                _talents.Build.Applied += SyncWithTree;
                SyncWithTree();
            }

            _stats.Sheet.Changed += RefreshMax;
            _health.Revived += Mana.Fill;
            if (_progress != null)
            {
                _progress.LevelUpShown += HandleLevelUp;
            }

            Subscribe();
        }

        private void OnDisable()
        {
            if (_talents != null)
            {
                _talents.Build.Applied -= SyncWithTree;
            }

            _stats.Sheet.Changed -= RefreshMax;
            _health.Revived -= Mana.Fill;
            if (_progress != null)
            {
                _progress.LevelUpShown -= HandleLevelUp;
            }

            Unsubscribe();
            _skillPending = -1;
            _portalPending = false;
        }

        private void Subscribe()
        {
            if (_subscribed || _reader == null)
            {
                return;
            }

            _reader.SkillPressed += HandleSkillPressed;
            _reader.PortalPressed += HandlePortalPressed;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _reader.SkillPressed -= HandleSkillPressed;
            _reader.PortalPressed -= HandlePortalPressed;
            _subscribed = false;
        }

        private void Update()
        {
            if (_health.IsDead)
            {
                _skillPending = -1;
                _portalPending = false;
                return;
            }

            float deltaTime = Time.deltaTime;
            Mana.Regenerate(deltaTime, ManaRegenPerSecond);
            Caster.Advance(deltaTime);

            if (_portalPending)
            {
                _portalPending = false;
                TryCastPortal();
            }

            if (_skillPending >= 0)
            {
                int slot = _skillPending;
                _skillPending = -1;

                // il destro sopra una finestra è un click della finestra; Q ed E valgono sempre
                bool overUI = slot == SpellBook.Secondary && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                if (!overUI && _controller != null)
                {
                    // l'anello e la pelle di pietra partono dal cavaliere: il punto serve solo al dardo
                    if (_controller.TryGetCursorPoint(out Vector3 target))
                    {
                        TryCastAt(slot, target);
                    }
                    else if (Book[slot] != null && !Book[slot].NeedsPoint)
                    {
                        TryCastAt(slot, transform.position);
                    }
                }
            }
        }

        private void HandleSkillPressed(int slot)
        {
            _skillPending = slot;
        }

        private void HandlePortalPressed()
        {
            _portalPending = true;
        }

        // salendo di livello il mana si riempie come la vita (D2): il massimo è già salito con la scheda
        private void HandleLevelUp(int level)
        {
            Mana.Fill();
        }

        /// <summary>
        /// Le abilità note seguono l'albero applicato: via quelle tolte, dentro le nuove, nella prima
        /// casella libera se c'è. Da spento non sente *Applica*: il salvataggio la chiama prima di
        /// rimettere le caselle.
        /// </summary>
        public void SyncWithTree()
        {
            if (_talents == null)
            {
                return;
            }

            var abilities = _talents.Build.Abilities();
            _dropped.Clear();
            foreach (var spell in _fromTree)
            {
                if (!abilities.Contains(spell))
                {
                    _dropped.Add(spell);
                }
            }

            foreach (var spell in _dropped)
            {
                _fromTree.Remove(spell);
                Book.Forget(spell);
            }

            // una già nota resta dov'è, anche fuori dalle caselle
            foreach (var spell in abilities)
            {
                _fromTree.Add(spell);
                Book.Learn(spell);
            }
        }

        private void RefreshMax()
        {
            Mana.SetMax(MaxMana());
        }

        private float MaxMana()
        {
            var sheet = _stats != null ? _stats.Sheet : GetComponent<CharacterStats>().Sheet;
            return CombatFormulas.MaxMana(sheet.Get(StatType.Magic), sheet.Get(StatType.Level), sheet.Get(StatType.Mana));
        }
    }
}
