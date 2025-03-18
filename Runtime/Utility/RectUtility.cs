using UnityEngine;

namespace NBC.ActionEditor
{
    /// <summary>
    /// 矩形工具类，提供一系列与矩形相关的实用方法
    /// </summary>
    public static class RectUtility
    {
        /// <summary>
        /// 获取一个包含所有给定矩形的最小边界矩形
        /// </summary>
        /// <param name="rects">需要包含的矩形数组</param>
        /// <returns>包含所有矩形的最小边界矩形</returns>
        public static Rect GetBoundRect(params Rect[] rects) {
            var xMin = float.PositiveInfinity;
            var xMax = float.NegativeInfinity;
            var yMin = float.PositiveInfinity;
            var yMax = float.NegativeInfinity;

            for ( var i = 0; i < rects.Length; i++ ) {
                xMin = Mathf.Min(xMin, rects[i].xMin);
                xMax = Mathf.Max(xMax, rects[i].xMax);
                yMin = Mathf.Min(yMin, rects[i].yMin);
                yMax = Mathf.Max(yMax, rects[i].yMax);
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        /// <summary>
        /// 获取一个包含所有给定点的最小边界矩形
        /// </summary>
        /// <param name="positions">需要包含的点数组</param>
        /// <returns>包含所有点的最小边界矩形</returns>
        public static Rect GetBoundRect(params Vector2[] positions) {
            var xMin = float.PositiveInfinity;
            var xMax = float.NegativeInfinity;
            var yMin = float.PositiveInfinity;
            var yMax = float.NegativeInfinity;

            for ( var i = 0; i < positions.Length; i++ ) {
                xMin = Mathf.Min(xMin, positions[i].x);
                xMax = Mathf.Max(xMax, positions[i].x);
                yMin = Mathf.Min(yMin, positions[i].y);
                yMax = Mathf.Max(yMax, positions[i].y);
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        /// <summary>
        /// 判断矩形a是否完全包含矩形b
        /// </summary>
        /// <param name="a">外层矩形</param>
        /// <param name="b">内层矩形</param>
        /// <returns>如果a完全包含b则返回true，否则返回false</returns>
        public static bool Encapsulates(this Rect a, Rect b) {
            if ( a == default(Rect) || b == default(Rect) ) { return false; }
            return a.x < b.x && a.xMax > b.xMax && a.y < b.y && a.yMax > b.yMax;
        }

        /// <summary>
        /// 按指定边距扩展矩形
        /// </summary>
        /// <param name="rect">原始矩形</param>
        /// <param name="margin">扩展边距</param>
        /// <returns>扩展后的矩形</returns>
        public static Rect ExpandBy(this Rect rect, float margin) {
            return rect.ExpandBy(margin, margin);
        }

        /// <summary>
        /// 按指定X和Y边距扩展矩形
        /// </summary>
        /// <param name="rect">原始矩形</param>
        /// <param name="xMargin">X轴扩展边距</param>
        /// <param name="yMargin">Y轴扩展边距</param>
        /// <returns>扩展后的矩形</returns>
        public static Rect ExpandBy(this Rect rect, float xMargin, float yMargin) {
            return Rect.MinMaxRect(rect.xMin - xMargin, rect.yMin - yMargin, rect.xMax + xMargin, rect.yMax + yMargin);
        }

        /// <summary>
        /// 将矩形从一个容器空间转换到另一个容器空间
        /// </summary>
        /// <param name="rect">需要转换的矩形</param>
        /// <param name="oldContainer">原始容器空间</param>
        /// <param name="newContainer">目标容器空间</param>
        /// <returns>转换后的矩形</returns>
        public static Rect TransformSpace(this Rect rect, Rect oldContainer, Rect newContainer) {
            var result = new Rect();
            result.xMin = Mathf.Lerp(newContainer.xMin, newContainer.xMax, Mathf.InverseLerp(oldContainer.xMin, oldContainer.xMax, rect.xMin));
            result.xMax = Mathf.Lerp(newContainer.xMin, newContainer.xMax, Mathf.InverseLerp(oldContainer.xMin, oldContainer.xMax, rect.xMax));
            result.yMin = Mathf.Lerp(newContainer.yMin, newContainer.yMax, Mathf.InverseLerp(oldContainer.yMin, oldContainer.yMax, rect.yMin));
            result.yMax = Mathf.Lerp(newContainer.yMin, newContainer.yMax, Mathf.InverseLerp(oldContainer.yMin, oldContainer.yMax, rect.yMax));
            return result;
        }

        /// <summary>
        /// 将Bounds对象转换为相机视角下的屏幕空间矩形
        /// </summary>
        /// <param name="b">需要转换的Bounds对象</param>
        /// <param name="cam">用于转换的相机</param>
        /// <returns>转换后的屏幕空间矩形</returns>
        public static Rect ToViewRect(this Bounds b, Camera cam) {

            var distance = cam.WorldToViewportPoint(b.center).z;
            if ( distance < 0 ) {
                return new Rect();
            }

            //8个顶点
            var pts = new Vector2[8];
            pts[0] = cam.WorldToViewportPoint(new Vector3(b.center.x + b.extents.x, b.center.y + b.extents.y, b.center.z + b.extents.z));
            pts[1] = cam.WorldToViewportPoint(new Vector3(b.center.x + b.extents.x, b.center.y + b.extents.y, b.center.z - b.extents.z));
            pts[2] = cam.WorldToViewportPoint(new Vector3(b.center.x + b.extents.x, b.center.y - b.extents.y, b.center.z + b.extents.z));
            pts[3] = cam.WorldToViewportPoint(new Vector3(b.center.x + b.extents.x, b.center.y - b.extents.y, b.center.z - b.extents.z));
            pts[4] = cam.WorldToViewportPoint(new Vector3(b.center.x - b.extents.x, b.center.y + b.extents.y, b.center.z + b.extents.z));
            pts[5] = cam.WorldToViewportPoint(new Vector3(b.center.x - b.extents.x, b.center.y + b.extents.y, b.center.z - b.extents.z));
            pts[6] = cam.WorldToViewportPoint(new Vector3(b.center.x - b.extents.x, b.center.y - b.extents.y, b.center.z + b.extents.z));
            pts[7] = cam.WorldToViewportPoint(new Vector3(b.center.x - b.extents.x, b.center.y - b.extents.y, b.center.z - b.extents.z));

            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for ( var i = 0; i < pts.Length; i++ ) {
                pts[i].y = 1 - pts[i].y;
                min = Vector2.Min(min, pts[i]);
                max = Vector2.Max(max, pts[i]);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}