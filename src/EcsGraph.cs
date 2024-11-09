using DCFApixels.DragonECS.Graphs.Internal;
using System;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    //Graph
    //Graph world
    //Rel entity
    //Component
    public class EcsGraph
    {
        private readonly EcsWorld _world;
        private readonly EcsWorld _graphWorld;

        private readonly GraphWorldHandler _arcWorldHandler;
        private readonly WorldHandler _loopWorldHandler;

        private RelationInfo[] _relEntityInfos; //N * (N - 1) / 2
        private readonly SparseMatrix _matrix;

        private bool _isInit = false;

        private int _count;

        #region Properties
        internal bool IsInit_Internal
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _isInit; }
        }
        public EcsWorld World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _world; }
        }
        public short WorldID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _world.ID; }
        }
        public EcsWorld GraphWorld
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _graphWorld; }
        }
        public short GraphWorldID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _graphWorld.ID; }
        }
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _count; }
        }
        #endregion

        #region Constructors/Destroy
        internal EcsGraph(EcsWorld world, EcsWorld graphWorld)
        {
            _world = world;
            _graphWorld = graphWorld;

            _relEntityInfos = new RelationInfo[_graphWorld.Capacity];
            _matrix = new SparseMatrix(_graphWorld.Capacity);

            _arcWorldHandler = new GraphWorldHandler(this);
            _loopWorldHandler = new WorldHandler(this);

            _isInit = true;
        }
        public void Destroy()
        {
            _arcWorldHandler.Destroy();
            _loopWorldHandler.Destroy();
        }
        #endregion

        #region New/Convert
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
            int relEntityID = _graphWorld.NewEntity();
            ConvertToRelationInternal(relEntityID, startEntityID, endEntityID);
            return relEntityID;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ConvertToRelation(int entityID, int startEntityID, int endEntityID)
        {
            if (IsRelation(entityID))
            {
                Throw.UndefinedException();
            }
            ConvertToRelationInternal(entityID, startEntityID, endEntityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ConvertToRelationInternal(int relEntityID, int startEntityID, int endEntityID)
        {
            _matrix.Add(startEntityID, endEntityID, relEntityID);
            _relEntityInfos[relEntityID] = new RelationInfo(startEntityID, endEntityID);
            _count++;
        }
        #endregion

        #region Inverse
        public int GetInverseRelation(int relEntityID)
        {
            if (relEntityID <= 0 || relEntityID >= _relEntityInfos.Length)
            {
                Throw.UndefinedException();
            }
            var x = _relEntityInfos[relEntityID];
            return GetOrNewRelation(x.end, x.start);
        }
        #endregion

        #region Has/Is
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasRelation(int startEntityID, int endEntityID)
        {
            return _matrix.HasKey(startEntityID, endEntityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsRelation(int relEntityID)
        {
            if (relEntityID <= 0 || relEntityID >= _relEntityInfos.Length)
            {
                return false;
            }
            return !_relEntityInfos[relEntityID].IsNull;
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
            _graphWorld.TryDelEntity(relEntityID);
            //ClearRelation_Internal(relEntityID);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ClearRelation_Internal(int relEntityID)
        {
            ref RelationInfo info = ref _relEntityInfos[relEntityID];
            if (_matrix.TryDel(info.start, info.end))
            {
                _count--;
                info = RelationInfo.Empty;
            }
        }
        #endregion

        #region ArcEntityInfo

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StartEnd GetRelationStartEnd(int relEntityID)
        {
            if (relEntityID <= 0 || relEntityID >= _relEntityInfos.Length)
            {
                Throw.UndefinedException();
            }
            return new StartEnd(_relEntityInfos[relEntityID]);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetRelationStart(int relEntityID)
        {
            if (relEntityID <= 0 || relEntityID >= _relEntityInfos.Length)
            {
                Throw.UndefinedException();
            }
            return _relEntityInfos[relEntityID].start;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetRelationEnd(int relEntityID)
        {
            if (relEntityID <= 0 || relEntityID >= _relEntityInfos.Length)
            {
                Throw.UndefinedException();
            }
            return _relEntityInfos[relEntityID].end;
        }
        #endregion

        #region GraphWorldHandler
        private class GraphWorldHandler : IEcsWorldEventListener
        {
            private readonly EcsGraph _arc;
            public GraphWorldHandler(EcsGraph arc)
            {
                _arc = arc;
                _arc.GraphWorld.AddListener(this);
            }
            public void Destroy()
            {
                _arc.GraphWorld.RemoveListener(this);
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
        #endregion

        #region WorldHandler
        private class WorldHandler : IEcsWorldEventListener
        {
            private readonly EcsGraph _arc;
            public WorldHandler(EcsGraph arc)
            {
                _arc = arc;
                _arc.World.AddListener(this);
            }
            public void Destroy()
            {
                _arc.World.RemoveListener(this);
            }
            #region Callbacks
            public void OnReleaseDelEntityBuffer(ReadOnlySpan<int> startEntityBuffer)
            {
                var graph = _arc.GraphWorld.JoinToSubGraph(EcsSubGraphMode.All);
                foreach (var e in startEntityBuffer)
                {
                    var span = graph.GetNodes(e);
                    foreach (var relE in span)
                    {
                        _arc.DelRelation(relE);
                    }
                }
                _arc._graphWorld.ReleaseDelEntityBufferAll();
            }
            public void OnWorldDestroy() { }
            public void OnWorldResize(int startWorldNewSize) { }
            #endregion
        }
        #endregion

        #region Other
        public static implicit operator EcsGraph(SingletonMarker marker) { return marker.Builder.World.GetGraph(); }
        #endregion
    }
}