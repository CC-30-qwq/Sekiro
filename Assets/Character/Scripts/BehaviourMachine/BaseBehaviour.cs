using System;
using System.Collections;
using System.Collections.Generic;
using Sekiro.Combat;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 角色行为基类，所有具体行为都继承此类
    /// </summary>
    /// <typeparam name="T">行为枚举类型</typeparam>
    /// <typeparam name="TMethods">行为方法类型</typeparam>
    /// <typeparam name="TVariable">角色变量类型</typeparam>
    /// <typeparam name="TSettings">角色设置类型</typeparam>
    public abstract class BaseBehaviour<T, TMethods, TVariable, TSettings>
        where T : struct, Enum
        where TMethods : BehaviourMethods<TSettings, TVariable>
        where TVariable : CharacterVariable
        where TSettings : CharacterSetting
    {
        #region Properties

        protected internal BehaviourMachine<T, TMethods, TVariable, TSettings> Machine => machine;
        protected internal TMethods Methods => machine.BehaviourMethods;
        protected internal TVariable Variable => machine.Variable;
        protected TSettings Setting => machine.Setting;
        protected Animator Animator => machine.Animator;
        protected Rigidbody Rigidbody => machine.Rigidbody;
        protected float Timer => _behaviourTimer;
        protected Coroutine StateCoroutine { get; private set; }

        #endregion

        #region Protected Fields

        protected readonly BehaviourMachine<T, TMethods, TVariable, TSettings> machine;

        #endregion

        #region Private Fields

        private float _behaviourTimer;
        private readonly List<Coroutine> _activeCoroutines = new List<Coroutine>();

        #endregion

        #region Constructor

        public BaseBehaviour(BehaviourMachine<T, TMethods, TVariable, TSettings> machine)
        {
            this.machine = machine;
        }

        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// 检查是否允许状态切换条件
        /// </summary>
        public virtual bool CanChangeCondition()
        {
            return true;
        }

        /// <summary>
        /// 是否需要在 FixedUpdate 中执行 Float（悬空/地面物理）。
        /// 由各行为子类按需开启，BehaviourMachine.FixedUpdate 统一调度。
        /// </summary>
        protected internal virtual bool ShouldApplyFloat => false;

        /// <summary>
        /// 受击标志位 — 统一从 Machine.Variable 读取，所有行为子类共享。
        /// </summary>
        protected internal bool IsHit => machine.Variable != null && machine.Variable.IsHit;

        /// <summary>
        /// 进入行为时调用，执行行为初始化逻辑
        /// </summary>
        public virtual void OnEnter()
        {
            _behaviourTimer = 0f;
            if (machine.Variable != null)
                machine.Variable.IsHit = false;
        }

        /// <summary>
        /// 每帧更新时调用，执行行为更新逻辑
        /// </summary>
        public virtual void OnUpdate()
        {
            _behaviourTimer += Time.deltaTime;
        }

        /// <summary>
        /// 固定更新时调用，执行物理相关更新逻辑
        /// </summary>
        public virtual void OnFixedUpdate()
        {
        }

        /// <summary>
        /// 退出行为时调用，执行行为清理逻辑
        /// </summary>
        public virtual void OnExit()
        {
            // 反向遍历，避免 ToArray() 分配
            for (int i = _activeCoroutines.Count - 1; i >= 0; i--)
            {
                var coroutine = _activeCoroutines[i];
                if (coroutine != null)
                {
                    machine.StopCoroutine(coroutine);
                }
                _activeCoroutines.RemoveAt(i);
            }
        }

        #endregion

        /// <summary>
        /// 评估状态转换规则，子类可重写以实现自定义的转换评估逻辑
        /// </summary>
        public virtual void EvaluateStateTransitionRules()
        {
        }

        /// <summary>
        /// 受击处理 — 子类可重写以追加特有的击退/特效逻辑。
        /// 基类默认行为：设置 IsHit 标志 + 记录受击方向。
        /// </summary>
        public virtual void HandleDamage(DamageInfo damageInfo)
        {
            if (machine.Variable != null)
            {
                machine.Variable.HitDirection = damageInfo.Direction;
                machine.Variable.IsHit = true;
            }
        }

        #region Protected Methods

        /// <summary>
        /// 重置计时器
        /// </summary>
        protected void ResetTimer()
        {
            _behaviourTimer = 0f;
        }

        /// <summary>
        /// 启动状态协程
        /// </summary>
        protected Coroutine StartStateCoroutine(IEnumerator routine)
        {
            StateCoroutine = machine.StartCoroutine(routine);
            _activeCoroutines.Add(StateCoroutine);
            return StateCoroutine;
        }

        /// <summary>
        /// 停止状态协程
        /// </summary>
        protected void StopStateCoroutine(Coroutine coroutine)
        {
            if (coroutine == null)
            {
                return;
            }

            machine.StopCoroutine(coroutine);
            _activeCoroutines.Remove(coroutine);
        }

        #endregion
    }
} // namespace Sekiro.BehaviourMachine
