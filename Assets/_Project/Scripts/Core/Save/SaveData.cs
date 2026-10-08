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
    /// formato 2, la mappa scoperta di ogni profondità visitata. Lo stato dei livelli non c'è (D8):
    /// un livello rientrandoci si rigenera dal seme. Il campo
    /// <c>version</c> dice il formato: il codice ne legge uno solo, e i formati vecchi si migrano
    /// prima di leggerli.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        /// <summary>Il formato che questo codice scrive e legge.</summary>
        public const int CurrentVersion = 2;

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
        internal void UpgradeTo(int version)
        {
            _version = version;
            _explored ??= new List<ExploredLevel>();
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
