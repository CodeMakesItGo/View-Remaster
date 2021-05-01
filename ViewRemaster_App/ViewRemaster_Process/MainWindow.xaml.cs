using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ViewRemaster_Tools;


namespace ViewRemaster_Process
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ProcessImages processImages = new ProcessImages();
        private bool forceUpdate = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            comboBox_slide.ItemsSource = processImages.ProcessOrder;
            slider_text_threshold.Value = processImages.Ocr_threshold;
            slider_mask_threshold.Value = processImages.Mask_threshold;

            processImages.SetImageEvent += ProcessImages_SetImageEvent;
            processImages.SetLabelStatusEvent += ProcessImages_SetLabelStatusEvent;
            processImages.SetTextboxEvent += ProcessImages_SetTextboxEvent;

            LoadDirectories();
        }

        private void LoadDirectories()
        {
            var dirs = Directory.EnumerateDirectories(processImages.rootPath);
            comboBox_path.Items.Clear();

            foreach (var dir in dirs)
            {
                if (dir.Contains("templates")) continue;
                comboBox_path.Items.Add(dir.Substring(dir.LastIndexOf('\\') + 1));
            }
            comboBox_path.SelectedIndex = -1;
        }

        private void Button_text_update_Click(object sender, RoutedEventArgs e)
        {
            processImages.ProcessImage();
        }

        private void Button_next_Click(object sender, RoutedEventArgs e)
        {
            if(forceUpdate)
            {
                //processImages.ProcessImage();
                forceUpdate = false;
            }

            if (comboBox_slide.SelectedIndex < comboBox_slide.Items.Count - 1)
                comboBox_slide.SelectedIndex++;
            else
            {
                //Rename Directory to indicate we completed this one
                if(processImages.Path.Contains("_Done") == false)
                {
                    var newDir = processImages.Path + "_Done";
                    try
                    {
                        Directory.Move(processImages.Path, newDir);
                    }
                    catch
                    {

                    }
                    processImages.Path = "";
                    LoadDirectories();
                }

                image_cropped.Source = null;
                image_final.Source = null;
                image_mask.Source = null;
                image_reel.Source = null;
                image_rotated.Source = null;
                image_slide.Source = null;
                image_text.Source = null;
                image_threshold.Source = null;
                textBox_ocr.Text = "";
                textBox_angle.Text = "";
            }
        }

        private void ComboBox_path_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var dir = comboBox_path.SelectedItem;
            if (dir != null)
            {
                processImages.UpdatePath((string)dir);
                comboBox_slide.SelectedIndex = 0;
            }
        }

        private void ComboBox_slide_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            processImages.CurrentSlide = comboBox_slide.SelectedIndex;
            processImages.ProcessImage();
        }

        private void Slider_text_threshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            processImages.Ocr_threshold = (int)slider_text_threshold.Value;
            label_slide_text.Content = $"Slide Text {processImages.Ocr_threshold}";
            forceUpdate = true;
        }

        private void Slider_mask_threshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            processImages.Mask_threshold = (int)slider_mask_threshold.Value;
            label_mask_threshold.Content = $"Mash Threshold {processImages.Mask_threshold}";
            forceUpdate = true;
        }

        private void TextBox_ocr_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (textBox_ocr.Text == "")
                return;

            processImages.UpdateOcrText(textBox_ocr.Text);
            forceUpdate = true;
        }

        private void TextBox_angle_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(double.TryParse(textBox_angle.Text, out var angle))
            {
                processImages.Rotation_angle = angle;
                forceUpdate = true;
            }
        }

        private void Image_reel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenInPaint((string)image_reel.Tag);
            forceUpdate = true;
        }

        private void Image_mask_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenInPaint((string)image_mask.Tag);
            forceUpdate = true;
        }

        private void OpenInPaint(string filePath)
        {
            if (filePath == "") return;
            ProcessStartInfo Info = new ProcessStartInfo()
            {
                FileName = "mspaint.exe",
                WindowStyle = ProcessWindowStyle.Maximized,
                Arguments = "\"" + filePath + "\""
            };
            Process.Start(Info);
        }

        private void ProcessImages_SetTextboxEvent(ProcessImages.TextType textType, string text)
        {
            if (!Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(new System.Action(() => { ProcessImages_SetTextboxEvent(textType, text); }));
            }
            else
            {
                TextBox textBox = null;

                switch (textType)
                {
                    case ProcessImages.TextType.ANGLE: textBox = textBox_angle; break;
                    case ProcessImages.TextType.OCR: textBox = textBox_ocr; break;
                    case ProcessImages.TextType.POPUP: MessageBox.Show(text); break;

                    default: return;
                }

                if (textBox != null)
                    textBox.Text = text;
            }
        }

        private void ProcessImages_SetLabelStatusEvent(string text)
        {
            if (!Dispatcher.CheckAccess())
            {
                label_status.Dispatcher.Invoke(new System.Action(() => { ProcessImages_SetLabelStatusEvent(text); }) );
            }
            else
            {
                label_status.Content = text;
            }
        }

        private void ProcessImages_SetImageEvent(ProcessImages.ImageType imageType, string path)
        {
            if (!Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(new System.Action(() => { ProcessImages_SetImageEvent(imageType, path); }));
            }
            else
            {
                var bitmap = new BitmapImage();
                using (var stream = File.OpenRead(path))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    stream.Close();
                    stream.Dispose();
                }

                System.Windows.Controls.Image imageCtrl = null;

                switch (imageType)
                {
                    case ProcessImages.ImageType.CROPPED: imageCtrl = image_cropped; break;
                    case ProcessImages.ImageType.FINAL: imageCtrl = image_final; break;
                    case ProcessImages.ImageType.MASK: imageCtrl = image_mask; break;
                    case ProcessImages.ImageType.REEL: imageCtrl = image_reel; break;
                    case ProcessImages.ImageType.ROTATED: imageCtrl = image_rotated; break;
                    case ProcessImages.ImageType.SLIDE: imageCtrl = image_slide; break;
                    case ProcessImages.ImageType.TEXT: imageCtrl = image_text; break;
                    case ProcessImages.ImageType.THREASHOLD: imageCtrl = image_threshold; break;
                    default: return;
                }

                imageCtrl.Source = bitmap;
                imageCtrl.Tag = path;
            }
        }

     
    }
}
