using DCFApixels.DragonECS.Relations.Utils;
using System;

namespace DCFApixels.DragonECS
{
    internal static class WorldGraph
    {
        private static SparseArray64<EcsEdge> _matrix = new SparseArray64<EcsEdge>(4);

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

    public static class WorldGraphExtensions
    {
        public static void SetEdgeWithSelf(this EcsWorld self) => SetEdgeWith(self, self);
        public static void SetEdgeWith(this EcsWorld self, EcsWorld otherWorld)
        {
            if (self == null || otherWorld == null)
                throw new ArgumentNullException();
            WorldGraph.Register(self, otherWorld, new EcsEdgeWorld());
        }
        public static void SetEdgeWithSelf(this EcsWorld self, EcsEdgeWorld relationWorld) => SetEdgeWith(self, self, relationWorld);
        public static void SetEdgeWith(this EcsWorld self, EcsWorld otherWorld, EcsEdgeWorld edgeWorld)
        {
            if (self == null || otherWorld == null || edgeWorld == null)
                throw new ArgumentNullException();
            WorldGraph.Register(self, otherWorld, edgeWorld);
        }

        public static void HasEdgeWithSelf(this EcsWorld self) => HasEdgeWith(self, self);
        public static void HasEdgeWith(this EcsWorld self, EcsWorld otherWorld)
        {
            if (self == null || otherWorld == null)
                throw new ArgumentNullException();
            WorldGraph.HasEdge(self, otherWorld);
        }

        public static EcsEdge GetEdgeWithSelf(this EcsWorld self) => GetEdgeWith(self, self);
        public static EcsEdge GetEdgeWith(this EcsWorld self, EcsWorld otherWorld)
        {
            if (self == null || otherWorld == null)
                throw new ArgumentNullException();
            return WorldGraph.Get(self, otherWorld);
        }

        public static void DelEdgeWithSelf(this EcsWorld self) => DelEdgeWith(self, self);
        public static void DelEdgeWith(this EcsWorld self, EcsWorld otherWorld)
        {
            if (self == null || otherWorld == null)
                throw new ArgumentNullException();
            WorldGraph.Unregister(self, otherWorld);
        }
    }
}
