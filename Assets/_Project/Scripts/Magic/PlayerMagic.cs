using System;
using DarkDescent.Combat;
using DarkDescent.Player;
using DarkDescent.Progression;
using DarkDescent.Stats;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DarkDescent.Magic
{
    /// <summary>
    /// La magia del cavaliere (M9): mana, incantesimi noti e lancio. Il mana massimo segue Magia,
    /// livello e affissi dalla scheda delle statistiche; si riempie salendo di livello e tornando in
    /// vita. Il tasto destro lancia l'incantesimo scelto verso il cursore e annuncia
    /// <see cref="Cast"/>: che cosa succede nel mondo lo decidono gli incantesimi che ascoltano.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterStats), typeof(Health))]
    public class PlayerMagic : MonoBehaviour
    {
        [Tooltip("Gli incantesimi del gioco, nell'ordine dei posti F1, F2, F3.")]
        [SerializeField] private SpellDefinition[] _spells;

        [Tooltip("Il suono sordo di un lancio che non parte: niente mana, ricarica, incantesimo non noto.")]
        [SerializeField] private AudioClip _failClip;

        [Tooltip("Il suono di un incantesimo imparato da una pergamena.")]
        [SerializeField] private AudioClip _learnClip;

        [SerializeField] private AudioSource _audio;

        private CharacterStats _stats;
        private Health _health;
        private PlayerProgress _progress;
        private PlayerInputReader _reader;
        private PlayerController _controller;
        private ManaPool _mana;
        private SpellBook _book;
        private SpellCaster _caster;
        private bool _subscribed;

        // la pressione arriva da una callback dell'input, dove la UI risponde con il frame prima:
        // si esegue nel primo Update, come il click del movimento
        private bool _castPending;

        /// <summary>Un incantesimo è partito verso un punto del mondo: il mana è già speso.</summary>
        public event Action<SpellDefinition, Vector3> Cast;

        /// <summary>Un lancio non è partito, e perché.</summary>
        public event Action<CastResult> CastFailed;

        public ManaPool Mana => _mana ??= new ManaPool(MaxMana());

        public SpellBook Book => _book ??= new SpellBook(_spells);

        public SpellCaster Caster => _caster ??= new SpellCaster(Book, Mana);

        private void Awake()
        {
            _stats = GetComponent<CharacterStats>();
            _health = GetComponent<Health>();
            _progress = GetComponent<PlayerProgress>();
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

        /// <summary>Sceglie il posto da lanciare (F1–F3 o un click sulla barra); false se non è noto.</summary>
        public bool Select(int slot) => Book.TrySelect(slot);

        /// <summary>Impara un incantesimo da una pergamena e lo sceglie; false se era già noto.</summary>
        public bool Learn(SpellDefinition spell)
        {
            if (!Book.Learn(spell))
            {
                return false;
            }

            Book.TrySelect(Book.SlotOf(spell));
            if (_audio != null && _learnClip != null)
            {
                _audio.PlayOneShot(_learnClip);
            }

            return true;
        }

        /// <summary>Lancia l'incantesimo scelto verso <paramref name="target"/>: il tasto destro, o un test.</summary>
        public CastResult TryCastAt(Vector3 target)
        {
            if (_health.IsDead)
            {
                return CastResult.NotKnown;
            }

            int slot = Book.Selected;
            var result = slot >= 0 ? Caster.TryCast(slot) : CastResult.NotKnown;
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
            _stats.Sheet.Changed -= RefreshMax;
            _health.Revived -= Mana.Fill;
            if (_progress != null)
            {
                _progress.LevelUpShown -= HandleLevelUp;
            }

            Unsubscribe();
            _castPending = false;
        }

        private void Subscribe()
        {
            if (_subscribed || _reader == null)
            {
                return;
            }

            _reader.CastPressed += HandleCastPressed;
            _reader.SpellSelected += HandleSpellSelected;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _reader.CastPressed -= HandleCastPressed;
            _reader.SpellSelected -= HandleSpellSelected;
            _subscribed = false;
        }

        private void Update()
        {
            if (_health.IsDead)
            {
                _castPending = false;
                return;
            }

            float deltaTime = Time.deltaTime;
            Mana.Regenerate(deltaTime, CombatFormulas.ManaRegenPerSecond);
            Caster.Advance(deltaTime);

            if (_castPending)
            {
                _castPending = false;
                bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                if (!overUI && _controller != null)
                {
                    // l'anello e la pelle di pietra partono dal cavaliere: il punto serve solo al dardo
                    if (_controller.TryGetCursorPoint(out Vector3 target))
                    {
                        TryCastAt(target);
                    }
                    else if (Book.SelectedSpell != null && Book.SelectedSpell.Kind != SpellKind.Projectile)
                    {
                        TryCastAt(transform.position);
                    }
                }
            }
        }

        private void HandleCastPressed()
        {
            _castPending = true;
        }

        private void HandleSpellSelected(int slot)
        {
            Select(slot);
        }

        // salendo di livello il mana si riempie come la vita (D2): il massimo è già salito con la scheda
        private void HandleLevelUp(int level)
        {
            Mana.Fill();
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
