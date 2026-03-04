public abstract class ModeStateBase : IModeState
{
    protected readonly ModeContext context;

    public abstract AppMode Mode { get; }

    protected ModeStateBase(ModeContext context)
    {
        this.context = context;
    }

    public abstract void Enter();
    public abstract void Exit();
    public virtual void Tick() { }
}
