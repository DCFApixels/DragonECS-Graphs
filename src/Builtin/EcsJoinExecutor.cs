/*
namespace DCFApixels.DragonECS
{
    public class EcsJoinExecutor<TAspect> : EcsQueryExecutor, IEcsWorldEventListener
        where TAspect : EcsAspect
    {
        private TAspect _aspect;
        //internal EcsGroup _filteredGroup;

        private IdsLinkedList _linkedBasket;
        private int[] _mapping;
        private int[] _counts;

        private long _executeVersion;

        private int _targetWorldCapacity = -1;
        private EcsProfilerMarker _executeMarker = new EcsProfilerMarker("JoinAttach");

        #region Properties
        public TAspect Aspect => _aspect;
        internal long ExecuteVersion => _executeVersion;
        #endregion

        #region OnInitialize/OnDestroy
        protected override void OnInitialize()
        {
            _linkedBasket = new IdsLinkedList(128);
            World.AddListener(this);
            _mapping = new int[World.Capacity];
            _counts = new int[World.Capacity];
        }
        protected override void OnDestroy()
        {
            World.RemoveListener(this);
        }
        #endregion

        public void Clear()
        {
            _linkedBasket.Clear();
            ArrayUtility.Fill(_mapping, 0, 0, _mapping.Length);
            ArrayUtility.Fill(_counts, 0, 0, _counts.Length);
        }

        public IdsLinkedList.Span GetEntitiesFor(int entity)
        {
            ref var nodeIndex = ref _mapping[entity];
            if (nodeIndex <= 0)
                return _linkedBasket.EmptySpan();
            else
                return _linkedBasket.GetSpan(nodeIndex, _counts[entity]);
        }

        #region Execute
        public EcsJoinAttachResult<TAspect> Execute() => ExecuteFor(World.Entities);
        public EcsJoinAttachResult<TAspect> ExecuteFor(EcsReadonlyGroup sourceGroup)
        {
            _executeMarker.Begin();
            var world = _aspect.World;
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (sourceGroup.IsNull) throw new ArgumentNullException();//TODO составить текст исключения. 
#endif
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            else
                if (World != sourceGroup.World) throw new ArgumentException();//TODO составить текст исключения. это проверка на то что пользователь использует правильный мир
#endif

            //Подготовка массивов
            if (_targetWorldCapacity < World.Capacity)
            {
                _targetWorldCapacity = World.Capacity;
                _mapping = new int[_targetWorldCapacity];
                _counts = new int[_targetWorldCapacity];
            }
            else
            {
                ArrayUtility.Fill(_counts, 0);
                ArrayUtility.Fill(_mapping, 0);
            }
            _linkedBasket.Clear();
            //Конец подготовки массивов

            EcsEdge edge = World.GetEdgeWithSelf();

            var iterator = new EcsAspectIterator(_aspect, sourceGroup);
            foreach (var arcEntityID in iterator)
            {
                var rel = edge.GetRelationTargets(arcEntityID);


                int sorceEntityID = rel.entity;
                //if (!CheckMaskInternal(targetWorldWhereQuery.query.mask, attachTargetID)) continue; //TODO проверить что все работает //исчключить все аттачи, цели которых не входят в targetWorldWhereQuery 

                ref int nodeIndex = ref _mapping[sorceEntityID];
                if (nodeIndex <= 0)
                    nodeIndex = _linkedBasket.Add(arcEntityID);
                else
                    _linkedBasket.InsertAfter(nodeIndex, arcEntityID);
                _counts[sorceEntityID]++;
            }

            _executeVersion++;
            _executeMarker.End();

            return new EcsJoinAttachResult<TAspect>(_aspect, this, _executeVersion);
        }
        #endregion

        #region IEcsWorldEventListener
        void IEcsWorldEventListener.OnWorldResize(int newSize)
        {
            Array.Resize(ref _mapping, newSize);
            Array.Resize(ref _counts, newSize);
        }
        void IEcsWorldEventListener.OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer)
        {
        }
        void IEcsWorldEventListener.OnWorldDestroy()
        {
        }
        #endregion
    }
}
*/
