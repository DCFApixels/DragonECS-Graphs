using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DCFApixels.DragonECS
{
    public class EcsJoinExecutor : EcsQueryExecutor, IEcsWorldEventListener
    {
        private long _lastWorldVersion;

        #region Properties
        public sealed override long Version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _lastWorldVersion;
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
