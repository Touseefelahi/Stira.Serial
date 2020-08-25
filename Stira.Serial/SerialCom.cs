using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;

namespace Stira.Serial
{
    /// <summary>
    /// Simple Serial communication class with some additional functionality like DataReady on
    /// behalf of PacketHeader and Endbyte indetifier
    /// </summary>
    public class SerialCom
    {
        private readonly List<byte> rxBuffer;
        private SerialPort serialPort;
        private bool isStartPacketFound;

        private int packetSize;

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
                19200, 38400, 57600, 115200, 128000, 256000 };
        }

        /// <summary>
        /// If not null, this array will be used to identify the Rx Packet starting message
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
        /// Payload index in Rx Packet to determine packet end. If its 0 Software will use <see
        /// cref="BytesThresholdForRxPush"/> as length
        /// </summary>
        public int PayloadIndex { get; set; }

        /// <summary>
        /// No of bytes to read for Payload e.g. IF protocol have 2 bytes for payload then set this
        /// to true.
        /// </summary>
        public bool TwoBytesForPayload { get; set; }

        /// <summary>
        /// Payload offset: if protocol payload excludes some bytes from the reply. Include them
        /// here. Some protocols doesn't include Header/CRC/Endbyte etc
        /// <para>e.g. If reply protocol excludes 3 bytes then set this to 3</para>
        /// </summary>
        public int PayloadOffset { get; set; }

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

        /// <summary>
        /// This event will be called when data is ready
        /// </summary>
        public EventHandler<byte[]> DataReady { get; set; }

        /// <summary>
        /// Whenever there's data on Rx, it will wait for this much bytes then fire the DataReadyEvent
        /// </summary>
        public int BytesThresholdForRxPush { get; set; }

        /// <summary>
        /// If for any reason we need to change any parameter you can change it using SerialPort object
        /// </summary>
        /// <returns></returns>
        public SerialPort GetSerialPort()
        {
            return serialPort;
        }

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

        private byte[] RemoveAt(byte[] array, int startIndex, int length)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (length < 0)
            {
                startIndex += 1 + length;
                length = -length;
            }

            if (startIndex < 0)
                throw new ArgumentOutOfRangeException("startIndex");
            if (startIndex + length > array.Length)
                throw new ArgumentOutOfRangeException("length");

            byte[] newArray = new byte[array.Length - length];

            Array.Copy(array, 0, newArray, 0, startIndex);
            Array.Copy(array, startIndex + length, newArray, startIndex, array.Length - startIndex - length);

            return newArray;
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
                else if (StartByteIdentifier != null)
                {
                    do
                    {
                        if (PayloadIndex == 0) packetSize = BytesThresholdForRxPush;
                        if (isStartPacketFound)
                        {
                            rxBuffer.AddRange(tempBuffer); //Mergin remaining bytes
                            if (rxBuffer.Count >= packetSize)
                            {
                                DataReady?.Invoke(null, rxBuffer.ToArray());
                                rxBuffer.Clear();
                                isStartPacketFound = false;
                            }
                        }
                        int foundAt = SearchPattern(tempBuffer, StartByteIdentifier);
                        if (foundAt != -1)
                        {
                            if (PayloadIndex != 0 && tempBuffer.Length >= foundAt + PayloadIndex + 2)
                            {
                                if (TwoBytesForPayload)
                                    packetSize = BitConverter.ToUInt16(tempBuffer, foundAt + PayloadIndex);
                                else packetSize = tempBuffer[PayloadIndex];
                            }
                            if (tempBuffer.Length >= foundAt + packetSize)
                            {
                                rxBuffer.AddRange(tempBuffer.Skip(foundAt).Take(packetSize));
                                DataReady?.Invoke(null, rxBuffer.ToArray());
                                rxBuffer.Clear();
                                tempBuffer = RemoveAt(tempBuffer, 0, foundAt + packetSize);
                                isStartPacketFound = false;
                            }
                            else
                            {
                                isStartPacketFound = true;
                                rxBuffer.AddRange(tempBuffer.Skip(foundAt).Take(tempBuffer.Length - foundAt));
                            }
                        }
                    } while (rxBuffer.Count >= packetSize);
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