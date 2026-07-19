using UnityEngine;

namespace Sekiro.Combat
{
    /// <summary>
    /// 可检测实体接口 — 任何可被玩家视觉系统检测/锁定的物体都应实现此接口
    /// 
    /// 设计意图：
    /// - 解耦 PlayerVision 与具体组件（Health、CharacterTarget 等）的硬编码依赖
    /// - 支持扩展多种可检测类型（敌人、可交互物品、NPC等）
    /// - 每个实体可自定义检测点（DetectPoint）和优先级
    /// </summary>
    public interface IDetectable
    {
        /// <summary>检测目标的主 Transform（用于追踪、锁定）</summary>
        Transform RootTransform { get; }

        /// <summary>检测点（相机瞄准点）— 通常是 CharacterTarget 或头部位置</summary>
        Transform DetectPoint { get; }

        /// <summary>该实体当前是否可被检测（例如存活、未被隐藏等）</summary>
        bool IsDetectable { get; }

        /// <summary>检测优先级偏移值（正数提高优先级，负数降低）</summary>
        float PriorityBoost { get; }
    }
}
