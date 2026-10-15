using UnityEngine;
using UnityEngine.InputSystem;

namespace Loomfall
{
    /// <summary>Слово и Разрыв: магия Ткача. Жжёт Эхо, а Эхо — конденсат памяти, поэтому Забвение растёт быстрее.</summary>
    public class WeaverWord : MonoBehaviour
    {
        public static WeaverWord Instance { get; private set; }

        public float maxEcho = 100f;
        public float echoRegen = 5f;
        public float wordCost = 25f;
        public float riftCost = 40f;

        public float Echo { get; private set; }

        /// <summary>Пробное нажатие для проверок и для кнопок на телефоне.</summary>
        public bool testPress;
        public bool testRift;

        private void Awake()
        {
            Instance = this;
            Echo = maxEcho;
        }

        private void Update()
        {
            Echo = Mathf.Min(maxEcho, Echo + echoRegen * Time.deltaTime);

            Keyboard keyboard = Keyboard.current;
            bool word = testPress;
            bool rift = testRift;
            testPress = false;
            testRift = false;

            if (keyboard != null)
            {
                if (keyboard.qKey.wasPressedThisFrame) word = true;
                if (keyboard.fKey.wasPressedThisFrame) rift = true;
            }

            if (word && Echo >= wordCost)
            {
                Echo -= wordCost;
                Cast("Слово", 3.2f, new Color(0.62f, 0.8f, 1f), 0.05f);
            }

            if (rift && Echo >= riftCost)
            {
                Echo -= riftCost;
                Cast("Разрыв", 6.5f, new Color(1f, 0.72f, 0.42f), 0.11f);
            }
        }

        /// <summary>Слово — вспышка и подсветка нитей, Разрыв — рвёт камень и жжёт память яруса.</summary>
        private void Cast(string name, float radius, Color tint, float oblivionCost)
        {
            var flash = new GameObject("Вспышка-" + name);
            flash.transform.position = transform.position + Vector3.up * 1.2f;
            var source = flash.AddComponent<Light>();
            source.type = LightType.Point;
            source.range = radius;
            source.intensity = 9f;
            source.color = tint;
            flash.AddComponent<LightPulse>();

            if (OblivionClock.Instance != null) OblivionClock.Instance.Burn(oblivionCost);
            if (GameSession.Instance != null) GameSession.Instance.Shout(name);
            Debug.Log("[Loomfall] " + name + ": Эхо " + Mathf.RoundToInt(Echo) + ", память сожжена");
        }
    }

    /// <summary>Вспышка магии: разгорается и гаснет за полсекунды.</summary>
    public class LightPulse : MonoBehaviour
    {
        private Light source;
        private float life;

        private void Awake()
        {
            source = GetComponent<Light>();
        }

        private void Update()
        {
            life += Time.deltaTime;
            if (source != null) source.intensity = Mathf.Lerp(9f, 0f, life / 0.45f);
            if (life > 0.45f) Destroy(gameObject);
        }
    }
}