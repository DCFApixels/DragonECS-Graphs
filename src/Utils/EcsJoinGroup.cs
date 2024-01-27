using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS.Relations.Utils
{
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    internal class EcsJoinGroup
    {
        private EcsArc _arc;
        private readonly bool _isLoop;

        private BasketList _startBaskets;
        private BasketList _endBaskets;
        private ArcInfo[] _arcMapping;

        public EcsJoinGroup(EcsArc arc)
        {
            _arc = arc;
            _isLoop = arc.IsLoop;

            _startBaskets = new BasketList();
            if (_isLoop)
            {
                _endBaskets = _startBaskets;
            }
            else
            {
                _endBaskets = new BasketList();
            }
            _arcMapping = new ArcInfo[arc.ArcWorld.Capacity];
        }
        public void Clear()
        {
            _startBaskets.Clear();
            if (!_isLoop)
            {
                _endBaskets.Clear();
            }
        }
        public void Add(int arcEntityID)
        {
            var (startEntityID, endEntityID) = _arc.GetArcInfo(arcEntityID);
            _startBaskets[startEntityID].Add(endEntityID);
            _startBaskets[endEntityID].Add(startEntityID);













            var (startEntityID, endEntityID) = _arc.GetArcInfo(arcEntityID);
            ArcInfo arcInfo = _arcMapping[arcEntityID];

            ref var startSpan = ref _startMapping[startEntityID];
            if (startSpan.nodeIndex <= 0)
            {
                startSpan.nodeIndex = _startList.Add(endEntityID);
                arcInfo.startNodeIndex = startSpan.nodeIndex;
            }
            else
            {
                arcInfo.startNodeIndex = _startList.InsertAfter(startSpan.nodeIndex, endEntityID);
            }
            startSpan.count++;

            ref var endSpan = ref _endMapping[endEntityID];
            if (endSpan.nodeIndex <= 0)
            {
                endSpan.nodeIndex = _endList.Add(startEntityID);
                arcInfo.endNodeIndex = endSpan.nodeIndex;
            }
            else
            {
                arcInfo.endNodeIndex = _endList.InsertAfter(endSpan.nodeIndex, startEntityID);
            }
            endSpan.count++;
        }

        public void Del(int arcEntityID)
        {
            var (startEntityID, endEntityID) = _arc.GetArcInfo(arcEntityID);
            ref ArcInfo arcInfo = ref _arcMapping[arcEntityID];
            int nextIndex;

            ref var startSpan = ref _startMapping[startEntityID];
            nextIndex = _startList.RemoveIndexAndReturnNextIndex(arcInfo.startNodeIndex);
            if (startSpan.nodeIndex == arcInfo.startNodeIndex)
            {
                startSpan.nodeIndex = nextIndex;
            }

            if (!_isLoop)
            {
                ref var endSpan = ref _endMapping[endEntityID];
                nextIndex = _endList.RemoveIndexAndReturnNextIndex(arcInfo.endNodeIndex); ;
                if (endSpan.nodeIndex == arcInfo.endNodeIndex)
                {
                    endSpan.nodeIndex = nextIndex;
                }
            }
            arcInfo = default;
        }
        public void DelEnd(int endEntityID)
        {
            DelPosInternal(_endMapping, _startList, _endList, endEntityID);
        }
        public void DelStart(int startEntityID)
        {
            DelPosInternal(_startMapping, _endList, _startList, startEntityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DelPosInternal(SpanInfo[] endMapping, IdsLinkedList startList, IdsLinkedList endList, int posEntityID)
        {
            ref var endSpan = ref endMapping[posEntityID];
            if (endSpan.nodeIndex <= 0)
            {
                return;
            }
            foreach (var startNodeIndex in endList.GetSpan(endSpan.nodeIndex, endSpan.count))
            {
                int indx = startList.RemoveIndexAndReturnNextIndex(startNodeIndex);
                if (endSpan.nodeIndex == startNodeIndex)
                {
                    endSpan.nodeIndex = indx;
                }
            }
            endList.RemoveSpan(endSpan.nodeIndex, endSpan.count);
            endSpan = SpanInfo.Empty;
        }


        public IdsLinkedList.Span GetSpanFor(int startEntityID)
        {
            ref var head = ref _startMapping[startEntityID];
            if (head.nodeIndex <= 0)
                return _startList.EmptySpan();
            else
                return _startList.GetSpan(head.nodeIndex, head.count);
        }
        public IdsLinkedList.LongSpan GetLongSpanFor(EcsWorld world, int startEntityID)
        {
            ref var head = ref _startMapping[startEntityID];
            if (head.nodeIndex <= 0)
                return _startList.EmptyLongSpan(world);
            else
                return _startList.GetLongSpan(world, head.nodeIndex, head.count);
        }

        private struct SpanInfo
        {
            public static readonly SpanInfo Empty = default;
            public int nodeIndex;
            public int count;
        }

        private struct ArcInfo
        {
            public int startNodeIndex;
            public int endNodeIndex;
        }

        #region DebuggerProxy
        internal class DebuggerProxy
        {
            private EcsJoinGroup _basket;

            public SpanDebugInfo[] HeadSpans => GetSpans(_basket._startList, _basket._startMapping);
            public SpanDebugInfo[] ValueSpans => GetSpans(_basket._endList, _basket._endMapping);

            private SpanDebugInfo[] GetSpans(IdsLinkedList list, SpanInfo[] mapping)
            {
                SpanDebugInfo[] result = new SpanDebugInfo[mapping.Length];
                for (int i = 0; i < mapping.Length; i++)
                    result[i] = new SpanDebugInfo(list, mapping[i].nodeIndex, mapping[i].count);
                return result;
            }
            public DebuggerProxy(EcsJoinGroup basket)
            {
                _basket = basket;
            }
            public struct SpanDebugInfo
            {
                private IdsLinkedList _list;
                public int index;
                public int count;
                public NodeDebugInfo[] Nodes
                {
                    get
                    {
                        var result = new NodeDebugInfo[this.count];
                        var nodes = _list.Nodes;
                        int index;
                        int count = this.count;
                        int next = this.index;
                        int i = 0;
                        while (true)
                        {
                            index = next;
                            next = nodes[next].next;
                            if (!(index > 0 && count-- > 0))
                                break;
                            var node = nodes[index];
                            result[i] = new NodeDebugInfo(index, node.prev, node.next, node.value);
                            i++;
                        }
                        return result;
                    }
                }
                public SpanDebugInfo(IdsLinkedList list, int index, int count)
                {
                    _list = list;
                    this.index = index;
                    this.count = count;
                }
                public override string ToString() => $"[{index}] {count}";
            }
            public struct NodeDebugInfo
            {
                public int index;
                public int prev;
                public int next;
                public int value;
                public NodeDebugInfo(int index, int prev, int next, int value)
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
