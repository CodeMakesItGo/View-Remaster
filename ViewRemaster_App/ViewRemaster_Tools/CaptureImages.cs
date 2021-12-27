using AForge.Imaging.Filters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ViewRemaster_Tools
{
    public class CaptureImages : ViewRemasterBase
    {
        private readonly string[] CaptureOrder = { "Reel", "1L", "5R", "2L", "6R", "3L", "7R", "4L", "1R", "5L", "2R", "6L", "3R", "7L", "4R" };

        private string ReelSnapshotFileName = "";
        private string SlideSnapshotFileName = "";

        public bool TriggerSlideSnapshot { get; set; }
        public bool TriggerReelSnapshot { get; set; }

        private SerialPort serialPort = null;

        private bool AlignSlideTopGood = false;
        private bool AlignSlideBottomGood = false;

        private bool AlignSlideEnabled = false;
        private bool _CommWaiting = false;
        private bool CommWaiting { get { return _CommWaiting; } set { CommWaitingEvent?.Invoke(value); _CommWaiting = value; } }
        public bool UpsideDown { get; set; } = false;
        public int Threshold { get; set; }
        public bool ThresholdEnabled { get; set; } = false;

        private bool StopThread = false;
        public bool CommOpen { get; private set; } = false;
        public byte Brightness { private set; get; }

        public delegate void CommWaitingDelegate(bool waiting);
        public event CommWaitingDelegate CommWaitingEvent;

        public delegate void UpdateStatusDelegate(string text);
        public event UpdateStatusDelegate UpdateStatusEvent;

        public delegate void RecievedSerialDataDelegate(string data);
        public event RecievedSerialDataDelegate RecievedSerialDataEvent;


        public CaptureImages()
        {
            Path = "";
        }

        public void CaptureAllImages(string dir)
        {
            //Set our path
            CreatePath(dir);

            Thread thread = new Thread(new ThreadStart(StartCapture));

            StopThread = false;
            thread.Start();
        }

        public void StopCapture()
        {
            StopThread = true;
        }

        private void StartCapture()
        {
            //set to false to skip capturing
            AlignReel();
            WaitForSerial();

            if (UpsideDown)
            {
                //skip 7 slides to get righ side up
                for (int s = 0; s < 7; s++)
                {
                    NextSlide();
                    WaitForSerial();
                }
            }

            CurrentSlide = 0;                               

            Lights(true, false);
            ReelShot();

            while (CurrentSlide < CaptureOrder.Length - 1 && StopThread == false)
            {
                if (!UpsideDown || CurrentSlide != 0)
                {
                    NextSlide();
                    WaitForSerial();
                }

                CurrentSlide++;

                Lights(true, false);
                AlignSlide();
                TakeThresholdImage();
                ReelShot();

                Lights(false, true);
                TakeColorImage();

                UpdateStatusEvent?.Invoke($"Done Capture {CaptureOrder[CurrentSlide]} Images");
            }

            Lights(false, false);
        }

      

        public void AlignSlide()
        {
            AlignSlideEnabled = true;
            AlignSlideBottomGood = false;
            AlignSlideTopGood = false;

            while (AlignSlideEnabled)//AlignSlideBottomGood == false || AlignSlideTopGood == false)
            {

                Thread.Sleep(10);
                /*if (AlignSlideTopGood == false)
                {
                    InchDown();
                    WaitForSerial();
                }


                if (AlignSlideBottomGood == false)
                {
                    InchUp();
                    WaitForSerial();
                } */
            }

            //AlignSlideEnabled = false;
        }

        private void Lights(bool spotlight, bool backlight)
        {
            //if spotlight on
            if (backlight)
            {
                SetBrightness(Brightness);
            }
            else
            {
                SetBrightness(0, false);
            }
            WaitForSerial();
            Spotlight(spotlight);
            WaitForSerial();
            WaitForCameraAdjust();
        }

        private void ReelShot()
        {
            if (CurrentSlide == 0 || CaptureOrder[CurrentSlide].Contains("L"))
            {
                UpdateStatusEvent?.Invoke($"{CaptureOrder[CurrentSlide]} - text");
                var filename = $"{Path}\\{CaptureOrder[CurrentSlide]}_text.png";
                ReelSnapshotFileName = filename;
                TriggerReelSnapshot = true;
                WaitForReelPicture();
            }
        }

        private void TakeThresholdImage()
        {
            UpdateStatusEvent?.Invoke($"{CaptureOrder[CurrentSlide]} - threshold");
            
            var filename = $"{Path}\\{CaptureOrder[CurrentSlide]}_mask.png";
            SlideSnapshotFileName = filename;
            TriggerSlideSnapshot = true;
            WaitForSlidePicture();
        }

        private void TakeColorImage()
        {
            UpdateStatusEvent?.Invoke($"{CaptureOrder[CurrentSlide]} - color");

            var filename = $"{Path}\\{CaptureOrder[CurrentSlide]}.png";
            SlideSnapshotFileName = filename;
            TriggerSlideSnapshot = true;
            WaitForSlidePicture();
        }

        public void UpdateSnapshotImage(Bitmap frame, bool slide)
        {
            using (var bm = (Bitmap)frame.Clone())
            {
                bool upsideDown = CaptureOrder[CurrentSlide].Contains("R");
                if (upsideDown)
                {
                    bm.RotateFlip(RotateFlipType.Rotate180FlipNone);
                }

                if (slide && SlideSnapshotFileName != "")
                {
                    bm.Save(SlideSnapshotFileName);
                    SlideSnapshotFileName = "";
                }

                if (!slide && ReelSnapshotFileName != "")
                {
                    bm.Save(ReelSnapshotFileName);
                    ReelSnapshotFileName = "";
                }
            }
        }

        public BitmapImage UpdateSlideImage(Bitmap frame, bool align_slide, System.Windows.Size videoResolution)
        {
            BitmapImage bi;

            using (var bm = (Bitmap)frame.Clone())
            {
                bool upsideDown = CaptureOrder[CurrentSlide].Contains("R");
                if (upsideDown)
                {
                    bm.RotateFlip(RotateFlipType.Rotate180FlipNone);
                }

                //top and bottom gap size
                int gap = 30;

                if (align_slide)
                {
                    using (Graphics gr = Graphics.FromImage(bm))
                    {
                        using (Pen thick_pen = new Pen(Color.Blue, 8))
                        {
                            gr.DrawLine(thick_pen, 0, gap, bm.Width, gap);
                            gr.DrawLine(thick_pen, 0, bm.Height - gap, bm.Width, bm.Height - gap);
                        }
                    }
                }

                if (ThresholdEnabled || AlignSlideEnabled)
                {
                    using (var grayscaledBitmap = Grayscale.CommonAlgorithms.BT709.Apply(bm))
                    using (var thresholdedBitmap = new Threshold(Threshold).Apply(grayscaledBitmap))
                    {
                        bi = thresholdedBitmap.ToBitmapImage();

                        if (AlignSlideEnabled)
                        {
                            int center = thresholdedBitmap.Width / 2;
                            int width = 30;
                            

                            //test 3 dots top
                            if (thresholdedBitmap.GetPixel(center, gap).Name.Equals("ffffffff") &&
                                thresholdedBitmap.GetPixel(center + width, gap).Name.Equals("ffffffff") &&
                                thresholdedBitmap.GetPixel(center - width, gap).Name.Equals("ffffffff"))
                            {
                                if (upsideDown) AlignSlideBottomGood = true; else AlignSlideTopGood = true;
                            }
                            else
                            {
                                if (upsideDown) AlignSlideBottomGood = false; else AlignSlideTopGood = false;
                            }

                            //test 3 dots bottom
                            if (thresholdedBitmap.GetPixel(center, thresholdedBitmap.Height - gap).Name.Equals("ffffffff") &&
                               thresholdedBitmap.GetPixel(center + width, thresholdedBitmap.Height - gap).Name.Equals("ffffffff") &&
                               thresholdedBitmap.GetPixel(center - width, thresholdedBitmap.Height - gap).Name.Equals("ffffffff"))
                            {
                                if (upsideDown) AlignSlideTopGood = true; else AlignSlideBottomGood = true;
                            }
                            else
                            {
                                if (upsideDown) AlignSlideTopGood = false; else AlignSlideBottomGood = false;
                            }

                            if (AlignSlideTopGood == false)
                            {
                                InchDown();
                                WaitForSerial();
              
                            }


                            if (AlignSlideBottomGood == false)
                            {
                                InchUp();
                                WaitForSerial();
     
                            }

                            if(AlignSlideTopGood && AlignSlideBottomGood)
                                AlignSlideEnabled = false;
                        }
                    }
                }
                else
                {
                    bi = bm.ToBitmapImage();
                }

            }
            bi.Freeze(); // avoid cross thread operations and prevents leaks

            return bi;
        }

        public BitmapImage UpdateReelImage(Bitmap frame, bool align_reel, System.Windows.Size videoResolution)
        {
            BitmapImage bi;
            using (var bitmap = (Bitmap)frame.Clone())
            {
                if (align_reel)
                {
                    using (Graphics gr = Graphics.FromImage(bitmap))
                    {
                        double xfactor = ReelWidth / videoResolution.Width;
                        double yfactor = ReelHeight / videoResolution.Height;

                        //Draw the out and inner circle on the reel live image
                        //Rectangle rectOuter = new Rectangle((int)(Settings.Instance.ReelCrop_X / xfactor), (int)(Settings.Instance.ReelCrop_Y / yfactor), (int)(Settings.Instance.ReelCrop_Width / xfactor), (int)(Settings.Instance.ReelCrop_Width / yfactor));
                        //var x = (Settings.Instance.ReelCrop_X / xfactor + (Settings.Instance.ReelCrop_Width / xfactor / 2)) - (Settings.Instance.ReelCropCenter_Width / xfactor / 2);
                        //var y = (Settings.Instance.ReelCrop_Y / yfactor + (Settings.Instance.ReelCrop_Width / yfactor / 2)) - (Settings.Instance.ReelCropCenter_Width / yfactor / 2);
                        var x = (frame.Width / 2) - (Settings.Instance.ReelCropCenter_Width / xfactor / 2);
                        var y = (frame.Height / 2) - (Settings.Instance.ReelCropCenter_Width / xfactor / 2);
                        Rectangle rectInner = new Rectangle((int)x, (int)y, (int)(Settings.Instance.ReelCropCenter_Width / xfactor), (int)(Settings.Instance.ReelCropCenter_Width / yfactor));

                        using (Pen thick_pen = new Pen(Color.Blue, 8))
                        {
                            //gr.DrawEllipse(thick_pen, rectOuter);
                            gr.DrawEllipse(thick_pen, rectInner);
                        }

                        //Draw the OCR text crop box
                       // Rectangle textBox = new Rectangle((int)(Settings.Instance.TextCrop_X / xfactor), (int)(Settings.Instance.TextCrop_Y / yfactor), (int)(Settings.Instance.TextCrop_Width / xfactor), (int)(Settings.Instance.TextCrop_Height / yfactor));
                       // using (Pen thick_pen = new Pen(Color.Red, 8))
                       // {
                       //     gr.DrawRectangle(thick_pen, textBox);
                       // }
                    }
                }
                bi = bitmap.ToBitmapImage();

             
            }
            bi.Freeze(); // avoid cross thread operations and prevents leaks
            return bi;
        }

        private void WaitForSerial()
        {
            while (CommWaiting && CommOpen)
            {
                System.Threading.Thread.Sleep(10);
            }
        }

        private void WaitForSlidePicture()
        {
            while (SlideSnapshotFileName != "")
            {
                System.Threading.Thread.Sleep(10);
            }
        }

        private void WaitForReelPicture()
        {
            while (ReelSnapshotFileName != "")
            {
                System.Threading.Thread.Sleep(10);
            }
        }

        private void WaitForCameraAdjust()
        {
            System.Threading.Thread.Sleep(3000);
        }

        public void ConnectSerial(string port)
        {
            serialPort = new SerialPort
            {
                // Set the read/write timeouts
                ReadTimeout = 500,
                WriteTimeout = 500,

                PortName = port,
                BaudRate = 9600,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None
            };
            serialPort.DataReceived += SerialPort_DataReceived;

            serialPort.Open();
            CommOpen = true;

        }

        public void CloseSerial()
        {
            serialPort?.Close();
            CommOpen = false;
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            do
            {
                var data = serialPort.ReadLine();
                RecievedSerialDataEvent?.Invoke(data);

                if (data.Contains("DONE")) CommWaiting = false;

            } while (serialPort.BytesToRead > 0);
        }

        private void WriteSerial(string s)
        {
            if (CommOpen)
            {
                CommWaiting = true;
                serialPort.WriteLine(s);
               // WaitForSerial();
            }
        }

        public void SetColor(byte R, byte G, byte B)
        {
            WriteSerial($"C{R},{G},{B}");
        }

        public void SetBrightness(byte brightness, bool save = true)
        {
            //Save the brightness
            if(save)
                Brightness = brightness;

            WriteSerial($"B{brightness}");
        }

        public void Spotlight(bool on)
        {
            WriteSerial($"L{(on ? "1":"0")}");
        }

        public void NextSlide()
        {
            WriteSerial($"N");
        }

        public void AlignReel()
        {
            WriteSerial($"A");
        }

        public void StopReel()
        {
            WriteSerial($"S");
        }

        public void InchUp()
        {
            WriteSerial($"U");
        }
 
        public void InchDown()
        {
            WriteSerial($"D");
        }

    }
}
