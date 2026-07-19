using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 输入缓冲数据结构
    /// </summary>
    public readonly struct BufferedInput
    {
        public InputType Type { get; }
        public float Time { get; }

        public BufferedInput(InputType type)
        {
            Type = type;
            Time = UnityEngine.Time.time;
        }
    }

    /// <summary>
    /// 输入缓冲区组件 - 管理输入缓冲队列
    /// 通过 IInputStateSink 接口操作输入标志，与 InputsUtility 解耦
    /// </summary>
    public class InputBuffer : MonoBehaviour
    {
        #region Serialized Fields

        [Header("缓冲设置")]
        [Tooltip("缓冲区最大容量")]
        [SerializeField] private int _bufferSize = 8;

        [Tooltip("缓冲有效时间（秒）")]
        public float BufferTime = 0.2f;

        #endregion

        #region Static Cache (GC Optimization)

        /// <summary>
        /// 预缓存的InputType数组 - 避免Enum.GetValues每次分配
        /// </summary>
        private static readonly InputType[] _cachedInputTypes = (InputType[])Enum.GetValues(typeof(InputType));

        #endregion

        #region Private Fields

        private Queue<BufferedInput> _bufferQueue;
        private IInputStateSink _inputStateSink;
        private IReadOnlyCollection<BufferedInput> _bufferQueueReadOnly;

        #endregion

        #region Properties

        public int Count => _bufferQueue.Count;
        public bool IsEmpty => _bufferQueue.Count == 0;

        /// <summary>
        /// 缓冲区只读视图 - 供外部代码遍历检查队列内容
        /// </summary>
        public IReadOnlyCollection<BufferedInput> BufferQueue => _bufferQueueReadOnly ??= _bufferQueue;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // 预分配队列容量避免扩容
            _bufferQueue = new Queue<BufferedInput>(_bufferSize);
            // 通过接口解耦依赖，不再直接引用 InputsUtility
            _inputStateSink = GetComponent<IInputStateSink>();
        }

        private void LateUpdate()
        {
            CleanExpiredInputs();
            UpdateInputFlags();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 缓冲输入
        /// </summary>
        public void BufferInput(InputType inputType)
        {
            // 超过缓冲长度时移除最旧的
            if (_bufferQueue.Count >= _bufferSize)
            {
                _bufferQueue.Dequeue();
            }

            _bufferQueue.Enqueue(new BufferedInput(inputType));
        }

        /// <summary>
        /// 消费缓冲 - 移除队列顶端的输入并更新输入标志
        /// 返回值已移除（所有调用方均忽略返回值），改为 void 避免未使用返回值误导
        /// </summary>
        public void ConsumeBuffer()
        {
            if (_bufferQueue.Count == 0)
            {
                ClearAllInputFlags();
                return;
            }

            var input = _bufferQueue.Dequeue();
            SetInputFlag(input.Type, true);
            ClearOtherInputFlags(input.Type);
        }

        /// <summary>
        /// 清空缓冲区
        /// </summary>
        public void ClearBuffer()
        {
            _bufferQueue.Clear();
            ClearAllInputFlags();
        }

        /// <summary>
        /// 安全获取队列顶端输入类型（无需暴露内部队列引用）
        /// </summary>
        public bool TryPeekType(out InputType type)
        {
            if (_bufferQueue.Count > 0)
            {
                type = _bufferQueue.Peek().Type;
                return true;
            }

            type = default;
            return false;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 清理超过缓冲时间的输入
        /// </summary>
        private void CleanExpiredInputs()
        {
            float currentTime = Time.time;

            while (_bufferQueue.Count > 0)
            {
                var oldest = _bufferQueue.Peek();
                if (currentTime - oldest.Time > BufferTime)
                {
                    _bufferQueue.Dequeue();
                    continue;
                }

                break;
            }
        }

        /// <summary>
        /// 更新输入标志 - 始终将队列顶端设为true，其他为false
        /// </summary>
        private void UpdateInputFlags()
        {
            if (_bufferQueue.Count == 0)
            {
                ClearAllInputFlags();
                return;
            }

            // 队列顶端（最新输入）设为true
            var latest = _bufferQueue.Peek();
            SetInputFlag(latest.Type, true);
            ClearOtherInputFlags(latest.Type);
        }

        private void SetInputFlag(InputType type, bool value)
        {
            _inputStateSink?.SetInputFlag(type, value);
        }

        /// <summary>
        /// 清除除指定类型外的所有输入标志 - 使用缓存的枚举数组
        /// </summary>
        private void ClearOtherInputFlags(InputType except)
        {
            if (_inputStateSink == null) return;

            // 使用预缓存的枚举数组避免GC
            for (int i = 0; i < _cachedInputTypes.Length; i++)
            {
                InputType type = _cachedInputTypes[i];
                if (type == except) continue;

                _inputStateSink.SetInputFlag(type, false);
            }
        }

        private void ClearAllInputFlags()
        {
            _inputStateSink?.ClearAllInputFlags();
        }

        #endregion
    }
}
