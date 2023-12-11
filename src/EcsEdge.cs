﻿using DCFApixels.DragonECS.Relations.Utils;
using System;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    //Edge world
    //Relation entity
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

        public bool IsLoop => _world == _otherWorld;

        internal EcsEdge(EcsWorld world, EcsWorld otherWorld, EcsEdgeWorld edgeWorld)
        {
            _edgeWorld = edgeWorld;
            _world = world;
            _otherWorld = otherWorld;

            _relationTargets = new RelationTargets[edgeWorld.Capacity];

            _edgeWorld.AddListener(worldEventListener: this);
            _edgeWorld.AddListener(entityEventListener: this);

            Forward = new ForwardOrientation(this);
            Reverse = new ReverseOrientation(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly RelationTargets GetRelationTargets(int arcEntityID)
        {
            return ref _relationTargets[arcEntityID];
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
                Forward.Del(rel.entity, rel.otherEntity);
        }
        #endregion

        #region Orientation
        public readonly struct ForwardOrientation : IEcsEdgeOrientation
        {
            private readonly EcsEdge _source;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ForwardOrientation(EcsEdge source) => _source = source;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int New(int entityID, int otherEntityID) => _source.NewRelation(entityID, otherEntityID);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Bind(int arcEntityID, int entityID, int otherEntityID) => _source.BindRelation(arcEntityID, entityID, otherEntityID);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Has(int entityID, int otherEntityID) => _source.HasRelation(entityID, otherEntityID);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Get(int entityID, int otherEntityID) => _source.GetRelation(entityID, otherEntityID);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGet(int entityID, int otherEntityID, out int arcEntityID) => _source.TryGetRelation(entityID, otherEntityID, out arcEntityID);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public IdsLinkedList.Span Get(int entityID) => _source._basket.GetSpanFor(entityID);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public IdsLinkedList.LongSpan GetLongs(int entityID) => _source._basket.GetLongSpanFor(_source._world, entityID);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Del(int entityID, int otherEntityID) => _source.DelRelation(entityID, otherEntityID);
        }
        public readonly struct ReverseOrientation : IEcsEdgeOrientation
        {
            private readonly EcsEdge _source;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ReverseOrientation(EcsEdge source) => _source = source;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int New(int otherEntityID, int entityID) => _source.NewRelation(entityID, otherEntityID);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Bind(int arcEntityID, int entityID, int otherEntityID) => _source.BindRelation(arcEntityID, otherEntityID, entityID);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Has(int otherEntityID, int entityID) => _source.HasRelation(entityID, otherEntityID);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Get(int otherEntityID, int entityID) => _source.GetRelation(entityID, otherEntityID);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGet(int otherEntityID, int entityID, out int arcEntityID) => _source.TryGetRelation(entityID, otherEntityID, out arcEntityID);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public IdsLinkedList.Span Get(int otherEntityID) => _source._otherBasket.GetSpanFor(otherEntityID);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public IdsLinkedList.LongSpan GetLongs(int otherEntityID) => _source._otherBasket.GetLongSpanFor(_source._otherWorld, otherEntityID);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Del(int otherEntityID, int entityID) => _source.DelRelation(entityID, otherEntityID);
        }

        //public readonly ref struct FilterIterator 
        //{
        //    private readonly IdsLinkedList.Span _listSpan;
        //    private readonly EcsMask _mask;
        //    public FilterIterator(EcsWorld world, IdsLinkedList.Span listSpan, EcsMask mask)
        //    {
        //        _listSpan = listSpan;
        //        _mask = mask;
        //    }
        //    public Enumerator GetEnumerator() => new Enumerator(_listSpan, _mask);
        //    public ref struct Enumerator
        //    {
        //        private readonly IdsLinkedList.SpanEnumerator _listEnumerator;
        //        private readonly EcsMask _mask;
        //        public Enumerator(IdsLinkedList.Span listSpan, EcsMask mask)
        //        {
        //            _listEnumerator = listSpan.GetEnumerator();
        //            _mask = mask;
        //        }
        //        public int Current => _listEnumerator.Current;
        //        public bool MoveNext()
        //        {
        //            while (_listEnumerator.MoveNext())
        //            {
        //                int e = _listEnumerator.Current;
        //                ...
        //            }
        //            return false;
        //        }
        //    }
        //}
        #endregion
    }
    public interface IEcsEdgeOrientation
    {
        public int New(int entityID, int otherEntityID);
        public void Bind(int arcEntityID, int entityID, int otherEntityID);
        public bool Has(int entityID, int otherEntityID);
        public int Get(int entityID, int otherEntityID);
        public bool TryGet(int otherEntityID, int entityID, out int arcEntityID);
        public IdsLinkedList.Span Get(int entityID);
        public IdsLinkedList.LongSpan GetLongs(int entityID);
        public void Del(int entityID, int otherEntityID);
    }
}