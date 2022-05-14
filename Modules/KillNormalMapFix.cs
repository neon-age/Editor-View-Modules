#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AV.Toolkit
{
    public class KillNormalMapFix : EditorViewModule
    {
        public override IEnumerable<Type> GetTargetTypes()
        {
            yield return typeof(Editor).Assembly.GetType("UnityEditor.BumpMapSettingsFixingWindow");
        }

        public override void OnViewRefresh()
        {
            view.window.Close();
        }
    }
}
#endif