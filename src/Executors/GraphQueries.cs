#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Graphs.Internal;

namespace DCFApixels.DragonECS
{
    public static class GraphQueriesExtensions
    {
        private struct JoinExecutorsManager : IEcsWorldComponent<JoinExecutorsManager>
        {
            const int N = 32;
            private JoinExecutor[] _joinExecutorsQueue;
            private int _increment;
            void IEcsWorldComponent<JoinExecutorsManager>.Init(ref JoinExecutorsManager component, EcsWorld graphWorld)
            {
                if (graphWorld.IsGraphWorld() == false)
                {
                    Throw.Exception($"The {nameof(Join)} query can only be used for EntityGraph.GraphWorld or a collection of that _graphWorld.");
                }
                component._joinExecutorsQueue = new JoinExecutor[N];
                for (int i = 0; i < N; i++)
                {
                    component._joinExecutorsQueue[i] = new JoinExecutor(graphWorld);
                }
                _increment = 0;
            }
            void IEcsWorldComponent<JoinExecutorsManager>.OnDestroy(ref JoinExecutorsManager component, EcsWorld graphWorld)
            {
                _joinExecutorsQueue = null;
            }
            public JoinExecutor GetExecutor()
            {
                _increment = ++_increment % N;
                return _joinExecutorsQueue[_increment];
            }
        }
        public static SubGraphMap Join<TCollection>(this TCollection entities, JoinMode mode = JoinMode.Start)
            where TCollection : IEntityStorage
        {
            var executor = entities.World.Get<JoinExecutorsManager>().GetExecutor();
            return entities.ToSpan().Join(mode);
        }
        public static SubGraphMap Join(this EcsReadonlyGroup group, JoinMode mode = JoinMode.Start)
        {
            return group.ToSpan().Join(mode);
        }
        public static SubGraphMap Join(this EcsSpan span, JoinMode mode = JoinMode.Start)
        {
            var executor = span.World.Get<JoinExecutorsManager>().GetExecutor();
            return executor.ExecuteFor(span, mode);
        }
    }
}
