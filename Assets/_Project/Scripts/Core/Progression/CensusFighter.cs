namespace DarkDescent.Progression
{
    /// <summary>
    /// I numeri di chi combatte nella misura delle difficoltà (D10 della M14): vita, danno dell'arma
    /// con i suoi affissi, attributi che entrano nella formula del colpo, blocco e moltiplicatore del
    /// danno; dalla M16 la velocità d'attacco e il danno ridotto; dalla M20 il danno del critico. Una fotografia: non cambia dopo
    /// essere stata presa.
    /// </summary>
    public readonly struct CensusFighter
    {
        public CensusFighter(float life, int minDamage, int maxDamage, float strength, float level, float dexterity,
            float armor, float toHit, float critBonus, bool canCrit, float block, float damageFactor,
            float attackSpeed = 0f, float damageReduction = 0f, float lifeSteal = 0f, float critDamage = 0f,
            float magic = 0f, float boltDivisor = 0f)
        {
            Life = life;
            MinDamage = minDamage;
            MaxDamage = maxDamage;
            Strength = strength;
            Level = level;
            Dexterity = dexterity;
            Armor = armor;
            ToHit = toHit;
            CritBonus = critBonus;
            CanCrit = canCrit;
            Block = block;
            DamageFactor = damageFactor;
            AttackSpeed = attackSpeed;
            DamageReduction = damageReduction;
            LifeSteal = lifeSteal;
            CritDamage = critDamage;
            Magic = magic;
            BoltDivisor = boltDivisor;
        }

        public float Life { get; }

        public int MinDamage { get; }

        public int MaxDamage { get; }

        public float Strength { get; }

        /// <summary>Il livello che entra nel danno con la Forza: 0 per i nemici.</summary>
        public float Level { get; }

        public float Dexterity { get; }

        public float Armor { get; }

        public float ToHit { get; }

        public float CritBonus { get; }

        /// <summary>Solo il cavaliere fa colpi critici (D13 della M7).</summary>
        public bool CanCrit { get; }

        /// <summary>La probabilità di bloccare, in punti percentuali, già con la Destrezza.</summary>
        public float Block { get; }

        /// <summary>Grado e difficoltà insieme, come <c>MeleeAttack.DamageMultiplier</c>.</summary>
        public float DamageFactor { get; }

        /// <summary>La velocità d'attacco in più, in punti percentuali (D5 della M16).</summary>
        public float AttackSpeed { get; }

        /// <summary>I punti tolti a ogni colpo preso, mai sotto 1 (D5 della M16).</summary>
        public float DamageReduction { get; }

        /// <summary>La parte del danno inflitto che torna in vita, in punti percentuali (D5 della M16).</summary>
        public float LifeSteal { get; }

        /// <summary>I punti in più del moltiplicatore del critico, in centesimi (Punto debole, M20).</summary>
        public float CritDamage { get; }

        /// <summary>La Magia, che il dardo del Mago somma al tiro dell'arma (D3 della M21).</summary>
        public float Magic { get; }

        /// <summary>Più di zero: il colpo è il dardo del Mago, tiro più Magia / questo numero, senza tiro per colpire né critico. Zero: il colpo dell'arma.</summary>
        public float BoltDivisor { get; }

        public bool IsBolt => BoltDivisor > 0f;
    }
}
