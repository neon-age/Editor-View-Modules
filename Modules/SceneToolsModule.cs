#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AV.Toolkit 
{
    class SceneToolsModule : EditorViewModule
    {
        const BindingFlags AnyBind = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        static Type editorToolGUIType = typeof(Editor).Assembly.GetType("UnityEditor.EditorToolGUI");
        static MethodInfo getToolbarEntryRect = editorToolGUIType.GetMethod("GetToolbarEntryRect", AnyBind);
        static MethodInfo doBuiltinToolbar = editorToolGUIType.GetMethod("DoBuiltinToolbar", AnyBind);
        static MethodInfo doBuiltinToolSettings = editorToolGUIType.GetMethod("DoBuiltinToolSettings", AnyBind, null, new[] { typeof(Rect) }, null);
        static PropertyInfo activeToolSupportsGridSnap = typeof(EditorSnapSettings).GetProperty("activeToolSupportsGridSnap", AnyBind);


        public override IEnumerable<Type> GetTargetTypes() { yield return typeof(SceneView); }

        public override void OnViewRefresh()
        {
            if (MainToolbarsView.isMainToolbarEnabled) return;
            if (SceneView.lastActiveSceneView != view.window) return;
            var root = view.root;

            var param = new object[1];
            var rect = new Rect(0, 0, 230 + 140 + 30, 18);

            var guiContainer = new IMGUIContainer(() =>
            {
                var r = new Rect(rect) { y = -2, width = 224 };
                param[0] = r;

                doBuiltinToolbar.Invoke(null, param);

                r.x += r.width; r.width = 136; r.y = -2; r.height += 4;
                param[0] = r;

                doBuiltinToolSettings.Invoke(null, param);

                r.x += r.width; r.width = 32; // r.y = -2;
                                              //using (new EditorGUI.DisabledScope(!(bool)activeToolSupportsGridSnap.GetValue(null)))
                {
                    var snap = EditorSnapSettings.gridSnapEnabled;
                    var icon = snap ? S.snapToGridIcons[1] : S.snapToGridIcons[0];
                    EditorSnapSettings.gridSnapEnabled = GUI.Toggle(r, snap, icon, S.command);
                }
            })
            { style = { width = rect.width, height = rect.height, overflow = Overflow.Hidden } };

            view.tabDock.Add(guiContainer);
        }

        static class S
        {
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