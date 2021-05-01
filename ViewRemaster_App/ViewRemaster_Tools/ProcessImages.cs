using AForge.Imaging.Filters;
using System;
using System.Collections.Generic;
using System.Drawing;
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

        public ProcessImages()
        {
            Path = "";
            TemplatePath = rootPath + TemplatePath;
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
                Rotation_angle = null;

                Thread thread = new Thread(new ThreadStart(ProcessImagesThread));
                thread.Start();
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
                Finalize3DImage();
            }
            SetLabelStatusEvent?.Invoke($"Done Process {ProcessOrder[CurrentSlide]} Images");
        }


        private void ReelImage()
        {
            var imagePath = $"{Path}\\Reel_text.png";
            var maskPath = $"{TemplatePath}\\reel_mask.png";
            SetImageEvent?.Invoke(ImageType.REEL, imagePath);

            using (Bitmap reel = new Bitmap(imagePath))
            using (Bitmap template = new Bitmap(maskPath))
            {
                PrepairImages pi = new PrepairImages();
                var reelImage = pi.MaskImage(reel, template);
                reelImage.Save($"{ProcessPath}\\0R.png");
                reelImage.Save($"{ProcessPath}\\0L.png");

                var rsReel = pi.ResizeReelImage(reelImage);
                rsReel.Save($"{ProcessPath}\\0R_final.png");
                rsReel.Save($"{ProcessPath}\\0L_final.png");
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
            //using (Bitmap textmask = new Bitmap(templatePath + @"\\text_mask.png"))
            using (var grayscaledBitmap = Grayscale.CommonAlgorithms.BT709.Apply(textInput))
            using (var thresholdedBitmap = new Threshold(Ocr_threshold).Apply(grayscaledBitmap))
            {
                //var textmap = pi.MaskImage(thresholdedBitmap, textmask);
                var image_crop = pi.CropImage(thresholdedBitmap, new Rectangle(820, 170, 270, 160));

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
                if (Rotation_angle.HasValue == false)
                    angle = pi.FindRotation(mask);
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

        private void Finalize3DReel()
        {
            //crop
            //550 1400
            int ReelWidth = 875;

            var filename = $"{ProcessPath}\\0R.png";

            using (Bitmap i = new Bitmap(filename))
            {
                PrepairImages pi = new PrepairImages();
                var left = pi.CropImage(i, new Rectangle(525, 0, ReelWidth, 1080));

                using (Bitmap bitmap = new Bitmap(1920, 1080, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                using (Graphics grD = Graphics.FromImage(bitmap))
                {
                    grD.FillRectangle(Brushes.Black, 0, 0, 1920, 1080);
                    grD.DrawImage(left, new Rectangle(75, 0, ReelWidth, 1080), new Rectangle(0, 0, ReelWidth, 1080), GraphicsUnit.Pixel);
                    grD.DrawImage(left, new Rectangle(970, 0, ReelWidth, 1080), new Rectangle(0, 0, ReelWidth, 1080), GraphicsUnit.Pixel);

                    filename = $"{FinalPath}\\0_final.png";
                    bitmap.Save(filename);
                    SetImageEvent?.Invoke(ImageType.FINAL, filename);
                }
            }
        }

        private void Finalize3DImage()
        {
            //crop
            //x260 - 1345 = 1085 width of slide
            //Make 3D image
            if (ProcessOrder[CurrentSlide].Contains("L"))
            {
                return;
            }

            var filenamer = $"{ProcessPath}\\{ProcessOrder[CurrentSlide]}_final.png";
            var leftFile = ProcessOrder[CurrentSlide].Replace('R', 'L');
            var filenamel = $"{ProcessPath}\\{leftFile}_final.png";
            int offset = 20;

            using (Bitmap ir = new Bitmap(filenamer))
            using (Bitmap il = new Bitmap(filenamel))
            {
                PrepairImages pi = new PrepairImages();

                float totalWidth = 1085 + (offset * 2);
                float totalHeight = 1200;

                var left = pi.CropImage(il, new Rectangle(260 - offset, 0, (int)totalWidth, 1200));
                var right = pi.CropImage(ir, new Rectangle(260 - offset, 0, (int)totalWidth, 1200));

                float ratio = totalWidth / totalHeight;
                float resizeRatio = 960.0f / ratio;
                int offsetHeight = (int)((1080 - (int)resizeRatio) / 2.0f);

                left = pi.ResizeImage(left, 960, (int)resizeRatio);
                right = pi.ResizeImage(right, 960, (int)resizeRatio);

                using (Bitmap bitmap = new Bitmap(1920, 1080, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                using (Graphics grD = Graphics.FromImage(bitmap))
                {
                    grD.FillRectangle(Brushes.Black, 0, 0, 1920, 1080);
                    grD.DrawImage(left, new Rectangle(0, offsetHeight, 960, (int)resizeRatio), new Rectangle(0, 0, 960, (int)resizeRatio), GraphicsUnit.Pixel);
                    grD.DrawImage(right, new Rectangle(960, offsetHeight, 960, (int)resizeRatio), new Rectangle(0, 0, 960, (int)resizeRatio), GraphicsUnit.Pixel);

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
                var image_colored = pi.RecolorImage(image_crop);

                var filename_cropped = $"{ProcessPath}\\{ProcessOrder[CurrentSlide]}_cropped.png";
                image_colored.Save(filename_cropped);
                SetImageEvent?.Invoke(ImageType.CROPPED, filename_cropped);

                var template = new Bitmap($"{TemplatePath}\\slide_1600_1200.png");
                var image_framed = pi.FrameImage(image_colored, template);

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
