using System;
using System.Collections.Generic;
using System.Linq;
using FullSerializer;
using UnityEngine;

namespace NBC.ActionEditor
{
    /// <summary>
    /// Asset 类是动作编辑器中的核心资产类，负责管理组、轨道和片段等元素
    /// </summary>
    [Serializable]
    public abstract class Asset : IDirector
    {
        /// <summary>
        /// 组列表
        /// </summary>
        [HideInInspector]
        public List<Group> groups = new();

        /// <summary>
        /// 资产总长度
        /// </summary>
        [SerializeField]
        private float length = 5f;

        /// <summary>
        /// 视图最小时间
        /// </summary>
        [SerializeField]
        private float viewTimeMin;

        /// <summary>
        /// 视图最大时间
        /// </summary>
        [SerializeField]
        private float viewTimeMax = 5f;

        /// <summary>
        /// 范围最小值
        /// </summary>
        [SerializeField]
        private float rangeMin;

        /// <summary>
        /// 范围最大值
        /// </summary>
        [SerializeField]
        private float rangeMax = 5f;

        /// <summary>
        /// 构造函数，初始化资产
        /// </summary>
        public Asset()
        {
            Init();
        }

        /// <summary>
        /// 可操作对象列表
        /// </summary>
        [fsIgnore]
        public List<IDirectable> directables { get; private set; }

        /// <summary>
        /// 获取或设置资产长度，最小值为0.1
        /// </summary>
        public float Length
        {
            get => length;
            set => length = Mathf.Max(value, 0.1f);
        }

        /// <summary>
        /// 获取或设置视图最小时间，确保不超过视图最大时间
        /// </summary>
        public float ViewTimeMin
        {
            get => viewTimeMin;
            set
            {
                if (ViewTimeMax > 0) viewTimeMin = Mathf.Min(value, ViewTimeMax - 0.25f);
            }
        }

        /// <summary>
        /// 获取或设置视图最大时间，确保不小于视图最小时间
        /// </summary>
        public float ViewTimeMax
        {
            get => viewTimeMax;
            set => viewTimeMax = Mathf.Max(value, ViewTimeMin + 0.25f, 0);
        }

        /// <summary>
        /// 获取视图时间范围
        /// </summary>
        public float ViewTime => ViewTimeMax - ViewTimeMin;

        /// <summary>
        /// 获取或设置范围最小值，确保不小于0
        /// </summary>
        public float RangeMin
        {
            get => rangeMin;
            set
            {
                rangeMin = value;
                if (rangeMin < 0) rangeMin = 0;
            }
        }

        /// <summary>
        /// 获取或设置范围最大值，确保不小于资产长度
        /// </summary>
        public float RangeMax
        {
            get => rangeMax;
            set
            {
                rangeMax = value;
                if (rangeMax < length) rangeMax = length;
            }
        }

        /// <summary>
        /// 更新最大时间，根据所有组、轨道和片段的结束时间计算
        /// </summary>
        public void UpdateMaxTime()
        {
            var t = 0f;
            foreach (var group in groups)
            {
                if (!group.IsActive) continue;
                foreach (var track in group.Tracks)
                {
                    if (!track.IsActive) continue;
                    foreach (var clip in track.Clips)
                        if (clip.EndTime > t)
                            t = clip.EndTime;
                }
            }

            Length = t;
        }

        /// <summary>
        /// 删除指定组
        /// </summary>
        /// <param name="group">要删除的组</param>
        public void DeleteGroup(Group group)
        {
            groups.Remove(group);
            Validate();
        }

        /// <summary>
        /// 验证资产，更新所有可操作对象的状态
        /// </summary>
        public void Validate()
        {
            directables = new List<IDirectable>();
            foreach (IDirectable group in groups.AsEnumerable().Reverse())
            {
                directables.Add(group);
                try
                {
                    group.Validate(this, null);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                foreach (var track in group.Children.Reverse())
                {
                    directables.Add(track);
                    try
                    {
                        track.Validate(this, group);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }

                    foreach (var clip in track.Children)
                    {
                        directables.Add(clip);
                        try
                        {
                            clip.Validate(this, track);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }
            }

            if (directables != null)
                foreach (var d in directables)
                    d.OnAfterDeserialize();

            UpdateMaxTime();
        }

        /// <summary>
        /// 添加指定类型的组
        /// </summary>
        /// <param name="type">组类型</param>
        /// <returns>新添加的组</returns>
        public Group AddGroup(Type type)
        {
            if (!typeof(Group).IsAssignableFrom(type)) return null;
            var newGroup = Activator.CreateInstance(type) as Group;
            if (newGroup != null)
            {
                newGroup.Name = "New Group";
                groups.Add(newGroup);
                Validate();
            }

            return newGroup;
        }

        /// <summary>
        /// 添加指定类型的组，支持泛型
        /// </summary>
        /// <typeparam name="T">组类型</typeparam>
        /// <param name="name">组名称</param>
        /// <returns>新添加的组</returns>
        public T AddGroup<T>(string name = "") where T : Group, new()
        {
            var newGroup = new T();
            if (string.IsNullOrEmpty(name))
            {
                name = newGroup.GetType().Name;
            }

            newGroup.Name = name;
            groups.Add(newGroup);
            Validate();
            return newGroup;
        }

        /// <summary>
        /// 初始化资产
        /// </summary>
        public void Init()
        {
            Validate();
        }

        /// <summary>
        /// 序列化前调用，更新所有可操作对象的序列化状态
        /// </summary>
        public void OnBeforeSerialize()
        {
            if (directables != null)
                foreach (var d in directables)
                    d.OnBeforeSerialize();
        }

        /// <summary>
        /// 反序列化后调用
        /// </summary>
        public void OnAfterDeserialize()
        {
        }
    }
}