using DCFApixels.DragonECS.Graphs.Internal;
using System;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    //Graph
    //Graph world
    //Rel entity
    //Component
    public class EntityGraph
    {
        private readonly EcsWorld _world;
        private readonly EcsWorld _graphWorld;

        private readonly GraphWorldHandler _arcWorldHandler;
        private readonly WorldHandler _loopWorldHandler;

        private readonly SparseMatrix _matrix;
        private RelationInfo[] _relEntityInfos; //N * (N - 1) / 2

        private int _count;

        private bool _isInit = false;
        private bool _isDestroyed = false;

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
        internal EntityGraph(EcsWorld world, EcsWorld graphWorld)
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

        #region New
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
        public int GetOrNewInverseRelation(int relEntityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            if (relEntityID <= 0 || relEntityID >= _relEntityInfos.Length) { Throw.UndefinedException(); }
#endif
            var info = _relEntityInfos[relEntityID];
            return GetOrNewRelation(info.end, info.start);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int NewRelationInternal(int startEntityID, int endEntityID)
        {
            int relEntityID = _graphWorld.NewEntity();
            _matrix.Add(startEntityID, endEntityID, relEntityID);
            _relEntityInfos[relEntityID] = new RelationInfo(startEntityID, endEntityID);
            _count++;
            return relEntityID;
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
            return relEntityID > 0 &&
                relEntityID < _relEntityInfos.Length &&
                _relEntityInfos[relEntityID].IsNull == false;
        }
        #endregion

        #region Get
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetRelation(int startEntityID, int endEntityID, out int relEntityID)
        {
            return _matrix.TryGetValue(startEntityID, endEntityID, out relEntityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetInverseRelation(int relEntityID, out int inverseRelEntityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            if (relEntityID <= 0 || relEntityID >= _relEntityInfos.Length) { Throw.UndefinedException(); }
#endif
            var info = _relEntityInfos[relEntityID];
            return _matrix.TryGetValue(info.end, info.start, out inverseRelEntityID);
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

        #region GetRelInfo
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StartEnd GetRelationStartEnd(int relEntityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            if (relEntityID <= 0 || relEntityID >= _relEntityInfos.Length) { Throw.UndefinedException(); }
#endif
            return new StartEnd(_relEntityInfos[relEntityID]);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetRelationStart(int relEntityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            if (relEntityID <= 0 || relEntityID >= _relEntityInfos.Length) { Throw.UndefinedException(); }
#endif
            return _relEntityInfos[relEntityID].start;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetRelationEnd(int relEntityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            if (relEntityID <= 0 || relEntityID >= _relEntityInfos.Length) { Throw.UndefinedException(); }
#endif
            return _relEntityInfos[relEntityID].end;
        }
        #endregion

        #region Other
        public static implicit operator EntityGraph(SingletonMarker marker) { return marker.Builder.World.GetGraph(); }
        #endregion

        #region WorldHandlers
        private class GraphWorldHandler : IEcsWorldEventListener
        {
            private readonly EntityGraph _arc;
            public GraphWorldHandler(EntityGraph arc)
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
        private class WorldHandler : IEcsWorldEventListener
        {
            private readonly EntityGraph _graph;
            public WorldHandler(EntityGraph arc)
            {
                _graph = arc;
                _graph.World.AddListener(this);
                IntHash.InitFor(_graph.World.Capacity);
            }
            public void Destroy()
            {
                _graph.World.RemoveListener(this);
            }
            #region Callbacks
            public void OnReleaseDelEntityBuffer(ReadOnlySpan<int> delEntities)
            {
                SubGraphMap subGraph;
                EcsWorld graphWorld = _graph._graphWorld;

                subGraph = graphWorld.JoinGraph(JoinMode.All);
                foreach (var sourceE in delEntities)
                {
                    var relEs = subGraph.GetRelations(sourceE);
                    foreach (var relE in relEs)
                    {
                        //int missingE = graphWorld.NewEntity();
                        _graph.DelRelation(relE);
                    }
                }

                graphWorld.ReleaseDelEntityBufferAll();
            }
            public void OnWorldDestroy() { }
            public void OnWorldResize(int startWorldNewSize)
            {
                IntHash.InitFor(startWorldNewSize);
            }
            #endregion
        }
        #endregion
    }
}