using DCFApixels.DragonECS.Relations.Internal;
using DCFApixels.DragonECS.Relations.Utils;
using System;
using System.Runtime.CompilerServices;
using System.Xml;

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

        //private readonly SparseArray64<int> _relationsMatrix = new SparseArray64<int>();

        private EcsGraph _entitiesGraph;
        private EcsGraph.FriendEcsArc _entitiesGraphFriend;

        private EcsGroup _relEntities;
        private RelEntityInfo[] _relEntityInfos; //N * (N - 1) / 2

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
        public EcsReadonlyGroup RelEntities
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _relEntities.Readonly; }
        }
        public EcsReadonlyGraph EntitiesGraph
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _entitiesGraph.Readonly; }
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

            _relEntityInfos = new RelEntityInfo[arcWorld.Capacity];


            _relEntities = EcsGroup.New(_arcWorld);
            _entitiesGraph = new EcsGraph(this);

            _entitiesGraphFriend = new EcsGraph.FriendEcsArc(this, _entitiesGraph);

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

        #region New/Del
        public int NewRelation(int startEntityID, int endEntityID)
        {
            //if (HasRelation(startEntityID, endEntityID))
            //{
            //    Throw.RelationAlreadyExists();
            //}

            int relEntity = _arcWorld.NewEntity();
            //_relationsMatrix.Add(startEntityID, endEntityID, relEntity);

            _relEntityInfos[relEntity] = new RelEntityInfo(startEntityID, endEntityID);
            _relEntities.Add(relEntity);
            _entitiesGraph.Add(relEntity);
            return relEntity;
        }
        //public void DelRelation(int startEntityID, int endEntityID)
        //{
        //    if (_relationsMatrix.TryGetValue(startEntityID, endEntityID, out int relEntityID))
        //    {
        //        _arcWorld.DelEntity(relEntityID);
        //    }
        //    else
        //    {
        //        Throw.UndefinedRelationException();
        //    }
        //}

        public void DelRelation(int relEntityID)
        {
            _arcWorld.DelEntity(relEntityID);
        }

        public void ClearRelation_Internal(int relEntityID)
        {
            _relEntities.Remove(relEntityID);
            _entitiesGraph.Del(relEntityID);
            _relEntityInfos[relEntityID] = RelEntityInfo.Empty;
        }

        //private void ClearRelation_Internal(int startEntityID, int endEntityID)
        //{
        //    if (_relationsMatrix.TryGetValue(startEntityID, endEntityID, out int relEntityID))
        //    {
        //        _relEntities.Remove(relEntityID);
        //        _entitiesGraph.Del(relEntityID);
        //        _relationsMatrix.Remove(startEntityID, endEntityID);
        //        _relEntityInfos[relEntityID] = RelEntityInfo.Empty;
        //    }
        //}
        #endregion

        #region GetRelation/HasRelation
        //public bool HasRelation(int startEntityID, int endEntityID)
        //{
        //    return _relationsMatrix.Contains(startEntityID, endEntityID);
        //}
        //public int GetRelation(int startEntityID, int endEntityID)
        //{
        //    if (!_relationsMatrix.TryGetValue(startEntityID, endEntityID, out int relEntityID))
        //    {
        //        Throw.UndefinedRelationException();
        //    }
        //    return relEntityID;
        //}
        //public bool TryGetRelation(int startEntityID, int endEntityID, out int relEntityID)
        //{
        //    return _relationsMatrix.TryGetValue(startEntityID, endEntityID, out relEntityID);
        //}
        #endregion

        #region ArcEntityInfo
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsRelation(int relEntityID)
        {
            if (relEntityID <= 0 || relEntityID >= _relEntityInfos.Length)
            {
                return false;
            }
            return !_relEntityInfos[relEntityID].IsEmpty;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RelEntityInfo GetRelationInfo(int relEntityID)
        {
            if (relEntityID <= 0 || relEntityID >= _relEntityInfos.Length)
            {
                Throw.UndefinedException();
            }
            return _relEntityInfos[relEntityID];
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

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public int GetInversedRelation(int relEntityID)
        //{
        //    var (startEntityID, endEntityID) = GetRelationInfo(relEntityID);
        //    return GetRelation(endEntityID, startEntityID);
        //}
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public bool TryGetInversedRelation(int relEntityID, out int inversedRelEntityID)
        //{
        //    var (startEntityID, endEntityID) = GetRelationInfo(relEntityID);
        //    return TryGetRelation(endEntityID, startEntityID, out inversedRelEntityID);
        //}
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
                    //var (startEntityID, endEntityID) = _arc._relEntityInfos[relEntityID];
                    //_arc.ClearRelation_Internal(startEntityID, endEntityID);
                    _arc.ClearRelation_Internal(relEntityID);
                }
            }
            public void OnWorldDestroy() { }
            public void OnWorldResize(int arcWorldNewSize)
            {
                Array.Resize(ref _arc._relEntityInfos, arcWorldNewSize);
                _arc._entitiesGraph.UpArcSize(arcWorldNewSize);
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
                OnWorldResize(_arc.StartWorld.Capacity);
            }
            public void Destroy()
            {
                _arc.StartWorld.RemoveListener(this);
            }
            #region Callbacks
            public void OnReleaseDelEntityBuffer(ReadOnlySpan<int> startEntityBuffer)
            {
                foreach (var startEntityID in startEntityBuffer)
                {
                    _arc._entitiesGraphFriend.DelStartAndDelRelEntities(startEntityID, _arc);
                }
                _arc._arcWorld.ReleaseDelEntityBufferAll();
            }
            public void OnWorldDestroy() { }
            public void OnWorldResize(int startWorldNewSize)
            {
                _arc._entitiesGraph.UpStartSize(startWorldNewSize);
            }
            #endregion
        }
        private class EndWorldHandler : IEcsWorldEventListener
        {
            private readonly EcsArc _arc;
            public EndWorldHandler(EcsArc arc)
            {
                _arc = arc;
                _arc.EndWorld.AddListener(this);
                OnWorldResize(_arc.EndWorld.Capacity);
            }
            public void Destroy()
            {
                _arc.EndWorld.RemoveListener(this);
            }
            #region Callbacks
            public void OnReleaseDelEntityBuffer(ReadOnlySpan<int> endEntityBuffer)
            {
                foreach (var endEntityID in endEntityBuffer)
                {
                    _arc._entitiesGraphFriend.DelEndAndDelRelEntities(endEntityID, _arc);
                }
                _arc._arcWorld.ReleaseDelEntityBufferAll();
            }
            public void OnWorldDestroy() { }
            public void OnWorldResize(int endWorldNewSize)
            {
                _arc._entitiesGraph.UpEndSize(endWorldNewSize);
            }
            #endregion
        }
        private class LoopWorldHandler : IEcsWorldEventListener
        {
            private readonly EcsArc _arc;
            public LoopWorldHandler(EcsArc arc)
            {
                _arc = arc;
                _arc.StartWorld.AddListener(this);
                OnWorldResize(_arc.StartWorld.Capacity);
            }
            public void Destroy()
            {
                _arc.StartWorld.RemoveListener(this);
            }
            #region Callbacks
            public void OnReleaseDelEntityBuffer(ReadOnlySpan<int> startEntityBuffer)
            {
                foreach (var startEntityID in startEntityBuffer)
                {
                    _arc._entitiesGraphFriend.DelStartAndDelRelEntities(startEntityID, _arc);
                    _arc._entitiesGraphFriend.DelEndAndDelRelEntities(startEntityID, _arc);
                }
                _arc._arcWorld.ReleaseDelEntityBufferAll();
            }
            public void OnWorldDestroy() { }
            public void OnWorldResize(int startWorldNewSize)
            {
                _arc._entitiesGraph.UpStartSize(startWorldNewSize);
                _arc._entitiesGraph.UpEndSize(startWorldNewSize);
            }
            #endregion
        }
        #endregion
    }
}