using System.Diagnostics;
using System.Windows;

namespace LapseOTron
{
    /// <summary>
    /// Interaction logic for CodecWarningWindow.xaml
    /// </summary>
    public partial class CodecWarningWindow : Window
    {
        public CodecWarningWindow()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri){ UseShellExecute = true });
            e.Handled = true;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
