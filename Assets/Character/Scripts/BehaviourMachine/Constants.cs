namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 全局常量类 — 集中管理所有硬编码数值，消除重复魔法数字
    /// </summary>
    public static class Constants
    {
        // ============ 检测阈值 ============

        /// <summary>
        /// 地面检测阈值（用于sqrMagnitude比较）
        /// </summary>
        public const float GroundCheckThreshold = 0.0001f;

        /// <summary>
        /// 向量接近零的判断阈值（通用用途，如动画参数收敛判断）
        /// </summary>
        public const float VectorNearZeroThreshold = 0.0001f;

        // ============ 更新间隔 ============

        /// <summary>
        /// 地面检测更新间隔（秒）- 50Hz
        /// </summary>
        public const float GroundCheckInterval = 0.02f;

        /// <summary>
        /// 输入方向更新间隔（秒）- 30Hz
        /// </summary>
        public const float InputUpdateInterval = 0.033f;

        // ============ 对象池 & 缓冲区 ============

        /// <summary>
        /// 输入缓冲区预分配容量（InputBuffer 循环队列）
        /// </summary>
        public const int InputBufferCapacity = 16;

        /// <summary>
        /// 碰撞器检测数组最大容量（GroundDetectionUtility + TargetDetector 复用）
        /// </summary>
        public const int MaxColliderArraySize = 32;

        // ============ 销毁/延迟帧守卫 ============

        /// <summary>
        /// 默认销毁延迟帧数（Projectile.OnTriggerEnter 帧守卫）
        /// </summary>
        public const int DefaultDestroyDelayFrames = 3;

        // ============ 性能安全限制 ============

        /// <summary>
        /// 每帧最大输入事件数 — 防止输入缓冲区滥用的安全上限
        /// </summary>
        public const int MaxInputsPerFrame = 10;
    }
}

