namespace DCFApixels.DragonECS
{
    public abstract class EcsArcWorld : EcsWorld { }
    public sealed class EcsLoopArcWorld<TWorld> : EcsArcWorld
        where TWorld : EcsWorld
    { }
    public sealed class EcsArcWorld<TStartWorld, TEndWorld> : EcsArcWorld
        where TStartWorld : EcsWorld
        where TEndWorld : EcsWorld
    { }
}
