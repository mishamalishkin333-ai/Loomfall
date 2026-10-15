using UnityEngine;

namespace Loomfall.EditorTools
{
    /// <summary>
    /// Ткач как объёмная фигура с настоящими шарнирами и точными размерами.
    /// Модель characterMedium.fbx для этого не годится: её меш — плоская
    /// пластина (локальные габариты 0,02 × 0,00 × 0,02 м), которую кости
    /// растягивают как хотят, а клипы idle/run/jump каждый кадр выписывают
    /// в корень масштаб 100. Из-за этого Ткач то вырастал до 46 метров, то
    /// сжимался до сантиметров, камера тонула в его коже, а игрок не видел
    /// аватара вовсе. Здесь каждый сантиметр задан кодом и портиться не может.
    /// </summary>
    public static class LoomfallWeaverFigure
    {
        /// <summary>Собирает фигуру внутрь узла героя и возвращает её корень.</summary>
        public static GameObject Build(GameObject holder, Material robe, Material hem, Material trim, Material skin, Material glow)
        {
            var root = new GameObject("Ткач");
            root.transform.SetParent(holder.transform, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            // Таз на 0,98 м: от него считаются все остальные части.
            Transform hips = Joint(root.transform, "Таз", new Vector3(0f, 0.98f, 0f));

            Transform legLeft = Joint(hips, "НогаЛевая", new Vector3(-0.14f, -0.02f, 0f));
            Leg(legLeft, robe, trim);
            Transform legRight = Joint(hips, "НогаПравая", new Vector3(0.14f, -0.02f, 0f));
            Leg(legRight, robe, trim);

            // Роба: пояс и подол двумя цилиндрами — силуэт читается даже издалека.
            Part(hips, PrimitiveType.Cylinder, "ПоясРобы", new Vector3(0f, 0.06f, 0f), new Vector3(0.6f, 0.5f, 0.52f), robe);
            Part(hips, PrimitiveType.Cylinder, "Подол", new Vector3(0f, -0.38f, 0f), new Vector3(0.82f, 0.6f, 0.74f), hem);
            Part(hips, PrimitiveType.Cylinder, "Кушак", new Vector3(0f, 0.02f, 0f), new Vector3(0.64f, 0.1f, 0.56f), trim);

            Transform torso = Part(hips, PrimitiveType.Capsule, "Грудь", new Vector3(0f, 0.24f, 0f), new Vector3(0.52f, 0.28f, 0.42f), robe).transform;
            Part(hips, PrimitiveType.Sphere, "Плечи", new Vector3(0f, 0.4f, 0f), new Vector3(0.6f, 0.32f, 0.48f), robe);
            Part(hips, PrimitiveType.Cylinder, "Шея", new Vector3(0f, 0.48f, 0f), new Vector3(0.17f, 0.12f, 0.17f), skin);

            // Голова так, чтобы макушка встала на 1,75 м: рост договорной.
            Transform head = Part(hips, PrimitiveType.Sphere, "Голова", new Vector3(0f, 0.575f, 0f), new Vector3(0.33f, 0.38f, 0.33f), skin).transform;
            // Капюшон сдвигаем назад: лицо остаётся открытым, силуэт — в плаще.
            Transform hood = Part(hips, PrimitiveType.Sphere, "Капюшон", new Vector3(0f, 0.585f, -0.085f), Vector3.one * 0.42f, robe).transform;

            Transform armLeft = Joint(hips, "РукаЛевая", new Vector3(-0.3f, 0.34f, 0f));
            Sleeve(armLeft, robe, skin, out _);
            Transform armRight = Joint(hips, "РукаПравая", new Vector3(0.3f, 0.34f, 0f));
            Sleeve(armRight, robe, skin, out Transform handRight);

            // Нить в правой руке: точка света, по которой в темноте видно Ткача.
            GameObject orb = Part(handRight, PrimitiveType.Sphere, "Нить", new Vector3(0f, -0.04f, 0.14f), Vector3.one * 0.15f, glow);
            var light = orb.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.55f, 0.85f, 1f);
            light.range = 4.5f;
            light.intensity = 2.5f;
            light.shadows = LightShadows.None;

            var avatar = root.AddComponent<Loomfall.WeaverAvatar>();
            avatar.hips = hips;
            avatar.torso = torso;
            avatar.head = head;
            avatar.hood = hood;
            avatar.armLeft = armLeft;
            avatar.armRight = armRight;
            avatar.legLeft = legLeft;
            avatar.legRight = legRight;
            avatar.handRight = handRight;
            avatar.thread = light;

            int parts = root.GetComponentsInChildren<Transform>(true).Length;
            Debug.Log("[Loomfall] Ткач: фигура собрана — узлов " + parts
                + ", рост " + Loomfall.WeaverAvatar.Height + " м, таз на 0,98 м, стопам 0 м");
            return root;
        }

        private static void Leg(Transform joint, Material robe, Material trim)
        {
            // Нога висит с шарнира: капсула 0,86 м вниз, стопа на земле.
            Part(joint, PrimitiveType.Capsule, "Голень", new Vector3(0f, -0.43f, 0f), new Vector3(0.17f, 0.43f, 0.17f), robe);
            Part(joint, PrimitiveType.Cube, "Стопа", new Vector3(0f, -0.9f, 0.06f), new Vector3(0.19f, 0.11f, 0.31f), trim);
        }

        private static void Sleeve(Transform joint, Material robe, Material skin, out Transform hand)
        {
            Part(joint, PrimitiveType.Capsule, "Рукав", new Vector3(0f, -0.23f, 0f), new Vector3(0.15f, 0.23f, 0.15f), robe);
            hand = Part(joint, PrimitiveType.Sphere, "Кисть", new Vector3(0f, -0.48f, 0f), Vector3.one * 0.16f, skin).transform;
        }

        private static Transform Joint(Transform parent, string name, Vector3 localPosition)
        {
            var node = new GameObject(name);
            node.transform.SetParent(parent, false);
            node.transform.localPosition = localPosition;
            node.transform.localRotation = Quaternion.identity;
            node.transform.localScale = Vector3.one;
            return node.transform;
        }

        private static GameObject Part(Transform parent, PrimitiveType type, string name, Vector3 localPosition, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;

            // Коллайдеры примитивов вредят: контроллер ищет пол лучом вниз и
            // натыкался бы на собственные колени Ткача.
            var collider = part.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);

            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = scale;

            var renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (material != null) renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            return part;
        }
    }
}
