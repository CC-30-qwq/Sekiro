namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 替代 GroundBehaviour/CombatBehaviour/SprintBehaviour 中间件的配置结构体。
    /// 每个叶子状态在 OnEnter 中设置对应字段，PlayerBaseBehaviour 统一消费。
    /// </summary>
    public struct PlayerBehaviourProfile
    {
        public bool ShouldApplyFloat;
        public bool EnableHitTransition;
        public bool EnableFallTransition;
        public float FallForceValue;

        /// <summary>地面移动默认值：Float + 受击过渡 + 坠落过渡</summary>
        public static PlayerBehaviourProfile GroundDefault(float fallForce)
        {
            return new PlayerBehaviourProfile
            {
                ShouldApplyFloat = true,
                EnableHitTransition = true,
                EnableFallTransition = true,
                FallForceValue = fallForce,
            };
        }

        /// <summary>战斗默认值：Float + 坠落过渡（无受击过渡，各战斗子状态自行处理）</summary>
        public static PlayerBehaviourProfile CombatDefault(float fallForce)
        {
            return new PlayerBehaviourProfile
            {
                ShouldApplyFloat = true,
                EnableHitTransition = false,
                EnableFallTransition = true,
                FallForceValue = fallForce,
            };
        }

        /// <summary>无 Float 的纯逻辑状态（默认值即为全 false）</summary>
        public static PlayerBehaviourProfile None => default;
    }
}
