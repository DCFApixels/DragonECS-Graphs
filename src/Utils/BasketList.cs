using DCFApixels.DragonECS.Relations.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    internal class BasketList
    {
        public const int RECYCLE = -1;
        public const int HEAD = 0;

        private BasketInfo[] _baskets = new BasketInfo[64];
        private Node[] _nodes;
        private int _recycledListLast = -1;

        #region Constructors
        public BasketList() : this(16) { }
        public BasketList(int capacity)
        {
            Initialize(capacity);
        }
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            for (int i = 0; i < _nodes.Length; i++)
            {
                _nodes[i].next = 0;
            }
            for (int i = 0; i < _baskets.Length; i++)
            {
                _baskets[i] = default;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize(int newSize)
        {
            int oldSize = _nodes.Length;
            Array.Resize(ref _nodes, newSize);
            int leftNode = newSize - 1;
            for (int i = oldSize; i < newSize; i++)
            {
                Link(i, leftNode);
                leftNode = i;
            }
            LinkToRecycled(newSize - 1, oldSize);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Initialize(int newSize)
        {
            _nodes = new Node[newSize];
            int leftNode = newSize - 1;
            for (int i = 1; i < newSize; i++)
            {
                Link(i, leftNode);
                leftNode = i;
            }
            LinkToRecycled(newSize - 1, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetBasketNodesCount(int basketIndex)
        {
            return _baskets[basketIndex].count;
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


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveFromBasket(int basketIndex, int nodeIndex)
        {
#if DEBUG
            if (nodeIndex <= 0)
            {
                //Throw.ArgumentOutOfRange();
                return;
            }
#endif
            ref BasketInfo basketInfo = ref _baskets[basketIndex];

            ref var node = ref _nodes[nodeIndex];
            int nextNode = node.next;

            Link(node.prev, nextNode);
            LinkToRecycled(nodeIndex, nodeIndex);
            if(basketInfo.nodeIndex == nodeIndex)
            {
                basketInfo.nodeIndex = nextNode;
            }
            basketInfo.count--;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AddToBasket(int basketIndex, int value)
        {
            ref BasketInfo basketInfo = ref _baskets[basketIndex];
            int newNodeIndex = TakeRecycledNode();
            if (basketInfo.count == 0)
            {
                basketInfo.nodeIndex = newNodeIndex;
                _nodes[newNodeIndex].Set(value, 0, 0);
            }
            else
            {
                int nodeIndex = basketInfo.nodeIndex;
                ref Node nextNode = ref _nodes[nodeIndex];

                _nodes[newNodeIndex].Set(value, nextNode.prev, nodeIndex);
                basketInfo.nodeIndex = newNodeIndex;
                nextNode.prev = newNodeIndex;
            }
            basketInfo.count++;
            return newNodeIndex;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int TakeRecycledNode()
        {
            if (_recycledListLast == -1)
            {
                Resize(_nodes.Length << 1);
            }
            int node = _recycledListLast;
            _recycledListLast = _nodes[node].prev;
            return node;
        }
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private void Separate(int leftNodeIndex, int rightNodeIndex)
        //{
        //    _nodes[rightNodeIndex].prev = 0;
        //    _nodes[leftNodeIndex].next = 0;
        //}
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Link(int leftNodeIndex, int rightNodeIndex)
        {
            _nodes[rightNodeIndex].prev = leftNodeIndex;
            _nodes[leftNodeIndex].next = rightNodeIndex;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LinkToRecycled(int startNodeIndex, int endNodeIndex)
        {
            if (_recycledListLast <= -1)
            {
                _nodes[startNodeIndex].prev = RECYCLE;
            }
            else
            {
                Link(_recycledListLast, startNodeIndex);
            }
            _recycledListLast = endNodeIndex;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveBasket(int basketIndex)
        {
            ref BasketInfo basket = ref _baskets[basketIndex];

            int startNodeIndex = basket.nodeIndex;
            int endNodeIndex = startNodeIndex;
            ref Node startNode = ref _nodes[startNodeIndex];
            for (int i = 0, n = basket.count; i < n; i++)
            {
                endNodeIndex = _nodes[endNodeIndex].next;
            }
            ref Node endNode = ref _nodes[endNodeIndex];

            LinkToRecycled(startNodeIndex, endNodeIndex);
            Link(startNode.prev, endNode.next);

            basket.count = 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void UpBasketsSize(int minSize)
        {
            int newSize = GetHighBitNumber((uint)minSize) << 1;
            Array.Resize(ref _baskets, newSize);
        }
        private static int GetHighBitNumber(uint bits)
        {
            if (bits == 0)
            {
                return -1;
            }
            int bit = 0;
            if ((bits & 0xFFFF0000) != 0)
            {
                bits >>= 16;
                bit |= 16;
            }
            if ((bits & 0xFF00) != 0)
            {
                bits >>= 8;
                bit |= 8;
            }
            if ((bits & 0xF0) != 0)
            {
                bits >>= 4;
                bit |= 4;
            }
            if ((bits & 0xC) != 0)
            {
                bits >>= 2;
                bit |= 2;
            }
            if ((bits & 0x2) != 0)
            {
                bit |= 1;
            }
            return bit;
        }

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

        #region BasketInfo
        private struct BasketInfo
        {
            public static readonly BasketInfo Empty = new BasketInfo() { nodeIndex = 0, count = 0, };
            public int nodeIndex;
            public int count;
            public override string ToString() => $"basket_info(i:{nodeIndex} c:{count})";
        }
        #endregion

        #region Basket
        public BasketIterator this[int basketIndex]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetBasketIterator(basketIndex);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BasketIterator GetBasketIterator(int basketIndex)
        {
            if (_baskets.Length <= basketIndex)
            {
                UpBasketsSize(basketIndex);
            }
            return new BasketIterator(this, basketIndex);
        }
        public readonly struct BasketIterator : IEnumerable<int>
        {
            private readonly BasketList _basketList;
            private readonly int _basketIndex;
            public int Count
            {
                get { return _basketList._baskets[_basketIndex].count; }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public BasketIterator(BasketList basketList, int basketIndex)
            {
                _basketList = basketList;
                _basketIndex = basketIndex;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator GetEnumerator() => new Enumerator(this);
            IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public struct Enumerator : IEnumerator<int>
            {
                private readonly Node[] _nodes;
                private int _nodeIndex;
                private int _nextNodeIndex;
                private int _count;
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public Enumerator(BasketIterator iterator)
                {
                    ref BasketInfo basketInfo = ref iterator._basketList._baskets[iterator._basketIndex];
                    _nodes = iterator._basketList._nodes;
                    _nodeIndex = -1;
                    _nextNodeIndex = basketInfo.nodeIndex;
                    _count = basketInfo.count;
                }
                public int Current
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => _nodes[_nodeIndex].value;
                }
                object IEnumerator.Current => Current;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool MoveNext()
                {
                    _nodeIndex = _nextNodeIndex;
                    _nextNodeIndex = _nodes[_nextNodeIndex].next;
                    return _nodeIndex > 0 && _count-- > 0;
                }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void Reset() { }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void Dispose() { }
            }
        }
        #endregion

        #region DebuggerProxy
        private class DebuggerProxy
        {
            private BasketList _basketList;
            public IEnumerable<BasketIteratorDebbugerProxy> Baskets
            {
                get
                {
                    List<BasketIteratorDebbugerProxy> result = new List<BasketIteratorDebbugerProxy>();
                    for (int i = 0; i < _basketList._baskets.Length; i++)
                    {
                        if (_basketList._baskets[i].count > 0)
                        {
                            result.Add(new BasketIteratorDebbugerProxy(_basketList[i]));
                        }
                    }
                    return result;
                }
            }
            public DebuggerProxy(BasketList basketList)
            {
                _basketList = basketList;
            }
            public struct BasketIteratorDebbugerProxy
            {
                private BasketIterator _iterrator;
                public int Count => _iterrator.Count;
                public IEnumerable<int> RelEntities
                {
                    get
                    {
                        List<int> result = new List<int>();
                        foreach (var e in _iterrator)
                        {
                            result.Add(e);
                        }
                        return result;
                    }
                }
                public BasketIteratorDebbugerProxy(BasketIterator iterrator)
                {
                    _iterrator = iterrator;
                }
                public override string ToString()
                {
                    return $"count: {_iterrator.Count}";
                }
            }
        }
        #endregion
    }
}
