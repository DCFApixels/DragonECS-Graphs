using DCFApixels.DragonECS.Relations.Internal;
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
        private EcsJoin.FriendEcsArc _joinEntitiesFriend;

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

        #region Constructors/Destroy
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

            _joinEntitiesFriend = new EcsJoin.FriendEcsArc(this, _joinEntities);
            _isInit = true;
        }
        public void Destroy()
        {
            _startWorldHandler.Destroy();
            _arcWorldHandler.Destroy(); 
            if (!_isLoop)
            {
              _endWorldHandler.Destroy();
            }
        }
        #endregion

        #region New/Del
        public int NewRelation(int startEntityID, int endEntityID)
        {
            if (HasRelation(startEntityID, endEntityID))
            {
                Throw.UndefinedRelationException();
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
            if (_relationsMatrix.TryGetValue(startEntityID, endEntityID, out int relEntity))
            {
                _arcWorld.DelEntity(relEntity);
            }
            else
            {
                Throw.UndefinedRelationException();
            }
            //if (!_relationsMatrix.TryGetValue(startEntityID, endEntityID, out int relEntity))
            //{
            //    Throw.UndefinedRelationException();
            //}
            //_joinEntities.Del(relEntity);
            //
            //_relationsMatrix.Remove(startEntityID, endEntityID);
            //_arcWorld.DelEntity(relEntity);
            //
            //_relEntityInfos[relEntity] = RelEntityInfo.Empty;
            //_relEntities.Remove(relEntity);
        }

        private void ClearRelation_Internal(int startEntityID, int endEntityID)
        {
            if (_relationsMatrix.TryGetValue(startEntityID, endEntityID, out int relEntity))
            {
                _relEntities.Remove(relEntity);
                _joinEntities.Del(relEntity);
                _relationsMatrix.Remove(startEntityID, endEntityID);
                _relEntityInfos[relEntity] = RelEntityInfo.Empty;
            }
        }
        #endregion

        #region GetRelation/HasRelation
        public bool HasRelation(int startEntityID, int endEntityID)
        {
            return _relationsMatrix.Contains(startEntityID, endEntityID);
        }
        public int GetRelation(int startEntityID, int endEntityID)
        {
            if (!_relationsMatrix.TryGetValue(startEntityID, endEntityID, out int relEntityID))
            {
                Throw.UndefinedRelationException();
            }
            return relEntityID;
        }
        public bool TryGetRelation(int startEntityID, int endEntityID, out int relEntityID)
        {
            return _relationsMatrix.TryGetValue(startEntityID, endEntityID, out relEntityID);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetInversedRelation(int relEntityID)
        {
            var (startEntityID, endEntityID) = GetRelationInfo(relEntityID);
            return GetRelation(endEntityID, startEntityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetInversedRelation(int relEntityID, out int inversedRelEntityID)
        {
            var (startEntityID, endEntityID) = GetRelationInfo(relEntityID);
            return TryGetRelation(endEntityID, startEntityID, out inversedRelEntityID);
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
                    var (startEntityID, endEntityID) = _arc._relEntityInfos[relEntityID];
                    _arc.ClearRelation_Internal(startEntityID, endEntityID);
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
                    //_arc._joinEntities.DelStart(startEntityID);
                    _arc._joinEntitiesFriend.DelStartAndDelRelEntities(startEntityID, _arc);
                }
                _arc._arcWorld.ReleaseDelEntityBuffer(startEntityBuffer.Length);
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
                    //_arc._joinEntities.DelEnd(endEntityID);
                    _arc._joinEntitiesFriend.DelEndAndDelRelEntities(endEntityID, _arc);
                }
                _arc._arcWorld.ReleaseDelEntityBuffer(endEntityBuffer.Length);
            }
            public void OnWorldDestroy() { }
            public void OnWorldResize(int endWorldNewSize) { }
            #endregion
        }
        #endregion
    }
}