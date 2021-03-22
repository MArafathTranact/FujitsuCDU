using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FujitsuCDU
{
    class FujitsuCDUProcessor
    {
        readonly Logger logger = new Logger();
        Utilities utilities = new Utilities();
        CRC cRC = new CRC();
        SerialPort commPort;// = new SerialPort("COM7", 9600, Parity.Even, 8, StopBits.One);
        StringBuilder sbreadyMessage = new StringBuilder();

        #region Constant
        const string STX = "02";
        const string ETX = "03";
        const string ENQ = "05";
        const string ACK = "06";
        const string NAK = "15";
        const string DLE = "10";
        const string ReqStatus = "01";
        const string ReqInit = "602";
        const string ReqBillCount1 = "603";
        const string ReqBillCount2 = "604";
        const string ReqBillTransport = "685";
        const string ReqBillRetrieval = "606";
        const string ReqReadSensorLevel = "6026";
        string[] canRequest = { "30", "B1", "B2", "33", "B4", "35", "36", "B7", "B8", "39" };

        // Good Response DH0,DH1 (DH2=$FF)
        const string StatusResp = "e01";
        const string GoodInitResp = "e02";
        const string GoodBillCountResp1 = "e03";
        const string GoodBillCountResp2 = "e04";
        const string BillsHalfway = "e85";
        const string GoodBillTransport = "e06";
        const string GoodBillRetrieval = "e07";
        const string GoodReadSensorLevel = "e026";

        // Negative Response Frame (DH2=$FF)
        const string AbnormalInit = "f002";
        const string AbnormalBillCount1 = "f003";
        const string AbnormalBillCount2 = "f004";
        const string AbnormalBillTransport = "f005";
        const string AbnormalBillRetrieval = "f006";

        #endregion

        public enum TCommState { csEnqSent, csMsgSent, csReady, csEnqReceived, csMsgReceived, csWaitEnq };
        public enum TState { stWaitTranReply, stWaitConfig, stWaitReject, stWaitRejectInit, stWaitPresenter, stReady, stWaitReset, stDisconnected, stDown, stWaitReadCassettes };
        public enum TDevState { stWaitTranReply, stWaitConfig, stWaitReject, stWaitRejectInit, stWaitPresenter, stReady, stWaitReset, stDisconnected, stDown, stWaitReadCassettes };

        public TState State;

        public TCommState CommState;
        public string receivedMessage = string.Empty;
        string MsgWaiting = string.Empty;
        public bool IsDispenseReqSent = false;
        public bool IsProcessCompleted = false;
        public bool CanRetry = false;
        public FujitsuCDUProcessor()
        {
            try
            {
                var comPort = GetFileLocation("ComPort");
                var baudRate = int.Parse(GetFileLocation("BaudRate"));
                var dataBit = int.Parse(GetFileLocation("DataBit"));
                var parity = GetFileLocation("Parity").Equals("Even") ? Parity.Even : Parity.None;
                var stopBits = GetFileLocation("StopBits").Equals("One") ? StopBits.One : StopBits.None;
                commPort = new SerialPort(comPort, baudRate, parity, dataBit, stopBits);
                commPort.ReceivedBytesThreshold = 1;
                commPort.Encoding = Encoding.ASCII;
                commPort.DataReceived += new SerialDataReceivedEventHandler(OnDataReceived);

                OpenPort();
            }
            catch (Exception ex)
            {
                TraceMessage($"Exception at FujitsuCDU.Cons {ex.Message}");
            }

        }

        public void SendMessage(string message)
        {
            try
            {
                TraceMessage("Entered SendStr");
                if (!commPort.IsOpen)
                {
                    TraceMessage("Opening Comm Port.");
                    commPort.Open();
                    Thread.Sleep(200);
                    CommState = TCommState.csReady;
                    Thread.Sleep(500);
                }

                if (CommState == TCommState.csReady)
                {
                    CommState = TCommState.csEnqSent;
                    MsgWaiting = message;
                    IsProcessCompleted = false;
                    MyWriteStr(DLE + ENQ); //0x1005
                }

            }
            catch (Exception ex)
            {
                TraceMessage($"Exception at SendMessage : {ex.Message}");
            }

        }

        private void MyWriteStr(string AnsiString)
        {
            try
            {
                var sendValue = utilities.GetSendBytes(AnsiString);
                if (!commPort.IsOpen)
                {
                    commPort.Open();
                }
                commPort.Write(sendValue, 0, sendValue.Length);

                TraceMessage($"Sent on port: ");
                utilities.SplitBytes(sendValue, 16); // Write detailed information into Log file.
                Thread.Sleep(200);

                //Trace..
            }
            catch (Exception ex)
            {
                TraceMessage($"Exception at MyWriteStr :{ex.Message}");
            }

        }

        private void DispenseAmount(int amount, int originalAmount)
        {
            try
            {
                TraceMessage("Entered DispenseAmount");

                CanRetry = false;
                // Sample message:
                // ------------------ODR-------- N1--- N2--- N3--- N4--- R1--- R2--- R3--- R4--- P1 P2 P3 P4 N5--- N6--- N7--- N8--- R5--- R6--- R7--- R8--- P5 P6 P7 P8 FS
                // 60 03 ff 00 00 2c fe dc ba 98 30 b1 30 30 30 30 30 30 b1 30 30 30 30 30 30 30 15 00 00 00 30 30 30 30 30 30 30 30 30 30 30 30 30 30 30 30 00 00 00 00 1c

                //var msg = string.Empty;
                StringBuilder sb = new StringBuilder();

                sb.Append("6003ff00002cfedcba98");

                //msg = "$6003ff00002cfedcba98"; // This includes ODR.

                for (int i = 0; i < 4; i++)
                {
                    //Cannisters[i].HostCycleCount := Cannisters[i].HostCycleCount + Cannisters[i].Dispensed;
                    //{ inc( }
                    //CurTran.CannDisp[Cannisters[i].ID] := CurTran.CannDisp[Cannisters[i].ID] + (Cannisters[i].Dispensed);
                    sb.Append(canRequest[0]); //CChar[Integer(Cannisters[i].Dispensed div 10)];
                    sb.Append(canRequest[2]); //CChar[Integer(Cannisters[i].Dispensed mod 10)];
                }

                sb.Append("b130b130b130b13015151515");// R1,R2,R3,R4,P1,P2,P3,P4

                for (int i = 5; i < 7; i++)
                {
                    //Cannisters[i].HostCycleCount := Cannisters[i].HostCycleCount + Cannisters[i].Dispensed;
                    //{ inc( }
                    //CurTran.CannDisp[Cannisters[i].ID] := CurTran.CannDisp[Cannisters[i].ID] + (Cannisters[i].Dispensed);

                    sb.Append(canRequest[0]); //CChar[Integer(Cannisters[i].Dispensed div 10)];
                    sb.Append(canRequest[2]); //CChar[Integer(Cannisters[i].Dispensed mod 10)];
                }

                sb.Append("30303030b130b130b130b130151515151C"); // N7,N8,R5,R6,R7,R8,P5,P6,P7,P8
                                                                 //State:= stWaitTranReply;
                                                                 //SendMessage(sb.ToString());
                MsgWaiting = sb.ToString();

                MsgWaiting = "6002ff00001a0003483a483a483a483a0d0d0d0d483a483a000000000d0d00001c";


            }
            catch (Exception ex)
            {
                TraceMessage(ex.Message);
            }
        }

        public void InitCDU()
        {
            try
            {
                TraceMessage("Entered InitCDU");
                StringBuilder Msg = new StringBuilder();
                Msg.Append("6002FF00001A0000"); //DH0(60),DH1(02),DH2(FF), RSV1(00) , DH3(001A) ,0DR(0000)
                int L, i;
                byte T;


                for (int can = 0; can < 4; can++) // Length  1-4
                {
                    L = 18490;
                    var div = (L / 256).ToString("X").Length < 2 ? "0" + (L / 256).ToString("X") : (L / 256).ToString("X");
                    var mod = (L % 256).ToString("X").Length < 2 ? "0" + (L % 256).ToString("X") : (L % 256).ToString("X");
                    var LMsg = div + mod;//483a

                    Msg.Append(LMsg); // Default Cash Length. 48 => decimal =8  ; 3a => decimal =58
                }

                for (int can = 0; can < 4; can++) // Thickness 1-4
                {
                    var t = 13; //default thickness.
                    var TMsg = t.ToString("X").Length < 2 ? "0" + t.ToString("X") : t.ToString("X");
                    Msg.Append(TMsg); //Default Thickness = Decimal 13 , Hex 0D
                }
                for (int can = 4; can < 8; can++) // Length 5-8
                {
                    L = 18490;
                    var div = (L / 256).ToString("X").Length < 2 ? "0" + (L / 256).ToString("X") : (L / 256).ToString("X");
                    var mod = (L % 256).ToString("X").Length < 2 ? "0" + (L % 256).ToString("X") : (L % 256).ToString("X");
                    var LMsg = div + mod;//483a
                    Msg.Append(can > 5 ? "0000" : LMsg);
                }
                for (int can = 4; can < 8; can++) // Thickness 5-8
                {
                    var t = 13; //default thickness.
                    var TMsg = t.ToString("X").Length < 2 ? "0" + t.ToString("X") : t.ToString("X");// t.ToString("x");
                    Msg.Append(can > 5 ? "00" : TMsg);   //Default Thickness = Decimal 13 , Hex 0D
                }

                Msg.Append("1C");
                State = TState.stWaitReset;
                SendMessage(Msg.ToString());
                //SendMessage("6003ff00002cfedcba9830b1303030303030b1303030303030301500000030303030303030303030303030303030000000001c");
                //SendMessage("6001ff000001001c");
            }
            catch (Exception ex)
            {
                TraceMessage($"Exception at InitCDU :{ex.Message}");
            }

        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs args)
        {
            try
            {
                TraceMessage("Entered OnDataReceived");
                var serialPort = (SerialPort)sender;
                Thread.Sleep(500);

                // String reading..
                //var data = serialPort.ReadExisting();
                //receivedMessage = utilities.ByteToHexaCDC(ASCIIEncoding.ASCII.GetBytes(data), serialPort.BytesToRead);

                //Bytes reading starts ..
                int intBuffer;
                intBuffer = serialPort.BytesToRead;
                byte[] byteBuffer = new byte[intBuffer];
                serialPort.Read(byteBuffer, 0, intBuffer); // Read all bytes.
                if (intBuffer > 0)
                {
                    TraceMessage($"Received on port: ");
                    receivedMessage = utilities.ByteToHexaCDC(byteBuffer, intBuffer);
                }
                //Bytes reading ends ..

                int Len, ChkSum, i;
                string Work, S, ReceivedCRC, CRC1, CRC2 = string.Empty;

                while (receivedMessage.Length >= 2)
                {
                    switch (CommState)
                    {
                        case TCommState.csEnqSent:

                            if (receivedMessage == DLE + ACK)
                            {
                                //Ready to send the message.
                                receivedMessage = string.Empty;
                                TraceMessage("Received ACK after ENQ. Sending message:");// + CRLF + HexDump(MsgWaiting, 4));
                                var sendValue = utilities.GetSendBytes(MsgWaiting);
                                utilities.SplitBytes(sendValue, 16); // Write detailed information into Log file.
                                SendNow();

                            }
                            else if (receivedMessage == DLE + NAK)
                            {
                                // Terminal not ready; resend ENQ but state does not change;
                                receivedMessage = string.Empty;
                                Thread.Sleep(500);
                                MyWriteStr(DLE + ENQ);
                            }
                            break;
                        case TCommState.csMsgSent:
                            if (receivedMessage.Substring(0, 4) == DLE + ACK)
                            {
                                // Message sent successfully
                                receivedMessage = receivedMessage.Substring(4, receivedMessage.Length - 4);

                                CommState = TCommState.csReady;
                                MsgWaiting = string.Empty;
                                CommState = TCommState.csWaitEnq;
                            }
                            else if (receivedMessage == DLE + NAK)
                            {
                                // Message failed to send. Need to resend.
                                // ...but pause briefly
                                TraceMessage("Sent message was NAK''ed. Resending...");
                                receivedMessage = string.Empty; ;
                                Thread.Sleep(200);
                                SendNow();

                            }
                            break;
                        case TCommState.csWaitEnq:
                            if (receivedMessage.Length >= 4)
                            {
                                CommState = TCommState.csReady;
                                receivedMessage = string.Empty;
                                MyWriteStr(DLE + ACK);
                            }
                            break;
                        case TCommState.csReady:
                            if (receivedMessage.Length >= 4)
                            {
                                try
                                {
                                    Len = int.Parse(receivedMessage.Substring(4, 4), System.Globalization.NumberStyles.HexNumber);
                                    if (byteBuffer.Length >= Len + 8)
                                    {
                                        S = receivedMessage.Substring(4, receivedMessage.Length - 8);

                                        TraceMessage($"Dev 99 : Processing ");
                                        var receivedBytes = utilities.GetSendBytes(S);
                                        utilities.SplitBytes(receivedBytes, 16); // Write detailed information into Log file.
                                        var crcChecksumByte = new byte[2];

                                        cRC.FujiCRC(ref receivedBytes, receivedBytes.Length, ref crcChecksumByte);

                                        CRC1 = BitConverter.ToString(crcChecksumByte).Replace("-", string.Empty);
                                        ReceivedCRC = receivedMessage.Substring(receivedMessage.Length - 4, 4);
                                        receivedMessage = string.Empty;

                                        TraceMessage($"Received CRC {ReceivedCRC}");
                                        TraceMessage($"Calculated CRC {CRC1}");

                                        if (CRC1.Equals(ReceivedCRC))
                                        {
                                            TraceMessage($"Received CRC matches calculated CRC.");
                                            MyWriteStr(DLE + ACK);
                                            ProcessMsg(S.Substring(4, S.Length - 8));
                                            IsProcessCompleted = true;
                                        }
                                        else
                                        {
                                            TraceMessage("Received message CRC mismatch; sending NAK...");
                                            MyWriteStr(DLE + NAK);
                                        }

                                    }
                                }
                                catch (Exception ex)
                                {
                                    TraceMessage($"Exception at Ready State :{ex.Message}");
                                    receivedMessage = string.Empty;
                                }

                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                TraceMessage($"Exception at OnDataReceived :{ex.Message}");
            }

        }

        private void SendNow()
        {
            try
            {
                TraceMessage("Entered SendNow");
                var sendValue = utilities.GetSendBytes(MsgWaiting);
                StringBuilder Msg = new StringBuilder();
                var Length = sendValue.Length;
                TraceMessage($"Length of the message is {Length}");
                Msg.Append((Length / 256).ToString("X").Length < 2 ? "0" + (Length / 256).ToString("X") : (Length / 256).ToString("X"));
                Msg.Append((Length % 256).ToString("X").Length < 2 ? "0" + (Length % 256).ToString("X") : (Length % 256).ToString("X"));// + MsgWaiting + DLE + ETX);
                Msg.Append(MsgWaiting);
                Msg.Append(DLE + ETX);
                var crcSourceBytes = utilities.GetSendBytes(Msg.ToString());
                var crcChecksumByte = new byte[2];

                cRC.FujiCRC(ref crcSourceBytes, crcSourceBytes.Length, ref crcChecksumByte);

                var hexString = BitConverter.ToString(crcChecksumByte).Replace("-", string.Empty);

                TraceMessage($"Calculated checksum is {hexString}");
                Msg.Insert(0, STX);
                Msg.Insert(0, DLE);
                Msg.Append(hexString);
                TraceMessage($"Adding CRC {hexString}");
                CommState = TCommState.csMsgSent;
                MyWriteStr(Msg.ToString());
            }
            catch (Exception ex)
            {
                TraceMessage($"Exception at SendNow :{ex.Message}");
            }

        }

        private void ProcessMsg(string message)
        {
            try
            {
                TraceMessage("Entered Fujitsu.ProcessMsg");
                TDevState MYState;
                int i;
                TCommonResp R = new TCommonResp();

                MYState = (TDevState)State;
                //if (!R.ErrorCode.Equals("0"))
                //{
                //    HandleErrorCode();
                //}
                State = TState.stReady;
                TraceMessage($"Back from changing state to Ready.");

                switch (MYState)
                {
                    case TDevState.stWaitRejectInit:
                        ProcessInitResponse(message);
                        CanRetry = true;
                        break;
                    case TDevState.stWaitReset:
                        ProcessInitResponse(message);
                        break;
                    case TDevState.stWaitReadCassettes:
                        ProcessReadCassettes(message);
                        break;
                    case TDevState.stWaitPresenter:
                        CanRetry = true;
                        //ProcessInitResponse(message);
                        break;
                    case TDevState.stWaitTranReply:
                        ProcessDispResponse(message);
                        break;
                }


            }
            catch (Exception ex)
            {
                TraceMessage($" Error at ProcessMsg : {ex.Message}");
            }
        }

        private void ProcessDispResponse(string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    return;
                }

                ProcessInitResponse(message);

                if (message.Substring(0, 1).ToUpper() == "F") //If the dispense fails, reject the notes and void. Else proceed with delivering the note.
                {
                    State = TState.stWaitRejectInit;
                    InitCDU();
                }
                else
                {
                    if (IsDispenseReqSent)
                    {
                        IsDispenseReqSent = false;
                        State = TState.stWaitPresenter;
                        DeliverAndWait();
                    }
                }


            }
            catch (Exception ex)
            {
                TraceMessage($"Exception at ProcessDispResponse :{ex.Message}");
            }
        }


        private void ProcessInitResponse(string work)
        {
            try
            {
                int i = 0;
                TCommonResp R = new TCommonResp();

                TraceMessage($"Entered ProcessInitResponse");
                for (int can = 1; can < 5; can++)
                {

                }

                for (int can = 5; can < 7; can++)
                {

                }

            }
            catch (Exception ex)
            {
                TraceMessage($" Exception at ProcessInitResponse : {ex.Message}");
            }
        }

        private void ProcessCassReg(int CassNum, byte Reg)
        {
            try
            {
                TraceMessage("Entered ProcessCassReg");

                TraceMessage("Leaving ProcessCassReg");
            }
            catch (Exception)
            {

                throw;
            }
        }
        private void TraceMessage(string message)
        {
            logger.Log($"{DateTime.Now:MM-dd-yyyy HH:mm:ss}:{message}");
        }

        private void GetConfig()
        {
            try
            {
                //SendMessage("0x6001FF000001001C");

            }
            catch (Exception ex)
            {
                TraceMessage(ex.Message);
            }
        }

        private void OpenPort()
        {
            try
            {
                TraceMessage("Opening port...");
                if (!commPort.IsOpen)
                {
                    commPort.Open();
                    CommState = TCommState.csReady;
                    Thread.Sleep(500);
                    TraceMessage("Connected to Monitor port.");

                }
            }
            catch (Exception)
            {
                TraceMessage("Attempting to reconnect");
            }
        }

        public string GetFileLocation(string name)
        {
            return ConfigurationManager.AppSettings[name];
        }

        private void DeliverAndWait()
        {
            try
            {
                SendMessage(RequestFrame.DeliverCash);
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void HandleErrorCode()
        {

        }

        private void ProcessReadCassettes(string message)
        {

        }

        private void TestDataReceived()
        {

            int Len;
            string CRC1, S, ReceivedCRC;
            while (receivedMessage.Length >= 2)
            {
                switch (CommState)
                {
                    case TCommState.csEnqSent:

                        if (receivedMessage == DLE + ACK)
                        {
                            //Ready to send the message.
                            receivedMessage = string.Empty;
                            TraceMessage("Received ACK after ENQ. Sending message:");// + CRLF + HexDump(MsgWaiting, 4));
                            var sendValue = utilities.GetSendBytes(MsgWaiting);
                            utilities.SplitBytes(sendValue, 16); // Write detailed information into Log file.

                        }
                        else if (receivedMessage == DLE + NAK)
                        {
                            // Terminal not ready; resend ENQ but state does not change;
                            receivedMessage = string.Empty;
                            Thread.Sleep(500);
                        }
                        break;
                    case TCommState.csMsgSent:
                        if (receivedMessage.Substring(0, 4) == DLE + ACK)
                        {
                            // Message sent successfully
                            receivedMessage = receivedMessage.Substring(4, receivedMessage.Length - 4);

                            CommState = TCommState.csReady;
                            MsgWaiting = string.Empty;
                            CommState = TCommState.csWaitEnq;
                        }
                        else if (receivedMessage == DLE + NAK)
                        {
                            // Message failed to send. Need to resend.
                            // ...but pause briefly
                            TraceMessage("Sent message was NAK''ed. Resending...");
                            receivedMessage = string.Empty; ;
                            Thread.Sleep(200);
                            SendNow();

                        }
                        break;
                    case TCommState.csWaitEnq:
                        if (receivedMessage.Length >= 4)
                        {
                            CommState = TCommState.csReady;
                            receivedMessage = string.Empty;
                        }
                        break;
                    case TCommState.csReady:
                        if (receivedMessage.Length >= 4)
                        {
                            var length = receivedMessage.Substring(8, receivedMessage.Length - 16);

                            Len = int.Parse(receivedMessage.Substring(4, 4), System.Globalization.NumberStyles.HexNumber);
                            if (receivedMessage.Length >= Len + 8)
                            {
                                S = receivedMessage.Substring(4, receivedMessage.Length - 8);

                                TraceMessage($"Dev 99 : Processing ");
                                var receivedBytes = utilities.GetSendBytes(S);
                                utilities.SplitBytes(receivedBytes, 16); // Write detailed information into Log file.
                                var crcChecksumByte = new byte[2];

                                cRC.FujiCRC(ref receivedBytes, receivedBytes.Length, ref crcChecksumByte);

                                CRC1 = BitConverter.ToString(crcChecksumByte).Replace("-", string.Empty);

                                ReceivedCRC = receivedMessage.Substring(receivedMessage.Length - 4, 4);

                                if (CRC1.Equals(ReceivedCRC))
                                {
                                    TraceMessage($"Received CRC matches calculated CRC.");
                                    ProcessMsg(S.Substring(4, S.Length - 8));
                                }
                                else
                                {
                                    TraceMessage("Received message CRC mismatch; sending NAK...");
                                }

                            }
                        }
                        break;
                }
            }
        }

        private void TestCRCCheckSum()
        {
            string S, CRC1, ReceivedCRC = string.Empty;
            var length = receivedMessage.Substring(8, receivedMessage.Length - 16);

            int Len = int.Parse(receivedMessage.Substring(4, 4), System.Globalization.NumberStyles.HexNumber);
            if (receivedMessage.Length >= Len + 8)
            {
                S = receivedMessage.Substring(4, receivedMessage.Length - 8);

                TraceMessage($"Dev 99 : Processing ");
                var receivedBytes = utilities.GetSendBytes(S);
                utilities.SplitBytes(receivedBytes, 16); // Write detailed information into Log file.
                var crcChecksumByte = new byte[2];

                cRC.FujiCRC(ref receivedBytes, receivedBytes.Length, ref crcChecksumByte);

                CRC1 = BitConverter.ToString(crcChecksumByte).Replace("-", string.Empty);
                ReceivedCRC = receivedMessage.Substring(4, receivedMessage.Length - 8);
                receivedMessage = string.Empty;
            }
        }

        private int ConvertByteArrayToInt32(byte[] b)
        {
            return BitConverter.ToInt32(b, 0);
        }

        private byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }


    }
}
