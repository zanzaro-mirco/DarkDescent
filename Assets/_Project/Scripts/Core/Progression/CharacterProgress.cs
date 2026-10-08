using System;
using DarkDescent.Stats;

namespace DarkDescent.Progression
{
    /// <summary>
    /// Livello, esperienza e punti attributo di un personaggio (D1–D3 della M8). Salire dà i punti
    /// e la vita in più, scritta come valore base della vita sulla sua scheda; spendere un punto alza
    /// di uno il valore base di un attributo. Logica pura: il componente del cavaliere la crea e la
    /// collega alla vita e all'HUD.
    /// </summary>
    public sealed class CharacterProgress
    {
        private readonly ProgressionSettings _settings;
        private readonly StatSheet _sheet;

        public CharacterProgress(ProgressionSettings settings, StatSheet sheet)
        {
            _settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
            _sheet = sheet ?? throw new ArgumentNullException(nameof(sheet));
        }

        /// <summary>Esperienza guadagnata, livelli compresi: per l'HUD e i suoni.</summary>
        public event Action<int> ExperienceGained;

        /// <summary>Il livello nuovo, una volta per ogni livello salito.</summary>
        public event Action<int> LeveledUp;

        /// <summary>Cambiati esperienza, livello o punti: chi li mostra li rilegge.</summary>
        public event Action Changed;

        public int Level { get; private set; } = 1;

        /// <summary>Esperienza dentro il livello corrente, da 0 a <see cref="ExperienceToNext"/>.</summary>
        public int Experience { get; private set; }

        /// <summary>Quanta ne serve per il livello successivo; 0 al livello massimo.</summary>
        public int ExperienceToNext => _settings.ExperienceToNext(Level);

        public int UnspentPoints { get; private set; }

        public bool IsMaxLevel => Level >= _settings.MaxLevel;

        /// <summary>Gli attributi su cui si spendono i punti: i quattro di Diablo.</summary>
        public static bool IsAttribute(StatType stat)
        {
            return stat == StatType.Strength || stat == StatType.Dexterity || stat == StatType.Magic || stat == StatType.Vitality;
        }

        /// <summary>Aggiunge esperienza; restituisce quanti livelli si sono saliti. Al livello massimo si ferma.</summary>
        public int Add(int amount)
        {
            if (amount <= 0 || IsMaxLevel)
            {
                return 0;
            }

            int before = Level;
            Experience += amount;
            while (!IsMaxLevel && Experience >= ExperienceToNext)
            {
                Experience -= ExperienceToNext;
                Level++;
                UnspentPoints += _settings.PointsPerLevel;
            }

            if (IsMaxLevel)
            {
                Experience = 0;
            }

            if (Level > before)
            {
                ApplyLevelLife();
            }

            ExperienceGained?.Invoke(amount);
            for (int level = before + 1; level <= Level; level++)
            {
                LeveledUp?.Invoke(level);
            }

            Changed?.Invoke();
            return Level - before;
        }

        /// <summary>Spende un punto su un attributo; false se non ci sono punti o se non è un attributo.</summary>
        public bool TrySpend(StatType stat)
        {
            if (UnspentPoints <= 0 || !IsAttribute(stat))
            {
                return false;
            }

            UnspentPoints--;
            _sheet.SetBase(stat, _sheet.GetBase(stat) + 1f);
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Rimette uno stato salvato. Gli attributi spesi stanno già nei valori base della scheda:
        /// qui solo livello, esperienza, punti e la vita dei livelli.
        /// </summary>
        public void Restore(int level, int experience, int unspentPoints)
        {
            Level = Math.Max(1, Math.Min(level, _settings.MaxLevel));
            Experience = IsMaxLevel ? 0 : Math.Max(0, Math.Min(experience, ExperienceToNext - 1));
            UnspentPoints = Math.Max(0, unspentPoints);
            ApplyLevelLife();
            Changed?.Invoke();
        }

        private void ApplyLevelLife()
        {
            _sheet.SetBase(StatType.Life, (Level - 1) * _settings.LifePerLevel);
        }
    }
}
