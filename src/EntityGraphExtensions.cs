#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Graphs.Internal;
using System;

namespace DCFApixels.DragonECS
{
    public static class EntityGraphExtensions
    {
        private static EntityGraph[] _worldGraphs = new EntityGraph[4];

        public static EntityGraph CreateGraph(this EcsWorld self)
        {
            return self.CreateGraph(self);
        }
        public static EntityGraph CreateGraph(this EcsWorld self, EcsWorld graphWorld)
        {
            int worldID = self.ID;
            if (_worldGraphs.Length <= worldID)
            {
                Array.Resize(ref _worldGraphs, worldID + 4);
            }
            ref EntityGraph graph = ref _worldGraphs[worldID];
            if (graph != null)
            {
                Throw.UndefinedException();
            }
            graph = new EntityGraph(self, graphWorld);
            new Destroyer(graph);
            _worldGraphs[graphWorld.ID] = graph;
            return graph;
        }

        public static EntityGraph CreateOrGetGraph(this EcsWorld self)
        {
            return self.CreateGraph(self);
        }
        public static EntityGraph CreateOrGetGraph(this EcsWorld self, EcsWorld graphWorld)
        {
            int worldID = self.ID;
            if (_worldGraphs.Length <= worldID)
            {
                Array.Resize(ref _worldGraphs, worldID + 4);
            }
            ref EntityGraph graph = ref _worldGraphs[worldID];
            if (graph != null)
            {
                return graph;
            }
            graph = new EntityGraph(self, graphWorld);
            new Destroyer(graph);
            _worldGraphs[graphWorld.ID] = graph;
            return graph;
        }

        public static bool TryGetGraph(this EcsWorld self, out EntityGraph graph)
        {
            int worldID = self.ID;
            if (_worldGraphs.Length <= worldID)
            {
                Array.Resize(ref _worldGraphs, worldID + 4);
            }
            graph = _worldGraphs[worldID];
            return graph != null;
        }

        public static EntityGraph GetGraph(this EcsWorld self)
        {
            if (self.TryGetGraph(out EntityGraph graph))
            {
                return graph;
            }
            Throw.UndefinedException();
            return null;
        }
        public static EcsWorld GetGraphWorld(this EcsWorld self)
        {
            if (self.TryGetGraph(out EntityGraph graph))
            {
                return graph.GraphWorld;
            }
            Throw.UndefinedException();
            return null;
        }

        public static bool IsGraphWorld(this EcsWorld self)
        {
            if (self.TryGetGraph(out EntityGraph graph))
            {
                return graph.GraphWorld == self;
            }
            return false;
        }

        #region Internal Destroy
        private static void TryDestroyGraph(EntityGraph graph)
        {
            short worldID = graph.WorldID;
            if (_worldGraphs.Length <= worldID)
            {
                Array.Resize(ref _worldGraphs, worldID + 4);
            }
            short graphWorldID = graph.GraphWorldID;
            if (_worldGraphs.Length <= graphWorldID)
            {
                Array.Resize(ref _worldGraphs, graphWorldID + 4);
            }
            _worldGraphs[worldID] = null;
            _worldGraphs[graphWorldID] = null;
        }
        private class Destroyer : IEcsWorldEventListener
        {
            private EntityGraph _graph;
            public Destroyer(EntityGraph graph)
            {
                _graph = graph;
                graph.World.AddListener(this);
                graph.GraphWorld.AddListener(this);
            }
            public void OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer) { }
            public void OnWorldDestroy()
            {
                TryDestroyGraph(_graph);
            }
            public void OnWorldResize(int newSize) { }
        }
        #endregion
    }
}