public interface IModeState
{
    AppMode Mode { get; }
    void Enter();
    void Exit();
    void Tick();
}
