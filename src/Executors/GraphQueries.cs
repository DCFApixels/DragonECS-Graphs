using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Graphs.Internal;

namespace DCFApixels.DragonECS
{
    public static class GraphQueriesExtensions
    {
        #region JoinToGraph Empty
        public static SubGraphMap Join(this EcsWorld entities, JoinMode mode = JoinMode.Start)
        {
            entities.GetQueryCache(out JoinExecutor executor, out EmptyAspect _);
            return executor.Execute(mode);
        }
        #endregion

        #region JoinToGraph Mask
        public static SubGraphMap Join<TCollection>(this TCollection entities, IComponentMask mask, JoinMode mode = JoinMode.Start)
            where TCollection : IEntityStorage
        {
            if (ReferenceEquals(entities, entities.World))
            {
                var executor = entities.World.GetExecutorForMask<JoinExecutor>(mask);
                return executor.Execute();
            }
            return entities.ToSpan().JoinSubGraph(mask, mode);
        }
        public static SubGraphMap Join(this EcsReadonlyGroup group, IComponentMask mask, JoinMode mode = JoinMode.Start)
        {
            return group.ToSpan().JoinSubGraph(mask, mode);
        }
        public static SubGraphMap JoinSubGraph(this EcsSpan span, IComponentMask mask, JoinMode mode = JoinMode.Start)
        {
            var executor = span.World.GetExecutorForMask<JoinExecutor>(mask);
            return executor.ExecuteFor(span, mode);
        }
        #endregion

        #region JoinToGraph
        public static SubGraphMap Join<TCollection, TAspect>(this TCollection entities, out TAspect aspect, JoinMode mode = JoinMode.Start)
            where TAspect : EcsAspect, new()
            where TCollection : IEntityStorage
        {
            if (ReferenceEquals(entities, entities.World))
            {
                entities.World.GetQueryCache(out JoinExecutor executor, out aspect);
                return executor.Execute();
            }
            return entities.ToSpan().Join(out aspect, mode);
        }
        public static SubGraphMap JoinSubGraph<TAspect>(this EcsReadonlyGroup group, out TAspect aspect, JoinMode mode = JoinMode.Start)
            where TAspect : EcsAspect, new()
        {
            return group.ToSpan().Join(out aspect, mode);
        }
        public static SubGraphMap Join<TAspect>(this EcsSpan span, out TAspect aspect, JoinMode mode = JoinMode.Start)
            where TAspect : EcsAspect, new()
        {
            span.World.GetQueryCache(out JoinExecutor executor, out aspect);
            return executor.ExecuteFor(span, mode);
        }
        #endregion
    }
}
