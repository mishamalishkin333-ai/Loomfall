using UnityEngine;

namespace Loomfall
{
    /// <summary>
    /// Живая анимация Ткача: шаг, бег, прыжок, дыхание и пульс нити считаются
    /// на каждом кадре по шарнирам фигуры. Такая анимация не зависит ни от
    /// импорта FBX, ни от клипов с чужим масштабом — фигура собрана из
    /// примитивов, и её размеры известны точно до сантиметра.
    /// </summary>
    public class WeaverAvatar : MonoBehaviour
    {
        [Header("Шарниры фигуры")]
        [Tooltip("Таз: корень корпуса, по нему идут покачивание и наклон.")]
        public Transform hips;
        public Transform torso;
        public Transform head;
        public Transform hood;
        public Transform armLeft;
        public Transform armRight;
        public Transform legLeft;
        public Transform legRight;
        public Transform handRight;
        [Tooltip("Свет нити: пульсирует в такт движению.")]
        public Light thread;

        /// <summary>Договор о размере аватара: столько Ткач должен занимать в метрах.</summary>
        public const float Height = 1.75f;

        private Vector3 hipsBase;
        private Vector3 torsoBase;
        private float phase;
        private float speedSmoothed;
        private float lean;
        private float bob;

        private void Awake()
        {
            if (hips != null) hipsBase = hips.localPosition;
            if (torso != null) torsoBase = torso.localScale;
        }

        private void LateUpdate()
        {
            WeaverController weaver = WeaverController.Instance;
            float speed = weaver != null ? weaver.Speed01 : 0f;
            bool airborne = weaver != null && weaver.Airborne;

            // Инерция: между шагом и бегом фигура щёлкает, а перетекает.
            speedSmoothed = Mathf.MoveTowards(speedSmoothed, speed, Time.deltaTime * 3.5f);
            float t = speedSmoothed;

            // Цикл шага: чем быстрее идем, тем чаще мах ноги.
            phase += Time.deltaTime * (0.5f + t * 1.8f);
            float swing = Mathf.Sin(phase * Mathf.PI * 2f);

            if (airborne)
            {
                // В воздухе ноги поджаты, руки подняты: силуэта хватает, чтобы понять прыжок.
                Rotate(legLeft, -26f);
                Rotate(legRight, 14f);
                Rotate(armLeft, -48f);
                Rotate(armRight, -58f);
                lean = Mathf.MoveTowards(lean, 9f, Time.deltaTime * 18f);
                bob = Mathf.MoveTowards(bob, 0.03f, Time.deltaTime * 6f);
            }
            else
            {
                float legAmp = 7f + 30f * t;
                float armAmp = 5f + 22f * t;
                Rotate(legLeft, swing * legAmp);
                Rotate(legRight, -swing * legAmp);
                Rotate(armLeft, -swing * armAmp);
                Rotate(armRight, swing * armAmp);
                lean = Mathf.MoveTowards(lean, 1.5f + 9f * t, Time.deltaTime * 8f);

                // Покачивание таза: в покое почти незаметно, в беге — шаг вверх-вниз.
                float target = Mathf.Abs(swing) * (0.012f + 0.045f * t);
                bob = Mathf.MoveTowards(bob, target, Time.deltaTime * 14f);
            }

            if (hips != null)
            {
                hips.localPosition = new Vector3(
                    hipsBase.x + (airborne ? 0f : swing * 0.014f * t),
                    hipsBase.y + bob,
                    hipsBase.z);
                hips.localRotation = Quaternion.Euler(0f, airborne ? 0f : swing * 3f * t, 0f);
            }

            if (torso != null)
            {
                torso.localRotation = Quaternion.Euler(lean, airborne ? 6f : -swing * 4f * t, 0f);

                // Дыхание: на покое корпус едва заметно ходит ходуном.
                float breath = 1f + Mathf.Sin(Time.time * 1.5f) * 0.014f * (1f - t);
                torso.localScale = new Vector3(torsoBase.x * breath, torsoBase.y, torsoBase.z * breath);
            }

            if (head != null)
            {
                // Голова держит горизонт: наклон корпуса компенсируется шеей.
                Quaternion look = Quaternion.Euler(-lean * 0.65f, swing * 5f * t, 0f);
                head.localRotation = look;
                if (hood != null) hood.localRotation = look;
            }

            if (handRight != null && !airborne)
            {
                handRight.localRotation = Quaternion.Euler(0f, 0f, swing * 12f * t);
            }

            if (thread != null)
            {
                float pulse = 0.85f + Mathf.Sin(Time.time * 2.3f) * 0.3f + t * 0.6f;
                thread.intensity = airborne ? pulse * 1.5f : pulse;
            }
        }

        private static void Rotate(Transform joint, float degrees)
        {
            if (joint != null) joint.localRotation = Quaternion.Euler(degrees, 0f, 0f);
        }
    }
}
