namespace DCFApixels.DragonECS
{
    public abstract class EcsArcWorld : EcsWorld
    {
        public EcsArcWorld() : base(null) { }
        public EcsArcWorld(IEcsWorldConfig config) : base(config) { }
    }
    public sealed class EcsLoopArcWorld<TWorld> : EcsArcWorld
        where TWorld : EcsWorld
    {
        public EcsLoopArcWorld() : base(null) { }
        public EcsLoopArcWorld(IEcsWorldConfig config) : base(config) { }
    }
    public sealed class EcsArcWorld<TStartWorld, TEndWorld> : EcsArcWorld
        where TStartWorld : EcsWorld
        where TEndWorld : EcsWorld
    {
        public EcsArcWorld() : base(null) { }
        public EcsArcWorld(IEcsWorldConfig config) : base(config) { }
    }
}
