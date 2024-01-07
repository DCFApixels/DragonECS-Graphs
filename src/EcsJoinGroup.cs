using DCFApixels.DragonECS.Relations.Utils;

namespace DCFApixels.DragonECS
{
    public class EcsJoinGroup
    {
        private EcsArc _source;

        private int[] _mapping;
        private int[] _counts;
        private IdsLinkedList _linkedList;
        internal bool _isReleased = true;

        #region Properites
        public EcsArc Edge => _source;
        #endregion


        #region Constrcutors/Dispose
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static EcsJoinGroup New(EcsArcController edge)
        //{
        //    return edge.GetFreeGroup();
        //}
        //internal EcsJoinGroup(EcsArcController edge, int denseCapacity = 64)
        //{
        //    _source = edge;
        //    _source.RegisterGroup(this);
        //    int capacity = edge.World.Capacity;
        //
        //    _mapping = new int[capacity];
        //    _counts = new int[capacity];
        //}
        //public void Dispose() => _source.ReleaseGroup(this);
        #endregion

        public void Add(int entityFrom, int entityTo)
        {
            ref int nodeIndex = ref _mapping[entityFrom];
            if (nodeIndex <= 0)
            {
                nodeIndex = _linkedList.Add(entityTo);
                _counts[entityFrom] = 1;
            }
            else
            {
                _linkedList.InsertAfter(nodeIndex, entityTo);
                _counts[entityFrom]++;
            }
        }

        public IdsLinkedList.Span GetEntitiesFor(int entity)
        {
            ref var nodeIndex = ref _mapping[entity];
            if (nodeIndex <= 0)
                return _linkedList.EmptySpan();
            else
                return _linkedList.GetSpan(nodeIndex, _counts[entity]);
        }

        public void Clear()
        {
            _linkedList.Clear();
            for (int i = 0; i < _mapping.Length; i++)
                _mapping[i] = 0;
        }
    }
}
