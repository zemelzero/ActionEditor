using System.Collections.Generic;
using UnityEngine;

namespace NBC.ActionEditor
{
    /// <summary>
    /// IDirectable 接口定义了可被导演系统控制的对象的基本属性和方法
    /// 继承自 IData 接口
    /// </summary>
    public interface IDirectable : IData
    {
        /// <summary>
        /// 获取所属的导演对象
        /// </summary>
        IDirector Root { get; }

        /// <summary>
        /// 获取父级可导演对象
        /// </summary>
        IDirectable Parent { get; }

        /// <summary>
        /// 获取所有子级可导演对象
        /// </summary>
        IEnumerable<IDirectable> Children { get; }

        /// <summary>
        /// 获取关联的游戏对象
        /// </summary>
        GameObject Actor { get; }

        /// <summary>
        /// 获取对象名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 获取或设置对象是否激活
        /// </summary>
        bool IsActive { get; set; }

        /// <summary>
        /// 获取或设置对象是否折叠
        /// </summary>
        bool IsCollapsed { get; set; }

        /// <summary>
        /// 获取或设置对象是否锁定
        /// </summary>
        bool IsLocked { get; set; }

        /// <summary>
        /// 获取开始时间
        /// </summary>
        float StartTime { get; }

        /// <summary>
        /// 获取结束时间
        /// </summary>
        float EndTime { get; }

        /// <summary>
        /// 获取或设置淡入时间
        /// </summary>
        float BlendIn { get; set; }

        /// <summary>
        /// 获取或设置淡出时间
        /// </summary>
        float BlendOut { get; set; }

        /// <summary>
        /// 获取是否允许交叉淡入淡出
        /// </summary>
        bool CanCrossBlend { get; }

        /// <summary>
        /// 验证对象与导演和父级的关系
        /// </summary>
        /// <param name="root">所属导演</param>
        /// <param name="parent">父级对象</param>
        void Validate(IDirector root, IDirectable parent);

        /// <summary>
        /// 初始化对象
        /// </summary>
        /// <returns>初始化是否成功</returns>
        bool Initialize();

        /// <summary>
        /// 序列化前调用
        /// </summary>
        void OnBeforeSerialize();

        /// <summary>
        /// 反序列化后调用
        /// </summary>
        void OnAfterDeserialize();
    }

    /// <summary>
    /// IClip 接口表示一个可导演的片段，继承自 IDirectable
    /// </summary>
    public interface IClip : IDirectable
    {
        /// <summary>
        /// 获取动画参数的目标对象
        /// </summary>
        object AnimatedParametersTarget { get; }
    }
}