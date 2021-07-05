using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ViewRemaster_Tools
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public bool PathChanged { get; set; } = false;
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            textBox_RootPath.Text = Settings.Instance.RootPath;
            spinner_reel_x.Text = Settings.Instance.ReelCrop_X.ToString();
            spinner_reel_y.Text = Settings.Instance.ReelCrop_Y.ToString();
            spinner_reel_width.Text = Settings.Instance.ReelCrop_Width.ToString();
            spinner_reel_center.Text = Settings.Instance.ReelCropCenter_Width.ToString();

            spinner_slide_x.Text = Settings.Instance.SlideCrop_X.ToString();
            spinner_slide_y.Text = Settings.Instance.SlideCrop_Y.ToString();
            spinner_slide_width.Text = Settings.Instance.SlideCrop_Width.ToString();
            spinner_slide_height.Text = Settings.Instance.SlideCrop_Height.ToString();
            spinner_slide_corner.Text = Settings.Instance.SlideCrop_Corner.ToString();
            spinner_slide_gap.Text = Settings.Instance.Slide_Gap.ToString();
            spinner_slide_top.Text = Settings.Instance.Slide_Top.ToString();

            spinner_text_x.Text = Settings.Instance.TextCrop_X.ToString();
            spinner_text_y.Text = Settings.Instance.TextCrop_Y.ToString();
            spinner_text_width.Text = Settings.Instance.TextCrop_Width.ToString();
            spinner_text_height.Text = Settings.Instance.TextCrop_Height.ToString();
            spinner_text_top.Text = Settings.Instance.SlideText_Top.ToString();
        }

        private void button_save_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.Instance.RootPath.Equals(textBox_RootPath.Text) == false)
            {
                if (Directory.Exists(textBox_RootPath.Text))
                {
                    Settings.Instance.RootPath = textBox_RootPath.Text;
                    PathChanged = true;
                }
                else
                {
                    MessageBox.Show("Path does not exist!", "Save Settings", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            Settings.Serialize();
            Settings.Deserialize();

            MessageBox.Show("Settings saved. :-)", "Save Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void spinner_reel_x_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Xceed.Wpf.Toolkit.IntegerUpDown iud = (Xceed.Wpf.Toolkit.IntegerUpDown)sender;

            if (int.TryParse(iud.Text, out var v))
                Settings.Instance.ReelCrop_X = v;
        }

        private void spinner_reel_y_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Xceed.Wpf.Toolkit.IntegerUpDown iud = (Xceed.Wpf.Toolkit.IntegerUpDown)sender;

            if (int.TryParse(iud.Text, out var v))
                Settings.Instance.ReelCrop_Y = v;
        }

        private void spinner_reel_width_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Xceed.Wpf.Toolkit.IntegerUpDown iud = (Xceed.Wpf.Toolkit.IntegerUpDown)sender;

            if (int.TryParse(iud.Text, out var v))
                Settings.Instance.ReelCrop_Width = v;
        }

        private void spinner_reel_center_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Xceed.Wpf.Toolkit.IntegerUpDown iud = (Xceed.Wpf.Toolkit.IntegerUpDown)sender;

            if (int.TryParse(iud.Text, out var v))
                Settings.Instance.ReelCropCenter_Width = v;
        }

        private void spinner_text_x_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Xceed.Wpf.Toolkit.IntegerUpDown iud = (Xceed.Wpf.Toolkit.IntegerUpDown)sender;

            if (int.TryParse(iud.Text, out var v))
                Settings.Instance.TextCrop_X = v;
        }

        private void spinner_text_y_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Xceed.Wpf.Toolkit.IntegerUpDown iud = (Xceed.Wpf.Toolkit.IntegerUpDown)sender;

            if (int.TryParse(iud.Text, out var v))
                Settings.Instance.TextCrop_Y = v;
        }

        private void spinner_text_width_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Xceed.Wpf.Toolkit.IntegerUpDown iud = (Xceed.Wpf.Toolkit.IntegerUpDown)sender;

            if (int.TryParse(iud.Text, out var v))
                Settings.Instance.TextCrop_Width = v;
        }

        private void spinner_text_height_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Xceed.Wpf.Toolkit.IntegerUpDown iud = (Xceed.Wpf.Toolkit.IntegerUpDown)sender;

            if (int.TryParse(iud.Text, out var v))
                Settings.Instance.TextCrop_Height = v;
        }

        private void spinner_text_top_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Xceed.Wpf.Toolkit.IntegerUpDown iud = (Xceed.Wpf.Toolkit.IntegerUpDown)sender;

            if (int.TryParse(iud.Text, out var v))
                Settings.Instance.SlideText_Top = v;
        }

        private void spinner_slide_x_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Xceed.Wpf.Toolkit.IntegerUpDown iud = (Xceed.Wpf.Toolkit.IntegerUpDown)sender;

            if (int.TryParse(iud.Text, out var v))
                Settings.Instance.SlideCrop_X = v;
        }

        private void spinner_slide_y_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Xceed.Wpf.Toolkit.IntegerUpDown iud = (Xceed.Wpf.Toolkit.IntegerUpDown)sender;

            if (int.TryParse(iud.Text, out var v))
                Settings.Instance.SlideCrop_Y = v;
        }

        private void spinner_slide_width_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Xceed.Wpf.Toolkit.IntegerUpDown iud = (Xceed.Wpf.Toolkit.IntegerUpDown)sender;

            if (int.TryParse(iud.Text, out var v))
                Settings.Instance.SlideCrop_Width = v;
        }

        private void spinner_slide_height_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Xceed.Wpf.Toolkit.IntegerUpDown iud = (Xceed.Wpf.Toolkit.IntegerUpDown)sender;

            if (int.TryParse(iud.Text, out var v))
                Settings.Instance.SlideCrop_Height = v;
        }

        private void spinner_slide_corner_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Xceed.Wpf.Toolkit.IntegerUpDown iud = (Xceed.Wpf.Toolkit.IntegerUpDown)sender;

            if (int.TryParse(iud.Text, out var v))
                Settings.Instance.SlideCrop_Corner = v;
        }

        private void spinner_slide_gap_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Xceed.Wpf.Toolkit.IntegerUpDown iud = (Xceed.Wpf.Toolkit.IntegerUpDown)sender;

            if (int.TryParse(iud.Text, out var v))
                Settings.Instance.Slide_Gap = v;
        }

        private void spinner_slide_top_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Xceed.Wpf.Toolkit.IntegerUpDown iud = (Xceed.Wpf.Toolkit.IntegerUpDown)sender;

            if (int.TryParse(iud.Text, out var v))
                Settings.Instance.Slide_Top = v;
        }
    }
}
