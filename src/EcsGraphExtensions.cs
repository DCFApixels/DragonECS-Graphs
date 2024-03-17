using DCFApixels.DragonECS.Graphs.Internal;
using System;

namespace DCFApixels.DragonECS
{
    public static class EcsGraphExtensions
    {
        private static EcsGraph[] _worldGraphs = new EcsGraph[4];

        public static EcsGraph CreateGraph(this EcsWorld self, EcsWorld graphWorld)
        {
            int worldID = self.id;
            if (_worldGraphs.Length <= worldID)
            {
                Array.Resize(ref _worldGraphs, worldID + 4);
            }
            ref EcsGraph graph = ref _worldGraphs[worldID];
            if (graph != null)
            {
                Throw.UndefinedException();
            }
            graph = new EcsGraph(self, graphWorld);
            new Destroyer(graph);
            _worldGraphs[graphWorld.id] = graph;
            return graph;
        }

        public static EcsGraph CreateOrGetGraph(this EcsWorld self, EcsWorld graphWorld)
        {
            int worldID = self.id;
            if (_worldGraphs.Length <= worldID)
            {
                Array.Resize(ref _worldGraphs, worldID + 4);
            }
            ref EcsGraph graph = ref _worldGraphs[worldID];
            if (graph != null)
            {
                return graph;
            }
            graph = new EcsGraph(self, graphWorld);
            new Destroyer(graph);
            _worldGraphs[graphWorld.id] = graph;
            return graph;
        }

        public static bool TryGetGraph(this EcsWorld self, out EcsGraph graph)
        {
            int worldID = self.id;
            if (_worldGraphs.Length <= worldID)
            {
                Array.Resize(ref _worldGraphs, worldID + 4);
            }
            graph = _worldGraphs[worldID];
            return graph != null;
        }

        public static EcsGraph GetGraph(this EcsWorld self)
        {
            if (self.TryGetGraph(out EcsGraph graph))
            {
                return graph;
            }
            Throw.UndefinedException();
            return null;
        }

        public static bool IsGraphWorld(this EcsWorld self)
        {
            if (self.TryGetGraph(out EcsGraph graph))
            {
                return graph.GraphWorld == self;
            }
            return false;
        }

        private static void TryDestroy(EcsGraph graph)
        {
            int worldID = graph.WorldID;
            if (_worldGraphs.Length <= worldID)
            {
                Array.Resize(ref _worldGraphs, worldID + 4);
            }
            int graphWorldID = graph.GraphWorldID;
            if (_worldGraphs.Length <= graphWorldID)
            {
                Array.Resize(ref _worldGraphs, graphWorldID + 4);
            }
            _worldGraphs[worldID] = null;
            _worldGraphs[graphWorldID] = null;
        }
        private class Destroyer : IEcsWorldEventListener
        {
            private EcsGraph _graph;
            public Destroyer(EcsGraph graph)
            {
                _graph = graph;
                graph.World.AddListener(this);
                graph.GraphWorld.AddListener(this);
            }
            public void OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer) { }
            public void OnWorldDestroy()
            {
                TryDestroy(_graph);
            }
            public void OnWorldResize(int newSize) { }
        }
    }
}