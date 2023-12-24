using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
    [Serializable]
    public readonly struct ArcTargets : IEquatable<ArcTargets>
    {
        public static readonly ArcTargets Empty = new ArcTargets();

        /// <summary>Start vertex entity ID.</summary>
        public readonly int start;
        /// <summary>End vertex entity ID.</summary>
        public readonly int end;

        #region Properties
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => start == 0 && end == 0;
        }
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ArcTargets(int startEntity, int endEntity)
        {
            start = startEntity;
            end = endEntity;
        }

        #region operators
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(ArcTargets a, ArcTargets b) => a.start == b.start && a.end == b.end;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(ArcTargets a, ArcTargets b) => a.start != b.start || a.end != b.end;
        #endregion

        #region Other
        public override bool Equals(object obj) => obj is ArcTargets targets && targets == this;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ArcTargets other) => this == other;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => ~start ^ end;
        public override string ToString() => $"arc({start} -> {end})";
        #endregion
    }
}