using UnityEngine;
using UnityEngine.InputSystem;

namespace Loomfall
{
    /// <summary>Осколок памяти: чужая жизнь. Подняв его, Ткач слышит чужой голос и получает запись.</summary>
    public class MemoryShard : MonoBehaviour
    {
        /// <summary>Записи Кодекса — дословно из сюжетной библии (раздел 7 и правила мира).</summary>
        public static readonly string[] Records =
        {
            "Вахта с шести до шести. Если вода прибыла — не геройствуй, воду не остановишь. Ставлю галочку. Ухожу.",
            "Хлеб, соль, нитки в три цвета, масло для лампы, что-нибудь для девочки, ей семь или восемь.",
            "Это дом. Это я. Это папа. Папа большой и весь из полосок, потому что его много.",
            "Уважаемая библиотека. Стеллаж номер четыре проехал по моей ноге. Нога заживёт, а книга — нет.",
            "День первый: туман на десять метров. День пятый: не помню, что писал вчера. День восьмой: это уже не журнал, это стена.",
            "Не ставь Эхо к огню. Оно тёплое, но если рядом огонь, оно всё стекается туда и уходит.",
            "Взял у Ключа два ключа. Один потерял. Второй тоже потерял, но помню, где: у водостока.",
            "Из тридцати огней горит один. Настоящих, которые держат сектор, — ни одного.",
            "Память — это нитка. Нитка — это мама. Мама — это дом. Дом — это когда помнят.",
            "Меня звали Одд. Больше ничего не надо. Просто знай, что был Одд.",
            "Наковальня — одна. Клещи — четыре. Нить-Коса — одна, лежит двадцать два года, брать никому не даю.",
            "Камень стоит, потому что помнит, что он камень.",
            "Мост держится, пока кто-то помнит, что по нему ходили.",
            "Забвение — не чувство, а число.",
            "Мир ломается ровно там, где он отказывается забыть.",
        };

        public Transform player;
        public int index;
        public float radius = 2.4f;

        public string Record { get; private set; }
        public bool Taken { get; private set; }

        private Vector3 start;
        private Light glow;
        private float spin;
        private float bob;

        private void Awake()
        {
            Record = Rules(index);
            start = transform.position;
            glow = gameObject.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.range = 4.5f;
            glow.intensity = 0.9f;
            glow.color = new Color(0.6f, 0.78f, 1f);
        }

        private void Update()
        {
            spin += 45f * Time.deltaTime;
            bob += Time.deltaTime;
            transform.rotation = Quaternion.Euler(0f, spin, 0f);
            transform.position = start + Vector3.up * (0.12f * Mathf.Sin(bob * 1.6f));

            if (Taken) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || player == null) return;
            if (!keyboard.eKey.wasPressedThisFrame) return;
            if (Vector3.Distance(player.position, transform.position) > radius) return;

            Taken = true;
            glow.enabled = false;
            if (GameSession.Instance != null) GameSession.Instance.CollectShard(Record);
            Debug.Log("[Loomfall] Осколок памяти: " + Record);
        }

        /// <summary>Запись по номеру осколка, по кругу.</summary>
        public static string Rules(int index)
        {
            if (index < 0) index = -index;
            return Records[index % Records.Length];
        }
    }
}