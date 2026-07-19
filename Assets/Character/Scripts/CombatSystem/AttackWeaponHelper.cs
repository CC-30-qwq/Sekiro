using System;
using System.Collections.Generic;
using Sekiro.BehaviourMachine;
using Sekiro.Character.Data;
using Sekiro.Combat;
using UnityEngine;

namespace Sekiro.Combat

{
    /// <summary>
    /// 攻击武器辅助静态类 — 消除 GroundAttackBehaviour 与 Enemy_AttackBehaviour 中的重复代码
    /// 
    /// 由于玩家（PlayerBaseBehaviour）和敌人（EnemyBaseBehaviour）继承自不同的基类，
    /// 无法直接提取共享泛型基类。因此将纯粹的工具方法提取到此静态类中。
    /// 
    /// 当前消除的重复：
    ///   1. TryEnableWeaponCollider — 武器碰撞器启用时序判定（两处代码完全一致）
    ///   2. TryFireRangedAttack — 远程投射物生成时序判定（Player/Enemy 通用）
    /// </summary>
    public static class AttackWeaponHelper
    {
        /// <summary>
        /// 静态缓存的 HashSet，避免每轮攻击分配新的堆内存。
        /// Unity 主线程单线程执行，不存在竞态条件，因此无需 [ThreadStatic] 标记。
        /// </summary>
        private static HashSet<int> _dispatchedIndicesCache;

        /// <summary>
        /// 唤起武器 HitCollider — 由 ComboConfigSO.HitColliderWindows 驱动
        /// 当 Attacking 阶段进度到达第一个窗口的 EnableTime 时，触发所有多段窗口调度
        /// Player/Enemy 通用版本
        /// 
        /// 根据 HitColliderWindow.WeaponIndex 自动分派到对应武器：
        /// - WeaponIndex == -1 → 默认武器（Machine.Weapons[0]）
        /// - WeaponIndex == N  → Machine.Weapons[N]
        /// </summary>
        /// <param name="allWeapons">角色注册的所有武器数组（Machine.Weapons）</param>
        /// <param name="currentCombo">当前连击配置</param>
        /// <param name="phaseDriver">攻击阶段驱动器（ref 值类型）</param>
        /// <param name="hasEnableCollider">是否已启用碰撞器的标志位（ref 引用）</param>
        public static void TryEnableWeaponCollider(
            Weapon[] allWeapons,
            ComboConfigSO currentCombo,
            ref AttackPhaseDriver phaseDriver,
            ref bool hasEnableCollider)
        {
            if (hasEnableCollider) return;
            if (currentCombo.HitColliderWindows == null || currentCombo.HitColliderWindows.Length == 0) return;
            if (allWeapons == null || allWeapons.Length == 0) return;

            // 使用 TotalElapsedTime（从 Initialize 起的总耗时）作为触发阈值
            float elapsed = phaseDriver.TotalElapsedTime;
            if (elapsed >= currentCombo.HitColliderWindows[0].EnableTime)
            {
                // 复用静态缓存的 HashSet，Clear() 代替 new 分配，实现零 GC。
                // Unity 主线程单线程执行，不存在竞态条件。
                _dispatchedIndicesCache ??= new HashSet<int>();
                _dispatchedIndicesCache.Clear();

                for (int i = 0; i < currentCombo.HitColliderWindows.Length; i++)
                {
                    int index = currentCombo.HitColliderWindows[i].WeaponIndex;

                    // -1 表示使用默认武器（索引0）
                    int actualIndex = (index == -1) ? 0 : index;

                    // 边界保护
                    if (actualIndex < 0 || actualIndex >= allWeapons.Length) continue;
                    if (!_dispatchedIndicesCache.Add(actualIndex)) continue; // 已分派过

                    var weapon = allWeapons[actualIndex];
                    if (weapon != null)
                    {
                        weapon.ExecuteAttack(currentCombo.HitColliderWindows, elapsed, actualIndex);
                    }
                }

                hasEnableCollider = true;
            }
        }

        /// <summary>
        /// 尝试发射远程投射物 — 由 ComboConfigSO.RangedHitWindows 驱动
        /// 逐帧轮询检查每个窗口的 EnableTime，到达时间后逐个触发。
        /// Player/Enemy 通用版本。
        /// 
        /// [改进] 从 "一次性触发所有窗口" 改为 "逐帧轮询逐个触发"，
        /// 支持多个 RangedHitWindow 在不同时间点生成投射物（如连射）。
        /// 
        /// 根据 RangedHitWindow.WeaponIndex 自动分派到对应远程武器：
        /// - WeaponIndex == -1 → 默认远程武器（RangedWeapons[0]）
        /// - WeaponIndex == N  → RangedWeapons[N]
        /// 
        /// 与 TryEnableWeaponCollider 分离，独立管理远程攻击的触发时序。
        /// 支持同一个 ComboConfigSO 同时配置近战窗口和远程窗口。
        /// </summary>
        /// <param name="allRangedWeapons">角色注册的所有远程武器数组（Machine.RangedWeapons）</param>
        /// <param name="currentCombo">当前连击配置</param>
        /// <param name="phaseDriver">攻击阶段驱动器（ref 值类型）</param>
        /// <param name="rangedWindowIndex">当前待处理的远程窗口索引（ref 引用，已触发的窗口索引递增）</param>
        /// <param name="overrideDirection">投射物发射方向覆盖（默认 Vector3.zero 使用武器自身朝向）</param>
        public static void TryFireRangedAttack(
            RangedWeapon[] allRangedWeapons,
            ComboConfigSO currentCombo,
            ref AttackPhaseDriver phaseDriver,
            ref int rangedWindowIndex,
            Vector3 overrideDirection = default)
        {
            if (currentCombo.RangedHitWindows == null || currentCombo.RangedHitWindows.Length == 0) return;
            if (allRangedWeapons == null || allRangedWeapons.Length == 0) return;

            int windowCount = currentCombo.RangedHitWindows.Length;
            // 所有窗口已处理完毕
            if (rangedWindowIndex >= windowCount) return;

            float elapsed = phaseDriver.TotalElapsedTime;

            // 逐帧轮询：从当前待处理窗口开始，处理所有已到达 EnableTime 的窗口
            while (rangedWindowIndex < windowCount &&
                   elapsed >= currentCombo.RangedHitWindows[rangedWindowIndex].EnableTime)
            {
                var window = currentCombo.RangedHitWindows[rangedWindowIndex];
                int index = window.WeaponIndex;

                // -1 表示使用默认远程武器（索引0）
                int actualIndex = (index == -1) ? 0 : index;

                if (actualIndex >= 0 && actualIndex < allRangedWeapons.Length)
                {
                    var rangedWeapon = allRangedWeapons[actualIndex];
                    if (rangedWeapon != null)
                    {
                        // 设置发射方向覆盖（如果需要）
                        if (overrideDirection != Vector3.zero)
                        {
                            rangedWeapon.OverrideDirection = overrideDirection;
                        }

                        rangedWeapon.Fire(window);
                    }
                }

                // 移动索引到下一个待处理窗口
                rangedWindowIndex++;
            }
        }

        /// <summary>
        /// 尝试进入下一段连击（通用版本，适用于策略类调用）
        /// 当玩家输入匹配时，切换到指定的下一段连击配置
        /// </summary>
        /// <param name="inputs">输入工具类实例</param>
        /// <param name="inputType">检测的输入类型（通常为 InputType.Attack）</param>
        /// <param name="nextCombo">下一段连击配置（为 null 时什么都不做）</param>
        /// <param name="variable">角色变量引用（用于设置 Variable.CurrentCombo）</param>
        /// <param name="needsComboInit">[ref] 重建连击标志</param>
        public static bool TryNextCombo(
            InputsUtility inputs,
            InputType inputType,
            ComboConfigSO nextCombo,
            CharacterVariable variable,
            ref bool needsComboInit)
        {
            if (nextCombo == null) return false;
            if (inputs.OutputIns(inputType))
            {
                variable.CurrentCombo = nextCombo;
                needsComboInit = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取攻击方向 — 通用版本（合并原 GetPlayerAttackDirection / GetEnemyAttackDirection）
        /// 优先使用 primaryDirection（玩家输入方向/敌人目标方向），其次 currentDirection，最后 forward
        /// </summary>
        public static Vector3 GetAttackDirection(Vector3 primaryDirection, Vector3 currentDirection, Vector3 forward)
        {
            return primaryDirection != Vector3.zero ? primaryDirection : currentDirection != Vector3.zero ? currentDirection : forward;
        }

        /// <summary>
        /// 初始化连击通用逻辑 — 消除 GroundAttackBehaviour 与 Enemy_AttackBehaviour 的重复
        /// 
        /// 统一处理：
        /// 1. 从 ComboConfigSO 获取动画哈希（优先 ComboDatabase 缓存）
        /// 2. FadeAnimation 淡入动画
        /// 3. 初始化 AttackPhaseDriver 阶段时序
        /// 
        /// 调用方仍需自行处理：
        /// - 攻击方向（玩家=输入方向，敌人=目标方向）
        /// - hasEnableCollider 重置
        /// </summary>
        /// <param name="comboDatabase">连击数据库（可为 null，回退到 Animator.StringToHash）</param>
        /// <param name="fadeAnimation">FadeAnimation 委托：void(int hash, float time)</param>
        /// <param name="combo">当前连击配置</param>
        /// <param name="phaseDriver">攻击阶段驱动器（ref 值类型）</param>
        public static void InitializeCombo(
            ComboDatabaseSO comboDatabase,
            System.Action<int, float> fadeAnimation,
            ComboConfigSO combo,
            ref AttackPhaseDriver phaseDriver)
        {
            int animHash = combo.CachedAnimHash != 0
                ? combo.CachedAnimHash
                : comboDatabase != null
                    ? comboDatabase.GetAnimHash(combo.AnimationClipName)
                    : Animator.StringToHash(combo.AnimationClipName);
            fadeAnimation(animHash, 0.1f);

            // 委托给 AttackPhaseDriver 管理阶段时序（统一从 Charging 开始，ChargeTime=0 时 Advance 自动跳过）
            phaseDriver.Initialize(combo.ToAttackData());
        }

        /// <summary>
        /// 推进攻击阶段时序并同步阶段字段 — 消除 GroundAttackBehaviour 与 Enemy_AttackBehaviour 的重复
        /// 
        /// 仅保留此完整版本（含 out 参数），移除无 out 参数的简化版重载，
        /// 避免调用方误用简化版忘记同步 CurrentAttackState / IsAttacking 导致 ConditionEvaluator 数据不一致。
        /// </summary>
        /// <param name="phaseDriver">攻击阶段驱动器（ref 值类型）</param>
        /// <param name="combo">当前连击配置</param>
        /// <param name="currentState">[out] 同步到调用方的 CurrentAttackState</param>
        /// <param name="isAttacking">[out] 同步到调用方的 IsAttacking</param>
        public static void AdvancePhase(
            ref AttackPhaseDriver phaseDriver,
            ComboConfigSO combo,
            out AttackState currentState,
            out bool isAttacking)
        {
            phaseDriver.Advance(combo.ToAttackData());
            currentState = phaseDriver.CurrentState;
            isAttacking = phaseDriver.IsAttacking;
        }
    }
}

