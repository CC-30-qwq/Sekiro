using System.Linq;
using Sekiro.BehaviourMachine;
using UnityEngine;

namespace Sekiro.Player.Runtime
{
    /// <summary>
    /// 输入缓冲队列可视化调试组件 - 直接UI绘制版本
    /// </summary>
    public class InputBufferDebugVisualizer : MonoBehaviour
    {
        [Header("引用设置")]
        [SerializeField] private InputBuffer _inputBuffer;
        [SerializeField] private InputsUtility _inputsUtility;

        [Header("样式设置")]
        [SerializeField] private Color _attackColor = Color.red;
        [SerializeField] private Color _parryColor = Color.yellow;
        [SerializeField] private Color _jumpColor = Color.green;
        [SerializeField] private Color _dodgeColor = Color.cyan;
        [SerializeField] private Color _backgroundColor = new Color(0, 0, 0, 0.7f);
        [SerializeField] private Color _borderColor = Color.white;

        [Header("布局设置")]
        [SerializeField] private Vector2 _position = new Vector2(20, 20);
        [SerializeField] private Vector2 _slotSize = new Vector2(60, 60);
        [SerializeField] private float _slotSpacing = 8f;
        [SerializeField] private int _maxSlots = 8;

        [Header("显示设置")]
        [SerializeField] private bool _showTimeRemaining = true;
        [SerializeField] private bool _showPercentage = true;
        [SerializeField] private KeyCode _toggleKey = KeyCode.F3;

        [Header("字体设置")]
        [SerializeField] private int _fontSize = 14;

        private bool _isVisible = true;
        private Texture2D _backgroundTexture;
        private Texture2D _borderTexture;
        private GUIStyle _slotStyle;
        private GUIStyle _textStyle;
        private GUIStyle _labelStyle;

        private void Awake()
        {
            // 创建纹理
            _backgroundTexture = CreateTexture(_backgroundColor);
            _borderTexture = CreateTexture(_borderColor);

            // 自动查找组件
            if (_inputBuffer == null)
                _inputBuffer = GetComponent<InputBuffer>();
            if (_inputsUtility == null)
                _inputsUtility = GetComponent<InputsUtility>();
        }

        private void OnDestroy()
        {
            if (_backgroundTexture != null) Destroy(_backgroundTexture);
            if (_borderTexture != null) Destroy(_borderTexture);
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
            {
                _isVisible = !_isVisible;
            }
        }

        private void OnGUI()
        {
            if (!_isVisible || _inputBuffer == null) return;

            // 确保样式初始化
            if (_slotStyle == null) InitializeStyles();

            float currentTime = Time.time;
            var queueArray = _inputBuffer.BufferQueue.ToArray();
            int displayCount = Mathf.Min(queueArray.Length, _maxSlots);

            // 计算总宽度
            float totalWidth = (_slotSize.x + _slotSpacing) * displayCount - _slotSpacing;
            float startX = _position.x;
            float startY = _position.y;

            // 绘制背景
            GUI.DrawTexture(new Rect(startX - 10, startY - 10, totalWidth + 20, _slotSize.y + 40), _backgroundTexture);

            // 绘制标题
            GUI.Label(new Rect(startX, startY - 25, 200, 20), "Input Buffer", _labelStyle);

            // 绘制槽位
            for (int i = 0; i < displayCount; i++)
            {
                var bufferedInput = queueArray[i];
                float timeElapsed = currentTime - bufferedInput.Time;
                float timeRemaining = _inputBuffer.BufferTime - timeElapsed;
                float percentage = Mathf.Clamp01(timeRemaining / _inputBuffer.BufferTime);

                float x = startX + i * (_slotSize.x + _slotSpacing);
                float y = startY;

                // 获取颜色
                Color baseColor = GetColorForInputType(bufferedInput.Type);
                Color finalColor = Color.Lerp(Color.gray, baseColor, percentage);

                // 绘制槽位背景
                GUI.color = finalColor;
                GUI.DrawTexture(new Rect(x, y, _slotSize.x, _slotSize.y), _slotStyle.normal.background);

                // 绘制边框
                GUI.color = _borderColor;
                GUI.DrawTexture(new Rect(x, y, _slotSize.x, 2), _borderTexture); // 顶部
                GUI.DrawTexture(new Rect(x, y + _slotSize.y - 2, _slotSize.x, 2), _borderTexture); // 底部
                GUI.DrawTexture(new Rect(x, y, 2, _slotSize.y), _borderTexture); // 左边
                GUI.DrawTexture(new Rect(x + _slotSize.x - 2, y, 2, _slotSize.y), _borderTexture); // 右边

                GUI.color = Color.white;

                // 绘制类型缩写
                string typeText = GetAbbreviation(bufferedInput.Type);
                float typeWidth = _slotSize.x - 10;
                float typeHeight = _slotSize.y * 0.4f;

                // 主文本（类型缩写）
                GUI.Label(new Rect(x + 5, y + 5, typeWidth, typeHeight), typeText, _textStyle);

                // 剩余时间
                string timeText = _showTimeRemaining ? $"{timeRemaining:F2}s" : "";
                GUI.Label(new Rect(x + 5, y + _slotSize.y * 0.4f, typeWidth, typeHeight * 0.8f), timeText, _textStyle);

                // 百分比
                if (_showPercentage)
                {
                    string percentText = $"{percentage * 100:F0}%";
                    GUI.Label(new Rect(x + 5, y + _slotSize.y * 0.65f, typeWidth, typeHeight * 0.7f), percentText, _textStyle);
                }

                // 绘制进度条
                float barWidth = _slotSize.x - 10;
                float barHeight = 4;
                float barY = y + _slotSize.y - 8;

                GUI.color = Color.black;
                GUI.DrawTexture(new Rect(x + 5, barY, barWidth, barHeight), _backgroundTexture);
                GUI.color = finalColor;
                GUI.DrawTexture(new Rect(x + 5, barY, barWidth * percentage, barHeight), _borderTexture);
                GUI.color = Color.white;
            }

            // 绘制状态信息
            float infoY = startY + _slotSize.y + 15;
            GUI.color = Color.white;
            GUI.Label(new Rect(startX, infoY, 200, 20), $"Count: {_inputBuffer.Count}/{_maxSlots}", _labelStyle);
            GUI.Label(new Rect(startX + 80, infoY, 200, 20), $"BufferTime: {_inputBuffer.BufferTime:F2}s", _labelStyle);
        }

        private void InitializeStyles()
        {
            _slotStyle = new GUIStyle();
            _slotStyle.normal.background = _backgroundTexture;

            _textStyle = new GUIStyle(GUI.skin.label);
            _textStyle.alignment = TextAnchor.UpperCenter;
            _textStyle.fontSize = _fontSize;
            _textStyle.fontStyle = FontStyle.Bold;
            _textStyle.normal.textColor = Color.white;

            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.fontSize = _fontSize - 2;
            _labelStyle.normal.textColor = Color.white;
        }

        private Texture2D CreateTexture(Color color)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        private Color GetColorForInputType(InputType type)
        {
            switch (type)
            {
                case InputType.Attack: return _attackColor;
                case InputType.Parry: return _parryColor;
                case InputType.Jump: return _jumpColor;
                case InputType.Dodge: return _dodgeColor;
                default: return Color.white;
            }
        }

        private string GetAbbreviation(InputType type)
        {
            switch (type)
            {
                case InputType.Attack: return "ATK";
                case InputType.Parry: return "PRY";
                case InputType.Jump: return "JMP";
                case InputType.Dodge: return "DGE";
                default: return "???";
            }
        }

        /// <summary>
        /// 手动显示/隐藏调试信息
        /// </summary>
        public void SetDebugVisible(bool visible)
        {
            _isVisible = visible;
        }

        /// <summary>
        /// 获取当前缓冲区内容
        /// </summary>
        public string GetBufferInfo()
        {
            if (_inputBuffer == null || _inputBuffer.IsEmpty)
                return "Buffer: Empty";

            var info = new System.Text.StringBuilder();
            info.AppendLine($"Buffer Count: {_inputBuffer.Count}");

            float currentTime = Time.time;
            var queueArray = _inputBuffer.BufferQueue.ToArray();
            for (int i = 0; i < queueArray.Length; i++)
            {
                float timeRemaining = _inputBuffer.BufferTime - (currentTime - queueArray[i].Time);
                info.AppendLine($"  [{i}] {queueArray[i].Type}: {timeRemaining:F3}s");
            }

            return info.ToString();
        }

        /// <summary>
        /// 设置显示位置
        /// </summary>
        public void SetPosition(Vector2 pos)
        {
            _position = pos;
        }
    }
}
