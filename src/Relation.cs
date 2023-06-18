using DCFApixels.DragonECS;

namespace DragonECS.DragonECS
{
    internal readonly struct Relation : IEcsComponent
    {
        public readonly int entity;
        public readonly int otherEntity;
    }
}