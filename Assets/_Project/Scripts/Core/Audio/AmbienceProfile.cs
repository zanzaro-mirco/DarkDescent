using UnityEngine;

namespace DarkDescent.Audio
{
    /// <summary>
    /// Il suono di un tipo di livello (passo 7.0 della M7): una musica cupa, un fondo di vento e
    /// gocce sotto, e ogni tanto un verso lontano nel buio da un punto a caso attorno al cavaliere.
    /// Sta nel tileset: la cripta ne ha uno, le caverne un altro. Dati immutabili.
    /// </summary>
    [CreateAssetMenu(menuName = "DarkDescent/Ambience Profile", fileName = "Ambience")]
    public class AmbienceProfile : ScriptableObject
    {
        [Tooltip("La musica, in loop. Due livelli con lo stesso profilo non la fanno ripartire.")]
        [SerializeField] private AudioClip _music;

        [Tooltip("Volume della musica sulla sorgente: le tracce hanno volumi diversi, qui si pareggiano.")]
        [SerializeField, Range(0f, 1f)] private float _musicVolume = 1f;

        [Tooltip("Il fondo, in loop sotto la musica: vento e gocce, copre anche il silenzio quando la musica ricomincia.")]
        [SerializeField] private AudioClip _bed;

        [SerializeField, Range(0f, 1f)] private float _bedVolume = 0.2f;

        [Tooltip("I versi nel buio: uno alla volta, mai lo stesso due volte di fila.")]
        [SerializeField] private AudioClip[] _stingers;

        [SerializeField, Range(0f, 1f)] private float _stingerVolume = 0.6f;

        [Tooltip("Secondi tra un verso e l'altro: da quanti a quanti.")]
        [SerializeField] private Vector2 _stingerInterval = new Vector2(20f, 50f);

        [Tooltip("Metri dal cavaliere: oltre il raggio della sua luce, dove non si vede niente.")]
        [SerializeField] private Vector2 _stingerDistance = new Vector2(10f, 18f);

        [Tooltip("Intonazione: più bassa, più grande la cosa che l'ha fatto.")]
        [SerializeField] private Vector2 _stingerPitch = new Vector2(0.75f, 1.05f);

        public AudioClip Music => _music;

        public float MusicVolume => _musicVolume;

        public AudioClip Bed => _bed;

        public float BedVolume => _bedVolume;

        public int StingerCount => _stingers != null ? _stingers.Length : 0;

        public float StingerVolume => _stingerVolume;

        public Vector2 StingerInterval => _stingerInterval;

        public Vector2 StingerDistance => _stingerDistance;

        public Vector2 StingerPitch => _stingerPitch;

        public AudioClip GetStinger(int index)
        {
            return _stingers[index];
        }
    }
}
