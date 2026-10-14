using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Loomfall.EditorTools
{
    /// <summary>Анимация Ткача: контроллер с покоем, бегом и прыжком из моделей Quaternius.</summary>
    public static class LoomfallHeroAnim
    {
        private const string Folder = "Assets/Art/Controllers";
        private const string ControllerPath = "Assets/Art/Controllers/Weaver.controller";
        private const string Hero = "Assets/Art/Models/Protagonist";

        /// <summary>Создаёт контроллер с тремя состояниями и переходами по параметрам.</summary>
        public static RuntimeAnimatorController EnsureController()
        {
            AnimationClip idle = LoadClip(Hero + "/idle.fbx");
            AnimationClip run = LoadClip(Hero + "/run.fbx");
            AnimationClip jump = LoadClip(Hero + "/jump.fbx");
            if (idle == null)
            {
                Debug.LogError("[Loomfall] Нет клипа покоя у Ткача");
                return null;
            }

            LoomfallCore.EnsureFolder(Folder);
            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("jump", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idleState = machine.AddState("Покой");
            idleState.motion = idle;
            idleState.speed = 1f;
            machine.defaultState = idleState;

            if (run != null)
            {
                AnimatorState runState = machine.AddState("Бег");
                runState.motion = run;
                runState.speed = 1f;
                idleState.AddTransition(runState).AddCondition(AnimatorConditionMode.Greater, 0.15f, "speed");
                runState.AddTransition(idleState).AddCondition(AnimatorConditionMode.Less, 0.15f, "speed");
            }

            if (jump != null)
            {
                AnimatorState jumpState = machine.AddState("Прыжок");
                jumpState.motion = jump;
                idleState.AddTransition(jumpState).AddCondition(AnimatorConditionMode.If, 0f, "jump");
                jumpState.AddTransition(idleState).AddCondition(AnimatorConditionMode.If, 0f, "jump");
            }

            AssetDatabase.SaveAssets();
            var saved = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            Debug.Log("[Loomfall] Контроллер Ткача: покой " + (idle != null) + ", бег " + (run != null) + ", прыжок " + (jump != null));
            return saved;
        }

        /// <summary>Первый клип внутри FBX с анимацией.</summary>
        private static AnimationClip LoadClip(string path)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                var clip = asset as AnimationClip;
                if (clip != null && !clip.name.StartsWith("__preview__")) return clip;
            }

            return null;
        }
    }
}