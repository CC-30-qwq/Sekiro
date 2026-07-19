namespace Sekiro.BehaviourMachine
{
    /// <summary>
    /// 输入状态接收接口 — 解耦 InputBuffer 与 InputsUtility 的循环依赖
    /// InputBuffer 仅依赖此接口，不再直接引用 InputsUtility 具体类型
    /// </summary>
    public interface IInputStateSink
    {
        void SetInputFlag(InputType type, bool value);
        void ClearAllInputFlags();
    }
}
