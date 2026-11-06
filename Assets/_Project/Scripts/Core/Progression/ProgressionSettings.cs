using UnityEngine;

namespace DarkDescent.Progression
{
    /// <summary>
    /// I numeri della crescita del cavaliere (D1–D3 della M8): curva dell'esperienza, punti e vita a
    /// livello, esperienza dei nemici secondo la profondità. Le formule stanno qui, i numeri
    /// nell'asset: si tarano provando senza toccare il codice.
    /// </summary>
    [CreateAssetMenu(fileName = "Progression", menuName = "DarkDescent/Progression")]
    public sealed class ProgressionSettings : ScriptableObject
    {
        [Tooltip("Il livello più alto: 20 nella v1.0 (D1), 50 con le difficoltà (D6 della M14).")]
        [SerializeField, Min(2)] private int _maxLevel = 50;

        [Tooltip("Esperienza per passare dal livello L al successivo: base × L^esponente (D1).")]
        [SerializeField, Min(1)] private int _baseExperience = 100;

        [SerializeField, Min(1f)] private float _exponent = 1.6f;

        [Tooltip("La soglia di ogni livello si arrotonda a questo multiplo.")]
        [SerializeField, Min(1)] private int _rounding = 10;

        [Tooltip("Punti attributo a ogni livello (D3, come in Diablo 1).")]
        [SerializeField, Min(0)] private int _pointsPerLevel = 5;

        [Tooltip("Vita massima in più a ogni livello, oltre a quella della Vitalità (D3).")]
        [SerializeField, Min(0)] private int _lifePerLevel = 2;

        [Tooltip("Esperienza di un nemico in più per ogni profondità oltre la prima: 0,15 è +15% a livello (D2).")]
        [SerializeField, Min(0f)] private float _depthBonus = 0.15f;

        [Tooltip("Il livello di una zona è la profondità per questo fattore: la profondità 8 vale il livello 10 (D2).")]
        [SerializeField, Min(0.1f)] private float _zoneLevelPerDepth = 1.25f;

        [Tooltip("Per ogni livello del cavaliere sopra quello della zona, l'esperienza si divide per 1 + questo (D2).")]
        [SerializeField, Min(0f)] private float _overLevelPenalty = 0.1f;

        [Header("Il mondo cresce con te (D12–D14 della M24)")]
        [Tooltip("Con l'interruttore acceso il livello efficace non scende sotto il livello del cavaliere meno questo margine.")]
        [SerializeField, Min(0)] private int _growthMargin = 3;

        [Tooltip("Vita e danno dei nemici in più per ogni livello efficace oltre la zona: 0,04 è +4% a livello.")]
        [SerializeField, Min(0f)] private float _growthPerLevel = 0.04f;

        [Tooltip("Il moltiplicatore di vita e danno non supera questo valore.")]
        [SerializeField, Min(1f)] private float _growthCap = 3f;

        public int MaxLevel => _maxLevel;

        public int GrowthMargin => _growthMargin;

        public float GrowthPerLevel => _growthPerLevel;

        public float GrowthCap => _growthCap;

        public int PointsPerLevel => _pointsPerLevel;

        public int LifePerLevel => _lifePerLevel;

        /// <summary>Esperienza per passare da <paramref name="level"/> al successivo; 0 al livello massimo.</summary>
        public int ExperienceToNext(int level)
        {
            if (level >= _maxLevel)
            {
                return 0;
            }

            float raw = _baseExperience * Mathf.Pow(Mathf.Max(1, level), _exponent);
            return Mathf.Max(_rounding, Mathf.RoundToInt(raw / _rounding) * _rounding);
        }

        /// <summary>Il livello dei nemici di una profondità, per la riduzione dell'esperienza e il livello degli oggetti.</summary>
        public int ZoneLevel(int depth)
        {
            return ZoneLevel(depth, 0);
        }

        /// <summary>Il livello della zona con quello in più della difficoltà (D5 della M14).</summary>
        public int ZoneLevel(int depth, int difficultyBonus)
        {
            // mezzo in su, non al pari come RoundToInt: la profondità 2 vale il livello 3
            return Mathf.Max(1, Mathf.FloorToInt(depth * _zoneLevelPerDepth + 0.5f)) + Mathf.Max(0, difficultyBonus);
        }

        /// <summary>
        /// Il livello efficace di una profondità (D12 della M24): quello della zona, e con il mondo che cresce non meno del livello
        /// del cavaliere meno il margine. Spento, è la zona e basta.
        /// </summary>
        public int EffectiveLevel(int depth, int heroLevel, int difficultyBonus, bool growingWorld)
        {
            int zone = ZoneLevel(depth, difficultyBonus);
            return growingWorld ? Mathf.Max(zone, heroLevel - _growthMargin) : zone;
        }

        /// <summary>
        /// Quanto crescono vita e danno dei nemici (D13): 1 + la curva per ogni livello efficace oltre la zona, con il tetto. 1
        /// a mondo spento o con il cavaliere dentro il margine.
        /// </summary>
        public float EnemyGrowth(int depth, int heroLevel, int difficultyBonus, bool growingWorld)
        {
            if (!growingWorld)
            {
                return 1f;
            }

            int over = EffectiveLevel(depth, heroLevel, difficultyBonus, true) - ZoneLevel(depth, difficultyBonus);
            return Mathf.Min(_growthCap, 1f + _growthPerLevel * Mathf.Max(0, over));
        }

        /// <summary>
        /// Esperienza per un nemico che ne vale <paramref name="baseExperience"/>, ucciso alla
        /// profondità data da un cavaliere di quel livello: cresce con la profondità e cala se il
        /// cavaliere è più forte della zona, come in Diablo. La difficoltà alza il livello della
        /// zona e moltiplica il resto (D5 della M14). Almeno 1, se il nemico vale qualcosa.
        /// </summary>
        public int ExperienceForKill(int baseExperience, int depth, int heroLevel, int difficultyBonus = 0, float difficultyFactor = 1f, bool growingWorld = false)
        {
            if (baseExperience <= 0)
            {
                return 0;
            }

            float experience = baseExperience * (1f + _depthBonus * (Mathf.Max(1, depth) - 1)) * Mathf.Max(1f, difficultyFactor);
            // con il mondo che cresce il calo parte dal livello efficace, cioè dal margine (D12)
            int over = heroLevel - EffectiveLevel(depth, heroLevel, difficultyBonus, growingWorld);
            if (over > 0)
            {
                experience /= 1f + _overLevelPenalty * over;
            }

            return Mathf.Max(1, Mathf.RoundToInt(experience));
        }
    }
}
