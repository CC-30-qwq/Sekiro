using System.Collections.Generic;
using Sekiro.BehaviourMachine;
using Sekiro.BehaviourTree.Utils;
using static Sekiro.BehaviourTree.Builder.NodeBuilder;

namespace Sekiro.BehaviourTree
{
    /// <summary>
    /// 敌人行为树实现
    ///
    /// 决策逻辑：
    ///   1. 远距离（Far）→ 追逐 Chase
    ///   2. 中距离（Mid）→ 按权重随机选择 攻击 / 巡逻 Stander
    ///   3. 兜底 → 空闲 Idle
    ///
    /// 攻击选择策略：
    ///   由 EnemyAttackMappingSO 数据驱动（合并了权重/距离/连击配置），
    ///   根据距离动态过滤可用攻击列表，再按权重随机选择具体攻击行为。
    /// </summary>
    public class EnemyBehaviourTree : BehaviourTree
    {
        public override void BuildTree()
        {
            base.BuildTree();

            Root = Selector()
                // ── 远距离 → 追逐 ──
                .Try(
                    Sequence()
                        .Then(Condition(() => Conditions.IsAtRange(DistanceRange.Far)))
                        .Then(Action(() =>
                        {
                            Actions.ChangeTo(EnemyBehaviour.Chase);
                            return NodeState.Success;
                        }))
                        .Build()
                )
                // ── 中距离 → 攻击或巡逻 ──
                .Try(
                    Sequence()
                        .Then(Condition(() => Conditions.IsInRange(DistanceRange.Mid)))
                        .Then(
                            WeightedRandom()
                                .Option(CreateDynamicAttackNode(), 0.7f)   // 70% 概率攻击
                                .Option(
                                    Action(() =>
                                    {
                                        Actions.ChangeTo(EnemyBehaviour.Stander);
                                        return NodeState.Success;
                                    }),
                                    0.3f                                    // 30% 概率巡逻
                                )
                                .Build()
                        )
                        .Build()
                )
                // ── 兜底：空闲 ──
                .Try(
                    Action(() =>
                    {
                        Actions.ChangeTo(EnemyBehaviour.Idle);
                        return NodeState.Success;
                    })
                )
                .Build();
        }

        /// <summary>
        /// 创建动态攻击选择节点
        ///
        /// 每次 Evaluate() 时从 BehaviourMachine 的 availableAttacks 列表中，
        /// 使用 WeightedRandomUtil 按权重选择一个攻击行为并切换到该状态。
        ///
        /// 此节点作为 WeightedRandomSelector 的子节点参与随机选择，
        /// 因此不再自建权重系统，职责单一。
        ///
        /// 返回 Success — 状态切换是瞬时操作，不持续多帧。
        /// 返回 Failure — 无可用攻击时，让父节点尝试其他分支。
        /// </summary>
        private Node CreateDynamicAttackNode()
        {
            return new Action(() =>
            {
                var availableAttacks = BehaviourMachine.Variable.AvailableAttacks;

                // 无可用攻击 → 让父节点尝试其他分支（如巡逻）
                if (availableAttacks == null || availableAttacks.Count == 0)
                {
                    return NodeState.Failure;
                }

                // 从可用攻击列表中按权重随机选择
                var selected = WeightedRandomUtil.Select(availableAttacks, entry => entry.weight);
                if (selected.behaviour == EnemyBehaviour.Idle)
                {
                    return NodeState.Failure;
                }

                // ✅ 修正：状态切换是瞬时操作，返回 Success
                Actions.ChangeTo(selected.behaviour);
                return NodeState.Success;
            });
        }
    }
}
