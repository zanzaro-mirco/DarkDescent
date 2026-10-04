using System.Text;
using DarkDescent.Combat;
using DarkDescent.Items;
using DarkDescent.Player;
using DarkDescent.Stats;
using TMPro;
using UnityEngine;

namespace DarkDescent.UI
{
    /// <summary>
    /// Il pannello del personaggio, nella metà sinistra: attributi e valori derivati, calcolati con
    /// le stesse formule del combattimento. Si aggiorna quando cambiano statistiche o arma, mai in Update.
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

        private readonly StringBuilder _builder = new StringBuilder(128);
        private CharacterStats _stats;
        private PlayerInventory _inventory;
        private PlayerInputReader _reader;
        private ShieldBlock _block;
        private bool _subscribed;

        public bool IsOpen => _window.activeSelf;

        public string ValuesText => _values.text;

        public void Bind(CharacterStats stats, PlayerInventory inventory, PlayerInputReader reader)
        {
            Unsubscribe();
            _stats = stats;
            _inventory = inventory;
            _block = stats.GetComponent<ShieldBlock>();
            _reader = reader;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        public void Toggle()
        {
            _window.SetActive(!_window.activeSelf);
        }

        /// <summary>Danno minimo e massimo dell'arma attuale, con la Forza: quello che il pannello mostra.</summary>
        public (int min, int max) DamageRange()
        {
            var weapon = _inventory.Equipment.Get(EquipSlot.Weapon)?.Definition as WeaponDefinition;
            if (weapon == null)
            {
                weapon = _inventory.Unarmed;
            }

            float multiplier = CombatFormulas.StrengthMultiplier(_stats.Strength);
            return (Mathf.RoundToInt(weapon.MinDamage * multiplier), Mathf.RoundToInt(weapon.MaxDamage * multiplier));
        }

        private void Awake()
        {
            _window.SetActive(false);
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed || _stats == null)
            {
                return;
            }

            _reader.CharacterToggled += Toggle;
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
            float hitChance = CombatFormulas.HitChance(_stats.Dexterity, _referenceArmor);

            // l'ordine segue la colonna dei nomi nella scena
            _builder.Clear();
            _builder.Append(Mathf.RoundToInt(_stats.Strength)).Append('\n');
            _builder.Append(Mathf.RoundToInt(_stats.Dexterity)).Append('\n');
            _builder.Append(Mathf.RoundToInt(_stats.Magic)).Append('\n');
            _builder.Append(Mathf.RoundToInt(_stats.Vitality)).Append("\n\n");
            _builder.Append(Mathf.RoundToInt(CombatFormulas.MaxLife(_stats.Vitality))).Append('\n');
            _builder.Append(Mathf.RoundToInt(_stats.Armor)).Append('\n');
            _builder.Append(min).Append('–').Append(max).Append('\n');
            _builder.Append(Mathf.RoundToInt(hitChance)).Append("%\n");
            _builder.Append(_block != null ? Mathf.RoundToInt(_block.BlockChance) : 0).Append('%');
            _values.SetText(_builder);
        }
    }
}
