using Stira.WpfCore;
using System.Windows.Input;

namespace Stira.Serial.Wpf
{
    public class SerialControlViewModel : BaseNotifyPropertyChanged
    {
        private bool isPortSelectionOpen;
        private string toggleButtonText = "Open";

        public SerialControlViewModel()
        {
            SerialCom = new SerialCom();
            ToggleSerialPortCommand = new DelegateCommand(ToggleSerialPort);
        }

        public bool IsPortSelectionOpen
        {
            get { return isPortSelectionOpen; }
            set
            {
                if (isPortSelectionOpen != value)
                {
                    isPortSelectionOpen = value;
                    if (value)
                    {
                        OnPropertyChanged(nameof(SerialCom));
                    }
                    OnPropertyChanged(nameof(IsPortSelectionOpen));
                }
            }
        }

        public string ToggleButtonText
        {
            get { return toggleButtonText; }
            set
            {
                if (toggleButtonText != value)
                {
                    toggleButtonText = value;
                    OnPropertyChanged(nameof(ToggleButtonText));
                }
            }
        }

        public ISerialCom SerialCom { get; }

        public ICommand ToggleSerialPortCommand { get; }

        private void ToggleSerialPort()
        {
            try
            {
                if (SerialCom.IsPortOpen)
                {
                    SerialCom.Close();
                }
                else
                {
                    SerialCom.Open();
                }
                ToggleButtonText = SerialCom.IsPortOpen ? "Close" : "Open";
                OnPropertyChanged(nameof(SerialCom));
            }
            catch (System.Exception)
            {
            }
        }
    }
}