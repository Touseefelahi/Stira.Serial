using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace Stira.Serial
{
    /// <summary>
    /// Simple Serial communication class with some additional functionality like DataReady on
    /// behalf of PacketHeader and Endbyte identifier
    /// </summary>
    public interface ISerialCom
    {
        /// <summary>
        /// If not null, this array will be used to identify the Rx Packet starting message
        /// </summary>
        public List<byte> StartByteIdentifier { get; }

        /// <summary>
        /// If not null, this array will be used to identify the Rx packet message completion
        /// <para>
        /// Note: StartByteIdentifier and EndByteIdentifier both must be set if you want to identify
        /// packet by Start/End
        /// </para>
        /// </summary>
        public List<byte> EndByteIdentifier { get; }

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
        public bool IsPortOpen { get; }

        /// <summary>
        /// List of available serial ports open
        /// </summary>
        public List<string> ListOfAvailablePorts => new(SerialPort.GetPortNames());

        /// <summary>
        /// List of baud rates available
        /// </summary>
        public List<int> ListOfBaudRates { get; }

        /// <summary>
        /// Selected baud rate
        /// </summary>
        public int BaudRate { get; set; }

        /// <summary>
        /// Serial Port name
        /// </summary>
        public string PortName { get; set; }

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
        public SerialPort GetSerialPort();

        /// <summary>
        /// Writes the byte to serial port if open
        /// </summary>
        /// <param name="bytesToWrite">bytes to write</param>
        /// <returns></returns>
        public bool Write(byte[] bytesToWrite);

        /// <summary>
        /// Tries to open the serial port
        /// </summary>
        /// <returns>Current Port status</returns>
        public bool Open();

        /// <summary>
        /// Sets the Port and Baud rate then opens the port
        /// </summary>
        /// <param name="portName"></param>
        /// <param name="baudRate"></param>
        /// <returns>Port open state</returns>
        public bool Open(string portName, int baudRate);

        /// <summary>
        /// Closes the port if open
        /// </summary>
        /// <returns>Current Port Status</returns>
        public bool Close();

        /// <summary>
        /// Sets StartByteIdentifier
        /// </summary>
        /// <param name="startByteId"></param>
        public void SetStartPacketID(byte[] startByteId);

        /// <summary>
        /// Sets EndByteIdentifier
        /// </summary>
        /// <param name="endByteId"></param>
        public void SetEndPacketID(byte[] endByteId);
    }
}