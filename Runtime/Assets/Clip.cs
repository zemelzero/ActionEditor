using System;
using System.Collections.Generic;
using FullSerializer;
using UnityEngine;

namespace NBC.ActionEditor
{
    /// <summary>
    /// 片段基类，表示时间轴上的一个片段
    /// </summary>
    [Serializable]
    public abstract class Clip : IClip
    {
        /// <summary>
        /// 片段开始时间
        /// </summary>
        [SerializeField]
        private float startTime;

        /// <summary>
        /// 片段长度
        /// </summary>
        [SerializeField]
        [HideInInspector]
        protected float length = 1f;

        /// <summary>
        /// 片段名称
        /// </summary>
        [SerializeField]
        private string name;

        /// <summary>
        /// 片段长度属性
        /// </summary>
        [MenuName("片段长度")]
        public virtual float Length
        {
            get => length;
            set
            {
                length = value;
            }
        }

        /// <summary>
        /// 获取片段信息
        /// </summary>
        public virtual string Info
        {
            get
            {
                var nameAtt = GetType().RTGetAttribute<NameAttribute>(true);
                if (nameAtt != null) return nameAtt.name;

                return GetType().Name.SplitCamelCase();
            }
        }

        /// <summary>
        /// 片段是否有效
        /// </summary>
        public virtual bool IsValid => false;

        /// <summary>
        /// 获取根节点
        /// </summary>
        [fsIgnore]
        public IDirector Root => Parent?.Root;

        /// <summary>
        /// 父节点
        /// </summary>
        [fsIgnore]
        public IDirectable Parent { get; private set; }

        /// <summary>
        /// 子节点集合
        /// </summary>
        IEnumerable<IDirectable> IDirectable.Children => null;

        /// <summary>
        /// 获取关联的GameObject
        /// </summary>
        public GameObject Actor => Parent?.Actor;

        /// <summary>
        /// 片段名称属性
        /// </summary>
        public string Name
        {
            get => name;
            set => name = value;
        }

        /// <summary>
        /// 片段开始时间属性
        /// </summary>
        [MenuName("开始时间")]
        public float StartTime
        {
            get => startTime;
            set
            {
                if (Math.Abs(startTime - value) > 0.0001f)
                {
                    startTime = Mathf.Max(value, 0);
                    // BlendIn = Mathf.Clamp(BlendIn, 0, Length - BlendOut);
                    // BlendOut = Mathf.Clamp(BlendOut, 0, Length - BlendIn);
                }
            }
        }

        /// <summary>
        /// 片段结束时间属性
        /// </summary>
        [MenuName("结束时间")]
        public float EndTime
        {
            get => StartTime + Length;
            set
            {
                if (Math.Abs(StartTime + Length - value) > 0.0001f)
                {
                    Length = Mathf.Max(value - StartTime, 0);
                    BlendOut = Mathf.Clamp(BlendOut, 0, Length - BlendIn);
                    BlendIn = Mathf.Clamp(BlendIn, 0, Length - BlendOut);
                }
            }
        }

        /// <summary>
        /// 片段是否激活
        /// </summary>
        public bool IsActive
        {
            get => Parent?.IsActive ?? false;
            set { }
        }

        /// <summary>
        /// 片段是否折叠
        /// </summary>
        public bool IsCollapsed
        {
            get { return Parent != null && Parent.IsCollapsed; }
            set { }
        }

        /// <summary>
        /// 片段是否锁定
        /// </summary>
        public bool IsLocked
        {
            get { return Parent != null && Parent.IsLocked; }
            set { }
        }

        /// <summary>
        /// 片段渐入时间
        /// </summary>
        public virtual float BlendIn
        {
            get => 0;
            set { }
        }

        /// <summary>
        /// 片段渐出时间
        /// </summary>
        public virtual float BlendOut
        {
            get => 0;
            set { }
        }

        /// <summary>
        /// 是否允许交叉混合
        /// </summary>
        public virtual bool CanCrossBlend { get; }

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
        /// 初始化片段
        /// </summary>
        public bool Initialize()
        {
            return true;
        }

        /// <summary>
        /// 验证片段
        /// </summary>
        public void Validate(IDirector root, IDirectable parent)
        {
            Parent = parent;
            // hideFlags = HideFlags.HideInHierarchy;
            // ValidateAnimParams();
            // OnAfterValidate();
        }

        /// <summary>
        /// 动画参数目标对象
        /// </summary>
        public object AnimatedParametersTarget { get; }

        /// <summary>
        /// 获取下一个片段
        /// </summary>
        public Clip GetNextClip()
        {
            return this.GetNextSibling<Clip>();
        }

        /// <summary>
        /// 获取片段权重
        /// </summary>
        public float GetClipWeight(float time)
        {
            return GetClipWeight(time, BlendIn, BlendOut);
        }

        /// <summary>
        /// 获取片段权重（使用相同的渐入渐出时间）
        /// </summary>
        public float GetClipWeight(float time, float blendInOut)
        {
            return GetClipWeight(time, blendInOut, blendInOut);
        }

        /// <summary>
        /// 获取片段权重（使用指定的渐入渐出时间）
        /// </summary>
        public float GetClipWeight(float time, float blendIn, float blendOut)
        {
            return this.GetWeight(time, blendIn, blendOut);
        }

        #region 长度匹配

        /// <summary>
        /// 尝试匹配子片段长度
        /// </summary>
        public void TryMatchSubClipLength()
        {
            if (this is ISubClipContainable)
                Length = ((ISubClipContainable)this).SubClipLength / ((ISubClipContainable)this).SubClipSpeed;
        }

        /// <summary>
        /// 尝试匹配上一个子片段循环
        /// </summary>
        public void TryMatchPreviousSubClipLoop()
        {
            if (this is ISubClipContainable) Length = (this as ISubClipContainable).GetPreviousLoopLocalTime();
        }

        /// <summary>
        /// 尝试匹配下一个子片段循环
        /// </summary>
        public void TryMatchNexSubClipLoop()
        {
            if (this is ISubClipContainable)
            {
                var targetLength = (this as ISubClipContainable).GetNextLoopLocalTime();
                var nextClip = GetNextClip();
                if (nextClip == null || StartTime + targetLength <= nextClip.StartTime) Length = targetLength;
            }
        }

        #endregion

        #region 混合切片

        /// <summary>
        /// 设置交叉渐入时间
        /// </summary>
        public virtual void SetCrossBlendIn(float value)
        {
        }

        /// <summary>
        /// 设置交叉渐出时间
        /// </summary>
        public virtual void SetCrossBlendOut(float value)
        {
        }

        #endregion

        /// <summary>
        /// 创建后调用
        /// </summary>
        public void PostCreate(IDirectable parent)
        {
            Parent = parent;
            // CreateAnimationDataCollection();
            // OnCreate();
        }
    }

    /// <summary>
    /// 信号片段基类
    /// </summary>
    [Serializable]
    public abstract class ClipSignal : Clip
    {
        /// <summary>
        /// 获取片段长度（固定为0）
        /// </summary>
        public override float Length
        {
            get => 0;
            //set => TimeCache();
        }
    }

    /// <summary>
    /// 交叉混合片段基类
    /// </summary>
    [Serializable]
    public abstract class ClipCrossBlend : Clip
    {
        [SerializeField][HideInInspector] protected float blendIn = 0f; // 渐入时间
        [SerializeField][HideInInspector] protected float blendOut = 0f; // 渐出时间

        [SerializeField][HideInInspector] private float CrossBlendIn = 0f; // 交叉渐入时间
        [SerializeField][HideInInspector] private float CrossBlendOut = 0f; // 交叉渐出时间

        /// <summary>
        /// 是否允许交叉混合（固定为true）
        /// </summary>
        public override bool CanCrossBlend => true;

        /// <summary>
        /// 获取或设置渐入时间
        /// </summary>
        [MenuName("渐入时间")]
        public override float BlendIn
        {
            get
            {
                if (CrossBlendIn > 0)
                {
                    return CrossBlendIn;
                }

                return blendIn;
            }
            set
            {
                blendIn = value;
                if (blendIn < 0)
                {
                    blendIn = 0;
                }
                else if (blendIn > Length - BlendOut)
                {
                    blendIn = Length - BlendOut;
                }
            }
        }

        /// <summary>
        /// 获取或设置渐出时间
        /// </summary>
        [MenuName("渐出时间")]
        public override float BlendOut
        {
            get
            {
                if (CrossBlendOut > 0)
                {
                    return CrossBlendOut;
                }

                return blendOut;
            }
            set
            {
                blendOut = value;
                if (blendOut < 0)
                {
                    blendOut = 0;
                }
                else if (blendOut > Length - BlendIn)
                {
                    blendOut = Length - BlendIn;
                }
            }
        }

        /// <summary>
        /// 设置交叉渐入时间
        /// </summary>
        public override void SetCrossBlendIn(float value)
        {
            CrossBlendIn = value;
        }

        /// <summary>
        /// 设置交叉渐出时间
        /// </summary>
        public override void SetCrossBlendOut(float value)
        {
            CrossBlendOut = value;
        }
    }
}