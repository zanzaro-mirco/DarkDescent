using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// La traiettoria di un oggetto che cade, come in Diablo: un arco da dove nasce (il nemico, la
    /// cassa, la mano del cavaliere) a dove si posa, poi un piccolo rimbalzo. Il tempo va da 0 a 1.
    /// </summary>
    public static class DropArc
    {
        /// <summary>La frazione del tempo in cui l'oggetto vola; il resto è il rimbalzo sul posto.</summary>
        public const float LandingTime = 0.78f;

        public static Vector3 Position(Vector3 from, Vector3 to, float height, float bounceHeight, float t)
        {
            t = Mathf.Clamp01(t);
            if (t < LandingTime)
            {
                float u = t / LandingTime;
                Vector3 point = Vector3.Lerp(from, to, u);
                point.y += 4f * height * u * (1f - u);
                return point;
            }

            float v = (t - LandingTime) / (1f - LandingTime);
            Vector3 bounce = to;
            bounce.y += 4f * bounceHeight * v * (1f - v);
            return bounce;
        }

        /// <summary>L'angolo della capriola in gradi: parte da giri interi indietro e arriva a 0 quando tocca terra.</summary>
        public static float Spin(float turns, float t)
        {
            float u = Mathf.Clamp01(t / LandingTime);
            return -turns * 360f * (1f - u);
        }
    }
}
