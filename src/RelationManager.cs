using DCFApixels.DragonECS;
using DCFApixels.DragonECS.Relations.Utils;

namespace DragonECS.DragonECS
{
    internal static class WorldRelationsMatrix
    {
        private static SparseArray64<RelationManager> _matrix;

        internal static bool HasRelation(EcsWorld world, EcsWorld otherWorld) => HasRelation(world.id, otherWorld.id);
        internal static bool HasRelation(int worldID, int otherWorldID) => _matrix.Contains(worldID, otherWorldID);

        internal static RelationManager Register(EcsWorld world, EcsWorld otherWorld, EcsRelationWorld relationWorld)
        {
            int worldID = world.id;
            int otherWorldID = otherWorld.id;
#if DEBUG
            if (_matrix.Contains(worldID, otherWorldID))
                throw new EcsFrameworkException();
#endif
            RelationManager manager = new RelationManager(relationWorld, world, otherWorld);
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

        public static RelationManager Get(EcsWorld world, EcsWorld otherWorld)
        {
#if DEBUG
            if (_matrix.Contains(world.id, otherWorld.id))
                throw new EcsFrameworkException();
#endif
            return _matrix[world.id, otherWorld.id];
        }
    }

    public class RelationManager : IEcsPoolEventListener, IEcsWorldEventListener, IEcsEntityEventListener
    {
        private EcsRelationWorld _relationWorld;

        private EcsWorld _world;
        private EcsWorld _otherWorld;

        private int[] _mapping = new int[256];
        private IdsLinkedList _idsBasket = new IdsLinkedList(256);
        private EcsPool<Relation> _relationsPool;

        public EcsWorld RelationWorld => _relationWorld;
        public bool IsSolo => _world == _otherWorld;

        internal RelationManager(EcsRelationWorld relationWorld, EcsWorld world, EcsWorld otherWorld)
        {
            _relationWorld = relationWorld;
            _world = world;
            _otherWorld = otherWorld;

            _relationsPool = relationWorld.GetPool<Relation>();
            _relationsPool.AddListener(this);
            _relationWorld.AddListener(worldEventListener: this);
            _relationWorld.AddListener(entityEventListener: this);
        }

        void IEcsPoolEventListener.OnAdd(int entityID)
        {
            //_idsBasket.
        }
        void IEcsPoolEventListener.OnGet(int entityID) { }
        void IEcsPoolEventListener.OnDel(int entityID) { }

        void IEcsWorldEventListener.OnWorldResize(int newSize) { }
        void IEcsWorldEventListener.OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer) { }
        void IEcsWorldEventListener.OnWorldDestroy() { }

        void IEcsEntityEventListener.OnNewEntity(int entityID) { }
        void IEcsEntityEventListener.OnDelEntity(int entityID)
        {

        }
    }


    public static class WorldRelationExtensions
    {
        public static void SetRelationWith(this EcsWorld self, EcsWorld otherWorld)
        {
            WorldRelationsMatrix.Register(self, otherWorld, new EcsRelationWorld());
        }
        public static void SetRelationWith(this EcsWorld self, EcsWorld otherWorld, EcsRelationWorld relationWorld)
        {
            WorldRelationsMatrix.Register(self, otherWorld, relationWorld);
        }
        public static void DelRelationWith(this EcsWorld self, EcsWorld otherWorld)
        {
            WorldRelationsMatrix.Unregister(self, otherWorld);
        }
    }
}
