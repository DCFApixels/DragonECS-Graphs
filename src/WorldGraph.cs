using DCFApixels.DragonECS.Relations.Internal;
using DCFApixels.DragonECS.Relations.Utils;
using System;

namespace DCFApixels.DragonECS.Relations.Internal
{
    internal static class WorldGraph
    {
        private static readonly SparseArray64<EcsArc> _matrix = new SparseArray64<EcsArc>(4);

        internal static EcsArc Register(EcsWorld startWorld, EcsWorld endWorld, EcsArcWorld edgeWorld)
        {
            int startWorldID = startWorld.id;
            int endWorldID = endWorld.id;
#if DEBUG
            if (_matrix.Contains(startWorldID, endWorldID))
                throw new EcsFrameworkException();
#endif
            EcsArc edge = new EcsArc(startWorld, endWorld, edgeWorld);
            _matrix[startWorldID, endWorldID] = edge;
            return edge;
        }
        internal static void Unregister(EcsWorld startWorld, EcsWorld endWorld)
        {
            int startWorldID = startWorld.id;
            int endWorldID = endWorld.id;
            _matrix.Remove(startWorldID, endWorldID);
        }

        internal static EcsArc Get(EcsWorld startWorld, EcsWorld otherWorld)
        {
#if DEBUG
            if (!_matrix.Contains(startWorld.id, otherWorld.id))
                throw new EcsFrameworkException();
#endif
            return _matrix[startWorld.id, otherWorld.id];
        }
        internal static bool HasArc(EcsWorld startWorld, EcsWorld endWorld) => HasArc(startWorld.id, endWorld.id);
        internal static bool HasArc(int startWorldID, int endWorldID) => _matrix.Contains(startWorldID, endWorldID);
    }
}

namespace DCFApixels.DragonECS
{
    public static class WorldGraphExtensions
    {
        public static EcsArc SetLoopArc(this EcsWorld self) => SetArcWith(self, self);
        public static EcsArc SetArcWith(this EcsWorld self, EcsWorld endWorld)
        {
            if (self == null || endWorld == null)
                throw new ArgumentNullException();
            return WorldGraph.Register(self, endWorld, new EcsArcWorld());
        }

        public static EcsArc SetLoopArc(this EcsWorld self, EcsArcWorld edgeWorld) => SetEdgeWith(self, self, edgeWorld);
        public static EcsArc SetEdgeWith(this EcsWorld startWorld, EcsWorld endWorld, EcsArcWorld edgeWorld)
        {
            if (startWorld == null || endWorld == null || edgeWorld == null)
                throw new ArgumentNullException();
            return WorldGraph.Register(startWorld, endWorld, edgeWorld);
        }

        public static bool HasLoopArc(this EcsWorld self) => HasArcWith(self, self);
        public static bool HasArcWith(this EcsWorld startWorld, EcsWorld endWorld)
        {
            if (startWorld == null || endWorld == null)
                throw new ArgumentNullException();
            return WorldGraph.HasArc(startWorld, endWorld);
        }

        public static EcsArc GetLoopArc(this EcsWorld self) => GetArcWith(self, self);
        public static EcsArc GetArcWith(this EcsWorld startWorld, EcsWorld endWorld)
        {
            if (startWorld == null || endWorld == null)
                throw new ArgumentNullException();
            return WorldGraph.Get(startWorld, endWorld);
        }

        public static bool TryGetLoopArc(this EcsWorld self, out EcsArc arc) => TryGetArcWith(self, self, out arc);
        public static bool TryGetArcWith(this EcsWorld startWorld, EcsWorld endWorld, out EcsArc arc)
        {
            if (startWorld == null || endWorld == null)
                throw new ArgumentNullException();
            bool result = WorldGraph.HasArc(startWorld, endWorld);
            arc = result ? WorldGraph.Get(startWorld, endWorld) : null;
            return result;
        }

        public static void DestroyLoopArc(this EcsWorld self) => DestroyArcWith(self, self);
        public static void DestroyArcWith(this EcsWorld startWorld, EcsWorld endWorld)
        {
            if (startWorld == null || endWorld == null)
                throw new ArgumentNullException();
            WorldGraph.Get(startWorld, endWorld).ArcWorld.Destroy();
            WorldGraph.Unregister(startWorld, endWorld);
        }
    }
}
