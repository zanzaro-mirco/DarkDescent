using System.Collections.Generic;
using System.Linq;
using DarkDescent.Levels;
using DarkDescent.Rendering;
using DarkDescent.Town;
using Unity.Cinemachine;
using UnityEngine;

namespace DarkDescent.Films
{
    /// <summary>
    /// Il set dell'apertura (D7 della M19), nel borgo in cui comincia la partita:
    /// <list type="number">
    /// <item>di notte le candele si spengono una a una, dalla cripta verso la piazza, e il fuoco muore;</item>
    /// <item>nel cimitero tre scheletri escono dalle tombe smosse e i paesani scappano con le lanterne;</item>
    /// <item>all'alba i sopravvissuti siedono attorno al fuoco spento;</item>
    /// <item>arriva il cavaliere, e il mercante accende l'ultima candela sull'altare e indica la grotta;</item>
    /// <item>il titolo sopra la grotta, poi la camera scende sul cavaliere e si gioca.</item>
    /// </list>
    /// I paesani e il mercante sono copie dei loro modelli, il cavaliere è la copia del tuo: alla fine
    /// spariscono e il borgo torna quello del gioco, di giorno, con il cavaliere dove la copia si è fermata.
    /// </summary>
    public class OpeningSet : FilmSet
    {
        private const int Graves = 3;
        private const float SnuffSeconds = 0.5f;
        private const float RunSpeed = 4.2f;
        private const float WalkSpeed = 2.2f;
        private const float SkeletonSpeed = 0.8f;
        private const float BlendSeconds = 0.35f;
        private const float CandleLight = 2.5f;
        // le lanterne in mano fanno più luce di quelle posate: sono l'unica luce del cimitero
        private const float CarriedLight = 2f;

        [Header("Oggetti")]
        [Tooltip("I nemici di cui si copia il modello per gli scheletri che escono dalle tombe.")]
        [SerializeField] private GameObject[] _skeletons = new GameObject[0];
        [SerializeField] private GameObject _lantern;
        [SerializeField] private GameObject _graveDug;
        [Tooltip("La candela spenta che il mercante accende sull'altare, e quella accesa.")]
        [SerializeField] private GameObject _candle;
        [SerializeField] private GameObject _candleLit;

        [Header("Animazioni")]
        [SerializeField] private AnimationClip _idle;
        [SerializeField] private AnimationClip _walk;
        [SerializeField] private AnimationClip _run;
        [SerializeField] private AnimationClip _hold;
        [SerializeField] private AnimationClip _interact;
        [SerializeField] private AnimationClip _wave;
        [SerializeField] private AnimationClip _awaken;
        [SerializeField] private AnimationClip _skeletonWalk;
        [Tooltip("L'osso della mano che tiene la lanterna.")]
        [SerializeField] private string _handSlot = "handslot.r";

        [Header("Luce")]
        [Tooltip("La luna delle scene di notte: senza, spente le candele, non si vedrebbe niente.")]
        [SerializeField] private Color _moonColor = new Color(0.55f, 0.65f, 1f);
        [SerializeField, Min(0f)] private float _moonIntensity = 0.6f;

        private readonly List<GameObject> _props = new List<GameObject>();
        private readonly List<Renderer> _heroRenderers = new List<Renderer>();
        private readonly List<Light> _heroLights = new List<Light>();
        private DayCycleView _daylight;
        private DayMoment? _held;
        private NightLight[] _candles = new NightLight[0];
        private Hearth _hearth;
        private Villager[] _people = new Villager[0];
        private Villager[] _survivors = new Villager[0];
        private GameObject _merchant;
        private Transform[] _graves = new Transform[0];
        private GameObject[] _dug = new GameObject[0];
        private FilmActor[] _risen = new FilmActor[0];
        private FilmActor[] _fleeing = new FilmActor[0];
        private Transform[] _carried = new Transform[0];
        private Vector3[][] _paths = new Vector3[0][];
        private GameObject[] _resting = new GameObject[0];
        private FilmActor _knight;
        private FilmActor _seller;
        private GameObject _wax;
        private GameObject _flame;
        private Light _candleLight;
        private Light _moon;

        private Vector3 _crypt;
        private Vector3 _arch;
        private Vector3 _fire;
        private Vector3 _cave;
        private Vector3 _spawn;
        private Vector3 _facing;
        private Transform _altar;
        private Vector3 _approach;
        private Vector3 _gameplayTarget;
        private float _gameplayYaw;
        private float _gameplayPitch;
        private float _gameplaySize;

        public override FilmScript Script => FilmLibrary.Opening;

        protected override void Stage(LevelContext level, GameObject hero, CinemachineCamera gameplay)
        {
            var roots = level.gameObject.scene.GetRootGameObjects();
            var map = level.Map;
            _crypt = Marker(map, 'Q');
            _arch = Marker(map, 'a');
            _fire = Marker(map, 'f');
            _cave = Marker(map, '>');
            _daylight = level.Daylight;
            _hearth = roots.SelectMany(r => r.GetComponentsInChildren<Hearth>(true)).FirstOrDefault();
            if (_hearth != null)
            {
                _fire = _hearth.transform.position;
            }

            // le candele dalla cripta verso la piazza: il buio parte dai morti
            _candles = roots.SelectMany(r => r.GetComponentsInChildren<NightLight>(true))
                .OrderBy(l => Vector3.Distance(l.transform.position, _crypt)).ToArray();
            _people = roots.SelectMany(r => r.GetComponentsInChildren<Villager>(true)).ToArray();
            _survivors = _hearth != null ? _hearth.GetComponentsInChildren<Villager>(true) : new Villager[0];
            // il mercante del banco è un modello fermo, senza Villager: si nasconde il modello
            var seller = level.Merchant != null ? level.Merchant.GetComponentInChildren<Animator>(true) : null;
            _merchant = seller != null ? seller.gameObject : null;

            _spawn = hero.transform.position;
            _facing = hero.transform.forward;
            _gameplayTarget = _spawn;
            _gameplayYaw = gameplay.transform.eulerAngles.y;
            _gameplayPitch = gameplay.transform.eulerAngles.x;
            _gameplaySize = gameplay.Lens.OrthographicSize;
            _knight = FilmActor.Clone(hero, transform, "Knight", _idle, _walk);
            HideHero(hero);

            StageGraves(roots);
            StageVillagers();
            StageAltar(level);
            _moon = new GameObject("Moon").AddComponent<Light>();
            _moon.transform.SetParent(transform, false);
            _moon.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            _moon.type = LightType.Directional;
            _moon.color = _moonColor;
            _moon.intensity = _moonIntensity;
            _moon.shadows = LightShadows.Soft;
            _moon.enabled = false;
        }

        protected override void Pose(float time)
        {
            time = Mathf.Clamp(time, 0f, Script.Duration - 0.001f);
            if (!Script.CameraSpan(time, out string camera, out float start, out float end))
            {
                return;
            }

            float t = time - start;
            float k = t / (end - start);
            switch (camera)
            {
                case FilmLibrary.NightCamera:
                    Night(t, k);
                    break;
                case FilmLibrary.GravesCamera:
                    Rising(t, k);
                    break;
                case FilmLibrary.DawnCamera:
                    Dawn(k);
                    break;
                case FilmLibrary.ArrivalCamera:
                    Arrival(t, k);
                    break;
                case FilmLibrary.AltarCamera:
                    Altar(t, k);
                    break;
                default:
                    Title(camera == FilmLibrary.DescentCamera ? 1f : 0f, k);
                    break;
            }
        }

        protected override void Strike()
        {
            foreach (var candle in _candles)
            {
                if (candle != null)
                {
                    candle.SetFlame(1f);
                }
            }

            if (_hearth != null)
            {
                _hearth.SetBurning(true);
            }

            foreach (var person in _people)
            {
                if (person != null)
                {
                    person.gameObject.SetActive(true);
                }
            }

            if (_merchant != null)
            {
                _merchant.SetActive(true);
            }

            foreach (var grave in _graves)
            {
                if (grave != null)
                {
                    grave.gameObject.SetActive(true);
                }
            }

            foreach (var prop in _props)
            {
                if (prop != null)
                {
                    Destroy(prop);
                }
            }

            _props.Clear();
            foreach (var part in _heroRenderers)
            {
                if (part != null)
                {
                    part.enabled = true;
                }
            }

            foreach (var light in _heroLights)
            {
                if (light != null)
                {
                    light.enabled = true;
                }
            }

            if (_daylight != null)
            {
                _daylight.Release();
            }

            _held = null;
        }

        // 1. il borgo di notte: le candele si spengono una a una, poi il fuoco
        private void Night(float t, float k)
        {
            Hold(DayMoment.Night);
            Cast(people: false, dug: false);
            _moon.enabled = true;
            float spacing = 6f / Mathf.Max(1, _candles.Length);
            for (int i = 0; i < _candles.Length; i++)
            {
                _candles[i].SetFlame(1f - Mathf.Clamp01((t - 2.5f - i * spacing) / SnuffSeconds));
            }

            Burn(t < 9.5f);
            Frame(Vector3.Lerp(_fire, _arch, 0.3f) + Vector3.forward * (3f * k), 10f, 40f, Mathf.Lerp(19f, 16f, Ease(k)));
        }

        // 2. il cimitero: gli scheletri escono dalla terra e i paesani scappano con le lanterne
        private void Rising(float t, float k)
        {
            Hold(DayMoment.Night);
            Cast(people: false, dug: true, rising: true);
            _moon.enabled = true;
            Snuff();
            Burn(false);
            for (int i = 0; i < _risen.Length; i++)
            {
                var grave = _graves[i];
                Vector3 rest = grave.position - grave.forward * 0.5f;
                float wake = 1.5f + i * 1.3f;
                float awake = Mathf.Max(0f, t - wake);
                float walking = awake - _awaken.length;
                if (walking <= 0f)
                {
                    _risen[i].Place(rest, -grave.forward);
                    _risen[i].Pose(_awaken, awake);
                    continue;
                }

                Vector3 toward = (_arch - rest).normalized;
                _risen[i].Place(rest + toward * (walking * SkeletonSpeed), Vector3.Slerp(-grave.forward, toward, walking / BlendSeconds));
                _risen[i].Pose(_awaken, awake, _skeletonWalk, walking, walking / BlendSeconds);
            }

            for (int i = 0; i < _fleeing.Length; i++)
            {
                float flee = t - (5.2f + i * 0.4f);
                var path = _paths[i];
                if (flee <= 0f)
                {
                    // guardano la cripta e le tombe attorno, con le spalle all'arco
                    _fleeing[i].Place(path[0], (GravesCenter() - path[0]).normalized + Vector3.forward);
                    _fleeing[i].Pose(_idle, t + i);
                }
                else
                {
                    Vector3 along = Along(path, flee * RunSpeed, out Vector3 heading);
                    _fleeing[i].Place(along, heading);
                    _fleeing[i].Pose(_idle, t + i, _run, flee, flee / BlendSeconds);
                }

                Hang(_carried[i], _fleeing[i].transform);
            }

            // da sud, sopra l'arco: le spalle dei paesani, gli scheletri che si alzano; poi scappano verso di noi
            Frame(_arch + Vector3.forward * Mathf.Lerp(6.5f, 3.5f, Ease(k)), 0f, 42f, Mathf.Lerp(6.2f, 8.5f, Ease(k)));
        }

        // 3. l'alba: i sopravvissuti attorno al fuoco spento, con le lanterne posate
        private void Dawn(float k)
        {
            Hold(DayMoment.Dawn);
            Cast(people: false, dug: true, survivors: true, resting: true);
            _moon.enabled = false;
            Snuff();
            Burn(false);
            Frame(_fire + Vector3.up * 0.6f, Mathf.Lerp(25f, 55f, k), 28f, Mathf.Lerp(5.2f, 4.6f, Ease(k)));
        }

        // 4. arriva il cavaliere, dalla strada a sud della piazza
        private void Arrival(float t, float k)
        {
            Hold(DayMoment.Day);
            Cast(people: true, dug: true, knight: true);
            _moon.enabled = false;
            Relight();
            Burn(true);
            Vector3 from = _spawn + Vector3.back * 9f;
            float walked = t * WalkSpeed;
            float distance = Vector3.Distance(from, _spawn);
            _knight.Place(Vector3.MoveTowards(from, _spawn, walked), _spawn - from);
            float stopped = Mathf.Max(0f, (walked - distance) / WalkSpeed);
            _knight.Pose(_walk, t, _idle, stopped, stopped / BlendSeconds);
            Frame(_knight.transform.position + Vector3.forward * 1.5f + Vector3.up, 200f, 30f, Mathf.Lerp(6.5f, 6f, k));
        }

        // 5. il mercante accende l'ultima candela sull'altare e indica la grotta al cavaliere
        private void Altar(float t, float k)
        {
            Hold(DayMoment.Day);
            Cast(people: true, dug: true, knight: true, seller: true);
            _moon.enabled = false;
            Relight();
            Burn(true);

            float lit = Mathf.Clamp01((t - 2.2f) / 0.5f);
            _wax.SetActive(lit <= 0f);
            _flame.SetActive(lit > 0f);
            _candleLight.intensity = CandleLight * lit;

            Vector3 toAltar = _altar.position - _approach;
            Vector3 toCave = _cave - _approach;
            float pointing = t - 3.6f;
            _seller.Place(_approach, pointing > 0f ? Vector3.Slerp(toAltar, toCave, pointing / 0.6f) : toAltar);
            if (pointing > 0f)
            {
                _seller.Pose(_interact, t - 1f, _wave, pointing, pointing / BlendSeconds);
            }
            else
            {
                _seller.Pose(_idle, t, _interact, t - 1f, Mathf.Clamp01((t - 1f) / BlendSeconds));
            }

            // il cavaliere arriva dalla piazza e si ferma davanti al mercante
            Vector3 near = _approach + (_spawn - _approach).normalized * 3.2f;
            Vector3 from = Vector3.Lerp(_spawn, near, 0.5f);
            float distance = Vector3.Distance(from, near);
            float walked = t * WalkSpeed;
            float stopped = Mathf.Max(0f, (walked - distance) / WalkSpeed);
            _knight.Place(Vector3.MoveTowards(from, near, walked), near - from);
            _knight.Pose(_walk, t, _idle, stopped, stopped / BlendSeconds);

            Frame(Vector3.Lerp(_approach, _altar.position, 0.4f) + Vector3.up, 40f, 30f, Mathf.Lerp(5f, 4.3f, Ease(k)));
        }

        // 6 e 7. il titolo sopra la grotta, poi la camera scende sul cavaliere dove si gioca
        private void Title(float descent, float k)
        {
            Hold(DayMoment.Day);
            Cast(people: true, dug: true, knight: true);
            _moon.enabled = false;
            Relight();
            Burn(true);
            _wax.SetActive(false);
            _flame.SetActive(false);
            _candleLight.intensity = 0f;
            _knight.Place(_spawn, _facing);
            _knight.Pose(_idle, k * 4f);

            Vector3 cave = _cave + Vector3.back * 3f;
            if (descent <= 0f)
            {
                Frame(cave, 0f, 28f, Mathf.Lerp(7.5f, 6.8f, Ease(k)));
                return;
            }

            float e = Ease(k);
            Frame(Vector3.Lerp(cave, _gameplayTarget, e), Mathf.LerpAngle(0f, _gameplayYaw, e), Mathf.Lerp(28f, _gameplayPitch, e),
                Mathf.Lerp(6.8f, _gameplaySize, e));
        }

        // chi si vede: gli abitanti del gioco, le comparse, la terra smossa
        private void Cast(bool people, bool dug, bool rising = false, bool survivors = false, bool resting = false,
            bool knight = false, bool seller = false)
        {
            foreach (var person in _people)
            {
                bool shown = people || survivors && _survivors.Contains(person);
                if (person.gameObject.activeSelf != shown)
                {
                    person.gameObject.SetActive(shown);
                }
            }

            if (_merchant != null)
            {
                _merchant.SetActive(people && !seller);
            }

            for (int i = 0; i < _graves.Length; i++)
            {
                _graves[i].gameObject.SetActive(!dug);
                _dug[i].SetActive(dug);
            }

            foreach (var actor in _risen)
            {
                actor.Show(rising);
            }

            foreach (var actor in _fleeing)
            {
                actor.Show(rising);
            }

            foreach (var lantern in _resting)
            {
                lantern.SetActive(resting);
            }

            _knight.Show(knight);
            if (_seller != null)
            {
                _seller.Show(seller);
            }
        }

        private void Hold(DayMoment moment)
        {
            if (_held == moment || _daylight == null)
            {
                return;
            }

            _held = moment;
            _daylight.Hold(DayCycle.Start(moment) + 20f);
        }

        private void Snuff()
        {
            foreach (var candle in _candles)
            {
                candle.SetFlame(0f);
            }
        }

        private void Relight()
        {
            foreach (var candle in _candles)
            {
                candle.SetFlame(1f);
            }
        }

        private void Burn(bool burning)
        {
            if (_hearth != null && _hearth.IsBurning != burning)
            {
                _hearth.SetBurning(burning);
            }
        }

        // le tre tombe più vicine alla cripta, smosse, con uno scheletro sdraiato nella terra di ognuna
        private void StageGraves(GameObject[] roots)
        {
            _graves = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .Where(t => t.name == "Grave" || t.name == "Grave(Clone)")
                .OrderBy(t => Vector3.Distance(t.position, _crypt)).Take(Graves).ToArray();
            _dug = new GameObject[_graves.Length];
            _risen = new FilmActor[_graves.Length];
            for (int i = 0; i < _graves.Length; i++)
            {
                _dug[i] = Prop(Instantiate(_graveDug, _graves[i].position, _graves[i].rotation));
                _dug[i].SetActive(false);
                _risen[i] = FilmActor.Clone(_skeletons[i % _skeletons.Length], transform, $"Risen_{i + 1}", _awaken, _skeletonWalk);
                _risen[i].Show(false);
            }
        }

        // i paesani del fuoco, con una lanterna in mano nel cimitero e posata a terra all'alba
        private void StageVillagers()
        {
            // sulla strada del cimitero, tra le tombe, qualche metro oltre l'arco
            Vector3[] starts = { new Vector3(-1.1f, 0f, 5.2f), new Vector3(1.4f, 0f, 6f), new Vector3(0.2f, 0f, 7.6f) };
            int count = Mathf.Min(starts.Length, _survivors.Length);
            _fleeing = new FilmActor[count];
            _carried = new Transform[count];
            _paths = new Vector3[count][];
            _resting = new GameObject[count];
            for (int i = 0; i < count; i++)
            {
                _fleeing[i] = FilmActor.Clone(_survivors[i].gameObject, transform, $"Fleeing_{i + 1}", _idle, _run);
                _carried[i] = Instantiate(_lantern).transform;
                foreach (var glow in _carried[i].GetComponentsInChildren<Light>())
                {
                    glow.intensity *= CarriedLight;
                    glow.range *= 1.5f;
                }

                _fleeing[i].Carry(_hold, _carried[i], _handSlot);
                _fleeing[i].Show(false);
                float side = starts[i].x;
                _paths[i] = new[]
                {
                    _arch + starts[i],
                    _arch + new Vector3(side * 0.4f, 0f, 0f),
                    _arch + new Vector3(side * 1.5f, 0f, -9f),
                    _arch + new Vector3(side * 4f, 0f, -22f),
                };

                // all'alba le lanterne sono a terra accanto a chi le portava
                float angle = (85f - i * 125f) * Mathf.Deg2Rad;
                var resting = Prop(Instantiate(_lantern));
                resting.transform.position = _fire + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 1.5f;
                float bottom = resting.GetComponentsInChildren<Renderer>().Min(r => r.bounds.min.y);
                resting.transform.position += Vector3.up * (_fire.y - bottom);
                resting.SetActive(false);
                _resting[i] = resting;
            }
        }

        // il mercante davanti all'altare, e la candela sul piano, dal lato di chi guarda
        private void StageAltar(LevelContext level)
        {
            _altar = level.Altar != null ? level.Altar.transform : transform;
            var point = _altar.Find("ApproachPoint");
            _approach = point != null ? point.position : _altar.position + _altar.forward * 1.4f;
            if (_merchant != null)
            {
                _seller = FilmActor.Clone(_merchant, transform, "Merchant", _idle, _interact, _wave);
                _seller.Show(false);
            }

            var model = _altar.Find("Model");
            float top = model != null ? model.GetComponentsInChildren<Renderer>().Max(r => r.bounds.max.y) : _altar.position.y + 1f;
            Vector3 spot = _altar.TransformPoint(new Vector3(0.45f, 0f, 0.35f));
            spot.y = top;
            _wax = Prop(Instantiate(_candle, spot, _altar.rotation));
            _flame = Prop(Instantiate(_candleLit, spot, _altar.rotation));
            _candleLight = new GameObject("CandleLight").AddComponent<Light>();
            _candleLight.transform.position = spot + Vector3.up * 0.4f;
            _candleLight.type = LightType.Point;
            _candleLight.range = 5f;
            _candleLight.color = new Color(1f, 0.72f, 0.4f);
            _candleLight.intensity = 0f;
            Prop(_candleLight.gameObject);
            _wax.SetActive(false);
            _flame.SetActive(false);
        }

        private void HideHero(GameObject hero)
        {
            foreach (var part in hero.GetComponentsInChildren<Renderer>())
            {
                if (part.enabled)
                {
                    part.enabled = false;
                    _heroRenderers.Add(part);
                }
            }

            foreach (var light in hero.GetComponentsInChildren<Light>())
            {
                if (light.enabled)
                {
                    light.enabled = false;
                    _heroLights.Add(light);
                }
            }
        }

        private GameObject Prop(GameObject prop)
        {
            _props.Add(prop);
            return prop;
        }

        // la lanterna pende dritta dalla mano, qualunque cosa faccia il braccio
        private static void Hang(Transform lantern, Transform holder)
        {
            lantern.rotation = Quaternion.LookRotation(holder.forward);
        }

        private Vector3 GravesCenter()
        {
            return _graves.Length > 0 ? _graves.Aggregate(Vector3.zero, (sum, g) => sum + g.position) / _graves.Length : _crypt;
        }

        // il punto a <paramref name="distance"/> metri lungo il percorso, e la direzione in cui si va
        private static Vector3 Along(Vector3[] path, float distance, out Vector3 heading)
        {
            heading = path[1] - path[0];
            for (int i = 1; i < path.Length; i++)
            {
                heading = path[i] - path[i - 1];
                float length = heading.magnitude;
                if (distance <= length || i == path.Length - 1)
                {
                    return path[i - 1] + heading.normalized * Mathf.Min(distance, length);
                }

                distance -= length;
            }

            return path[path.Length - 1];
        }

        private static Vector3 Marker(LevelMap map, char symbol)
        {
            foreach (var marker in map.Markers)
            {
                if (marker.Symbol == symbol)
                {
                    return LevelMap.CellCenter(marker.X, marker.Y);
                }
            }

            return Vector3.zero;
        }
    }
}
