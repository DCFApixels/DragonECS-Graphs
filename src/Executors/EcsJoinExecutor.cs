using DCFApixels.DragonECS.Core;
using System;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public class EcsJoinExecutor : MaskQueryExecutor, IEcsWorldEventListener
    {
        private long _lastWorldVersion;

        #region Properties
        public sealed override long Version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _lastWorldVersion;
        }
        public override bool IsCached
        {
            get { return false; }
        }
        public override int LastCachedCount
        {
            get { return 0; }
        }
        #endregion

        #region Callbacks
        protected override void OnInitialize()
        {
        }
        protected override void OnDestroy()
        {
        }
        public void OnWorldResize(int newSize)
        {
        }
        public void OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer)
        {
        }
        public void OnWorldDestroy()
        {
        }
        #endregion
    }
}
