using System.Linq;
using DarkDescent.Levels;
using DarkDescent.Rendering;
using DarkDescent.Town;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;

namespace DarkDescent.Films
{
    /// <summary>
    /// Il secondo tempo della vittoria nel borgo (D8 della M19), dopo il viaggio del portale:
    /// <list type="number">
    /// <item>all'alba il cavaliere esce dal buio della grotta;</item>
    /// <item>scende verso la piazza, i sopravvissuti attorno al fuoco si alzano ed esultano, e le candele
    /// del borgo si riaccendono una a una, dal fuoco verso fuori;</item>
    /// <item>la camera scende sul cavaliere, che è dove si gioca.</item>
    /// </list>
    /// Poi la finestra delle statistiche. Il cavaliere vero aspetta dove la copia si ferma, accanto al
    /// fuoco (D4).
    /// </summary>
    public class VictoryReturnSet : FilmSet
    {
        private const float WalkSpeed = 2.2f;
        private const float BlendSeconds = 0.35f;
        // la copia si ferma a questa distanza dal fuoco, dalla parte della grotta, fuori dal cerchio di chi siede
        private const float StopFromFire = 4.3f;
        // la grotta all'alba piena, la piazza quando il sole esce: la luce sale con il ritorno
        private const float CaveHour = 40f;
        private const float SquareHour = 135f;
        private const float RoadLength = 13f;
        private const float StandAt = 3f;
        private const float RelightAt = 3.5f;
        private const float RelightSeconds = 5f;
        private const float FlameSeconds = 0.5f;

        [Header("Animazioni")]
        [SerializeField] private AnimationClip _idle;
        [SerializeField] private AnimationClip _walk;
        [SerializeField] private AnimationClip _standUp;
        [SerializeField] private AnimationClip _cheer;

        private DayCycleView _daylight;
        private float _held = -1f;
        private Hearth _hearth;
        private NightLight[] _candles = new NightLight[0];
        private Villager[] _survivors = new Villager[0];
        private FilmActor[] _cheering = new FilmActor[0];
        private AnimationClip[] _sitting = new AnimationClip[0];
        private Vector3[] _seats = new Vector3[0];
        private Vector3[] _facings = new Vector3[0];
        private FilmActor _knight;
        private GameObject _hero;

        private Vector3 _cave;
        private Vector3 _fire;
        private Vector3 _stop;
        private float _gameplayYaw;
        private float _gameplayPitch;
        private float _gameplaySize;

        public override FilmScript Script => FilmLibrary.VictoryReturn;

        protected override void Stage(LevelContext level, GameObject hero, CinemachineCamera gameplay)
        {
            var roots = level.gameObject.scene.GetRootGameObjects();
            _cave = Marker(level.Map, '>');
            _fire = Marker(level.Map, 'f');
            _daylight = level.Daylight;
            _hearth = roots.SelectMany(r => r.GetComponentsInChildren<Hearth>(true)).FirstOrDefault();
            if (_hearth != null)
            {
                _fire = _hearth.transform.position;
                _survivors = _hearth.GetComponentsInChildren<Villager>(true);
            }

            // le candele dal fuoco verso fuori: la luce riparte da chi è rimasto
            _candles = roots.SelectMany(r => r.GetComponentsInChildren<NightLight>(true))
                .OrderBy(l => Vector3.Distance(l.transform.position, _fire)).ToArray();

            _gameplayYaw = gameplay.transform.eulerAngles.y;
            _gameplayPitch = gameplay.transform.eulerAngles.x;
            _gameplaySize = gameplay.Lens.OrthographicSize;
            _hero = hero;
            _knight = FilmActor.Clone(hero, transform, "Knight", _idle, _walk);
            HideHero(hero);

            _cheering = new FilmActor[_survivors.Length];
            _sitting = new AnimationClip[_survivors.Length];
            _seats = new Vector3[_survivors.Length];
            _facings = new Vector3[_survivors.Length];
            for (int i = 0; i < _survivors.Length; i++)
            {
                var model = _survivors[i].GetComponentInChildren<Animator>(true).transform;
                _sitting[i] = _survivors[i].Pose;
                _cheering[i] = FilmActor.Clone(_survivors[i].gameObject, transform, $"Survivor_{i + 1}", _sitting[i], _standUp, _cheer);
                _seats[i] = model.position;
                _facings[i] = model.forward;
                _survivors[i].gameObject.SetActive(false);
            }

            _stop = Stop();
        }

        protected override void Pose(float time)
        {
            time = Mathf.Clamp(time, 0f, Script.Duration - 0.001f);
            if (!Script.CameraSpan(time, out string camera, out float start, out float end))
            {
                return;
            }

            Hold(camera == FilmLibrary.TunnelCamera ? CaveHour : SquareHour);
            if (_hearth != null && !_hearth.IsBurning)
            {
                _hearth.SetBurning(true);
            }

            float t = time - start;
            float k = t / (end - start);
            switch (camera)
            {
                case FilmLibrary.TunnelCamera:
                    Tunnel(t, k);
                    break;
                case FilmLibrary.SquareCamera:
                    Square(t, k);
                    break;
                default:
                    Home(k);
                    break;
            }
        }

        protected override void Strike()
        {
            foreach (var survivor in _survivors)
            {
                if (survivor != null)
                {
                    survivor.gameObject.SetActive(true);
                }
            }

            foreach (var candle in _candles)
            {
                if (candle != null)
                {
                    candle.SetFlame(1f);
                }
            }

            if (_daylight != null)
            {
                _daylight.Release();
            }

            _held = -1f;

            // il cavaliere vero dove si è fermata la copia, girato verso il fuoco
            if (_hero != null)
            {
                if (_hero.TryGetComponent(out NavMeshAgent agent) && agent.enabled && agent.isOnNavMesh)
                {
                    agent.Warp(_stop);
                }
                else
                {
                    _hero.transform.position = _stop;
                }

                Vector3 toFire = _fire - _stop;
                toFire.y = 0f;
                if (toFire.sqrMagnitude > 0.01f)
                {
                    _hero.transform.rotation = Quaternion.LookRotation(toFire);
                }
            }
        }

        // 1. all'alba il cavaliere esce dal buio della grotta, verso di noi
        private void Tunnel(float t, float k)
        {
            Relight(0f);
            Survivors(0f);
            Vector3 from = _cave + Vector3.forward * 2.5f;
            Vector3 to = _cave + Vector3.back * 4f;
            float walking = Mathf.Max(0f, t - 0.6f);
            _knight.Place(Vector3.MoveTowards(from, to, walking * WalkSpeed), to - from);
            _knight.Pose(_idle, t, _walk, walking, walking / BlendSeconds);
            Frame(_cave + Vector3.back * 1.5f + Vector3.up * 1.1f, 0f, 12f, Mathf.Lerp(3.4f, 3.9f, Ease(k)));
        }

        // 2. scende verso la piazza: i sopravvissuti si alzano ed esultano, le candele si riaccendono
        private void Square(float t, float k)
        {
            Relight(t);
            Survivors(t);
            Vector3 from = _stop + (_cave - _stop).normalized * RoadLength;
            float walked = t * WalkSpeed;
            float stopped = Mathf.Max(0f, (walked - RoadLength) / WalkSpeed);
            _knight.Place(Vector3.MoveTowards(from, _stop, walked), _stop - from);
            _knight.Pose(_walk, t, _idle, stopped, stopped / BlendSeconds);
            Frame(Vector3.Lerp(_knight.transform.position, _fire, 0.55f) + Vector3.up, -25f, 30f, Mathf.Lerp(7.5f, 6f, Ease(k)));
        }

        // 3. la camera scende sul cavaliere fermo accanto al fuoco, fino a diventare quella del gioco
        private void Home(float k)
        {
            float square = FilmLibrary.VictoryReturn.Shots.Where(s => s.Camera == FilmLibrary.SquareCamera).Sum(s => s.Duration);
            Relight(square + k * 3f);
            Survivors(square + k * 3f);
            _knight.Place(_stop, _fire - _stop);
            _knight.Pose(_idle, k * 3f);
            Vector3 last = Vector3.Lerp(_stop, _fire, 0.55f) + Vector3.up;
            float e = Ease(k);
            Frame(Vector3.Lerp(last, _stop, e), Mathf.LerpAngle(-25f, _gameplayYaw, e), Mathf.Lerp(30f, _gameplayPitch, e), Mathf.Lerp(6f, _gameplaySize, e));
        }

        // dove si ferma il cavaliere: dalla parte della grotta, nel varco più largo tra chi siede attorno al fuoco
        private Vector3 Stop()
        {
            Vector3 toCave = _cave - _fire;
            toCave.y = 0f;
            Vector3 best = _fire + toCave.normalized * StopFromFire;
            float widest = -1f;
            for (float angle = -60f; angle <= 60f; angle += 10f)
            {
                Vector3 spot = _fire + Quaternion.Euler(0f, angle, 0f) * toCave.normalized * StopFromFire;
                float nearest = _seats.Length > 0 ? _seats.Min(seat => Vector3.Distance(seat, spot)) : float.MaxValue;
                if (nearest > widest + 0.3f)
                {
                    widest = nearest;
                    best = spot;
                }
            }

            return best;
        }

        // seduti attorno al fuoco; al secondo StandAt della piazza si alzano, poi esultano verso il cavaliere
        private void Survivors(float t)
        {
            for (int i = 0; i < _cheering.Length; i++)
            {
                float stand = t - StandAt - i * 0.25f;
                float cheer = stand - _standUp.length;
                _cheering[i].Place(_seats[i], Vector3.Slerp(_facings[i], _stop - _seats[i], Mathf.Clamp01(cheer / 0.6f)));
                if (stand <= 0f)
                {
                    _cheering[i].Pose(_sitting[i], t);
                }
                else if (cheer <= 0f)
                {
                    _cheering[i].Pose(_standUp, stand);
                }
                else
                {
                    _cheering[i].Pose(_standUp, stand, _cheer, cheer, cheer / BlendSeconds);
                }
            }
        }

        // le candele spente fino al secondo RelightAt della piazza, poi una a una
        private void Relight(float t)
        {
            float spacing = RelightSeconds / Mathf.Max(1, _candles.Length);
            for (int i = 0; i < _candles.Length; i++)
            {
                _candles[i].SetFlame(Mathf.Clamp01((t - RelightAt - i * spacing) / FlameSeconds));
            }
        }

        // l'ora del borgo, in secondi dall'inizio dell'alba
        private void Hold(float sinceDawn)
        {
            if (Mathf.Approximately(_held, sinceDawn) || _daylight == null)
            {
                return;
            }

            _held = sinceDawn;
            _daylight.Hold(DayCycle.Start(DayMoment.Dawn) + sinceDawn);
        }
    }
}
