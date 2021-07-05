using AForge.Imaging.Filters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TesseractSharp;

namespace ViewRemaster_Tools
{
    public class ProcessImages : ViewRemasterBase
    {
        
        public readonly string[] ProcessOrder = { "Reel", "1L", "1R", "2L", "2R", "3L", "3R", "4L", "4R", "5L", "5R", "6L", "6R", "7L", "7R" };
        public int Ocr_threshold { get; set; } = 150;
        private int Last_Ocr_threshold = 0;
        public int Mask_threshold { get; set; } = 100;
        public double? Rotation_angle { get; set; } = null;

        public enum ImageType { SLIDE, REEL, MASK, THREASHOLD, ROTATED, CROPPED, TEXT, FINAL};
        public enum TextType { POPUP, OCR, ANGLE};
        
        public delegate void SetLabelStatusDelegate(string text);
        public event SetLabelStatusDelegate SetLabelStatusEvent;

        public delegate void SetImageDelegate(ImageType imageType, string path);
        public event SetImageDelegate SetImageEvent;

        public delegate void SetTextboxDelegate(TextType textBox, string text);
        public event SetTextboxDelegate SetTextboxEvent;

        private Thread process_thread;
        private int PreviousSlide = 0;

        public ProcessImages()
        {
            Path = "";
            Last_Ocr_threshold = Ocr_threshold;
        }

        public void UpdateOcrText(string text)
        {
            if (CurrentSlide > ProcessOrder.Length)
                return;

            var textFile = ProcessOrder[CurrentSlide].Replace('R', 'L');
            var filename = $"{ProcessPath}\\{textFile}_text_masked.txt";

            using (StreamWriter sw = new StreamWriter(filename))
            {
                sw.Write(text);
            }
        }


        public void ProcessImage()
        {
            if (Path != "")
            {
                if(process_thread != null &&
                   process_thread.ThreadState == ThreadState.Running)
                {
                    return;
                }
                process_thread = new Thread(new ThreadStart(ProcessImagesThread))
                {
                    Name = "Process Thread"
                };

                process_thread.Start();
            }
        }

        private void ProcessImagesThread()
        {
            if (CurrentSlide == 0)
            {
                ReelImage();
                Finalize3DReel();
            }
            else
            {
                ThresholdImage();
                TextOCR();
                RotateImage();
                Finalize2DImage();
                Finalize3DSlide();
            }
            SetLabelStatusEvent?.Invoke($"Done Process {ProcessOrder[CurrentSlide]} Images");
        }

        /// <summary>
        /// Creates the cropped reel image
        /// </summary>
        private void ReelImage()
        {
            var imagePath = $"{Path}\\Reel_text.png";
            SetImageEvent?.Invoke(ImageType.REEL, imagePath);

            if (!File.Exists(imagePath))
            {
                SetTextboxEvent(TextType.POPUP, $"File name does not exist {imagePath}.");
                return;
            }

            //Crop to the reel
            using (Bitmap reel = new Bitmap(imagePath))
            {
                PrepairImages pi = new PrepairImages();
                Point center = new Point { X = Settings.Instance.ReelCrop_X + (Settings.Instance.ReelCrop_Width / 2),
                                           Y = Settings.Instance.ReelCrop_Y + (Settings.Instance.ReelCrop_Width / 2)};

                var reelImage = pi.CropToCircle(reel, center, Settings.Instance.ReelCrop_Width / 2, Color.Black);

                //Draw the center black circle
                using (Graphics gr = Graphics.FromImage(reelImage))
                {
                    var x = (Settings.Instance.ReelCrop_X + (Settings.Instance.ReelCrop_Width / 2)) - (Settings.Instance.ReelCropCenter_Width / 2);
                    var y = (Settings.Instance.ReelCrop_Y + (Settings.Instance.ReelCrop_Width / 2)) - (Settings.Instance.ReelCropCenter_Width / 2);
                    Rectangle rectInner = new Rectangle(x, y, Settings.Instance.ReelCropCenter_Width, Settings.Instance.ReelCropCenter_Width);

                    gr.FillEllipse(Brushes.Black, rectInner);
                }

                reelImage = pi.ResizeImage(reelImage, OutputWidth, OutputHeight);
                reelImage.Save($"{ProcessPath}\\0R_final.png");
                reelImage.Save($"{ProcessPath}\\0L_final.png");
            }
        }

        private void ThresholdImage()
        {
            var input = $"{Path}\\{ProcessOrder[CurrentSlide]}_mask.png";
            SetImageEvent?.Invoke(ImageType.MASK, input);

            using (var bitmap = new Bitmap(input))
            using (var grayscaledBitmap = Grayscale.CommonAlgorithms.BT709.Apply(bitmap))
            using (var thresholdedBitmap = new Threshold(Mask_threshold).Apply(grayscaledBitmap))
            {
                var filename = $"{ProcessPath}\\{ProcessOrder[CurrentSlide]}_threshold.png";
                thresholdedBitmap.Save(filename);
                SetImageEvent?.Invoke(ImageType.THREASHOLD, filename);
            }
        }

        private void TextOCR()
        {
            var slide = ProcessOrder[CurrentSlide];

            bool exitEarly = false;

            if (slide.Contains("R"))
            {
                slide = slide.Replace('R', 'L');
                exitEarly = true;
            }
            var filename = $"{ProcessPath}\\{slide}_text_masked.png";
            var textFile = filename.Replace(".png", ".txt");
            if (File.Exists(textFile))
            {
                var str = "";
                using (StreamReader sr = new StreamReader(textFile))
                {
                    str = sr.ReadToEnd();
                }

                SetTextboxEvent?.Invoke(TextType.OCR, str);
                SetImageEvent?.Invoke(ImageType.TEXT, filename);

                if (str != "" && Last_Ocr_threshold == Ocr_threshold)
                    return;
            }

            var input = $"{Path}\\{ProcessOrder[CurrentSlide]}_text.png";
            SetImageEvent?.Invoke(ImageType.REEL, input);

            if (exitEarly)
            {
                return;
            }

            Last_Ocr_threshold = Ocr_threshold;
            SetLabelStatusEvent?.Invoke($"{ProcessOrder[CurrentSlide]} - OCR");

            PrepairImages pi = new PrepairImages();
            using (Bitmap textInput = new Bitmap(input))
            
            using (var grayscaledBitmap = Grayscale.CommonAlgorithms.BT709.Apply(textInput))
            using (var thresholdedBitmap = new Threshold(Ocr_threshold).Apply(grayscaledBitmap))
            {
                var image_crop = pi.CropImage(thresholdedBitmap, new Rectangle(Settings.Instance.TextCrop_X, Settings.Instance.TextCrop_Y, Settings.Instance.TextCrop_Width, Settings.Instance.TextCrop_Height));

                image_crop.Save(filename);
                SetImageEvent?.Invoke(ImageType.TEXT, filename);

                using (var stream = Tesseract.ImageToTxt(image_crop))
                {
                    StreamReader reader = new StreamReader(stream);
                    string str = reader.ReadToEnd();

                    str = str.Replace('\n', ' ');
                    str = str.Replace("  ", " "); //do this twice
                    str = str.Replace("  ", " ");
                    Regex rgx = new Regex("[^a-zA-Z, -]");
                    str = rgx.Replace(str, "");

                    SetTextboxEvent?.Invoke(TextType.OCR, str);
                }
            }
        }

        private void RotateImage()
        {
            SetLabelStatusEvent?.Invoke($"{ProcessOrder[CurrentSlide]} - Rotate");

            var filename_mask = $"{ProcessPath}\\{ProcessOrder[CurrentSlide]}_threshold.png";
            var filename_image = $"{Path}\\{ProcessOrder[CurrentSlide]}.png";
            SetImageEvent?.Invoke(ImageType.SLIDE, filename_image);

            using (Bitmap mask = new Bitmap(filename_mask))
            using (Bitmap image = new Bitmap(filename_image))
            {
                PrepairImages pi = new PrepairImages();

                double angle = 0.0;
                if (PreviousSlide != CurrentSlide)
                {
                    angle = pi.FindRotation(mask);
                    Rotation_angle = angle;
                    PreviousSlide = CurrentSlide;
                }
                else
                    angle = Rotation_angle.Value;

                SetTextboxEvent?.Invoke(TextType.ANGLE, angle.ToString("0.##"));

                var imageR = pi.RotateImage(image, angle);
                var maskR = pi.RotateImage(mask, angle);
                var bm = pi.MaskImage(imageR, maskR);
               
                var filename = $"{ProcessPath}\\{ProcessOrder[CurrentSlide]}_rot.png";
                bm.Save(filename);
                SetImageEvent?.Invoke(ImageType.ROTATED, filename);
            }
        }

        /// <summary>
        /// Creates the first side-by-side image that is a picture of the reel
        /// </summary>
        private void Finalize3DReel()
        {
            var filename = $"{ProcessPath}\\0R_final.png";

            if (!File.Exists(filename))
            {
                SetTextboxEvent(TextType.POPUP, $"File name does not exist {filename}.");
                return;
            }

            using (Bitmap i = new Bitmap(filename))
            {
                PrepairImages pi = new PrepairImages();

                //Crop to just the reel width
                var left = pi.CropImage(i, new Rectangle(Settings.Instance.ReelCrop_X, 0, Settings.Instance.ReelCrop_Width, ReelHeight));

                float totalWidth = Settings.Instance.ReelCrop_Width + (Settings.Instance.Slide_Gap);
                float totalHeight = ReelHeight;

                var halfWidth = (OutputWidth / 2);
                var halfGap = (Settings.Instance.Slide_Gap / 2);
          
                float ratio = totalWidth / totalHeight;
                int resizeRatio = (int)(halfWidth / ratio);
                int offsetHeight = ((OutputHeight - resizeRatio) / 2);

                //left = pi.ResizeImage(left, halfWidth, (int)resizeRatio);

                using (Bitmap bitmap = new Bitmap(OutputWidth, OutputHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                using (Graphics grD = Graphics.FromImage(bitmap))
                {
                    grD.SmoothingMode = SmoothingMode.AntiAlias;

                    //Black background
                    grD.FillRectangle(Brushes.Black, 0, 0, OutputWidth, OutputHeight);

                    //Left image resized
                    grD.DrawImage(left, new Rectangle(halfGap, offsetHeight, halfWidth - Settings.Instance.Slide_Gap, resizeRatio), new Rectangle(0, 0, Settings.Instance.ReelCrop_Width, ReelHeight), GraphicsUnit.Pixel);

                    //Right image resized, same image as left
                    grD.DrawImage(left, new Rectangle(halfWidth + halfGap, offsetHeight, halfWidth - Settings.Instance.Slide_Gap, resizeRatio), new Rectangle(0, 0, Settings.Instance.ReelCrop_Width, ReelHeight), GraphicsUnit.Pixel);

                    filename = $"{FinalPath}\\0_final.png";
                    bitmap.Save(filename);
                    SetImageEvent?.Invoke(ImageType.FINAL, filename);
                }
            }
        }

        /// <summary>
        /// Creates each Side-by-side slide
        /// </summary>
        private void Finalize3DSlide()
        {
            if (ProcessOrder[CurrentSlide].Contains("L"))
            {
                return;
            }

            var filenamer = $"{ProcessPath}\\{ProcessOrder[CurrentSlide]}_final.png";
            var leftFile = ProcessOrder[CurrentSlide].Replace('R', 'L');
            var filenamel = $"{ProcessPath}\\{leftFile}_final.png";

            if (!File.Exists(filenamer))
            {
                SetTextboxEvent(TextType.POPUP, $"File name does not exist {filenamer}.");
                return;
            }

            if (!File.Exists(filenamel))
            {
                SetTextboxEvent(TextType.POPUP, $"File name does not exist {filenamel}.");
                return;
            }

            using (Bitmap ir = new Bitmap(filenamer))
            using (Bitmap il = new Bitmap(filenamel))
            {
                PrepairImages pi = new PrepairImages();
                
                float totalWidth = Settings.Instance.SlideCrop_Width + (Settings.Instance.Slide_Gap);
                float totalHeight = SlideHeight;

                var halfWidth = (OutputWidth / 2);
                var halfGap = (Settings.Instance.Slide_Gap / 2);
      
                float ratio = totalWidth / totalHeight;
                int resizeHeight = (int)((halfWidth) / ratio);
                int offsetHeight = ((OutputHeight - resizeHeight) / 2);

                //Parallel VR viewing
                using (Bitmap bitmap = new Bitmap(OutputWidth, OutputHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                using (Graphics grD = Graphics.FromImage(bitmap))
                {
                    grD.FillRectangle(Brushes.Black, 0, 0, OutputWidth, OutputHeight);

                    grD.DrawImage(il, new Rectangle(halfGap, offsetHeight, halfWidth - Settings.Instance.Slide_Gap, resizeHeight), new Rectangle(0, 0, Settings.Instance.SlideCrop_Width, SlideHeight), GraphicsUnit.Pixel);
                    grD.DrawImage(ir, new Rectangle(halfWidth + halfGap, offsetHeight, halfWidth - Settings.Instance.Slide_Gap, resizeHeight), new Rectangle(0, 0, Settings.Instance.SlideCrop_Width, SlideHeight), GraphicsUnit.Pixel);

                    var filename = $"{FinalPath}\\{ProcessOrder[CurrentSlide].Substring(0, 1)}_final.png";
                    bitmap.Save(filename);
                }

            }
        }

        private void Finalize2DImage()
        {
            SetLabelStatusEvent?.Invoke($"{ProcessOrder[CurrentSlide]} - Finalize");

            var filename_image = $"{ProcessPath}\\{ProcessOrder[CurrentSlide]}_rot.png";
            using (Bitmap image_rot = new Bitmap(filename_image))
            {
                PrepairImages pi = new PrepairImages();

                var image_crop = pi.CropImage(image_rot, true);
                image_crop = pi.RecolorImage(image_crop);
                var filename_cropped = $"{ProcessPath}\\{ProcessOrder[CurrentSlide]}_cropped.png";
                image_crop.Save(filename_cropped);

                var rect = new Rectangle()
                {
                    X = 0,
                    Y = 0,
                    Width = Settings.Instance.SlideCrop_Width,
                    Height = Settings.Instance.SlideCrop_Height
                };


                //Draw Rounded Rect crop line
                var image_trimmed = pi.CropToRoundedRectangle(image_crop, rect, Settings.Instance.SlideCrop_Corner, Color.Black, true);
                var filename_trimmed = $"{ProcessPath}\\{ProcessOrder[CurrentSlide]}_cropped_trimmed.png";
                image_trimmed.Save(filename_trimmed);
                SetImageEvent?.Invoke(ImageType.CROPPED, filename_trimmed);

                //Crop rounded Rect
                image_trimmed = pi.CropToRoundedRectangle(image_crop, rect, Settings.Instance.SlideCrop_Corner, Color.Black);

                var filename_frammed = $"{ProcessPath}\\{ProcessOrder[CurrentSlide]}_cropped_frammed.png";
                var image_framed = pi.FrameImage(image_trimmed);
                image_framed.Save(filename_frammed);

                var textFile = ProcessOrder[CurrentSlide].Replace('R', 'L');
                var filename = $"{ProcessPath}\\{textFile}_text_masked.txt";
                if (File.Exists(filename))
                {
                    using (StreamReader sr = new StreamReader(filename))
                    {
                        var str = sr.ReadToEnd();

                        var image = pi.AddText(image_framed, str);
                        filename = $"{ProcessPath}\\{ProcessOrder[CurrentSlide]}_final.png";
                        image.Save(filename);
                        SetImageEvent?.Invoke(ImageType.FINAL, filename);
                    }
                }
                else
                {
                    SetTextboxEvent?.Invoke(TextType.POPUP, $"{filename} Does not exist. Can't build final frame.");
                }
            }
        }
    }
}
