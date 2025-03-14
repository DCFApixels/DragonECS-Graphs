#if DISABLE_DEBUG
#undef DEBUG
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS.Graphs.Internal
{
    internal unsafe static class UnsafeArray
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Resize<T>(ref UnsafeArray<T> array, int newSize)
            where T : unmanaged
        {
            array.ptr = UnmanagedArrayUtility.Resize<T>(array.ptr, newSize);
            array.Length = newSize;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ResizeAndInit<T>(ref UnsafeArray<T> array, int newSize)
            where T : unmanaged
        {
            array.ptr = UnmanagedArrayUtility.ResizeAndInit<T>(array.ptr, array.Length, newSize);
            array.Length = newSize;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear<T>(ref UnsafeArray<T> array)
            where T : unmanaged
        {
            T* ptr = array.ptr;
            for (int i = 0; i < array.Length; i++)
            {
                ptr[i] = default;
            }
        }
    }
    [DebuggerTypeProxy(typeof(UnsafeArray<>.DebuggerProxy))]
    internal unsafe struct UnsafeArray<T> : IDisposable, IEnumerable<T>
        where T : unmanaged
    {
        internal T* ptr;
        internal int Length;

        //        public ref T this[int index]
        //        {
        //            [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //            get
        //            {
        //#if DEBUG
        //                if (index < 0 || index >= Length) { Throw.ArgumentOutOfRange(); }
        //#endif
        //                return ref ptr[index];
        //            }
        //        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeArray(int length)
        {
            ptr = UnmanagedArrayUtility.New<T>(length);
            Length = length;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeArray(int length, bool isInit)
        {
            ptr = UnmanagedArrayUtility.NewAndInit<T>(length);
            Length = length;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeArray(T* ptr, int length)
        {
            this.ptr = ptr;
            Length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeArray<T> Clone()
        {
            return new UnsafeArray<T>(UnmanagedArrayUtility.Clone(ptr, Length), Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            UnmanagedArrayUtility.Free(ref ptr, ref Length);
        }
        public override string ToString()
        {
            return $"ua({Length}) ({string.Join(", ", this.ToArray())})";
        }

        public static void Resize(ref UnsafeArray<T> array, int newSize)
        {
            array.ptr = UnmanagedArrayUtility.Resize<T>(array.ptr, newSize);
            array.Length = newSize;
        }
        public static UnsafeArray<T> Resize(UnsafeArray<T> array, int newSize)
        {
            return new UnsafeArray<T>(UnmanagedArrayUtility.Resize<T>(array.ptr, newSize), newSize);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new Enumerator(ptr, Length);
        public struct Enumerator : IEnumerator<T>
        {
            private readonly T* _ptr;
            private readonly int _length;
            private int _index;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(T* ptr, int length)
            {
                _ptr = ptr;
                _length = length;
                _index = -1;
            }
            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _ptr[_index];
            }
            object IEnumerator.Current => Current;
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
            public T[] elements;
            public int length;
            public DebuggerProxy(UnsafeArray<T> instance)
            {
                ptr = instance.ptr;
                length = instance.Length;
                elements = new T[length];
                for (int i = 0; i < length; i++)
                {
                    elements[i] = instance.ptr[i];
                }
            }
        }
    }

    internal static class UnsafeArrayExtentions
    {
        public static void Resize<T>(ref UnsafeArray<T> self, int newSize)
            where T : unmanaged
        {
            UnsafeArray<T>.Resize(ref self, newSize);
        }
    }
}
