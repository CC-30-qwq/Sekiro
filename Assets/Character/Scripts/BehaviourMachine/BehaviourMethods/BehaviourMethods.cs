using System.Collections.Generic;
using Sekiro.Character.Data;
using UnityEngine;
using UnityEngine.AI;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 行为方法工具类，提供角色行为相关的核心方法
    /// 内部委托到 GroundDetectionUtility / AnimationUtility / RootMotionUtility / CombatUtility 四个独立工具类
    /// </summary>
    /// <typeparam name="TSettings">角色设置类型</typeparam>
    /// <typeparam name="TVariable">角色变量类型</typeparam>
    public class BehaviourMethods<TSettings, TVariable>
        where TSettings : CharacterSetting
        where TVariable : CharacterVariable
    {
        #region Properties

        public Transform Transform { get; internal set; }
        public TVariable Variable { get; internal set; }
        public TSettings Setting { get; internal set; }
        public InputsUtility Inputs { get; internal set; }
        public NavMeshAgent Agent { get; internal set; }
        public Animator Animator { get; internal set; }
        public Rigidbody Rigidbody { get; internal set; }

        /// <summary>
        /// 动画配置资产 — 优先使用 SO 配置，fallback 到静态字典
        /// </summary>
        public AnimationConfigSO AnimConfig { get; internal set; }

        /// <summary>
        /// 角色配置资产 — 新增，替代旧的 CharacterStatus 配置字段
        /// 在 BehaviourMachine.Start() 中注入
        /// </summary>
        public CharacterConfigSO CharacterConfig { get; internal set; }

        #endregion

        #region Animator Hash Cache (GC Optimization)

        /// <summary>
        /// 缓存的动画哈希字典 — 避免每帧调用 ToLegacyHashDict() 产生GC分配
        /// </summary>
        private Dictionary<int, float> _cachedAnimHash;

        /// <summary>
        /// 构建并缓存动画哈希字典（在所有属性注入完成后调用一次）
        /// </summary>
        public void BuildAnimHashCache()
        {
            _cachedAnimHash = AnimConfig?.ToLegacyHashDict();
        }

        /// <summary>
        /// 获取当前角色类型的动画哈希字典（缓存版本，零GC）
        /// 从 AnimationConfigSO 获取，不再回退到已弃用的静态字典
        /// </summary>
        public Dictionary<int, float> AnimHash
        {
            get
            {
                if (_cachedAnimHash == null)
                {
                    _cachedAnimHash = AnimConfig?.ToLegacyHashDict();
                }
                return _cachedAnimHash;
            }
        }

        #endregion

        #region Ground Check Methods (Delegated to GroundDetectionUtility)

        /// <inheritdoc cref="GroundDetectionUtility.GroundCheck(Transform, CharacterSetting, CharacterVariable)"/>
        public void GroundCheck()
        {
            GroundDetectionUtility.GroundCheck(Transform, Setting, Variable);
        }

        /// <inheritdoc cref="GroundDetectionUtility.Float(Transform, Rigidbody, CharacterSetting)"/>
        public void Float()
        {
            GroundDetectionUtility.Float(Transform, Rigidbody, Setting);
        }

        #endregion

        #region Animation Methods (Delegated to AnimationUtility)

        /// <inheritdoc cref="AnimationUtility.FadeAnimation(Animator, string, Dictionary{int, float})"/>
        public void FadeAnimation(string animName)
        {
            AnimationUtility.FadeAnimation(Animator, animName, AnimHash);
        }

        /// <inheritdoc cref="AnimationUtility.FadeAnimation(Animator, int, float)"/>
        public void FadeAnimation(int stateHash, float time)
        {
            AnimationUtility.FadeAnimation(Animator, stateHash, time);
        }

        /// <summary>
        /// 更新移动参数 - 从 CharacterConfigSO 读取缓动速度
        /// </summary>
        public void UpdateLocomotionParameters()
        {
            float lerpSpeed = CharacterConfig != null ? CharacterConfig.AnimParameterLerp : 5f;
            AnimationUtility.UpdateLocomotionParameters(Variable, lerpSpeed);
        }

        /// <inheritdoc cref="AnimationUtility.CalculateAnimInputValue(Vector3, Transform)"/>
        protected Vector2 CalculateAnimInputValue(Vector3 worldDirection, Transform referenceTransform)
        {
            return AnimationUtility.CalculateAnimInputValue(worldDirection, referenceTransform);
        }

        #endregion

        #region Combat Methods (Delegated to CombatUtility)

        /// <inheritdoc cref="CombatUtility.GetHitDirection(Transform)"/>
        public Vector2 GetHitDirection(Vector3 worldDirection, Transform referenceTransform)
        {
            return CombatUtility.GetHitDirection(worldDirection, referenceTransform);
        }

        #endregion

        #region Movement Methods (Delegated to RootMotionUtility)

        /// <inheritdoc cref="RootMotionUtility.UpdateModifyRootPosition(Rigidbody, Vector3)"/>
        public void UpdateModifyRootPosition()
        {
            RootMotionUtility.UpdateModifyRootPosition(Rigidbody, Variable.RootMovePosition);
        }

        /// <inheritdoc cref="RootMotionUtility.UpdateOriginRootPosition(Rigidbody, Vector3)"/>
        public void UpdateOriginRootPosition()
        {
            RootMotionUtility.UpdateOriginRootPosition(Rigidbody, Variable.RootMovePosition);
        }

        /// <inheritdoc cref="RootMotionUtility.UpdateDirectRootPosition(Rigidbody, Vector3, Vector3)"/>
        public void UpdateDirectRootPosition(Vector3 direction)
        {
            RootMotionUtility.UpdateDirectRootPosition(Rigidbody, Variable.RootMovePosition, direction);
        }

        /// <inheritdoc cref="RootMotionUtility.UpdateRotation(Transform, CharacterVariable, Vector3, float)"/>
        public void UpdateRotation(Vector3 faceDir, float speed)
        {
            RootMotionUtility.UpdateRotation(Transform, Variable, faceDir, speed);
        }

        #endregion
    }
} // namespace Sekiro.BehaviourMachine
