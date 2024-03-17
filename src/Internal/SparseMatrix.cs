using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TValue = System.Int32;

namespace DCFApixels.DragonECS.Graphs.Internal
{
    internal sealed unsafe class SparseMatrix
    {
        public const int MIN_CAPACITY_BITS_OFFSET = 4;
        public const int MIN_CAPACITY = 1 << MIN_CAPACITY_BITS_OFFSET;


        private const int MAX_CHAIN_LENGTH = 5;

        private UnsafeArray<Basket> _buckets;
        private UnsafeArray<Entry> _entries;
        private int _capacity;

        private int _count;

        private int _freeList;
        private int _freeCount;

        private int _modBitMask;

        #region Properties
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _count; }
        }
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _capacity; }
        }
        #endregion

        #region Constructors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SparseMatrix(int minCapacity = MIN_CAPACITY)
        {
            minCapacity = NormalizeCapacity(minCapacity);
            _buckets = new UnsafeArray<Basket>(minCapacity);
            for (int i = 0; i < minCapacity; i++)
            {
                _buckets[i] = Basket.Empty;
            }
            _entries = new UnsafeArray<Entry>(minCapacity, true);
            _modBitMask = (minCapacity - 1) & 0x7FFFFFFF;

            _count = 0;
            _freeList = 0;
            _freeCount = 0;

            _capacity = minCapacity;
        }
        #endregion

        #region Add/TryAdd/Set
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int x, int y, TValue value)
        {
            Key key = Key.FromXY(x, y);
#if DEBUG
            if (FindEntry(key) >= 0)
            {
                Throw.ArgumentException("Has(x, y) is true");
            }
#endif
            int targetBucket = key.yHash & _modBitMask;
            AddInternal(key, targetBucket, value);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(int x, int y, TValue value)
        {
            Key key = Key.FromXY(x, y);
            if (FindEntry(key) >= 0)
            {
                return false;
            }
            int targetBucket = key.yHash & _modBitMask;
            AddInternal(key, targetBucket, value);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int x, int y, TValue value)
        {
            Key key = Key.FromXY(x, y);
            int targetBucket = key.yHash & _modBitMask;

            for (int i = _buckets[targetBucket].index; i >= 0; i = _entries[i].next)
            {
                if (_entries[i].key == key)
                {
                    _entries[i].value = value;
                    return;
                }
            }
            AddInternal(key, targetBucket, value);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddInternal(Key key, int targetBucket, int value)
        {
            int index;
            if (_freeCount == 0)
            {
                if (_count == _capacity)
                {
                    Resize();
                    targetBucket = key.yHash & _modBitMask;
                }
                index = _count++;
            }
            else
            {
                index = _freeList;
                _freeList = _entries[index].next;
                _freeCount--;
            }

#if DEBUG
            if (_freeCount < 0) { Throw.UndefinedException(); }
#endif

            ref Basket basket = ref _buckets[targetBucket];
            ref Entry entry = ref _entries[index];

            entry.next = basket.index;
            entry.key = key;
            entry.value = value;
            basket.count++;
            basket.index = index;

            if (basket.count >= MAX_CHAIN_LENGTH && Count / Capacity >= 0.7f)
            {
                Resize();
            }
        }
        #endregion

        #region FindEntry/Has
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindEntry(int x, int y)
        {
            Key key = Key.FromXY(x, y);
            for (int i = _buckets[key.yHash & _modBitMask].index; i >= 0; i = _entries[i].next)
            {
                if (_entries[i].key == key)
                {
                    return i;
                }
            }
            return -1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindEntry(Key key)
        {
            for (int i = _buckets[key.yHash & _modBitMask].index; i >= 0; i = _entries[i].next)
            {
                if (_entries[i].key == key)
                {
                    return i;
                }
            }
            return -1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasKey(int x, int y)
        {
            return FindEntry(x, y) >= 0;
        }
        #endregion

        #region GetValue/TryGetValue
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue GetValue(int x, int y)
        {
            int index = FindEntry(x, y);
#if DEBUG
            if (index < 0) { Throw.KeyNotFound(); }
#endif
            return _entries[index].value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(int x, int y, out TValue value)
        {
            int index = FindEntry(x, y);
            if (index < 0)
            {
                value = default;
                return false;
            }
            value = _entries[index].value;
            return true;
        }
        #endregion

        #region TryDel
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDel(int x, int y)
        {
            Key key = Key.FromXY(x, y);
            int targetBucket = key.yHash & _modBitMask;
            ref Basket basket = ref _buckets[targetBucket];

            int last = -1;
            for (int i = basket.index; i >= 0; last = i, i = _entries[i].next)
            {
                if (_entries[i].key == key)
                {
                    if (last < 0)
                    {
                        basket.index = _entries[i].next;
                    }
                    else
                    {
                        _entries[last].next = _entries[i].next;
                    }
                    _entries[i].next = _freeList;
                    _entries[i].key = Key.Null;
                    _entries[i].value = default;
                    _freeList = i;
                    _freeCount++;
                    basket.count--;
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Clear
        public void Clear()
        {
            if (_count > 0)
            {
                for (int i = 0; i < _capacity; i++)
                {
                    _buckets[i] = Basket.Empty;
                }
                for (int i = 0; i < _capacity; i++)
                {
                    _entries[i] = default;
                }
                _count = 0;
            }
        }
        #endregion

        #region Resize
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize()
        {
            int newSize = _capacity << 1;
            _modBitMask = (newSize - 1) & 0x7FFFFFFF;

            //newBuckets create and ini
            //Basket* newBuckets = UnmanagedArrayUtility.New<Basket>(newSize);
            UnsafeArray<Basket> newBuckets = new UnsafeArray<Basket>(newSize);
            for (int i = 0; i < newSize; i++)
            {
                newBuckets[i] = Basket.Empty;
            }
            //END newBuckets create and ini

            //Entry* newEntries = UnmanagedArrayUtility.ResizeAndInit<Entry>(_entries.ptr, _capacity, newSize);
            UnsafeArray<Entry> newEntries = UnsafeArray<Entry>.Resize(_entries, newSize);
            for (int i = 0; i < _count; i++)
            {
                if (newEntries[i].key.x >= 0)
                {
                    ref Entry entry = ref newEntries[i];
                    ref Basket basket = ref newBuckets[entry.key.yHash & _modBitMask];
                    entry.next = basket.index;
                    basket.index = i;
                    basket.count++;
                }
            }

            _buckets = newBuckets;
            _entries = newEntries;

            _capacity = newSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int NormalizeCapacity(int capacity)
        {
            int result = MIN_CAPACITY;
            while (result < capacity) { result <<= 1; }
            return result;
        }
        #endregion

        #region Utils
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct Entry
        {
            public int next;        // Index of next entry, -1 if last
            public Key key;
            public TValue value;
            public override string ToString() { return key.x == 0 ? "NULL" : $"{key} {value}"; }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
        public struct Basket
        {
            public static readonly Basket Empty = new Basket(-1, 0);
            public int index;
            public int count;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Basket(int index, int length)
            {
                this.index = index;
                this.count = length;
            }
            public override string ToString() { return index < 0 ? "NULL" : $"{index} {count}"; }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
        public readonly struct Key : IEquatable<Key>
        {
            public static readonly Key Null = new Key(-1, 0);
            public readonly int x;
            public readonly int yHash;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private Key(int x, int yHash)
            {
                this.x = x;
                this.yHash = yHash;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Key FromXY(int x, int y) 
            {
                unchecked
                {
                    return new Key(x, x ^ y ^ XXX(y));
                }
            }
            private static int XXX(int x)
            {
                x *= 3571;
                x ^= x << 13;
                x ^= x >> 17;
                return x;
            }
            public static bool operator ==(Key a, Key b) { return a.x == b.x && a.yHash == b.yHash; }
            public static bool operator !=(Key a, Key b) { return a.x != b.x || a.yHash != b.yHash; }
            public override int GetHashCode() { return yHash; }
            public bool Equals(Key other) { return this == other; }
            public override bool Equals(object obj) { return obj is Key && Equals((Key)obj); }
            public override string ToString()
            {
                return $"({x}, {yHash})";
            }
        }
        #endregion
    }
}