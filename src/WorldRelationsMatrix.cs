using DCFApixels.DragonECS.Relations.Utils;

namespace DCFApixels.DragonECS
{
    internal static class WorldRelationsMatrix
    {
        private static SparseArray64<RelationManager> _matrix = new SparseArray64<RelationManager>(4);

        internal static RelationManager Register(EcsWorld world, EcsWorld otherWorld, EcsRelationWorld relationWorld)
        {
            int worldID = world.id;
            int otherWorldID = otherWorld.id;
#if DEBUG
            if (_matrix.Contains(worldID, otherWorldID))
                throw new EcsFrameworkException();
#endif
            RelationManager manager = new RelationManager(world, relationWorld, otherWorld);
            _matrix[worldID, otherWorldID] = manager;
            return manager;
        }
        internal static void Unregister(EcsWorld world, EcsWorld otherWorld)
        {
            int worldID = world.id;
            int otherWorldID = otherWorld.id;
            //var manager = _matrix[worldID, otherWorldID];
            _matrix.Remove(worldID, otherWorldID);
        }

        internal static RelationManager Get(EcsWorld world, EcsWorld otherWorld)
        {
#if DEBUG
            if (!_matrix.Contains(world.id, otherWorld.id))
                throw new EcsFrameworkException();
#endif
            return _matrix[world.id, otherWorld.id];
        }
        internal static bool HasRelation(EcsWorld world, EcsWorld otherWorld) => HasRelation(world.id, otherWorld.id);
        internal static bool HasRelation(int worldID, int otherWorldID) => _matrix.Contains(worldID, otherWorldID);
    }
}
