using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Graphs.Internal;

namespace DCFApixels.DragonECS
{
    public static class GraphQueriesExtensions
    {
        #region JoinToGraph Empty
        public static EcsSubGraph JoinToSubGraph(this EcsWorld entities, EcsSubGraphMode mode = EcsSubGraphMode.StartToEnd)
        {
            entities.GetQueryCache(out JoinToSubGraphExecutor executor, out EmptyAspect _);
            return executor.Execute(mode);
        }
        #endregion

        #region JoinToGraph Mask
        public static EcsSubGraph JoinToSubGraph<TCollection>(this TCollection entities, IComponentMask mask, EcsSubGraphMode mode = EcsSubGraphMode.StartToEnd)
            where TCollection : IEntityStorage
        {
            if (ReferenceEquals(entities, entities.World))
            {
                var executor = entities.World.GetExecutorForMask<JoinToSubGraphExecutor>(mask);
                return executor.Execute();
            }
            return entities.ToSpan().JoinToSubGraph(mask, mode);
        }
        public static EcsSubGraph JoinToSubGraph(this EcsReadonlyGroup group, IComponentMask mask, EcsSubGraphMode mode = EcsSubGraphMode.StartToEnd)
        {
            return group.ToSpan().JoinToSubGraph(mask, mode);
        }
        public static EcsSubGraph JoinToSubGraph(this EcsSpan span, IComponentMask mask, EcsSubGraphMode mode = EcsSubGraphMode.StartToEnd)
        {
            var executor = span.World.GetExecutorForMask<JoinToSubGraphExecutor>(mask);
            return executor.ExecuteFor(span, mode);
        }
        #endregion

        #region JoinToGraph
        public static EcsSubGraph JoinToSubGraph<TCollection, TAspect>(this TCollection entities, out TAspect aspect, EcsSubGraphMode mode = EcsSubGraphMode.StartToEnd)
            where TAspect : EcsAspect, new()
            where TCollection : IEntityStorage
        {
            if (ReferenceEquals(entities, entities.World))
            {
                entities.World.GetQueryCache(out JoinToSubGraphExecutor executor, out aspect);
                return executor.Execute();
            }
            return entities.ToSpan().JoinToSubGraph(out aspect, mode);
        }
        public static EcsSubGraph JoinToSubGraph<TAspect>(this EcsReadonlyGroup group, out TAspect aspect, EcsSubGraphMode mode = EcsSubGraphMode.StartToEnd)
            where TAspect : EcsAspect, new()
        {
            return group.ToSpan().JoinToSubGraph(out aspect, mode);
        }
        public static EcsSubGraph JoinToSubGraph<TAspect>(this EcsSpan span, out TAspect aspect, EcsSubGraphMode mode = EcsSubGraphMode.StartToEnd)
            where TAspect : EcsAspect, new()
        {
            span.World.GetQueryCache(out JoinToSubGraphExecutor executor, out aspect);
            return executor.ExecuteFor(span, mode);
        }
        #endregion
    }
}
