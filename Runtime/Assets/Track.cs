using System;
using System.Collections.Generic;
using System.Linq;
using FullSerializer;
using UnityEngine;

namespace NBC.ActionEditor
{
    /// <summary>
    /// 轨道基类，用于管理剪辑(Clip)的集合
    /// </summary>
    [Serializable]
    [Attachable(typeof(Group))]
    public abstract class Track : IDirectable
    {
        [SerializeField] private List<Clip> actionClips = new();

        [SerializeField] [HideInInspector] private string name;
        [SerializeField] [HideInInspector] private bool active = true;
        [SerializeField] [HideInInspector] private bool isLocked;
        [SerializeField] private Color color = Color.white;

        /// <summary>
        /// 获取轨道颜色，如果透明度小于0.1则返回白色
        /// </summary>
        public Color Color => color.a > 0.1f ? color : Color.white;

        /// <summary>
        /// 轨道信息（虚属性，子类可重写）
        /// </summary>
        public virtual string info => string.Empty;

        /// <summary>
        /// 获取或设置轨道包含的剪辑列表
        /// </summary>
        public List<Clip> Clips
        {
            get => actionClips;
            set => actionClips = value;
        }

        /// <summary>
        /// 序列化前调用（虚方法，子类可重写）
        /// </summary>
        public virtual void OnBeforeSerialize()
        {
        }

        /// <summary>
        /// 反序列化后调用（虚方法，子类可重写）
        /// </summary>
        public virtual void OnAfterDeserialize()
        {
        }

        [fsIgnore] public IDirector Root => Parent?.Root;
        [fsIgnore] public IDirectable Parent { get; private set; }

        [fsIgnore] public Group Group => Parent as Group;
        
        /// <summary>
        /// 获取或设置轨道名称
        /// </summary>
        public string Name
        {
            get => name;
            set => name = value;
        }

        IEnumerable<IDirectable> IDirectable.Children => Clips;

        /// <summary>
        /// 获取父级的Actor对象
        /// </summary>
        public GameObject Actor => Parent?.Actor;

        /// <summary>
        /// 获取或设置轨道是否折叠（虚属性，子类可重写）
        /// </summary>
        public virtual bool IsCollapsed
        {
            get => Parent != null && Parent.IsCollapsed;
            set { }
        }

        /// <summary>
        /// 获取或设置轨道是否激活（虚属性，子类可重写）
        /// </summary>
        public virtual bool IsActive
        {
            get => Parent != null && Parent.IsActive && active;
            set
            {
                if (active != value)
                {
                    active = value;
                    if (Root != null) Root.Validate();
                }
            }
        }

        /// <summary>
        /// 获取或设置轨道是否锁定（虚属性，子类可重写）
        /// </summary>
        public virtual bool IsLocked
        {
            get => Parent != null && (Parent.IsLocked || isLocked);
            set => isLocked = value;
        }

        public int StartTimeInt => 0;
        public int EndTimeInt => 0;

        /// <summary>
        /// 获取或设置轨道开始时间（虚属性，子类可重写）
        /// </summary>
        public virtual float StartTime
        {
            get => Parent?.StartTime ?? 0;
            set { }
        }

        /// <summary>
        /// 获取或设置轨道结束时间（虚属性，子类可重写）
        /// </summary>
        public virtual float EndTime
        {
            get => Parent?.EndTime ?? 0;
            set { }
        }

        /// <summary>
        /// 获取或设置淡入时间（虚属性，子类可重写）
        /// </summary>
        public virtual float BlendIn
        {
            get => 0f;
            set { }
        }

        /// <summary>
        /// 获取或设置淡出时间（虚属性，子类可重写）
        /// </summary>
        public virtual float BlendOut
        {
            get => 0f;
            set { }
        }

        /// <summary>
        /// 是否允许交叉淡入淡出
        /// </summary>
        public bool CanCrossBlend => false;

        /// <summary>
        /// 初始化轨道
        /// </summary>
        /// <returns>总是返回true</returns>
        public bool Initialize()
        {
            return true;
        }

        /// <summary>
        /// 验证轨道
        /// </summary>
        /// <param name="root">根节点</param>
        /// <param name="parent">父节点</param>
        public void Validate(IDirector root, IDirectable parent)
        {
            // Debug.Log($"设置轨道的父节点==={parent}");
            Parent = parent;
            OnAfterValidate();
        }

        /// <summary>
        /// 创建时调用（虚方法，子类可重写）
        /// </summary>
        protected virtual void OnCreate()
        {
        }

        /// <summary>
        /// 验证后调用（虚方法，子类可重写）
        /// </summary>
        protected virtual void OnAfterValidate()
        {
        }

        /// <summary>
        /// 创建后处理
        /// </summary>
        /// <param name="parent">父节点</param>
        public void PostCreate(IDirectable parent)
        {
            Parent = parent;
            OnCreate();
        }

        /// <summary>
        /// 添加指定类型的剪辑
        /// </summary>
        /// <typeparam name="T">剪辑类型</typeparam>
        /// <param name="time">开始时间</param>
        /// <returns>新创建的剪辑</returns>
        public T AddClip<T>(float time) where T : Clip
        {
            return (T)AddClip(typeof(T), time);
        }

        /// <summary>
        /// 添加指定类型的剪辑
        /// </summary>
        /// <param name="type">剪辑类型</param>
        /// <param name="time">开始时间</param>
        /// <returns>新创建的剪辑</returns>
        public Clip AddClip(Type type, float time)
        {
            var catAtt =
                type.GetCustomAttributes(typeof(CategoryAttribute), true).FirstOrDefault() as CategoryAttribute;
            if (catAtt != null && Clips.Count == 0) Name = catAtt.category + " Track";

            var newAction = Activator.CreateInstance(type) as Clip;

            Debug.Log($"type={type} newAction={newAction}");

            if (newAction != null)
            {
                // if (!newAction.CanAdd(this)) return null;

                newAction.StartTime = time;
                newAction.Name = type.Name;
                Clips.Add(newAction);
                newAction.PostCreate(this);

                var nextAction = Clips.FirstOrDefault(a => a.StartTime > newAction.StartTime);
                if (nextAction != null) newAction.EndTime = Mathf.Min(newAction.EndTime, nextAction.StartTime);

                Root.Validate();
                // DirectorUtility.selectedObject = newAction;
            }

            return newAction;
        }

        /// <summary>
        /// 添加已有剪辑
        /// </summary>
        /// <param name="clip">要添加的剪辑</param>
        /// <returns>添加的剪辑</returns>
        public Clip AddClip(Clip clip)
        {
            if (clip != null && clip.CanValidTime(this, clip.StartTime, clip.EndTime))
            {
                // if (!clip.CanAdd(this)) return null;
                if (clip.Parent != null && clip.Parent is Track track)
                {
                    track.DeleteAction(clip);
                }

                Clips.Add(clip);
                Root.Validate();
            }

            return clip;
        }

        /// <summary>
        /// 删除指定剪辑
        /// </summary>
        /// <param name="action">要删除的剪辑</param>
        public void DeleteAction(Clip action)
        {
            Clips.Remove(action);
            Root.Validate();
        }
    }
}