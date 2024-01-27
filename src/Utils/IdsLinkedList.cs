using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS.Relations.Utils
{
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public class IdsLinkedList : IEnumerable<int>
    {
        public const int Head = 0;

        private Node[] _nodes;
        private int _count;
        private int _lastNodeIndex;

        private int[] _recycledNodes = new int[4];
        private int _recycledNodesCount;

        #region Properties
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _count;
        }
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _nodes.Length;
        }

        public int Last
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _lastNodeIndex;
        }
        public ReadOnlySpan<Node> Nodes
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new ReadOnlySpan<Node>(_nodes);
        }
        #endregion

        #region Constructors
        public IdsLinkedList(int capacity)
        {
            _nodes = new Node[capacity + 10];
            Clear();
        }
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            for (int i = 0; i < _nodes.Length; i++)
                _nodes[i].next = 0;
            _lastNodeIndex = Head;
            _count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int nodeIndex, int value)
        {
            _nodes[nodeIndex].value = value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Get(int nodeIndex)
        {
            return _nodes[nodeIndex].value;
        }

        /// <summary> Insert after</summary>
        /// <returns> new node index</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int InsertAfter(int nodeIndex, int value)
        {
            if (++_count >= _nodes.Length)
            {
                Array.Resize(ref _nodes, _nodes.Length << 1);
            }
            int newNodeIndex = _recycledNodesCount > 0 ? _recycledNodes[--_recycledNodesCount] : _count;

            ref Node prevNode = ref _nodes[nodeIndex];
            ref Node nextNode = ref _nodes[prevNode.next];
            if (prevNode.next == 0)
            {
                _lastNodeIndex = newNodeIndex;
            }
            _nodes[newNodeIndex].Set(value, nextNode.prev, prevNode.next);
            prevNode.next = newNodeIndex;
            nextNode.prev = newNodeIndex;

            return newNodeIndex;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int InsertBefore(int nodeIndex, int value)
        {
            if (++_count >= _nodes.Length)
                Array.Resize(ref _nodes, _nodes.Length << 1);
            int newNodeIndex = _recycledNodesCount > 0 ? _recycledNodes[--_recycledNodesCount] : _count;

            ref Node nextNode = ref _nodes[nodeIndex];
            ref Node prevNode = ref _nodes[nextNode.prev];
            _nodes[newNodeIndex].Set(value, nextNode.prev, prevNode.next);
            prevNode.next = newNodeIndex;
            nextNode.prev = newNodeIndex;

            return newNodeIndex;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveIndex(int nodeIndex)
        {
            if (nodeIndex <= 0)
                throw new ArgumentOutOfRangeException();

            ref var node = ref _nodes[nodeIndex];
            _nodes[node.next].prev = node.prev;
            _nodes[node.prev].next = node.next;

            if (_recycledNodesCount >= _recycledNodes.Length)
                Array.Resize(ref _recycledNodes, _recycledNodes.Length << 1);
            _recycledNodes[_recycledNodesCount++] = nodeIndex;
            _count--;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int RemoveIndexAndReturnNextIndex(int nodeIndex)
        {
            if (nodeIndex <= 0)
                throw new ArgumentOutOfRangeException();

            ref var node = ref _nodes[nodeIndex];
            int nextNode = node.next;
            _nodes[node.next].prev = node.prev;
            _nodes[node.prev].next = nextNode;

            if (_recycledNodesCount >= _recycledNodes.Length)
                Array.Resize(ref _recycledNodes, _recycledNodes.Length << 1);
            _recycledNodes[_recycledNodesCount++] = nodeIndex;
            _count--;

            return nextNode;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveSpan(int startNodeIndex, int count)
        {
            if (count <= 0)
                return;

            int endNodeIndex = startNodeIndex;

            if (_recycledNodesCount >= _recycledNodes.Length)
                Array.Resize(ref _recycledNodes, _recycledNodes.Length << 1);
            _recycledNodes[_recycledNodesCount++] = startNodeIndex;

            for (int i = 1; i < count; i++)
            {
                endNodeIndex = _nodes[endNodeIndex].next;
                if (endNodeIndex == 0)
                    throw new ArgumentOutOfRangeException();

                if (_recycledNodesCount >= _recycledNodes.Length)
                    Array.Resize(ref _recycledNodes, _recycledNodes.Length << 1);
                _recycledNodes[_recycledNodesCount++] = endNodeIndex;
            }

            ref var startNode = ref _nodes[startNodeIndex];
            ref var endNode = ref _nodes[endNodeIndex];

            _nodes[endNode.next].prev = startNode.prev;
            _nodes[startNode.prev].next = endNode.next;

            _count -= count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Add(int id)
        {
            return InsertAfter(_lastNodeIndex, id);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly Node GetNode(int nodeIndex)
        {
            return ref _nodes[nodeIndex];
        }

        #region Span/Enumerator
        IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SpanEnumerator GetEnumerator() => new SpanEnumerator(_nodes, _nodes[Head].next, _count);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span GetSpan(int startNodeIndex, int count) => new Span(this, startNodeIndex, count);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span EmptySpan() => new Span(this, 0, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LongSpan GetLongs(EcsWorld world) => new LongSpan(world, this, _nodes[Head].next, _count);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LongSpan GetLongSpan(EcsWorld world, int startNodeIndex, int count) => new LongSpan(world, this, startNodeIndex, count);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LongSpan EmptyLongSpan(EcsWorld world) => new LongSpan(world, this, 0, 0);

        public readonly ref struct Span
        {
            private readonly IdsLinkedList _source;
            private readonly int _startNodeIndex;
            private readonly int _count;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span(IdsLinkedList source, int startNodeIndex, int count)
            {
                _source = source;
                _startNodeIndex = startNodeIndex;
                _count = count;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public SpanEnumerator GetEnumerator() => new SpanEnumerator(_source._nodes, _startNodeIndex, _count);
        }
        public struct SpanEnumerator : IEnumerator<int>
        {
            private readonly Node[] _nodes;
            private int _count;
            private int _index;
            private int _next;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public SpanEnumerator(Node[] nodes, int startIndex, int count)
            {
                _nodes = nodes;
                _index = -1;
                _count = count;
                _next = startIndex;
            }
            public int Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return _nodes[_index].value;
                }
            }

            object IEnumerator.Current => Current;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                _index = _next;
                _next = _nodes[_next].next;
                return _index > 0 && _count-- > 0;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void IDisposable.Dispose() { }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void IEnumerator.Reset()
            {
                _index = -1;
                _next = Head;
            }
        }
        public readonly ref struct LongSpan
        {
            private readonly EcsWorld _world;
            private readonly IdsLinkedList _source;
            private readonly int _startNodeIndex;
            private readonly int _count;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public LongSpan(EcsWorld world, IdsLinkedList source, int startNodeIndex, int count)
            {
                _world = world;
                _source = source;
                _startNodeIndex = startNodeIndex;
                _count = count;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public LongSpanEnumerator GetEnumerator() => new LongSpanEnumerator(_world, _source._nodes, _startNodeIndex, _count);
        }
        public struct LongSpanEnumerator : IEnumerator<entlong>
        {
            private EcsWorld _world;
            private readonly Node[] _nodes;
            private int _count;
            private int _index;
            private int _next;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public LongSpanEnumerator(EcsWorld world, Node[] nodes, int startIndex, int count)
            {
                _world = world;
                _nodes = nodes;
                _index = -1;
                _count = count;
                _next = startIndex;
            }
            public entlong Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return _world.GetEntityLong(_nodes[_index].value);
                }
            }

            object IEnumerator.Current => Current;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                _index = _next;
                _next = _nodes[_next].next;
                return _index > 0 && _count-- > 0;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void IDisposable.Dispose() { }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void IEnumerator.Reset()
            {
                _index = -1;
                _next = Head;
            }
        }
        #endregion

        #region Node
        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
        public struct Node
        {
            public static readonly Node Empty = new Node() { value = 0, next = -1 };
            public int value;
            /// <summary>next node index</summary>
            public int next;
            /// <summary>prev node index</summary>
            public int prev;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Set(int value, int prev, int next)
            {
                this.value = value;
                this.next = next;
                this.prev = prev;
            }
            public override string ToString() => $"node({prev}<>{next} v:{value})";
        }
        #endregion

        #region Debug
        internal class DebuggerProxy
        {
            private IdsLinkedList list;
            public NodeInfo[] Nodes
            {
                get
                {
                    var result = new NodeInfo[list.Count];

                    Node[] nodes = list._nodes;
                    int index = -1;
                    int count = list._count;
                    int next = list._nodes[Head].next;

                    int i = 0;
                    while (true)
                    {
                        index = next;
                        next = nodes[next].next;
                        if (!(index > 0 && count-- > 0))
                            break;
                        ref var node = ref nodes[index];
                        result[i] = new NodeInfo(index, node.prev, node.next, node.value);
                        i++;
                    }
                    return result;
                }
            }
            public DebuggerProxy(IdsLinkedList list)
            {
                this.list = list;
            }

            public struct NodeInfo
            {
                public int index;
                public int prev;
                public int next;
                public int value;
                public NodeInfo(int index, int prev, int next, int value)
                {
                    this.index = index;
                    this.prev = prev;
                    this.next = next;
                    this.value = value;
                }
                public override string ToString() => $"[{index}] {prev}_{next} - {value}";
            }
        }
        #endregion
    }
}
