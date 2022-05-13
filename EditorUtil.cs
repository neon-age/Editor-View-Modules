#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AV.Toolkit 
{
    public static class EditorUtil
    {
        const BindingFlags AnyBind = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public static Assembly Asm = typeof(Editor).Assembly;

        static FieldInfo globalEventHandlerInfo = typeof(EditorApplication).GetField("globalEventHandler", AnyBind);

        public static void SetGlobalKeyHandler(EditorApplication.CallbackFunction func, bool add)
        {
            var value = (EditorApplication.CallbackFunction)globalEventHandlerInfo.GetValue(null);
            if (add) value += func; else value -= func;
            globalEventHandlerInfo.SetValue(null, value);
        }


        public static void AddOrAssign<K, V>(Dictionary<K, V> lookup, K k, V v)
        {
            if (!lookup.ContainsKey(k)) lookup.Add(k, v); else lookup[k] = v;
        }
        public static V GetOrAdd<K, V>(Dictionary<K, V> lookup, K k, Func<V> createValue)
        {
            if (!lookup.TryGetValue(k, out var v)) v = createValue(); return v;
        }

        public static T FindObject<T>() where T : Object
        {
            return FindObject(typeof(T)) as T;
        }
        public static Object FindObject(Type type)
        {
            var objs = Resources.FindObjectsOfTypeAll(type);
            return objs.Length > 0 ? objs[0] : null;
        }

        public static void Log(params object[] objs)
        {
            var l = "";
            foreach (var o in objs)
                l += o.ToString() + " ";
            Debug.Log(l);
        }

        public static void TryInvoke(Action action)
        {
            try { action?.Invoke(); } catch (Exception ex) { Debug.LogException(ex); }
        }
        public static void TryInvoke<T1>(Action<T1> action, T1 v1)
        {
            try { action?.Invoke(v1); } catch (Exception ex) { Debug.LogException(ex); }
        }

        public static void TryInvoke<TOut>(Func<TOut> action)
        {
            try { action?.Invoke(); } catch (Exception ex) { Debug.LogException(ex); }
        }
        public static void TryInvoke<T1, TOut>(Func<T1, TOut> action, T1 v1)
        {
            try { action?.Invoke(v1); } catch (Exception ex) { Debug.LogException(ex); }
        }
    }
}

#endif