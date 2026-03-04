public sealed class ModeContext
{
    public TwinDatabase TwinDatabase { get; }
    public PlantVisualRegistry PlantVisualRegistry { get; }

    public ModeContext(
        TwinDatabase twinDatabase,
        PlantVisualRegistry plantVisualRegistry)
    {
        TwinDatabase = twinDatabase;
        PlantVisualRegistry = plantVisualRegistry;
    }
}
