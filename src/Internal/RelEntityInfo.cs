using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS.Graphs.Internal
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
    [Serializable]
    internal readonly struct RelEntityInfo : IEquatable<RelEntityInfo>
    {
        public static readonly RelEntityInfo Empty = new RelEntityInfo();

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
        internal RelEntityInfo(int startEntity, int endEntity)
        {
            start = startEntity;
            end = endEntity;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out int start, out int end)
        {
            start = this.start;
            end = this.end;
        }

        #region operators
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(RelEntityInfo a, RelEntityInfo b) => a.start == b.start && a.end == b.end;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(RelEntityInfo a, RelEntityInfo b) => a.start != b.start || a.end != b.end;
        #endregion

        #region Other
        public override bool Equals(object obj) => obj is RelEntityInfo targets && targets == this;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(RelEntityInfo other) => this == other;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => ~start ^ end;
        public override string ToString() => $"arc({start} -> {end})";
        #endregion
    }
}