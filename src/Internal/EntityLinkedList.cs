using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS.Graphs.Internal
{
    internal class EntityLinkedList
    {
        public const int Enter = 0;

        internal Node[] _nodes;
        private int _count;
        private int _lastNodeIndex;

        #region Properties
        public int Count => _count;
        public int Capacity => _nodes.Length;
        public int Last => _lastNodeIndex;
        #endregion

        #region Constructors
        public EntityLinkedList(int capacity)
        {
            _nodes = new Node[capacity + 10];
            Clear();
        }
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Resize(int newCapacity)
        {
            Array.Resize(ref _nodes, newCapacity + 10);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            for (int i = 0; i < _nodes.Length; i++)
            {
                _nodes[i].next = 0;
            }
            _lastNodeIndex = Enter;
            _count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int nodeIndex, int entityID)
        {
            _nodes[nodeIndex].entityID = entityID;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Get(int nodeIndex)
        {
            return _nodes[nodeIndex].entityID;
        }

        /// <summary> Insert after</summary>
        /// <returns> new node index</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int InsertAfter(int nodeIndex, int entityID)
        {
            _nodes[++_count].Set(entityID, _nodes[nodeIndex].next);
            _nodes[nodeIndex].next = _count;
            _lastNodeIndex = _count;
            return _count;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Add(int entityID)
        {
            return InsertAfter(_lastNodeIndex, entityID);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
            return new Enumerator(_nodes);
        }
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
        #region Utils
        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
        public struct Node
        {
            public static readonly Node Empty = new Node() { entityID = 0, next = -1 };
            public int entityID;
            /// <summary>next node index</summary>
            public int next;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Set(int entityID, int next)
            {
                this.entityID = entityID;
                this.next = next;
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
                _next = Enter;
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
                return _index > 0;
            }
        }
        #endregion
    }
}
