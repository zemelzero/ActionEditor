using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBC.ActionEditor
{
    public class StartupWindow : EditorWindow
    {
        [MenuItem("NBC/NewActionEditor")]
        public static void ShowExample()
        {
            StartupWindow wnd = GetWindow<StartupWindow>();
            wnd.titleContent = new GUIContent("技能编辑器");
        }

        public void CreateGUI()
        {
            InitializeAll();

            var root = rootVisualElement;

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/ActionEditor/Editor/NewWindow/StartupWindow/StartupWindow.uxml");
            var labelFromUXML = visualTree.Instantiate();
            root.Add(labelFromUXML);

            var btnCreateTimeline = root.Q<Button>("btnCreateTimeline");
            btnCreateTimeline.text = Lan.CreateAsset;
            btnCreateTimeline.RegisterCallback<MouseUpEvent>(OnBtnCreateTimelineMouseUp);

            var btnEditorPreference = root.Q<Button>("btnEditorPreference");
            btnEditorPreference.text = Lan.Setting;
            btnEditorPreference.RegisterCallback<MouseUpEvent>(OnBtnEditorPreferenceMouseUp);
        }

        private void OnBtnCreateTimelineMouseUp(MouseUpEvent evt)
        {
            VisualElement root = rootVisualElement;
            Button myButton = root.Q<Button>("btnCreateTimeline");
            UnityEditor.PopupWindow.Show(myButton.worldBound, new CreateAssetPopupWindow());
        }

        private void OnBtnEditorPreferenceMouseUp(MouseUpEvent evt)
        {
            VisualElement root = rootVisualElement;
            Button myButton = root.Q<Button>("btnEditorPreference");
            UnityEditor.PopupWindow.Show(myButton.worldBound, new PreferencePopupWindow());
        }

        private void OnEnable()
        {
            InitializeAll();
        }

        private void InitializeAll()
        {
            Lan.Load();
            Styles.Load();
            Prefs.InitializeAssetTypes();
            App.OnInitialize?.Invoke();
            //停止播放
            if (App.AssetData != null)
            {
                if (!Application.isPlaying)
                {
                    // App.Stop(true);
                }
            }

            // WillRepaint = true;
        }
    }
}