using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS.Graphs.Internal
{
    internal class OnlyAppendHeadLinkedList
    {
        public const NodeIndex Enter = 0;

        internal Node[] _nodes;
        private int _count;
        private NodeIndex _lastNodeIndex;

        #region Properties
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _count; }
        }
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _nodes.Length; }
        }
        public NodeIndex Last
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _lastNodeIndex; }
        }
        #endregion

        #region Constructors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OnlyAppendHeadLinkedList(int capacity)
        {
            _nodes = new Node[capacity * 2 + 10];
            Clear();
        }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InsertIntoHead(ref NodeIndex headIndex, int entityID)
        {
            _nodes[++_count].Set(entityID, headIndex);
            headIndex = (NodeIndex)_count;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NodeIndex NewHead(int entityID)
        {
            _nodes[++_count].Set(entityID, 0);
            return (NodeIndex)_count;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Get(NodeIndex nodeIndex)
        {
            return _nodes[(int)nodeIndex].entityID;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _lastNodeIndex = Enter;
            _count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
            return new Enumerator(_nodes);
        }
        #endregion

        #region TMP
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public void Resize(int newCapacity)
        //{
        //    Array.Resize(ref _nodes, newCapacity * 2 + 10);
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public void Set(HeadIndex nodeIndex, int entityID)
        //{
        //    _nodes[(int)nodeIndex].entityID = entityID;
        //}

        ///// <returns> new node index</returns>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public NodeID InsertBefore(NodeID nodeIndex, int entityID)
        //{
        //    NodeID newNode = (NodeID)(++_count);
        //    _nodes[(int)newNode] = _nodes[(int)nodeIndex];
        //    _nodes[(int)newNode].Set(entityID, nodeIndex);
        //    return newNode;
        //}
        ///// <returns> new node index</returns>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public NodeID InsertAfter(NodeID nodeIndex, int entityID)
        //{
        //    NodeID newNode = (NodeID)(++_count);
        //    _nodes[(int)nodeIndex].next = newNode;
        //    _nodes[(int)newNode].Set(entityID, _nodes[(int)nodeIndex].next);
        //    _lastNodeIndex = newNode;
        //    return newNode;
        //
        //    //_nodes[++_count].Set(entityID, _nodes[(int)nodeIndex].next);
        //    //_nodes[(int)nodeIndex].next = (NodeID)_count;
        //    //_lastNodeIndex = (NodeID)((int)nodeIndex + 1);
        //    //return (NodeID)_count;
        //}
        /// <returns> new node index</returns>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public NodeID Add(int entityID)
        //{
        //    var result = InsertAfter(_lastNodeIndex, entityID);
        //    _lastNodeIndex = (NodeID)_count;
        //    return result;
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public EcsJoinedSpan GetSpan(int startNodeIndex, int count)
        //{
        //    return new EcsJoinedSpan(_nodes, startNodeIndex, count);
        //}
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public EcsJoinedSpan GetEmptySpan()
        //{
        //    return new EcsJoinedSpan(_nodes, 0, 0);
        //}
        #endregion

        #region Utils
        public enum NodeIndex : int { }

        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
        public struct Node
        {
            public static readonly Node Empty = new Node() { entityID = 0, next = (int)Enter };

            public int entityID;
            public NodeIndex next;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Set(int entityID, NodeIndex next)
            {
                this.entityID = entityID;
                this.next = next;
            }
            public override string ToString()
            {
                return $"e:{entityID} next:{next}";
            }
        }
        public struct Enumerator
        {
            private readonly Node[] _nodes;
            private int _index;
            private int _next;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(Node[] nodes)
            {
                _nodes = nodes;
                _index = -1;
                _next = (int)Enter;
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
                return _index > 0;
            }
        }
        #endregion
    }
}
