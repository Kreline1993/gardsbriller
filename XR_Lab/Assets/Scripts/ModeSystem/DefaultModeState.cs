public sealed class DefaultModeState : ModeStateBase
{
    public override AppMode Mode => AppMode.Default;

    public DefaultModeState(ModeContext context) : base(context) { }

    public override void Enter()
    {
        context.PlantVisualRegistry?.ResetAll();
    }

    public override void Exit() { }
}
