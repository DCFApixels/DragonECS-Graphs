#if DISABLE_DEBUG
#undef DEBUG
#endif
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS.Graphs.Internal
{
    internal static class ArrayUtility
    {
        private static int GetHighBitNumber(uint bits)
        {
            if (bits == 0)
            {
                return -1;
            }
            int bit = 0;
            if ((bits & 0xFFFF0000) != 0)
            {
                bits >>= 16;
                bit |= 16;
            }
            if ((bits & 0xFF00) != 0)
            {
                bits >>= 8;
                bit |= 8;
            }
            if ((bits & 0xF0) != 0)
            {
                bits >>= 4;
                bit |= 4;
            }
            if ((bits & 0xC) != 0)
            {
                bits >>= 2;
                bit |= 2;
            }
            if ((bits & 0x2) != 0)
            {
                bit |= 1;
            }
            return bit;
        }
        public static int NormalizeSizeToPowerOfTwo(int minSize)
        {
            return 1 << (GetHighBitNumber((uint)minSize - 1u) + 1);
        }
    }
    internal static unsafe class UnmanagedArrayUtility
    {
        private static class MetaCache<T>
        {
            public readonly static int Size;
            static MetaCache()
            {
                T def = default;
                Size = Marshal.SizeOf(def);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* New<T>(int capacity) where T : unmanaged
        {
            return (T*)Marshal.AllocHGlobal(MetaCache<T>.Size * capacity).ToPointer();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void New<T>(out T* ptr, int capacity) where T : unmanaged
        {
            ptr = (T*)Marshal.AllocHGlobal(MetaCache<T>.Size * capacity).ToPointer();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* NewAndInit<T>(int capacity) where T : unmanaged
        {
            int newSize = MetaCache<T>.Size * capacity;
            byte* newPointer = (byte*)Marshal.AllocHGlobal(newSize).ToPointer();

            for (int i = 0; i < newSize; i++)
            {
                *(newPointer + i) = 0;
            }

            return (T*)newPointer;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NewAndInit<T>(out T* ptr, int capacity) where T : unmanaged
        {
            int newSize = MetaCache<T>.Size * capacity;
            byte* newPointer = (byte*)Marshal.AllocHGlobal(newSize).ToPointer();

            for (int i = 0; i < newSize; i++)
            {
                *(newPointer + i) = 0;
            }

            ptr = (T*)newPointer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Free(void* pointer)
        {
            Marshal.FreeHGlobal(new IntPtr(pointer));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Free<T>(ref T* pointer, ref int length) where T : unmanaged
        {
            Marshal.FreeHGlobal(new IntPtr(pointer));
            pointer = null;
            length = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* Clone<T>(T* sourcePtr, int length) where T : unmanaged
        {
            T* clone = New<T>(length);
            for (int i = 0; i < length; i++)
            {
                clone[i] = sourcePtr[i];
            }
            return clone;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* Resize<T>(void* oldPointer, int newCount) where T : unmanaged
        {
            Console.WriteLine("Resize ptr: " + ((IntPtr)oldPointer));
            return (T*)Marshal.ReAllocHGlobal(
                new IntPtr(oldPointer),
                new IntPtr(Marshal.SizeOf<T>() * newCount)).ToPointer();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* ResizeAndInit<T>(void* oldPointer, int oldSize, int newSize) where T : unmanaged
        {
            int sizeT = MetaCache<T>.Size;
            T* result = (T*)Marshal.ReAllocHGlobal(
                new IntPtr(oldPointer),
                new IntPtr(sizeT * newSize)).ToPointer();
            Init((byte*)result, sizeT * oldSize, sizeT * newSize);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Init(byte* pointer, int startByteIndex, int endByteIndex)
        {
            for (int i = startByteIndex; i < endByteIndex; i++)
            {
                *(pointer + i) = 0;
            }
        }
    }
}