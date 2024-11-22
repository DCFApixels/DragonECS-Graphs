using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Graphs.Internal;
using DCFApixels.DragonECS.UncheckedCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using LinkedList = DCFApixels.DragonECS.Graphs.Internal.OnlyAppendHeadLinkedList;

namespace DCFApixels.DragonECS.Graphs.Internal
{
    internal sealed class JoinExecutor : MaskQueryExecutor, IEcsWorldEventListener
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
        //private EcsProfilerMarker _executeMarker = new EcsProfilerMarker("Join");

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
            if (World.IsGraphWorld() == false)
            {
                Throw.Exception("The JounSubGraph query can only be used for EntityGraph.GraphWorld or a collection of that world.");
            }

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SubGraphMap Execute_Internal(JoinMode mode)
        {
            //_executeMarker.Begin();

            World.ReleaseDelEntityBufferAllAuto();

            if (Mask.IsEmpty || _versionsChecker.CheckAndNext() == false)
            {
                _filteredAllEntitiesCount = _iterator.IterateTo(World.Entities, ref _filteredAllEntities);
                if (_sourceEntities.Length < _graph.World.Capacity * 2)
                {
                    _sourceEntities = new int[_graph.World.Capacity * 2];
                }
                //Подготовка массивов
            }


            //установка текущего массива
            _currentFilteredEntities = _filteredAllEntities;
            _currentFilteredEntitiesCount = _filteredAllEntitiesCount;

            //Подготовка массивов
            if (_targetWorldCapacity < _graph.World.Capacity)
            {
                _targetWorldCapacity = _graph.World.Capacity;
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
            if ((mode & JoinMode.Start) != 0)
            {
                for (int i = 0; i < _filteredAllEntitiesCount; i++)
                {
                    AddStart(_filteredAllEntities[i]);
                }
            }
            if ((mode & JoinMode.End) != 0)
            {
                for (int i = 0; i < _filteredAllEntitiesCount; i++)
                {
                    AddEnd(_filteredAllEntities[i]);
                }
            }


            _version++;

            //_executeMarker.End();
            return new SubGraphMap(this);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SubGraphMap ExecuteFor_Internal(EcsSpan span, JoinMode mode)
        {
            //_executeMarker.Begin();
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (span.IsNull) { /*_executeMarker.End();*/ Throw.ArgumentNull(nameof(span)); }
            if (span.WorldID != World.ID) { /*_executeMarker.End();*/ Throw.Quiery_ArgumentDifferentWorldsException(); }
#endif
            if (_filteredEntities == null)
            {
                _filteredEntities = new int[32];
            }
            _filteredEntitiesCount = _iterator.IterateTo(span, ref _filteredEntities);
            if (_sourceEntities.Length < _filteredEntitiesCount * 2)
            {
                _sourceEntities = new int[_filteredEntitiesCount * 2];
            }


            //установка текущего массива
            _currentFilteredEntities = _filteredEntities;
            _currentFilteredEntitiesCount = _filteredEntitiesCount;

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
            if ((mode & JoinMode.Start) != 0)
            {
                for (int i = 0; i < _filteredEntitiesCount; i++)
                {
                    AddStart(_filteredEntities[i]);
                }
            }
            if ((mode & JoinMode.End) != 0)
            {
                for (int i = 0; i < _filteredEntitiesCount; i++)
                {
                    AddEnd(_filteredEntities[i]);
                }
            }


            //_executeMarker.End();
            return new SubGraphMap(this);
        }

        public SubGraphMap Execute(JoinMode mode = JoinMode.Start)
        {
            return Execute_Internal(mode);
        }
        public SubGraphMap ExecuteFor(EcsSpan span, JoinMode mode = JoinMode.Start)
        {
            return ExecuteFor_Internal(span, mode);
        }

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
        internal SubGraphMap.NodeInfo GetRelations_Internal(int sourceEntityID)
        {
            LinkedListHead basket = _linkedListSourceHeads[sourceEntityID];
            return new SubGraphMap.NodeInfo(_linkedList._nodes, basket.head, basket.count);
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
        internal EcsSpan GetNodeEntities()
        {
            return UncheckedCoreUtility.CreateSpan(WorldID, _sourceEntities, _sourceEntitiesCount);
        }
        internal EcsSpan GetRelEntities()
        {
            return UncheckedCoreUtility.CreateSpan(WorldID, _currentFilteredEntities, _currentFilteredEntitiesCount);
        }
        #endregion
    }
}

namespace DCFApixels.DragonECS
{
    public enum JoinMode : byte
    {
        NONE = 0,
        Start = 1 << 0,
        End = 1 << 1,
        All = Start | End,
    }

    #region SubGraphMap
    public readonly ref struct SubGraphMap
    {
        private readonly JoinExecutor _executer;
        public EntityGraph Graph
        {
            get { return _executer.Graph; }
        }
        internal SubGraphMap(JoinExecutor executer)
        {
            _executer = executer;
        }

        public EcsSpan WhereNodes<TAspect>(out TAspect a)
            where TAspect : EcsAspect, new()
        {
            return _executer.GetNodeEntities().Where(out a);
        }
        public EcsSpan WhereNodes<TAspect>(IComponentMask mask)
        {
            return _executer.GetNodeEntities().Where(mask);
        }
        public EcsSpan WhereNodes<TAspect>(out TAspect a, Comparison<int> comparison)
            where TAspect : EcsAspect, new()
        {
            return _executer.GetNodeEntities().Where(out a, comparison);
        }
        public EcsSpan WhereNodes<TAspect>(IComponentMask mask, Comparison<int> comparison)
        {
            return _executer.GetNodeEntities().Where(mask, comparison);
        }

        public EcsSpan GetNodes()
        {
            return _executer.GetNodeEntities();
        }
        public EcsSpan GetAllRelations()
        {
            return _executer.GetRelEntities();
        }

        public NodeInfo GetRelations(int nodeEntityID)
        {
            return _executer.GetRelations_Internal(nodeEntityID);
        }

        public int GetRelation(int nodeEntityID)
        {
            return _executer.GetRelation_Internal(nodeEntityID);
        }
        public int GetRelationsCount(int nodeEntityID)
        {
            return _executer.GetRelationsCount_Internal(nodeEntityID);
        }

        [DebuggerTypeProxy(typeof(DebuggerProxy))]
        public readonly ref struct NodeInfo
        {
            private readonly LinkedList.Node[] _nodes;
            private readonly LinkedList.NodeIndex _startNodeIndex;
            private readonly int _count;
            public int Count
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return _count; }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal NodeInfo(LinkedList.Node[] nodes, LinkedList.NodeIndex startNodeIndex, int count)
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
            private class DebuggerProxy
            {
                private readonly LinkedList.Node[] _nodes;
                private readonly LinkedList.NodeIndex _startNodeIndex;
                private readonly int _count;
                private IEnumerable<int> Entities
                {
                    get
                    {
                        List<int> result = new List<int>();
                        foreach (var item in new NodeInfo(_nodes, _startNodeIndex, _count))
                        {
                            result.Add(item);
                        }
                        return result;
                    }
                }
                public DebuggerProxy(NodeInfo node)
                {
                    _nodes = node._nodes;
                    _startNodeIndex = node._startNodeIndex;
                    _count = node._count;
                }
            }
        }
    }
    #endregion
}