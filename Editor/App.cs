using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Linq;

namespace NBC.ActionEditor
{
    // 定义回调函数委托
    public delegate void CallbackFunction();

    // 定义打开资源函数委托
    public delegate void OpenAssetFunction(Asset asset);

    /// <summary>
    /// App类，用于管理应用程序的核心功能
    /// </summary>
    public static class App
    {
        private static TextAsset _textAsset;

        // 初始化回调
        public static CallbackFunction OnInitialize;
        // 禁用回调
        public static CallbackFunction OnDisable;
        // 打开资源回调
        public static OpenAssetFunction OnOpenAsset;
        
        // 当前资源数据
        public static Asset AssetData { get; private set; } = null;

        /// <summary>
        /// 获取或设置文本资源
        /// </summary>
        public static TextAsset TextAsset
        {
            get => _textAsset;
            set
            {
                _textAsset = value;

                if (_textAsset == null)
                {
                    AssetData = null;
                }
                else
                {
                    var obj = Json.Deserialize(typeof(Asset), _textAsset.text);
                    if (obj is Asset asset)
                    {
                        AssetData = asset;
                        asset.Init();
                        OnOpenAsset?.Invoke(AssetData);
                        App.Refresh();
                    }
                }
            }
        }

        // 当前窗口
        public static EditorWindow Window;

        // 当前帧数
        public static long Frame;

        // 窗口宽度
        public static float Width;

        /// <summary>
        /// 对象选择器配置
        /// </summary>
        /// <param name="obj">选择的对象</param>
        public static void OnObjectPickerConfig(Object obj)
        {
            if (obj is TextAsset textAsset)
            {
                TextAsset = textAsset;
            }
        }

        /// <summary>
        /// 保存资源
        /// </summary>
        public static void SaveAsset()
        {
            if (AssetData == null) return;
            AssetData.OnBeforeSerialize();
            var path = AssetDatabase.GetAssetPath(TextAsset);
            var json = Json.Serialize(AssetData);
            System.IO.File.WriteAllText(path, json);
        }

        /// <summary>
        /// GUI结束时调用
        /// </summary>
        public static void OnGUIEnd()
        {
            if (Frame > NeedForceRefreshFrame)
            {
                NeedForceRefresh = false;
            }

            Frame++;
            if (Frame >= long.MaxValue)
            {
                Frame = 0;
            }
        }

        /// <summary>
        /// 更新时调用
        /// </summary>
        public static void OnUpdate()
        {
            TryAutoSave();
            PlayerUpdate();
        }

        #region AutoSave

        // 上次保存时间
        public static DateTime LastSaveTime => _lastSaveTime;

        private static DateTime _lastSaveTime = DateTime.Now;

        /// <summary>
        /// 尝试自动保存
        /// </summary>
        public static void TryAutoSave()
        {
            var timespan = DateTime.Now - _lastSaveTime;
            if (timespan.Seconds > Prefs.autoSaveSeconds)
            {
                AutoSave();
            }
        }

        /// <summary>
        /// 自动保存
        /// </summary>
        public static void AutoSave()
        {
            _lastSaveTime = DateTime.Now;
            SaveAsset();
        }

        #endregion

        #region Copy&Cut

        // 复制的资源
        public static IDirectable CopyAsset { get; set; }
        // 是否剪切
        public static bool IsCut { get; set; }

        #endregion

        #region Select

        // 选中的项目
        public static IDirectable[] SelectItems => _selectList.ToArray();
        // 选中数量
        public static int SelectCount => _selectList.Count;
        private static readonly List<IDirectable> _selectList = new List<IDirectable>();

        // 第一个选中的项目
        public static IDirectable FistSelect => _selectList.Count > 0 ? _selectList.First() : null;

        // 是否可以多选
        public static bool CanMultipleSelect { get; set; }

        [System.NonSerialized] private static InspectorPreviewAsset _currentInspectorPreviewAsset;

        // 当前预览资源
        public static InspectorPreviewAsset CurrentInspectorPreviewAsset
        {
            get
            {
                if (_currentInspectorPreviewAsset == null)
                {
                    _currentInspectorPreviewAsset = ScriptableObject.CreateInstance<InspectorPreviewAsset>();
                }

                return _currentInspectorPreviewAsset;
            }
        }

        /// <summary>
        /// 选择项目
        /// </summary>
        /// <param name="objs">要选择的项目</param>
        public static void Select(params IDirectable[] objs)
        {
            var change = false;
            if (objs == null)
            {
                if (_selectList.Count > 0) change = true;
            }
            else
            {
                if (objs.Length != _selectList.Count) change = true;
                else
                {
                    var pickCount = 0;
                    foreach (var obj in objs)
                    {
                        if (_selectList.Contains(obj)) pickCount++;
                    }

                    if (pickCount != objs.Length)
                    {
                        change = true;
                    }
                }
            }

            if (!change) return;
            _selectList.Clear();
            if (objs != null)
            {
                foreach (var obj in objs)
                {
                    _selectList.Add(obj);
                }

                Selection.activeObject = CurrentInspectorPreviewAsset;
                EditorUtility.SetDirty(CurrentInspectorPreviewAsset);

                // DirectorUtility.selectedObject = FistSelect;
            }

            if (_selectList.Count == 1 && _selectList[0] is not Clip)
            {
                CanMultipleSelect = true;
            }
            else
            {
                CanMultipleSelect = false;
            }
        }

        /// <summary>
        /// 判断是否选中
        /// </summary>
        /// <param name="directable">要判断的项目</param>
        /// <returns>是否选中</returns>
        public static bool IsSelect(IDirectable directable)
        {
            return _selectList.Contains(directable);
        }

        #endregion

        #region Refresh

        // 是否需要强制刷新
        public static bool NeedForceRefresh { get; private set; }
        // 需要强制刷新的帧数
        public static long NeedForceRefreshFrame { get; private set; }

        /// <summary>
        /// 刷新
        /// </summary>
        public static void Refresh()
        {
            NeedForceRefresh = true;
            NeedForceRefreshFrame = Frame;
        }

        /// <summary>
        /// 重绘
        /// </summary>
        public static void Repaint()
        {
            if (Window != null)
            {
                Window.Repaint();
            }
        }

        #endregion

        #region 播放相关

        // 播放回调
        public static CallbackFunction OnPlay;
        // 停止回调
        public static CallbackFunction OnStop;

        // 播放器实例
        private static AssetPlayer _player => AssetPlayer.Inst;

        // 是否在播放
        public static bool IsPlay { get; private set; }
        // 是否暂停
        public static bool IsPause { get; private set; }

        // 是否在范围内
        public static bool IsRange { get; set; }

        private static float _editorPreviousTime;

        /// <summary>
        /// 播放
        /// </summary>
        /// <param name="callback">回调函数</param>
        public static void Play(Action callback = null)
        {
            if (Application.isPlaying)
            {
                return;
            }

            OnPlay?.Invoke();
            IsPlay = true;
        }

        /// <summary>
        /// 暂停
        /// </summary>
        /// <param name="pause">是否暂停</param>
        public static void Pause(bool pause = true)
        {
            IsPause = pause;
        }

        /// <summary>
        /// 停止
        /// </summary>
        public static void Stop()
        {
            if (AssetData != null)
                _player.CurrentTime = 0;

            OnStop?.Invoke();
            IsPlay = false;
            IsPause = false;
        }

        /// <summary>
        /// 向前步进
        /// </summary>
        public static void StepForward()
        {
            if (Math.Abs(_player.CurrentTime - _player.Length) < 0.00001f)
            {
                _player.CurrentTime = 0;
                return;
            }

            _player.CurrentTime += Prefs.SnapInterval;
        }

        /// <summary>
        /// 向后步进
        /// </summary>
        public static void StepBackward()
        {
            if (_player.CurrentTime == 0)
            {
                _player.CurrentTime = _player.Length;
                return;
            }

            _player.CurrentTime -= Prefs.SnapInterval;
        }

        /// <summary>
        /// 播放器更新
        /// </summary>
        private static void PlayerUpdate()
        {
            if (_player == null) return;
            var delta = (Time.realtimeSinceStartup - _editorPreviousTime) * Time.timeScale;

            _editorPreviousTime = Time.realtimeSinceStartup;

            _player.Sample();

            if (!IsPlay) return;
            
            if(IsPause) return;

            if (_player.CurrentTime >= App.AssetData.Length)
            {
                _player.Sample(0);
                _player.Sample(delta);
                return;
            }

            _player.CurrentTime += delta; 
            Repaint();
        }

        #endregion
    }
}