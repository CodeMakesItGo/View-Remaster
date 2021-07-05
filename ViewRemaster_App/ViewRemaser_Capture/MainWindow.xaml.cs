using System;
using System.Drawing;
using System.IO.Ports;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ViewRemaster_Tools;

namespace ViewRemaser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly CaptureImages captureImages = new CaptureImages();
        private bool align_reel = false;

        private Camera SlideCamera = null;
        private Camera ReelCamera = null;
        private int Exposure { get; set; } = -6;
 

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cp.ShowAvailableColors = false;
            cp.ShowDropDownButton = false;
            cp.SelectedColor = Colors.White;

            comboBox_comm.Items.Clear();
            foreach (string s in SerialPort.GetPortNames())
            {
                comboBox_comm.Items.Add($"{s}");
            }

            //default first index
            comboBox_comm.SelectedIndex = 0;

            captureImages.CommWaitingEvent += CaptureImages_CommWaitingEvent;
            captureImages.UpdateStatusEvent += CaptureImages_UpdateStatusEvent;
            captureImages.RecievedSerialDataEvent += CaptureImages_RecievedSerialDataEvent;
            SlideCamera = new Camera(Slide_NewFrame);
            ReelCamera = new Camera(Reel_NewFrame);        
        }

        private void CaptureImages_RecievedSerialDataEvent(string data)
        {
            if (data.Contains("DONE") == false)
            {
                label_comm.Dispatcher.Invoke(new Action(() => label_comm.Content = "RX: " + data));
            }
        }

        private void CaptureImages_UpdateStatusEvent(string text)
        {
            SetLabelStatus(text);
        }

        private void CaptureImages_CommWaitingEvent(bool waiting)
        {
            SetDot(waiting);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            captureImages.StopCapture();
            captureImages.CloseSerial();
            SlideCamera.StopCamera();
            ReelCamera.StopCamera();
        }

        private void Slide_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            try
            {
                BitmapImage bi = captureImages.UpdateSlideImage((Bitmap)eventArgs.Frame.Clone());
                Dispatcher.Invoke(new Action(() => videoPlayer.Source = bi));
                SlideCamera.SetExposure(Exposure);
            }
            catch (Exception exc)
            {
                MessageBox.Show("Error on _videoSource_NewFrame:\n" + exc.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SlideCamera.StopCamera();
            }
        }

         private void Reel_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            try
            {
                BitmapImage bi = captureImages.UpdateReelImage(eventArgs.Frame, align_reel);
                Dispatcher.BeginInvoke(new Action(() => ReelPlayer.Source = bi));
            }
            catch (Exception exc)
            {
                MessageBox.Show("Error on _videoSource_NewFrame:\n" + exc.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ReelCamera.StopCamera();
            }
        }

        private void Button_Connect_Click(object sender, RoutedEventArgs e)
        {
            if (captureImages.CommOpen == false)
            {
                captureImages.ConnectSerial(comboBox_comm.SelectedItem.ToString());

                button_comm.Content = "Disconnect";
                label_connect.Content = "Connected";
                comboBox_comm.IsEnabled = false;

                captureImages.SetBrightness((byte)slider_brightness.Value);
                captureImages.SetColor(cp.SelectedColor.Value.R, cp.SelectedColor.Value.G, cp.SelectedColor.Value.B);
            }
            else
            {
                captureImages.CloseSerial();

                button_comm.Content = "Connect";
                label_connect.Content = "Disconnected";
                comboBox_comm.IsEnabled = true;
            }
        }

        private void Button_slidecamera_Click(object sender, RoutedEventArgs e)
        {
            if (SlideCamera.SelectCamera())
            {
                SlideCamera.GetExposureRange(out var min, out var max, out var defaultvalue);
                slider_exposure.Minimum = min;
                slider_exposure.Maximum = max;
                slider_exposure.Value = defaultvalue;
            }
        }

        private void Button_reelcamera_Click(object sender, RoutedEventArgs e)
        {
            ReelCamera.SelectCamera();
        }

        private void Button_start_Click(object sender, RoutedEventArgs e)
        {
            if(textBox_ReelName.Text == "")
            {
                MessageBox.Show("Set Reel Name.");
            }
            else
                captureImages.CaptureAllImages(textBox_ReelName.Text);
        }

        private void Button_stop_capture_Click(object sender, RoutedEventArgs e)
        {
            captureImages.StopCapture();
        }

        private void Button_next_Click(object sender, RoutedEventArgs e)
        {
            captureImages.NextSlide();
        }

        private void Button_align_Click(object sender, RoutedEventArgs e)
        {
            captureImages.AlignReel();
        }

        private void Button_stop_Click(object sender, RoutedEventArgs e)
        {
            captureImages.StopReel();
        }

        private void Cp_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<System.Windows.Media.Color?> e)
        {
            captureImages.SetColor(cp.SelectedColor.Value.R, cp.SelectedColor.Value.G, cp.SelectedColor.Value.B);
        }

        private void Slider_brightness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            captureImages.SetBrightness((byte)slider_brightness.Value);
        }

        private void Slider_exposure_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Exposure = (int)slider_exposure.Value;
            label_exposure.Content = $"Exposure = {Exposure}";
        }


        delegate void SetDotDelegate(bool red = false);
        private void SetDot(bool red = false)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new SetDotDelegate(SetDot), red);
                return;
            }

            if (!red)
            {
                dot.Fill = System.Windows.Media.Brushes.Green;
                label_status.Content = "Ready";
            }
            else
            {
                dot.Fill = System.Windows.Media.Brushes.Red;
                label_status.Content = "Working";
            }
        }

        private delegate void SetLabelStatusDelegate(string text);
        private void SetLabelStatus(string text)
        {
            if (label_run_status.Dispatcher.CheckAccess())
            {
                label_run_status.Content = text;
            }
            else
            {
                label_run_status.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,
                    new SetLabelStatusDelegate(SetLabelStatus), text);
            }
        }

        private void CheckBox_align_Click(object sender, RoutedEventArgs e)
        {
            align_reel = checkBox_align.IsChecked.Value;
        }

        private void CheckBox_spotlight_Click(object sender, RoutedEventArgs e)
        {
            captureImages.Spotlight(checkBox_spotlight.IsChecked.Value);
        }

        private void CheckBox_reel_lock_Click(object sender, RoutedEventArgs e)
        {
            if(checkBox_reel_lock.IsChecked.Value)
            {
                ReelCamera.LockCameraProperties();
            }
            else
            {
                ReelCamera.LockCameraProperties(false);
            }
        }

        private void CheckBox_slide_lock_Click(object sender, RoutedEventArgs e)
        {
            if (checkBox_slide_lock.IsChecked.Value)
            {
                SlideCamera.LockCameraProperties();
            }
            else
            {
                SlideCamera.LockCameraProperties(false);
            }
        }

        private void CheckBox_upsidedown_Click(object sender, RoutedEventArgs e)
        {
            if(checkBox_upsidedown.IsChecked.Value)
            {
                captureImages.UpsideDown = true;
            }
            else
            {
                captureImages.UpsideDown = false;
            }
        }

        private void slider_threshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(label_threshold != null)
                label_threshold.Content = $"Threshold {(int)slider_threshold.Value}";

            captureImages.Threshold = (int)slider_threshold.Value;
        }

        private void checkBox_threshold_Click(object sender, RoutedEventArgs e)
        {
            if (checkBox_threshold.IsChecked.HasValue)
            {
                captureImages.ThresholdEnabled = checkBox_threshold.IsChecked.Value;
            }
            else
            {
                captureImages.ThresholdEnabled = false;
            }
        }

        private void image_settings_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SettingsWindow sw = new ViewRemaster_Tools.SettingsWindow();
            sw.ShowDialog();

            if (sw.PathChanged)
            {

            }
        }
    }
}
