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

        private SerialPort serialPort = null;

        private bool _CommWaiting = false;
        private bool CommWaiting { get { return _CommWaiting; } set { CommWaitingEvent?.Invoke(value); _CommWaiting = value; } }
        public bool UpsideDown { get; set; } = false;

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
            TemplatePath = rootPath + TemplatePath;
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
                TakeThresholdImage();
                ReelShot();

                Lights(false, true);
                TakeColorImage();

                UpdateStatusEvent?.Invoke($"Done Capture {CaptureOrder[CurrentSlide]} Images");
            }

            Lights(false, false);
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
                WaitForReelPicture();
            }
        }

        private void TakeThresholdImage()
        {
            UpdateStatusEvent?.Invoke($"{CaptureOrder[CurrentSlide]} - threshold");
            
            var filename = $"{Path}\\{CaptureOrder[CurrentSlide]}_mask.png";
            SlideSnapshotFileName = filename;
            WaitForSlidePicture();
        }

        private void TakeColorImage()
        {
            UpdateStatusEvent?.Invoke($"{CaptureOrder[CurrentSlide]} - color");

            var filename = $"{Path}\\{CaptureOrder[CurrentSlide]}.png";
            SlideSnapshotFileName = filename;
            WaitForSlidePicture();
        }

        public BitmapImage UpdateSlideImage(Bitmap frame)
        {
            BitmapImage bi;

            using (var bm = (Bitmap)frame.Clone())
            {
                if (CaptureOrder[CurrentSlide].Contains("R"))
                {
                    bm.RotateFlip(RotateFlipType.Rotate180FlipNone);
                }

                bi = bm.ToBitmapImage();

                if (SlideSnapshotFileName != "")
                {
                    bm.Save(SlideSnapshotFileName);
                    SlideSnapshotFileName = "";
                }

            }
            bi.Freeze(); // avoid cross thread operations and prevents leaks

            return bi;
        }

        public BitmapImage UpdateReelImage(Bitmap frame, bool align_reel)
        {
            BitmapImage bi;
            using (var bitmap = (Bitmap)frame.Clone())
            {
                if (align_reel)
                {
                    var maskPath = $"{TemplatePath}\\reel_mask.png";
                    using (Bitmap template = new Bitmap(maskPath))
                    {
                        var pi = new PrepairImages();
                        var reelImage = pi.MaskImage(bitmap, template);
                        bi = reelImage.ToBitmapImage();
                    }
                }
                else
                {
                    bi = bitmap.ToBitmapImage();
                }

                if (ReelSnapshotFileName != "")
                {
                    bitmap.Save(ReelSnapshotFileName);
                    ReelSnapshotFileName = "";
                }
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
            System.Threading.Thread.Sleep(7000);
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
    }
}
