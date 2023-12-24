using DCFApixels.DragonECS.Relations.Utils;
using System;
using System.Collections.Generic;
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

        private readonly VertexWorldHandler _worldHandler;
        private readonly VertexWorldHandler _otherWorldHandler;

        private readonly IdsBasket _basket = new IdsBasket(256);
        private readonly IdsBasket _otherBasket = new IdsBasket(256);
        private readonly SparseArray64<int> _relationsMatrix = new SparseArray64<int>();

        private ArcTargets[] _arkTargets; //N * (N - 1) / 2

        private List<WeakReference<EcsJoinGroup>> _groups = new List<WeakReference<EcsJoinGroup>>();
        private Stack<EcsJoinGroup> _groupsPool = new Stack<EcsJoinGroup>(64);

        #region Properties
        public EcsWorld World => _world;
        public EcsWorld OtherWorld => _otherWorld;
        public EcsEdgeWorld EdgeWorld => _edgeWorld;

        public bool IsLoop => _world == _otherWorld;
        #endregion

        #region Constructors
        internal EcsEdge(EcsWorld world, EcsWorld otherWorld, EcsEdgeWorld edgeWorld)
        {
            _edgeWorld = edgeWorld;
            _world = world;
            _otherWorld = otherWorld;

            _worldHandler = new VertexWorldHandler(this, _world, _basket);
            _world.AddListener(_worldHandler);
            if (IsLoop)
            {
                _otherWorldHandler = _worldHandler;
            }
            else
            {
                _otherWorldHandler = new VertexWorldHandler(this, _otherWorld, _otherBasket);
                _world.AddListener(_otherWorldHandler);
            }

            _arkTargets = new ArcTargets[edgeWorld.Capacity];

            _edgeWorld.AddListener(worldEventListener: this);
            _edgeWorld.AddListener(entityEventListener: this);
        }
        #endregion

        #region Join Groups Pool
        internal void RegisterGroup(EcsJoinGroup group)
        {
            _groups.Add(new WeakReference<EcsJoinGroup>(group));
        }
        internal EcsJoinGroup GetFreeGroup()
        {
            EcsJoinGroup result = _groupsPool.Count <= 0 ? new EcsJoinGroup(this) : _groupsPool.Pop();
            result._isReleased = false;
            return result;
        }
        internal void ReleaseGroup(EcsJoinGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            if (group.Edge != this) throw new Exception();
#endif
            group._isReleased = true;
            group.Clear();
            _groupsPool.Push(group);
        }
        #endregion

        #region New/Del
        public int New(int entityID, int otherEntityID)
        {
            if (Has(entityID, otherEntityID))
                throw new EcsRelationException();
            int arcEntity = _edgeWorld.NewEntity();
            _basket.AddToHead(entityID, otherEntityID);
            _otherBasket.AddToHead(otherEntityID, entityID);
            _relationsMatrix.Add(entityID, otherEntityID, arcEntity);
            _arkTargets[arcEntity] = new ArcTargets(entityID, otherEntityID);
            return arcEntity;
        }
        public void Del(int entityID, int otherEntityID)
        {
            if (!_relationsMatrix.TryGetValue(entityID, otherEntityID, out int e))
                throw new EcsRelationException();
            _relationsMatrix.Remove(entityID, otherEntityID);
            _basket.DelHead(entityID);
            _otherBasket.Del(entityID);
            _edgeWorld.DelEntity(e);
            _arkTargets[e] = ArcTargets.Empty;
        }
        #endregion

        #region Get/Has
        public bool Has(int entityID, int otherEntityID) => _relationsMatrix.Contains(entityID, otherEntityID);
        //public bool HasRelationWith(EcsSubject subject, int entityID, int otherEntityID)
        //{
        //    if (subject.World != _relationWorld)
        //        throw new ArgumentException();
        //    return _source._relationsMatrix.TryGetValue(entityID, otherEntityID, out int entity) && subject.IsMatches(entity);
        //}
        public int Get(int entityID, int otherEntityID)
        {
            if (!_relationsMatrix.TryGetValue(entityID, otherEntityID, out int e))
                throw new EcsRelationException();
            return e;
        }
        private bool TryGet(int entityID, int otherEntityID, out int entity)
        {
            return _relationsMatrix.TryGetValue(entityID, otherEntityID, out entity);
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

        #region Other
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsArc(int arcEntityID)
        {
            if (arcEntityID <= 0 || arcEntityID >= _arkTargets.Length)
                return false;
            return !_arkTargets[arcEntityID].IsEmpty;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArcTargets GetArcTargets(int arcEntityID)
        {
            if (arcEntityID <= 0 || arcEntityID >= _arkTargets.Length)
                throw new Exception();
            return _arkTargets[arcEntityID];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IdsLinkedList.Span Get(int entityID) => _basket.GetSpanFor(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IdsLinkedList.LongSpan GetLongs(int entityID) => _basket.GetLongSpanFor(_world, entityID);
        #endregion

        #region Callbacks
        void IEcsWorldEventListener.OnWorldResize(int newSize)
        {
            Array.Resize(ref _arkTargets, newSize);
        }
        void IEcsWorldEventListener.OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer) { }
        void IEcsWorldEventListener.OnWorldDestroy() { }

        void IEcsEntityEventListener.OnNewEntity(int entityID) { }
        void IEcsEntityEventListener.OnDelEntity(int entityID)
        {
            ref ArcTargets rel = ref _arkTargets[entityID];
            if (_relationsMatrix.Contains(rel.start, rel.end))
                Del(rel.start, rel.end);
        }
        #endregion

        #region VertexWorldHandler
        private class VertexWorldHandler : IEcsEntityEventListener
        {
            private readonly EcsEdge _source;
            private readonly EcsWorld _world;
            private readonly IdsBasket _basket;

            public VertexWorldHandler(EcsEdge source, EcsWorld world, IdsBasket basket)
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