using DCFApixels.DragonECS.Relations.Internal;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    internal class BasketList
    {
        public const int NULL = 0;

        private UnsafeArray<BasketInfo> _baskets = new UnsafeArray<BasketInfo>(64, true);
        private UnsafeArray<Node> _nodes;
        private int _recycledListHead = NULL;

        #region Constructors/Destroy
        public BasketList() : this(16) { }
        public BasketList(int minCapacity)
        {
            Initialize(ArrayUtility.NormalizeSizeToPowerOfTwo(minCapacity));
        }
        //Dispose //GC.SuppressFinalize
        ~BasketList()
        {
            _baskets.Dispose();
            _nodes.Dispose();
        }
        #endregion

        #region Clear
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
        #endregion

        #region Other
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
        private Node GetNode(int nodeIndex)
        {
            return _nodes[nodeIndex];
        }
        #endregion

        #region AddToBasket/TakeRecycledNode
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int TakeRecycledNode()
        {
            if (_recycledListHead == NULL)
            {
                ResizeNodes(_nodes.Length << 1);
            }
            int node = _recycledListHead;
            _recycledListHead = _nodes[node].next;
            return node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AddToBasket(int basketIndex, int value)
        {
            ref BasketInfo basketInfo = ref _baskets[basketIndex];
            int newNodeIndex = TakeRecycledNode();
            if (basketInfo.count == 0)
            {
                _nodes[newNodeIndex].SetValue_Prev(value, 0);
            }
            else
            {
                int nodeIndex = basketInfo.nodeIndex;
                ref int nextNode_Prev = ref _nodes[nodeIndex].prev;

                _nodes[newNodeIndex].Set(value, nextNode_Prev, nodeIndex);
                nextNode_Prev = newNodeIndex;
            }
            basketInfo.nodeIndex = newNodeIndex;
            basketInfo.count++;
            return newNodeIndex;
        }
        #endregion

        #region RemoveFromBasket/RemoveBasket
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveFromBasket(int basketIndex, int nodeIndex)
        {//нужно добавить ограничение на удаление повторяющейся ноды, иначе recycled ноды зацикливаются
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
            if (basketInfo.nodeIndex == nodeIndex)
            {
                basketInfo.nodeIndex = nextNode;
            }
            basketInfo.count--;
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
        #endregion

        #region Links
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
            if (_recycledListHead <= NULL)
            {
                _nodes[endNodeIndex].next = NULL;
            }
            else
            {
                Link(startNodeIndex, _recycledListHead);
            }
            _recycledListHead = startNodeIndex;
        }
        #endregion

        #region UpSize/Resize/Initialize
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ResizeNodes(int newSize)
        {
            int oldSize = _nodes.Length;
            UnsafeArray.Resize(ref _nodes, newSize);
            InitNewNodes(oldSize, newSize);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Initialize(int newSize)
        {
            _nodes = new UnsafeArray<Node>(newSize);
            _nodes[0] = Node.Empty;
            InitNewNodes(1, newSize);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void InitNewNodes(int oldSize, int newSize)
        {
            int leftNode = NULL;
            for (int i = oldSize; i < newSize; i++)
            {
                Link(leftNode, i);
                leftNode = i;
            }
            LinkToRecycled(oldSize, newSize - 1);
        }

        public void UpNodesSize(int minSize)
        {
            if (minSize > _nodes.Length)
            {
                int newSize = ArrayUtility.NormalizeSizeToPowerOfTwo(minSize);
                ResizeNodes(newSize);
            }
        }
        public void UpBasketsSize(int minSize)
        {
            if (minSize > _baskets.Length)
            {
                int newSize = ArrayUtility.NormalizeSizeToPowerOfTwo(minSize);
                UnsafeArray.ResizeAndInit(ref _baskets, newSize);
            }
        }
        #endregion

        #region Node
        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
        public struct Node
        {
            public static readonly Node Empty = new Node() { value = 0, next = NULL };
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
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetValue_Prev(int value, int prev)
            {
                this.value = value;
                this.prev = prev;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetPrev_Next(int prev, int next)
            {
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
                private readonly UnsafeArray<Node> _nodes;
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
            public IEnumerable<Node> Recycled
            {
                get
                {
                    List<Node> result = new List<Node>();
                    Node curNode = new Node();
                    curNode.index = _basketList._recycledListHead;

                    for (int i = 0; i < _basketList._nodes.Length; i++)
                    {
                        if (curNode.index == NULL)
                        {
                            break;
                        }
                        BasketList.Node x = _basketList.GetNode(curNode.index);
                        curNode.prev = x.prev;
                        curNode.next = x.next;

                        result.Add(curNode);

                        curNode.index = curNode.next;
                    }
                    return result;
                }
            }
            public IEnumerable<Node> AllNodes
            {
                get
                {
                    List<Node> result = new List<Node>();

                    for (int i = 0; i < _basketList._nodes.Length; i++)
                    {
                        result.Add(new Node(_basketList._nodes[i].prev, i, _basketList._nodes[i].next));
                    }
                    return result;
                }
            }
            public DebuggerProxy(BasketList basketList)
            {
                _basketList = basketList;
            }
            public struct Node
            {
                public int prev;
                public int index;
                public int next;
                public Node(int prev, int index, int next)
                {
                    this.prev = prev;
                    this.index = index;
                    this.next = next;
                }
                public override string ToString() => $"node({prev}< {index} >{next})";
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
