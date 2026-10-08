using System.Text;
using DarkDescent.Combat;
using DarkDescent.Items;
using DarkDescent.Localization;
using DarkDescent.Player;
using DarkDescent.Progression;
using DarkDescent.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkDescent.UI
{
    /// <summary>
    /// Il pannello del personaggio, nella metà sinistra: attributi e valori derivati, calcolati con
    /// le stesse formule del combattimento. Si aggiorna quando cambiano statistiche o arma, mai in Update.
    /// Passando su una riga, un tooltip a destra della finestra dice a cosa serve (D16 della M6).
    /// In alto livello, esperienza e punti da spendere; con punti da spendere, un "+" accanto a ogni
    /// attributo ne compra uno (D3 della M8).
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterPanel : MonoBehaviour
    {
        [Tooltip("La finestra da aprire e chiudere; il componente sta sul padre, sempre attivo.")]
        [SerializeField] private GameObject _window;

        [Tooltip("La colonna dei valori, allineata a quella dei nomi.")]
        [SerializeField] private TMP_Text _values;

        [Tooltip("L'Armatura contro cui si mostra la probabilità di colpire: quella dello scheletro.")]
        [SerializeField, Min(0f)] private float _referenceArmor = 10f;

        [Tooltip("La colonna dei nomi: le sue righe dicono dove mettere il tooltip.")]
        [SerializeField] private TMP_Text _names;

        [Tooltip("Il riquadro delle spiegazioni: uno suo, così l'inventario aperto non lo spegne.")]
        [SerializeField] private ItemTooltip _tooltip;

        [Tooltip("Livello, esperienza e punti da spendere, sopra le colonne.")]
        [SerializeField] private TMP_Text _header;

        [Tooltip("I + di Forza, Destrezza, Magia e Vitalità, in quest'ordine: accanto alle prime quattro righe.")]
        [SerializeField] private Button[] _raiseButtons = new Button[4];

        /// <summary>Gli attributi dei "+", nell'ordine dei bottoni e delle prime righe.</summary>
        public static readonly StatType[] RaisedStats = { StatType.Strength, StatType.Dexterity, StatType.Magic, StatType.Vitality };

        /// <summary>
        /// La spiegazione di ogni riga, nell'ordine della colonna dei nomi (hud.stat_names); null per
        /// la riga vuota tra attributi e valori derivati.
        /// </summary>
        public static readonly string[] LineTips =
        {
            TextKeys.TipStrength, TextKeys.TipDexterity, TextKeys.TipMagic, TextKeys.TipVitality, null,
            TextKeys.TipLife, TextKeys.TipArmor, TextKeys.TipDamage, TextKeys.TipHitChance, TextKeys.TipCritical, TextKeys.TipBlock,
        };

        private readonly StringBuilder _builder = new StringBuilder(128);
        private CharacterStats _stats;
        private PlayerInventory _inventory;
        private CharacterProgress _progress;
        private PlayerInputReader _reader;
        private ShieldBlock _block;
        private Localizer _localizer;
        private int _hoveredLine = -1;
        private bool _subscribed;

        public bool IsOpen => _window.activeSelf;

        public string ValuesText => _values.text;

        public ItemTooltip Tooltip => _tooltip;

        public string HeaderText => _header.text;

        /// <summary>Il "+" di un attributo, per i test: acceso solo con punti da spendere.</summary>
        public Button RaiseButton(StatType stat) => _raiseButtons[System.Array.IndexOf(RaisedStats, stat)];

        public void Bind(CharacterStats stats, PlayerInventory inventory, CharacterProgress progress, PlayerInputReader reader, Localizer localizer)
        {
            Unsubscribe();
            _stats = stats;
            _inventory = inventory;
            _progress = progress;
            _block = stats.GetComponent<ShieldBlock>();
            _reader = reader;
            _localizer = localizer;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        public void Toggle()
        {
            _window.SetActive(!_window.activeSelf);

            // i "+" vanno accanto alle righe, che si misurano solo a finestra accesa
            if (_window.activeSelf && _subscribed)
            {
                Refresh();
            }

            // una finestra spenta non riceve l'uscita del cursore: il tooltip si dimentica qui
            _hoveredLine = -1;
            RefreshTooltip();
        }

        /// <summary>Il cursore è su una riga della colonna dei nomi, da 0; -1 se è uscito.</summary>
        public void HoverLine(int line)
        {
            if (line == _hoveredLine)
            {
                return;
            }

            _hoveredLine = line;
            RefreshTooltip();
        }

        /// <summary>Il centro di una riga, nello spazio del mondo della UI: in Overlay sono pixel dello schermo.</summary>
        public Vector3 LineWorldPosition(int line)
        {
            _names.ForceMeshUpdate();
            var info = _names.textInfo.lineInfo[line];
            return _names.transform.TransformPoint(new Vector3(_names.rectTransform.rect.center.x, (info.ascender + info.descender) / 2f, 0f));
        }

        /// <summary>Danno minimo e massimo dell'arma attuale, con i suoi affissi e la Forza: quello che il pannello mostra.</summary>
        public (int min, int max) DamageRange()
        {
            // dall'oggetto e non da MeleeAttack: l'ordine tra il suo aggiornamento e questo non è garantito
            var item = _inventory.Equipment.Get(EquipSlot.Weapon);
            var (min, max) = item?.Definition is WeaponDefinition
                ? ItemStats.WeaponDamage(item)
                : (_inventory.Unarmed.MinDamage, _inventory.Unarmed.MaxDamage);

            float multiplier = CombatFormulas.StrengthMultiplier(_stats.Strength);
            return (Mathf.RoundToInt(min * multiplier), Mathf.RoundToInt(max * multiplier));
        }

        private void Awake()
        {
            _window.SetActive(false);
        }

        private void OnEnable()
        {
            _raiseButtons[0].onClick.AddListener(RaiseStrength);
            _raiseButtons[1].onClick.AddListener(RaiseDexterity);
            _raiseButtons[2].onClick.AddListener(RaiseMagic);
            _raiseButtons[3].onClick.AddListener(RaiseVitality);
            Subscribe();
        }

        private void OnDisable()
        {
            _raiseButtons[0].onClick.RemoveListener(RaiseStrength);
            _raiseButtons[1].onClick.RemoveListener(RaiseDexterity);
            _raiseButtons[2].onClick.RemoveListener(RaiseMagic);
            _raiseButtons[3].onClick.RemoveListener(RaiseVitality);
            Unsubscribe();
        }

        private void RaiseStrength() => _progress.TrySpend(StatType.Strength);

        private void RaiseDexterity() => _progress.TrySpend(StatType.Dexterity);

        private void RaiseMagic() => _progress.TrySpend(StatType.Magic);

        private void RaiseVitality() => _progress.TrySpend(StatType.Vitality);

        private void Subscribe()
        {
            if (_subscribed || _stats == null)
            {
                return;
            }

            _reader.CharacterToggled += Toggle;
            _localizer.LanguageChanged += RefreshTooltip;
            _localizer.LanguageChanged += Refresh;
            _progress.Changed += Refresh;
            _stats.Sheet.Changed += Refresh;
            _inventory.Equipment.Changed += HandleEquipmentChanged;
            if (_block != null)
            {
                _block.ShieldChanged += Refresh;
            }

            _subscribed = true;
            Refresh();
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _reader.CharacterToggled -= Toggle;
            _localizer.LanguageChanged -= RefreshTooltip;
            _localizer.LanguageChanged -= Refresh;
            _progress.Changed -= Refresh;
            _stats.Sheet.Changed -= Refresh;
            _inventory.Equipment.Changed -= HandleEquipmentChanged;
            if (_block != null)
            {
                _block.ShieldChanged -= Refresh;
            }

            _subscribed = false;
        }

        // l'arma cambia il danno senza toccare le statistiche
        private void HandleEquipmentChanged(EquipSlot slot)
        {
            Refresh();
        }

        private void Refresh()
        {
            var (min, max) = DamageRange();
            float hitChance = CombatFormulas.HitChance(_stats.Dexterity, _referenceArmor, _stats.ToHit);

            // l'ordine segue la colonna dei nomi nella scena
            _builder.Clear();
            _builder.Append(Mathf.RoundToInt(_stats.Strength)).Append('\n');
            _builder.Append(Mathf.RoundToInt(_stats.Dexterity)).Append('\n');
            _builder.Append(Mathf.RoundToInt(_stats.Magic)).Append('\n');
            _builder.Append(Mathf.RoundToInt(_stats.Vitality)).Append("\n\n");
            _builder.Append(Mathf.RoundToInt(CombatFormulas.MaxLife(_stats.Vitality) + _stats.Life)).Append('\n');
            _builder.Append(Mathf.RoundToInt(_stats.Armor)).Append('\n');
            _builder.Append(min).Append('–').Append(max).Append('\n');
            _builder.Append(Mathf.RoundToInt(hitChance)).Append("%\n");
            _builder.Append(Mathf.RoundToInt(CombatFormulas.CritChance(_stats.Dexterity))).Append("%\n");
            _builder.Append(_block != null ? Mathf.RoundToInt(_block.BlockChance) : 0).Append('%');
            _values.SetText(_builder);
            RefreshProgress();
        }

        private void RefreshProgress()
        {
            int next = _progress.ExperienceToNext;
            string header = next > 0
                ? _localizer.Format(TextKeys.CharacterLevel, _progress.Level, _progress.Experience, next)
                : _localizer.Format(TextKeys.ExperienceMaxLevel, _progress.Level);
            bool canSpend = _progress.UnspentPoints > 0;
            if (canSpend)
            {
                header += "\n<color=#E0C060>" + _localizer.Format(TextKeys.CharacterPoints, _progress.UnspentPoints) + "</color>";
            }

            _header.text = header;

            // a finestra chiusa le righe non si misurano: ci pensa Toggle all'apertura
            bool place = canSpend && IsOpen;
            for (int i = 0; i < _raiseButtons.Length; i++)
            {
                var button = _raiseButtons[i].gameObject;
                button.SetActive(canSpend);
                if (place)
                {
                    var rect = (RectTransform)button.transform;
                    Vector3 line = LineWorldPosition(i);
                    rect.position = new Vector3(rect.position.x, line.y, rect.position.z);
                }
            }
        }

        // titolo e spiegazione dalla tabella: la colonna dei nomi si traduce da sé, e al cambio di
        // lingua può arrivare dopo questo metodo
        private void RefreshTooltip()
        {
            string key = _hoveredLine >= 0 && _hoveredLine < LineTips.Length ? LineTips[_hoveredLine] : null;
            if (!IsOpen || key == null || _localizer == null)
            {
                _tooltip.Hide();
                return;
            }

            string[] names = _localizer.Get(TextKeys.StatNames).Split('\n');
            string title = _hoveredLine < names.Length ? names[_hoveredLine] : string.Empty;
            _tooltip.ShowText("<b>" + title + "</b>\n" + _localizer.Get(key), (RectTransform)_window.transform, LineWorldPosition(_hoveredLine));
        }
    }
}
