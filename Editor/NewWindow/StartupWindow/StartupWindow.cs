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

        }

        private void OnBtnEditorPreferenceMouseUp(MouseUpEvent evt)
        {
            VisualElement root = rootVisualElement;
            Button myButton = root.Q<Button>("btnEditorPreference");
            UnityEditor.PopupWindow.Show(myButton.worldBound, new PreferencePopupWindow());
        }
    }
}