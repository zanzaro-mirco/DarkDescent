using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DarkDescent.Classes
{
    /// <summary>
    /// Le classi del gioco, nell'ordine in cui la creazione del personaggio le mostra (D2 della M21). Il
    /// Guerriero è la classe di chi non ne ha una: i salvataggi di prima della M21 e le partite di prova.
    /// </summary>
    [CreateAssetMenu(menuName = "DarkDescent/Elenco delle classi", fileName = "ClassCatalog")]
    public sealed class CharacterClassCatalog : ScriptableObject
    {
        [SerializeField] private CharacterClass[] _classes = System.Array.Empty<CharacterClass>();

        public IReadOnlyList<CharacterClass> All => _classes;

        /// <summary>Quelle che si possono scegliere, cioè che hanno il loro albero.</summary>
        public IEnumerable<CharacterClass> Playable => _classes.Where(c => c != null && c.IsPlayable);

        /// <summary>Il Guerriero: la classe di chi non ne ha una.</summary>
        public CharacterClass Warrior => _classes.FirstOrDefault(c => c != null && c.Id == CharacterClass.WarriorId);

        /// <summary>La classe con quell'id, o il Guerriero se non c'è (un salvataggio di un'altra versione).</summary>
        public CharacterClass Find(string id)
        {
            return _classes.FirstOrDefault(c => c != null && c.Id == id) ?? Warrior;
        }

        /// <summary>Riempie l'elenco: lo strumento dell'editor e i test.</summary>
        public void Define(IEnumerable<CharacterClass> classes)
        {
            _classes = classes.ToArray();
        }
    }
}
