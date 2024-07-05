using DCFApixels.DragonECS.Graphs.Internal;
using DCFApixels.DragonECS.UncheckedCore;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public sealed class EcsJoinToSubGraphExecutor<TAspect> : EcsJoinToSubGraphExecutor
        where TAspect : EcsAspect, new()
    {
        private TAspect _aspect;

        #region Properties
        public TAspect Aspect
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _aspect; }
        }
        protected override EcsAspect AspectRaw
        {
            get
            {
                if (_aspect == null) { _aspect = World.GetAspect<TAspect>(); }
                return _aspect;
            }
        }
        #endregion
    }
    public abstract class EcsJoinToSubGraphExecutor : EcsQueryExecutor, IEcsWorldEventListener
    {
        private EcsAspect _aspect;

        private EntityLinkedList _linkedList;
        private Basket[] _baskets;
        private int[] _startEntities;
        private int _startEntitiesCount;

        private EcsGraph _graph;

        private long _lastWorldVersion;

        private int _targetWorldCapacity = -1;
        private EcsProfilerMarker _executeMarker = new EcsProfilerMarker("JoinAttach");

        #region Properties
        protected abstract EcsAspect AspectRaw { get; }
        public sealed override long Version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _lastWorldVersion; }
        }
        public EcsGraph Graph
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _graph; }
        }
        #endregion

        #region OnInitialize/OnDestroy
        protected override void OnInitialize()
        {
            _linkedList = new EntityLinkedList(World.Capacity);
            _baskets = new Basket[World.Capacity];
            World.AddListener(this);
            _aspect = AspectRaw;
            _graph = World.GetGraph();
        }
        protected override void OnDestroy()
        {
            World.RemoveListener(this);
        }
        #endregion

        #region Execute
        public EcsSubGraph Execute(EcsSubGraphMode mode = EcsSubGraphMode.StartToEnd)
        {
            return ExecuteFor(World.Entities, mode);
        }
        public EcsSubGraph ExecuteFor(EcsSpan span, EcsSubGraphMode mode = EcsSubGraphMode.StartToEnd)
        {
            _executeMarker.Begin();
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (span.IsNull) { Throw.ArgumentException(""); }//TODO составить текст исключения. 
            else if (World != span.World) { Throw.ArgumentException(""); } //TODO составить текст исключения. это проверка на то что пользователь использует правильный мир
#endif

            //if (_lastWorldVersion != World.Version)
            {
                //Подготовка массивов
                if (_targetWorldCapacity < World.Capacity)
                {
                    _targetWorldCapacity = World.Capacity;
                    _baskets = new Basket[_targetWorldCapacity];
                    _startEntities = new int[_targetWorldCapacity];
                }
                else
                {
                    ArrayUtility.Fill(_baskets, default);
                }
                _startEntitiesCount = 0;
                _linkedList.Clear();
                //Конец подготовки массивов

                if ((mode & EcsSubGraphMode.StartToEnd) != 0)
                {
                    if (_aspect.Mask.IsEmpty)
                    {
                        foreach (var relationEntityID in span) { AddStart(relationEntityID); }
                    }
                    else
                    {
                        var iterator = _aspect.GetIteratorFor(span);
                        foreach (var relationEntityID in iterator) { AddStart(relationEntityID); }
                    }
                }
                if ((mode & EcsSubGraphMode.EndToStart) != 0)
                {
                    if (_aspect.Mask.IsEmpty)
                    {
                        foreach (var relationEntityID in span) { AddEnd(relationEntityID); }
                    }
                    else
                    {
                        var iterator = _aspect.GetIteratorFor(span);
                        foreach (var relationEntityID in iterator) { AddEnd(relationEntityID); }
                    }
                }

                _lastWorldVersion = World.Version;
            }
            //else
            //{
            //
            //}

            _executeMarker.End();
            return new EcsSubGraph(this, UncheckedCoreUtility.CreateSpan(WorldID, _startEntities, _startEntitiesCount));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddStart(int relationEntityID)
        {
            AddFrom(_graph.GetRelationStart(relationEntityID), relationEntityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddEnd(int relationEntityID)
        {
            AddFrom(_graph.GetRelationEnd(relationEntityID), relationEntityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddFrom(int fromEntityID, int relationEntityID)
        {
            if (fromEntityID == 0)
            {
                return;
            }
            ref var basket = ref _baskets[fromEntityID];
            if (basket.index <= 0)
            {
                _startEntities[_startEntitiesCount++] = fromEntityID;
                basket.index = _linkedList.Add(relationEntityID);
            }
            else
            {
                basket.index = _linkedList.InsertBefore(basket.index, relationEntityID);
            }
            basket.count++;
        }
        #endregion

        #region Internal result methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EcsSubGraphSpan GetNodes_Internal(int startEntityID)
        {
            Basket basket = _baskets[startEntityID];
            return new EcsSubGraphSpan(_linkedList._nodes, basket.index, basket.count);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetNode_Internal(int startEntityID)
        {
            Basket basket = _baskets[startEntityID];
            return basket.count > 0 ? _linkedList.Get(basket.index) : 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetNodesCount_Internal(int startEntityID)
        {
            return _baskets[startEntityID].count;
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
            Array.Resize(ref _baskets, newSize);
        }
        void IEcsWorldEventListener.OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer) { }
        void IEcsWorldEventListener.OnWorldDestroy() { }
        #endregion

        #region Basket
        public struct Basket
        {
            public int index;
            public int count;
            public override string ToString()
            {
                return $"i:{index} count:{count}";
            }
        }
        #endregion
    }

    public enum EcsSubGraphMode
    {
        NONE = 0,
        StartToEnd = 1 << 0,
        EndToStart = 1 << 1,
        All = StartToEnd | EndToStart,
    }

    #region EcsJoinedSpan/EcsJoined
    public readonly ref struct EcsSubGraphSpan
    {
        public static EcsSubGraphSpan Empty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new EcsSubGraphSpan(null, 0, 0); }
        }

        private readonly EntityLinkedList.Node[] _nodes;
        private readonly int _startNodeIndex;
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
        internal EcsSubGraphSpan(EntityLinkedList.Node[] nodes, int startNodeIndex, int count)
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
            private readonly EntityLinkedList.Node[] _nodes;
            private int _index;
            private int _count;
            private int _next;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(EntityLinkedList.Node[] nodes, int startIndex, int count)
            {
                _nodes = nodes;
                _index = -1;
                _count = count;
                _next = startIndex;
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
                _next = _nodes[_next].next;
                return _index > 0 && _count-- > 0;
            }
        }
    }

    public readonly ref struct EcsSubGraph
    {
        private readonly EcsJoinToSubGraphExecutor _executer;
        private readonly EcsSpan _startEntities;
        public EcsSpan FromEntitiesSpan
        {
            get { return _startEntities; }
        }
        public EcsGraph Graph
        {
            get { return _executer.Graph; }
        }
        internal EcsSubGraph(EcsJoinToSubGraphExecutor executer, EcsSpan startEntites)
        {
            _executer = executer;
            _startEntities = startEntites;
        }
        public EcsSubGraphSpan GetNodes(int startEntityID)
        {
            return _executer.GetNodes_Internal(startEntityID);
        }
        public int GetNode(int startEntityID)
        {
            return _executer.GetNode_Internal(startEntityID);
        }
        public int GetNodesCount(int startEntityID)
        {
            return _executer.GetNodesCount_Internal(startEntityID);
        }
    }

    #endregion









    public static class GraphQueries
    {
        #region JoinToGraph Empty
        public static EcsSubGraph JoinToSubGraph<TCollection>(this TCollection entities, EcsSubGraphMode mode = EcsSubGraphMode.StartToEnd)
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().JoinToSubGraph(mode);
        }
        public static EcsSubGraph JoinToSubGraph(this EcsReadonlyGroup group, EcsSubGraphMode mode = EcsSubGraphMode.StartToEnd)
        {
            return group.ToSpan().JoinToSubGraph(mode);
        }
        public static EcsSubGraph JoinToSubGraph(this EcsSpan span, EcsSubGraphMode mode = EcsSubGraphMode.StartToEnd)
        {
            EcsWorld world = span.World;
            if (world.IsEnableReleaseDelEntBuffer)
            {
                world.ReleaseDelEntityBufferAll();
            }
            var executor = world.GetExecutor<EcsJoinToSubGraphExecutor<EmptyAspect>>();
            return executor.ExecuteFor(span, mode);
        }
        #endregion


        #region JoinToGraph
        public static EcsSubGraph JoinToSubGraph<TCollection, TAspect>(this TCollection entities, out TAspect aspect, EcsSubGraphMode mode = EcsSubGraphMode.StartToEnd)
            where TAspect : EcsAspect, new()
            where TCollection : IEntityStorage
        {
            return entities.ToSpan().JoinToSubGraph(out aspect, mode);
        }
        public static EcsSubGraph JoinToSubGraph<TAspect>(this EcsReadonlyGroup group, out TAspect aspect, EcsSubGraphMode mode = EcsSubGraphMode.StartToEnd)
            where TAspect : EcsAspect, new()
        {
            return group.ToSpan().JoinToSubGraph(out aspect, mode);
        }
        public static EcsSubGraph JoinToSubGraph<TAspect>(this EcsSpan span, out TAspect aspect, EcsSubGraphMode mode = EcsSubGraphMode.StartToEnd)
            where TAspect : EcsAspect, new()
        {
            EcsWorld world = span.World;
            if (world.IsEnableReleaseDelEntBuffer)
            {
                world.ReleaseDelEntityBufferAll();
            }
            var executor = world.GetExecutor<EcsJoinToSubGraphExecutor<TAspect>>();
            aspect = executor.Aspect;
            return executor.ExecuteFor(span, mode);
        }
        #endregion
    }
}
