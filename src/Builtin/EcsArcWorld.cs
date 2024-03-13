namespace DCFApixels.DragonECS
{
    public abstract class EcsArcWorld : EcsWorld
    {
        public EcsArcWorld(ConfigContainer config = null) : base(config) { }
        public EcsArcWorld(IConfigContainer config = null) : base(config) { }
    }
    public sealed class EcsLoopArcWorld<TWorld> : EcsArcWorld
        where TWorld : EcsWorld
    {
        public EcsLoopArcWorld(ConfigContainer config = null) : base(config) { }
        public EcsLoopArcWorld(IConfigContainer config = null) : base(config) { }
    }
    public sealed class EcsArcWorld<TStartWorld, TEndWorld> : EcsArcWorld
        where TStartWorld : EcsWorld
        where TEndWorld : EcsWorld
    {
        public EcsArcWorld(ConfigContainer config = null) : base(config) { }
        public EcsArcWorld(IConfigContainer config = null) : base(config) { }
    }
}
