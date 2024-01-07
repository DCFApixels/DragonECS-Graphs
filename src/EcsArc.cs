using DCFApixels.DragonECS.Relations.Utils;
using System;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    //Edge world
    //Relation entity
    //Relation component
    public class EcsArc : IEcsWorldEventListener, IEcsEntityEventListener
    {
        private readonly EcsWorld _startWorld;
        private readonly EcsWorld _endWorld;
        private readonly EcsArcWorld _arcWorld;

        private readonly VertexWorldHandler _startWorldHandler;

        private readonly SparseArray64<int> _relationsMatrix = new SparseArray64<int>();

        private EcsGroup _arcEntities;
        private ArcEntityInfo[] _arkEntityInfos; //N * (N - 1) / 2

        #region Properties
        public EcsWorld StartWorld => _startWorld;
        public EcsWorld EndWorld => _endWorld;
        public EcsArcWorld ArcWorld => _arcWorld;
        public EcsReadonlyGroup ArcEntities => _arcEntities.Readonly;
        public bool IsLoop => _startWorld == _endWorld;
        #endregion

        #region Constructors
        internal EcsArc(EcsWorld startWorld, EcsWorld endWorld, EcsArcWorld arcWorld)
        {
            _startWorld = startWorld;
            _endWorld = endWorld;
            _arcWorld = arcWorld;

            _arkEntityInfos = new ArcEntityInfo[arcWorld.Capacity];

            _startWorldHandler = new VertexWorldHandler(this, _startWorld);

            _startWorld.AddListener(worldEventListener: _startWorldHandler);
            _startWorld.AddListener(entityEventListener: _startWorldHandler);

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
            _relationsMatrix.Add(startEntityID, endEntityID, arcEntity);

            _arkEntityInfos[arcEntity] = new ArcEntityInfo(startEntityID, endEntityID);
            _arcEntities.Add(arcEntity);
            return arcEntity;
        }
        public void Del(int startEntityID, int endEntityID)
        {
            if (!_relationsMatrix.TryGetValue(startEntityID, endEntityID, out int arcEntity))
                throw new EcsRelationException();

            _relationsMatrix.Remove(startEntityID, endEntityID);
            _arcWorld.DelEntity(arcEntity);

            _arkEntityInfos[arcEntity] = ArcEntityInfo.Empty;
            _arcEntities.Remove(arcEntity);
        }
        #endregion

        #region Get/Has
        public bool Has(int startEntityID, int endEntityID)
        {
            return _relationsMatrix.Contains(startEntityID, endEntityID);
        }
        public int Get(int startEntityID, int endEntityID)
        {
            if (!_relationsMatrix.TryGetValue(startEntityID, endEntityID, out int arcEntityID))
                throw new EcsRelationException();
            return arcEntityID;
        }
        public bool TryGet(int startEntityID, int endEntityID, out int arcEntityID)
        {
            return _relationsMatrix.TryGetValue(startEntityID, endEntityID, out arcEntityID);
        }
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
        #endregion

        #region Callbacks
        void IEcsWorldEventListener.OnWorldResize(int newSize)
        {
            Array.Resize(ref _arkEntityInfos, newSize);
        }
        void IEcsWorldEventListener.OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer)
        {
            _arcWorld.ReleaseDelEntityBuffer(buffer.Length);
        }
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
        private class VertexWorldHandler : IEcsWorldEventListener, IEcsEntityEventListener
        {
            private readonly EcsArc _source;
            private readonly EcsWorld _world;

            public VertexWorldHandler(EcsArc source, EcsWorld world)
            {
                _source = source;
                _world = world;
            }

            #region Callbacks
            public void OnDelEntity(int entityID)
            {
            }
            public void OnNewEntity(int entityID)
            {
            }
            public void OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer)
            {
            }
            public void OnWorldDestroy()
            {
            }
            public void OnWorldResize(int newSize)
            {
            }
            #endregion
        }
        #endregion
    }
}