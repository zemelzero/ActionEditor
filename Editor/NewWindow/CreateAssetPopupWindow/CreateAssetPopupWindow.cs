using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


namespace NBC.ActionEditor
{
    public class CreateAssetPopupWindow : PopupWindowContent
    {
        public override void OnGUI(Rect rect)
        {
            // Intentionally left empty
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(300, 150);
        }


        public override void OnOpen()
        {
            var root = editorWindow.rootVisualElement;

            // Import UXML
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"Assets/ActionEditor/Editor/NewWindow/{nameof(CreateAssetPopupWindow)}/{nameof(CreateAssetPopupWindow)}.uxml");
            var labelFromUXML = visualTree.Instantiate();
            root.Add(labelFromUXML);

            InitUI();
        }

        private void InitUI()
        {
            var root = editorWindow.rootVisualElement;

            var lblTitle = root.Q<Label>("lblTitle");
            lblTitle.text = Lan.CreateAsset;

            var dfAssetTypes = root.Q<DropdownField>("dfAssetTypes");
            dfAssetTypes.label = Lan.CrateAssetType;
            if (Prefs.AssetNames.Count > 0 && !string.IsNullOrEmpty(Prefs.AssetNames[0]))
            {
                dfAssetTypes.choices = Prefs.AssetNames;
                dfAssetTypes.value = Prefs.AssetNames[0];
            }

            var tfTimeAxisName = root.Q<TextField>("tfTimeAxisName");
            tfTimeAxisName.label = Lan.CrateAssetName;
            tfTimeAxisName.tooltip = Lan.CreateAssetFileName;

            var btnCreate = root.Q<Button>("btnCreate");
            btnCreate.text = Lan.CreateAssetConfirm;
            btnCreate.RegisterCallback<MouseUpEvent>(OnBtnCreateMouseUpEvent);
        }

        private void OnBtnCreateMouseUpEvent(MouseUpEvent evt)
        {
            CreateConfirm();
        }

        void CreateConfirm()
        {
            var root = editorWindow.rootVisualElement;
            var tfTimeAxisName = root.Q<TextField>("tfTimeAxisName");
            var dfAssetTypes = root.Q<DropdownField>("dfAssetTypes");

            var path = $"{Prefs.savePath}/{tfTimeAxisName.text}.json";
            if (string.IsNullOrEmpty(tfTimeAxisName.text))
            {
                EditorUtility.DisplayDialog(Lan.TipsTitle, Lan.CreateAssetTipsNameNull, Lan.TipsConfirm);
            }
            else if (AssetDatabase.LoadAssetAtPath<TextAsset>(path) != null)
            {
                EditorUtility.DisplayDialog(Lan.TipsTitle, Lan.CreateAssetTipsRepetitive, Lan.TipsConfirm);
            }
            else
            {
                var t = Prefs.AssetTypes[dfAssetTypes.value];
                var inst = Activator.CreateInstance(t);
                if (inst != null)
                {
                    var json = Json.Serialize(inst);
                    System.IO.File.WriteAllText(path, json);
                    AssetDatabase.Refresh();
                    var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                    if (textAsset != null)
                    {
                        App.OnObjectPickerConfig(textAsset);
                    }
                    editorWindow.Close();
                }
            }
        }
    }
}