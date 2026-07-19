using Sekiro.Character.Data;
using Sekiro.Combat;
using UnityEngine;

namespace Sekiro.BehaviourMachine
{
	/// <summary>
	/// Ground attack behaviour base class - 地面攻击行为基类
	/// 封装攻击阶段时序管理（Charging → Windup → Attacking → Recovery → Exit）、
	/// 视觉进度属性、动画Hash缓存、武器碰撞器控制等共享逻辑
	///
	/// 使用 AttackPhaseDriver 值类型管理阶段时序，零GC分配
	/// </summary>
	public abstract class GroundAttackBehaviour : PlayerBaseBehaviour
	{
		protected GroundAttackBehaviour(PlayerBehaviourMachine machine) : base(machine)
		{
		}

		#region Protected Fields

		protected ComboConfigSO _currentCombo;
		protected bool _needsComboInit;
		protected Vector3 _attackDirection;
		protected int _currentComboHash;
		protected bool _hasEnableCollider;

		/// <summary>远程攻击窗口索引 — 标记下一个待触发的远程窗口位置</summary>
		protected int _nextRangedWindowIndex;

		/// <summary>攻击阶段驱动器（值类型，零GC）</summary>
		protected internal AttackPhaseDriver PhaseDriver;

		#endregion

		#region Public Properties (for Visualizer)


		public AttackState VisualCurrentState => PhaseDriver.CurrentState;
		public float VisualStateProgress => PhaseDriver.StateProgress;
		public float VisualTotalProgress => PhaseDriver.TotalProgress;
		public bool VisualIsAttacking => PhaseDriver.IsAttacking;

		#endregion

	public override void OnEnter()
	{
		base.OnEnter();
		_attackDirection = Machine.transform.forward;
		_needsComboInit = true;
		_nextRangedWindowIndex = 0;
		Profile = PlayerBehaviourProfile.CombatDefault(CharacterConfig.FallForce);
		Locomotion.ActiveRootMotion = LocomotionDriver.RootMotionMode.Modify;
		Locomotion.ActiveRotation = LocomotionDriver.RotationSource.FixedDirection;
		Locomotion.FixedDirection = _attackDirection;
		Locomotion.RotationSpeed = CharacterConfig.NormalRotateSpeed;
	}

	public override void OnExit()
	{
		base.OnExit();
		if (Machine.Weapons != null)
		{
			foreach (var weapon in Machine.Weapons)
			{
				if (weapon != null)
					weapon.ResetWeapon();
			}
		}

		if (Machine.RangedWeapons != null)
		{
			foreach (var rangedWeapon in Machine.RangedWeapons)
			{
				if (rangedWeapon != null)
					rangedWeapon.ResetWeapon();
			}
		}
	}

		/// <summary>
		/// 初始化连击时序
		/// </summary>
		protected void InitializeCombo()
		{
			_needsComboInit = false;
			_attackDirection = Variable.InputDirection != Vector3.zero ? Variable.InputDirection : _attackDirection;
			Locomotion.FixedDirection = _attackDirection;
			_hasEnableCollider = false;
			_nextRangedWindowIndex = 0;

			AttackWeaponHelper.InitializeCombo(
				Machine.ComboDatabase,
				(hash, time) => Methods.FadeAnimation(hash, time),
				_currentCombo,
				ref PhaseDriver);

			StateData.CurrentAttackStateValue = PhaseDriver.CurrentState;
			StateData.IsAttackingValue = PhaseDriver.IsAttacking;
		}


		/// <summary>
		/// 推进阶段时序
		/// </summary>
		protected void AdvancePhase()
		{
			AttackWeaponHelper.AdvancePhase(
				ref PhaseDriver,
				_currentCombo,
				out var currentState,
				out var isAttacking);

			StateData.CurrentAttackStateValue = currentState;
			StateData.IsAttackingValue = isAttacking;
		}


	/// <summary>
	/// 在攻击阶段中点启用武器碰撞器（仅执行一次）
	/// </summary>
	protected void TryEnableWeaponCollider()
	{
		AttackWeaponHelper.TryEnableWeaponCollider(
			Machine.Weapons,
			_currentCombo,
			ref PhaseDriver,
			ref _hasEnableCollider);
	}

	/// <summary>
	/// 在攻击阶段中点发射远程投射物（仅执行一次）
	/// </summary>
	protected void TryFireRangedAttack()
	{
		AttackWeaponHelper.TryFireRangedAttack(
			Machine.RangedWeapons,
			_currentCombo,
			ref PhaseDriver,
			ref _nextRangedWindowIndex);
	}

		/// <summary>
		/// 尝试进入下一段连击
		/// </summary>
		protected void TryNextCombo(ComboConfigSO nextCombo)
		{
			if (Inputs.OutputIns(InputType.Attack))
			{
				Variable.CurrentCombo = nextCombo;
				_needsComboInit = true;
			}
		}
	}
}
