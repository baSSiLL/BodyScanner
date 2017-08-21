using System;
using System.Diagnostics.Contracts;

namespace BodyScanner
{
    class ThreadSafeBitmap
    {
        private readonly object sync = new object();
        private readonly int[] data;

        public ThreadSafeBitmap(int width, int height)
        {
            Contract.Requires(width > 0 && height > 0);

            Width = width;
            Height = height;
            data = new int[width * height];
        }

        public int Width { get; }
        public int Height { get; }

        public void Access(Action<int[]> accessor)
        {
            Contract.Requires(accessor != null);

            lock (sync)
            {
                accessor.Invoke(data);
            }
        }
    }
}
