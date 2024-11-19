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

        #region MoveRelation
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private void MoveRelation(int relEntityID, int newStartEntityID, int newEndEntityID)
        //{
        //    var startEnd = GetRelationStartEnd(relEntityID);
        //
        //    //Тут будет не стабильное состояние если TryDel пройдет, а TryAdd - нет
        //    if (_matrix.TryDel(startEnd.start, startEnd.end) == false ||
        //        _matrix.TryAdd(newStartEntityID, newEndEntityID, relEntityID) == false)
        //    {
        //        Throw.UndefinedException();
        //    }
        //
        //    _relEntityInfos[relEntityID] = new RelationInfo(newStartEntityID, newEndEntityID);
        //}
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

        #region Other
        public static implicit operator EntityGraph(SingletonMarker marker) { return marker.Builder.World.GetGraph(); }
        #endregion

        #region RelEntityInfo

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
        #endregion

        #region WorldHandler
        private class WorldHandler : IEcsWorldEventListener
        {
            private readonly EntityGraph _graph;
            public WorldHandler(EntityGraph arc)
            {
                _graph = arc;
                _graph.World.AddListener(this);
            }
            public void Destroy()
            {
                _graph.World.RemoveListener(this);
            }
            #region Callbacks
            public void OnReleaseDelEntityBuffer(ReadOnlySpan<int> delEntities)
            {
                EcsSubGraph subGraph;
                EcsWorld graphWorld = _graph._graphWorld;

                subGraph = graphWorld.JoinToSubGraph(EcsSubGraphMode.All);
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
                IntHashes.InitFor(startWorldNewSize);
            }
            #endregion
        }
        #endregion
    }
}