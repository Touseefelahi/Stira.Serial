using System.Windows.Controls;

namespace Stira.Serial.Wpf
{
    /// <summary>
    /// Interaction logic for SerialControl.xaml
    /// </summary>
    public partial class SerialControl : UserControl
    {
        public SerialControl()
        {
            InitializeComponent();
            DataContext = new SerialControlViewModel();
        }
    }
}