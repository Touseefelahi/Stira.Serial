using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace Stira.Serial
{
    public class SerialCom
    {
        private readonly List<byte> rxBuffer;
        private SerialPort serialPort;
        private bool isStartPacketFound;

        /// <summary>
        /// Initializes the baudrate list and serial port
        /// </summary>
        public SerialCom()
        {
            serialPort = new SerialPort
            {
                ReadBufferSize = 30_000
            };
            rxBuffer = new List<byte>();
            ListOfBaudRates = new List<int> { 110, 300, 600, 1200, 2400, 4800, 9600, 14400,
                19200, 38400, 57600, 115200, 230400, 128000, 256000 };
        }

        /// <summary>
        /// If not null, this array will be used to identify the Rx Packet starting message
        /// <para>
        /// Note: StartByteIdentifier and EndByteIdentifier both must be set if you want to identify
        /// packet by Start/End
        /// </para>
        /// </summary>
        public List<byte> StartByteIdentifier { get; private set; }

        /// <summary>
        /// If not null, this array will be used to identify the Rx packet message completion
        /// <para>
        /// Note: StartByteIdentifier and EndByteIdentifier both must be set if you want to identify
        /// packet by Start/End
        /// </para>
        /// </summary>
        public List<byte> EndByteIdentifier { get; private set; }

        /// <summary>
        /// It logs last error
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        /// Current
        /// </summary>
        public bool IsPortOpen => serialPort.IsOpen;

        /// <summary>
        /// List of availabe serial ports open
        /// </summary>
        public List<string> ListOfAvailablePorts => new List<string>(SerialPort.GetPortNames());

        /// <summary>
        /// List of baud rates available
        /// </summary>
        public List<int> ListOfBaudRates { get; }

        /// <summary>
        /// Selected baud rate
        /// </summary>
        public int BaudRate { get; set; } = 9600;

        /// <summary>
        /// Serial Port name
        /// </summary>
        public string PortName { get; set; } = "COM1";

        public EventHandler<byte[]> DataReady { get; set; }

        /// <summary>
        /// Whenever there's data on Rx, it will wait for this much bytes then fire the DataReadyEvent
        /// </summary>
        public int BytesThresholdForRxPush { get; set; }

        /// <summary>
        /// Writes the byte to serial port if open
        /// </summary>
        /// <param name="bytesToWrite">bytes to write</param>
        /// <returns></returns>
        public bool Write(byte[] bytesToWrite)
        {
            try
            {
                if (IsPortOpen)
                {
                    serialPort.Write(bytesToWrite, 0, bytesToWrite.Length);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Tries to open the serial port
        /// </summary>
        /// <returns>Current Port status</returns>
        public bool Open()
        {
            try
            {
                serialPort = new SerialPort
                {
                    ReadTimeout = 500,
                    WriteTimeout = 500
                };
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                }

                serialPort.BaudRate = BaudRate;
                serialPort.PortName = PortName;
                serialPort.Open();
                serialPort.DataReceived += SerialPort_DataReceived;
                return IsPortOpen;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return IsPortOpen;
            }
        }

        /// <summary>
        /// Sets the Port and Baudrate then opens the port
        /// </summary>
        /// <param name="portName"></param>
        /// <param name="baudRate"></param>
        /// <returns>Port open state</returns>
        public bool Open(string portName, int baudRate)
        {
            BaudRate = baudRate;
            PortName = portName;
            return Open();
        }

        /// <summary>
        /// Closes the port if open
        /// </summary>
        /// <returns>Current Port Status</returns>
        public bool Close()
        {
            if (serialPort.IsOpen)
            {
                serialPort.DataReceived -= SerialPort_DataReceived;
                serialPort.Close();
            }
            return IsPortOpen;
        }

        /// <summary>
        /// Sets StartByteIdentifier
        /// </summary>
        /// <param name="startByteId"></param>
        public void SetStartPacketID(byte[] startByteId)
        {
            StartByteIdentifier = new List<byte>(startByteId);
            if (BytesThresholdForRxPush == 0)
            {
                BytesThresholdForRxPush = startByteId.Length;
            }
        }

        /// <summary>
        /// Sets EndByteIdentifier
        /// </summary>
        /// <param name="endByteId"></param>
        public void SetEndPacketID(byte[] endByteId)
        {
            EndByteIdentifier = new List<byte>(endByteId);
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int availableBytes = serialPort.BytesToRead;
            if (availableBytes >= BytesThresholdForRxPush)
            {
                byte[] tempBuffer = new byte[availableBytes];
                serialPort.Read(tempBuffer, 0, availableBytes);
                if (StartByteIdentifier != null && EndByteIdentifier != null)
                {
                    int foundAt;
                    if (!isStartPacketFound)
                    {
                        foundAt = SearchPattern(tempBuffer, StartByteIdentifier);
                        if (foundAt != -1)
                        {
                            isStartPacketFound = true;
                            rxBuffer.AddRange(tempBuffer);
                        }
                    }

                    if (isStartPacketFound)
                    {
                        foundAt = SearchPattern(tempBuffer, EndByteIdentifier);
                        rxBuffer.AddRange(tempBuffer);
                        if (foundAt != -1)
                        {
                            isStartPacketFound = false;
                            DataReady?.Invoke(null, rxBuffer.ToArray());
                            rxBuffer.Clear();
                        }
                    }
                }
                else
                {
                    DataReady?.Invoke(null, tempBuffer);
                }
            }
        }

        private int SearchPattern(byte[] completePacket, List<byte> header)
        {
            int len = header.Count;
            int limit = completePacket.Length - len;
            for (int i = 0; i <= limit; i++)
            {
                int k = 0;
                for (; k < len; k++)
                {
                    if (header[k] != completePacket[i + k])
                    {
                        break;
                    }
                }
                if (k == len)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}