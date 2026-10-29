using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace DarkDescent.Films
{
    /// <summary>La clip della traccia del set: trova il set accanto al regista.</summary>
    public class FilmSetClip : PlayableAsset, ITimelineClipAsset
    {
        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<FilmSetBehaviour>.Create(graph);
            playable.GetBehaviour().Set = owner != null ? owner.GetComponent<FilmSet>() : null;
            return playable;
        }
    }

    /// <summary>Porta il set al tempo della clip, che comincia con il filmato.</summary>
    public class FilmSetBehaviour : PlayableBehaviour
    {
        public FilmSet Set { get; set; }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (Set != null)
            {
                Set.Show((float)playable.GetTime());
            }
        }
    }
}
