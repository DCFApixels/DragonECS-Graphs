using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Graphs.Internal;
using DCFApixels.DragonECS.UncheckedCore;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    using LinkedList = OnlyAppendHeadLinkedList;
    public sealed class EcsJoinToSubGraphExecutor : MaskQueryExecutor, IEcsWorldEventListener
    {
        private EntityGraph _graph;
        private EcsMaskIterator _iterator;

        private int[] _filteredAllEntities = new int[32];
        private int _filteredAllEntitiesCount = 0;
        private int[] _filteredEntities = null;
        private int _filteredEntitiesCount = 0;

        private int[] _currentFilteredEntities = null;
        private int _currentFilteredEntitiesCount = 0;

        private long _version;
        private WorldStateVersionsChecker _versionsChecker;

        private LinkedList _linkedList;
        private LinkedListHead[] _linkedListSourceHeads;

        private int[] _sourceEntities;
        private int _sourceEntitiesCount;

        private int _targetWorldCapacity = -1;
        private EcsProfilerMarker _executeMarker = new EcsProfilerMarker("Join");

        public bool _isDestroyed = false;


        #region Properties
        public sealed override long Version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _version; }
        }
        public EntityGraph Graph
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _graph; }
        }
        public sealed override bool IsCached
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _versionsChecker.Check(); }
        }
        public sealed override int LastCachedCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _filteredAllEntitiesCount; }
        }
        #endregion

        #region OnInitialize/OnDestroy
        protected override void OnInitialize()
        {
            _versionsChecker = new WorldStateVersionsChecker(Mask);
            _linkedList = new OnlyAppendHeadLinkedList(World.Capacity);
            _linkedListSourceHeads = new LinkedListHead[World.Capacity];
            _sourceEntities = new int[World.Capacity * 2];
            World.AddListener(this);
            _graph = World.GetGraph();
            _iterator = Mask.GetIterator();
        }
        protected override void OnDestroy()
        {
            if (_isDestroyed) { return; }
            _isDestroyed = true;
            World.RemoveListener(this);
            _versionsChecker.Dispose();
        }
        #endregion

        #region Execute
        private EcsSubGraph Execute_Internal(EcsSubGraphMode mode)
        {
            _executeMarker.Begin();

            World.ReleaseDelEntityBufferAllAuto();

            if (Mask.IsEmpty || _versionsChecker.CheckAndNext() == false)
            {
                _filteredAllEntitiesCount = _iterator.IterateTo(World.Entities, ref _filteredAllEntities);
                //Подготовка массивов
                if (_sourceEntities.Length < _filteredAllEntitiesCount * 2)
                {
                    _sourceEntities = new int[_filteredAllEntitiesCount * 2];
                }
            }

            //установка текущего массива
            _currentFilteredEntities = _filteredAllEntities;
            _currentFilteredEntitiesCount = _filteredAllEntitiesCount;

            //Подготовка массивов
            if (_targetWorldCapacity < World.Capacity)
            {
                _targetWorldCapacity = World.Capacity;
                _linkedListSourceHeads = new LinkedListHead[_targetWorldCapacity];
                //_startEntities = new int[_targetWorldCapacity];
            }
            else
            {
                //ArrayUtility.Fill(_linkedListSourceHeads, default); //TODO оптимизировать, сделав не полную отчистку а только по элементов с прошлого раза
                for (int i = 0; i < _sourceEntitiesCount; i++)
                {
                    _linkedListSourceHeads[_sourceEntities[i]] = default;
                }
            }
            _sourceEntitiesCount = 0;
            _linkedList.Clear();

            //Заполнение массивов
            if ((mode & EcsSubGraphMode.StartToEnd) != 0)
            {
                for (int i = 0; i < _filteredAllEntitiesCount; i++)
                {
                    AddStart(_filteredAllEntities[i]);
                }
            }
            if ((mode & EcsSubGraphMode.EndToStart) != 0)
            {
                for (int i = 0; i < _filteredAllEntitiesCount; i++)
                {
                    AddEnd(_filteredAllEntities[i]);
                }
            }

            _version++;

            _executeMarker.End();
            return new EcsSubGraph(this);
        }
        private EcsSubGraph ExecuteFor_Internal(EcsSpan span, EcsSubGraphMode mode)
        {
            _executeMarker.Begin();
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (span.IsNull) { _executeMarker.End(); Throw.ArgumentNull(nameof(span)); }
            if (span.WorldID != World.ID) { _executeMarker.End(); Throw.Quiery_ArgumentDifferentWorldsException(); }
#endif
            if (_filteredEntities == null)
            {
                _filteredEntities = new int[32];
            }
            _filteredEntitiesCount = _iterator.IterateTo(span, ref _filteredEntities);

            throw new NotImplementedException();

            _executeMarker.End();
        }


        public EcsSubGraph Execute(EcsSubGraphMode mode = EcsSubGraphMode.StartToEnd)
        {
            return Execute_Internal(mode);
        }
        public EcsSubGraph ExecuteFor(EcsSpan span, EcsSubGraphMode mode = EcsSubGraphMode.StartToEnd)
        {
            return ExecuteFor_Internal(span, mode);
        }

        #region TMP
        //        public EcsSubGraph Execute(EcsSubGraphMode mode = EcsSubGraphMode.StartToEnd)
        //        {
        //            return ExecuteFor(World.Entities, mode);
        //        }
        //        public EcsSubGraph ExecuteFor(EcsSpan span, EcsSubGraphMode mode = EcsSubGraphMode.StartToEnd)
        //        {
        //            _executeMarker.Begin();
        //#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
        //            if (span.IsNull) { Throw.ArgumentException(""); }//TODO составить текст исключения. 
        //            else if (World != span.World) { Throw.ArgumentException(""); } //TODO составить текст исключения. это проверка на то что пользователь использует правильный мир
        //#endif
        //
        //            //if (_lastWorldVersion != World.Version)
        //            {
        //                //Подготовка массивов
        //                if (_targetWorldCapacity < World.Capacity)
        //                {
        //                    _targetWorldCapacity = World.Capacity;
        //                    _baskets = new Basket[_targetWorldCapacity];
        //                    _startEntities = new int[_targetWorldCapacity];
        //                }
        //                else
        //                {
        //                    ArrayUtility.Fill(_baskets, default);
        //                }
        //                _startEntitiesCount = 0;
        //                _linkedList.Clear();
        //                //Конец подготовки массивов
        //
        //                if ((mode & EcsSubGraphMode.StartToEnd) != 0)
        //                {
        //                    if (Mask.IsEmpty)
        //                    {
        //                        foreach (var relationEntityID in span) { AddStart(relationEntityID); }
        //                    }
        //                    else
        //                    {
        //                        foreach (var relationEntityID in _iterator.Iterate(span)) { AddStart(relationEntityID); }
        //                    }
        //                }
        //                if ((mode & EcsSubGraphMode.EndToStart) != 0)
        //                {
        //                    if (Mask.IsEmpty)
        //                    {
        //                        foreach (var relationEntityID in span) { AddEnd(relationEntityID); }
        //                    }
        //                    else
        //                    {
        //                        foreach (var relationEntityID in _iterator.Iterate(span)) { AddEnd(relationEntityID); }
        //                    }
        //                }
        //
        //                _lastWorldVersion = World.Version;
        //            }
        //            //else
        //            //{
        //            //
        //            //}
        //
        //            _executeMarker.End();
        //            return new EcsSubGraph(this, UncheckedCoreUtility.CreateSpan(WorldID, _startEntities, _startEntitiesCount));
        //        }
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddStart(int relationEntityID)
        {
            AddSourceEntity(_graph.GetRelationStart(relationEntityID), relationEntityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddEnd(int relationEntityID)
        {
            AddSourceEntity(_graph.GetRelationEnd(relationEntityID), relationEntityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddSourceEntity(int sourceEntityID, int relationEntityID)
        {
            if (sourceEntityID == 0)
            {
                return;
            }
            ref var basket = ref _linkedListSourceHeads[sourceEntityID];
            if (basket.head == 0)
            {
                _sourceEntities[_sourceEntitiesCount++] = sourceEntityID;
                basket.head = _linkedList.NewHead(relationEntityID);
            }
            else
            {
                _linkedList.InsertIntoHead(ref basket.head, relationEntityID);
            }
            basket.count++;
        }
        #endregion

        #region Internal result methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EcsSubGraphSpan GetRelations_Internal(int sourceEntityID)
        {
            LinkedListHead basket = _linkedListSourceHeads[sourceEntityID];
            return new EcsSubGraphSpan(_linkedList._nodes, basket.head, basket.count);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetRelation_Internal(int sourceEntityID)
        {
            LinkedListHead basket = _linkedListSourceHeads[sourceEntityID];
            return basket.count > 0 ? _linkedList.Get(basket.head) : 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetRelationsCount_Internal(int sourceEntityID)
        {
            return _linkedListSourceHeads[sourceEntityID].count;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetCount_Internal()
        {
            return _linkedList.Count;
        }
        #endregion

        #region IEcsWorldEventListener
        void IEcsWorldEventListener.OnWorldResize(int newSize)
        {
            Array.Resize(ref _linkedListSourceHeads, newSize);
        }
        void IEcsWorldEventListener.OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer) { }
        void IEcsWorldEventListener.OnWorldDestroy() { }
        #endregion

        #region Basket
        private struct LinkedListHead
        {
            public LinkedList.NodeIndex head;
            public int count;
            public override string ToString()
            {
                return $"i:{head} count:{count}";
            }
        }
        #endregion

        #region GetEntites
        internal EcsSpan GetSourceEntities()
        {
            return UncheckedCoreUtility.CreateSpan(WorldID, _sourceEntities, _sourceEntitiesCount);
        }
        internal EcsSpan GetRelEntities()
        {
            return UncheckedCoreUtility.CreateSpan(WorldID, _currentFilteredEntities, _currentFilteredEntitiesCount);
        }
        #endregion
    }

    public enum EcsSubGraphMode : byte
    {
        NONE = 0,
        StartToEnd = 1 << 0,
        EndToStart = 1 << 1,
        All = StartToEnd | EndToStart,
    }

    #region EcsSubGraphSpan/EcsSubGraph
    public readonly ref struct EcsSubGraph
    {
        private readonly EcsJoinToSubGraphExecutor _executer;
        public EntityGraph Graph
        {
            get { return _executer.Graph; }
        }
        internal EcsSubGraph(EcsJoinToSubGraphExecutor executer)
        {
            _executer = executer;
        }
        public EcsSpan GetSourceEntities()
        {
            return _executer.GetSourceEntities();
        }
        public EcsSpan GetAllRelEntities()
        {
            return _executer.GetRelEntities();
        }
        public EcsSubGraphSpan GetRelations(int startEntityID)
        {
            return _executer.GetRelations_Internal(startEntityID);
        }
        public int GetRelation(int startEntityID)
        {
            return _executer.GetRelation_Internal(startEntityID);
        }
        public int GetRelationsCount(int startEntityID)
        {
            return _executer.GetRelationsCount_Internal(startEntityID);
        }
    }

    public readonly ref struct EcsSubGraphSpan
    {
        public static EcsSubGraphSpan Empty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new EcsSubGraphSpan(null, 0, 0); }
        }

        private readonly LinkedList.Node[] _nodes;
        private readonly LinkedList.NodeIndex _startNodeIndex;
        private readonly int _count;
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _count; }
        }
        private IEnumerable<int> E
        {
            get
            {
                List<int> result = new List<int>();
                foreach (var item in this)
                {
                    result.Add(item);
                }
                return result;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EcsSubGraphSpan(LinkedList.Node[] nodes, LinkedList.NodeIndex startNodeIndex, int count)
        {
            _nodes = nodes;
            _startNodeIndex = startNodeIndex;
            _count = count;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
            return new Enumerator(_nodes, _startNodeIndex, _count);
        }
        public ref struct Enumerator
        {
            private readonly LinkedList.Node[] _nodes;
            private int _index;
            private int _count;
            private int _next;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(LinkedList.Node[] nodes, LinkedList.NodeIndex startIndex, int count)
            {
                _nodes = nodes;
                _index = -1;
                _count = count;
                _next = (int)startIndex;
            }
            public int Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return _nodes[_index].entityID; }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                _index = _next;
                _next = (int)_nodes[_next].next;
                return _index > 0 && _count-- > 0;
                //return _count-- > 0;
            }
        }
    }
    #endregion
}