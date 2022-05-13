
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Ex = System.Exception;

namespace AV.Toolkit 
{
    public abstract class EditorViewModule
    {
        static EditorViewModule activeModule;

        public virtual int priority { get; }
        public View view { get; private set; } // active view

        EditorViewModule SetView(View v) { view = v; return this; }

        IEnumerable<Type> targetTypes;

        public abstract IEnumerable<Type> GetTargetTypes();
        public abstract void OnViewRefresh();
        public virtual void OnRegister() {}
        //public virtual void OnFocusChange(bool focused) {}
        //public virtual void OnVisibilityChange(bool visible) {}

        static Dictionary<int, View> views = new Dictionary<int, View>();
        static List<EditorViewModule> modules = new List<EditorViewModule>();
        static bool wasMaximized;

        static void LogEx(Ex ex) => Debug.LogException(ex);

        [InitializeOnLoadMethod]
        static void Init()
        {
            views.Clear();
            Action<object> onActualViewChanged = OnActualViewChanged;
            R.actualViewChangedInfo.AddMethod.Invoke(null, new object[] { onActualViewChanged });

            EditorApplication.update += OnEditorUpdate;
            EditorApplication.delayCall += RefreshAllModules;
            EditorApplication.playModeStateChanged += OnPlayModeChange;

            foreach (var moduleType in TypeCache.GetTypesDerivedFrom<EditorViewModule>())
            {
                try { CreateViewModule(moduleType); } catch (Ex ex) { LogEx(ex); }
            }
            modules.Sort((x, y) => x.priority.CompareTo(y.priority));
        }
        static void CreateViewModule(Type moduleType)
        {
            var m = (EditorViewModule)Activator.CreateInstance(moduleType);
            m.OnRegister();
            m.targetTypes = m.GetTargetTypes();
            modules.Add(m);
        }


        static EditorWindow focusedWindow;
        static void OnEditorUpdate()
        { 
            if (focusedWindow != EditorWindow.focusedWindow)
            {
                var last = focusedWindow;
                focusedWindow = EditorWindow.focusedWindow;
                var focus = last != focusedWindow;

                TryInitView(last, out var a); TryInitView(focusedWindow, out var b);

                //if (a != null) InvokeViewEvents(a, m => m.OnFocusChange(focus));
                //if (b != null) InvokeViewEvents(b, m => m.OnFocusChange(!focus));

                RefreshAllModules();
            }
        }

        static void OnActualViewChanged(object hostView)
        {
            var w = R.actualView.GetValue(hostView) as EditorWindow;
            if (!w)
                return;

            if (TryInitView(w, out var v)) InvokeViewEvents(v, m => m.OnViewRefresh());

            // Re-init all views, so multiple modules that depend on each other (like SceneTools and PlayModeButtons) get proper rebuild
            RefreshAllModules();
        }

        static void OnPlayModeChange(PlayModeStateChange state)
        {
            //if (state == PlayModeStateChange.EnteredEditMode || state == PlayModeStateChange.EnteredPlayMode)
            //    RefreshAllModules();
        }

        public static void RefreshAllModules()
        {
            foreach (var w in Resources.FindObjectsOfTypeAll<EditorWindow>())
                if (TryInitView(w, out var v))
                    InvokeViewEvents(v, m => m.OnViewRefresh());
        }

        static void InvokeViewEvents(View v, Action<EditorViewModule> action)
        {
            foreach (var m in GetAssignableModules(v))
                try { m.view = v; action(m); } catch (Ex ex) { LogEx(ex); }
        }

        static IEnumerable<EditorViewModule> GetAssignableModules(View v)
        {
            foreach (var m in modules)
                if (IsAssignable(v.type, m.targetTypes))
                    yield return m;
        }
        
        static bool IsAssignable(Type type, IEnumerable<Type> types)
        {
            foreach (var t in types) if (t.IsAssignableFrom(type)) { return true; }
            return false;
        }

        static bool TryInitView(EditorWindow w, out View view)
        {
            view = null;
            if (!w) return false;
            var root = w.rootVisualElement;
            if (root == null) return false;
            if (root.parent == null) return false;

            var id = w.GetInstanceID();
            if (!views.TryGetValue(id, out view))
                views.Add(id, view = new View());

            view.Init(w);
            return true;
        }


        public enum ShowMode { NormalWindow, PopupMenu, Utility, NoShadow, MainWindow, AuxWindow, Tooltip, ModalUtility }

        /// Per-window data
        public class View : IEquatable<View>
        {
            public object containerWindow;
            public object hostView;
            public bool isMaximized;
            public bool isFloating => showMode != ShowMode.MainWindow;
            public ShowMode showMode;
            public int id;
            public Type type;
            public EditorWindow window;
            public VisualElement root;
            public VisualElement userRoot;
            public VisualElement tabDock;
            public Action onGUI;
            IMGUIContainer guiArea;

            public Event evt = new Event();

            bool isDockArea;
            bool isVisible;

            public bool Equals(View other) => id == other.id;

            public IEnumerable<View> GetVisibleViews(Type windowType)
            {
                foreach (var v in views.Values)
                    if (windowType.IsAssignableFrom(v.type))
                        if (v.isVisible) yield return v;
            }
            public bool IsAnyWindowVisible(Type windowType) => GetVisibleViews(windowType).Count() > 0;

            public void Clear()
            {
                tabDock?.Clear();
                userRoot.Clear();
                onGUI = null;
                guiArea.onGUIHandler -= OnGUIHandler;
            }

            internal void OnBecameVisible() 
            {
                isVisible = true;
                guiArea.onGUIHandler += OnGUIHandler;
                InvokeVisibilityChange(true);
            }
            void OnBecameInvisible()
            {
                isVisible = false;
                guiArea.onGUIHandler -= OnGUIHandler;
                InvokeVisibilityChange(false);
            }

            void InvokeVisibilityChange(bool visible)
            {
                //foreach (var m in GetAssignableModules(this))
                //    try { m.view = this; m.OnVisibilityChange(visible); } catch (Ex ex) { LogEx(ex); }
            }

            void OnGUIHandler()
            {
                try {
                    if (tabDock != null)
                        UpdateTabDockRect();
                    
                    var rect = new Rect(0, 40, 100, 20);
                    GUI.Button(rect, "test"); rect.y += 25;
                    GUI.Button(rect, "test"); rect.y += 25;
                    onGUI?.Invoke(); 
                } 
                catch(Exception ex) { Debug.LogException(ex); }
            }

            internal void Init(EditorWindow window)
            {
                this.id = window.GetInstanceID();
                this.type = window.GetType();
                this.window = window;
                this.hostView = R.m_Parent.GetValue(window);
                this.containerWindow = R.m_Window.GetValue(hostView);
                this.showMode = 0;
                if (containerWindow != null)
                    this.showMode = (ShowMode)R.windowShowMode.GetValue(containerWindow);

                this.root = window.rootVisualElement.parent;
                this.userRoot = root.Q(name: "user-root-element");
                this.tabDock = root.Q(name: "view-tab-dock");
                this.guiArea = root.Q<IMGUIContainer>();

                var onBecameInvisible = (Delegate)R.m_OnBecameInvisible.GetValue(hostView);
                onBecameInvisible = Delegate.Combine(onBecameInvisible, R.ConvertDelegate(OnBecameInvisible, R.windowDelegateT));
                R.m_OnBecameInvisible.SetValue(hostView, onBecameInvisible);

                isMaximized = hostView.GetType() == R.maximizedHostT;
                isDockArea = hostView.GetType() == R.dockAreaT;

                if (tabDock == null)
                    root.Add(tabDock = new VisualElement() { pickingMode = PickingMode.Ignore, name = "view-tab-dock" });

                if (userRoot == null)
                    root.Add(userRoot = new VisualElement() { pickingMode = PickingMode.Ignore, name = "user-root-element" });

                if (tabDock != null)
                    tabDock.style.flexDirection = FlexDirection.Row;

                userRoot.StretchToParentSize();

                Clear();
                OnBecameVisible();
            }

            static GUIStyle titleLabel;

            void UpdateTabDockRect()
            {
                var winPos = window.position;
                var tabDockRect = new Rect(winPos) { x = 0, y = 0, height = 21 };

                if (isFloating) tabDockRect.y += 2;

                if (isMaximized)
                {
                    if (titleLabel == null)
                        titleLabel = "dragtab";

                    var titleContent = window.titleContent;

                    tabDockRect.xMin += 16 + titleLabel.CalcSize(titleContent).x;
                }
                else if (isDockArea)
                {
                    var totalTabWidth = (float)R.m_TotalTabWidth.GetValue(hostView);
                    tabDockRect.xMin += totalTabWidth;
                }
                else
                {
                    R.veLayout.SetValue(tabDock, tabDockRect);
                    return;
                }
                tabDockRect.xMax -= (float)R.GetExtraButtonsWidth.Invoke(hostView, null) + 24f;
                tabDockRect.width = Mathf.Max(0, tabDockRect.width);

                R.veLayout.SetValue(tabDock, tabDockRect);
            }
        }

        static class R
        {
            public const BindingFlags AnyBind = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

            public static readonly Type viewT = EditorUtil.Asm.GetType("UnityEditor.View");
            public static readonly Type hostViewT = EditorUtil.Asm.GetType("UnityEditor.HostView");
            public static readonly Type dockAreaT = EditorUtil.Asm.GetType("UnityEditor.DockArea");
            public static readonly Type maximizedHostT = EditorUtil.Asm.GetType("UnityEditor.MaximizedHostView");
            public static readonly Type containerWindowT = EditorUtil.Asm.GetType("UnityEditor.ContainerWindow");
            public static readonly Type windowDelegateT = hostViewT.GetNestedType("EditorWindowDelegate", AnyBind);

            public static readonly FieldInfo m_Pos = typeof(EditorWindow).GetField("m_Pos", AnyBind);
            public static readonly FieldInfo m_Position = viewT.GetField("m_Position", AnyBind);
            public static readonly FieldInfo m_Parent = typeof(EditorWindow).GetField("m_Parent", AnyBind);
            public static readonly FieldInfo m_BorderSize = hostViewT.GetField("m_BorderSize", AnyBind);
            public static readonly FieldInfo m_TotalTabWidth = dockAreaT.GetField("m_TotalTabWidth", AnyBind);
            public static readonly FieldInfo windowShowMode = containerWindowT.GetField("m_ShowMode", AnyBind);
            public static readonly FieldInfo m_OnBecameInvisible = hostViewT.GetField("m_OnBecameInvisible", AnyBind);
            public static readonly FieldInfo m_OnGUI = hostViewT.GetField("m_OnGUI", AnyBind);
            public static readonly FieldInfo oldOnGUI = hostViewT.GetField("m_OnGUI", AnyBind);

            public static readonly EventInfo actualViewChangedInfo = hostViewT.GetEvent("actualViewChanged", AnyBind);
            public static readonly MethodInfo GetExtraButtonsWidth = hostViewT.GetMethod("GetExtraButtonsWidth", AnyBind);
            public static readonly MethodInfo CreateDelegate = hostViewT.GetMethod("CreateDelegate", AnyBind);
            public static readonly MethodInfo evtCopyFrom = typeof(Event).GetMethod("CopyFrom", AnyBind);

            public static readonly PropertyInfo m_Window = viewT.GetProperty("window", AnyBind);
            public static readonly PropertyInfo windowPosition = viewT.GetProperty("windowPosition", AnyBind);
            public static readonly PropertyInfo actualView = hostViewT.GetProperty("actualView", AnyBind);
            public static readonly PropertyInfo veLayout = typeof(VisualElement).GetProperty("layout", AnyBind);

            public static readonly FieldInfo genericMenuTopOffset =
             dockAreaT.GetNestedType("Styles", AnyBind).GetField("genericMenuTopOffset", AnyBind);
            public static readonly FieldInfo svcFloatValue = genericMenuTopOffset.FieldType.GetField("m_Value", AnyBind);


            public static Delegate ConvertDelegate(Action src, Type type) => ConvertDelegate((Delegate)src, type);
            public static Delegate ConvertDelegate(Delegate src, Type type)
            {
                return Delegate.CreateDelegate(type, src.Target, src.Method);
            }
            public static Delegate WindowDelegate(Action action) => ConvertDelegate(action, windowDelegateT);

            static object[] objParam = new object[1];
            static object[] objParam2 = new object[2];
            public static object[] ObjParam(object p1) { objParam[0] = p1; return objParam; }
            public static object[] ObjParam(object p1, object p2) { objParam2[0] = p1; objParam2[1] = p2; return objParam2; }
        }
    }
}

#endif
