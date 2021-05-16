using AForge.Imaging.Filters;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ViewRemaster_Tools
{
    public class PrepairImages : ViewRemasterBase
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

                        //If the mask color is black, then add this pixel to the new image
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

        public Bitmap CropToCircle(Bitmap image, Point center, int radius, Color backGround)
        {
            Bitmap dstImage = new Bitmap(image.Width, image.Height, image.PixelFormat);

            using (Graphics g = Graphics.FromImage(dstImage))
            {
                RectangleF r = new RectangleF(center.X - radius, center.Y - radius,
                                                         radius * 2, radius * 2);

                // enables smoothing of the edge of the circle (less pixelated)
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // fills background color
                using (Brush br = new SolidBrush(backGround))
                {
                    g.FillRectangle(br, 0, 0, dstImage.Width, dstImage.Height);
                }

                // adds the new ellipse & draws the image again 
                GraphicsPath path = new GraphicsPath();
                path.AddEllipse(r);
                g.SetClip(path);
                g.DrawImage(image, 0, 0);
            }
            return dstImage;
        }

        internal Bitmap ResizeImage(Bitmap bm, int width, int height)
        {
            if (bm.Width == width && bm.Height == height)
                return bm;

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

            ContrastCorrection cc = new ContrastCorrection();
            cc.ApplyInPlace(bm);

            return bm;
        }

        public Bitmap CropToRoundedRectangle(Bitmap image, Rectangle bounds, int cornerRadius, Color backGround)
        {
            Bitmap dstImage = new Bitmap(Settings.Instance.SlideCrop_Width, Settings.Instance.SlideCrop_Height, image.PixelFormat);

            
            using (Graphics g = Graphics.FromImage(dstImage))
            {
                // enables smoothing of the edge of the circle (less pixelated)
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // fills background color
                using (Brush br = new SolidBrush(backGround))
                {
                    g.FillRectangle(br, 0, 0, dstImage.Width, dstImage.Height);
                }

                using (GraphicsPath path = RoundedRect(bounds, cornerRadius))
                {               
                    g.SetClip(path);
                    g.DrawImage(image, new Rectangle(0, 0, dstImage.Width, dstImage.Height), new Rectangle(Settings.Instance.SlideCrop_X, Settings.Instance.SlideCrop_Y, dstImage.Width, dstImage.Height), GraphicsUnit.Pixel);
                }
            }

            return dstImage;
        }

        public GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            // top left arc  
            path.AddArc(arc, 180, 90);

            // top right arc  
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // bottom right arc  
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // bottom left arc 
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
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

        internal Bitmap FrameImage(Bitmap image)
        {
            Bitmap dstImage = new Bitmap(image.Width, SlideHeight, image.PixelFormat);

            using (Graphics grD = Graphics.FromImage(dstImage))
            {
                grD.FillRectangle(Brushes.Black, 0, 0, image.Width, SlideHeight);

                var rect = new Rectangle()
                {
                    X = 0,
                    Y = Settings.Instance.Slide_Top,
                    Width = image.Width,
                    Height = image.Height
                };

                //Create the framed image
                grD.DrawImage(image, rect, new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
             
            }
            return dstImage;
        }

        internal Bitmap AddText(Bitmap image, string text)
        {
            using (Graphics g = Graphics.FromImage(image))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                StringFormat drawFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center
                };

                var rect = new Rectangle()
                {
                    X = 0,
                    Y = Settings.Instance.SlideText_Top,
                    Width = image.Width,
                    Height = image.Height - Settings.Instance.SlideText_Top
                };

                g.DrawString(text, new Font("Arial", 32, FontStyle.Bold), Brushes.White, rect, drawFormat);
            }

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
