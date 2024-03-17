using DCFApixels.DragonECS.Graphs.Internal;
using DCFApixels.DragonECS.UncheckedCore;
using System;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public abstract class EcsJoinToGraphExecutor : EcsQueryExecutor, IEcsWorldEventListener
    {
        private EcsAspect _aspect;
        private EcsArcWorld _arcWorld;

        private EntityLinkedList _linkedList;
        private Basket[] _baskets;
        private int[] _startEntities;
        private int _startEntitiesCount;

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
        public EcsArcWorld ArcWorld
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _arcWorld; }
        }
        #endregion

        #region OnInitialize/OnDestroy
        protected override void OnInitialize()
        {
            _linkedList = new EntityLinkedList(World.Capacity);
            _baskets = new Basket[World.Capacity];
            World.AddListener(this);
            _arcWorld = (EcsArcWorld)World;
        }
        protected override void OnDestroy()
        {
            World.RemoveListener(this);
        }
        #endregion

        #region Execute
        public EcsGraph Execute()
        {
            return ExecuteFor(World.Entities);
        }
        public EcsGraph ExecuteFor(EcsSpan span)
        {
            _executeMarker.Begin();
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (span.IsNull) { Throw.ArgumentException(""); }//TODO составить текст исключения. 
            else if (World != span.World) { Throw.ArgumentException(""); } //TODO составить текст исключения. это проверка на то что пользователь использует правильный мир
#endif

            if (_lastWorldVersion != World.Version)
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

                EcsArc arc = _arcWorld.GetRegisteredArc();

                var iterator = _aspect.GetIteratorFor(span);
                foreach (var relationEntityID in iterator)
                {
                    int startEntityID = arc.GetRelationStart(relationEntityID);
                    if(startEntityID == 0)
                    {
                        continue;
                    }
                    _startEntities[_startEntitiesCount++] = startEntityID;

                    ref var basket = ref _baskets[startEntityID];
                    if (basket.index <= 0)
                    {
                        basket.index = _linkedList.Add(relationEntityID);
                    }
                    else
                    {
                        _linkedList.InsertAfter(basket.index, relationEntityID);
                    }
                    basket.count++;
                }

                _lastWorldVersion = World.Version;
            }
            else
            {

            }

            _executeMarker.End();
            return new EcsGraph(this, UncheckedCoreUtility.CreateSpan(WorldID, _startEntities, _startEntitiesCount));
        }
        #endregion

        #region Internal result methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EcsGraphSpan GetNodes_Internal(int startEntityID)
        {
            Basket basket = _baskets[startEntityID];
            return new EcsGraphSpan(_linkedList._nodes, basket.index, basket.count);
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
        }
        #endregion
    }
    public sealed class EcsJoinExecutor<TAspect> : EcsJoinToGraphExecutor
        where TAspect : EcsAspect
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
            get { return _aspect; }
        }
        #endregion
    }


    #region EcsJoinedSpan/EcsJoined
    public readonly ref struct EcsGraphSpan
    {
        public static EcsGraphSpan Empty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new EcsGraphSpan(null, 0, 0); }
        }

        private readonly EntityLinkedList.Node[] _nodes;
        private readonly int _startNodeIndex;
        private readonly int _count;
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _count; }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EcsGraphSpan(EntityLinkedList.Node[] nodes, int startNodeIndex, int count)
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
        public struct Enumerator
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

    public readonly ref struct EcsGraph
    {
        private readonly EcsJoinToGraphExecutor _executer;
        private readonly EcsSpan _startEntities;
        public EcsSpan StartEntitiesSpan
        {
            get { return _startEntities; }
        }
        internal EcsGraph(EcsJoinToGraphExecutor executer, EcsSpan startEntites)
        {
            _executer = executer;
            _startEntities = startEntites;
        }
        public EcsGraphSpan GetNodes(int startEntityID)
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
}
