#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Graphs.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using LinkedList = DCFApixels.DragonECS.Graphs.Internal.OnlyAppendHeadLinkedList;

namespace DCFApixels.DragonECS.Graphs.Internal
{
    internal sealed class JoinExecutor : IEcsWorldEventListener
    {
        private EcsWorld _graphWorld;
        private EntityGraph _graph;

        private long _version = 0;

        private LinkedList _linkedList;
        private LinkedListHead[] _linkedListSourceHeads;

        //заменить на спарссет без пейджей
        private EcsGroup _sourceEntities;

        private int _targetWorldCapacity = -1;
        //private EcsProfilerMarker _executeMarker = new EcsProfilerMarker("Join");

        public bool _isDestroyed = false;

        #region Properties
        public long Version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _version; }
        }
        public EntityGraph Graph
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _graph; }
        }
        #endregion

        #region Constructors/OnDestroy
        public JoinExecutor(EcsWorld world)
        {
            _graphWorld = world;
            _linkedList = new OnlyAppendHeadLinkedList(_graphWorld.Capacity);
            _linkedListSourceHeads = new LinkedListHead[_graphWorld.Capacity];
            _graphWorld.AddListener(this);
            _graph = _graphWorld.GetGraph();
            _sourceEntities = EcsGroup.New(_graph.World);
        }
        public void Destroy()
        {
            if (_isDestroyed) { return; }
            _isDestroyed = true;
            _graphWorld.RemoveListener(this);
        }
        #endregion

        #region Execute
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SubGraphMap ExecuteFor_Internal(EcsSpan span, JoinMode mode)
        {
            //_executeMarker.Begin();
#if DEBUG || DRAGONECS_STABILITY_MODE
            if (span.IsNull) { /*_executeMarker.End();*/ Throw.ArgumentNull(nameof(span)); }
            if (span.WorldID != _graphWorld.ID) { /*_executeMarker.End();*/ Throw.Quiery_ArgumentDifferentWorldsException(); }
#endif
            //Подготовка массивов
            if (_targetWorldCapacity < _graphWorld.Capacity)
            {
                _targetWorldCapacity = _graphWorld.Capacity;
                _linkedListSourceHeads = new LinkedListHead[_targetWorldCapacity];
                //_startEntities = new int[_targetWorldCapacity];
            }
            else
            {
                //ArrayUtility.Fill(_linkedListSourceHeads, default); //TODO оптимизировать, сделав не полную отчистку а только по элементов с прошлого раза
                //for (int i = 0; i < _sourceEntitiesCount; i++)
                for (int i = 0; i < _sourceEntities.Count; i++)
                {
                    _linkedListSourceHeads[_sourceEntities[i]] = default;
                }
            }
            //_sourceEntitiesCount = 0;
            _sourceEntities.Clear();
            _linkedList.Clear();

            //Заполнение массивов
            if ((mode & JoinMode.Start) != 0)
            {
                for (int i = 0, iMax = span.Count; i < iMax; i++)
                {
                    AddStart(span[i]);
                }
            }
            if ((mode & JoinMode.End) != 0)
            {
                for (int i = 0, iMax = span.Count; i < iMax; i++)
                {
                    AddEnd(span[i]);
                }
            }

            //_executeMarker.End();
            return new SubGraphMap(this);
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
            _sourceEntities.Add(sourceEntityID);
            ref var basket = ref _linkedListSourceHeads[sourceEntityID];
            //EcsDebug.Print("e" + sourceEntityID);
            if (basket.head == 0)
            {
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
            //return UncheckedCoreUtility.CreateSpan(_graph.World.ID, _sourceEntities, _sourceEntitiesCount);
            return _sourceEntities.ToSpan();
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
        public EcsSpan Nodes
        {
            get { return _executer.GetNodeEntities(); }
        }

        internal SubGraphMap(JoinExecutor executer)
        {
            _executer = executer;
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