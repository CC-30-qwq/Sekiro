# Sekiro

基于 Unity 2022.3（URP）开发的第三人称动作战斗原型，以「弹反（Parry）／连招（Combo）」为核心的类《只狼》式战斗手感。

玩家与敌人各自拥有一套**行为状态机 + 行为树**驱动的角色控制器，所有数值与连招配置均由 ScriptableObject 驱动，可在编辑器中直接调参。

## 技术栈

| 分类 | 技术 |
|---|---|
| 引擎 | Unity 2022.3.62f1c1（URP 14.0.12） |
| 输入 | Unity Input System 1.14.2 |
| 动画 | Animator + Timeline + Animation Rigging 1.2.1 |
| 相机 | Cinemachine 2.10.5 |
| UI / 文本 | UGUI + TextMeshPro |
| 编辑器扩展 | 自研状态机可视化编辑器、Inspector 定制 |

## 核心特性

### 角色行为状态机
- `BehaviourMachine` 提供状态基类（`BaseBehaviour`）、状态枚举、条件求值与状态转移配置（`StateTransitionConfigSO`）。
- 玩家侧按「移动 / 战斗 / 受击」三大分支组织行为：
  - **移动（Locomotion）**：`Idle`、`Run`、`Start_Run`、`Stop_*`、`Turn_*`、`Sprint`、`Jump_*`、`Fall`、`Land_*`。
  - **战斗（Combat）**：轻重攻击与蓄力／冲刺连招、防御（`DefenceBehaviour`）、弹反（`Parry_Start` / `Parry_Sprint` / `Parry_ReStart_*`）、闪避（`Dodge`）。
  - **受击反馈（Counter/Impact）**：`Block`、`Deflect`、`Hit` 三种命中结果分支。

### 连招系统
- `ComboConfigSO` / `ComboDatabaseSO` 描述连招段位、输入窗口与动画片段。
- `IComboAttackStrategy` 策略族：`MeleeAttackStrategy`、`RangedAttackStrategy`、`ChargeAttackStrategy`、`SprintAttackStrategy`，由 `ComboAttackStrategyFactory` 按配置创建。
- `RootMotionExtractor` + `RootMotionUtility`：从动画中提取根位移并驱动角色实际位移，保证打击位移与动画同步。

### 战斗判定
- `AttackPhaseDriver` / `ParryPhaseDriver`：按动画阶段（前摇／判定／后摇）驱动攻击与弹反窗口。
- `HitColliderWindow` + `AttackData`：以碰撞体窗口描述每段攻击的判定范围与伤害。
- `Health`、`IDetectable`、`Projectile` / `RangedWeapon`：血量、可被侦测标记与远程投射物。

### 敌人系统
- **行为树（BehaviourTree）**：`SelectorNode`、`SequenceNode`、`ParallelNode`、`InverterNode`、`RepeaterNode`、`UntilSuccessNode` / `UntilFailureNode`、`CooldownNode`、`WeightedRandomSelectorNode`，配套 `NodeBuilder` 构建。
- **行为状态机**：`EnemyBehaviourMachine` 与玩家共用同一套框架，敌人行为包括 `Enemy_IdleBehaviour`、`Enemy_ChaseBehaviour`、`Enemy_StanderBehaviour`、`Enemy_AttackBehaviour`、`Enemy_ParryBehaviour`。
- **攻击策略**：`IEnemyAttackStrategy` → `EnemyMeleeAttackStrategy` / `EnemyRangedAttackStrategy`，由 `EnemyAttackStrategyFactory` 创建。
- `EnemyConfigSO`、`EnemyAttackMappingSO` 配置敌人属性与攻击映射。

### 摄像机与锁定
- `PlayerCamera` + Cinemachine 提供第三人称跟随。
- 锁定目标由 `TargetDetector` → `TargetVisibilityChecker` → `TargetScorer` 打分选出最佳目标（`DetectableTarget`），`PlayerVision` 描述视野范围。

### 输入缓冲
- `InputBuffer` 缓存提前输入的指令（如攻击／弹反），在窗口内自动消费，避免因动画未结束而丢输入。

## 项目结构

```
Assets/
├── Character/Scripts/
│   ├── BehaviourMachine/       # 行为状态机框架（基类、条件、转移配置、可视化配置）
│   │   ├── BaseComponent/      # 角色配置、武器、根位移、地面检测等基础组件
│   │   ├── BehaviourMethods/   # 动画/战斗/地面/根位移工具方法
│   │   └── StateMachineVisualConfig/  # 状态机可视化配置数据
│   ├── CombatSystem/           # 攻击/弹反阶段驱动、血量、武器、投射物
│   ├── Enemy/                  # 敌人行为树 + 敌人状态机 + 敌人攻击策略
│   └── Player/
│       ├── Behaviours/         # 玩家全部行为（移动/战斗/受击）
│       ├── PlayerCamera/       # 锁定目标侦测与打分
│       ├── PlayerInputs/       # 输入缓冲与输入工具
│       └── Runtime/            # 调试可视化（Gizmos、攻击进度）
└── Editor/                     # 自研编辑器扩展
    └── StateMachineVisualEditor/  # 状态机节点可视化编辑
```

## 快速开始

1. 使用 **Unity 2022.3.62f1c1**（含中文语言包 `c1` 后缀版本）打开项目。
2. 首次打开等待 Package Manager 还原依赖。
3. 打开 `Assets/Scenes/SampleScene.unity` 运行。
4. 在 `Assets/Character/Scripts/.../Config/` 下的 `ScriptableObject` 资产中调整角色数值与连招配置。

> 建议配合 Unity 的 `PlayerGizmosDrawer`、`EnemyGizmosDrawer`、`RuntimeAttackProgressVisualizer` 在 Scene 视图中调试攻击判定与状态切换。

## 备注

本项目为战斗系统的技术原型，重点在于**可配置、可扩展的角色行为框架**：玩家与敌人共用行为状态机，差异通过配置与策略类注入，新增一段连招或一种敌人只需新增配置与策略实现。
