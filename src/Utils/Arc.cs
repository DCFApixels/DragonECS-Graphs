using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public readonly struct StartArcEnd
    {
        /// <summary>Start vertex entity ID.</summary>
        public readonly int start;
        /// <summary>Arc entity ID.</summary>
        public readonly int arc;
        /// <summary>End vertex entity ID.</summary>
        public readonly int end;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StartArcEnd(int start, int arc, int end)
        {
            this.start = start;
            this.arc = arc;
            this.end = end;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out int start, out int arc, out int end)
        {
            start = this.start;
            arc = this.arc;
            end = this.end;
        }
    }
    public readonly struct ArcEnd
    {
        /// <summary>Arc entity ID.</summary>
        public readonly int arc;
        /// <summary>End vertex entity ID.</summary>
        public readonly int end;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArcEnd(int arc, int end)
        {
            this.arc = arc;
            this.end = end;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out int arc, out int end)
        {
            arc = this.arc;
            end = this.end;
        }
    }
}
