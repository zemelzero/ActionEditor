namespace NBC.ActionEditor
{
    /// <summary>
    /// 定义包含子剪辑的接口，继承自IDirectable
    /// </summary>
    public interface ISubClipContainable : IDirectable
    {
        /// <summary>
        /// 获取或设置子剪辑的偏移量
        /// </summary>
        float SubClipOffset { get; set; }
        
        /// <summary>
        /// 获取子剪辑的播放速度
        /// </summary>
        float SubClipSpeed { get; }
        
        /// <summary>
        /// 获取子剪辑的长度
        /// </summary>
        float SubClipLength { get; }
    }
}