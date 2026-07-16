
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DODModAPI;

// single-threaded reimplementation of System.Runtime.CompilerServices.ConditionalWeakTable
// note that while the original ConditionalWeakTable uses DependentHandle, since we're in NET 3.5 DependentHandle doesn't exists
// due to this, TValue objects must never hold strong references to their associated TKey objects
public sealed class WeakTable<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    where TKey : class
{
    private const uint InitialCapacity = 8;

    private struct Entry {
        public GCHandle keyHandle;
        public TValue value;
        public uint hashCode;
        public int nextIndex;
    }

    private int[] _buckets; // _buckets[hashcode & (_buckets.Length - 1)] contains index of the first entry in bucket (-1 if empty)
    private Entry[] _entries; // the table entries containing the stored key and value
    private int _nextUnusedEntry;
    private int _freeListIdx; // represents an entry index to the internal chain of freed entries

    public WeakTable() {
        _buckets = new int[InitialCapacity];
        for (int i = 0; i < _buckets.Length; ++i) {
            _buckets[i] = -1;
        }
        _entries = new Entry[InitialCapacity];
        _freeListIdx = -1;
    }

    ~WeakTable() {
        // if OutOfMemoryException has been thrown in ctor, then entries will be uninitialized (i.e. null)
        if (_entries is null) { return; }

        for (int i = 0; i < _nextUnusedEntry; ++i) {
            if (_entries[i].keyHandle != default) {
                _entries[i].keyHandle.Free();
            }
        }
    }

    private static uint HashKey(TKey key) => (uint)RuntimeHelpers.GetHashCode(key);

    public bool TryGetValue(TKey key, out TValue value) {
        if (key is null) { throw new ArgumentNullException(nameof(key)); }

        uint hashCode = HashKey(key);
        uint bucketIdx = hashCode & ((uint)_buckets.Length - 1); // _buckets.Length must be PO2

        for (int entryIdx = _buckets[bucketIdx]; entryIdx != -1; entryIdx = _entries[entryIdx].nextIndex) { // traverse bucket
            ref Entry entry = ref _entries[entryIdx];
            if (entry.hashCode == hashCode && ReferenceEquals(entry.keyHandle.Target, key)) {
                value = entry.value;
                return true;
            }
        }
        value = default!;
        return false;
    }

    public void Add(TKey key, TValue value) {
        if (key is null) { throw new ArgumentNullException(nameof(key)); }

        uint hashCode = HashKey(key);
        uint bucketIdx = hashCode & ((uint)_buckets.Length - 1); // _buckets.Length must be PO2

        for (int entryIdx = _buckets[bucketIdx]; entryIdx != -1; entryIdx = _entries[entryIdx].nextIndex) { // traverse bucket
            ref Entry entry = ref _entries[entryIdx];
            if (entry.hashCode == hashCode && ReferenceEquals(entry.keyHandle.Target, key)) {
                throw new ArgumentException("Key already exists in the table", nameof(key));
            }
        }
        InternalInsert(hashCode, bucketIdx, key, value);
    }

    public bool TryAdd(TKey key, TValue value) {
        if (key is null) { throw new ArgumentNullException(nameof(key)); }

        uint hashCode = HashKey(key);
        uint bucketIdx = hashCode & ((uint)_buckets.Length - 1); // _buckets.Length must be PO2

        for (int entryIdx = _buckets[bucketIdx]; entryIdx != -1; entryIdx = _entries[entryIdx].nextIndex) { // traverse bucket
            ref Entry entry = ref _entries[entryIdx];
            if (entry.hashCode == hashCode && ReferenceEquals(entry.keyHandle.Target, key)) {
                return false;
            }
        }
        InternalInsert(hashCode, bucketIdx, key, value);
        return true;
    }

    public void AddOrUpdate(TKey key, TValue value) {
        if (key is null) { throw new ArgumentNullException(nameof(key)); }

        uint hashCode = HashKey(key);
        uint bucketIdx = hashCode & ((uint)_buckets.Length - 1); // _buckets.Length must be PO2

        for (int entryIdx = _buckets[bucketIdx]; entryIdx != -1; entryIdx = _entries[entryIdx].nextIndex) { // traverse bucket
            ref Entry entry = ref _entries[entryIdx];
            if (entry.hashCode == hashCode && ReferenceEquals(entry.keyHandle.Target, key)) {
                entry.value = value;
                return;
            }
        }
        InternalInsert(hashCode, bucketIdx, key, value);
    }

    public bool Remove(TKey key) => Remove(key, out _);

    public bool Remove(TKey key, out TValue? value) {
        if (key is null) { throw new ArgumentNullException(nameof(key)); }

        uint hashCode = HashKey(key);
        uint bucketIdx = hashCode & ((uint)_buckets.Length - 1); // _buckets.Length must be PO2

        int prevEntryIdx = -1;
        for (int entryIdx = _buckets[bucketIdx]; entryIdx != -1; entryIdx = _entries[entryIdx].nextIndex) { // traverse bucket
            ref Entry entry = ref _entries[entryIdx];
            if (entry.hashCode == hashCode && ReferenceEquals(entry.keyHandle.Target, key)) {
                if (prevEntryIdx == -1) {
                    _buckets[bucketIdx] = entry.nextIndex;
                } else {
                    _entries[prevEntryIdx].nextIndex = entry.nextIndex;
                }
                entry.keyHandle.Free(); // sets handle to IntPtr.Zero

                value = entry.value;
                entry.value = default!;
                entry.hashCode = 0;

                entry.nextIndex = _freeListIdx;
                _freeListIdx = entryIdx;

                return true;
            }
            prevEntryIdx = entryIdx;
        }
        value = default;
        return false;
    }

    public void Clear() {
        for (int i = 0; i < _nextUnusedEntry; ++i) {
            ref Entry entry = ref _entries[i];
            if (entry.keyHandle != default) {
                entry.keyHandle.Free();
                entry.keyHandle = default;
                entry.value = default!;
            }
        }
        for (int i = 0; i < _buckets.Length; ++i) {
            _buckets[i] = -1;
        }
        _nextUnusedEntry = 0;
        _freeListIdx = -1;
    }

    public TValue GetOrAdd(TKey key, TValue value) {
        if (TryGetValue(key, out TValue existing)) {
            return existing;
        }
        InternalCreateEntry(key, value);
        return value;
    }

    // warning: call to valueFactory must NOT add the same key that this method is adding
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory) {
        if (key is null) { throw new ArgumentNullException(nameof(key)); }
        if (valueFactory is null) { throw new ArgumentNullException(nameof(valueFactory)); }

        if (TryGetValue(key, out TValue? existing)) {
            return existing;
        }
        TValue value = valueFactory(key);
        // assuming key still doesn't exists
        InternalCreateEntry(key, value);
        return value;
    }

    // warning: call to valueFactory must NOT add the same key that this method is adding
    public TValue GetOrAdd<TArg>(TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArg) {
        if (key is null) { throw new ArgumentNullException(nameof(key)); }
        if (valueFactory is null) { throw new ArgumentNullException(nameof(valueFactory)); }

        if (TryGetValue(key, out TValue? existing)) {
            return existing;
        }
        TValue value = valueFactory(key, factoryArg);
        // assuming key still doesn't exists
        InternalCreateEntry(key, value);
        return value;
    }

    public TValue GetValueOrDefault(TKey key) {
        if (key is null) { throw new ArgumentNullException(nameof(key)); }

        if (TryGetValue(key, out TValue value)) {
            return value;
        }
        return default!;
    }

    public bool ContainsKey(TKey key) {
        if (key is null) { throw new ArgumentNullException(nameof(key)); }
        return TryGetValue(key, out _);
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
        for (int entryIdx = 0; entryIdx < _nextUnusedEntry; ++entryIdx) {
            Entry entry = _entries[entryIdx]; // copying since ref inside iterator method not allowed in c# 12
            if (entry.keyHandle.IsAllocated) {
                object? target = entry.keyHandle.Target;
                if (target is not null) {
                    yield return new KeyValuePair<TKey, TValue>((TKey)target, entry.value);
                }
            }
        } 
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void InternalCreateEntry(TKey key, TValue value) {
        uint hashCode = HashKey(key);
        uint bucketIdx = hashCode & ((uint)_buckets.Length - 1); // _buckets.Length must be PO2

        // assuming the key doesn't exists

        InternalInsert(hashCode, bucketIdx, key, value);
    }

    private void InternalInsert(uint hashCode, uint bucketIdx, TKey key, TValue value) {
        if (_freeListIdx == -1 && _nextUnusedEntry >= _entries.Length) {
            Grow();

            // resize may happen so we should recalculate bucket index
            bucketIdx = hashCode & ((uint)_buckets.Length - 1);
        }
        // Handle allocation can throw (OutOfMemoryException) so to avoid corrupting table state we do it first
        GCHandle newHandle = GCHandle.Alloc(key, GCHandleType.Weak);

        int entryIdx;
        if (_freeListIdx != -1) {
            entryIdx = _freeListIdx;
            _freeListIdx = _entries[entryIdx].nextIndex;
        } else {
            entryIdx = _nextUnusedEntry;
            _nextUnusedEntry += 1;
        }

        ref Entry entry = ref _entries[entryIdx];
        entry.keyHandle = newHandle;
        entry.value = value;
        entry.hashCode = hashCode;
        entry.nextIndex = _buckets[bucketIdx];
        _buckets[bucketIdx] = entryIdx;
    }

    private void Grow() {
        CleanupDeadEntries();

        // if cleanup populated the free list, then there are avaliable slots
        if (_freeListIdx != -1) {
            return;
        }

        uint newSize = (uint)_buckets.Length * 2;
        int[] newBuckets = new int[newSize];
        for (int i = 0; i < newBuckets.Length; ++i) {
            newBuckets[i] = -1;
        }
        Entry[] newEntries = new Entry[newSize];

        int nextEntry = 0;
        for (int i = 0; i < _buckets.Length; ++i) {
            for (int entryIdx = _buckets[i]; entryIdx != -1; entryIdx = _entries[entryIdx].nextIndex) {
                ref Entry oldEntry = ref _entries[entryIdx];
                ref Entry newEntry = ref newEntries[nextEntry];

                newEntry.keyHandle = oldEntry.keyHandle;
                newEntry.value = oldEntry.value;
                newEntry.hashCode = oldEntry.hashCode;

                uint newBucketIdx = newEntry.hashCode & (newSize - 1);
                newEntry.nextIndex = newBuckets[newBucketIdx];
                newBuckets[newBucketIdx] = nextEntry;

                nextEntry += 1;
            }
        }

        _buckets = newBuckets;
        _entries = newEntries;
        _nextUnusedEntry = nextEntry;
        _freeListIdx = -1;
    }

    private void CleanupDeadEntries() {
        for (int i = 0; i < _buckets.Length; ++i) {
            int currBucketIdx = _buckets[i];
            int prevEntryIdx = -1;

            while (currBucketIdx != -1) {
                ref Entry entry = ref _entries[currBucketIdx];
                int nextIdx = entry.nextIndex;

                if (entry.keyHandle.Target is null) {
                    if (prevEntryIdx == -1) {
                        _buckets[i] = nextIdx;
                    } else {
                        _entries[prevEntryIdx].nextIndex = nextIdx;
                    }
                    entry.keyHandle.Free(); // sets handle to IntPtr.Zero

                    entry.value = default!;

                    entry.nextIndex = _freeListIdx;
                    _freeListIdx = currBucketIdx;
                } else {
                    prevEntryIdx = currBucketIdx;
                }
                currBucketIdx = nextIdx;
            }
        }
    }
}
