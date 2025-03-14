#if DISABLE_DEBUG
#undef DEBUG
#endif
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TValue = System.Int32;

namespace DCFApixels.DragonECS.Graphs.Internal
{
    internal sealed unsafe class SparseMatrix
    {
        private const int _NULL_NEXT = -1;

        private const int MIN_CAPACITY_BITS_OFFSET = 4;
        private const int MIN_CAPACITY = 1 << MIN_CAPACITY_BITS_OFFSET;

        private const int CHAIN_LENGTH_THRESHOLD = 5;
        private const float CHAIN_LENGTH_THRESHOLD_CAPCITY_THRESHOLD = 0.65f;

        private int* _buckets;
        private Entry* _entries;
        private int _capacity;
        private int _count_Threshold;

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
            _buckets = UnmanagedArrayUtility.New<int>(minCapacity);
            _entries = UnmanagedArrayUtility.NewAndInit<Entry>(minCapacity);
            for (int i = 0; i < minCapacity; i++)
            {
                _buckets[i] = _NULL_NEXT;
            }
            _modBitMask = (minCapacity - 1) & 0x7FFFFFFF;

            _count = 0;
            _freeList = 0;
            _freeCount = 0;

            SetCapacity(minCapacity);
        }
        ~SparseMatrix()
        {
            UnmanagedArrayUtility.Free(_buckets);
            UnmanagedArrayUtility.Free(_entries);
        }
        #endregion

        #region Add/TryAdd/Set
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int x, int y, TValue value)
        {
            unchecked
            {
#if DEBUG
                if (FindEntry(x, y) >= 0)
                {
                    Throw.ArgumentException("Has(x, y) is true");
                }
#endif
                int hash = IntHash.hashes[y] ^ x;
                AddInternal(KeyUtility.FromXY(x, y), hash, value);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddInternal(long key, int hash, int value)
        {
            unchecked
            {
                int targetBucket = hash & _modBitMask;
                int index;
                if (_freeCount == 0)
                {
                    if (_count > _count_Threshold)
                    {
                        Resize();
                        // обновляем под новое значение _modBitMask
                        targetBucket = hash & _modBitMask;
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

                ref int bucket = ref _buckets[targetBucket];
                ref Entry entry = ref _entries[index];

                entry.hash = hash;
                entry.next = bucket;
                entry.key = key;
                entry.value = value;
                bucket = index;
            }
        }
        #endregion

        #region FindEntry/Has
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindEntry(int x, int y)
        {
            long key = KeyUtility.FromXY(x, y);
            int hash = IntHash.hashes[y] ^ x;
            for (int i = _buckets[hash & _modBitMask]; i != _NULL_NEXT; i = _entries[i].next)
            {
                if (_entries[i].hash == hash && _entries[i].key == key)
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
            long key = KeyUtility.FromXY(x, y);
            int hash = IntHash.hashes[y] ^ x;
            int targetBucket = hash & _modBitMask;
            ref int bucket = ref _buckets[targetBucket];

            int last = -1;
            for (int i = bucket; i >= 0; last = i, i = _entries[i].next)
            {
                if (_entries[i].hash == hash && _entries[i].key == key)
                {
                    if (last < 0)
                    {
                        bucket = _entries[i].next;
                    }
                    else
                    {
                        _entries[last].next = _entries[i].next;
                    }
                    _entries[i].hash = -1;
                    _entries[i].next = _freeList;
                    //_entries[i].key = default;
                    //_entries[i].value = default;
                    _freeList = i;
                    _freeCount++;
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
                    _buckets[i] = _NULL_NEXT;
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
            int* newBuckets = UnmanagedArrayUtility.New<int>(newSize);
            for (int i = 0; i < newSize; i++)
            {
                newBuckets[i] = _NULL_NEXT;
            }
            //END newBuckets create and ini

            Entry* newEntries = UnmanagedArrayUtility.ResizeAndInit<Entry>(_entries, _capacity, newSize);
            for (int i = 0; i < _count; i++)
            {
                if (newEntries[i].key >= 0)
                {
                    ref Entry entry = ref newEntries[i];
                    ref int bucket = ref newBuckets[entry.hash & _modBitMask];
                    entry.next = bucket;
                    bucket = i;
                }
            }

            UnmanagedArrayUtility.Free(_buckets);
            _buckets = newBuckets;
            _entries = newEntries;

            SetCapacity(newSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetCapacity(int newSize)
        {
            _capacity = newSize;
            _count_Threshold = (int)(_capacity * CHAIN_LENGTH_THRESHOLD_CAPCITY_THRESHOLD);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int NormalizeCapacity(int capacity)
        {
            int result = ArrayUtility.NormalizeSizeToPowerOfTwo(capacity);
            return result < MIN_CAPACITY ? MIN_CAPACITY : result;
        }
        #endregion

        #region Utils
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct Entry
        {
            public long key;
            public int next;        // Index of next entry, -1 if last
            public int hash;
            public TValue value;
            public override string ToString() { return key == 0 ? "NULL" : $"{key} {value}"; }
        }

        private static class KeyUtility
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static long FromXY(int x, int y)
            {
                unchecked
                {
                    return ((long)x << 32) | (long)y;
                }
            }
        }
        #endregion
    }
}