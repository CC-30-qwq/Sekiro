using UnityEngine;

namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 玩家行为状态机，管理玩家角色的行为状态转换和逻辑
    /// </summary>
    /// <remarks>
    /// 此类继承自<see cref="BehaviourMachine{T, TMethods, TVariable, TSettings}"/>，
    /// 专门处理玩家角色的行为逻辑，包括移动、跳跃、攻击、防御等。
    /// 使用性能优化策略（间隔更新）减少不必要的计算开销。
    /// </remarks>
    [RequireComponent(typeof(InputsUtility))]
    public class PlayerBehaviourMachine : BehaviourMachine<PlayerBehaviour, PlayerMethods, PlayerVariable, PlayerSetting>
    {
        /// <summary>
        /// 玩家输入工具类实例
        /// </summary>
        /// <remarks>
        /// 用于处理玩家的输入逻辑，包括方向输入、按钮输入等。
        /// 在<see cref="Awake"/>方法中自动获取组件。
        /// </remarks>
        [field: SerializeField] public InputsUtility Inputs { get; private set; }

        #region Performance Optimization

        /// <summary>
        /// 上次执行地面检测的时间戳
        /// </summary>
        private float _lastGroundCheckTime;

        /// <summary>
        /// 上次执行输入更新的时间戳
        /// </summary>
        private float _lastInputUpdateTime;

        #endregion

        /// <summary>
        /// 获取当前攻击行为实例（类型安全，消除反射）
        /// </summary>
        public AttackGround CurrentAttackBehaviour => CurrentBehaviourInstance as AttackGround;

        protected override PlayerMethods CreateMethods()
        {
            return new PlayerMethods();
        }

        protected override void InitializeStates()
        {
            behaviours.Add(PlayerBehaviour.Idle, new Idle(this));
            behaviours.Add(PlayerBehaviour.StartRun, new StartRun(this));
            behaviours.Add(PlayerBehaviour.StartSprint, new StartSprint(this));
            behaviours.Add(PlayerBehaviour.Run, new Run(this));
            behaviours.Add(PlayerBehaviour.Sprint, new Sprint(this));
            behaviours.Add(PlayerBehaviour.StopStart, new StopStart(this));
            behaviours.Add(PlayerBehaviour.StopRun, new StopMedium(this));
            behaviours.Add(PlayerBehaviour.StopSprint, new StopHard(this));
            behaviours.Add(PlayerBehaviour.TurnStart, new TurnStart(this));
            behaviours.Add(PlayerBehaviour.TurnMedium, new TurnMedium(this));
            behaviours.Add(PlayerBehaviour.TurnHard, new TurnHard(this));
            behaviours.Add(PlayerBehaviour.StartJumpInPlace, new StartJumpInPlace(this));
            behaviours.Add(PlayerBehaviour.StartJumpDirect, new StartJumpDirect(this));
            behaviours.Add(PlayerBehaviour.JumpInPlace, new JumpInPlace(this));
            behaviours.Add(PlayerBehaviour.JumpDirect, new JumpDirect(this));
            behaviours.Add(PlayerBehaviour.Fall, new Fall(this));
            behaviours.Add(PlayerBehaviour.StartLand, new StartLand(this));
            behaviours.Add(PlayerBehaviour.LandInPlace, new LandInPlace(this));
            behaviours.Add(PlayerBehaviour.LandDirect, new LandDirect(this));
            behaviours.Add(PlayerBehaviour.Dodge, new Dodge(this));
            behaviours.Add(PlayerBehaviour.AttackGround, new AttackGround(this));
            behaviours.Add(PlayerBehaviour.ParryStart, new ParryStart(this));
            behaviours.Add(PlayerBehaviour.ParrySprint, new ParrySprint(this));
            behaviours.Add(PlayerBehaviour.ParryReStart0, new ParryReStart0(this));
            behaviours.Add(PlayerBehaviour.ParryReStart1, new ParryReStart1(this));
            behaviours.Add(PlayerBehaviour.Defence, new DefenceBehaviour(this, PlayerBehaviour.Defence));
            behaviours.Add(PlayerBehaviour.DefenceMove, new DefenceBehaviour(this, PlayerBehaviour.DefenceMove));
            behaviours.Add(PlayerBehaviour.DefenceMoveStop, new DefenceBehaviour(this, PlayerBehaviour.DefenceMoveStop));
            behaviours.Add(PlayerBehaviour.CancelDefence, new CancelDefence(this));
            behaviours.Add(PlayerBehaviour.Hit, new Hit(this));
            behaviours.Add(PlayerBehaviour.Block, new Block(this));
            behaviours.Add(PlayerBehaviour.Deflect, new Deflect(this));

            ActivateInitialState(PlayerBehaviour.Idle);

        }

        protected override void InitializeSetting()
        {
            base.InitializeSetting();
            Variable.BufferedDirection = transform.forward;
        }

        protected override void Awake()
        {

            base.Awake();
            Inputs = GetComponent<InputsUtility>();
        }

        protected override void Start()
        {
            base.Start();
            BehaviourMethods.Inputs = Inputs;
        }

        protected override void Update()

        {
            base.Update();

            float currentTime = Time.time;
            if (currentTime - _lastInputUpdateTime >= Constants.InputUpdateInterval)
            {
                BehaviourMethods.UpdateInputDirection();
                _lastInputUpdateTime = currentTime;
            }
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            float currentTime = Time.time;
            if (currentTime - _lastGroundCheckTime >= Constants.GroundCheckInterval)
            {
                BehaviourMethods.GroundCheck();
                _lastGroundCheckTime = currentTime;
            }
        }

        // OnAnimatorMove 继承基类默认实现（根运动位移提取）

        /// <summary>
        /// Gizmos 调试绘制已迁移到独立的 PlayerGizmosDrawer 组件
        /// 请在挂载 PlayerBehaviourMachine 的 GameObject 上添加 PlayerGizmosDrawer 组件
        /// </summary>
    }
}
