using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NBC.ActionEditor
{
    /// <summary>
    /// IDirectable 扩展方法类，提供对 IDirectable 接口的扩展功能
    /// </summary>
    public static class IDirectableExtensions
    {
        #region 长度 and 时间转换

        /// <summary>
        /// 获取 IDirectable 的长度
        /// </summary>
        /// <param name="directable">IDirectable 对象</param>
        /// <returns>长度值</returns>
        public static float GetLength(this IDirectable directable)
        {
            return directable.EndTime - directable.StartTime;
        }

        /// <summary>
        /// 将全局时间转换为本地时间，并进行范围限制
        /// </summary>
        /// <param name="directable">IDirectable 对象</param>
        /// <param name="time">全局时间</param>
        /// <returns>本地时间</returns>
        public static float ToLocalTime(this IDirectable directable, float time)
        {
            return Mathf.Clamp(time - directable.StartTime, 0, directable.GetLength());
        }

        /// <summary>
        /// 将全局时间转换为本地时间，不进行范围限制
        /// </summary>
        /// <param name="directable">IDirectable 对象</param>
        /// <param name="time">全局时间</param>
        /// <returns>本地时间</returns>
        public static float ToLocalTimeUnclamped(this IDirectable directable, float time)
        {
            return time - directable.StartTime;
        }

        #endregion

        #region 操作判定

        /// <summary>
        /// 判断两个 IDirectable 是否可以交叉混合
        /// </summary>
        /// <param name="directable">当前 IDirectable 对象</param>
        /// <param name="other">另一个 IDirectable 对象</param>
        /// <returns>是否可以交叉混合</returns>
        public static bool CanCrossBlend(this IDirectable directable, IDirectable other)
        {
            if (directable == null || other == null) return false;

            if ((directable.CanCrossBlend || other.CanCrossBlend) && directable.GetType() == other.GetType())
                return true;

            return false;
        }

        /// <summary>
        /// 判断 IDirectable 是否可以混合进入
        /// </summary>
        /// <param name="directable">IDirectable 对象</param>
        /// <returns>是否可以混合进入</returns>
        public static bool CanBlendIn(this IDirectable directable)
        {
            var blendInProp = directable.GetType().GetProperty("BlendIn", BindingFlags.Instance | BindingFlags.Public);
            return blendInProp != null && blendInProp.CanWrite && Math.Abs(directable.BlendIn - -1) > 0.0001f &&
                   blendInProp.DeclaringType != typeof(Clip);
        }

        /// <summary>
        /// 判断 IDirectable 是否可以混合退出
        /// </summary>
        /// <param name="directable">IDirectable 对象</param>
        /// <returns>是否可以混合退出</returns>
        public static bool CanBlendOut(this IDirectable directable)
        {
            var blendOutProp =
                directable.GetType().GetProperty("BlendOut", BindingFlags.Instance | BindingFlags.Public);
            return blendOutProp != null && blendOutProp.CanWrite && Math.Abs(directable.BlendOut - -1) > 0.0001f &&
                   blendOutProp.DeclaringType != typeof(Clip);
        }

        /// <summary>
        /// 判断 IDirectable 是否可以缩放
        /// </summary>
        /// <param name="directable">IDirectable 对象</param>
        /// <returns>是否可以缩放</returns>
        public static bool CanScale(this IDirectable directable)
        {
            var lengthProp = directable.GetType().GetProperty("Length", BindingFlags.Instance | BindingFlags.Public);
            return lengthProp != null && lengthProp.CanWrite && lengthProp.DeclaringType != typeof(Clip);
        }

        /// <summary>
        /// 判断当前开始时间是否有效
        /// </summary>
        /// <param name="directable">IDirectable 对象</param>
        /// <returns>开始时间是否有效</returns>
        public static bool CanValidTime(this IDirectable directable)
        {
            return CanValidTime(directable, directable.StartTime, directable.EndTime);
        }

        /// <summary>
        /// 判断指定开始时间和结束时间是否有效
        /// </summary>
        /// <param name="directable">IDirectable 对象</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>时间是否有效</returns>
        public static bool CanValidTime(this IDirectable directable, float startTime, float endTime)
        {
            if (directable.Parent != null)
            {
                return CanValidTime(directable, directable.Parent, startTime, endTime);
            }

            return true;
        }

        /// <summary>
        /// 判断指定父对象下的开始时间和结束时间是否有效
        /// </summary>
        /// <param name="directable">IDirectable 对象</param>
        /// <param name="parent">父对象</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>时间是否有效</returns>
        public static bool CanValidTime(this IDirectable directable, IDirectable parent, float startTime, float endTime)
        {
            var prevDirectable = directable.GetPreviousSibling(parent);
            var nextDirectable = directable.GetNextSibling(parent);

            var limitStartTime = 0f;
            var limitEndTime = float.MaxValue;

            if (prevDirectable != null)
            {
                limitStartTime = prevDirectable.EndTime;
                if (directable.CanCrossBlend(prevDirectable))
                {
                    limitStartTime = prevDirectable.StartTime;

                    //如果完全包含
                    if (startTime > limitStartTime && endTime < prevDirectable.EndTime)
                    {
                        return false;
                    }
                }
            }

            if (nextDirectable != null)
            {
                limitEndTime = nextDirectable.StartTime;
                if (directable.CanCrossBlend(nextDirectable))
                {
                    limitEndTime = nextDirectable.EndTime;
                }
            }

            if (limitStartTime - startTime > 0.0001f) //直接比大小存在精度问题
            {
                return false;
            }

            if (endTime - limitEndTime > 0.0001f)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region 切片获取

        /// <summary>
        /// 获取与当前 IDirectable 同级的切片
        /// </summary>
        /// <param name="directable">IDirectable 对象</param>
        /// <returns>同级的切片数组</returns>
        public static IDirectable[] GetCoincideSibling(this IDirectable directable)
        {
            return GetCoincideSibling(directable, directable.Parent);
        }

        /// <summary>
        /// 获取指定父对象下与当前 IDirectable 同级的切片
        /// </summary>
        /// <param name="directable">IDirectable 对象</param>
        /// <param name="parent">父对象</param>
        /// <returns>同级的切片数组</returns>
        public static IDirectable[] GetCoincideSibling(this IDirectable directable, IDirectable parent)
        {
            if (parent != null)
            {
                return parent.Children.Where(child => child != directable).Where(child =>
                        child.StartTime == directable.StartTime && child.EndTime == directable.EndTime)
                    .ToArray();
            }

            return Array.Empty<IDirectable>();
        }

        /// <summary>
        /// 获取当前 IDirectable 的上一个同级切片
        /// </summary>
        /// <typeparam name="T">切片类型</typeparam>
        /// <param name="directable">IDirectable 对象</param>
        /// <returns>上一个同级切片</returns>
        public static T GetPreviousSibling<T>(this IDirectable directable) where T : IDirectable
        {
            return (T)GetPreviousSibling(directable, directable.Parent);
        }

        /// <summary>
        /// 获取当前 IDirectable 的上一个同级切片
        /// </summary>
        /// <param name="directable">IDirectable 对象</param>
        /// <returns>上一个同级切片</returns>
        public static IDirectable GetPreviousSibling(this IDirectable directable)
        {
            return GetPreviousSibling(directable, directable.Parent);
        }

        /// <summary>
        /// 获取指定父对象下当前 IDirectable 的上一个同级切片
        /// </summary>
        /// <param name="directable">IDirectable 对象</param>
        /// <param name="parent">父对象</param>
        /// <returns>上一个同级切片</returns>
        public static IDirectable GetPreviousSibling(this IDirectable directable, IDirectable parent)
        {
            if (parent != null)
            {
                return parent.Children.LastOrDefault(d =>
                    d != directable && (d.StartTime < directable.StartTime));
            }

            return null;
        }

        /// <summary>
        /// 获取当前 IDirectable 的下一个同级切片
        /// </summary>
        /// <typeparam name="T">切片类型</typeparam>
        /// <param name="directable">IDirectable 对象</param>
        /// <returns>下一个同级切片</returns>
        public static T GetNextSibling<T>(this IDirectable directable) where T : IDirectable
        {
            return (T)GetNextSibling(directable, directable.Parent);
        }

        /// <summary>
        /// 获取当前 IDirectable 的下一个同级切片
        /// </summary>
        /// <param name="directable">IDirectable 对象</param>
        /// <returns>下一个同级切片</returns>
        public static IDirectable GetNextSibling(this IDirectable directable)
        {
            return GetNextSibling(directable, directable.Parent);
        }

        /// <summary>
        /// 获取指定父对象下当前 IDirectable 的下一个同级切片
        /// </summary>
        /// <param name="directable">IDirectable 对象</param>
        /// <param name="parent">父对象</param>
        /// <returns>下一个同级切片</returns>
        public static IDirectable GetNextSibling(this IDirectable directable, IDirectable parent)
        {
            if (parent != null)
            {
                return parent.Children.FirstOrDefault(d =>
                    d != directable && d.StartTime > directable.StartTime);
            }

            return null;
        }

        #endregion

        #region 混合权重

        /// <summary>
        /// 根据混合特性获取指定本地时间的权重
        /// </summary>
        /// <param name="directable">IDirectable 对象</param>
        /// <param name="time">本地时间</param>
        /// <returns>权重值</returns>
        public static float GetWeight(this IDirectable directable, float time)
        {
            return GetWeight(directable, time, directable.BlendIn, directable.BlendOut);
        }

        /// <summary>
        /// 根据提供的混合入/出属性获取指定本地时间的权重
        /// </summary>
        /// <param name="directable">IDirectable 对象</param>
        /// <param name="time">本地时间</param>
        /// <param name="blendInOut">混合入/出值</param>
        /// <returns>权重值</returns>
        public static float GetWeight(this IDirectable directable, float time, float blendInOut)
        {
            return GetWeight(directable, time, blendInOut, blendInOut);
        }

        /// <summary>
        /// 根据提供的混合入和混合出属性获取指定本地时间的权重
        /// </summary>
        /// <param name="directable">IDirectable 对象</param>
        /// <param name="time">本地时间</param>
        /// <param name="blendIn">混合入值</param>
        /// <param name="blendOut">混合出值</param>
        /// <returns>权重值</returns>
        public static float GetWeight(this IDirectable directable, float time, float blendIn, float blendOut)
        {
            var length = GetLength(directable);
            if (time <= 0) return blendIn <= 0 ? 1 : 0;

            if (time >= length) return blendOut <= 0 ? 1 : 0;

            if (time < blendIn) return time / blendIn;

            if (time > length - blendOut) return (length - time) / blendOut;

            return 1;
        }

        #endregion

        #region 循环长度

        /// <summary>
        /// 获取剪辑的上一个循环长度
        /// </summary>
        /// <param name="clip">ISubClipContainable 对象</param>
        /// <returns>上一个循环长度</returns>
        public static float GetPreviousLoopLocalTime(this ISubClipContainable clip)
        {
            var clipLength = clip.GetLength();
            var loopLength = clip.SubClipLength / clip.SubClipSpeed;
            if (clipLength > loopLength)
            {
                var mod = (clipLength - clip.SubClipOffset) % loopLength;
                var aproxZero = Mathf.Abs(mod) < 0.01f;
                return clipLength - (aproxZero ? loopLength : mod);
            }

            return clipLength;
        }

        /// <summary>
        /// 获取剪辑的下一个循环长度
        /// </summary>
        /// <param name="clip">ISubClipContainable 对象</param>
        /// <returns>下一个循环长度</returns>
        public static float GetNextLoopLocalTime(this ISubClipContainable clip)
        {
            var clipLength = clip.GetLength();
            var loopLength = clip.SubClipLength / clip.SubClipSpeed;
            var mod = (clipLength - clip.SubClipOffset) % loopLength;
            var aproxZero = Mathf.Abs(mod) < 0.01f || Mathf.Abs(loopLength - mod) < 0.01f;
            return clipLength + (aproxZero ? loopLength : loopLength - mod);
        }

        #endregion
    }
}