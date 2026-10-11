using System.Text;
using DarkDescent.Combat;
using DarkDescent.Enemies;
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
    /// attributo ne compra uno (D3 della M8). La probabilità di colpire dipende dall'Armatura del
    /// nemico: la riga ne mostra l'intervallo su tutti i tipi, il tooltip il valore contro ognuno.
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterPanel : MonoBehaviour
    {
        [Tooltip("La finestra da aprire e chiudere; il componente sta sul padre, sempre attivo.")]
        [SerializeField] private GameObject _window;

        [Tooltip("La colonna dei valori, allineata a quella dei nomi.")]
        [SerializeField] private TMP_Text _values;

        [Tooltip("Un prefab per tipo di nemico: la probabilità di colpire si mostra contro la sua Armatura, con il suo nome.")]
        [SerializeField] private EnemyAI[] _hitTargets;

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
            TextKeys.TipMana,
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

        // l'Armatura dei prefab non cambia: si legge una volta
        private float[] _targetArmor;
        private float _minTargetArmor;
        private float _maxTargetArmor;

        private const int HitChanceLine = 8;

        /// <summary>Un valore più alto della base per oggetti o effetti (D1 della M10): l'azzurro degli affissi, come in Diablo 1.</summary>
        public const string RaisedColor = ItemDescription.AffixColor;

        /// <summary>Un valore più basso della base: il rosso dei requisiti non soddisfatti.</summary>
        public const string LoweredColor = ItemDescription.UnmetColor;

        // il grigio della nota sotto la spiegazione
        private const string NoteColor = "#8C8577";

        public bool IsOpen => _window.activeSelf;

        /// <summary>La finestra si è aperta (true) o chiusa: il pulsante dei punti si nasconde a finestra aperta.</summary>
        public event System.Action<bool> OpenChanged;

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
            OpenChanged?.Invoke(_window.activeSelf);

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

        /// <summary>Danno minimo e massimo dell'arma attuale, con i suoi affissi, la Forza e il livello: quello che il pannello mostra.</summary>
        public (int min, int max) DamageRange()
        {
            // dall'oggetto e non da MeleeAttack: l'ordine tra il suo aggiornamento e questo non è garantito
            var item = _inventory.Equipment.Get(EquipSlot.Weapon);
            var (min, max) = item?.Definition is WeaponDefinition
                ? ItemStats.WeaponDamage(item)
                : (_inventory.Unarmed.MinDamage, _inventory.Unarmed.MaxDamage);

            float level = _stats.Sheet.Get(StatType.Level);
            return (CombatFormulas.Damage(min, _stats.Strength, level), CombatFormulas.Damage(max, _stats.Strength, level));
        }

        private void Awake()
        {
            _window.SetActive(false);
            _targetArmor = new float[_hitTargets.Length];
            _minTargetArmor = float.MaxValue;
            _maxTargetArmor = 0f;
            for (int i = 0; i < _hitTargets.Length; i++)
            {
                _targetArmor[i] = CharacterStats.ValueOf(_hitTargets[i].GetComponent<CharacterStats>(), StatType.Armor);
                _minTargetArmor = Mathf.Min(_minTargetArmor, _targetArmor[i]);
                _maxTargetArmor = Mathf.Max(_maxTargetArmor, _targetArmor[i]);
            }
        }

        /// <summary>La probabilità di colpire contro un'Armatura, con Destrezza e affissi del cavaliere, arrotondata come nel pannello.</summary>
        public int HitChanceAgainst(float armor)
        {
            return Mathf.RoundToInt(CombatFormulas.HitChance(_stats.Dexterity, armor, _stats.ToHit));
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

            // più Armatura, meno probabilità: il nemico più corazzato dà il minimo
            int lowestHit = HitChanceAgainst(_maxTargetArmor);
            int highestHit = HitChanceAgainst(_minTargetArmor);

            // l'ordine segue la colonna dei nomi nella scena
            _builder.Clear();
            AppendLine(0).Append('\n');
            AppendLine(1).Append('\n');
            AppendLine(2).Append('\n');
            AppendLine(3).Append("\n\n");
            AppendLine(5).Append('\n');
            AppendLine(6).Append('\n');
            _builder.Append(min).Append('–').Append(max).Append('\n');
            _builder.Append(lowestHit);
            if (highestHit != lowestHit)
            {
                _builder.Append('–').Append(highestHit);
            }

            _builder.Append("%\n");
            AppendLine(9).Append("%\n");
            _builder.Append(_block != null ? Mathf.RoundToInt(_block.BlockChance) : 0).Append("%\n");
            AppendLine(11);
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
            string text = "<b>" + title + "</b>\n" + _localizer.Get(key);
            if (_hoveredLine == HitChanceLine)
            {
                text += HitChanceList();
            }

            if (TryGetLine(_hoveredLine, out int value, out int baseValue) && value != baseValue)
            {
                text += "\n<size=85%><color=" + NoteColor + ">" + _localizer.Format(TextKeys.TipBonus, baseValue, value - baseValue) + "</color></size>";
            }

            _tooltip.ShowText(text, (RectTransform)_window.transform, LineWorldPosition(_hoveredLine));
        }

        /// <summary>
        /// Il valore di una riga e quello di base, senza oggetti né effetti, per le righe che si
        /// colorano (D1 della M10): i quattro attributi, Vita, Armatura, critico e Mana. Danno,
        /// colpire e blocco dipendono sempre dall'arma o dallo scudo, e restano bianchi.
        /// </summary>
        public bool TryGetLine(int line, out int value, out int baseValue)
        {
            var sheet = _stats.Sheet;
            float level = sheet.Get(StatType.Level);
            switch (line)
            {
                case 0: return Pair(sheet.Get(StatType.Strength), sheet.GetBase(StatType.Strength), out value, out baseValue);
                case 1: return Pair(sheet.Get(StatType.Dexterity), sheet.GetBase(StatType.Dexterity), out value, out baseValue);
                case 2: return Pair(sheet.Get(StatType.Magic), sheet.GetBase(StatType.Magic), out value, out baseValue);
                case 3: return Pair(sheet.Get(StatType.Vitality), sheet.GetBase(StatType.Vitality), out value, out baseValue);
                case 5:
                    return Pair(CombatFormulas.MaxLife(sheet.Get(StatType.Vitality)) + sheet.Get(StatType.Life),
                        CombatFormulas.MaxLife(sheet.GetBase(StatType.Vitality)) + sheet.GetBase(StatType.Life), out value, out baseValue);
                case 6: return Pair(sheet.Get(StatType.Armor), sheet.GetBase(StatType.Armor), out value, out baseValue);
                case 9:
                    return Pair(CombatFormulas.CritChance(sheet.Get(StatType.Dexterity), sheet.Get(StatType.CritChance)),
                        CombatFormulas.CritChance(sheet.GetBase(StatType.Dexterity), sheet.GetBase(StatType.CritChance)), out value, out baseValue);
                case 11:
                    return Pair(CombatFormulas.MaxMana(sheet.Get(StatType.Magic), level, sheet.Get(StatType.Mana)),
                        CombatFormulas.MaxMana(sheet.GetBase(StatType.Magic), level, sheet.GetBase(StatType.Mana)), out value, out baseValue);
                default:
                    value = baseValue = 0;
                    return false;
            }
        }

        private static bool Pair(float value, float baseValue, out int roundedValue, out int roundedBase)
        {
            roundedValue = Mathf.RoundToInt(value);
            roundedBase = Mathf.RoundToInt(baseValue);
            return true;
        }

        // il valore della riga, colorato se oggetti o effetti lo spostano dalla base
        private StringBuilder AppendLine(int line)
        {
            TryGetLine(line, out int value, out int baseValue);
            if (value == baseValue)
            {
                return _builder.Append(value);
            }

            return _builder.Append("<color=").Append(value > baseValue ? RaisedColor : LoweredColor).Append('>').Append(value).Append("</color>");
        }

        // una riga per tipo di nemico; si costruisce solo al passaggio del cursore
        private string HitChanceList()
        {
            var list = new StringBuilder();
            for (int i = 0; i < _hitTargets.Length; i++)
            {
                var archetype = _hitTargets[i].Archetype;
                string name = archetype != null ? _localizer.Get(archetype.NameKey) : _hitTargets[i].name;
                list.Append('\n').Append(name).Append(": ").Append(HitChanceAgainst(_targetArmor[i])).Append('%');
            }

            return list.ToString();
        }
    }
}
