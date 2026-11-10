using System.Collections.Generic;
using System.Threading;

namespace HobGob
{
    /// <summary>
    /// A set that is safe to read from any thread while the main thread changes it.
    /// Each change builds a new set and swaps the reference, so readers always see a whole set
    /// without taking a lock. Fit for sets that are read often and changed rarely.
    /// Writers must not run concurrently with each other (main thread only).
    /// </summary>
    public sealed class SnapshotSet<T>
    {
        private HashSet<T> current = new HashSet<T>();

        public bool Contains(T item)
        {
            HashSet<T> snapshot = Volatile.Read(ref current);
            return item != null && snapshot.Contains(item);
        }

        public int Count => Volatile.Read(ref current).Count;

        /// <summary>Adds the item. Returns false if it was already present.</summary>
        public bool Add(T item)
        {
            HashSet<T> snapshot = Volatile.Read(ref current);
            if (item == null || snapshot.Contains(item))
            {
                return false;
            }
            HashSet<T> next = new HashSet<T>(snapshot) { item };
            Volatile.Write(ref current, next);
            return true;
        }

        /// <summary>Removes the item. Returns false if it was not present.</summary>
        public bool Remove(T item)
        {
            HashSet<T> snapshot = Volatile.Read(ref current);
            if (item == null || !snapshot.Contains(item))
            {
                return false;
            }
            HashSet<T> next = new HashSet<T>(snapshot);
            next.Remove(item);
            Volatile.Write(ref current, next);
            return true;
        }

        public void Clear()
        {
            Volatile.Write(ref current, new HashSet<T>());
        }
    }
}
