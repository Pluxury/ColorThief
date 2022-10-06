﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ColorThief.Shared;

namespace ColorThiefDotNet
{
    public class ColorThiefIOS
    {
        // private IColorThiefBitmap bitmapConverter = new ColorThiefBitmap();

        /// <summary>
        ///     Use the median cut algorithm to cluster similar colors and return the base color from the largest cluster.
        /// </summary>
        /// <param name="sourceImage">The source image.</param>
        /// <param name="quality">
        ///     1 is the highest quality settings. 10 is the default. There is
        ///     a trade-off between quality and speed. The bigger the number,
        ///     the faster a color will be returned but the greater the
        ///     likelihood that it will not be the visually most dominant color.
        /// </param>
        /// <param name="ignoreWhite">if set to <c>true</c> [ignore white].</param>
        /// <returns></returns>
        public QuantizedColor GetColor(UIImage sourceImage, int quality = ColorThief.Shared.ColorThief.DefaultQuality, bool ignoreWhite = ColorThief.Shared.ColorThief.DefaultIgnoreWhite)
        {
            var palette = GetPalette(sourceImage, 3, quality, ignoreWhite);

            var dominantColor = new QuantizedColor(new Color
            {
                A = Convert.ToByte(palette.Average(a => a.Color.A)),
                R = Convert.ToByte(palette.Average(a => a.Color.R)),
                G = Convert.ToByte(palette.Average(a => a.Color.G)),
                B = Convert.ToByte(palette.Average(a => a.Color.B))
            }, Convert.ToInt32(palette.Average(a => a.Population)));

            return dominantColor;
        }

        /// <summary>
        ///     Use the median cut algorithm to cluster similar colors.
        /// </summary>
        /// <param name="sourceImage">The source image.</param>
        /// <param name="colorCount">The color count.</param>
        /// <param name="quality">
        ///     1 is the highest quality settings. 10 is the default. There is
        ///     a trade-off between quality and speed. The bigger the number,
        ///     the faster a color will be returned but the greater the
        ///     likelihood that it will not be the visually most dominant color.
        /// </param>
        /// <param name="ignoreWhite">if set to <c>true</c> [ignore white].</param>
        /// <returns></returns>
        /// <code>true</code>
        public List<QuantizedColor> GetPalette(UIImage sourceImage, int colorCount = ColorThief.Shared.ColorThief.DefaultColorCount, int quality = ColorThief.Shared.ColorThief.DefaultQuality, bool ignoreWhite = ColorThief.Shared.ColorThief.DefaultIgnoreWhite)
        {
            var pixelArray = GetPixelsFast(sourceImage, quality, ignoreWhite);
            var cmap = new ColorThief.Shared.ColorThief().GetColorMap(pixelArray, colorCount);
            if(cmap != null)
            {
                var colors = cmap.GeneratePalette();
                return colors;
            }
            return new List<QuantizedColor>();
        }

        private byte[] GetIntFromPixel(UIImage bmp)
        {
            var width = (int)bmp.Size.Width;
            var height = (int)bmp.Size.Height;
            using(var colourSpace = CGColorSpace.CreateDeviceRGB())
            {
                var rawData = Marshal.AllocHGlobal(width * height * 4);
                using(var context = new CGBitmapContext(rawData, width, height, 8, 4 * width, colourSpace, CGImageAlphaInfo.PremultipliedLast))
                {
                    context.DrawImage(new CGRect(0, 0, width, height), bmp.CGImage);
                    var pixelData = new byte[width * height * 4];
                    Marshal.Copy(rawData, pixelData, 0, pixelData.Length);
                    Marshal.FreeHGlobal(rawData);

                    return pixelData;
                }
            }
        }

        private byte[][] ConvertPixelsiOS(byte[] pixels, int pixelCount, int quality, bool ignoreWhite)
        {


            var expectedDataLength = pixelCount * ColorThief.Shared.ColorThief.ColorDepth;
            if (expectedDataLength != pixels.Length)
            {
                throw new ArgumentException("(expectedDataLength = "
                                            + expectedDataLength + ") != (pixels.length = "
                                            + pixels.Length + ")");
            }

            // Store the RGB values in an array format suitable for quantize
            // function

            // numRegardedPixels must be rounded up to avoid an
            // ArrayIndexOutOfBoundsException if all pixels are good.

            var numRegardedPixels = (pixelCount + quality - 1) / quality;

            var numUsedPixels = 0;
            var pixelArray = new byte[numRegardedPixels][];

            for (var i = 0; i < pixelCount; i += quality)
            {
                var offset = i * ColorThief.Shared.ColorThief.ColorDepth;
                var r = pixels[offset];
                var g = pixels[offset + 1];
                var b = pixels[offset + 2];
                var a = pixels[offset + 3];

                // If pixel is mostly opaque and not white
                if (a >= 125 && !(ignoreWhite && r > 250 && g > 250 && b > 250))
                {
                    pixelArray[numUsedPixels] = new[] { r, g, b };
                    numUsedPixels++;
                }
            }

            // Remove unused pixels from the array
            var copy = new byte[numUsedPixels][];
            Array.Copy(pixelArray, copy, numUsedPixels);
            return copy;
        }

        private byte[][] GetPixelsFast(UIImage sourceImage, int quality, bool ignoreWhite)
        {
            if(quality < 1)
            {
                quality = ColorThief.Shared.ColorThief.DefaultQuality;
            }
            var pixels = GetIntFromPixel(sourceImage);
            var pixelCount = (int)sourceImage.Size.Width * (int)sourceImage.Size.Height;

            return ConvertPixelsiOS(pixels, pixelCount, quality, ignoreWhite);
        }
    }
}