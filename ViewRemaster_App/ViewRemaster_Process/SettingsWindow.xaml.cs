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
using ViewRemaster_Tools;

namespace ViewRemaster_Process
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
        }

        private void button_save_Click(object sender, RoutedEventArgs e)
        {
            if(Settings.Instance.RootPath != textBox_RootPath.Text && Directory.Exists(textBox_RootPath.Text))
            {
                Settings.Instance.RootPath = textBox_RootPath.Text;
                PathChanged = true;
            }
            else
            {
                MessageBox.Show("Path does not exist!", "Save Settings", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Settings.Serialize();
            Settings.Deserialize();

            MessageBox.Show("Settings saved. :-)", "Save Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
