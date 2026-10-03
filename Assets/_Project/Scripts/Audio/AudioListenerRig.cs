using UnityEngine;

namespace DarkDescent.Audio
{
    /// <summary>
    /// Tiene l'AudioListener sulla testa del player, orientato come la camera. Sulla camera, a 20 m,
    /// l'attenuazione 3D renderebbe tutto quasi muto; figlio del player, girerebbe con lui e destra e
    /// sinistra si scambierebbero a ogni cambio di direzione. Così la destra dello schermo è la
    /// destra delle orecchie.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioListener))]
    public class AudioListenerRig : MonoBehaviour
    {
        [SerializeField] private Transform _target;

        [Tooltip("Da cui prendere l'orientamento orizzontale: la camera.")]
        [SerializeField] private Transform _orientation;

        [Tooltip("Altezza delle orecchie sopra il pivot del target, che sta ai piedi.")]
        [SerializeField, Min(0f)] private float _height = 1.6f;

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            // solo l'imbardata: l'inclinazione della camera isometrica non dice niente all'orecchio
            Quaternion rotation = _orientation != null
                ? Quaternion.Euler(0f, _orientation.eulerAngles.y, 0f)
                : transform.rotation;

            transform.SetPositionAndRotation(_target.position + Vector3.up * _height, rotation);
        }
    }
}
