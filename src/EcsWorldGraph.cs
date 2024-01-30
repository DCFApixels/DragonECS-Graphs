using DCFApixels.DragonECS.Relations.Internal;
using DCFApixels.DragonECS.Relations.Utils;
using System;

namespace DCFApixels.DragonECS
{
    public static class EcsWorldGraph
    {
        private static readonly SparseArray64<EcsArc> _matrix = new SparseArray64<EcsArc>(4);
        private static EcsArc[] _arcsMapping = new EcsArc[4];

        #region Register/Unregister
        private static EcsArc Register(EcsWorld startWorld, EcsWorld endWorld, EcsArcWorld arcWorld)
        {
            int startWorldID = startWorld.id;
            int endWorldID = endWorld.id;
            int arcWorldID = arcWorld.id;

            if (_arcsMapping.Length <= arcWorldID)
            {
                Array.Resize(ref _arcsMapping, arcWorldID + 4);
            }

#if DEBUG
            if (_matrix.Contains(startWorldID, endWorldID))
            {
                throw new EcsFrameworkException();
            }
#endif
            EcsArc arc = new EcsArc(startWorld, endWorld, arcWorld);
            _matrix[startWorldID, endWorldID] = arc;
            _arcsMapping[arcWorldID] = arc;
            return arc;
        }
        public static bool IsRegistered(EcsArc arc)
        {
            return Has(arc.StartWorld.id, arc.EndWorld.id);
        }
        private static void Unregister(EcsWorld startWorld, EcsWorld endWorld)
        {
            int startWorldID = startWorld.id;
            int endWorldID = endWorld.id;
            EcsArc arc = _matrix[startWorldID, endWorldID];
            _arcsMapping[arc.ArcWorld.id] = null;
            _matrix.Remove(startWorldID, endWorldID);
            arc.Destroy();
        }
        #endregion

        #region Get/Has
        //        private static EcsArc GetOrRigister(EcsWorld startWorld, EcsWorld otherWorld)
        //        {
        //#if DEBUG
        //            if (!_matrix.Contains(startWorld.id, otherWorld.id))
        //            {
        //                return Register();
        //            }
        //#endif
        //            return _matrix[startWorld.id, otherWorld.id];
        //        }
        private static EcsArc Get(EcsWorld startWorld, EcsWorld otherWorld)
        {
#if DEBUG
            if (!_matrix.Contains(startWorld.id, otherWorld.id))
            {
                throw new EcsFrameworkException();
            }
#endif
            return _matrix[startWorld.id, otherWorld.id];
        }
        private static bool Has(EcsWorld startWorld, EcsWorld endWorld)
        {
            return Has(startWorld.id, endWorld.id);
        }
        private static bool Has(int startWorldID, int endWorldID)
        {
            return _matrix.Contains(startWorldID, endWorldID);
        }
        #endregion

        #region Extension
        public static bool IsRegistered(this EcsArcWorld self)
        {
            if (self == null)
            {
                Throw.ArgumentNull();
            }
            int id = self.id;
            return id < _arcsMapping.Length && _arcsMapping[self.id] != null;
        }
        public static EcsArc GetRegisteredArc(this EcsArcWorld self)
        {
            if (self == null)
            {
                Throw.ArgumentNull();
            }
            int id = self.id;
            if (id < _arcsMapping.Length && _arcsMapping[self.id] == null)
            {
                throw new Exception();
            }
            return _arcsMapping[self.id];
        }


        public static EcsArc SetLoopArcAuto<TWorld>(this TWorld self, out EcsLoopArcWorld<TWorld> arcWorld)
            where TWorld : EcsWorld
        {
            if (self == null)
            {
                Throw.ArgumentNull();
            }
            if (typeof(TWorld) != self.GetType())
            {
                EcsDebug.PrintWarning($"{nameof(TWorld)} is not {self.GetType().Name}");
            }
            arcWorld = new EcsLoopArcWorld<TWorld>();
            return Register(self, self, arcWorld);
        }
        public static EcsArc SetArcAuto<TStartWorld, TEndWorld>(this TStartWorld start, TEndWorld end, out EcsArcWorld<TStartWorld, TEndWorld> arcWorld)
            where TStartWorld : EcsWorld
            where TEndWorld : EcsWorld
        {
            if (start == null || end == null)
            {
                Throw.ArgumentNull();
            }
            if (typeof(TStartWorld) == typeof(EcsWorld) && typeof(TEndWorld) == typeof(EcsWorld))
            {
                EcsDebug.PrintWarning($"{nameof(TStartWorld)} is not {start.GetType().Name} or {nameof(TEndWorld)} is not {end.GetType().Name}");
            }
            arcWorld = new EcsArcWorld<TStartWorld, TEndWorld>();
            return Register(start, end, arcWorld);
        }
        public static EcsArc SetLoopArcAuto<TWorld>(this TWorld self)
            where TWorld : EcsWorld
        {
            return SetLoopArcAuto(self, out _);
        }
        public static EcsArc SetArcAuto<TStartWorld, TEndWorld>(this TStartWorld start, TEndWorld end)
            where TStartWorld : EcsWorld
            where TEndWorld : EcsWorld
        {
            return SetArcAuto(start, end, out _);
        }

        public static EcsArc SetLoopArc(this EcsWorld self, EcsArcWorld arc) => SetArc(self, self, arc);
        public static EcsArc SetArc(this EcsWorld start, EcsWorld end, EcsArcWorld arc)
        {
            if (start == null || end == null || arc == null)
            {
                Throw.ArgumentNull();
            }
            return Register(start, end, arc);
        }

        public static bool HasLoopArc(this EcsWorld self) => HasArc(self, self);
        public static bool HasArc(this EcsWorld start, EcsWorld end)
        {
            if (start == null || end == null)
            {
                Throw.ArgumentNull();
            }
            return Has(start, end);
        }

        public static EcsArc GetLoopArc(this EcsWorld self) => GetArc(self, self);
        public static EcsArc GetArc(this EcsWorld start, EcsWorld end)
        {
            if (start == null || end == null)
            {
                Throw.ArgumentNull();
            }
            return Get(start, end);
        }

        public static bool TryGetLoopArc(this EcsWorld self, out EcsArc arc) => TryGetArc(self, self, out arc);
        public static bool TryGetArc(this EcsWorld start, EcsWorld end, out EcsArc arc)
        {
            if (start == null || end == null)
            {
                Throw.ArgumentNull();
            }
            bool result = Has(start, end);
            arc = result ? Get(start, end) : null;
            return result;
        }

        public static void DestroyLoopArc(this EcsWorld self) => DestroyArcWith(self, self);
        public static void DestroyArcWith(this EcsWorld start, EcsWorld end)
        {
            if (start == null || end == null)
            {
                Throw.ArgumentNull();
            }
            Get(start, end).ArcWorld.Destroy();
            Unregister(start, end);
        }
        #endregion
    }
}