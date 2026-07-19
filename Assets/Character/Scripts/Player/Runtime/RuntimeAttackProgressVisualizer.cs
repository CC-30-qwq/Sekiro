using Sekiro.BehaviourMachine;
using UnityEngine;

namespace Sekiro.Player.Runtime
{
    /// <summary>
    /// Runtime attack progress visualizer - displays attack progress in game view using OnGUI
    /// </summary>
    public class RuntimeAttackProgressVisualizer : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Settings")]
        [SerializeField] private PlayerBehaviourMachine _targetPlayer;
        [SerializeField] private float _progressBarWidth = 300f;
        [SerializeField] private float _progressBarHeight = 30f;

        #endregion

        #region Private Fields

        private AttackGround _attackBehaviour;

        // Cached GUIStyle objects
        private GUIStyle _cachedLabelStyle;
        private GUIStyle _cachedPhaseLabelStyle;
        private bool _stylesInitialized;

        /// <summary>
        /// 单一 1x1 白色纹理 — 替代可变的纹理缓存字典
        /// 所有颜色绘制通过 GUI.color 实现，消除纹理无限泄漏风险
        /// </summary>
        private Texture2D _whiteTexture;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (_targetPlayer == null)
            {
                _targetPlayer = FindObjectOfType<PlayerBehaviourMachine>();
            }
        }

        private void Update()
        {
            if (_targetPlayer != null)
            {
                var currentBehaviour = GetCurrentAttackBehaviour();
                if (currentBehaviour != null)
                {
                    _attackBehaviour = currentBehaviour;
                }
            }
        }

        private void OnGUI()
        {
            if (_attackBehaviour == null)
                return;

            InitializeStyles();

            // 延迟创建白色纹理
            if (_whiteTexture == null)
                _whiteTexture = Create1x1WhiteTexture();

            // Draw background panel
            float panelWidth = _progressBarWidth + 40;
            float panelHeight = 120;
            Rect panelRect = new Rect(20, Screen.height - panelHeight - 20, panelWidth, panelHeight);
            GUI.color = new Color(0, 0, 0, 0.5f);
            GUI.DrawTexture(panelRect, _whiteTexture);
            GUI.color = Color.white;

            // Draw title
            GUI.Label(new Rect(30, Screen.height - panelHeight - 10, 200, 20), "Attack Progress", _cachedLabelStyle);

            // Get progress data
            AttackState currentState = _attackBehaviour.VisualCurrentState;
            float stateProgress = _attackBehaviour.VisualStateProgress;
            float totalProgress = _attackBehaviour.VisualTotalProgress;

            // Draw state info
            GUI.Label(new Rect(30, Screen.height - panelHeight + 10, 200, 20),
                $"State: {currentState} ({stateProgress:P0})", _cachedLabelStyle);
            GUI.Label(new Rect(30, Screen.height - panelHeight + 30, 200, 20),
                $"Total: {totalProgress:P0}", _cachedLabelStyle);

            // Draw three-segment progress bar
            Rect barRect = new Rect(30, Screen.height - panelHeight + 70, _progressBarWidth, _progressBarHeight);
            DrawAttackProgressBar(barRect, currentState, stateProgress, totalProgress);
        }

        private void OnDestroy()
        {
            if (_whiteTexture != null)
            {
                Destroy(_whiteTexture);
                _whiteTexture = null;
            }
        }

        #endregion

        #region Private Methods

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _cachedLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _cachedPhaseLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = Color.white }
            };

            _stylesInitialized = true;
        }

        private AttackGround GetCurrentAttackBehaviour()
        {
            if (_targetPlayer.CurrentBehaviour == PlayerBehaviour.AttackGround)
            {
                return _targetPlayer.CurrentAttackBehaviour;
            }
            return null;
        }

        /// <summary>
        /// 创建 1x1 白色纹理 — 整个组件仅此一个纹理
        /// 所有颜色通过 GUI.color 参数控制，不再为每种颜色创建新纹理
        /// </summary>
        private static Texture2D Create1x1WhiteTexture()
        {
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return tex;
        }

        private void DrawAttackProgressBar(Rect rect, AttackState currentState, float stateProgress, float totalProgress)
        {
            // Background — 灰色背景
            GUI.color = new Color(0.2f, 0.2f, 0.2f);
            GUI.DrawTexture(rect, _whiteTexture);

            float thirdWidth = rect.width / 3f;

            // Windup segment (red)
            Rect windupRect = new Rect(rect.x, rect.y, thirdWidth, rect.height);
            Color windupColor = currentState == AttackState.Windup ?
                Color.Lerp(new Color(0.8f, 0.2f, 0.2f), new Color(1f, 0.5f, 0.5f), stateProgress) :
                new Color(0.4f, 0.1f, 0.1f);
            GUI.color = windupColor;
            GUI.DrawTexture(windupRect, _whiteTexture);

            // Attacking segment (green)
            Rect attackRect = new Rect(rect.x + thirdWidth, rect.y, thirdWidth, rect.height);
            Color attackColor = currentState == AttackState.Attacking ?
                Color.Lerp(new Color(0.2f, 0.8f, 0.2f), new Color(0.5f, 1f, 0.5f), stateProgress) :
                new Color(0.1f, 0.4f, 0.1f);
            GUI.color = attackColor;
            GUI.DrawTexture(attackRect, _whiteTexture);

            // Recovery segment (blue)
            Rect recoveryRect = new Rect(rect.x + thirdWidth * 2, rect.y, thirdWidth, rect.height);
            Color recoveryColor = currentState == AttackState.Recovery ?
                Color.Lerp(new Color(0.2f, 0.4f, 0.8f), new Color(0.5f, 0.7f, 1f), stateProgress) :
                new Color(0.1f, 0.2f, 0.4f);
            GUI.color = recoveryColor;
            GUI.DrawTexture(recoveryRect, _whiteTexture);

            // Draw progress cursor (white line)
            float cursorX = rect.x + rect.width * totalProgress;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(cursorX - 2, rect.y, 4, rect.height), _whiteTexture);

            // Reset color
            GUI.color = Color.white;

            // Draw segment labels (using cached style)
            GUI.Label(new Rect(rect.x, rect.y + rect.height + 2, thirdWidth, 20), "Windup", _cachedPhaseLabelStyle);
            GUI.Label(new Rect(rect.x + thirdWidth, rect.y + rect.height + 2, thirdWidth, 20), "Attack", _cachedPhaseLabelStyle);
            GUI.Label(new Rect(rect.x + thirdWidth * 2, rect.y + rect.height + 2, thirdWidth, 20), "Recovery", _cachedPhaseLabelStyle);
        }

        #endregion
    }
}

