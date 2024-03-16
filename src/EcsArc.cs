using DCFApixels.DragonECS.Graphs.Internal;
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
        private readonly LoopWorldHandler _loopWorldHandler;

        private RelationInfo[] _relEntityInfos; //N * (N - 1) / 2
        private readonly SparseMatrix _matrix;

        private bool _isLoop;
        private bool _isInit = false;

        #region Properties
        internal bool IsInit_Internal
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _isInit; }
        }
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
        public bool IsLoop
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _isLoop; }
        }
        #endregion

        #region Constructors/Destroy
        internal EcsArc(EcsWorld startWorld, EcsWorld endWorld, EcsArcWorld arcWorld)
        {
            _startWorld = startWorld;
            _endWorld = endWorld;
            _arcWorld = arcWorld;

            _isLoop = startWorld == endWorld;

            _relEntityInfos = new RelationInfo[arcWorld.Capacity];
            _matrix = new SparseMatrix(arcWorld.Capacity);

            _arcWorldHandler = new ArcWorldHandler(this);
            if (_isLoop)
            {
                _loopWorldHandler = new LoopWorldHandler(this);
            }
            else
            {
                _startWorldHandler = new StartWorldHandler(this);
                _endWorldHandler = new EndWorldHandler(this);
            }

            _isInit = true;
        }
        public void Destroy()
        {
            _arcWorldHandler.Destroy();
            if (_isLoop)
            {
                _loopWorldHandler.Destroy();
            }
            else
            {
                _startWorldHandler.Destroy();
                _endWorldHandler.Destroy();
            }

        }
        #endregion

        #region New
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int NewRelation(int startEntityID, int endEntityID)
        {
            return NewRelationInternal(startEntityID, endEntityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetOrNewRelation(int startEntityID, int endEntityID)
        {
            if (_matrix.TryGetValue(startEntityID, endEntityID, out int relEntityID))
            {
                return relEntityID;
            }
            return NewRelationInternal(startEntityID, endEntityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int NewRelationInternal(int startEntityID, int endEntityID)
        {
            int relEntityID = _arcWorld.NewEntity();
            _matrix.Add(startEntityID, endEntityID, relEntityID);
            _relEntityInfos[relEntityID] = new RelationInfo(startEntityID, endEntityID);
            return relEntityID;
        }
        #endregion

        #region Has
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasRelation(int startEntityID, int endEntityID)
        {
            return _matrix.HasKey(startEntityID, endEntityID);
        }
        #endregion

        #region Get
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetRelation(int startEntityID, int endEntityID)
        {
            return _matrix.GetValue(startEntityID, endEntityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetRelation(int startEntityID, int endEntityID, out int relEntityID)
        {
            return _matrix.TryGetValue(startEntityID, endEntityID, out relEntityID);
        }
        #endregion

        #region Del
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DelRelation(int relEntityID)
        {
            _arcWorld.DelEntity(relEntityID);
            ClearRelation_Internal(relEntityID);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearRelation_Internal(int relEntityID)
        {
            ref RelationInfo info = ref _relEntityInfos[relEntityID];
            _matrix.TryDel(info.start, info.end);
            info = RelationInfo.Empty;
        }
        #endregion

        #region ArcEntityInfo
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsRelation(int relEntityID)
        {
            if (relEntityID <= 0 || relEntityID >= _relEntityInfos.Length)
            {
                return false;
            }
            return !_relEntityInfos[relEntityID].IsNull;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StartEnd GetRelationInfo(int relEntityID)
        {
            if (relEntityID <= 0 || relEntityID >= _relEntityInfos.Length)
            {
                Throw.UndefinedException();
            }
            return new StartEnd(_relEntityInfos[relEntityID]);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetRelStart(int relEntityID)
        {
            return GetRelationInfo(relEntityID).start;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetRelEnd(int relEntityID)
        {
            return GetRelationInfo(relEntityID).end;
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
        private class ArcWorldHandler : IEcsWorldEventListener
        {
            private readonly EcsArc _arc;
            public ArcWorldHandler(EcsArc arc)
            {
                _arc = arc;
                _arc.ArcWorld.AddListener(this);
            }
            public void Destroy()
            {
                _arc.ArcWorld.RemoveListener(this);
            }
            #region Callbacks
            public void OnReleaseDelEntityBuffer(ReadOnlySpan<int> relEntityBuffer)
            {
                foreach (var relEntityID in relEntityBuffer)
                {
                    _arc.ClearRelation_Internal(relEntityID);
                }
            }
            public void OnWorldDestroy() { }
            public void OnWorldResize(int arcWorldNewSize)
            {
                Array.Resize(ref _arc._relEntityInfos, arcWorldNewSize);
            }
            #endregion
        }
        private class StartWorldHandler : IEcsWorldEventListener
        {
            private readonly EcsArc _arc;
            public StartWorldHandler(EcsArc arc)
            {
                _arc = arc;
                _arc.StartWorld.AddListener(this);
                //OnWorldResize(_arc.StartWorld.Capacity);
            }
            public void Destroy()
            {
                _arc.StartWorld.RemoveListener(this);
            }
            #region Callbacks
            public void OnReleaseDelEntityBuffer(ReadOnlySpan<int> startEntityBuffer)
            {
                _arc._arcWorld.ReleaseDelEntityBufferAll();
            }
            public void OnWorldDestroy() { }
            public void OnWorldResize(int startWorldNewSize) { }
            #endregion
        }
        private class EndWorldHandler : IEcsWorldEventListener
        {
            private readonly EcsArc _arc;
            public EndWorldHandler(EcsArc arc)
            {
                _arc = arc;
                _arc.EndWorld.AddListener(this);
                //OnWorldResize(_arc.EndWorld.Capacity);
            }
            public void Destroy()
            {
                _arc.EndWorld.RemoveListener(this);
            }
            #region Callbacks
            public void OnReleaseDelEntityBuffer(ReadOnlySpan<int> endEntityBuffer)
            {
                _arc._arcWorld.ReleaseDelEntityBufferAll();
            }
            public void OnWorldDestroy() { }
            public void OnWorldResize(int endWorldNewSize) { }
            #endregion
        }
        private class LoopWorldHandler : IEcsWorldEventListener
        {
            private readonly EcsArc _arc;
            public LoopWorldHandler(EcsArc arc)
            {
                _arc = arc;
                _arc.StartWorld.AddListener(this);
                //OnWorldResize(_arc.StartWorld.Capacity);
            }
            public void Destroy()
            {
                _arc.StartWorld.RemoveListener(this);
            }
            #region Callbacks
            public void OnReleaseDelEntityBuffer(ReadOnlySpan<int> startEntityBuffer)
            {
                _arc._arcWorld.ReleaseDelEntityBufferAll();
            }
            public void OnWorldDestroy() { }
            public void OnWorldResize(int startWorldNewSize) { }
            #endregion
        }
        #endregion
    }
}