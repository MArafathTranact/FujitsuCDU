using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using FujitsuCDU.Common;
using Newtonsoft.Json;

namespace FujitsuCDU
{
    public class FujitsuCDUProcessor
    {
        readonly Logger logger = new Logger();
        Utilities utilities = new Utilities();
        CRC cRC = new CRC();
        SerialPort commPort;// = new SerialPort("COM7", 9600, Parity.Even, 8, StopBits.One);
        StringBuilder sbreadyMessage = new StringBuilder();
        public EZCashSocket ezcashSocket;
        ErrorCode errorCode = new ErrorCode();
        public System.Timers.Timer timeoutTimer = new System.Timers.Timer(1000 * 4);
        public bool SocketConnected = false;
        public string SuccessDispenseMessage = string.Empty;

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
        public string[] canRequest = { "30", "B1", "B2", "33", "B4", "35", "36", "B7", "B8", "39" };

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

        #region Variables
        public enum TCanState
        {
            canLow, canEmpty
        }
        public enum TCommState { csEnqSent, csMsgSent, csReady, csEnqReceived, csMsgReceived, csWaitEnq };
        public enum TState { stWaitTranReply, stWaitConfig, stWaitReject, stWaitRejectInit, stWaitPresenter, stReady, stWaitReset, stDisconnected, stDown, stWaitReadCassettes };
        public enum TDevState { stWaitTranReply, stWaitConfig, stWaitReject, stWaitRejectInit, stWaitPresenter, stReady, stWaitReset, stDisconnected, stDown, stWaitReadCassettes };

        public TState State;
        public TDevState devState;

        public TCommState CommState;
        public string receivedMessage = string.Empty;
        public string MsgWaiting = string.Empty;
        public bool IsDispenseReqSent = false;
        public bool IsProcessCompleted = false;
        public bool CanRetry = false;

        public decimal TotalAmount = 0.0m;
        public decimal DispensingAmount = 0.0m;
        //public CDU cdu;
        //public CDU3 cdu;
        public CDU cdu;
        public List<Cannister> TheCan = new List<Cannister>(); // To hold the Transaction (error or success) details .

        public decimal AmountDispensed = 0.0m;
        public int VoidCode = 602;
        public string Socketerrorcode = string.Empty;
        public bool ErrorCodeReceived = false;

        public string DispensingMessage = string.Empty;
        public enum TErrorState { stCassetteError, stOtherError };
        public TErrorState errorState;
        #endregion

        public FujitsuCDUProcessor(CDU cduForm)
        {
            try
            {
                ezcashSocket = new EZCashSocket(this);
                cdu = cduForm;
                var comPort = GetFileLocation("ComPort");
                var baudRate = int.Parse(GetFileLocation("BaudRate"));
                var dataBit = int.Parse(GetFileLocation("DataBit"));
                var parity = GetFileLocation("Parity").Equals("Even") ? Parity.Even : Parity.None;
                var stopBits = GetFileLocation("StopBits").Equals("One") ? StopBits.One : StopBits.None;
                commPort = new SerialPort(comPort, baudRate, parity, dataBit, stopBits);
                commPort.ReceivedBytesThreshold = 1;
                commPort.Encoding = Encoding.ASCII;
                commPort.DataReceived += new SerialDataReceivedEventHandler(OnDataReceived);

                timeoutTimer.Elapsed += new ElapsedEventHandler(TimeOutEvent);
                timeoutTimer.Enabled = false;

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

        public void DispenseAmount(string amount, string originalAmount, string[] dispenseMessage)
        {
            try
            {
                logger.Log("Entered DispenseAmount");
                logger.Log($"Loading screen : Dispensing ${ amount} of ${ originalAmount} ");
                DisplayDescription(3, "", 20, "", 20, $"Dispensing ${amount} of ${originalAmount}", 20, "", 20);

                CanRetry = false;
                // Sample message:
                // ------------------ODR-------- N1--- N2--- N3--- N4--- R1--- R2--- R3--- R4--- P1 P2 P3 P4 N5--- N6--- N7--- N8--- R5--- R6--- R7--- R8--- P5 P6 P7 P8 FS
                // 60 03 ff 00 00 2c fe dc ba 98 30 b1 30 30 30 30 30 30 b1 30 30 30 30 30 30 30 15 00 00 00 30 30 30 30 30 30 30 30 30 30 30 30 30 30 30 30 00 00 00 00 1c

                //var msg = string.Empty;
                StringBuilder sb = new StringBuilder();

                sb.Append("6003ff00002cfedcba98");//DH0(60),DH1(03),DH2(FF), RSV1(00) , DH3(002C) ,0DR(fedcba98)

                //msg = "$6003ff00002cfedcba98"; // This includes ODR.

                for (int i = 0; i < 4; i++)
                {
                    //Cannisters[i].HostCycleCount := Cannisters[i].HostCycleCount + Cannisters[i].Dispensed;
                    //{ inc( }
                    //CurTran.CannDisp[Cannisters[i].ID] := CurTran.CannDisp[Cannisters[i].ID] + (Cannisters[i].Dispensed);
                    var divval = int.Parse(dispenseMessage[i]) / 10;
                    var modVal = int.Parse(dispenseMessage[i]) % 10;
                    sb.Append(canRequest[divval]); //CChar[Integer(Cannisters[i].Dispensed div 10)];
                    sb.Append(canRequest[modVal]); //CChar[Integer(Cannisters[i].Dispensed mod 10)];
                }

                sb.Append("b130b130b130b13015151515");// R1,R2,R3,R4,P1,P2,P3,P4

                for (int i = 4; i < 6; i++)
                {
                    //Cannisters[i].HostCycleCount := Cannisters[i].HostCycleCount + Cannisters[i].Dispensed;
                    //{ inc( }
                    //CurTran.CannDisp[Cannisters[i].ID] := CurTran.CannDisp[Cannisters[i].ID] + (Cannisters[i].Dispensed);
                    var divval = int.Parse(dispenseMessage[i]) / 10;
                    var modVal = int.Parse(dispenseMessage[i]) % 10;

                    sb.Append(canRequest[divval]); //CChar[Integer(Cannisters[i].Dispensed div 10)];
                    sb.Append(canRequest[modVal]); //CChar[Integer(Cannisters[i].Dispensed mod 10)];
                }

                sb.Append("30303030b130b130b130b130151515151C"); // N7,N8,R5,R6,R7,R8,P5,P6,P7,P8
                                                                 //State:= stWaitTranReply;
                                                                 //SendMessage(sb.ToString());
                                                                 //cduProcessor.MsgWaiting = sb.ToString();
                TotalAmount = Convert.ToDecimal(originalAmount);
                DispensingAmount = Convert.ToDecimal(amount);
                State = TState.stWaitTranReply;
                SendMessage(sb.ToString());
                //cduProcessor.MsgWaiting = "6002ff00001a0003483a483a483a483a0d0d0d0d483a483a000000000d0d00001c";


            }
            catch (Exception ex)
            {
                logger.Log(ex.Message);
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
                devState = TDevState.stWaitReset;
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

                int Len;
                string S, ReceivedCRC, CRC1, CRC2 = string.Empty;

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
                                            ProcessMsg(S.Substring(4, S.Length - 8), byteBuffer.Skip(4).ToArray());
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

        private void ProcessMsg(string message, byte[] messagebyte)
        {
            try
            {
                TraceMessage("Entered Fujitsu.ProcessMsg");
                TDevState MYState;
                TCommonResp R = new TCommonResp();

                MYState = (TDevState)State;

                State = TState.stReady;
                TraceMessage($"Back from changing state to Ready.");

                switch (MYState)
                {
                    case TDevState.stWaitRejectInit:
                        ProcessInitResponse(message, messagebyte);
                        CanRetry = true;
                        break;
                    case TDevState.stWaitReset:
                        ProcessInitResponse(message, messagebyte);
                        break;
                    case TDevState.stWaitReadCassettes:
                        ProcessReadCassettes(message);
                        if (!SocketConnected)
                        {
                            ezcashSocket.CreateEZCashSocket();
                        }
                        break;
                    case TDevState.stWaitPresenter:
                        TraceMessage($"Dispense Completed");
                        CanRetry = true;
                        OnDispenseCompleted(message, messagebyte);
                        break;
                    case TDevState.stWaitTranReply:
                        ProcessDispResponse(message, messagebyte);
                        break;
                }


            }
            catch (Exception ex)
            {
                TraceMessage($" Error at ProcessMsg : {ex.Message}");
            }
        }

        public void ProcessDispResponse(string message, byte[] messagebyte)
        {
            try
            {
                byte[] TMsgData = new byte[40];
                byte[,] TTotals = new byte[4, 32];
                var R = new TCommonResp();
                var E = new TErrResponse();
                var D = new TDispenseResp();
                var C = new TCassetteStats();
                var SensorRep = new TSensorResponse();
                var BillCounts1 = new TBillCounts();
                var BillCounts2 = new TBillCounts();
                var MsgDta = TMsgData;
                var Totals1 = TTotals;
                var Totals2 = TTotals;

                if (string.IsNullOrWhiteSpace(message))
                {
                    return;
                }

                // ProcessInitResponse(message, messagebyte);

                if (message.Substring(0, 1).ToUpper() == "F") //If the dispense fails, reject the notes and void. Else proceed with delivering the note.
                {
                    ErrorCodeReceived = true;
                    var errorcode = message.Substring(12, 4).Trim().ToUpper();
                    TraceMessage($"Error code received : {errorcode}");

                    var result = errorCode.MapErrorCan(errorcode);
                    var canStatus = new StringBuilder();
                    var errorseverity = new StringBuilder();
                    canStatus.Append(0);

                    if (result != null && !string.IsNullOrWhiteSpace(result.MappingCan))
                    {
                        errorState = TErrorState.stCassetteError;
                        for (int i = 0; i < 6; i++)
                        {
                            switch (i)
                            {
                                case 0:
                                    if (result.MappingCan == "01")
                                    {
                                        canStatus.Append(4);
                                        errorseverity.Append(3);
                                    }
                                    else
                                    {
                                        canStatus.Append(1);
                                        errorseverity.Append(1);
                                    }
                                    break;
                                case 1:
                                    if (result.MappingCan == "02")
                                    {
                                        canStatus.Append(4);
                                        errorseverity.Append(3);
                                    }
                                    else
                                    {
                                        canStatus.Append(1);
                                        errorseverity.Append(1);
                                    }
                                    break;
                                case 2:
                                    if (result.MappingCan == "03")
                                    {
                                        canStatus.Append(4);
                                        errorseverity.Append(3);
                                    }
                                    else
                                    {
                                        canStatus.Append(1);
                                        errorseverity.Append(1);
                                    }
                                    break;
                                case 3:
                                    if (result.MappingCan == "04")
                                    {
                                        canStatus.Append(4);
                                        errorseverity.Append(3);
                                    }
                                    else
                                    {
                                        canStatus.Append(1);
                                        errorseverity.Append(1);
                                    }
                                    break;
                                case 4:
                                    if (result.MappingCan == "05")
                                    {
                                        canStatus.Append(4);
                                        errorseverity.Append(3);
                                    }
                                    else
                                    {
                                        canStatus.Append(1);
                                        errorseverity.Append(1);
                                    }
                                    break;
                                case 5:
                                    if (result.MappingCan == "06")
                                    {
                                        canStatus.Append(4);
                                        errorseverity.Append(3);
                                    }
                                    else
                                    {
                                        canStatus.Append(1);
                                        errorseverity.Append(1);
                                    }
                                    break;
                            }

                        }

                        Socketerrorcode = $"0022.000..8.E2000000000000.{canStatus.ToString()}.0401500000000300000000.{errorseverity.ToString()}";
                        TraceMessage($"Message to EZCash socket : {Socketerrorcode}");
                    }
                    else
                    {
                        errorState = TErrorState.stOtherError;
                    }

                    AmountDispensed = 0;
                    State = TState.stWaitRejectInit;
                    InitCDU();
                }
                else
                {
                    SuccessDispenseMessage = message;
                    var canDetails1to4 = message.Substring(180, 16);
                    var canMaxMinDetails1to4 = message.Substring(342, 40);
                    var canDetails5to8 = message.Substring(414, 16);
                    var canMaxMinDetails5to8 = message.Substring(576, 40);
                    int count = 1;
                    var dispensedMessage = string.Empty;
                    foreach (var item in canDetails1to4.SplitInParts(4))
                    {
                        TraceMessage($"Cassette {count} dispensed {item[1] }{item[3]} bills.");
                        dispensedMessage += item[1].ToString() + item[3].ToString();
                        count++;
                    }
                    count = 5;
                    foreach (var item in canDetails5to8.SplitInParts(4))
                    {
                        TraceMessage($"Cassette {count} dispensed {item[1] }{item[3]} bills.");
                        dispensedMessage += item[1].ToString() + item[3].ToString();
                        count++;
                    }

                    TraceMessage($"Total Dispensed bill details : {dispensedMessage}");
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

        private void ProcessInitResponse(string work, byte[] workbyte)
        {
            try
            {

                if (ErrorCodeReceived)
                {
                    ErrorCodeReceived = false;

                    switch (errorState)
                    {
                        case TErrorState.stCassetteError:
                            TraceMessage($"Error code received , sending {Socketerrorcode} to EZcash socket");
                            ezcashSocket.SendMessage(Socketerrorcode);
                            //var ezResponse = ezcashSocket.ReceiveMessage();

                            //var dispenseMessage = ezResponse.Split('.')[5].Replace("\u001d", "");

                            //var originalAmount = ezResponse.Split('.')[6].Replace("\u001d", "");
                            //var dispensingAmount = ezResponse.Split('.')[7].Replace("\u001d", "");
                            //DispenseAmount(dispensingAmount, originalAmount, dispenseMessage.SplitInParts(2).ToArray());
                            break;
                        case TErrorState.stOtherError:
                            DispenseAmount(Convert.ToString(DispensingAmount), Convert.ToString(TotalAmount), DispensingMessage.SplitInParts(2).ToArray());
                            break;
                    }

                }
                //int i = 0;
                //TCommonResp R = new TCommonResp();
                //if (workbyte != null)
                //{
                //    R.DH0 = workbyte[0];
                //    R.DH1 = workbyte[1];
                //    R.DH2 = workbyte[2];
                //    R.DH3 = new byte[] { workbyte[3], workbyte[4] };
                //    R.ErrorCode = "";
                //    R.CassetteRegister = new byte[] { workbyte[15], workbyte[16], workbyte[17], workbyte[18] };
                //    R.CassetteRegister2 = new byte[] { workbyte[45], workbyte[46], workbyte[47], workbyte[48] };

                //    TraceMessage($"Entered ProcessInitResponse {work}");


                //    for (int can = 0; can < 4; can++)
                //    {
                //        ProcessCassReg(can, R.CassetteRegister[can]);
                //    }

                //    for (int can = 4; can < 6; can++)
                //    {
                //        ProcessCassReg(can, R.CassetteRegister[can]);
                //    }
                //}

            }
            catch (Exception ex)
            {
                TraceMessage($" Exception at ProcessInitResponse : {ex.Message}");
            }
        }

        private void OnDispenseCompleted(string message, byte[] messageByte)
        {
            try
            {
                TraceMessage($"Processing  : LABEL1=<Transaction complete.>Label2=<Thank you.>");

                DisplayDescription(3, string.Empty, 0, string.Empty, 0, "Transaction complete.", 20, string.Empty, 0);
                DisplayDescription(4, string.Empty, 0, string.Empty, 0, string.Empty, 0, "Thank you.", 20);

                timeoutTimer.Enabled = true;
                timeoutTimer.Start();

                TraceMessage($"Sending EZCash socket response on dispense completed.");
                ezcashSocket.SendMessage($"0022.000..9");
                Thread.Sleep(2000);
                UpdateCassetteStatus();
            }
            catch (Exception ex)
            {
                TraceMessage($" Exception at OnDispenseCompleted : {ex.Message}");
            }
        }

        public void UpdateCassetteStatus()
        {
            //var message = "e0 03 ff 00 01 32 00 00 41 54 30 32 00 00 00 00 00 4a 3f 81 00 8b 0d 09 0e e8 48 3a 48 3a 48 3a 48 3a 0d 0d 0d 0d 00 00 00 00 30 36 31 30 32 33 41 54 30 32 30 31 00 00 00 00 00"
            //   + "00 00 00 00 00 00 00 00 00 00 0a 8c 30 30 00 48 3a 48 3a 00 00 00 00 0d 0d 00 00 00 00 00 00 30 30 " +
            //   "30 33 30 b1 30 b1 30 30 30 30 30 30 30 30 00 00 00 00 00 00 00 00 00 00 00 00 ff 00 ff ff 00 00 00 00 00 00 00 00 00 00 00 00 ff 00 ff ff 00 00 00 00 00 00 00 00 00 00 00 00 ff 00 ff ff 00 00 " +
            //   "00 00 00 00 00 00 00 00 00 00 ff 00 ff ff 00 30 30 30 33 30 b1 30 b1 b1 30 b1 30 b1 30 b1 30 15 15 15 15 e7 cf e7 e7 e7 ef ef e7 ef e7 d7 e7 e7 00 00 00 30 b1 30 b1 30" +
            //   "30 30 30 30 30 30 30 30 30 30 30 00 00 00 00 00 00 00 00 00 00 00 00 ff 00 ff ff 00 00 00 00 00 00 00 00 00 00 00 00 ff" +
            //   "00 ff ff 00 00 00 00 00 00 00 00 00 00 00 00 ff 00 ff ff 00 00 00 00 00 00 00 00 00 00 00 00 ff 00 ff ff 00 30 b1 30 b1 30 30 30 30 b1 30 b1 30 b1 30 b1 30 15 15 15 15 fe dc ba 98 1c 10 03 78 bb"
            //   ;
            //SuccessDispenseMessage = message.Trim();
            var can1to4Status = SuccessDispenseMessage.Replace(" ", "").Substring(42, 8);
            var cassetteStatus = string.Empty;
            foreach (var item in can1to4Status.SplitInParts(2))
            {
                if (item[0] == '8')
                    cassetteStatus += "4";
                else
                    cassetteStatus += "0";

            }
            var can5to8Status = SuccessDispenseMessage.Replace(" ", "").Substring(138, 8);
            foreach (var item in can5to8Status.SplitInParts(2))
            {
                if (item[0] == '8')
                    cassetteStatus += "4";
                else
                    cassetteStatus += "0";

            }

            ezcashSocket.SendMessage($"0031.{cassetteStatus}..9");

        }


        private void ProcessCassReg(int CassNum, byte Reg)
        {
            var ID = string.Empty;
            int j;


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
            catch (Exception ex)
            {
                TraceMessage($"Exception at OpenPort :{ex.Message} .Attempting to reconnect .");
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
                TraceMessage($"Processing  : LABEL1=<Dispensed ${DispensingAmount} of ${TotalAmount}>Label2=<Please take your cash>");

                DisplayDescription(3, "", 0, "", 0, $"Dispensed ${DispensingAmount} of ${TotalAmount}", 20, "", 0);
                DisplayDescription(4, "", 0, "", 0, "", 0, "Please take your cash", 20);

                //timeoutTimer.Enabled = true;
                //timeoutTimer.Start();
                SendMessage(RequestFrame.DeliverCash);

            }
            catch (Exception ex)
            {
                TraceMessage($"Exception at DeliverAndWait :{ex.Message}.");
            }
        }

        private void RejectNote()
        {
            try
            {
                TraceMessage("Connected to Monitor port.");
                State = TState.stWaitReject;
                SendMessage(RequestFrame.Reject);

            }
            catch (Exception ex)
            {
                TraceMessage($"Exception at RejectNote :{ex.Message}.");
            }
        }

        private void HandleErrorCode()
        {

        }

        private void ProcessReadCassettes(string message)
        {

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

        public void DisplayDescription(int ScreenNo, string message, int size1, string message2, int size2, string message3, int size3, string message4, int size4)
        {
            //await Task.Run(() =>
            //{

            switch (ScreenNo)
            {
                case 1:
                    var lblInitial1 = cdu.Controls.Find("lblInitial1", true).FirstOrDefault();
                    var pnlInitialize = cdu.Controls.Find("pnlInitialize", true).FirstOrDefault();

                    if (null != lblInitial1 && lblInitial1 is Label && null != pnlInitialize && pnlInitialize is Panel)
                    {

                        cdu.BeginInvoke((Action)delegate ()
                        {
                            (lblInitial1 as Label).Text = string.Empty;
                            (lblInitial1 as Label).Text = message;
                            (lblInitial1 as Label).Font = new Font("Calibri", size1, FontStyle.Regular);
                            (lblInitial1 as Label).SetBounds(((pnlInitialize as Panel).ClientSize.Width - (lblInitial1 as Label).Width) / 2, ((pnlInitialize as Panel).ClientSize.Height - (lblInitial1 as Label).Height) / 2, 0, 0, BoundsSpecified.Location);

                        });

                    }
                    break;
                case 2:
                    var lblInitial2 = cdu.Controls.Find("lblInitial2", true).FirstOrDefault();
                    var pnlInitialize2 = cdu.Controls.Find("pnlInitialize2", true).FirstOrDefault();

                    if (null != lblInitial2 && lblInitial2 is Label && null != pnlInitialize2 && pnlInitialize2 is Panel)
                    {

                        cdu.BeginInvoke((Action)delegate ()
                        {
                            (lblInitial2 as Label).Text = string.Empty;
                            (lblInitial2 as Label).Text = message;
                            (lblInitial2 as Label).Font = new Font("Calibri", size2, FontStyle.Regular);
                            (lblInitial2 as Label).SetBounds(((pnlInitialize2 as Panel).ClientSize.Width - (lblInitial2 as Label).Width) / 2, (((pnlInitialize2 as Panel).ClientSize.Height - (lblInitial2 as Label).Height) / 2) - 40, 0, 0, BoundsSpecified.Location);

                        });

                    }
                    break;
                case 3:
                    var lblMessage1 = cdu.Controls.Find("lblMessage1", true).FirstOrDefault();
                    var pnlMessage = cdu.Controls.Find("pnlMessage", true).FirstOrDefault();
                    var lblMessage21 = cdu.Controls.Find("lblMessage2", true).FirstOrDefault();
                    if (null != lblMessage1 && lblMessage1 is Label && null != pnlMessage && pnlMessage is Panel && lblMessage21 != null && lblMessage21 is Label)
                    {

                        cdu.BeginInvoke((Action)delegate ()
                        {
                            (lblMessage1 as Label).Text = string.Empty;
                            (lblMessage21 as Label).Text = string.Empty;
                            (lblMessage1 as Label).Text = message3;
                            (lblMessage1 as Label).Font = new Font("Calibri", size3, FontStyle.Regular);
                            (lblMessage1 as Label).SetBounds(((pnlMessage as Panel).ClientSize.Width - (lblMessage1 as Label).Width) / 2, ((pnlMessage as Panel).ClientSize.Height - (lblMessage1 as Label).Height) / 2, 0, 0, BoundsSpecified.Location);

                        });

                    }
                    break;
                case 4:
                    var lblMessage2 = cdu.Controls.Find("lblMessage2", true).FirstOrDefault();
                    var pnlMessage2 = cdu.Controls.Find("pnlMessage2", true).FirstOrDefault();

                    if (null != lblMessage2 && lblMessage2 is Label && null != pnlMessage2 && pnlMessage2 is Panel)
                    {

                        cdu.BeginInvoke((Action)delegate ()
                        {
                            (lblMessage2 as Label).Text = string.Empty;
                            (lblMessage2 as Label).Text = message4;
                            (lblMessage2 as Label).Font = new Font("Calibri", size4, FontStyle.Regular);
                            (lblMessage2 as Label).SetBounds(((pnlMessage2 as Panel).ClientSize.Width - (lblMessage2 as Label).Width) / 2, (((pnlMessage2 as Panel).ClientSize.Height - (lblMessage2 as Label).Height) / 2) - 40, 0, 0, BoundsSpecified.Location);

                        });

                    }
                    break;
                case 5:
                    var Initial1 = cdu.Controls.Find("lblInitial1", true).FirstOrDefault();
                    var Initial2 = cdu.Controls.Find("lblInitial2", true).FirstOrDefault();
                    var Message1 = cdu.Controls.Find("lblMessage1", true).FirstOrDefault();
                    var Message2 = cdu.Controls.Find("lblMessage2", true).FirstOrDefault();
                    var pnlMessage1 = cdu.Controls.Find("pnlMessage", true).FirstOrDefault();

                    if (null != Initial1 && Initial1 is Label && null != Initial2 && Initial2 is Label && null != Message1 && Message1 is Label && null != Message2 && Message2 is Label && null != pnlMessage1 && pnlMessage1 is Panel)
                    {

                        cdu.BeginInvoke((Action)delegate ()
                        {
                            (Initial1 as Label).Text = string.Empty;
                            (Initial2 as Label).Text = string.Empty;
                            (Message2 as Label).Text = string.Empty;

                            (Message1 as Label).Text = message3;
                            (Message1 as Label).Font = new Font("Calibri", size3, FontStyle.Italic);
                            (Message1 as Label).SetBounds(((pnlMessage1 as Panel).ClientSize.Width - (Message1 as Label).Width) / 2, ((pnlMessage1 as Panel).ClientSize.Height - (Message1 as Label).Height) / 2, 0, 0, BoundsSpecified.Location);


                        });

                    }
                    break;
                default:
                    break;

            }


            //}

            //);


        }

        private void TimeOutEvent(object source, ElapsedEventArgs e)
        {
            DisplayDescription(5, "", 0, "", 0, "Please scan the barcode", 20, "", 0);
            timeoutTimer.Stop();
            timeoutTimer.Enabled = false;
        }

        public async Task ProcessBarcodeTransaction(string ezResponse)
        {
            await Task.Run(() =>
            {
                try
                {
                    //logger.Log($"Sending Socket transaction request {barcode}..");
                    //CommState = FujitsuCDUProcessor.TCommState.csReady;
                    //IsDispenseReqSent = true;
                    //State = FujitsuCDUProcessor.TState.stWaitTranReply;
                    ////cduProcessor.devState = FujitsuCDUProcessor.TDevState.stWaitTranReply;
                    //// cduProcessor.SendMessage("6003ff00002cfedcba9830b130b130303030b1303030303030301500000030303030303030303030303030303030000000001c");
                    //ezcashSocket.SendMessage($"1111.000...1 > .{barcode}..ABC....");
                    //var ezResponse = cduProcessor.ezcashSocket.ReceiveMessage();

                    var dispenseMessage = ezResponse.Split('.')[4].Replace("\u001d", "");
                    DispensingMessage = dispenseMessage;
                    var originalAmount = ezResponse.Split('.')[5].Replace("\u001d", "");
                    var dispensingAmount = ezResponse.Split('.')[6].Replace("\u001d", "");
                    //var errorMessage = ezResponse.Split('.')[32].Substring(1, ezResponse.Split('.')[32].Length - 1);
                    logger.Log($"Received Socket Dispense response : {dispenseMessage}");

                    if (Convert.ToInt64(dispenseMessage) != 0)
                    {
                        DispenseAmount(dispensingAmount, originalAmount, dispenseMessage.SplitInParts(2).ToArray());
                    }
                    else
                    {
                        TraceMessage($"Processing  : LABEL1=<{"Invalid Card"}>Label2=<Transaction cancelled.>");
                        DisplayDescription(3, "", 20, "", 20, "Invalid Card", 20, "", 20);
                        DisplayDescription(4, "", 20, "", 20, "", 0, "Transaction cancelled.", 20);

                        timeoutTimer.Enabled = true;
                        timeoutTimer.Start();
                    }
                    //logger.Log($"Received Socket response {dispenseMessage} , Original Amount = ${originalAmount} , Dispensing Amount= ${dispensingAmount}");
                    //dispenseMessage = "150000000000";


                }
                catch (Exception ex)
                {
                    logger.Log($"ParseBarcodeMessage {ex.Message} ");
                }
            });
        }

        public async Task ProcessCassetteStatus(string ezResponse)
        {
            await Task.Run(() =>
            {

                if (!string.IsNullOrWhiteSpace(ezResponse))
                {

                    try
                    {
                        ezResponse = ezResponse.Remove(0, ezResponse.IndexOf('['));


                        int index = ezResponse.LastIndexOf("]");
                        if (index > 0)
                            ezResponse = ezResponse.Substring(0, index + 1);

                        var denoms = JsonConvert.DeserializeObject<List<DenominationInfo>>(ezResponse);

                        var pnlCasstteStatus = cdu.Controls.Find("pnlCasstteStatus", true).FirstOrDefault();

                        foreach (var item in denoms)
                        {
                            switch (item.cassette_nbr)
                            {
                                case "1":

                                    var pnlCasette1 = cdu.Controls.Find("pnlCasette1", true).FirstOrDefault();
                                    if (pnlCasette1 != null && pnlCasstteStatus != null && pnlCasette1 is Panel && pnlCasstteStatus is Panel)
                                    {
                                        cdu.BeginInvoke((Action)delegate ()
                                        {
                                            pnlCasette1.SetBounds(10, (pnlCasstteStatus.ClientSize.Height - pnlCasette1.Height) / 2, 0, 0, BoundsSpecified.Location);
                                            pnlCasette1.Visible = true;
                                            switch (item.status)
                                            {
                                                case "0":
                                                    pnlCasette1.BackColor = Color.Green;
                                                    break;
                                                case "2":
                                                    pnlCasette1.BackColor = Color.Red;
                                                    break;
                                                case "4":
                                                    pnlCasette1.BackColor = Color.Yellow;
                                                    break;

                                            }
                                        });
                                    }
                                    break;
                                case "2":
                                    var pnlCasette2 = cdu.Controls.Find("pnlCasette2", true).FirstOrDefault();
                                    if (pnlCasette2 != null && pnlCasstteStatus != null && pnlCasette2 is Panel && pnlCasstteStatus is Panel)
                                    {
                                        cdu.BeginInvoke((Action)delegate ()
                                        {
                                            pnlCasette2.SetBounds(40, (pnlCasstteStatus.ClientSize.Height - pnlCasette2.Height) / 2, 0, 0, BoundsSpecified.Location);
                                            pnlCasette2.Visible = true;
                                            switch (item.status)
                                            {
                                                case "0":
                                                    pnlCasette2.BackColor = Color.Green;
                                                    break;
                                                case "2":
                                                    pnlCasette2.BackColor = Color.Red;
                                                    break;
                                                case "4":
                                                    pnlCasette2.BackColor = Color.Yellow;
                                                    break;

                                            }
                                        });
                                    }

                                    break;
                                case "3":
                                    var pnlCasette3 = cdu.Controls.Find("pnlCasette3", true).FirstOrDefault();
                                    if (pnlCasette3 != null && pnlCasstteStatus != null && pnlCasette3 is Panel && pnlCasstteStatus is Panel)
                                    {
                                        cdu.BeginInvoke((Action)delegate ()
                                        {
                                            pnlCasette3.SetBounds(70, (pnlCasstteStatus.ClientSize.Height - pnlCasette3.Height) / 2, 0, 0, BoundsSpecified.Location);
                                            pnlCasette3.Visible = true;
                                            switch (item.status)
                                            {
                                                case "0":
                                                    pnlCasette3.BackColor = Color.Green;
                                                    break;
                                                case "2":
                                                    pnlCasette3.BackColor = Color.Red;
                                                    break;
                                                case "4":
                                                    pnlCasette3.BackColor = Color.Yellow;
                                                    break;

                                            }
                                        });
                                    }

                                    break;
                                case "4":
                                    var pnlCasette4 = cdu.Controls.Find("pnlCasette4", true).FirstOrDefault();
                                    if (pnlCasette4 != null && pnlCasstteStatus != null && pnlCasette4 is Panel && pnlCasstteStatus is Panel)
                                    {
                                        cdu.BeginInvoke((Action)delegate ()
                                        {
                                            pnlCasette4.SetBounds(100, (pnlCasstteStatus.ClientSize.Height - pnlCasette4.Height) / 2, 0, 0, BoundsSpecified.Location);
                                            pnlCasette4.Visible = true;
                                            switch (item.status)
                                            {
                                                case "0":
                                                    pnlCasette4.BackColor = Color.Green;
                                                    break;
                                                case "2":
                                                    pnlCasette4.BackColor = Color.Red;
                                                    break;
                                                case "4":
                                                    pnlCasette4.BackColor = Color.Yellow;
                                                    break;

                                            }
                                        });
                                    }

                                    break;
                                case "5":
                                    var pnlCasette5 = cdu.Controls.Find("pnlCasette5", true).FirstOrDefault();
                                    if (pnlCasette5 != null && pnlCasstteStatus != null && pnlCasette5 is Panel && pnlCasstteStatus is Panel)
                                    {
                                        cdu.BeginInvoke((Action)delegate ()
                                        {
                                            pnlCasette5.SetBounds(130, (pnlCasstteStatus.ClientSize.Height - pnlCasette5.Height) / 2, 0, 0, BoundsSpecified.Location);
                                            pnlCasette5.Visible = true;
                                            switch (item.status)
                                            {
                                                case "0":
                                                    pnlCasette5.BackColor = Color.Green;
                                                    break;
                                                case "2":
                                                    pnlCasette5.BackColor = Color.Red;
                                                    break;
                                                case "4":
                                                    pnlCasette5.BackColor = Color.Yellow;
                                                    break;

                                            }
                                        });
                                    }

                                    break;
                                case "6":
                                    var pnlCasette6 = cdu.Controls.Find("pnlCasette6", true).FirstOrDefault();
                                    if (pnlCasette6 != null && pnlCasstteStatus != null && pnlCasette6 is Panel && pnlCasstteStatus is Panel)
                                    {
                                        cdu.BeginInvoke((Action)delegate ()
                                        {
                                            pnlCasette6.SetBounds(160, (pnlCasstteStatus.ClientSize.Height - pnlCasette6.Height) / 2, 0, 0, BoundsSpecified.Location);
                                            pnlCasette6.Visible = true;
                                            switch (item.status)
                                            {
                                                case "0":
                                                    pnlCasette6.BackColor = Color.Green;
                                                    break;
                                                case "2":
                                                    pnlCasette6.BackColor = Color.Red;
                                                    break;
                                                case "4":
                                                    pnlCasette6.BackColor = Color.Yellow;
                                                    break;

                                            }
                                        });
                                    }

                                    break;
                                case "7":
                                    var pnlCasette7 = cdu.Controls.Find("pnlCasette7", true).FirstOrDefault();
                                    if (pnlCasette7 != null && pnlCasstteStatus != null && pnlCasette7 is Panel && pnlCasstteStatus is Panel)
                                    {
                                        cdu.BeginInvoke((Action)delegate ()
                                        {
                                            pnlCasette7.SetBounds(190, (pnlCasstteStatus.ClientSize.Height - pnlCasette7.Height) / 2, 0, 0, BoundsSpecified.Location);
                                            pnlCasette7.BackColor = Color.Red;
                                            pnlCasette7.Visible = true;

                                        });
                                    }


                                    break;
                                case "8":
                                    var pnlCasette8 = cdu.Controls.Find("pnlCasette8", true).FirstOrDefault();
                                    if (pnlCasette8 != null && pnlCasstteStatus != null && pnlCasette8 is Panel && pnlCasstteStatus is Panel)
                                    {
                                        cdu.BeginInvoke((Action)delegate ()
                                        {
                                            pnlCasette8.SetBounds(220, (pnlCasstteStatus.ClientSize.Height - pnlCasette8.Height) / 2, 0, 0, BoundsSpecified.Location);
                                            pnlCasette8.BackColor = Color.Red;
                                            pnlCasette8.Visible = true;

                                        });
                                    }


                                    break;
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        var pnlCasstteStatus = cdu.Controls.Find("pnlCasstteStatus", true).FirstOrDefault();
                        var pnlCasette1 = cdu.Controls.Find("pnlCasette1", true).FirstOrDefault();
                        var pnlCasette2 = cdu.Controls.Find("pnlCasette2", true).FirstOrDefault();
                        var pnlCasette3 = cdu.Controls.Find("pnlCasette3", true).FirstOrDefault();
                        var pnlCasette4 = cdu.Controls.Find("pnlCasette4", true).FirstOrDefault();
                        var pnlCasette5 = cdu.Controls.Find("pnlCasette5", true).FirstOrDefault();
                        var pnlCasette6 = cdu.Controls.Find("pnlCasette6", true).FirstOrDefault();
                        var pnlCasette7 = cdu.Controls.Find("pnlCasette7", true).FirstOrDefault();
                        var pnlCasette8 = cdu.Controls.Find("pnlCasette8", true).FirstOrDefault();

                        if (pnlCasette1 != null && pnlCasette2 != null && pnlCasette3 != null && pnlCasette4 != null && pnlCasette5 != null && pnlCasette6 != null && pnlCasette7 != null && pnlCasette8 != null && pnlCasstteStatus != null && pnlCasette1 is Panel && pnlCasstteStatus is Panel)
                        {
                            cdu.BeginInvoke((Action)delegate ()
                            {
                                pnlCasette1.SetBounds(10, (pnlCasstteStatus.ClientSize.Height - pnlCasette1.Height) / 2, 0, 0, BoundsSpecified.Location);
                                pnlCasette1.Visible = true;
                                pnlCasette1.BackColor = Color.Red;
                                pnlCasette2.SetBounds(40, (pnlCasstteStatus.ClientSize.Height - pnlCasette2.Height) / 2, 0, 0, BoundsSpecified.Location);
                                pnlCasette2.Visible = true;
                                pnlCasette2.BackColor = Color.Red;
                                pnlCasette3.SetBounds(70, (pnlCasstteStatus.ClientSize.Height - pnlCasette3.Height) / 2, 0, 0, BoundsSpecified.Location);
                                pnlCasette3.Visible = true;
                                pnlCasette3.BackColor = Color.Red;
                                pnlCasette4.SetBounds(100, (pnlCasstteStatus.ClientSize.Height - pnlCasette4.Height) / 2, 0, 0, BoundsSpecified.Location);
                                pnlCasette4.Visible = true;
                                pnlCasette4.BackColor = Color.Red;
                                pnlCasette5.SetBounds(130, (pnlCasstteStatus.ClientSize.Height - pnlCasette5.Height) / 2, 0, 0, BoundsSpecified.Location);
                                pnlCasette5.Visible = true;
                                pnlCasette5.BackColor = Color.Red;
                                pnlCasette6.SetBounds(160, (pnlCasstteStatus.ClientSize.Height - pnlCasette6.Height) / 2, 0, 0, BoundsSpecified.Location);
                                pnlCasette6.Visible = true;
                                pnlCasette6.BackColor = Color.Red;
                                pnlCasette7.SetBounds(190, (pnlCasstteStatus.ClientSize.Height - pnlCasette7.Height) / 2, 0, 0, BoundsSpecified.Location);
                                pnlCasette7.Visible = true;
                                pnlCasette7.BackColor = Color.Red;
                                pnlCasette8.SetBounds(220, (pnlCasstteStatus.ClientSize.Height - pnlCasette8.Height) / 2, 0, 0, BoundsSpecified.Location);
                                pnlCasette8.Visible = true;
                                pnlCasette8.BackColor = Color.Red;

                            });

                        }
                        logger.Log($"ParseBarcodeMessage {ex.Message} ");

                    }

                }

            });
        }

        public async Task ProcessConfigRequest(string ezResponse)
        {
            await Task.Run(() =>
            {
            });
        }

        public async Task ProcessDownRequest(string ezResponse)
        {
            await Task.Run(() =>
            {
                var lblInitial1 = cdu.Controls.Find("lblInitial1", true).FirstOrDefault();
                var lblInitial2 = cdu.Controls.Find("lblInitial2", true).FirstOrDefault();
                var lblMessage1 = cdu.Controls.Find("lblMessage1", true).FirstOrDefault();
                var lblMessage2 = cdu.Controls.Find("lblMessage2", true).FirstOrDefault();
                var pnlMessage = cdu.Controls.Find("pnlMessage", true).FirstOrDefault();
                cdu.IsDown = true;
                if (lblInitial1 != null && lblInitial1 is Label && lblInitial2 != null && lblInitial2 is Label && lblMessage1 != null && lblMessage1 is Label && lblMessage2 != null && lblMessage2 is Label)
                {
                    cdu.BeginInvoke((Action)delegate ()
                    {
                        lblInitial1.Text = string.Empty;
                        lblInitial2.Text = string.Empty;
                        lblMessage1.Text = "Sorry, Dispenser is temporarily out of service.";
                        lblMessage1.SetBounds((pnlMessage.ClientSize.Width - lblMessage1.Width) / 2, (pnlMessage.ClientSize.Height - lblMessage1.Height) / 2, 0, 0, BoundsSpecified.Location);
                        lblMessage2.Text = string.Empty;
                    });
                }
                ezcashSocket.SendMessage($"0022.000..9");
            });
        }

        public async Task ProcessUpRequest(string ezResponse)
        {
            await Task.Run(() =>
            {
                InitCDU();

                var lblInitial1 = cdu.Controls.Find("lblInitial1", true).FirstOrDefault();
                var lblInitial2 = cdu.Controls.Find("lblInitial2", true).FirstOrDefault();
                var lblMessage1 = cdu.Controls.Find("lblMessage1", true).FirstOrDefault();
                var lblMessage2 = cdu.Controls.Find("lblMessage2", true).FirstOrDefault();
                var pnlInitialize = cdu.Controls.Find("pnlInitialize", true).FirstOrDefault();
                var pnlInitailize2 = cdu.Controls.Find("pnlInitailize2", true).FirstOrDefault();
                var pnlMessage = cdu.Controls.Find("pnlMessage", true).FirstOrDefault();
                cdu.IsDown = false;
                if (lblInitial1 != null && lblInitial1 is Label && lblInitial2 != null && lblInitial2 is Label && lblMessage1 != null && lblMessage1 is Label && lblMessage2 != null && lblMessage2 is Label)
                {
                    Thread initCDU = new Thread(cdu.InitCDU);
                    initCDU.Start();

                    Thread.Sleep(20);

                    Thread changedescription = new Thread(cdu.ChangeDescription);
                    changedescription.Start();

                    //while (true)
                    //{
                    //    if (IsProcessCompleted && SocketConnected)
                    //    {
                    //        cdu.BeginInvoke((Action)delegate ()
                    //        {

                    //            //
                    //            TraceMessage($"Processing: SCREEN =< 1 > LABEL1 =< Welcome to Fujitsu 2 CDU > LABEL2 =< Ready for Processing >");

                    //            lblInitial1.Text = "Welcome to Fujitsu 2 CDU";
                    //            lblInitial2.Text = "Ready for Processing";
                    //            if (cdu.descriptionLoad == "1")
                    //                lblMessage1.Text = "Please scan the Barcode.";
                    //            else if (cdu.descriptionLoad == "2")
                    //                lblMessage1.Text = "Please scan the Barcode.";
                    //            else if (cdu.descriptionLoad == "3")
                    //                lblMessage1.Text = "Waiting for data in.";
                    //            lblMessage1.Font = new Font("Calibri", 20, FontStyle.Italic);

                    //            lblInitial1.SetBounds((pnlInitialize.ClientSize.Width - lblInitial1.Width) / 2, (pnlInitialize.ClientSize.Height - lblInitial1.Height) / 2, 0, 0, BoundsSpecified.Location);
                    //            lblInitial2.SetBounds((pnlInitailize2.ClientSize.Width - lblInitial2.Width) / 2, (pnlInitailize2.ClientSize.Height - lblInitial2.Height) / 2, 0, 0, BoundsSpecified.Location);

                    //            lblMessage1.SetBounds((pnlMessage.ClientSize.Width - lblMessage1.Width) / 2, (pnlMessage.ClientSize.Height - lblMessage1.Height) / 2, 0, 0, BoundsSpecified.Location);

                    //            lblMessage2.Text = string.Empty;
                    //        });
                    //        break;
                    //    }
                    //}

                }
                ezcashSocket.SendMessage($"0022.000..9");
            });
        }

        public async Task ProcessDisconnectRequest(string ezResponse)
        {
            await Task.Run(() =>
            {
            });
        }

    }
}
