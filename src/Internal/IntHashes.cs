#if DISABLE_DEBUG
#undef DEBUG
#endif
namespace DCFApixels.DragonECS.Graphs.Internal
{
    internal static unsafe class IntHash
    {
        public static int* hashes = null;
        public static int length = 0;
        public static void InitFor(int count)
        {
            if (count <= length) { return; }

            unchecked
            {
                //quasi random consts
                const decimal G1 = 1.6180339887498948482045868383m;
                const uint Q32_MAX = uint.MaxValue;
                const uint X1_Q32 = (uint)(1m / G1 * Q32_MAX) + 1;

                if (hashes != null)
                {
                    UnmanagedArrayUtility.Free(hashes);
                }
                hashes = UnmanagedArrayUtility.New<int>(count);

                uint state = 3571U;
                for (int i = 0; i < count; i++)
                {
                    state = X1_Q32 * state;
                    hashes[i] = ((int)state) & 0x7FFFFFFF;
                }

                count = length;
            }
        }
    }
}