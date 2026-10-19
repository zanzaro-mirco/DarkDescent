using UnityEngine;

namespace DarkDescent.Enemies
{
    /// <summary>
    /// Un modificatore dei campioni e degli unici (D5 della M12): Rapido, Gelido, Rigenerante,
    /// Inamovibile, Furioso. L'id va nella mappa del livello, il nome nella barra in alto. Un
    /// modificatore nuovo dello stesso tipo è un asset; uno di tipo nuovo vuole anche il codice.
    /// Dati immutabili.
    /// </summary>
    [CreateAssetMenu(menuName = "DarkDescent/Champion Modifier", fileName = "ChampionModifier")]
    public sealed class ChampionModifier : ScriptableObject
    {
        [Tooltip("Stabile e senza spazi: va nella direttiva @champions della mappa.")]
        [SerializeField] private string _id;

        [Tooltip("Il nome nella tabella delle stringhe, nella barra in alto.")]
        [SerializeField] private string _nameKey;

        [SerializeField] private ChampionModifierKind _kind;

        [Tooltip("Quanto: 0,5 = 50% più in fretta (Rapido), 50% di velocità in meno (Gelido), 3% della vita al secondo con 0,03 (Rigenerante), 50% di danno in più (Furioso).")]
        [SerializeField, Min(0f)] private float _magnitude;

        [Tooltip("Secondi: quanto dura il rallentamento (Gelido), quanto aspetta a rigenerare dopo un colpo (Rigenerante).")]
        [SerializeField, Min(0f)] private float _duration;

        [Tooltip("La parte della vita sotto cui si accende, tra 0 e 1 (Furioso).")]
        [SerializeField, Range(0f, 1f)] private float _threshold;

        [Tooltip("Velocità in più, di movimento e di attacco, quando si accende: 0,25 = 25% (Furioso).")]
        [SerializeField, Min(0f)] private float _speedBonus;

        public string Id => _id;

        public string NameKey => _nameKey;

        public ChampionModifierKind Kind => _kind;

        public float Magnitude => _magnitude;

        public float Duration => _duration;

        public float Threshold => _threshold;

        public float SpeedBonus => _speedBonus;
    }
}
