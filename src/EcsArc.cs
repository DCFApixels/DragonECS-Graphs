using DCFApixels.DragonECS.Relations.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    //Edge world
    //Relation entity
    //Relation component

    public class EcsEdge
    {

    }
    public class EcsArc : IEcsWorldEventListener, IEcsEntityEventListener
    {
        private readonly EcsWorld _startWorld;
        private readonly EcsWorld _endWorld;
        private readonly EcsArcWorld _arcWorld;

        private readonly VertexWorldHandler _worldHandler;

        private readonly IdsBasket _basket = new IdsBasket(256);

        private readonly SparseArray64<int> _relationsMatrix = new SparseArray64<int>();

        private ArcEntityInfo[] _arkEntityInfos; //N * (N - 1) / 2

        #region Properties
        public EcsWorld StartWorld => _startWorld;
        public EcsWorld EndWorld => _endWorld;
        public EcsArcWorld ArcWorld => _arcWorld;

        public bool IsLoop => _startWorld == _endWorld;
        #endregion

        #region Constructors
        internal EcsArc(EcsWorld world, EcsWorld otherWorld, EcsArcWorld arcWorld)
        {
            _arcWorld = arcWorld;
            _startWorld = world;
            _endWorld = otherWorld;

            _worldHandler = new VertexWorldHandler(this, _startWorld, _basket);
            _startWorld.AddListener(_worldHandler);

            _arkEntityInfos = new ArcEntityInfo[arcWorld.Capacity];

            _arcWorld.AddListener(worldEventListener: this);
            _arcWorld.AddListener(entityEventListener: this);
        }
        #endregion

        #region New/Del
        public int New(int startEntityID, int endEntityID)
        {
            if (Has(startEntityID, endEntityID))
                throw new EcsRelationException();
            int arcEntity = _arcWorld.NewEntity();
            _basket.AddToHead(startEntityID, endEntityID);
            _relationsMatrix.Add(startEntityID, endEntityID, arcEntity);
            _arkEntityInfos[arcEntity] = new ArcEntityInfo(startEntityID, endEntityID);
            return arcEntity;
        }
        public void Del(int startEntityID, int endEntityID)
        {
            if (!_relationsMatrix.TryGetValue(startEntityID, endEntityID, out int e))
                throw new EcsRelationException();
            _relationsMatrix.Remove(startEntityID, endEntityID);
            _basket.DelHead(startEntityID);
            _arcWorld.DelEntity(e);
            _arkEntityInfos[e] = ArcEntityInfo.Empty;
        }
        #endregion

        #region Get/Has
        public bool Has(int startEntityID, int endEntityID)
        {
            return _relationsMatrix.Contains(startEntityID, endEntityID);
        }
        //public bool HasRelationWith(EcsSubject subject, int entityID, int otherEntityID)
        //{
        //    if (subject.World != _relationWorld)
        //        throw new ArgumentException();
        //    return _source._relationsMatrix.TryGetValue(entityID, otherEntityID, out int entity) && subject.IsMatches(entity);
        //}
        public int Get(int startEntityID, int endEntityID)
        {
            if (!_relationsMatrix.TryGetValue(startEntityID, endEntityID, out int e))
                throw new EcsRelationException();
            return e;
        }
        public bool TryGet(int startEntityID, int endEntityID, out int arcEntityID)
        {
            return _relationsMatrix.TryGetValue(startEntityID, endEntityID, out arcEntityID);
        }
        //public bool TryGetRelation(EcsSubject subject, int entityID, int otherEntityID, out int entity)
        //{
        //    return _source._relationsMatrix.TryGetValue(entityID, otherEntityID, out entity) && subject.IsMatches(entity);
        //}

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

        #region ArcEntityInfo
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsArc(int arcEntityID)
        {
            if (arcEntityID <= 0 || arcEntityID >= _arkEntityInfos.Length)
                return false;
            return !_arkEntityInfos[arcEntityID].IsEmpty;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArcEntityInfo GetArcInfo(int arcEntityID)
        {
            if (arcEntityID <= 0 || arcEntityID >= _arkEntityInfos.Length)
                throw new Exception();
            return _arkEntityInfos[arcEntityID];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetArcStart(int arcEntityID)
        {
            return GetArcInfo(arcEntityID).start;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetArcEnd(int arcEntityID)
        {
            return GetArcInfo(arcEntityID).end;
        }
        #endregion

        #region Other
        public EcsArc GetInversetArc()
        {
            return _endWorld.GetArcWith(_startWorld);
        }
        public bool TryGetInversetArc(out EcsArc arc)
        {
            return _endWorld.TryGetArcWith(_startWorld, out arc);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IdsLinkedList.Span Get(int entityID) => _basket.GetSpanFor(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IdsLinkedList.LongSpan GetLongs(int entityID) => _basket.GetLongSpanFor(_startWorld, entityID);
        #endregion

        #region Callbacks
        void IEcsWorldEventListener.OnWorldResize(int newSize)
        {
            Array.Resize(ref _arkEntityInfos, newSize);
        }
        void IEcsWorldEventListener.OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer) { }
        void IEcsWorldEventListener.OnWorldDestroy() { }

        void IEcsEntityEventListener.OnNewEntity(int entityID) { }
        void IEcsEntityEventListener.OnDelEntity(int entityID)
        {
            ref ArcEntityInfo rel = ref _arkEntityInfos[entityID];
            if (_relationsMatrix.Contains(rel.start, rel.end))
                Del(rel.start, rel.end);
        }
        #endregion

        #region VertexWorldHandler
        private class VertexWorldHandler : IEcsEntityEventListener
        {
            private readonly EcsArc _source;
            private readonly EcsWorld _world;
            private readonly IdsBasket _basket;

            public VertexWorldHandler(EcsArc source, EcsWorld world, IdsBasket basket)
            {
                _source = source;
                _world = world;
                _basket = basket;
            }

            public void OnDelEntity(int entityID)
            {
                var span = _basket.GetSpanFor(entityID);
                foreach (var arcEntityID in span)
                {
                }
            }
            public void OnNewEntity(int entityID)
            {

            }
        }
        #endregion
    }
}