using DCFApixels.DragonECS;
using DCFApixels.DragonECS.Relations.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace DragonECS.DragonECS
{
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public class IdsBasket
    {
        private IdsLinkedList _headList = new IdsLinkedList(4);
        private IdsLinkedList _valueList = new IdsLinkedList(4);
        private SpanInfo[] _headMapping;
        private SpanInfo[] _valueMapping;
        private int _size;

        public IdsBasket(int size)
        {
            _headMapping = new SpanInfo[size];
            _valueMapping = new SpanInfo[size];
            _size = size;
        }

        public void Resize(int newSize)
        {
            Array.Resize(ref _headMapping, newSize);
            Array.Resize(ref _valueMapping, newSize);
            _size = newSize;
        }
        public void Clear()
        {
            _headList.Clear();
            _valueList.Clear();
            ArrayUtility.Fill(_headMapping, SpanInfo.Empty);
            ArrayUtility.Fill(_valueMapping, SpanInfo.Empty);
            _size = 0;
        }
        public void AddToHead(int headID, int id)
        {
            int _savedNode;
            ref var headSpan = ref _headMapping[headID];
            if (headSpan.startNodeIndex <= 0)
            {
                _savedNode = _headList.Add(id);
                headSpan.startNodeIndex = _savedNode;
            }
            else
            {
                _savedNode = _headList.InsertAfter(headSpan.startNodeIndex, id);
            }
            headSpan.count++;

            ref var valueSpan = ref _valueMapping[id];
            if (valueSpan.startNodeIndex <= 0)
                valueSpan.startNodeIndex = _valueList.Add(_savedNode);
            else
                _valueList.InsertAfter(valueSpan.startNodeIndex, _savedNode);
            valueSpan.count++;
        }
        public void Del(int id)
        {
            ref var valueSpan = ref _valueMapping[id];
            ref var headSpan = ref _headMapping[id];
            if (valueSpan.startNodeIndex <= 0)
                return;
            foreach (var nodeIndex in _valueList.GetSpan(valueSpan.startNodeIndex, valueSpan.count))
                _headList.Remove(nodeIndex);
            _valueList.RemoveSpan(valueSpan.startNodeIndex, valueSpan.count);
            valueSpan = SpanInfo.Empty;
        }

        public void DelHead(int headID)
        {
            ref var headSpan = ref _headMapping[headID];
            _valueList.RemoveSpan(headSpan.startNodeIndex, headSpan.count);
            headSpan = SpanInfo.Empty;
        }

        public IdsLinkedList.Span GetSpanFor(int value)
        {
            ref var head = ref _headMapping[value];
            if (head.startNodeIndex <= 0)
                return _headList.EmptySpan();
            else
                return _headList.GetSpan(head.startNodeIndex, head.count);
        }

        private struct SpanInfo
        {
            public readonly static SpanInfo Empty = default;
            public int startNodeIndex;
            public int count;
        }

        #region DebuggerProxy
        internal class DebuggerProxy
        {
            private IdsBasket _basket;

            public SpanDebugInfo[] HeadSpans => GetSpans(_basket._headList, _basket._headMapping);
            public SpanDebugInfo[] ValueSpans => GetSpans(_basket._valueList, _basket._valueMapping);

            private SpanDebugInfo[] GetSpans(IdsLinkedList list, SpanInfo[] mapping)
            {
                SpanDebugInfo[] result = new SpanDebugInfo[mapping.Length];
                for (int i = 0; i < mapping.Length; i++)
                    result[i] = new SpanDebugInfo(list, mapping[i].startNodeIndex, mapping[i].count);
                return result;
            }
            public DebuggerProxy(IdsBasket basket)
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
