using System;
using System.Collections.Generic;
using System.Linq;
using FullSerializer;
using UnityEngine;

namespace NBC.ActionEditor
{
    /// <summary>
    /// 默认组类，用于管理轨道集合
    /// </summary>
    [Name("Default Group")]
    [Serializable]
    public class Group : IDirectable
    {
        [SerializeField] [HideInInspector] private string name; // 组名称
        [SerializeField] private List<Track> tracks = new(); // 轨道列表
        [SerializeField] [HideInInspector] private bool isCollapsed; // 是否折叠
        [SerializeField] [HideInInspector] private bool active = true; // 是否激活
        [SerializeField] [HideInInspector] private bool isLocked; // 是否锁定

        public int ActorId; // 关联的Actor ID

        /// <summary>
        /// 获取或设置轨道列表
        /// </summary>
        public List<Track> Tracks
        {
            get => tracks;
            set => tracks = value;
        }

        [fsIgnore] public IDirector Root { get; private set; } // 根目录
        IDirectable IDirectable.Parent => null; // 父对象（始终为null）

        IEnumerable<IDirectable> IDirectable.Children => Tracks; // 子对象集合（轨道列表）
        private GameObject _actor; // 关联的GameObject

        [HideInInspector]
        /// <summary>
        /// 获取或设置关联的Actor对象
        /// </summary>
        public GameObject Actor
        {
            get { return _actor; }
            set
            {
                _actor = value;
                // 以下代码已被注释，用于设置ActorId
                // var key = _actor.GetComponent<ObjectKey>();
                // if ((key == null))
                // {
                //     _actor.AddComponent<ObjectKey>();
                //     ActorId = _actor.GetInstanceID();
                // }
                // else
                // {
                //     ActorId = key.ObjectId;
                // }
            }
        }

        public int StartTimeInt => 0; // 开始时间（整数）
        public int EndTimeInt => 0; // 结束时间（整数）

        float IDirectable.StartTime => 0; // 开始时间（浮点数）

        float IDirectable.EndTime => Root.Length; // 结束时间（浮点数）

        /// <summary>
        /// 获取或设置淡入时间
        /// </summary>
        public float BlendIn
        {
            get => 0;
            set { }
        }

        /// <summary>
        /// 获取或设置淡出时间
        /// </summary>
        public float BlendOut
        {
            get => 0;
            set { }
        }

        // 以下代码已被注释，用于设置淡入淡出时间
        // float IDirectable.BlendIn => 0f;
        //
        // float IDirectable.BlendOut => 0f;

        bool IDirectable.CanCrossBlend => false; // 是否支持交叉淡入淡出

        /// <summary>
        /// 获取或设置组名称
        /// </summary>
        public string Name
        {
            get => name;
            set => name = value;
        }

        /// <summary>
        /// 获取或设置是否激活
        /// </summary>
        public bool IsActive
        {
            get => active;
            set
            {
                if (active == value) return;
                active = value;
                if (Root != null) Root?.Validate();
            }
        }

        /// <summary>
        /// 获取或设置是否折叠
        /// </summary>
        public bool IsCollapsed
        {
            get => isCollapsed;
            set => isCollapsed = value;
        }

        /// <summary>
        /// 获取或设置是否锁定
        /// </summary>
        public bool IsLocked
        {
            get => isLocked;
            set => isLocked = value;
        }

        /// <summary>
        /// 验证组状态
        /// </summary>
        /// <param name="_root">根目录</param>
        /// <param name="_parent">父对象</param>
        public void Validate(IDirector _root, IDirectable _parent)
        {
            Root = _root;
        }

        /// <summary>
        /// 初始化组
        /// </summary>
        /// <returns>是否初始化成功</returns>
        public bool Initialize()
        {
            return true;
        }

        /// <summary>
        /// 序列化前调用
        /// </summary>
        public virtual void OnBeforeSerialize()
        {
        }

        /// <summary>
        /// 反序列化后调用
        /// </summary>
        public virtual void OnAfterDeserialize()
        {
        }

        /// <summary>
        /// 检查是否可以添加指定轨道
        /// </summary>
        /// <param name="track">轨道对象</param>
        /// <returns>是否可以添加</returns>
        public bool CanAddTrack(Track track)
        {
            return track != null && CanAddTrackOfType(track.GetType());
        }

        /// <summary>
        /// 检查是否可以添加指定类型的轨道
        /// </summary>
        /// <param name="type">轨道类型</param>
        /// <returns>是否可以添加</returns>
        public bool CanAddTrackOfType(Type type)
        {
            if (type == null || !type.IsSubclassOf(typeof(Track)) || type.IsAbstract) return false;

            if (type.IsDefined(typeof(UniqueAttribute), true) &&
                Tracks.FirstOrDefault(t => t.GetType() == type) != null)
                return false;

            var attachAtt = type.RTGetAttribute<AttachableAttribute>(true);
            if (attachAtt == null || attachAtt.Types == null || attachAtt.Types.All(t => t != GetType())) return false;

            return true;
        }

        /// <summary>
        /// 添加指定类型的轨道
        /// </summary>
        /// <typeparam name="T">轨道类型</typeparam>
        /// <param name="_name">轨道名称</param>
        /// <returns>添加的轨道对象</returns>
        public T AddTrack<T>(string _name = null) where T : Track
        {
            return (T)AddTrack(typeof(T), _name);
        }

        /// <summary>
        /// 添加指定类型的轨道
        /// </summary>
        /// <param name="type">轨道类型</param>
        /// <param name="_name">轨道名称</param>
        /// <returns>添加的轨道对象</returns>
        public Track AddTrack(Type type, string _name = null)
        {
            var newTrack = Activator.CreateInstance(type);
            if (newTrack is Track track)
            {
                // 以下代码已被注释，用于检查是否可以添加轨道
                // if (!track.CanAdd(this)) return null;
                track.Name = type.Name;
                Tracks.Add(track);

                Debug.Log("tracks.count=" + Tracks.Count);
                Root?.Validate();

                return track;
            }

            return null;
        }

        /// <summary>
        /// 在指定位置插入轨道
        /// </summary>
        /// <typeparam name="T">轨道类型</typeparam>
        /// <param name="track">轨道对象</param>
        /// <param name="index">插入位置</param>
        /// <returns>实际插入位置</returns>
        public int InsertTrack<T>(T track, int index) where T : Track
        {
            if (tracks.Contains(track))
            {
                DeleteTrack(track);
            }

            if (index >= tracks.Count)
            {
                index = tracks.Count;
                tracks.Add(track);
            }
            else
            {
                if (index < 0) index = 0;
                tracks.Insert(index, track);
            }

            Root?.Validate();
            return index;
        }

        /// <summary>
        /// 删除指定轨道
        /// </summary>
        /// <param name="track">轨道对象</param>
        public void DeleteTrack(Track track)
        {
            // 以下代码已被注释，用于撤销操作
            // Undo.RegisterCompleteObjectUndo(this, "Delete Track");
            Tracks.Remove(track);
            // 以下代码已被注释，用于处理选中对象
            // if (ReferenceEquals(DirectorUtility.selectedObject, track))
            // {
            //     DirectorUtility.selectedObject = null;
            // }

            Root?.Validate();
        }

        /// <summary>
        /// 获取指定轨道的索引
        /// </summary>
        /// <param name="track">轨道对象</param>
        /// <returns>轨道索引</returns>
        public int GetTrackIndex(Track track)
        {
            return tracks.FindIndex(t => t == track);
        }
    }
}