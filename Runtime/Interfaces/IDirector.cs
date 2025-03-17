using System.Collections.Generic;
using UnityEngine;

namespace NBC.ActionEditor
{
    /// <summary>
    /// IDirector 接口定义了导演（Director）的基本行为和属性。
    /// 该接口继承自 IData 接口，用于管理时间轴、视图范围等。
    /// </summary>
    public interface IDirector : IData
    {
        /// <summary>
        /// 获取时间轴的总长度。
        /// </summary>
        float Length { get; }

        /// <summary>
        /// 获取或设置视图时间的最小值。
        /// </summary>
        public float ViewTimeMin { get; set; }

        /// <summary>
        /// 获取或设置视图时间的最大值。
        /// </summary>
        public float ViewTimeMax { get; set; }

        /// <summary>
        /// 获取当前视图时间。
        /// </summary>
        public float ViewTime { get; }

        /// <summary>
        /// 获取或设置时间轴范围的最小值。
        /// </summary>
        public float RangeMin { get; set; }

        /// <summary>
        /// 获取或设置时间轴范围的最大值。
        /// </summary>
        public float RangeMax { get; set; }

        /// <summary>
        /// 删除指定的组（Group）。
        /// </summary>
        /// <param name="group">要删除的组对象。</param>
        void DeleteGroup(Group group);

        /// <summary>
        /// 更新时间轴的最大时间。
        /// </summary>
        void UpdateMaxTime();

        /// <summary>
        /// 验证时间轴的有效性。
        /// </summary>
        void Validate();
    }
}