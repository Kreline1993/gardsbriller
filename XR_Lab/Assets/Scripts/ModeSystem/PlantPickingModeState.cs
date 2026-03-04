public sealed class PlantPickingModeState : ModeStateBase
{
    public override AppMode Mode => AppMode.PlantPicking;

    public PlantPickingModeState(ModeContext context) : base(context) { }

    public override void Enter()
    {
        context.PlantVisualRegistry?.ResetAll();
    }

    public override void Exit() { }
}
