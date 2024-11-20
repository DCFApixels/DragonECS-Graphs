using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Graphs.Internal;

namespace DCFApixels.DragonECS
{
    public static class GraphQueriesExtensions
    {
        #region JoinToGraph Empty
        public static SubGraphMap JoinGraph(this EcsWorld entities, JoinMode mode = JoinMode.StartToEnd)
        {
            entities.GetQueryCache(out JoinToSubGraphExecutor executor, out EmptyAspect _);
            return executor.Execute(mode);
        }
        #endregion

        #region JoinToGraph Mask
        public static SubGraphMap JoinSubGraph<TCollection>(this TCollection entities, IComponentMask mask, JoinMode mode = JoinMode.StartToEnd)
            where TCollection : IEntityStorage
        {
            if (ReferenceEquals(entities, entities.World))
            {
                var executor = entities.World.GetExecutorForMask<JoinToSubGraphExecutor>(mask);
                return executor.Execute();
            }
            return entities.ToSpan().JoinSubGraph(mask, mode);
        }
        public static SubGraphMap JoinSubGraph(this EcsReadonlyGroup group, IComponentMask mask, JoinMode mode = JoinMode.StartToEnd)
        {
            return group.ToSpan().JoinSubGraph(mask, mode);
        }
        public static SubGraphMap JoinSubGraph(this EcsSpan span, IComponentMask mask, JoinMode mode = JoinMode.StartToEnd)
        {
            var executor = span.World.GetExecutorForMask<JoinToSubGraphExecutor>(mask);
            return executor.ExecuteFor(span, mode);
        }
        #endregion

        #region JoinToGraph
        public static SubGraphMap JoinSubGraph<TCollection, TAspect>(this TCollection entities, out TAspect aspect, JoinMode mode = JoinMode.StartToEnd)
            where TAspect : EcsAspect, new()
            where TCollection : IEntityStorage
        {
            if (ReferenceEquals(entities, entities.World))
            {
                entities.World.GetQueryCache(out JoinToSubGraphExecutor executor, out aspect);
                return executor.Execute();
            }
            return entities.ToSpan().JoinSubGraph(out aspect, mode);
        }
        public static SubGraphMap JoinSubGraph<TAspect>(this EcsReadonlyGroup group, out TAspect aspect, JoinMode mode = JoinMode.StartToEnd)
            where TAspect : EcsAspect, new()
        {
            return group.ToSpan().JoinSubGraph(out aspect, mode);
        }
        public static SubGraphMap JoinSubGraph<TAspect>(this EcsSpan span, out TAspect aspect, JoinMode mode = JoinMode.StartToEnd)
            where TAspect : EcsAspect, new()
        {
            span.World.GetQueryCache(out JoinToSubGraphExecutor executor, out aspect);
            return executor.ExecuteFor(span, mode);
        }
        #endregion
    }
}
