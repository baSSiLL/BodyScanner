using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BodyScanner
{
    class ThreadSafeBitmap
    {
        private readonly object sync = new object();
        private readonly byte[] data;

        public ThreadSafeBitmap(int width, int height)
        {
            Contract.Requires(width > 0 && height > 0);

            Width = width;
            Height = height;
            data = new byte[width * height * 4];
        }

        public int Width { get; }
        public int Height { get; }

        public void Access(Action<byte[]> accessor)
        {
            Contract.Requires(accessor != null);

            lock (sync)
            {
                accessor.Invoke(data);
            }
        }
    }
}
