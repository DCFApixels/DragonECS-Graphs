using DCFApixels.DragonECS.Relations.Utils;
using System;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    //Relation entity
    //Relation
    //Relation component
    public readonly struct RelationData
    {
        public readonly RelationManager manager;
        public RelationData(RelationManager manager)
        {
            this.manager = manager;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly RelationTargets GetRelationTargets(int relationEntityID) => ref manager.GetRelationTargets(relationEntityID);
    }
    public class RelationManager : IEcsWorldEventListener, IEcsEntityEventListener
    {
        private EcsRelationWorld _relationWorld;

        private EcsWorld _world;
        private EcsWorld _otherWorld;

        private readonly IdsBasket _basket = new IdsBasket(256);
        private readonly IdsBasket _otherBasket = new IdsBasket(256);
        private SparseArray64<int> _relationsMatrix = new SparseArray64<int>();

        public readonly ForwardOrientation Forward;
        public readonly ReverseOrientation Reverse;

        private RelationTargets[] _relationTargets;

        public EcsWorld World => _world;
        public EcsWorld RelationWorld => _relationWorld;
        public EcsWorld OtherWorld => _otherWorld;
        public bool IsSolo => _world == _otherWorld;

        internal RelationManager(EcsWorld world, EcsRelationWorld relationWorld, EcsWorld otherWorld)
        {
            _relationWorld = relationWorld;
            _world = world;
            _otherWorld = otherWorld;

            _relationTargets = new RelationTargets[relationWorld.Capacity];

            _relationWorld.AddListener(worldEventListener: this);
            _relationWorld.AddListener(entityEventListener: this);

            Forward = new ForwardOrientation(this);
            Reverse = new ReverseOrientation(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly RelationTargets GetRelationTargets(int relationEntityID)
        {
            return ref _relationTargets[relationEntityID];
        }

        #region Methods

        #region New/Del
        private int NewRelation(int entityID, int otherEntityID)
        {

            if (HasRelation(entityID, otherEntityID))
                throw new EcsRelationException();
            int e = _relationWorld.NewEmptyEntity();
            _basket.AddToHead(entityID, otherEntityID);
            _otherBasket.AddToHead(otherEntityID, entityID);
            _relationsMatrix.Add(entityID, otherEntityID, e);
            _relationTargets[e] = new RelationTargets(entityID, otherEntityID);
            return e;
        }
        private void DelRelation(int entityID, int otherEntityID)
        {
            if (!_relationsMatrix.TryGetValue(entityID, otherEntityID, out int e))
                throw new EcsRelationException();
            _relationsMatrix.Remove(entityID, otherEntityID);
            _basket.DelHead(entityID);
            _otherBasket.Del(entityID);
            _relationWorld.DelEntity(e);
            _relationTargets[e] = RelationTargets.Empty;
        }
        #endregion

        #region Has
        private bool HasRelation(int entityID, int otherEntityID) => _relationsMatrix.Contains(entityID, otherEntityID);
        //public bool HasRelationWith(EcsSubject subject, int entityID, int otherEntityID)
        //{
        //    if (subject.World != _relationWorld)
        //        throw new ArgumentException();
        //    return _source._relationsMatrix.TryGetValue(entityID, otherEntityID, out int entity) && subject.IsMatches(entity);
        //}
        #endregion

        #region GetRelation
        private int GetRelation(int entityID, int otherEntityID)
        {
            if (!_relationsMatrix.TryGetValue(entityID, otherEntityID, out int e))
                throw new EcsRelationException();
            return e;
        }
        private bool TryGetRelation(int entityID, int otherEntityID, out int entity)
        {
            return _relationsMatrix.TryGetValue(entityID, otherEntityID, out entity);
        }
        //public bool TryGetRelation(EcsSubject subject, int entityID, int otherEntityID, out int entity)
        //{
        //    return _source._relationsMatrix.TryGetValue(entityID, otherEntityID, out entity) && subject.IsMatches(entity);
        //}
        #endregion

        //#region GetRelations
        //private IdsLinkedList.Span GetRelations(int entityID)
        //{
        //    return _basket.GetSpanFor(entityID);
        //}
        ////ReadOnlySpan<int> временная заглушка, потому тут будет спан из линкедлиста
        ////public ReadOnlySpan<int> GetRelationsWith(EcsSubject subject, int entityID)
        ////{
        ////    if (subject.World != _relationWorld)
        ////        throw new ArgumentException();
        ////    throw new NotImplementedException();
        ////}
        //#endregion

        #endregion

        #region Callbacks
        void IEcsWorldEventListener.OnWorldResize(int newSize)
        {
            Array.Resize(ref _relationTargets, newSize);
        }
        void IEcsWorldEventListener.OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer) { }
        void IEcsWorldEventListener.OnWorldDestroy() { }

        void IEcsEntityEventListener.OnNewEntity(int entityID) { }
        void IEcsEntityEventListener.OnDelEntity(int entityID)
        {
            ref RelationTargets rel = ref _relationTargets[entityID];
            if (_relationsMatrix.Contains(rel.entity, rel.otherEntity))
                Forward.DelRelation(rel.entity, rel.otherEntity);
        }
        #endregion


        #region Orientation
        public readonly struct ForwardOrientation
        {
            private readonly RelationManager _source;
            internal ForwardOrientation(RelationManager source) => _source = source;
            public int NewRelation(int entityID, int otherEntityID) => _source.NewRelation(entityID, otherEntityID);
            public void DelRelation(int entityID, int otherEntityID) => _source.DelRelation(entityID, otherEntityID);
            public bool HasRelation(int entityID, int otherEntityID) => _source.HasRelation(entityID, otherEntityID);
            public int GetRelation(int entityID, int otherEntityID) => _source.GetRelation(entityID, otherEntityID);
            public bool TryGetRelation(int entityID, int otherEntityID, out int relationEntityID) => _source.TryGetRelation(entityID, otherEntityID, out relationEntityID);
            public IdsLinkedList.Span GetRelations(int relationEntityID) => _source._basket.GetSpanFor(relationEntityID);
        }
        public readonly struct ReverseOrientation
        {
            private readonly RelationManager _source;
            internal ReverseOrientation(RelationManager source) => _source = source;
            public int NewRelation(int otherEntityID, int entityID) => _source.NewRelation(entityID, otherEntityID);
            public void DelRelation(int otherEntityID, int entityID) => _source.DelRelation(entityID, otherEntityID);
            public bool HasRelation(int otherEntityID, int entityID) => _source.HasRelation(entityID, otherEntityID);
            public int GetRelation(int otherEntityID, int entityID) => _source.GetRelation(entityID, otherEntityID);
            public bool TryGetRelation(int otherEntityID, int entityID, out int relationEntityID) => _source.TryGetRelation(entityID, otherEntityID, out relationEntityID);
            public IdsLinkedList.Span GetRelations(int relationEntityID) => _source._otherBasket.GetSpanFor(relationEntityID);
        }

        public struct RelationsSpan
        {
            private readonly IdsBasket _basket;
            private readonly EcsAspect _aspect;
        }
        #endregion
    }

    public static class WorldRelationExtensions
    {
        public static void SetRelationWithSelf(this EcsWorld self) => SetRelationWith(self, self);
        public static void SetRelationWith(this EcsWorld self, EcsWorld otherWorld)
        {
            if (self == null || otherWorld == null)
                throw new ArgumentNullException();
            WorldRelationsMatrix.Register(self, otherWorld, new EcsRelationWorld());
        }
        public static void SetRelationWithSelf(this EcsWorld self, EcsRelationWorld relationWorld) => SetRelationWith(self, relationWorld);
        public static void SetRelationWith(this EcsWorld self, EcsWorld otherWorld, EcsRelationWorld relationWorld)
        {
            if (self == null || otherWorld == null || relationWorld == null)
                throw new ArgumentNullException();
            WorldRelationsMatrix.Register(self, otherWorld, relationWorld);
        }

        public static void DelRelationWithSelf(this EcsWorld self, EcsWorld otherWorld) => DelRelationWith(self, self);
        public static void DelRelationWith(this EcsWorld self, EcsWorld otherWorld)
        {
            if (self == null || otherWorld == null)
                throw new ArgumentNullException();
            WorldRelationsMatrix.Unregister(self, otherWorld);
        }

        public static RelationManager GetRelationWithSelf(this EcsWorld self) => GetRelationWith(self, self);
        public static RelationManager GetRelationWith(this EcsWorld self, EcsWorld otherWorld)
        {
            if (self == null || otherWorld == null)
                throw new ArgumentNullException();
            return WorldRelationsMatrix.Get(self, otherWorld);
        }
    }
}
