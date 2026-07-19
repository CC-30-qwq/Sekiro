using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sekiro.BehaviourTree.Utils
{
    /// <summary>
    /// 权重随机工具类 — 消除 WeightedRandomSelector 与 CreateWeightedAttackNode 之间的算法重复
    /// 提供通用的、零GC分配的加权随机选择方法
    /// </summary>
    public static class WeightedRandomUtil
    {
        /// <summary>
        /// 从可枚举集合中按权重随机选择一个元素
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="items">元素集合</param>
        /// <param name="weightSelector">权重选择器函数</param>
        /// <returns>随机选中的元素，如果集合为空或总权重<=0则返回 default</returns>
        public static T Select<T>(IEnumerable<T> items, Func<T, float> weightSelector)
        {
            // 第一阶段：计算总权重
            float totalWeight = 0f;
            foreach (var item in items)
            {
                totalWeight += weightSelector(item);
            }

            if (totalWeight <= 0f) return default;

            // 第二阶段：随机选择（使用全限定名避免 System.Random 歧义）
            float randomValue = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;
            foreach (var item in items)
            {
                cumulative += weightSelector(item);
                if (randomValue <= cumulative)
                    return item;
            }

            return default;
        }

        /// <summary>
        /// 从列表中按权重随机选择一个元素的索引
        /// </summary>
        /// <param name="weights">权重列表</param>
        /// <returns>选中的索引，如果列表为空或总权重<=0则返回 -1</returns>
        public static int SelectIndex(IList<float> weights)
        {
            if (weights == null || weights.Count == 0) return -1;

            // 计算总权重
            float totalWeight = 0f;
            for (int i = 0; i < weights.Count; i++)
            {
                totalWeight += weights[i];
            }

            if (totalWeight <= 0f) return -1;

            // 随机选择（使用全限定名避免 System.Random 歧义）
            float randomValue = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;
            for (int i = 0; i < weights.Count; i++)
            {
                cumulative += weights[i];
                if (randomValue <= cumulative)
                    return i;
            }

            return weights.Count - 1; // 兜底
        }
    }
}
