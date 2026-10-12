using System;
using System.Collections.Generic;
using System.Globalization;
using DarkDescent.Items;
using DarkDescent.Levels;
using UnityEngine;

namespace DarkDescent.Save
{
    /// <summary>
    /// Un salvataggio (D7–D9 della M8): seme della partita, dove si è (scena, ingresso, profondità),
    /// la crescita del cavaliere, i valori base degli attributi, la vita, l'inventario completo e, dal
    /// formato 2, la mappa scoperta di ogni profondità visitata; dal formato 3 gli incantesimi noti,
    /// quello scelto e il mana (D11 della M9); dal formato 4 l'oro, la città, la profondità a cui
    /// riporta il portale e i contatori della partita (D11 della M10). Lo stato dei livelli non c'è (D8):
    /// un livello rientrandoci si rigenera dal seme. Il campo
    /// <c>version</c> dice il formato: il codice ne legge uno solo, e i formati vecchi si migrano
    /// prima di leggerli.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        /// <summary>Il formato che questo codice scrive e legge.</summary>
        public const int CurrentVersion = 4;

        /// <summary>Il mana salvato quando va riempito: un salvataggio migrato, o un cavaliere morto.</summary>
        public const float FullMana = -1f;

        [SerializeField] private int _version = CurrentVersion;

        // un ulong in testo: JsonUtility lo scriverebbe come numero, e altri lettori lo perderebbero
        [SerializeField] private string _runSeed;

        [SerializeField] private string _scene;
        [SerializeField] private string _entrance;
        [SerializeField] private int _depth;

        [SerializeField] private int _level = 1;
        [SerializeField] private int _experience;
        [SerializeField] private int _unspentPoints;

        [SerializeField] private float _strength;
        [SerializeField] private float _dexterity;
        [SerializeField] private float _magic;
        [SerializeField] private float _vitality;

        [SerializeField] private float _life;

        [SerializeField] private InventorySnapshot _inventory;

        // formato 2: le mappe scoperte. Un salvataggio del formato 1 non ha il campo, e resta vuoto
        [SerializeField] private List<ExploredLevel> _explored = new List<ExploredLevel>();

        // formato 3: gli id degli incantesimi noti, il posto scelto e il mana (FullMana: pieno)
        [SerializeField] private List<string> _knownSpells = new List<string>();
        [SerializeField] private int _selectedSpell;
        [SerializeField] private float _mana = FullMana;

        // formato 4: l'oro, se si è in città e dove riporta il portale (0: da nessuna parte), i contatori
        [SerializeField] private int _gold;
        [SerializeField] private bool _inTown;
        [SerializeField] private int _portalDepth;
        [SerializeField] private float _playTime;
        [SerializeField] private int _kills;
        [SerializeField] private int _deaths;
        [SerializeField] private int _goldCollected;
        [SerializeField] private bool _bossDefeated;

        // ancora formato 4, aggiunto con il 10.9: il punto in cui riporta il portale. Un salvataggio
        // che non ha i campi legge false, e il portale riporta all'ingresso come prima
        [SerializeField] private bool _hasPortalSpot;
        [SerializeField] private Vector3 _portalSpot;

        public int Version => _version;

        public ulong RunSeed => ulong.TryParse(_runSeed, NumberStyles.None, CultureInfo.InvariantCulture, out ulong seed) ? seed : 0UL;

        public string Scene => _scene;

        public string Entrance => _entrance;

        public int Depth => _depth;

        public int Level => _level;

        public int Experience => _experience;

        public int UnspentPoints => _unspentPoints;

        public float Strength => _strength;

        public float Dexterity => _dexterity;

        public float Magic => _magic;

        public float Vitality => _vitality;

        public float Life => _life;

        public InventorySnapshot Inventory => _inventory;

        public IReadOnlyList<ExploredLevel> Explored => _explored;

        public IReadOnlyList<string> KnownSpells => _knownSpells;

        public int SelectedSpell => _selectedSpell;

        /// <summary>Il mana del cavaliere; <see cref="FullMana"/> se va riempito.</summary>
        public float Mana => _mana;

        public int Gold => _gold;

        public bool InTown => _inTown;

        /// <summary>La profondità all'ingresso della quale riporta il portale della città; 0 se non c'è.</summary>
        public int PortalDepth => _portalDepth;

        /// <summary>Il punto della profondità in cui riporta il portale della città; null: all'ingresso.</summary>
        public Vector3? PortalSpot => _hasPortalSpot ? _portalSpot : (Vector3?)null;

        public float PlayTime => _playTime;

        public int Kills => _kills;

        public int Deaths => _deaths;

        public int GoldCollected => _goldCollected;

        public bool BossDefeated => _bossDefeated;

        public static SaveData Create(ulong runSeed, string scene, string entrance, int depth)
        {
            return new SaveData
            {
                _runSeed = runSeed.ToString(CultureInfo.InvariantCulture),
                _scene = scene,
                _entrance = entrance,
                _depth = depth,
            };
        }

        public SaveData WithProgress(int level, int experience, int unspentPoints)
        {
            _level = level;
            _experience = experience;
            _unspentPoints = unspentPoints;
            return this;
        }

        public SaveData WithAttributes(float strength, float dexterity, float magic, float vitality)
        {
            _strength = strength;
            _dexterity = dexterity;
            _magic = magic;
            _vitality = vitality;
            return this;
        }

        public SaveData WithLife(float life)
        {
            _life = life;
            return this;
        }

        public SaveData WithInventory(InventorySnapshot inventory)
        {
            _inventory = inventory;
            return this;
        }

        public SaveData WithExplored(List<ExploredLevel> explored)
        {
            _explored = explored ?? new List<ExploredLevel>();
            return this;
        }

        /// <summary>Per la migrazione: dichiara il formato a cui il salvataggio è stato portato.</summary>
        /// <summary>Gli incantesimi: gli id di quelli noti, il posto scelto e il mana (<see cref="FullMana"/> per pieno).</summary>
        public SaveData WithMagic(List<string> knownSpells, int selectedSpell, float mana)
        {
            _knownSpells = knownSpells ?? new List<string>();
            _selectedSpell = selectedSpell;
            _mana = mana;
            return this;
        }

        public SaveData WithGold(int gold)
        {
            _gold = gold;
            return this;
        }

        /// <summary>Se si è in città, a che profondità riporta il portale (0: nessuna) e in che punto (null: all'ingresso).</summary>
        public SaveData WithTown(bool inTown, int portalDepth, Vector3? portalSpot = null)
        {
            _inTown = inTown;
            _portalDepth = portalDepth;
            _hasPortalSpot = portalDepth > 0 && portalSpot.HasValue;
            _portalSpot = _hasPortalSpot ? portalSpot.Value : Vector3.zero;
            return this;
        }

        /// <summary>I contatori della partita, per la schermata di vittoria.</summary>
        public SaveData WithStats(float playTime, int kills, int deaths, int goldCollected, bool bossDefeated)
        {
            _playTime = playTime;
            _kills = kills;
            _deaths = deaths;
            _goldCollected = goldCollected;
            _bossDefeated = bossDefeated;
            return this;
        }

        internal void UpgradeTo(int version)
        {
            // dal formato 2 al 3: niente incantesimi imparati, il primo posto e il mana pieno; JsonUtility
            // lascia ai campi mancanti il valore di default del tipo, non quello dell'inizializzatore
            if (_version < 3 && version >= 3)
            {
                _knownSpells = new List<string>();
                _selectedSpell = 0;
                _mana = FullMana;
            }

            // dal formato 3 al 4 (D11 della M10): niente oro, nel dungeon, nessun portale, contatori a zero
            if (_version < 4 && version >= 4)
            {
                _gold = 0;
                _inTown = false;
                _portalDepth = 0;
                _hasPortalSpot = false;
                _playTime = 0f;
                _kills = 0;
                _deaths = 0;
                _goldCollected = 0;
                _bossDefeated = false;
            }

            _version = version;
            _explored ??= new List<ExploredLevel>();
            _knownSpells ??= new List<string>();
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }

        /// <summary>Legge il JSON; null se non è un salvataggio (vuoto, troncato, di un altro gioco).</summary>
        public static SaveData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                var data = JsonUtility.FromJson<SaveData>(json);
                return data != null && data._version > 0 && !string.IsNullOrEmpty(data._scene) && data._inventory != null ? data : null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
