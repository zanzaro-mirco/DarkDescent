using System.Linq;
using DarkDescent.Levels;
using DarkDescent.Town;
using Unity.Cinemachine;
using UnityEngine;

namespace DarkDescent.Films
{
    /// <summary>
    /// Il secondo tempo della vittoria nella sala (D8 della M19): il cavaliere, davanti al portale, guarda
    /// il mucchio d'ossa dov'era il re, si gira ed entra. Il viaggio verso il borgo parte alla fine del
    /// filmato, o al salto. Il cavaliere è la copia del tuo, con quello che porti.
    /// </summary>
    public class VictoryLeaveSet : FilmSet
    {
        private const float WalkSpeed = 2.2f;
        private const float BlendSeconds = 0.35f;
        private const float LookSeconds = 2.2f;
        private const float TurnSeconds = 0.5f;
        // da qui al portale: abbastanza per vederlo andare
        private const float FromPortal = 3.2f;

        [Header("Animazioni")]
        [SerializeField] private AnimationClip _idle;
        [SerializeField] private AnimationClip _walk;

        private FilmActor _knight;
        private Vector3 _start;
        private Vector3 _bones;
        private Vector3 _portal;

        public override FilmScript Script => FilmLibrary.VictoryLeave;

        protected override void Stage(LevelContext level, GameObject hero, CinemachineCamera gameplay)
        {
            var portals = FindFirstObjectByType<TownPortals>();
            _start = hero.transform.position;
            _portal = portals != null && portals.VictoryPortal != null ? portals.VictoryPortal.transform.position : _start + hero.transform.forward;

            // le ossa del re, se il primo tempo le ha lasciate; sennò dove stava il re, dietro le spalle
            var remains = FindObjectsByType<FilmActor>(FindObjectsSortMode.None).FirstOrDefault(a => a.name == VictoryFallSet.RemainsName);
            _bones = remains != null ? remains.transform.position : _start + (_start - _portal).normalized * 4f;

            // qualche passo indietro dal portale, verso le ossa: si gira e lo si vede andare
            Vector3 back = _bones - _portal;
            back.y = 0f;
            if (back.sqrMagnitude > 0.01f)
            {
                _start = _portal + back.normalized * Mathf.Min(FromPortal, back.magnitude * 0.6f);
            }

            _knight = FilmActor.Clone(hero, transform, "Knight", _idle, _walk);
            HideHero(hero);
        }

        protected override void Pose(float time)
        {
            time = Mathf.Clamp(time, 0f, Script.Duration);
            float k = time / Script.Duration;
            Vector3 toBones = _bones - _start;
            Vector3 toPortal = _portal - _start;
            float distance = new Vector3(toPortal.x, 0f, toPortal.z).magnitude;
            float turning = Mathf.Clamp01((time - LookSeconds) / TurnSeconds);
            float walking = Mathf.Max(0f, time - LookSeconds - TurnSeconds);
            float walked = Mathf.Min(distance, walking * WalkSpeed);

            // dentro il portale non si vede più
            _knight.Show(walked < distance - 0.2f);
            _knight.Place(Vector3.MoveTowards(_start, _portal, walked), Vector3.Slerp(toBones, toPortal, Ease(turning)));
            if (walking > 0f)
            {
                _knight.Pose(_idle, time, _walk, walking, walking / BlendSeconds);
            }
            else
            {
                _knight.Pose(_idle, time);
            }

            // di fianco alla linea tra le ossa e il portale: si vedono tutti e due, e il cavaliere in mezzo
            Vector3 middle = Vector3.Lerp(_bones, _portal, 0.6f) + Vector3.up;
            Vector3 line = _portal - _bones;
            float yaw = line.sqrMagnitude > 0.01f ? Quaternion.LookRotation(line).eulerAngles.y - 90f : 45f;
            Frame(middle, yaw, 32f, Mathf.Lerp(4.4f, 4f, Ease(k)));
        }

        protected override void Strike()
        {
        }
    }
}
