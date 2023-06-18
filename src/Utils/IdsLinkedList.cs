using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
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
        public int Count => _count;
        public int Capacity => _nodes.Length;
        public int Last => _lastNodeIndex;
        #endregion

        #region Constructors
        public IdsLinkedList(int capacity)
        {
            _nodes = new Node[capacity + 10];
            Clear();
        }
        #endregion

        public void Resize(int newCapacity)
        {
            Array.Resize(ref _nodes, newCapacity + 10);
        }

        public void Clear()
        {
            for (int i = 0; i < _nodes.Length; i++)
                _nodes[i].next = 0;
            _lastNodeIndex = Head;
            _count = 0;
        }

        public void Set(int nodeIndex, int value) => _nodes[nodeIndex].value = value;
        public int Get(int nodeIndex) => _nodes[nodeIndex].value;

        /// <summary> Insert after</summary>
        /// <returns> new node index</returns>
        public int InsertAfter(int nodeIndex, int value)
        {
            int newNodeIndex;
            if (_recycledNodesCount > 0)
            {
                newNodeIndex = _recycledNodes[--_recycledNodesCount];
                _count++;
            }
            else
            {
                newNodeIndex = ++_count;
            }

            ref Node prevNode = ref _nodes[nodeIndex];
            ref Node nextNode = ref _nodes[prevNode.next];

            _nodes[newNodeIndex].Set(value, nextNode.prev, prevNode.next);

            prevNode.next = newNodeIndex;
            nextNode.prev = newNodeIndex;

            _lastNodeIndex = newNodeIndex;
            return newNodeIndex;
        }
        //public int InsertBefore(int nodeIndex, int value) { }
        public void Remove(int nodeIndex)
        {
            if(nodeIndex <= 0)
                throw new ArgumentOutOfRangeException();

            ref var node = ref _nodes[nodeIndex];
            _nodes[node.next].prev = node.prev;
            _nodes[node.prev].next = node.next;

           // node = Node.Empty;

            if (_recycledNodesCount >= _recycledNodes.Length)
                Array.Resize(ref _recycledNodes, _recycledNodes.Length << 1);
            _recycledNodes[_recycledNodesCount++] = nodeIndex;
            _count--;
        }

        public int Add(int id) => InsertAfter(_lastNodeIndex, id);
        public ref readonly Node GetNode(int nodeIndex) => ref _nodes[nodeIndex];

        public SpanEnumerator GetEnumerator() => new SpanEnumerator(_nodes, _nodes[Head].next, _count);
        public Span GetSpan(int startNodeIndex, int count) => new Span(this, startNodeIndex, count);
        public Span EmptySpan() => new Span(this, 0, 0);

        #region IEnumerable
        IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion

        #region Utils Node/Enumerator/Span
        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
        public struct Node
        {
            public static readonly Node Empty = new Node() { value = 0, next = -1 };
            public int value;
            /// <summary>next node index</summary>
            public int next;
            /// <summary>prev node index</summary>
            public int prev;
            public void Set(int value, int prev, int next)
            {
                this.value = value;
                this.next = next;
                this.prev = prev;
            }
            public override string ToString() => $"node(<:{prev} >:{next} v:{value})";
        }

        #region Enumerator/Span
        public struct Enumerator : IEnumerator<int>
        {
            private readonly Node[] _nodes;
            private int _index;
            private int _next;
            public Enumerator(Node[] nodes)
            {
                _nodes = nodes;
                _index = -1;
                _next = _nodes[Head].next;
            }
            public int Current => _nodes[_index].value;


            public bool MoveNext()
            {
                _index = _next;
                _next = _nodes[_next].next;
                return _index > 0;
            }

            object IEnumerator.Current => Current;
            void IDisposable.Dispose() { }
            void IEnumerator.Reset()
            {
                _index = -1;
                _next = Head;
            }
        }
        public readonly ref struct Span
        {
            private readonly IdsLinkedList _source;
            private readonly int _startNodeIndex;
            private readonly int _count;
            public Span(IdsLinkedList source, int startNodeIndex, int count)
            {
                _source = source;
                _startNodeIndex = startNodeIndex;
                _count = count;
            }
            public SpanEnumerator GetEnumerator() => new SpanEnumerator(_source._nodes, _startNodeIndex, _count);
        }
        public struct SpanEnumerator : IEnumerator<int>
        {
            private readonly Node[] _nodes;
            private int _index;
            private int _count;
            private int _next;
            public SpanEnumerator(Node[] nodes, int startIndex, int count)
            {
                _nodes = nodes;
                _index = -1;
                _count = count;
                _next = startIndex;
            }
            public int Current => _nodes[_index].value;
            public bool MoveNext()
            {
                _index = _next;
                _next = _nodes[_next].next;
                return _index > 0 && _count-- > 0;
            }

            object IEnumerator.Current => Current;
            void IDisposable.Dispose() { }
            void IEnumerator.Reset()
            {
                _index = -1;
                _next = Head;
            }
        }
        #endregion

        #endregion

        public class DebuggerProxy
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
                        if (index > 0 && count-- > 0)
                        { }
                        else break;

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
    }
}
