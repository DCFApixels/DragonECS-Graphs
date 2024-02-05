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
    internal unsafe class BasketList
    {
        public const int NULL = 0;

        private UnsafePointersArray<BasketNode> _basketNodePointers;
        private UnsafeArray<Node> _nodes;
        private int _recycledListHead = NULL;

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
            for (int i = 0; i < _basketNodePointers.Length; i++)
            {
                _basketNodePointers[i] = BasketNode.EmptyInstancePtr;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ResizeNodes(int newSize)
        {
            int oldSize = _nodes.Length;
            IntPtr offset = (IntPtr)_nodes.ptr;
            UnsafeArray.Resize(ref _nodes, newSize);
            offset = ((IntPtr)_nodes.ptr - offset) / 8;

            for (int i = 0; i < _basketNodePointers.Length; i++)
            {
                if (_basketNodePointers[i] != BasketNode.EmptyInstancePtr)
                {
                    _basketNodePointers.ptr[i] += offset;
                }
            }
            int leftNode = NULL;
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
            _basketNodePointers = new UnsafePointersArray<BasketNode>(64);
            for (int i = 0; i < 64; i++)
            {
                _basketNodePointers[i] = BasketNode.EmptyInstancePtr;
            }
            _nodes = new UnsafeArray<Node>(newSize, true);
            int leftNode = NULL;
            for (int i = 1; i < newSize; i++)
            {
                Link(i, leftNode);
                leftNode = i;
            }
            LinkToRecycled(newSize - 1, 1);
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public int GetBasketNodesCount(int basketIndex)
        //{
        //}
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasBasket(int basketIndex)
        {
            return _basketNodePointers[basketIndex] != BasketNode.EmptyInstancePtr;
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


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveNextNodeFromBasket(int basketIndex, int prevNodeIndex) // +
        {
#if DEBUG
            if (prevNodeIndex <= 0)
            {
                //Throw.ArgumentOutOfRange();
                return;
            }
#endif
            ref BasketNode* basketNode = ref _basketNodePointers[basketIndex];
            if (basketNode == BasketNode.EmptyInstancePtr)
            {
                //Поидее тут не должно быть пусто
                Throw.UndefinedException();
            }

            int targetNodeIndex = _nodes[prevNodeIndex].next;
            int nextNodeIndex = _nodes[targetNodeIndex].next;

            if(targetNodeIndex == 15)
            {

            }

            Link(prevNodeIndex, nextNodeIndex);
            LinkToRecycled(targetNodeIndex);

            if (basketNode->nodeIndex == targetNodeIndex)
            {
                basketNode->nodeIndex = nextNodeIndex;
            }
            if (--basketNode->count <= 0)
            {
                LinkToRecycled(prevNodeIndex);
                basketNode = BasketNode.EmptyInstancePtr;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateBasket(ref BasketNode* toPointer) // +
        {
            toPointer = (BasketNode*)TakeRecycledNodePtr();
            *toPointer = BasketNode.Empty;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AddToBasket(int basketIndex, int value) // +
        {
            ref BasketNode* basketNode = ref _basketNodePointers[basketIndex];
            int prevNodeIndex;
            int newNodeIndex = TakeRecycledNode();
            if (basketNode == BasketNode.EmptyInstancePtr)
            {
                CreateBasket(ref basketNode);
                prevNodeIndex = (int)((Node*)basketNode - _nodes.ptr);
                _nodes[newNodeIndex].value = value;
            }
            else
            {
                _nodes[newNodeIndex].Set(value, basketNode->nodeIndex);
                prevNodeIndex = basketNode->nodeIndex;
            }
            basketNode->nodeIndex = newNodeIndex;
            basketNode->count++;
            return prevNodeIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Node* TakeRecycledNodePtr() // +
        {
            if (_recycledListHead == NULL)
            {
                ResizeNodes(_nodes.Length << 1);
            }
            Node* resultNode = _nodes.ptr + _recycledListHead;
            _recycledListHead = resultNode->next;
            resultNode->next = 0;
            return resultNode;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int TakeRecycledNode() // +
        {
            if (_recycledListHead == NULL)
            {
                ResizeNodes(_nodes.Length << 1);
            }
            int resultNode = _recycledListHead;
            _recycledListHead = _nodes[resultNode].next;
            _nodes[resultNode].next = 0;
            return resultNode;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Link(int leftNodeIndex, int rightNodeIndex) // +
        {
            _nodes[leftNodeIndex].next = rightNodeIndex;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LinkToRecycled(int nodeIndex) // +
        {
            if(nodeIndex == 15)
            {

            }
            if (_recycledListHead >= 0)
            {
                _nodes[nodeIndex].next = _recycledListHead;
            }
            else
            {
                _nodes[nodeIndex].next = 0;
            }
            _recycledListHead = nodeIndex;

            int i = 0;
            int cureNodeIndex = _recycledListHead;
            while (cureNodeIndex != NULL)
            {
                int nextNodeIndex = _nodes[cureNodeIndex].next;
                if (i > _nodes.Length)
                {
                    Console.WriteLine("WTF");
                }
                cureNodeIndex = nextNodeIndex;
                i++;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LinkToRecycled(int startNodeIndex, int endNodeIndex) // +
        {
            if (_recycledListHead >= 0)
            {
                _nodes[endNodeIndex].next = _recycledListHead;
            }
            _recycledListHead = startNodeIndex;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveBasket(int basketIndex) // +
        {
            ref BasketNode* basketNode = ref _basketNodePointers[basketIndex];
            if (basketNode == null) { Throw.UndefinedException(); }

            int startBasketNodeIndex = (int)(basketNode - (BasketNode*)_nodes.ptr);

            int endNodeIndex = startBasketNodeIndex;
            for (int i = 0, n = basketNode->count; i < n; i++)
            {
                endNodeIndex = _nodes[endNodeIndex].next;
            }
            LinkToRecycled(startBasketNodeIndex, endNodeIndex);
            basketNode = BasketNode.EmptyInstancePtr;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void UpBasketsSize(int minSize)
        {
            if (minSize > _basketNodePointers.Length)
            {
                int newSize = ArrayUtility.NormalizeSizeToPowerOfTwo(minSize);
                int oldSize = _basketNodePointers.Length;
                UnsafePointersArray.Resize(ref _basketNodePointers, newSize);
                for (int i = oldSize; i < newSize; i++)
                {
                    _basketNodePointers[i] = BasketNode.EmptyInstancePtr;
                }
            }
        }


        #region Node
        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
        public struct Node
        {
            public static readonly Node Empty = new Node() { value = 0, next = NULL };
            public int value;
            /// <summary>next node index</summary>
            public int next;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Set(int value, int next)
            {
                this.value = value;
                this.next = next;
            }
            public override string ToString() => $"node(>{next} v:{value})";
        }
        #endregion
        #region BasketInfo
        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
        private struct BasketNode
        {
            public static readonly BasketNode Empty = new BasketNode() { nodeIndex = 0, count = 0, };
            public static BasketNode* EmptyInstancePtr;
            static BasketNode()
            {
                void* ptr = (void*)Marshal.AllocHGlobal(Marshal.SizeOf<BasketNode>(default));
                EmptyInstancePtr = (BasketNode*)ptr;
                *EmptyInstancePtr = default;
            }
            public int count;
            public int nodeIndex;
            public override string ToString() => $"basket_info(i:{nodeIndex} c:{count})";

            private static BasketNode* EmptyInstancePtr_Debug => EmptyInstancePtr;
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
                get { return _basketList._basketNodePointers[_basketIndex]->count; }
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
                    BasketNode* basketNode = iterator._basketList._basketNodePointers[iterator._basketIndex];

                    _nodes = iterator._basketList._nodes;
                    _nodeIndex = -1;
                    _nextNodeIndex = basketNode->nodeIndex;
                    _count = basketNode->count;
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
                    for (int i = 0; i < _basketList._basketNodePointers.Length; i++)
                    {
                        if (_basketList._basketNodePointers[i] != BasketNode.EmptyInstancePtr)
                        {
                            result.Add(new BasketIteratorDebbugerProxy(i, _basketList[i]));
                        }
                    }
                    return result;
                }
            }

            public int RecycledListHead => _basketList._recycledListHead;
            public IEnumerable<Node> Recycled
            {
                get
                {
                    List<Node> result = new List<Node>();
                    Node curNode = new Node();
                    curNode.index = _basketList._recycledListHead;

                    int i = 0;
                    while (curNode.index != NULL)
                    {
                        BasketList.Node x = _basketList.GetNode(curNode.index);
                        curNode.next = x.next;

                        result.Add(curNode);
                        if (i++ > _basketList._nodes.Length)
                        {
                            result.Add(new Node(int.MinValue, int.MinValue, int.MinValue));
                            break;
                        }

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
                        result.Add(new Node(i, _basketList._nodes[i].value, _basketList._nodes[i].next));
                    }
                    return result;
                }
            }
            public Node[][] Chains
            {
                get
                {
                    //bool IsChainLoop(int checkedNodeIndex)
                    //{
                    //    bool result = false;
                    //    int currentCheckedNodeIndex = checkedNodeIndex;
                    //    for (int i = 0; i <= _basketList._nodes.Length; i++)
                    //    {
                    //        if (currentCheckedNodeIndex == 0)
                    //        {
                    //            result = true;
                    //        }
                    //        currentCheckedNodeIndex = _basketList._nodes[currentCheckedNodeIndex].next;
                    //    }
                    //    return result;
                    //}
                    List<Stack<int>> chains = new List<Stack<int>>();
                    for (int i = 1; i < _basketList._nodes.Length; i++)
                    {
                        if (_basketList._nodes[i].next == 0)
                        {
                            Stack<int> chain = new Stack<int>();
                            int lastNext = i;
                            chain.Push(lastNext);

                            for (int queueX = 1; queueX < _basketList._nodes.Length; queueX++)
                            {
                                for (int j = 1; j < _basketList._nodes.Length; j++)
                                {
                                    var nodeJ = _basketList._nodes[j];
                                    if(nodeJ.next == lastNext)
                                    {
                                        lastNext = j;
                                        chain.Push(lastNext);
                                    }
                                }
                            }
                            chains.Add(chain);
                        }
                    }
                    var nodes = _basketList._nodes;
                    return chains.Select(
                        o => o.Select(o => new Node(o, nodes[o].value, nodes[o].next)).ToArray()
                        ).ToArray();
                }
            }
            public DebuggerProxy(BasketList basketList)
            {
                _basketList = basketList;
            }
            public struct Node
            {
                public int index;
                public int value;
                public int next;
                public Node(int index, int value, int next)
                {
                    this.index = index;
                    this.value = value;
                    this.next = next;
                }
                public override string ToString() => $"[{index}]          {value}          >{next}";
            }
            public struct BasketIteratorDebbugerProxy
            {
                public int index;
                private BasketIterator _iterrator;
                public int Count => _iterrator.Count;
                public IEnumerable<int> RelEntities
                {
                    get
                    {
                        List<int> result = new List<int>(_iterrator);
                        return result;
                    }
                }
                public BasketIteratorDebbugerProxy(int index, BasketIterator iterrator)
                {
                    this.index = index;
                    _iterrator = iterrator;
                }
                public override string ToString()
                {
                    return $"[{index}] count: {_iterrator.Count}";
                }
            }
        }
        #endregion
    }
}
