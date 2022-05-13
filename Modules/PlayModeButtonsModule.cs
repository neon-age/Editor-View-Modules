#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AV.Toolkit 
{
    class PlayModeButtonsModule : EditorViewModule
    {
        const BindingFlags AnyBind = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        static Type gameViewType = EditorUtil.Asm.GetType("UnityEditor.GameView");
        static Type editorToolGUIType = EditorUtil.Asm.GetType("UnityEditor.EditorToolGUI");

        public override IEnumerable<Type> GetTargetTypes() => new Type[] { gameViewType, typeof(SceneView) };

        public override void OnViewRefresh()
        {
            if (MainToolbarsView.isMainToolbarEnabled) return;
            var gameVisible = view.IsAnyWindowVisible(gameViewType);
            if (view.type == typeof(SceneView) && gameVisible)
                return;
            if (!gameVisible && SceneView.lastActiveSceneView != view.window)
                return;

            if (view.type == gameViewType)
            {
                var gameViews = view.GetVisibleViews(gameViewType).ToArray();
                if (gameViews.Length > 1)
                    if (view != gameViews[0])
                        return;
            }

            var root = view.root;
            var rect = new Rect(0, 0, 128, 18);

            var guiContainer = new IMGUIContainer(DrawPlayModeButtons)
            { style = { width = rect.width, height = rect.height, overflow = Overflow.Hidden } };

            view.tabDock.Add(guiContainer);
        }

        static void DrawPlayModeButtons()
        {
            GUILayout.BeginHorizontal();

            var c = GUI.color + new Color(0.01f, 0.01f, 0.01f, 0.01f);
            var contentColor = new Color(1.0f / c.r, 1.0f / c.g, 1.0f / c.g, 1.0f / c.a);
            GUI.contentColor = contentColor;
            GUI.backgroundColor = Color.white;

            var isPlaying = EditorApplication.isPlaying;
            if (isPlaying)
                GUI.backgroundColor = (Color)new Color32(35, 74, 108, 255) * 4;

            isPlaying = GUILayout.Toggle(isPlaying, S.playIcon, S.barButton);
            if (GUI.changed)
                EditorApplication.isPlaying = isPlaying;
            GUI.backgroundColor = Color.white;

            var isPaused = GUILayout.Toggle(EditorApplication.isPaused, S.pauseIcon, S.barButton);
            if (GUI.changed)
                EditorApplication.isPaused = isPaused;

            if (GUILayout.Button(S.stepIcon, S.barButton))
                EditorApplication.Step();

            GUILayout.Space(10);
            GUILayout.EndHorizontal();
        }


        static class S
        {
            public static readonly GUIContent playIcon = EditorGUIUtility.TrIconContent("PlayButton");
            public static readonly GUIContent pauseIcon = EditorGUIUtility.TrIconContent("PauseButton");
            public static readonly GUIContent stepIcon = EditorGUIUtility.TrIconContent("StepButton");

            public static readonly GUIStyle barButton = new GUIStyle(EditorStyles.toolbarButton) { fixedHeight = 18, padding = new RectOffset(8, 8, 0, 0) };
            public static readonly GUIStyle command = "AppCommand";

            public static GUIContent[] snapToGridIcons = new GUIContent[]
            {
            EditorGUIUtility.TrIconContent("SceneViewSnap-Off", "Toggle Grid Snapping on and off. Available when you set tool handle rotation to Global."),
            EditorGUIUtility.TrIconContent("SceneViewSnap-On", "Toggle Grid Snapping on and off. Available when you set tool handle rotation to Global.")
            };
        }
    }
}

#endif