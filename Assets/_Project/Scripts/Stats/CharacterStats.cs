using UnityEngine;

namespace DarkDescent.Stats
{
    /// <summary>
    /// Guscio Unity di StatSheet: i valori base stanno nell'Inspector, i modificatori arrivano a
    /// runtime (equipaggiamento). Il cavaliere ha i quattro attributi; agli scheletri basta un
    /// blocco fisso di Destrezza e Armatura (D1 della M4).
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterStats : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _strength;
        [SerializeField, Min(0f)] private float _dexterity;
        [SerializeField, Min(0f)] private float _magic;
        [SerializeField, Min(0f)] private float _vitality;
        [SerializeField, Min(0f)] private float _armor;

        [Tooltip("Vero: la vita massima viene dalla Vitalità. Falso: la decide Health, come per i nemici.")]
        [SerializeField] private bool _lifeFromVitality;

        private StatSheet _sheet;

        // Creato al primo accesso, come il model di Health: Health ci legge la Vitalità quando crea
        // il suo, e l'ordine degli Awake tra componenti non è garantito.
        public StatSheet Sheet => _sheet ??= CreateSheet();

        public bool LifeFromVitality => _lifeFromVitality;

        public float Strength => Sheet.Get(StatType.Strength);
        public float Dexterity => Sheet.Get(StatType.Dexterity);
        public float Magic => Sheet.Get(StatType.Magic);
        public float Vitality => Sheet.Get(StatType.Vitality);
        public float Armor => Sheet.Get(StatType.Armor);
        public float ToHit => Sheet.Get(StatType.ToHit);
        public float Life => Sheet.Get(StatType.Life);

        /// <summary>Il valore di <paramref name="stat"/> di <paramref name="stats"/>, o 0 se manca (o è distrutto).</summary>
        public static float ValueOf(CharacterStats stats, StatType stat)
        {
            return stats != null ? stats.Sheet.Get(stat) : 0f;
        }

        private StatSheet CreateSheet()
        {
            var sheet = new StatSheet();
            sheet.SetBase(StatType.Strength, _strength);
            sheet.SetBase(StatType.Dexterity, _dexterity);
            sheet.SetBase(StatType.Magic, _magic);
            sheet.SetBase(StatType.Vitality, _vitality);
            sheet.SetBase(StatType.Armor, _armor);
            return sheet;
        }
    }
}
