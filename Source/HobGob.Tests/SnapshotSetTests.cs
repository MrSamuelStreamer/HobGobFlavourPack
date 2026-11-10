using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HobGob.Tests
{
    public class SnapshotSetTests
    {
        [Fact]
        public void AddRemoveContains()
        {
            SnapshotSet<string> set = new SnapshotSet<string>();
            Assert.True(set.Add("a"));
            Assert.False(set.Add("a"));
            Assert.True(set.Contains("a"));
            Assert.Equal(1, set.Count);
            Assert.True(set.Remove("a"));
            Assert.False(set.Remove("a"));
            Assert.False(set.Contains("a"));
        }

        [Fact]
        public void Nulls_AreIgnored()
        {
            SnapshotSet<string> set = new SnapshotSet<string>();
            Assert.False(set.Add(null));
            Assert.False(set.Contains(null));
            Assert.False(set.Remove(null));
        }

        [Fact]
        public void Clear_EmptiesSet()
        {
            SnapshotSet<string> set = new SnapshotSet<string>();
            set.Add("a");
            set.Add("b");
            set.Clear();
            Assert.Equal(0, set.Count);
            Assert.False(set.Contains("a"));
        }

        /// <summary>
        /// Readers on other threads while one writer adds and removes: no exceptions, and an item
        /// that is never removed is always seen.
        /// </summary>
        [Fact]
        public async Task ConcurrentReaders_WhileWriterChanges_NeverFail()
        {
            SnapshotSet<object> set = new SnapshotSet<object>();
            object pinned = new object();
            set.Add(pinned);
            List<object> churn = new List<object>();
            for (int i = 0; i < 64; i++)
            {
                churn.Add(new object());
            }
            using CancellationTokenSource stop = new CancellationTokenSource();
            int missedPinned = 0;
            Task[] readers = new Task[4];
            for (int r = 0; r < readers.Length; r++)
            {
                readers[r] = Task.Run(() =>
                {
                    while (!stop.IsCancellationRequested)
                    {
                        if (!set.Contains(pinned))
                        {
                            Interlocked.Increment(ref missedPinned);
                        }
                        foreach (object o in churn)
                        {
                            set.Contains(o);
                        }
                    }
                });
            }
            // Fresh items each round make the set grow and shrink. A plain HashSet under this load
            // misses the pinned item and throws (checked once by hand), because readers see it mid-resize.
            for (int round = 0; round < 100; round++)
            {
                List<object> fresh = new List<object>();
                for (int i = 0; i < 1000; i++)
                {
                    fresh.Add(new object());
                }
                foreach (object o in fresh)
                {
                    set.Add(o);
                }
                foreach (object o in fresh)
                {
                    set.Remove(o);
                }
            }
            stop.Cancel();
            await Task.WhenAll(readers);
            Assert.Equal(0, missedPinned);
            Assert.True(set.Contains(pinned));
        }
    }
}
