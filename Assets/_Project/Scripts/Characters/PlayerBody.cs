using System;
using UnityEngine;

namespace DarkDescent.Characters
{
    /// <summary>
    /// I corpi del personaggio sul rig comune (D2 della M21). I modelli KayKit hanno tutti lo stesso
    /// scheletro, e il prefab del giocatore ha una sola <c>Model</c> con l'Animator, le mani per le armi e
    /// il lampo del colpo: i pezzi di ogni corpo (il Cavaliere, il Mago) sono figli suoi, legati alle stesse
    /// ossa, e a ogni classe se ne mostra uno e si nascondono gli altri. Li prepara lo strumento
    /// <c>PlayerBodyTools</c>; niente si crea a partita iniziata.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerBody : MonoBehaviour
    {
        [Serializable]
        public struct BodySet
        {
            [Tooltip("L'id del corpo, quello della classe: knight, mage.")]
            public string Id;

            [Tooltip("I pezzi che lo fanno: testa, corpo, braccia, gambe, mantello, cappello.")]
            public GameObject[] Parts;
        }

        [SerializeField] private BodySet[] _sets = new BodySet[0];

        [Tooltip("Il lampo del colpo: raccoglie i renderer del corpo.")]
        [SerializeField] private HitFlash _hitFlash;

        /// <summary>L'id del corpo mostrato; null finché non se n'è scelto uno.</summary>
        public string Current { get; private set; }

        public int Count => _sets != null ? _sets.Length : 0;

        /// <summary>Mostra il corpo con questo id e nasconde gli altri; false, senza cambiare niente, se non c'è.</summary>
        public bool Show(string id)
        {
            int index = -1;
            for (int i = 0; _sets != null && i < _sets.Length; i++)
            {
                if (_sets[i].Id == id)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                return false;
            }

            for (int i = 0; i < _sets.Length; i++)
            {
                foreach (var part in _sets[i].Parts)
                {
                    if (part != null)
                    {
                        part.SetActive(i == index);
                    }
                }
            }

            Current = id;
            if (_hitFlash != null)
            {
                _hitFlash.RefreshRenderers();
            }

            return true;
        }

        /// <summary>Lo strumento dell'editor: i pezzi di ogni corpo.</summary>
        public void Define(BodySet[] sets, HitFlash hitFlash)
        {
            _sets = sets;
            _hitFlash = hitFlash;
        }
    }
}
