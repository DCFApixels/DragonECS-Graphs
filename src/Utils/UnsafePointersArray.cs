using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS.Relations.Internal
{
    internal unsafe static class UnsafePointersArray
    {
        public static void Resize<T>(ref UnsafePointersArray<T> array, int newSize)
            where T : unmanaged
        {
            array.ptr = (T**)UnmanagedArrayUtility.Resize<IntPtr>(array.ptr, newSize);
            array.Length = newSize;
        }
        public static void ResizeAndInit<T>(ref UnsafePointersArray<T> array, int newSize)
            where T : unmanaged
        {
            array.ptr = (T**)UnmanagedArrayUtility.ResizeAndInit<IntPtr>(array.ptr, array.Length, newSize);
            array.Length = newSize;
        }
    }

    [DebuggerTypeProxy(typeof(UnsafePointersArray<>.DebuggerProxy))]
    internal unsafe struct UnsafePointersArray<T> : IDisposable
        where T : unmanaged
    {
        internal T** ptr;
        internal int Length;

        public ref T* this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if DEBUG
                if (index < 0 || index >= Length)
                    Throw.ArgumentOutOfRange();
#endif
                return ref ptr[index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafePointersArray(int length)
        {
            ptr = (T**)UnmanagedArrayUtility.New<IntPtr>(length);
            Length = length;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafePointersArray(int length, bool isInit)
        {
            ptr = (T**)UnmanagedArrayUtility.NewAndInit<IntPtr>(length);
            Length = length;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private UnsafePointersArray(T** ptr, int length)
        {
            this.ptr = ptr;
            Length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafePointersArray<T> Clone()
        {
            return new UnsafePointersArray<T>(UnmanagedArrayUtility.ClonePointersArray(ptr, Length), Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            UnmanagedArrayUtility.FreePointersArray(ref ptr, ref Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new Enumerator(ptr, Length);
        public struct Enumerator
        {
            private readonly T** _ptr;
            private readonly int _length;
            private int _index;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(T** ptr, int length)
            {
                _ptr = ptr;
                _length = length;
                _index = -1;
            }
            public T* Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _ptr[_index];
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++_index < _length;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset() { }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose() { }
        }

        internal class DebuggerProxy
        {
            public void* ptr;
            public T*[] elements;
            public int length;
            public DebuggerProxy(UnsafePointersArray<T> instance)
            {
                ptr = instance.ptr;
                length = instance.Length;
                elements = new T*[length];
                for (int i = 0; i < length; i++)
                {
                    elements[i] = instance[i];
                }
            }
        }
    }
}
