using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using static DCFApixels.DragonECS.Relations.Utils.EcsJoin;

namespace DCFApixels.DragonECS.Relations.Utils
{
    public readonly ref struct EcsReadonlyJoin
    {
        private readonly EcsJoin _source;

        #region Properties
        public bool IsNull => _source == null;
        public EcsArc Arc
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.Arc; }
        }
        public EcsWorld StartWorld
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.StartWorld; }
        }
        public EcsWorld EndWorld
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.EndWorld; }
        }
        public EcsArcWorld ArcWorld
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.ArcWorld; }
        }
        public int ArcWorldID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.ArcWorldID; }
        }
        public bool IsLoopArc
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.IsLoopArc; }
        }
        public EnumerableArcEnd this[int startEntityID]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source[startEntityID]; }
        }
        #endregion

        #region Constructors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsReadonlyJoin(EcsJoin source) { _source = source; }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int relEntityID) { return _source.Has(relEntityID); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasStart(int startEntityID) { return _source.HasStart(startEntityID); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasEnd(int endEntityID) { return _source.HasEnd(endEntityID); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EnumerableArcEnd GetRelEnds(int startEntityID) { return _source.GetRelEnds(startEntityID); }
        #endregion

        #region Internal
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EcsJoin GetSource_Internal() => _source;
        #endregion

        #region Other
        public override string ToString()
        {
            return _source != null ? _source.ToString() : "NULL";
        }
#pragma warning disable CS0809 // Устаревший член переопределяет неустаревший член
        [Obsolete("Equals() on EcsGroup will always throw an exception. Use the equality operator instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => throw new NotSupportedException();
        [Obsolete("GetHashCode() on EcsGroup will always throw an exception.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => throw new NotSupportedException();
#pragma warning restore CS0809 // Устаревший член переопределяет неустаревший член
        #endregion
    }
    //[DebuggerTypeProxy(typeof(DebuggerProxy))]
    public class EcsJoin
    {
        private readonly EcsArc _source;
        private readonly bool _isLoop;

        private readonly BasketList _startBaskets;
        private readonly BasketList _endBaskets;
        private readonly RelInfo[] _relMapping;

        #region Properties
        public EcsReadonlyJoin Readonly
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new EcsReadonlyJoin(this); }
        }
        public EcsArc Arc
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source; }
        }
        public EcsWorld StartWorld
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.StartWorld; }
        }
        public EcsWorld EndWorld
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.EndWorld; }
        }
        public EcsArcWorld ArcWorld
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.ArcWorld; }
        }
        public int ArcWorldID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.ArcWorldID; }
        }
        public bool IsLoopArc
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _isLoop; }
        }

        public EnumerableArcEnd this[int startEntityID]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return GetRelEnds(startEntityID); }
        }
        #endregion

        #region Constructors
        public EcsJoin(EcsArc arc)
        {
            _source = arc;
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
            _relMapping = new RelInfo[arc.ArcWorld.Capacity];
        }
        #endregion

        #region Add/Del
        public void Add(int relEntityID)
        {
            var (startEntityID, endEntityID) = _source.GetRelInfo(relEntityID);
            ref RelInfo arcInfo = ref _relMapping[relEntityID];

            arcInfo.startNodeIndex = _startBaskets.AddToBasket(startEntityID, relEntityID);
            if (_isLoop)
            {
                arcInfo.endNodeIndex = arcInfo.startNodeIndex;
            }
            else
            {
                arcInfo.endNodeIndex = _endBaskets.AddToBasket(endEntityID, relEntityID);
            }

            //arcInfo.endNodeIndex = _endBaskets.AddToBasket(endEntityID, relEntityID);
            //if (!_isLoop)
            //{
            //    arcInfo.startNodeIndex = _startBaskets.AddToBasket(startEntityID, relEntityID);
            //}
            //else
            //{
            //    arcInfo.startNodeIndex = arcInfo.endNodeIndex;
            //}
        }
        public void Del(int relEntityID)
        {
            var (startEntityID, endEntityID) = _source.GetRelInfo(relEntityID);
            ref RelInfo relInfo = ref _relMapping[relEntityID];
            _startBaskets.RemoveFromBasket(startEntityID, relInfo.startNodeIndex);
            if (!_isLoop)
            {
                _startBaskets.RemoveFromBasket(endEntityID, relInfo.endNodeIndex);
            }
        }
        public void DelStart(int startEntityID)
        {
            foreach (var relEntityID in _startBaskets.GetBasketIterator(startEntityID))
            {
                var endEntityID = _source.GetRelEnd(relEntityID);
                ref RelInfo relInfo = ref _relMapping[relEntityID];
                _endBaskets.RemoveFromBasket(endEntityID, relInfo.startNodeIndex);
            }
            _startBaskets.RemoveBasket(startEntityID);
        }
        public void DelEnd(int endEntityID)
        {
            foreach (var relEntityID in _endBaskets.GetBasketIterator(endEntityID))
            {
                var startEntityID = _source.GetRelStart(relEntityID);
                ref RelInfo relInfo = ref _relMapping[relEntityID];
                _startBaskets.RemoveFromBasket(startEntityID, relInfo.endNodeIndex);
            }
            _endBaskets.RemoveBasket(endEntityID);
        }
        #endregion

        #region Has
        public bool Has(int relEntityID)
        {
            return _relMapping[relEntityID] != RelInfo.Empty;
        }
        public bool HasStart(int startEntityID)
        {
            return _startBaskets.GetBasketNodesCount(startEntityID) > 0;
        }
        public bool HasEnd(int endEntityID)
        {
            return _endBaskets.GetBasketNodesCount(endEntityID) > 0;
        }
        #endregion

        #region Clear
        public void Clear()
        {
            _startBaskets.Clear();
            if (!_isLoop)
            {
                _endBaskets.Clear();
            }
        }
        #endregion

        #region GetRelEnds
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EnumerableArcEnd GetRelEnds(int startEntityID)
        {
            return new EnumerableArcEnd(_source, _startBaskets.GetBasketIterator(startEntityID).GetEnumerator());
        }
        #endregion

        #region EnumerableArcEnd
        public readonly ref struct EnumerableArcEnd //: IEnumerable<RelEnd>
        {
            private readonly EcsArc _arc;
            private readonly BasketList.BasketIterator.Enumerator _iterator;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal EnumerableArcEnd(EcsArc arc, BasketList.BasketIterator.Enumerator iterator)
            {
                _arc = arc;
                _iterator = iterator;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator GetEnumerator() { return new Enumerator(_arc, _iterator); }
            //IEnumerator<RelEnd> IEnumerable<RelEnd>.GetEnumerator() { return GetEnumerator(); }
            //IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
            public ref struct Enumerator //: IEnumerator<RelEnd>
            {
                private readonly EcsArc _arc;
                private BasketList.BasketIterator.Enumerator _iterator;
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                internal Enumerator(EcsArc arc, BasketList.BasketIterator.Enumerator iterator)
                {
                    _arc = arc;
                    _iterator = iterator;
                }
                public RelEnd Current
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get
                    {
                        int currentArc = _iterator.Current;
                        return new RelEnd(currentArc, _arc.GetRelEnd(currentArc));
                    }
                }
                //object IEnumerator.Current => Current;
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool MoveNext() { return _iterator.MoveNext(); }
                //[MethodImpl(MethodImplOptions.AggressiveInlining)]
                //public void Reset() { }
                //[MethodImpl(MethodImplOptions.AggressiveInlining)]
                //public void Dispose() { }
            }
        }
        #endregion

        #region ArcInfo
        private struct RelInfo : IEquatable<RelInfo>
        {
            public readonly static RelInfo Empty = default;
            public int startNodeIndex;
            public int endNodeIndex;

            #region Object
            public override bool Equals(object obj)
            {
                return obj is RelInfo && Equals((RelInfo)obj);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(RelInfo other)
            {
                return startNodeIndex == other.startNodeIndex &&
                    endNodeIndex == other.endNodeIndex;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode()
            {
                return ~startNodeIndex ^ endNodeIndex;
            }
            #endregion

            #region operators
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(RelInfo a, RelInfo b) => a.startNodeIndex == b.startNodeIndex && a.endNodeIndex == b.endNodeIndex;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(RelInfo a, RelInfo b) => a.startNodeIndex != b.startNodeIndex || a.endNodeIndex != b.endNodeIndex;
            #endregion
        }
        #endregion

        #region Operators
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EcsReadonlyJoin(EcsJoin a) => a.Readonly;
        #endregion

        #region DebuggerProxy
        //internal class DebuggerProxy
        //{
        //    private EcsJoinGroup _basket;
        //
        //    public SpanDebugInfo[] HeadSpans => GetSpans(_basket._startList, _basket._startMapping);
        //    public SpanDebugInfo[] ValueSpans => GetSpans(_basket._endList, _basket._endMapping);
        //
        //    private SpanDebugInfo[] GetSpans(IdsLinkedList list, SpanInfo[] mapping)
        //    {
        //        SpanDebugInfo[] result = new SpanDebugInfo[mapping.Length];
        //        for (int i = 0; i < mapping.Length; i++)
        //            result[i] = new SpanDebugInfo(list, mapping[i].nodeIndex, mapping[i].count);
        //        return result;
        //    }
        //    public DebuggerProxy(EcsJoinGroup basket)
        //    {
        //        _basket = basket;
        //    }
        //    public struct SpanDebugInfo
        //    {
        //        private IdsLinkedList _list;
        //        public int index;
        //        public int count;
        //        public NodeDebugInfo[] Nodes
        //        {
        //            get
        //            {
        //                var result = new NodeDebugInfo[this.count];
        //                var nodes = _list.Nodes;
        //                int index;
        //                int count = this.count;
        //                int next = this.index;
        //                int i = 0;
        //                while (true)
        //                {
        //                    index = next;
        //                    next = nodes[next].next;
        //                    if (!(index > 0 && count-- > 0))
        //                        break;
        //                    var node = nodes[index];
        //                    result[i] = new NodeDebugInfo(index, node.prev, node.next, node.value);
        //                    i++;
        //                }
        //                return result;
        //            }
        //        }
        //        public SpanDebugInfo(IdsLinkedList list, int index, int count)
        //        {
        //            _list = list;
        //            this.index = index;
        //            this.count = count;
        //        }
        //        public override string ToString() => $"[{index}] {count}";
        //    }
        //    public struct NodeDebugInfo
        //    {
        //        public int index;
        //        public int prev;
        //        public int next;
        //        public int value;
        //        public NodeDebugInfo(int index, int prev, int next, int value)
        //        {
        //            this.index = index;
        //            this.prev = prev;
        //            this.next = next;
        //            this.value = value;
        //        }
        //        public override string ToString() => $"[{index}] {prev}_{next} - {value}";
        //    }
        //}
        #endregion
    }
}
