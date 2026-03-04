public sealed class OverviewModeState : ModeStateBase
{
    public override AppMode Mode => AppMode.Overview;

    public OverviewModeState(ModeContext context) : base(context) { }

    public override void Enter()
    {
        context.PlantVisualRegistry?.ResetAll();
    }

    public override void Exit() { }
}
