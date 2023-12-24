using DCFApixels.DragonECS.Relations.Internal;
using DCFApixels.DragonECS.Relations.Utils;
using System;

namespace DCFApixels.DragonECS.Relations.Internal
{
    internal static class WorldGraph
    {
        private static readonly SparseArray64<EcsEdge> _matrix = new SparseArray64<EcsEdge>(4);

        internal static EcsEdge Register(EcsWorld world, EcsWorld otherWorld, EcsEdgeWorld edgeWorld)
        {
            int worldID = world.id;
            int otherWorldID = otherWorld.id;
#if DEBUG
            if (_matrix.Contains(worldID, otherWorldID))
                throw new EcsFrameworkException();
#endif
            EcsEdge edge = new EcsEdge(world, otherWorld, edgeWorld);
            _matrix[worldID, otherWorldID] = edge;
            _matrix[otherWorldID, worldID] = edge;
            return edge;
        }
        internal static void Unregister(EcsWorld world, EcsWorld otherWorld)
        {
            int worldID = world.id;
            int otherWorldID = otherWorld.id;
            //var manager = _matrix[worldID, otherWorldID];
            _matrix.Remove(worldID, otherWorldID);
            _matrix.Remove(otherWorldID, worldID);
        }

        internal static EcsEdge Get(EcsWorld world, EcsWorld otherWorld)
        {
#if DEBUG
            if (!_matrix.Contains(world.id, otherWorld.id))
                throw new EcsFrameworkException();
#endif
            return _matrix[world.id, otherWorld.id];
        }
        internal static bool HasEdge(EcsWorld world, EcsWorld otherWorld) => HasEdge(world.id, otherWorld.id);
        internal static bool HasEdge(int worldID, int otherWorldID) => _matrix.Contains(worldID, otherWorldID);
    }
}

namespace DCFApixels.DragonECS
{
    public static class WorldGraphExtensions
    {
        public static EcsEdge SetEdgeWithSelf(this EcsWorld self) => SetEdgeWith(self, self);
        public static EcsEdge SetEdgeWith(this EcsWorld self, EcsWorld otherWorld)
        {
            if (self == null || otherWorld == null)
                throw new ArgumentNullException();
            return WorldGraph.Register(self, otherWorld, new EcsEdgeWorld());
        }
        public static EcsEdge SetEdgeWithSelf(this EcsWorld self, EcsEdgeWorld edgeWorld) => SetEdgeWith(self, self, edgeWorld);
        public static EcsEdge SetEdgeWith(this EcsWorld self, EcsWorld otherWorld, EcsEdgeWorld edgeWorld)
        {
            if (self == null || otherWorld == null || edgeWorld == null)
                throw new ArgumentNullException();
            return WorldGraph.Register(self, otherWorld, edgeWorld);
        }

        public static bool HasEdgeWithSelf(this EcsWorld self) => HasEdgeWith(self, self);
        public static bool HasEdgeWith(this EcsWorld self, EcsWorld otherWorld)
        {
            if (self == null || otherWorld == null)
                throw new ArgumentNullException();
            return WorldGraph.HasEdge(self, otherWorld);
        }

        public static EcsEdge GetEdgeWithSelf(this EcsWorld self) => GetEdgeWith(self, self);
        public static EcsEdge GetEdgeWith(this EcsWorld self, EcsWorld otherWorld)
        {
            if (self == null || otherWorld == null)
                throw new ArgumentNullException();
            return WorldGraph.Get(self, otherWorld);
        }

        public static void DestroyEdgeWithSelf(this EcsWorld self) => DestroyEdgeWith(self, self);
        public static void DestroyEdgeWith(this EcsWorld self, EcsWorld otherWorld)
        {
            if (self == null || otherWorld == null)
                throw new ArgumentNullException();
            WorldGraph.Get(self, otherWorld).EdgeWorld.Destroy();
            WorldGraph.Unregister(self, otherWorld);
        }
    }
}
