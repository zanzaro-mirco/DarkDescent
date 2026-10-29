using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace DarkDescent.Films
{
    /// <summary>
    /// La traccia del set nella Timeline di un filmato (D2 della M19): una clip sola, lunga quanto il
    /// filmato, che a ogni fotogramma dà al <see cref="FilmSet"/> del regista il secondo a cui è
    /// arrivata. Il set è sullo stesso oggetto del regista: la traccia non ha bisogno di legami.
    /// </summary>
    [TrackColor(0.85f, 0.6f, 0.25f)]
    [TrackClipType(typeof(FilmSetClip))]
    public class FilmSetTrack : TrackAsset
    {
    }
}
