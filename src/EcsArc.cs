using DCFApixels.DragonECS.Relations.Utils;
using Leopotam.EcsLite;
using System;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    //Arc
    //Arc world
    //Rel entity
    //Component
    public class EcsArc
    {
        private readonly EcsWorld _startWorld;
        private readonly EcsWorld _endWorld;
        private readonly EcsArcWorld _arcWorld;

        private readonly StartWorldHandler _startWorldHandler;
        private readonly ArcWorldHandler _arcWorldHandler;
        private readonly EndWorldHandler _endWorldHandler;

        private readonly SparseArray64<int> _relationsMatrix = new SparseArray64<int>();

        private EcsJoin _joinEntities;
        private EcsGroup _relEntities;
        private RelEntityInfo[] _relEntityInfos; //N * (N - 1) / 2

        private bool _isLoop;

        #region Properties
        public EcsWorld StartWorld
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _startWorld; }
        }
        public EcsWorld EndWorld
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _endWorld; }
        }
        public EcsArcWorld ArcWorld
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _arcWorld; }
        }
        public int ArcWorldID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _arcWorld.id; }
        }
        public EcsReadonlyGroup RelEntities
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _relEntities.Readonly; }
        }
        public EcsReadonlyJoin JoinEntities
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _joinEntities.Readonly; }
        }
        public bool IsLoop
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _isLoop; }
        }
        #endregion

        #region Constructors
        internal EcsArc(EcsWorld startWorld, EcsWorld endWorld, EcsArcWorld arcWorld)
        {
            _startWorld = startWorld;
            _endWorld = endWorld;
            _arcWorld = arcWorld;

            _isLoop = startWorld == endWorld;

            _relEntityInfos = new RelEntityInfo[arcWorld.Capacity];

            _startWorldHandler = new StartWorldHandler(this);
            _arcWorldHandler = new ArcWorldHandler(this);
            if (!_isLoop)
            {
                _endWorldHandler = new EndWorldHandler(this);
            }

            _relEntities = EcsGroup.New(_arcWorld);
            _joinEntities = new EcsJoin(this);
        }
        #endregion

        #region New/Del
        public int NewRelation(int startEntityID, int endEntityID)
        {
            if (Has(startEntityID, endEntityID))
            {
                throw new EcsRelationException();
            }

            int relEntity = _arcWorld.NewEntity();
            _relationsMatrix.Add(startEntityID, endEntityID, relEntity);

            _relEntityInfos[relEntity] = new RelEntityInfo(startEntityID, endEntityID);
            _relEntities.Add(relEntity);
            _joinEntities.Add(relEntity);
            return relEntity;
        }
        public void DelRelation(int startEntityID, int endEntityID)
        {
            if (!_relationsMatrix.TryGetValue(startEntityID, endEntityID, out int relEntity))
            {
                throw new EcsRelationException();
            }
            _joinEntities.Del(relEntity);

            _relationsMatrix.Remove(startEntityID, endEntityID);
            _arcWorld.DelEntity(relEntity);

            _relEntityInfos[relEntity] = RelEntityInfo.Empty;
            _relEntities.Remove(relEntity);
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
        public bool IsRel(int arcEntityID)
        {
            if (arcEntityID <= 0 || arcEntityID >= _relEntityInfos.Length)
                return false;
            return !_relEntityInfos[arcEntityID].IsEmpty;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RelEntityInfo GetRelInfo(int arcEntityID)
        {
            if (arcEntityID <= 0 || arcEntityID >= _relEntityInfos.Length)
                throw new Exception();
            return _relEntityInfos[arcEntityID];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetRelStart(int arcEntityID)
        {
            return GetRelInfo(arcEntityID).start;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetRelEnd(int arcEntityID)
        {
            return GetRelInfo(arcEntityID).end;
        }
        #endregion

        #region Other
        public EcsArc GetInversetArc()
        {
            return _endWorld.GetArc(_startWorld);
        }
        public bool TryGetInversetArc(out EcsArc arc)
        {
            return _endWorld.TryGetArc(_startWorld, out arc);
        }
        #endregion


        #region VertexWorldHandler
        private class ArcWorldHandler : IEcsWorldEventListener, IEcsEntityEventListener
        {
            private readonly EcsArc _arc;
            public ArcWorldHandler(EcsArc arc)
            {
                _arc = arc;
                EcsArcWorld arcWorld = arc.ArcWorld;
                arcWorld.AddListener(worldEventListener: this);
                arcWorld.AddListener(entityEventListener: this);
            }

            #region Callbacks
            public void OnDelEntity(int entityID) { }
            public void OnNewEntity(int entityID) { }
            public void OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer)
            {
                foreach (var relEntityID in buffer)
                {
                    ref RelEntityInfo rel = ref _arc._relEntityInfos[relEntityID];
                    if (_arc._relationsMatrix.Contains(rel.start, rel.end))
                    {
                        _arc.DelRelation(rel.start, rel.end);
                    }
                }
                _arc._arcWorld.ReleaseDelEntityBuffer(buffer.Length);
            }
            public void OnWorldDestroy() { }
            public void OnWorldResize(int newSize)
            {
                Array.Resize(ref _arc._relEntityInfos, newSize);
            }
            #endregion
        }
        private class StartWorldHandler : IEcsWorldEventListener, IEcsEntityEventListener
        {
            private readonly EcsArc _arc;
            public StartWorldHandler(EcsArc arc)
            {
                _arc = arc;
                EcsWorld startWorld = arc.StartWorld;
                startWorld.AddListener(worldEventListener: this);
                startWorld.AddListener(entityEventListener: this);
            }

            #region Callbacks
            public void OnDelEntity(int startEntityID) { }
            public void OnNewEntity(int startEntityID) { }
            public void OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer)
            {
                foreach (var startEntityID in buffer)
                {
                    _arc._joinEntities.DelStart(startEntityID);
                }
            }
            public void OnWorldDestroy() { }
            public void OnWorldResize(int newSize) { }
            #endregion
        }
        private class EndWorldHandler : IEcsWorldEventListener, IEcsEntityEventListener
        {
            private readonly EcsArc _arc;
            public EndWorldHandler(EcsArc arc)
            {
                _arc = arc;
                EcsWorld endWorld = arc.EndWorld;
                endWorld.AddListener(worldEventListener: this);
                endWorld.AddListener(entityEventListener: this);
            }

            #region Callbacks
            public void OnDelEntity(int endEntityID) { }
            public void OnNewEntity(int endEntityID) { }
            public void OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer)
            {
                foreach (var endEntityID in buffer)
                {
                    _arc._joinEntities.DelEnd(endEntityID);
                }
            }
            public void OnWorldDestroy() { }
            public void OnWorldResize(int newSize) { }
            #endregion
        }
        #endregion
    }
}