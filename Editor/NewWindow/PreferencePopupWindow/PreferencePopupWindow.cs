using System;
using System.Linq;
using NBC.ActionEditor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBC.ActionEditor
{
    public class PreferencePopupWindow : PopupWindowContent
    {
        //Set the window size
        public override Vector2 GetWindowSize()
        {
            return new Vector2(450, 180);
        }

        public override void OnGUI(Rect rect)
        {
            // Intentionally left empty
        }

        public override void OnOpen()
        {
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/ActionEditor/Editor/NewWindow/PreferencePopupWindow/PreferencePopupWindow.uxml");
            visualTreeAsset.CloneTree(editorWindow.rootVisualElement);

            InitUI();
        }

        private void InitUI()
        {
            var root = editorWindow.rootVisualElement;

            var lblTitle = root.Q<Label>("lblTitle");
            lblTitle.text = Lan.PreferencesTitle;

            var dfLanguage = root.Q<DropdownField>("dfLanguage");
            dfLanguage.choices = Lan.AllLanguages.Keys.ToList();
            dfLanguage.value = Lan.Language;
            dfLanguage.RegisterValueChangedCallback(OnDFLanguageValueChanged);

            var enumStrideMode = root.Q<EnumField>("enumStrideMode");
            enumStrideMode.label = Lan.PreferencesTimeStepMode;
            enumStrideMode.Init(Prefs.timeStepMode);
            enumStrideMode.RegisterValueChangedCallback(OnEnumStrideModeValueChanged);

            var dfStride = root.Q<DropdownField>("dfStride");
            RefreshDfStride();
            dfStride.RegisterValueChangedCallback(OnDFStrideValueChanged);

            var tgClipSnapping = root.Q<Toggle>("tgClipSnapping");
            tgClipSnapping.label = Lan.PreferencesMagnetSnapping;
            tgClipSnapping.tooltip = Lan.PreferencesMagnetSnappingTips;
            tgClipSnapping.value = Prefs.MagnetSnapping;
            tgClipSnapping.RegisterValueChangedCallback(OnTgClipSnappingValueChanged);

            var tgScrollWheelZoom = root.Q<Toggle>("tgScrollWheelZoom");
            tgScrollWheelZoom.label = Lan.PreferencesScrollWheelZooms;
            tgScrollWheelZoom.tooltip = Lan.PreferencesScrollWheelZoomsTips;
            tgScrollWheelZoom.value = Prefs.scrollWheelZooms;
            tgScrollWheelZoom.RegisterValueChangedCallback(OnTgScrollWheelZoomValueChanged);

            var tfSelectFile = root.Q<TextField>("tfSelectFile");
            tfSelectFile.label = Lan.PreferencesSavePath;
            tfSelectFile.tooltip = Lan.PreferencesSavePathTips;
            tfSelectFile.value = Prefs.savePath;
            tfSelectFile.isReadOnly = true;

            var btnSelectFile = root.Q<Button>("btnSelectFile");
            btnSelectFile.text = Lan.SelectFolder;
            btnSelectFile.RegisterCallback<MouseUpEvent>(OnBtnSelectFileClicked);

            var siAutoSaveTime = root.Q<SliderInt>("siAutoSaveTime");
            siAutoSaveTime.label = Lan.PreferencesAutoSaveTime;
            siAutoSaveTime.tooltip = Lan.PreferencesAutoSaveTimeTips;
            siAutoSaveTime.value = Prefs.autoSaveSeconds;
            siAutoSaveTime.RegisterValueChangedCallback(OnSiAutoSaveTimeValueChanged);
        }

        private void OnSiAutoSaveTimeValueChanged(ChangeEvent<int> evt)
        {
            Prefs.autoSaveSeconds = evt.newValue;
        }


        private void OnBtnSelectFileClicked(EventBase<MouseUpEvent> evt)
        {
            var select_path = EditorUtility.OpenFolderPanel(Lan.SelectFolder, string.IsNullOrEmpty(Prefs.savePath) ? "Assets/" : Prefs.savePath, "");
            if (string.IsNullOrEmpty(select_path))
                return;
            int asset_start_index = select_path.IndexOf("Assets", StringComparison.Ordinal);
            if (asset_start_index > -1)
            {
                select_path = select_path.Substring(asset_start_index, select_path.Length - asset_start_index);
            }
            Prefs.savePath = select_path;

            var root = editorWindow.rootVisualElement;
            var tfSelectFile = root.Q<TextField>("tfSelectFile");
            tfSelectFile.value = Prefs.savePath;
        }

        private void OnTgScrollWheelZoomValueChanged(ChangeEvent<bool> evt)
        {
            Prefs.scrollWheelZooms = evt.newValue;
        }

        private void OnTgClipSnappingValueChanged(ChangeEvent<bool> evt)
        {
            Prefs.MagnetSnapping = evt.newValue;
        }

        private void OnDFStrideValueChanged(ChangeEvent<string> evt)
        {
            if (Prefs.timeStepMode == Prefs.TimeStepMode.Seconds)
            {
                Prefs.SnapInterval = float.Parse(evt.newValue);
            }
            else
            {
                Prefs.FrameRate = int.Parse(evt.newValue);
            }
        }

        private void RefreshDfStride()
        {
            var root = editorWindow.rootVisualElement;
            var dfStride = root.Q<DropdownField>("dfStride");

            if (Prefs.timeStepMode == Prefs.TimeStepMode.Seconds)
            {
                dfStride.choices = Prefs.snapIntervals.Select(v => v.ToString()).ToList();
                dfStride.value = Prefs.SnapInterval.ToString();
                dfStride.label = Lan.PreferencesSnapInterval;
            }
            else
            {
                dfStride.choices = Prefs.frameRates.Select(v => v.ToString()).ToList();
                dfStride.value = Prefs.FrameRate.ToString();
                dfStride.label = Lan.PreferencesFrameRate;
            }
        }

        private void OnEnumStrideModeValueChanged(ChangeEvent<Enum> evt)
        {
            Prefs.timeStepMode = (Prefs.TimeStepMode)evt.newValue;
            RefreshDfStride();
        }

        private void OnDFLanguageValueChanged(ChangeEvent<string> evt)
        {
            Lan.SetLanguage(evt.newValue);
        }

        public override void OnClose()
        {
        }
    }
}