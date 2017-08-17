using System;
using System.Windows.Media;

namespace BodyScanner
{
    class DepthToColorConverter
    {
        private const ushort minDepth = 500;
        private const ushort maxDepth = 5000;
        private readonly Color[] palette;

        public DepthToColorConverter()
        {
            palette = CreatePalette();
        }

        private static Color[] CreatePalette()
        {
            var coeff = 255f / (maxDepth - minDepth);
            var palette = new Color[maxDepth - minDepth + 1];
            for (var depth = minDepth; depth <= maxDepth; depth++)
            {
                var grey = (byte)(coeff * (depth - minDepth));
                palette[depth - minDepth] = Color.FromArgb(255, grey, grey, grey);
            }
            return palette;
        }

        public Color Convert(ushort depth)
        {
            return palette[Math.Max(minDepth, Math.Min(maxDepth, depth)) - minDepth];
        }
    }
}
