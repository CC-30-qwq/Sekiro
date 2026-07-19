using Sekiro.BehaviourMachine;
using Sekiro.Enemy.Setting;

namespace Sekiro.BehaviourTree.Utils
{
    /// <summary>
    /// 距离范围判定工具类 — 消除 EnemyConditions / EnemyBehaviourMachine 之间三重距离判定重复
    /// 所有距离判定逻辑集中在此处，修改距离配置只需改这一个地方
    /// </summary>
    public static class DistanceUtility
    {
        /// <summary>
        /// 判断给定距离是否在指定距离范围内（使用 > 边界，适合"超出某范围"的场景）
        /// </summary>
        public static bool IsAtRange(float distance, DistanceRange range, EnemyConfigSO config)
        {
            if (config == null) return false;

            switch (range)
            {
                case DistanceRange.Close:    return distance <= config.CloseRange;
                case DistanceRange.Mid:      return distance > config.CloseRange && distance <= config.MidRange;
                case DistanceRange.Far:      return distance > config.MidRange && distance <= config.FarRange;
                case DistanceRange.OutOfRange: return distance > config.FarRange;
                default: return false;
            }
        }

        /// <summary>
        /// 判断给定距离是否在指定距离范围内（使用 <= 边界，适合"在范围内"的场景）
        /// </summary>
        public static bool IsInRange(float distance, DistanceRange range, EnemyConfigSO config)
        {
            if (config == null) return false;

            switch (range)
            {
                case DistanceRange.Close:    return distance <= config.CloseRange;
                case DistanceRange.Mid:      return distance <= config.MidRange;
                case DistanceRange.Far:      return distance <= config.FarRange;
                case DistanceRange.OutOfRange: return distance > config.FarRange;
                default: return false;
            }
        }

        /// <summary>
        /// 根据距离获取对应的 DistanceRange 枚举值
        /// </summary>
        public static DistanceRange GetDistanceRange(float distance, EnemyConfigSO config)
        {
            if (config == null) return DistanceRange.OutOfRange;

            if (distance <= config.CloseRange) return DistanceRange.Close;
            if (distance <= config.MidRange)   return DistanceRange.Mid;
            if (distance <= config.FarRange)   return DistanceRange.Far;

            return DistanceRange.OutOfRange;
        }
    }
}
