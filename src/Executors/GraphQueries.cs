namespace DCFApixels.DragonECS
{
    public static class GraphQueries
    {
        #region JoinToGraph Empty
        //public static EcsSubGraph JoinToSubGraph<TCollection>(this TCollection entities, EcsSubGraphMode mode = EcsSubGraphMode.StartToEnd)
        //    where TCollection : IEntityStorage
        //{
        //    return entities.ToSpan().JoinToSubGraph(mode);
        //}
        //public static EcsSubGraph JoinToSubGraph(this EcsReadonlyGroup group, EcsSubGraphMode mode = EcsSubGraphMode.StartToEnd)
        //{
        //    return group.ToSpan().JoinToSubGraph(mode);
        //}
        //public static EcsSubGraph JoinToSubGraph(this EcsSpan span, EcsSubGraphMode mode = EcsSubGraphMode.StartToEnd)
        //{
        //    EcsWorld world = span.World;
        //    if (world.IsEnableReleaseDelEntBuffer)
        //    {
        //        world.ReleaseDelEntityBufferAll();
        //    }
        //    world.GetQueryCache(out EcsJoinToSubGraphExecutor executor, out EmptyAspect _);
        //    return executor.ExecuteFor(span, mode);
        //}
        #endregion

        #region JoinToGraph
        public static EcsSubGraph JoinToSubGraph<TCollection>(this TCollection entities, EcsSubGraphMode mode = EcsSubGraphMode.StartToEnd)
            where TCollection : IEntityStorage
        {
            if (ReferenceEquals(entities, entities.World))
            {
                entities.World.GetQueryCache(out EcsJoinToSubGraphExecutor executor, out EmptyAspect _);
                return executor.Execute(mode);
            }
            return entities.ToSpan().JoinToSubGraph(out EmptyAspect _, mode);
        }

        public static EcsSubGraph JoinToSubGraph<TCollection, TAspect>(this TCollection entities, out TAspect aspect, EcsSubGraphMode mode = EcsSubGraphMode.StartToEnd)
            where TAspect : EcsAspect, new()
            where TCollection : IEntityStorage
        {
            if (ReferenceEquals(entities, entities.World))
            {
                entities.World.GetQueryCache(out EcsJoinToSubGraphExecutor executor, out aspect);
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
            span.World.GetQueryCache(out EcsJoinToSubGraphExecutor executor, out aspect);
            return executor.ExecuteFor(span, mode);
        }
        #endregion
    }
}
