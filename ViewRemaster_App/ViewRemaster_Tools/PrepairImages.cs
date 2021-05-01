using AForge.Imaging.Filters;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ViewRemaster_Tools
{
    public class PrepairImages
    {
        public double FindRotation(Bitmap mask)
        {
            double angle = 0;
            using (var temp = CropImage(mask, true, true))
            {
                //find first black group of pixels
                int y1 = 0;
                int x1 = 200;

                while (temp.GetPixel(x1, y1).GetBrightness() > 0)
                {
                    y1++;
                }

                int y2 = 0;
                int x2 = temp.Width - 200; ;
                while (temp.GetPixel(x2, y2).GetBrightness() > 0)
                {
                    y2++;
                }

                angle = Math.Atan2(y2 - y1, x2 - x1) * 180 / Math.PI;
            }
            return angle;
        }

        public Bitmap RotateImage(Bitmap image, double angle)
        {
            RotateBilinear rb = new RotateBilinear(angle / 2.0, true)
            {
                FillColor = Color.White
            };
            Bitmap rotated = rb.Apply(image);

            return rotated;
        }

        public Bitmap ResizeReelImage(Bitmap src)
        {
            Bitmap nreel = new Bitmap(1600, 1200, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(nreel);
            g.Clear(System.Drawing.Color.FromArgb(0, System.Drawing.Color.Black));

            var ci = CropImage(src, new Rectangle(160, 0, 1600, 1080));
            g.DrawImage(ci, 0, 0);

            return nreel;
        }

        public Bitmap MaskImage(Bitmap image, Bitmap mask)
        {
            if (image.Width != mask.Width || image.Height != mask.Height)
            {
                return null;
            }

            Bitmap newImage = new Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(newImage);
            g.Clear(Color.FromArgb(0, Color.Black));

            unsafe
            {
                BitmapData maskData = mask.LockBits(new Rectangle(0, 0, mask.Width, mask.Height), ImageLockMode.ReadWrite, mask.PixelFormat);
                BitmapData newData = newImage.LockBits(new Rectangle(0, 0, newImage.Width, newImage.Height), ImageLockMode.ReadWrite, newImage.PixelFormat);
                BitmapData imageData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, image.PixelFormat);

                int imagebpp = System.Drawing.Bitmap.GetPixelFormatSize(image.PixelFormat) / 8;
                int newbpp = System.Drawing.Bitmap.GetPixelFormatSize(newImage.PixelFormat) / 8;
                int maskbpp = System.Drawing.Bitmap.GetPixelFormatSize(mask.PixelFormat) / 8;
                int heightInPixels = maskData.Height;
                int widthInBytes = maskData.Width * maskbpp;

                byte* maskfp = (byte*)maskData.Scan0;
                byte* newfp = (byte*)newData.Scan0;
                byte* imagefp = (byte*)imageData.Scan0;

                for (int x = 0; x < widthInBytes; x += maskbpp)
                {
                    for (int y = 0; y < heightInPixels; y++)
                    {
                        byte* currentLine = maskfp + x + (y * maskData.Stride);

                        if (currentLine[0] == 0 && currentLine[1] == 0 && currentLine[2] == 0)
                        {
                            byte* newLine = newfp + ((x / maskbpp) * newbpp) + (y * newData.Stride);
                            byte* imageLine = imagefp + ((x / maskbpp) * imagebpp) + (y * imageData.Stride);

                            newLine[3] = 255;
                            newLine[2] = imageLine[2];
                            newLine[1] = imageLine[1];
                            newLine[0] = imageLine[0];
                        }
                    }
                }
                mask.UnlockBits(maskData);
                newImage.UnlockBits(newData);
                image.UnlockBits(imageData);
            }

            return newImage;
        }

        internal Bitmap CropImage(Bitmap bm, bool FindStartingPoint = false, bool UsingBrightness = false)
        {
            int minX = bm.Width;
            int maxX = 0;
            int minY = bm.Height;
            int maxY = 0;
            int ystart = 0;
            int xstart = 0;
            int yend = 0;
            int xend = 0;

            unsafe
            {
                BitmapData bitmapData = bm.LockBits(new Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.ReadWrite, bm.PixelFormat);
                int bytesPerPixel = System.Drawing.Bitmap.GetPixelFormatSize(bm.PixelFormat) / 8;
                int heightInPixels = bitmapData.Height;
                int widthInBytes = bitmapData.Width * bytesPerPixel;
                byte* ptrFirstPixel = (byte*)bitmapData.Scan0;

                yend = bm.Height;
                xend = widthInBytes;

                if (FindStartingPoint)
                {
                    //Find minY
                    ystart = bm.Height / 2;
                    for (; ystart > 0; ystart--)
                    {
                        bool blocked = false;
                        for (int x = 0; x < widthInBytes; x += bytesPerPixel)
                        {
                            byte* currentLine = ptrFirstPixel + x + (ystart * bitmapData.Stride);
                            if (!UsingBrightness && currentLine[3] != 0)
                            {
                                blocked = true;
                                break;
                            }
                            if (UsingBrightness &&
                                currentLine[2] == 0 &&
                                currentLine[1] == 0 &&
                                currentLine[0] == 0)
                            {
                                blocked = true;
                                break;
                            }
                        }
                        if (blocked == false)
                        {
                            break;
                        }
                    }

                    //find maxY
                    yend = bm.Height / 2;
                    for (; yend < bm.Height; yend++)
                    {
                        bool blocked = false;
                        for (int x = 0; x < widthInBytes; x += bytesPerPixel)
                        {
                            byte* currentLine = ptrFirstPixel + x + (yend * bitmapData.Stride);
                            if (!UsingBrightness && currentLine[3] != 0)
                            {
                                blocked = true;
                                break;
                            }
                            if (UsingBrightness &&
                                currentLine[2] == 0 &&
                                currentLine[1] == 0 &&
                                currentLine[0] == 0)
                            {
                                blocked = true;
                                break;
                            }
                        }
                        if (blocked == false)
                        {
                            break;
                        }
                    }

                    //Find min X
                    xstart = (bm.Width / 2) * bytesPerPixel;
                    for (; xstart > 0; xstart -= bytesPerPixel)
                    {
                        bool blocked = false;
                        for (int y = ystart; y < heightInPixels; y++)
                        {
                            byte* currentLine = ptrFirstPixel + xstart + (y * bitmapData.Stride);
                            if (!UsingBrightness && currentLine[3] != 0)
                            {
                                blocked = true;
                                break;
                            }
                            if (UsingBrightness &&
                               currentLine[2] == 0 &&
                               currentLine[1] == 0 &&
                               currentLine[0] == 0)
                            {
                                blocked = true;
                                break;
                            }
                        }

                        if (blocked == false)
                        {
                            break;
                        }
                    }

                    //find maxX
                    xend = (bm.Width / 2) * bytesPerPixel;
                    for (; xend < widthInBytes; xend += bytesPerPixel)
                    {
                        bool blocked = false;
                        for (int y = ystart; y < heightInPixels; y++)
                        {
                            byte* currentLine = ptrFirstPixel + xend + (y * bitmapData.Stride);
                            if (!UsingBrightness && currentLine[3] != 0)
                            {
                                blocked = true;
                                break;
                            }
                            if (UsingBrightness &&
                               currentLine[2] == 0 &&
                               currentLine[1] == 0 &&
                               currentLine[0] == 0)
                            {
                                blocked = true;
                                break;
                            }
                        }

                        if (blocked == false)
                        {
                            break;
                        }
                    }

                    //Find minY based on minX and maxX
                    ystart = bm.Height / 2;
                    for (; ystart > 0; ystart--)
                    {
                        bool blocked = false;
                        for (int x = xstart; x < xend; x += bytesPerPixel)
                        {
                            byte* currentLine = ptrFirstPixel + x + (ystart * bitmapData.Stride);
                            if (!UsingBrightness && currentLine[3] != 0)
                            {
                                blocked = true;
                                break;
                            }
                            if (UsingBrightness &&
                                currentLine[2] == 0 &&
                                currentLine[1] == 0 &&
                                currentLine[0] == 0)
                            {
                                blocked = true;
                                break;
                            }
                        }
                        if (blocked == false)
                        {
                            break;
                        }
                    }

                    //Find maxY based on minX and maxX
                    yend = bm.Height / 2;
                    for (; yend < bm.Height; yend++)
                    {
                        bool blocked = false;
                        for (int x = xstart; x < xend; x += bytesPerPixel)
                        {
                            byte* currentLine = ptrFirstPixel + x + (yend * bitmapData.Stride);
                            if (!UsingBrightness && currentLine[3] != 0)
                            {
                                blocked = true;
                                break;
                            }
                            if (UsingBrightness &&
                                currentLine[2] == 0 &&
                                currentLine[1] == 0 &&
                                currentLine[0] == 0)
                            {
                                blocked = true;
                                break;
                            }
                        }
                        if (blocked == false)
                        {
                            break;
                        }
                    }
                }

                for (int x = xstart; x < xend; x += bytesPerPixel)
                {
                    for (int y = ystart; y < yend; y++)
                    {
                        byte* currentLine = ptrFirstPixel + x + (y * bitmapData.Stride);
                        if (!UsingBrightness && currentLine[3] != 0)
                        {
                            minY = Math.Min(minY, y);
                            maxY = Math.Max(maxY, y);
                            minX = Math.Min(minX, x);
                            maxX = Math.Max(maxX, x);
                        }
                        if (UsingBrightness &&
                               currentLine[2] == 0 &&
                               currentLine[1] == 0 &&
                               currentLine[0] == 0)
                        {
                            minY = Math.Min(minY, y);
                            maxY = Math.Max(maxY, y);
                            minX = Math.Min(minX, x);
                            maxX = Math.Max(maxX, x);
                        }
                    }
                }

                minX /= bytesPerPixel;
                maxX /= bytesPerPixel;

                bm.UnlockBits(bitmapData);
            }

            return CropImage(bm, new Rectangle(minX, minY, maxX - minX, maxY - minY));
        }

        internal Bitmap CropImage(Bitmap bm, Rectangle rect)
        {
            // create filter
            Crop filter = new Crop(rect);
            // apply the filter
            Bitmap newImage = filter.Apply(bm);

            return newImage;
        }

        internal Bitmap ResizeImage(Bitmap bm, int width, int height)
        {
            // create filter
            ResizeBilinear filter = new ResizeBilinear(width, height);
            // apply the filter
            Bitmap newImage = filter.Apply(bm);

            return newImage;
        }

        internal Bitmap RecolorImage(Bitmap bm)
        {
            unsafe
            {
                BitmapData bitmapData = bm.LockBits(new Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.ReadWrite, bm.PixelFormat);
                int bytesPerPixel = System.Drawing.Bitmap.GetPixelFormatSize(bm.PixelFormat) / 8;
                int heightInPixels = bitmapData.Height;
                int widthInBytes = bitmapData.Width * bytesPerPixel;
                byte* ptrFirstPixel = (byte*)bitmapData.Scan0;

                for (int x = 0; x < widthInBytes; x += bytesPerPixel)
                {
                    byte r = 0;
                    byte g = 0;
                    byte b = 0;
                    for (int y = 0; y < heightInPixels; y++)
                    {
                        byte* currentLine = ptrFirstPixel + x + (y * bitmapData.Stride);

                        if (currentLine[3] != 0)
                        {
                            r = currentLine[2];
                            g = currentLine[1];
                            b = currentLine[0];
                        }
                        else
                        {
                            currentLine[3] = 255;
                            currentLine[2] = r;
                            currentLine[1] = g;
                            currentLine[0] = b;
                        }

                    }
                }
                bm.UnlockBits(bitmapData);
            }

            //ConservativeSmoothing cs = new ConservativeSmoothing();
            ContrastCorrection cc = new ContrastCorrection();
            //ContrastStretch contrastStretch = new ContrastStretch();
            //Median median = new Median();

            cc.ApplyInPlace(bm);

            return bm;
        }

        internal Bitmap FrameImage(Bitmap orig, Bitmap template)
        {
            int xt_start = 800;
            int yt_start = 525;

            if (orig.PixelFormat != template.PixelFormat)
            {
                return null;
            }

            Bitmap newImage = new Bitmap(template);

            unsafe
            {
                BitmapData origpData = orig.LockBits(new Rectangle(0, 0, orig.Width, orig.Height), ImageLockMode.ReadWrite, orig.PixelFormat);
                BitmapData tempData = template.LockBits(new Rectangle(0, 0, template.Width, template.Height), ImageLockMode.ReadWrite, template.PixelFormat);
                BitmapData newData = newImage.LockBits(new Rectangle(0, 0, newImage.Width, newImage.Height), ImageLockMode.ReadWrite, newImage.PixelFormat);

                int bytesPerPixel = System.Drawing.Bitmap.GetPixelFormatSize(origpData.PixelFormat) / 8;
                int heightInPixels = origpData.Height;
                int widthInBytes = origpData.Width * bytesPerPixel;
                byte* origfp = (byte*)origpData.Scan0;
                byte* tempfp = (byte*)tempData.Scan0;
                byte* newfp = (byte*)newData.Scan0;

                int xt = xt_start * bytesPerPixel;
                int x = (origpData.Width / 2) * bytesPerPixel;
                for (; x < widthInBytes; x += bytesPerPixel)
                {
                    int yt = yt_start;
                    for (int y = heightInPixels / 2; y < heightInPixels; y++)
                    {
                        byte* origLine = origfp + x + (y * origpData.Stride);
                        byte* tempLine = tempfp + xt + (yt * tempData.Stride);
                        byte* newLine = newfp + xt + (yt * newData.Stride);

                        if (tempLine[3] != 0)
                        {
                            newLine[3] = origLine[3];
                            newLine[2] = origLine[2];
                            newLine[1] = origLine[1];
                            newLine[0] = origLine[0];
                        }
                        yt++;
                    }
                    xt += bytesPerPixel;
                }

                xt = xt_start * bytesPerPixel;
                x = (origpData.Width / 2) * bytesPerPixel;
                for (; x < widthInBytes; x += bytesPerPixel)
                {
                    int yt = yt_start;
                    for (int y = heightInPixels / 2; y > 0; y--)
                    {
                        byte* origLine = origfp + x + (y * origpData.Stride);
                        byte* tempLine = tempfp + xt + (yt * tempData.Stride);
                        byte* newLine = newfp + xt + (yt * newData.Stride);

                        if (tempLine[3] != 0)
                        {
                            newLine[3] = origLine[3];
                            newLine[2] = origLine[2];
                            newLine[1] = origLine[1];
                            newLine[0] = origLine[0];
                        }
                        yt--;

                        if (yt < 0) break;
                    }
                    xt += bytesPerPixel;
                }

                xt = xt_start * bytesPerPixel;
                x = (origpData.Width / 2) * bytesPerPixel;
                for (; x > 0; x -= bytesPerPixel)
                {
                    int yt = yt_start;
                    for (int y = heightInPixels / 2; y > 0; y--)
                    {
                        byte* origLine = origfp + x + (y * origpData.Stride);
                        byte* tempLine = tempfp + xt + (yt * tempData.Stride);
                        byte* newLine = newfp + xt + (yt * newData.Stride);

                        if (tempLine[3] != 0)
                        {
                            newLine[3] = origLine[3];
                            newLine[2] = origLine[2];
                            newLine[1] = origLine[1];
                            newLine[0] = origLine[0];
                        }
                        yt--;

                        if (yt < 0) break;
                    }
                    xt -= bytesPerPixel;
                    if (xt < 0) continue;
                }

                xt = xt_start * bytesPerPixel;
                x = (origpData.Width / 2) * bytesPerPixel;
                for (; x > 0; x -= bytesPerPixel)
                {
                    int yt = yt_start;
                    for (int y = heightInPixels / 2; y < heightInPixels; y++)
                    {
                        byte* origLine = origfp + x + (y * origpData.Stride);
                        byte* tempLine = tempfp + xt + (yt * tempData.Stride);
                        byte* newLine = newfp + xt + (yt * newData.Stride);

                        if (tempLine[3] != 0)
                        {
                            newLine[3] = 255;
                            newLine[2] = origLine[2];
                            newLine[1] = origLine[1];
                            newLine[0] = origLine[0];
                        }
                        yt++;
                    }
                    xt -= bytesPerPixel;
                    if (xt < 0) continue;
                }
                orig.UnlockBits(origpData);
                template.UnlockBits(tempData);
                newImage.UnlockBits(newData);
            }

            return newImage;

        }


        internal Bitmap AddText(Bitmap image, string text)
        {
            RectangleF rectf = new RectangleF(420, 1040, 800, 100);

            Graphics g = Graphics.FromImage(image);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            StringFormat drawFormat = new StringFormat
            {
                Alignment = StringAlignment.Center
            };

            g.DrawString(text, new Font("Arial", 32, FontStyle.Bold), Brushes.White, rectf, drawFormat);

            g.Flush();

            return image;
        }

        public static Tuple<int[], int[], int[]> GetHistogram(Bitmap OriginalImage)
        {
            if (OriginalImage == null) return null;

            var histogram_r = new int[256];
            var histogram_g = new int[256];
            var histogram_b = new int[256];


            var data = OriginalImage.LockBits(new Rectangle(0, 0, OriginalImage.Width, OriginalImage.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            var offset = data.Stride - OriginalImage.Width * 3;
            unsafe
            {
                var p = (byte*)data.Scan0.ToPointer();

                for (var i = 0; i < OriginalImage.Height; i++)
                {
                    for (var j = 0; j < OriginalImage.Width; j++, p += 3)
                    {
                        histogram_r[p[2]]++;
                        histogram_g[p[1]]++;
                        histogram_b[p[0]]++;
                    }
                    p += offset;
                }
            }

            OriginalImage.UnlockBits(data);
            OriginalImage.Dispose();

            return new Tuple<int[], int[], int[]>(histogram_r, histogram_g, histogram_b);
        }

    }
}
