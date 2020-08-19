﻿## How to use
      private readonly SerialCom serialCom;

        /// <summary>
        /// Initializes the baudrate list and serial port
        /// </summary>
        public ClassThatWantToUseSerial()
        {
            serialCom = new SerialCom();
            serialCom.SetStartPacketID(new byte[] { 0xFF, 0x02 });
            serialCom.SetEndPacketID(new byte[] { 0x03 });
            serialCom.BytesThresholdForRxPush = 15; //This can be min packet size
            serialCom.DataReady += InclinoDataReady;
            serialCom.Open("COM1", 9600);
            var lastError = serialCom.LastError;//Check last error for any errors
        }

        private void InclinoDataReady(object sender, byte[] tempBuffer)
        {
           //Data starting from 0xFF 0x22 and ending on 0x03
        }

        public bool Write(byte[] value)
        {
            return serialCom.Write(value);
        }