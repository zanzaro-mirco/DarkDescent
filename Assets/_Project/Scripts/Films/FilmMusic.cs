using UnityEngine;

namespace DarkDescent.Films
{
    /// <summary>
    /// La musica di un filmato (D10 della M19): il brano, da che secondo parte e come finisce. Un brano che
    /// sta già suonando continua: così la vittoria tiene la stessa musica dal portale al borgo, attraverso
    /// il viaggio, che una Timeline sola non potrebbe.
    /// </summary>
    public readonly struct FilmMusic
    {
        public FilmMusic(AudioClip clip, float from, bool runsOut)
        {
            Clip = clip;
            From = from;
            RunsOut = runsOut;
        }

        /// <summary>Il brano; null, il filmato va in silenzio (la musica del livello tace comunque).</summary>
        public AudioClip Clip { get; }

        /// <summary>Il secondo del brano da cui parte, se non sta già suonando.</summary>
        public float From { get; }

        /// <summary>Vero: alla fine del filmato il brano continua fino in fondo; falso: sfuma.</summary>
        public bool RunsOut { get; }
    }
}
