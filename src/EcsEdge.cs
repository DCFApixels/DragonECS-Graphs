using DCFApixels.DragonECS.Relations.Utils;
using System;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    //Edge world
    //Relation entity
    //Relation
    //Relation component
    public class EcsEdge : IEcsWorldEventListener, IEcsEntityEventListener
    {
        private readonly EcsWorld _world;
        private readonly EcsWorld _otherWorld;
        private readonly EcsEdgeWorld _edgeWorld;

        private readonly IdsBasket _basket = new IdsBasket(256);
        private readonly IdsBasket _otherBasket = new IdsBasket(256);
        private readonly SparseArray64<int> _relationsMatrix = new SparseArray64<int>();

        public readonly ForwardOrientation Forward;
        public readonly ReverseOrientation Reverse;

        private RelationTargets[] _relationTargets;

        public EcsWorld World => _world;
        public EcsWorld OtherWorld => _otherWorld;
        public EcsEdgeWorld EdgeWorld => _edgeWorld;

        public bool IsSolo => _world == _otherWorld;

        internal EcsEdge(EcsWorld world, EcsWorld otherWorld, EcsEdgeWorld relationWorld)
        {
            _edgeWorld = relationWorld;
            _world = world;
            _otherWorld = otherWorld;

            _relationTargets = new RelationTargets[relationWorld.Capacity];

            _edgeWorld.AddListener(worldEventListener: this);
            _edgeWorld.AddListener(entityEventListener: this);

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
            int e = _edgeWorld.NewEmptyEntity();
            _basket.AddToHead(entityID, otherEntityID);
            _otherBasket.AddToHead(otherEntityID, entityID);
            _relationsMatrix.Add(entityID, otherEntityID, e);
            _relationTargets[e] = new RelationTargets(entityID, otherEntityID);
            return e;
        }
        private void BindRelation(int relationEntityID, int entityID, int otherEntityID)
        {
            ref var rel = ref _relationTargets[relationEntityID];
            if (HasRelation(entityID, otherEntityID) || rel.IsEmpty)
                throw new EcsRelationException();
            _basket.AddToHead(entityID, otherEntityID);
            _otherBasket.AddToHead(otherEntityID, entityID);
            _relationsMatrix.Add(entityID, otherEntityID, relationEntityID);
            rel = new RelationTargets(entityID, otherEntityID);
        }
        private void DelRelation(int entityID, int otherEntityID)
        {
            if (!_relationsMatrix.TryGetValue(entityID, otherEntityID, out int e))
                throw new EcsRelationException();
            _relationsMatrix.Remove(entityID, otherEntityID);
            _basket.DelHead(entityID);
            _otherBasket.Del(entityID);
            _edgeWorld.DelEntity(e);
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

        #region GetRelations
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
            private readonly EcsEdge _source;
            internal ForwardOrientation(EcsEdge source) => _source = source;
            public int NewRelation(int entityID, int otherEntityID) => _source.NewRelation(entityID, otherEntityID);
            public void BindRelation(int relationEntityID, int entityID, int otherEntityID) => _source.BindRelation(relationEntityID, entityID, otherEntityID);
            public bool HasRelation(int entityID, int otherEntityID) => _source.HasRelation(entityID, otherEntityID);
            public int GetRelation(int entityID, int otherEntityID) => _source.GetRelation(entityID, otherEntityID);
            public void DelRelation(int entityID, int otherEntityID) => _source.DelRelation(entityID, otherEntityID);
            public bool TryGetRelation(int entityID, int otherEntityID, out int relationEntityID) => _source.TryGetRelation(entityID, otherEntityID, out relationEntityID);
            public IdsLinkedList.Span GetRelations(int entityID) => _source._basket.GetSpanFor(entityID);
        }
        public readonly struct ReverseOrientation
        {
            private readonly EcsEdge _source;
            internal ReverseOrientation(EcsEdge source) => _source = source;
            public int NewRelation(int otherEntityID, int entityID) => _source.NewRelation(entityID, otherEntityID);
            public void BindRelation(int relationEntityID, int entityID, int otherEntityID) => _source.BindRelation(relationEntityID, otherEntityID, entityID);
            public bool HasRelation(int otherEntityID, int entityID) => _source.HasRelation(entityID, otherEntityID);
            public int GetRelation(int otherEntityID, int entityID) => _source.GetRelation(entityID, otherEntityID);
            public void DelRelation(int otherEntityID, int entityID) => _source.DelRelation(entityID, otherEntityID);
            public bool TryGetRelation(int otherEntityID, int entityID, out int relationEntityID) => _source.TryGetRelation(entityID, otherEntityID, out relationEntityID);
            public IdsLinkedList.Span GetRelations(int otherEntityID) => _source._otherBasket.GetSpanFor(otherEntityID);
        }

        public struct RelationsSpan
        {
            private readonly IdsBasket _basket;
            private readonly EcsAspect _aspect;
        }
        #endregion
    }
}
