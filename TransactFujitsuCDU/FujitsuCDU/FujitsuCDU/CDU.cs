using FujitsuCDU.BarcodeProcess;
using FujitsuCDU.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace FujitsuCDU
{
    public partial class CDU : Form
    {
        #region variables

        Thread initializeThread;
        Thread bgThread;
        public string descriptionLoad;
        private string _barcode = "";
        public BackgroundWorker backgroundWorker1;

        public bool IsDown = false;
        public bool IsDisConnected = false;
        #endregion


        Utilities utilities = new Utilities();
        CRC cRC = new CRC();
        SerialPort commPort;// = new SerialPort("COM7", 9600, Parity.Even, 8, StopBits.One);
        StringBuilder sbreadyMessage = new StringBuilder();
        public EZCashSocket ezcashSocket;
        ErrorCode errorCode = new ErrorCode();
        public System.Timers.Timer timeoutTimer = new System.Timers.Timer(1000 * 4);
        public bool SocketConnected = false;
        public string SuccessDispenseMessage = string.Empty;

        public TcpClient ezCashclient;
        readonly ServiceConfiguration serviceConfiguration = new ServiceConfiguration();
        public NetworkStream clientStream;
        public Thread listenThread;
        public System.Timers.Timer timeoutTrans = new System.Timers.Timer(1000 * 15);
        public string WelcomeScreen1 = string.Empty;
        public string WelcomeScreen2 = string.Empty;
        public bool InitailConnected = false;

        public bool BarcodeReceived = false;
        public bool NoCasstteUpdateonFatal = false;
        public List<int> fatalCassetteList = new List<int>();
        //

        #region Constant
        const string STX = "02";
        const string ETX = "03";
        const string ENQ = "05";
        const string ACK = "06";
        const string NAK = "15";
        const string DLE = "10";
        public string[] canRequest = { "30", "B1", "B2", "33", "B4", "35", "36", "B7", "B8", "39" };

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
        public string ValidShortDispenseMessage = string.Empty;
        public string ValidShortDispenseCode = string.Empty;
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

        public string GetFileLocation(string name)
        {
            return ConfigurationManager.AppSettings[name];
        }

        public void BackgroundInitializing()
        {
            try
            {
                Thread cduInit = new Thread(StartCDUInit);
                cduInit.Start();
                Task.Delay(10000);

                Thread changedescription = new Thread(ChangeDescription);
                changedescription.Start();
            }
            catch (Exception ex)
            {
                LogEvents($"Exception at BackgroundInitializing {ex.Message}");
            }
        }

        public void ChangeDescription()
        {
            try
            {


                while (true)
                {
                    if (IsProcessCompleted && SocketConnected)
                    {
                        this.Invoke(new MethodInvoker(delegate
                        {
                            LogEvents($"Processing: SCREEN =< 1 > LABEL1 =< {WelcomeScreen1} > LABEL2 =< {WelcomeScreen2} >");

                            lblInitial1.Text = WelcomeScreen1;
                            lblInitial2.Text = WelcomeScreen2;

                            lblMessage1.Text = descriptionLoad;

                            lblMessage1.Font = new Font("Calibri", 30, FontStyle.Italic);

                            lblInitial1.SetBounds((pnlInitialize.ClientSize.Width - lblInitial1.Width) / 2, (pnlInitialize.ClientSize.Height - lblInitial1.Height) / 2, 0, 0, BoundsSpecified.Location);
                            lblInitial2.SetBounds((pnlInitialize.ClientSize.Width - lblInitial2.Width) / 2, (pnlInitialize.ClientSize.Height - lblInitial2.Height) / 2, 0, 0, BoundsSpecified.Location);

                            lblMessage1.SetBounds((pnlMessage.ClientSize.Width - lblMessage1.Width) / 2, (pnlMessage.ClientSize.Height - lblMessage1.Height) / 2, 0, 0, BoundsSpecified.Location);


                        }));


                        bgThread = new Thread(() => DisplayCassetteStatus());
                        bgThread.Start();

                        Thread th = new Thread(() =>
                        {
                            while (true)
                            {
                                if (ezCashclient != null && !ezCashclient.Connected)
                                {
                                    this.Invoke(new MethodInvoker(delegate
                                    {
                                        lblMessage1.Text = "Sorry, Dispenser is temporarily out of service.";
                                        lblMessage1.SetBounds((pnlMessage.ClientSize.Width - lblMessage1.Width) / 2, (pnlMessage.ClientSize.Height - lblMessage1.Height) / 2, 0, 0, BoundsSpecified.Location);
                                        lblMessage2.Text = string.Empty;

                                    }));
                                    StartCDUInit();

                                }
                                break;
                            }


                        });
                        th.IsBackground = true;
                        th.Start();

                        break;
                    }
                    else
                    {
                        Thread.Sleep(5000);
                    }
                }
            }
            catch (Exception ex)
            {
                LogEvents($"Exception at ChangeDescription {ex.Message}");
            }

        }

        private void StartCDUInit()
        {
            try
            {
                if (fatalCassetteList != null)
                    fatalCassetteList.Clear();

                if (!InitailConnected)
                {
                    if (InvokeRequired)
                    {
                        this.Invoke(new MethodInvoker(delegate
                        {
                            lblInitial1.Text = string.Empty;

                            lblInitial2.Text = string.Empty;
                            lblMessage1.Text = "Initializing Dispenser...";
                            lblMessage1.Font = new Font("Calibri", 30, FontStyle.Regular);
                            lblMessage1.SetBounds((pnlMessage.ClientSize.Width - lblMessage1.Width) / 2, (pnlMessage.ClientSize.Height - lblMessage1.Height) / 2, 0, 0, BoundsSpecified.Location);
                            lblMessage2.Text = string.Empty;

                        }));
                    }
                }

                InitailConnected = true;
                while (!SocketConnected)
                {

                    if (CreateEZCashSocket())
                    {
                        SocketConnected = true;
                        LogEvents($"EZCash Socket Connected.");
                        IsProcessCompleted = false;
                        Thread initCDU = new Thread(InitCDU);
                        initCDU.Start();
                        break;

                    }
                    else
                    {
                        DisplayErrorMessage();
                        Thread.Sleep(5000);
                        //Thread initializeThread = new Thread(() => BackgroundInitializing());
                        //initializeThread.Start();

                        Task.Factory.StartNew(() => BackgroundInitializing());
                        break;

                    }

                }


            }
            catch (Exception ex)
            {
                LogEvents($"Exception at StartCDUInit{ex.Message}");
                DisplayErrorMessage();

            }
        }

        public void InitCDU()
        {
            try
            {
                int tryCount = 0;
                while (!IsProcessCompleted)
                {

                    if (SocketConnected)
                    {
                        this.Invoke(new MethodInvoker(delegate
                        {
                            lblInitial1.Text = string.Empty;
                            lblInitial2.Text = string.Empty;
                            lblMessage1.Text = "Initializing Dispenser...";
                            lblMessage1.SetBounds((pnlMessage.ClientSize.Width - lblMessage1.Width) / 2, (pnlMessage.ClientSize.Height - lblMessage1.Height) / 2, 0, 0, BoundsSpecified.Location);
                            lblMessage2.Text = string.Empty;

                        }));

                        FujitsuProcessorInitCDU();
                        Thread.Sleep(15000);
                        if (IsProcessCompleted)
                        {
                            break;
                        }
                        tryCount++;
                    }
                    else
                        break;

                    if (tryCount >= 3)
                    {
                        DisplayErrorMessage();
                        this.Invoke(new MethodInvoker(delegate
                        {
                            lblInitial1.Text = string.Empty;
                            lblInitial2.Text = string.Empty;
                            lblMessage2.Text = "Timeout error while trying to initiate the cdu.";
                            lblMessage2.Font = new Font("Calibri", 15, FontStyle.Regular);
                            lblMessage2.SetBounds((pnlMessage2.ClientSize.Width - lblMessage2.Width) / 2, ((pnlMessage2.ClientSize.Height - lblMessage2.Height) / 2) - 30, 0, 0, BoundsSpecified.Location);

                        }));

                        break;
                    }

                }

                // IsProcessCompleted = false;

            }
            catch (Exception ex)
            {

                LogEvents($"Exception at Transact.InitCDU {ex.Message}");
            }

        }

        private void DisplayCassetteStatus()
        {
            try
            {
                LogEvents($"Getting Cassette status from EZcash Service.");
                if (ezCashclient != null && ezCashclient.Connected)
                {
                    LogEvents($"Client socket connected .");
                    this.Invoke(new MethodInvoker(delegate
                    {
                        pnlCasstteStatus.Visible = true;
                    }));

                    SendSocketMessage($"0032.000..9");

                }
                else
                {
                    LogEvents($"Client socket not connected.");
                }
            }
            catch (Exception ex)
            {

                LogEvents($"Exception at StartCDUInit{ex.Message}");
            }
        }

        private void DisplayErrorMessage()
        {
            this.Invoke(new MethodInvoker(delegate
            {

                lblInitial1.Text = string.Empty;
                lblInitial2.Text = string.Empty;

                lblMessage1.Text = "Sorry, Dispenser is temporarily out of service.";

                lblMessage1.SetBounds((pnlMessage.ClientSize.Width - lblMessage1.Width) / 2, (pnlMessage.ClientSize.Height - lblMessage1.Height) / 2, 0, 0, BoundsSpecified.Location);

                lblInitial1.Text = string.Empty;
                lblInitial2.Text = string.Empty;
                lblMessage2.Text = string.Empty;
            }));

        }

        private void LoadScreen(int ScreenNumber, string message)
        {
            try
            {
                switch (ScreenNumber)
                {

                    case 1:
                        this.Invoke(new MethodInvoker(delegate
                        {
                            lblMessage1.Text = message;// "CDU is Ready for dispensing.";
                            lblMessage1.Font = new Font("Calibri", 35, FontStyle.Regular);
                            lblInitial1.SetBounds((pnlInitialize.ClientSize.Width - lblInitial1.Width) / 2, (pnlInitialize.ClientSize.Height - lblInitial1.Height) / 2, 0, 0, BoundsSpecified.Location);

                            lblMessage1.SetBounds((pnlMessage.ClientSize.Width - lblMessage1.Width) / 2, (pnlMessage.ClientSize.Height - lblMessage1.Height) / 2, 0, 0, BoundsSpecified.Location);


                        }));
                        return;
                    case 2:
                        this.Invoke(new MethodInvoker(delegate
                        {
                            lblMessage1.Text = message;// "Dispensing $4000.00 out of $4500.00";
                            lblMessage1.Font = new Font("Calibri", 35, FontStyle.Regular);
                            lblInitial1.SetBounds((pnlInitialize.ClientSize.Width - lblInitial1.Width) / 2, (pnlInitialize.ClientSize.Height - lblInitial1.Height) / 2, 0, 0, BoundsSpecified.Location);

                            lblMessage1.SetBounds((pnlMessage.ClientSize.Width - lblMessage1.Width) / 2, (pnlMessage.ClientSize.Height - lblMessage1.Height) / 2, 0, 0, BoundsSpecified.Location);



                        }));
                        return;
                    case 3:
                        this.Invoke(new MethodInvoker(delegate
                        {
                            lblMessage1.Text = message;// "Dispensed $4500.00 out of $4500.00";
                            lblMessage1.Font = new Font("Calibri", 35, FontStyle.Regular);
                            lblInitial1.SetBounds((pnlInitialize.ClientSize.Width - lblInitial1.Width) / 2, (pnlInitialize.ClientSize.Height - lblInitial1.Height) / 2, 0, 0, BoundsSpecified.Location);

                            lblMessage1.SetBounds((pnlMessage.ClientSize.Width - lblMessage1.Width) / 2, (pnlMessage.ClientSize.Height - lblMessage1.Height) / 2, 0, 0, BoundsSpecified.Location);



                        }));
                        return;
                    default:
                        this.Invoke(new MethodInvoker(delegate
                        {
                            lblMessage1.Text = message;// "Waiting for data in...";
                            lblMessage1.Font = new Font("Calibri", 20, FontStyle.Regular);
                            lblInitial1.SetBounds((pnlInitialize.ClientSize.Width - lblInitial1.Width) / 2, (pnlInitialize.ClientSize.Height - lblInitial1.Height) / 2, 0, 0, BoundsSpecified.Location);

                            lblMessage1.SetBounds((pnlMessage.ClientSize.Width - lblMessage1.Width) / 2, (pnlMessage.ClientSize.Height - lblMessage1.Height) / 2, 0, 0, BoundsSpecified.Location);



                        }));
                        return;
                }
            }
            catch (Exception ex)
            {
                LogEvents($"Exception at LoadScreen {ex.Message}");
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {

            try
            {
                char c = (char)keyData;

                if (char.IsNumber(c))
                    _barcode += c;

                if (c == (char)Keys.Return)
                {
                    if (!IsDown && !IsDisConnected && !BarcodeReceived)
                    {
                        LogEvents($"Barcode Received : {_barcode}");
                        BarcodeReceived = true;
                        NoCasstteUpdateonFatal = false;
                        fatalCassetteList.Clear();
                        Thread cduDispense = new Thread(() => ProcessBarcodeAndDispense(_barcode));
                        cduDispense.Start();
                        cduDispense.Join();
                        _barcode = "";
                    }
                }
            }
            catch (Exception ex)
            {
                LogEvents($"Exception at ProcessCmdKey {ex.Message}");
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private async void ProcessBarcodeAndDispense(string barcode)
        {
            try
            {
                Thread thread = new Thread(() =>
                {
                    this.Invoke(new MethodInvoker(delegate
                    {

                        lblInitial1.Text = "Dispensing Cash.";
                        lblInitial2.Text = "Please wait...";
                        lblMessage1.Text = string.Empty;

                        lblInitial1.SetBounds((pnlInitialize.ClientSize.Width - lblInitial1.Width) / 2, (pnlInitialize.ClientSize.Height - lblInitial1.Height) / 2, 0, 0, BoundsSpecified.Location);
                        lblInitial2.SetBounds((pnlInitailize2.ClientSize.Width - lblInitial2.Width) / 2, (pnlInitailize2.ClientSize.Height - lblInitial2.Height) / 2, 0, 0, BoundsSpecified.Location);

                    }));

                });
                thread.Start();

                timeoutTrans.Start();
                timeoutTrans.Enabled = true;

                await ProcessBarcode(barcode);

            }
            catch (Exception ex)
            {
                LogEvents($"Exception at ProcessBarcodeAndDispense {ex.Message}");
            }
        }

        ~CDU()
        {
            try
            {
                if (ezCashclient != null)
                {
                    if (ezCashclient.Client.Connected)
                        ezCashclient.Client.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                    ezCashclient.Client.Close();
                    ezCashclient.Client.Dispose();
                    ezCashclient = null;
                }
                Thread.Sleep(5000);

                if (initializeThread != null)
                    initializeThread.Abort(50);
                if (bgThread != null)
                    bgThread.Abort(50);


                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                LogEvents($"Exception at CDU destruction : {ex.Message}");
            }

        }


        public CDU()
        {
            InitializeComponent();

            LogEvents($"Launching CDU Application.....");

            this.MinimumSize = new Size(1500, 950);
            this.MaximumSize = new Size(1500, 950);
            descriptionLoad = GetFileLocation("DescriptionLoad");

            lblInitial1.SetBounds((pnlInitialize.ClientSize.Width - lblInitial1.Width) / 2, (pnlInitialize.ClientSize.Height - lblInitial1.Height) / 2, 0, 0, BoundsSpecified.Location);

            lblInitial2.SetBounds((pnlInitailize2.ClientSize.Width - lblInitial2.Width) / 2, ((pnlInitailize2.ClientSize.Height - lblInitial2.Height) / 2) - 20, 0, 0, BoundsSpecified.Location);

            lblMessage1.SetBounds((pnlMessage.ClientSize.Width - lblMessage1.Width) / 2, (pnlMessage.ClientSize.Height - lblMessage1.Height) / 2, 0, 0, BoundsSpecified.Location);

            lblMessage2.SetBounds((pnlMessage2.ClientSize.Width - lblMessage2.Width) / 2, (pnlMessage2.ClientSize.Height - lblMessage2.Height) / 2, 0, 0, BoundsSpecified.Location);

        }


        private void CDU_Load(object sender, EventArgs e)
        {
            try
            {

                timeoutTrans.Elapsed += new ElapsedEventHandler(TimeOutTransaction);

                lblInitial1.SetBounds((pnlInitialize.ClientSize.Width - lblInitial1.Width) / 2, (pnlInitialize.ClientSize.Height - lblInitial1.Height) / 2, 0, 0, BoundsSpecified.Location);

                lblInitial2.SetBounds((pnlInitailize2.ClientSize.Width - lblInitial2.Width) / 2, ((pnlInitailize2.ClientSize.Height - lblInitial2.Height) / 2) - 20, 0, 0, BoundsSpecified.Location);

                lblMessage1.SetBounds((pnlMessage.ClientSize.Width - lblMessage1.Width) / 2, (pnlMessage.ClientSize.Height - lblMessage1.Height) / 2, 0, 0, BoundsSpecified.Location);

                lblMessage2.SetBounds((pnlMessage2.ClientSize.Width - lblMessage2.Width) / 2, (pnlMessage2.ClientSize.Height - lblMessage2.Height) / 2, 0, 0, BoundsSpecified.Location);


                //
                var comPort = GetFileLocation("ComPort");
                var baudRate = int.Parse(GetFileLocation("BaudRate"));
                var dataBit = int.Parse(GetFileLocation("DataBit"));
                var parity = GetFileLocation("Parity").Equals("Even") ? Parity.Even : Parity.None;
                var stopBits = GetFileLocation("StopBits").Equals("One") ? StopBits.One : StopBits.None;
                commPort = new SerialPort(comPort, baudRate, parity, dataBit, stopBits)
                {
                    ReceivedBytesThreshold = 1,
                    Encoding = Encoding.ASCII
                };
                commPort.DataReceived += new SerialDataReceivedEventHandler(OnDataReceived);

                timeoutTimer.Elapsed += new ElapsedEventHandler(TimeOutEvent);
                timeoutTimer.Enabled = false;
                OpenPort();
                //

                initializeThread = new Thread(() => BackgroundInitializing());
                initializeThread.Start();
            }
            catch (Exception ex)
            {
                LogEvents($"Exception at CDU_Load {ex.Message}");
            }
        }

        private void CDU_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                LogEvents("Cleaning up resource allocation.");

                if (initializeThread != null)
                    initializeThread.Abort(50);
                if (bgThread != null)
                    bgThread.Abort(50);


                if (initializeThread != null)
                    initializeThread.Abort(50);
                if (bgThread != null)
                    bgThread.Abort(50);
                LogEvents("CDU Application closed.");

                Task.Delay(30000);

                foreach (var process in Process.GetProcessesByName("FujitsuCDU"))
                {
                    LogEvents("Cleaning up resource allocation and its usuage is completed");
                    Task.Delay(30000);
                    process.Kill();
                }

            }
            catch (Exception ex)
            {
                LogEvents($"Exception at CDU_FormClosing{ex.Message}");
            }

        }


        public void SendMessage(string message)
        {
            try
            {
                LogEvents("Entered SendStr");
                if (!commPort.IsOpen)
                {
                    LogEvents("Opening Comm Port.");
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
                LogEvents($"Exception at SendMessage : {ex.Message}");
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

                LogEvents($"Sent on port: ");
                utilities.SplitBytes(sendValue, 16); // Write detailed information into Log file.
                Thread.Sleep(200);

                //Trace..
            }
            catch (Exception ex)
            {
                LogEvents($"Exception at MyWriteStr :{ex.Message}");
            }

        }

        public void DispenseAmount(string amount, string originalAmount, string[] dispenseMessage)
        {
            try
            {
                LogEvents("Entered DispenseAmount");
                LogEvents($"Loading screen : Dispensing ${amount} of ${originalAmount} ");
                DisplayDescription(3, "", 25, "", 25, $"Dispensing ${amount} of ${originalAmount}", 25, "", 25);

                CanRetry = false;
                // Sample message:
                // ------------------ODR-------- N1--- N2--- N3--- N4--- R1--- R2--- R3--- R4--- P1 P2 P3 P4 N5--- N6--- N7--- N8--- R5--- R6--- R7--- R8--- P5 P6 P7 P8 FS
                // 60 03 ff 00 00 2c fe dc ba 98 30 b1 30 30 30 30 30 30 b1 30 30 30 30 30 30 30 15 00 00 00 30 30 30 30 30 30 30 30 30 30 30 30 30 30 30 30 00 00 00 00 1c

                //var msg = string.Empty;
                StringBuilder sb = new StringBuilder();

                sb.Append("6003ff00002cfedcba98");//DH0(60),DH1(03),DH2(FF), RSV1(00) , DH3(002C) ,0DR(fedcba98)


                for (int i = 0; i < 4; i++)
                {
                    var divval = int.Parse(dispenseMessage[i]) / 10;
                    var modVal = int.Parse(dispenseMessage[i]) % 10;
                    sb.Append(canRequest[divval]); //CChar[Integer(Cannisters[i].Dispensed div 10)];
                    sb.Append(canRequest[modVal]); //CChar[Integer(Cannisters[i].Dispensed mod 10)];
                }

                sb.Append("b130b130b130b13015151515");// R1,R2,R3,R4,P1,P2,P3,P4

                for (int i = 4; i < 6; i++)
                {
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


            }
            catch (Exception ex)
            {
                LogEvents(ex.Message);
            }
        }

        public void FujitsuProcessorInitCDU()
        {
            try
            {
                if (BarcodeReceived)
                {
                    DisplayDescription(4, "", 0, "", 0, "", 0, "Please wait. We are trying to dispense with different combination", 30);
                }
                LogEvents("Entered InitCDU");
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
            }
            catch (Exception ex)
            {
                LogEvents($"Exception at InitCDU :{ex.Message}");
            }

        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs args)
        {
            try
            {
                LogEvents("Entered OnDataReceived");
                var serialPort = (SerialPort)sender;
                Thread.Sleep(500);


                //Bytes reading starts ..
                int intBuffer;
                intBuffer = serialPort.BytesToRead;
                byte[] byteBuffer = new byte[intBuffer];
                serialPort.Read(byteBuffer, 0, intBuffer); // Read all bytes.
                if (intBuffer > 0)
                {
                    LogEvents($"Received on port: ");
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
                                LogEvents("Received ACK after ENQ. Sending message:");// + CRLF + HexDump(MsgWaiting, 4));
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
                                LogEvents("Sent message was NAK''ed. Resending...");
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

                                        LogEvents($"Processing ");
                                        var receivedBytes = utilities.GetSendBytes(S);
                                        utilities.SplitBytes(receivedBytes, 16); // Write detailed information into Log file.
                                        var crcChecksumByte = new byte[2];

                                        cRC.FujiCRC(ref receivedBytes, receivedBytes.Length, ref crcChecksumByte);

                                        CRC1 = BitConverter.ToString(crcChecksumByte).Replace("-", string.Empty);
                                        ReceivedCRC = receivedMessage.Substring(receivedMessage.Length - 4, 4);
                                        receivedMessage = string.Empty;

                                        LogEvents($"Received CRC {ReceivedCRC}");
                                        LogEvents($"Calculated CRC {CRC1}");

                                        if (CRC1.Equals(ReceivedCRC))
                                        {
                                            LogEvents($"Received CRC matches calculated CRC.");
                                            IsProcessCompleted = true;

                                            MyWriteStr(DLE + ACK);
                                            ProcessMsg(S.Substring(4, S.Length - 8), byteBuffer.Skip(4).ToArray());
                                        }
                                        else
                                        {
                                            LogEvents("Received message CRC mismatch; sending NAK...");
                                            MyWriteStr(DLE + NAK);
                                        }

                                    }
                                }
                                catch (Exception ex)
                                {
                                    LogEvents($"Exception at Ready State :{ex.Message}");
                                    receivedMessage = string.Empty;
                                }

                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogEvents($"Exception at OnDataReceived :{ex.Message}");
            }

        }

        private void SendNow()
        {
            try
            {
                LogEvents("Entered SendNow");
                var sendValue = utilities.GetSendBytes(MsgWaiting);
                StringBuilder Msg = new StringBuilder();
                var Length = sendValue.Length;
                LogEvents($"Length of the message is {Length}");
                Msg.Append((Length / 256).ToString("X").Length < 2 ? "0" + (Length / 256).ToString("X") : (Length / 256).ToString("X"));
                Msg.Append((Length % 256).ToString("X").Length < 2 ? "0" + (Length % 256).ToString("X") : (Length % 256).ToString("X"));// + MsgWaiting + DLE + ETX);
                Msg.Append(MsgWaiting);
                Msg.Append(DLE + ETX);
                var crcSourceBytes = utilities.GetSendBytes(Msg.ToString());
                var crcChecksumByte = new byte[2];

                cRC.FujiCRC(ref crcSourceBytes, crcSourceBytes.Length, ref crcChecksumByte);

                var hexString = BitConverter.ToString(crcChecksumByte).Replace("-", string.Empty);

                LogEvents($"Calculated checksum is {hexString}");
                Msg.Insert(0, STX);
                Msg.Insert(0, DLE);
                Msg.Append(hexString);
                LogEvents($"Adding CRC {hexString}");
                CommState = TCommState.csMsgSent;
                MyWriteStr(Msg.ToString());
            }
            catch (Exception ex)
            {
                LogEvents($"Exception at SendNow :{ex.Message}");
            }

        }

        private void ProcessMsg(string message, byte[] messagebyte)
        {
            try
            {
                LogEvents("Entered Fujitsu.ProcessMsg");
                TDevState MYState;
                TCommonResp R = new TCommonResp();

                MYState = (TDevState)State;

                State = TState.stReady;
                LogEvents($"Back from changing state to Ready.");

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
                        LogEvents($"Dispense Completed");
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
                LogEvents($" Error at ProcessMsg : {ex.Message}");
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


                if (message.Substring(0, 1).ToUpper() == "F") //If the dispense fails, reject the notes and void. Else proceed with delivering the note.
                {
                    ErrorCodeReceived = true;
                    var errorcode = message.Substring(12, 4).Trim().ToUpper();
                    LogEvents($"Error code received : {errorcode}");

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
                                        fatalCassetteList.Add(1);
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
                                        fatalCassetteList.Add(2);
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
                                        fatalCassetteList.Add(3);
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
                                        fatalCassetteList.Add(4);
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
                                        fatalCassetteList.Add(5);
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
                                        fatalCassetteList.Add(6);
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
                        NoCasstteUpdateonFatal = true;
                        LogEvents($"Message to EZCash socket : {Socketerrorcode}");
                    }
                    else
                    {
                        errorState = TErrorState.stOtherError;
                    }

                    AmountDispensed = 0;
                    State = TState.stWaitRejectInit;
                    FujitsuProcessorInitCDU();
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
                        LogEvents($"Cassette {count} dispensed {item[1]}{item[3]} bills.");
                        dispensedMessage += item[1].ToString() + item[3].ToString();
                        count++;
                    }
                    count = 5;
                    foreach (var item in canDetails5to8.SplitInParts(4))
                    {
                        LogEvents($"Cassette {count} dispensed {item[1]}{item[3]} bills.");
                        dispensedMessage += item[1].ToString() + item[3].ToString();
                        count++;
                    }

                    LogEvents($"Total Dispensed bill details : {dispensedMessage}");
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
                LogEvents($"Exception at ProcessDispResponse :{ex.Message}");
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
                            LogEvents($"Error code received , sending {Socketerrorcode} to EZcash socket");
                            SendSocketMessage(Socketerrorcode);
                            break;
                        case TErrorState.stOtherError:
                            DispenseAmount(Convert.ToString(DispensingAmount), Convert.ToString(TotalAmount), DispensingMessage.SplitInParts(2).ToArray());
                            break;
                    }

                }
                if (work.Contains("FF000064"))
                {

                    var cassetteRegister1 = work.Substring(42, 8).Trim().ToUpper();
                    var cassetteRegister2 = work.Substring(138, 8).Trim().ToUpper();
                    var canStatus = new StringBuilder();
                    var cassetteCount = 1;
                    foreach (var cassette in cassetteRegister1.SplitInParts(2))
                    {
                        foreach (var item in cassette)
                        {
                            if (fatalCassetteList != null && fatalCassetteList.Contains(cassetteCount))
                            {
                                canStatus.Append("4");
                            }
                            else
                            {
                                if (item.ToString() == "8")
                                {
                                    canStatus.Append("2");
                                }
                                else if (item.ToString() == "9")
                                {
                                    canStatus.Append("4");
                                }
                                else
                                {
                                    canStatus.Append("0");
                                }
                            }


                            break;
                        }
                        cassetteCount++;
                    }

                    foreach (var cassette in cassetteRegister2.SplitInParts(2))
                    {
                        foreach (var item in cassette)
                        {
                            if (fatalCassetteList != null && fatalCassetteList.Contains(cassetteCount))
                            {
                                canStatus.Append("4");
                            }
                            else
                            {

                                if (item.ToString() == "8")
                                {
                                    canStatus.Append("2");
                                }
                                else if (item.ToString() == "9")
                                {
                                    canStatus.Append("4");
                                }
                                else
                                {
                                    canStatus.Append("0");
                                }
                            }
                            break;
                        }
                        cassetteCount++;
                    }
                    SendSocketMessage($"0031.{canStatus}..9");
                    LogEvents($"Message to EZCash socket : {Socketerrorcode}");
                }


            }
            catch (Exception ex)
            {
                LogEvents($" Exception at ProcessInitResponse : {ex.Message}");
            }
        }

        private void OnDispenseCompleted(string message, byte[] messageByte)
        {
            try
            {
                // Todo for Coin dispensing message
                if (TotalAmount != DispensingAmount)
                {
                    if (ValidShortDispenseMessage.ToLower().Contains("rescan"))
                    {

                        LogEvents($"Processing  : LABEL1=<{ValidShortDispenseMessage}>Label2=<Transaction completed.>");
                        DisplayDescription(1, "", 10, "", 20, "", 25, "", 20);
                        DisplayDescription(2, "", 10, "", 20, "", 25, "", 20);
                        DisplayDescription(3, "", 20, "", 20, ValidShortDispenseMessage, 25, "", 20);
                        DisplayDescription(4, "", 20, "", 20, "", 0, "Transaction completed.", 25);

                        LogEvents($"Sending EZCash socket response on short/multibundle dispense");
                        SendSocketMessage($"0022.000..9");
                        Thread.Sleep(2000);
                        UpdateCassetteStatus();
                        Thread.Sleep(2000);
                        timeoutTimer.Enabled = true;
                        timeoutTimer.Start();
                        BarcodeReceived = false;
                    }
                    else
                    {
                        LogEvents($"Processing  : LABEL3=<Dispensing ${TotalAmount} of ${TotalAmount}>");

                        DisplayDescription(3, string.Empty, 0, string.Empty, 0, $"Dispensing ${TotalAmount} of ${TotalAmount}", 25, string.Empty, 0);
                        // DisplayDescription(4, string.Empty, 0, string.Empty, 0, string.Empty, 0, "Thank you.", 25);
                    }

                }
                else
                {
                    DisplayTransactionCompleteScreen();
                }

                LogEvents($"Sending EZCash socket response on cash dispense completed.");
                SendSocketMessage($"0022.000..9");
                Thread.Sleep(2000);
                UpdateCassetteStatus();
                //

            }
            catch (Exception ex)
            {
                LogEvents($" Exception at OnDispenseCompleted : {ex.Message}");
            }
        }

        private void DisplayTransactionCompleteScreen()
        {
            LogEvents($"Processing  : LABEL1=<Transaction complete.>Label2=<Thank you.>");

            DisplayDescription(1, string.Empty, 10, string.Empty, 0, "", 25, string.Empty, 0);
            DisplayDescription(2, string.Empty, 0, string.Empty, 10, "", 25, string.Empty, 0);
            DisplayDescription(3, string.Empty, 0, string.Empty, 0, "Transaction complete.", 25, string.Empty, 0);
            DisplayDescription(4, string.Empty, 0, string.Empty, 0, string.Empty, 0, "Thank you.", 25);

            timeoutTimer.Enabled = true;
            timeoutTimer.Start();

            BarcodeReceived = false;
        }

        public void UpdateCassetteStatus()
        {
            if (string.IsNullOrEmpty(SuccessDispenseMessage))
                return;
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

            //SuccessDispenseMessage = string.Empty;
            SendSocketMessage($"0031.{cassetteStatus}..9");

        }

        private void OpenPort()
        {
            try
            {
                LogEvents("Opening port...");
                if (!commPort.IsOpen)
                {
                    commPort.Open();
                    CommState = TCommState.csReady;
                    Thread.Sleep(500);
                    LogEvents("Connected to Monitor port.");

                }
            }
            catch (Exception ex)
            {
                LogEvents($"Exception at OpenPort :{ex.Message} .Attempting to reconnect ...");
            }
        }

        private void DeliverAndWait()
        {
            try
            {
                LogEvents($"Processing  : LABEL1=<Dispensed ${DispensingAmount} of ${TotalAmount}>Label2=<Please take your cash>");

                DisplayDescription(3, "", 0, "", 0, $"Dispensed ${DispensingAmount} of ${TotalAmount}", 25, "", 0);
                DisplayDescription(4, "", 0, "", 0, "", 0, "Please take your cash", 20);

                SendMessage(RequestFrame.DeliverCash);

            }
            catch (Exception ex)
            {
                LogEvents($"Exception at DeliverAndWait :{ex.Message}.");
            }
        }

        private void RejectNote()
        {
            try
            {
                LogEvents("Connected to Monitor port.");
                State = TState.stWaitReject;
                SendMessage(RequestFrame.Reject);

            }
            catch (Exception ex)
            {
                LogEvents($"Exception at RejectNote :{ex.Message}.");
            }
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

                LogEvents($"Processing ");
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
            switch (ScreenNo)
            {
                case 1:
                    var lblInitial1 = Controls.Find("lblInitial1", true).FirstOrDefault();
                    var pnlInitialize = Controls.Find("pnlInitialize", true).FirstOrDefault();

                    if (null != lblInitial1 && lblInitial1 is Label && null != pnlInitialize && pnlInitialize is Panel)
                    {

                        BeginInvoke((Action)delegate ()
                        {
                            (lblInitial1 as Label).Text = string.Empty;
                            (lblInitial1 as Label).Text = message;
                            (lblInitial1 as Label).Font = new Font("Calibri", size1, FontStyle.Bold);
                            (lblInitial1 as Label).SetBounds(((pnlInitialize as Panel).ClientSize.Width - (lblInitial1 as Label).Width) / 2, ((pnlInitialize as Panel).ClientSize.Height - (lblInitial1 as Label).Height) / 2, 0, 0, BoundsSpecified.Location);

                        });

                    }
                    break;
                case 2:
                    var lblInitial2 = Controls.Find("lblInitial2", true).FirstOrDefault();
                    var pnlInitialize2 = Controls.Find("pnlInitailize2", true).FirstOrDefault();

                    if (null != lblInitial2 && lblInitial2 is Label && null != pnlInitialize2 && pnlInitialize2 is Panel)
                    {

                        BeginInvoke((Action)delegate ()
                        {
                            (lblInitial2 as Label).Text = string.Empty;
                            (lblInitial2 as Label).Text = message2;
                            (lblInitial2 as Label).Font = new Font("Calibri", size2, FontStyle.Regular);
                            (lblInitial2 as Label).SetBounds(((pnlInitialize2 as Panel).ClientSize.Width - (lblInitial2 as Label).Width) / 2, (((pnlInitialize2 as Panel).ClientSize.Height - (lblInitial2 as Label).Height) / 2) - 10, 0, 0, BoundsSpecified.Location);

                        });

                    }
                    break;
                case 3:
                    var lblMessage1 = Controls.Find("lblMessage1", true).FirstOrDefault();
                    var pnlMessage = Controls.Find("pnlMessage", true).FirstOrDefault();
                    var lblMessage21 = Controls.Find("lblMessage2", true).FirstOrDefault();
                    if (null != lblMessage1 && lblMessage1 is Label && null != pnlMessage && pnlMessage is Panel && lblMessage21 != null && lblMessage21 is Label)
                    {

                        BeginInvoke((Action)delegate ()
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
                    var lblMessage2 = Controls.Find("lblMessage2", true).FirstOrDefault();
                    var pnlMessage2 = Controls.Find("pnlMessage2", true).FirstOrDefault();

                    if (null != lblMessage2 && lblMessage2 is Label && null != pnlMessage2 && pnlMessage2 is Panel)
                    {

                        BeginInvoke((Action)delegate ()
                        {
                            (lblMessage2 as Label).Text = string.Empty;
                            (lblMessage2 as Label).Text = message4;
                            (lblMessage2 as Label).Font = new Font("Calibri", size4, FontStyle.Regular);
                            (lblMessage2 as Label).SetBounds(((pnlMessage2 as Panel).ClientSize.Width - (lblMessage2 as Label).Width) / 2, (((pnlMessage2 as Panel).ClientSize.Height - (lblMessage2 as Label).Height) / 2) - 30, 0, 0, BoundsSpecified.Location);

                        });

                    }
                    break;
                case 5:
                    var Initial1 = Controls.Find("lblInitial1", true).FirstOrDefault();
                    var Initial2 = Controls.Find("lblInitial2", true).FirstOrDefault();
                    var Message1 = Controls.Find("lblMessage1", true).FirstOrDefault();
                    var Message2 = Controls.Find("lblMessage2", true).FirstOrDefault();
                    var pnlMessage1 = Controls.Find("pnlMessage", true).FirstOrDefault();

                    if (null != Initial1 && Initial1 is Label && null != Initial2 && Initial2 is Label && null != Message1 && Message1 is Label && null != Message2 && Message2 is Label && null != pnlMessage1 && pnlMessage1 is Panel)
                    {

                        BeginInvoke((Action)delegate ()
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

        }

        private void TimeOutEvent(object source, ElapsedEventArgs e)
        {
            DisplayDescription(5, "", 0, "", 0, "Please scan the barcode", 25, "", 0);
            DisplayDescription(1, WelcomeScreen1, 50, "", 0, "", 25, "", 0);
            DisplayDescription(2, "", 0, WelcomeScreen2, 30, "", 25, "", 0);
            timeoutTimer.Stop();
            timeoutTimer.Enabled = false;
            IsProcessCompleted = true;
        }

        private void TimeOutTransaction(object source, ElapsedEventArgs e)
        {
            try
            {
                if (!IsProcessCompleted)
                {
                    LogEvents($"Processing  : LABEL1=<{"Invalid Card"}>Label2=<Transaction cancelled.>");
                    var lblInitial1 = Controls.Find("lblInitial1", true).FirstOrDefault();
                    var lblInitial2 = Controls.Find("lblInitial2", true).FirstOrDefault();

                    BeginInvoke((Action)delegate ()
                    {
                        (lblInitial1 as Label).Text = string.Empty;
                    });
                    BeginInvoke((Action)delegate ()
                    {
                        (lblInitial2 as Label).Text = string.Empty;
                    });

                    DisplayDescription(3, "", 20, "", 20, "Invalid Card", 25, "", 25);
                    DisplayDescription(4, "", 20, "", 20, "", 0, "Transaction cancelled.", 25);

                    timeoutTrans.Stop();
                    timeoutTrans.Enabled = false;

                    timeoutTimer.Enabled = true;
                    timeoutTimer.Start();
                }
            }
            catch (Exception ex)
            {
                LogEvents($"Exception at TimeOutTransaction {ex.Message}");
            }
        }

        private void TimeOutTransactionForCoins()
        {
            try
            {
                LogEvents($"Processing  : LABEL1=<{"Transaction timed out"}>Label2=<Transaction cancelled>");

                DisplayDescription(1, string.Empty, 10, string.Empty, 0, "", 25, string.Empty, 0);
                DisplayDescription(2, string.Empty, 0, string.Empty, 10, "", 25, string.Empty, 0);
                DisplayDescription(3, "", 20, "", 20, "Transaction timed out", 25, "", 25);
                DisplayDescription(4, "", 20, "", 20, "", 0, "Transaction cancelled.", 25);

                timeoutTimer.Enabled = true;
                timeoutTimer.Start();

            }
            catch (Exception ex)
            {
                LogEvents($"Exception at TimeOutTransactionForCoins {ex.Message}");
            }
        }
        public async Task ProcessBarcode(string barcode)
        {
            await Task.Run(() =>
            {
                try
                {
                    LogEvents($"Sending Socket transaction request {barcode}..");
                    CommState = TCommState.csReady;
                    IsDispenseReqSent = true;
                    State = TState.stWaitTranReply;
                    SendSocketMessage($"1111.000...1 > .{barcode}..ABC....");

                }
                catch (Exception ex)
                {
                    LogEvents($"ProcessBarcode {ex.Message} ");
                }
            });
        }

        public async Task ProcessBarcodeTransaction(string ezResponse)
        {
            await Task.Run(() =>
            {
                try
                {
                    var dispenseMessage = ezResponse.Split('.')[4].Replace("\u001d", "");
                    DispensingMessage = dispenseMessage;

                    LogEvents($"Received Socket Dispense response : {dispenseMessage}");


                    timeoutTrans.Stop();
                    timeoutTrans.Enabled = false;
                    if (Convert.ToInt64(dispenseMessage) != 0)
                    {
                        var originalAmount = string.Empty;
                        var dispensingAmount = string.Empty;
                        var successMessage = string.Empty;
                        var successCode = string.Empty;
                        if (ezResponse.Split('.').Length > 8)
                        {
                            var deci = ezResponse.Split('.')[7].Replace("\u001d", "").Length == 1 ? ezResponse.Split('.')[7].Replace("\u001d", "") + "0" : ezResponse.Split('.')[7].Replace("\u001d", "");
                            originalAmount = ezResponse.Split('.')[6].Replace("\u001d", "") + '.' + deci;
                            dispensingAmount = ezResponse.Split('.')[5].Replace("\u001d", "");

                        }
                        else
                        {
                            originalAmount = ezResponse.Split('.')[6].Replace("\u001d", "");
                            dispensingAmount = ezResponse.Split('.')[5].Replace("\u001d", "");

                        }

                        try
                        {
                            var tempezResponse = ezResponse.TrimEnd(new Char[] { '.' });
                            ValidShortDispenseMessage = tempezResponse.Split('.')[tempezResponse.Substring(0, tempezResponse.Length - 2).Split('.').Length - 1].Remove(0, 1);
                            ValidShortDispenseCode = tempezResponse.Substring(0, tempezResponse.Length - 2).Split('.')[tempezResponse.Substring(0, tempezResponse.Length - 2).Split('.').Length - 2];
                        }
                        catch (Exception)
                        {

                        }

                        DispenseAmount(dispensingAmount, originalAmount, dispenseMessage.SplitInParts(2).ToArray());
                    }
                    else
                    {
                        ezResponse = ezResponse.TrimEnd(new Char[] { '.' });
                        var message = ezResponse.Split('.')[ezResponse.Substring(0, ezResponse.Length - 2).Split('.').Length - 1].Remove(0, 1);
                        var code = ezResponse.Substring(0, ezResponse.Length - 2).Split('.')[ezResponse.Substring(0, ezResponse.Length - 2).Split('.').Length - 2];


                        LogEvents($"Processing  : LABEL1=<{message}>Label2=<Transaction cancelled.>");
                        DisplayDescription(1, "", 10, "", 20, "", 25, "", 20);
                        DisplayDescription(2, "", 10, "", 20, "", 25, "", 20);
                        DisplayDescription(3, "", 20, "", 20, message, 25, "", 20);
                        DisplayDescription(4, "", 20, "", 20, "", 0, "Transaction cancelled.", 25);

                        LogEvents($"Sending EZCash socket response on no dispense");
                        SendSocketMessage($"0022.000..9");
                        Thread.Sleep(2000);
                        UpdateCassetteStatus();
                        Thread.Sleep(2000);
                        timeoutTimer.Enabled = true;
                        timeoutTimer.Start();
                        BarcodeReceived = false;
                    }

                }
                catch (Exception ex)
                {
                    LogEvents($"ProcessBarcodeTransaction {ex.Message} ");
                    timeoutTrans.Enabled = true;
                    timeoutTrans.Start();


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
                        if (ezResponse.Contains("].."))
                        {
                            ezResponse = ezResponse.Replace("]..", "]");
                        }

                        var denoms = JsonConvert.DeserializeObject<List<DenominationInfo>>(ezResponse);

                        var pnlCasstteStatus = Controls.Find("pnlCasstteStatus", true).FirstOrDefault();

                        foreach (var item in denoms)
                        {
                            switch (item.cassette_nbr)
                            {
                                case "1":

                                    var pnlCasette1 = Controls.Find("pnlCasette1", true).FirstOrDefault();
                                    if (pnlCasette1 != null && pnlCasstteStatus != null && pnlCasette1 is Panel && pnlCasstteStatus is Panel)
                                    {
                                        BeginInvoke((Action)delegate ()
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
                                                case "3":
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
                                    var pnlCasette2 = Controls.Find("pnlCasette2", true).FirstOrDefault();
                                    if (pnlCasette2 != null && pnlCasstteStatus != null && pnlCasette2 is Panel && pnlCasstteStatus is Panel)
                                    {
                                        BeginInvoke((Action)delegate ()
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
                                                case "3":
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
                                    var pnlCasette3 = Controls.Find("pnlCasette3", true).FirstOrDefault();
                                    if (pnlCasette3 != null && pnlCasstteStatus != null && pnlCasette3 is Panel && pnlCasstteStatus is Panel)
                                    {
                                        BeginInvoke((Action)delegate ()
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
                                                case "3":
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
                                    var pnlCasette4 = Controls.Find("pnlCasette4", true).FirstOrDefault();
                                    if (pnlCasette4 != null && pnlCasstteStatus != null && pnlCasette4 is Panel && pnlCasstteStatus is Panel)
                                    {
                                        BeginInvoke((Action)delegate ()
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
                                                case "3":
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
                                    var pnlCasette5 = Controls.Find("pnlCasette5", true).FirstOrDefault();
                                    if (pnlCasette5 != null && pnlCasstteStatus != null && pnlCasette5 is Panel && pnlCasstteStatus is Panel)
                                    {
                                        BeginInvoke((Action)delegate ()
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
                                                case "3":
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
                                    var pnlCasette6 = Controls.Find("pnlCasette6", true).FirstOrDefault();
                                    if (pnlCasette6 != null && pnlCasstteStatus != null && pnlCasette6 is Panel && pnlCasstteStatus is Panel)
                                    {
                                        BeginInvoke((Action)delegate ()
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
                                                case "3":
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
                                    var pnlCasette7 = Controls.Find("pnlCasette7", true).FirstOrDefault();
                                    if (pnlCasette7 != null && pnlCasstteStatus != null && pnlCasette7 is Panel && pnlCasstteStatus is Panel)
                                    {
                                        BeginInvoke((Action)delegate ()
                                        {
                                            pnlCasette7.SetBounds(190, (pnlCasstteStatus.ClientSize.Height - pnlCasette7.Height) / 2, 0, 0, BoundsSpecified.Location);
                                            pnlCasette7.BackColor = Color.Red;
                                            pnlCasette7.Visible = true;

                                        });
                                    }


                                    break;
                                case "8":
                                    var pnlCasette8 = Controls.Find("pnlCasette8", true).FirstOrDefault();
                                    if (pnlCasette8 != null && pnlCasstteStatus != null && pnlCasette8 is Panel && pnlCasstteStatus is Panel)
                                    {
                                        BeginInvoke((Action)delegate ()
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
                        var pnlCasstteStatus = Controls.Find("pnlCasstteStatus", true).FirstOrDefault();
                        var pnlCasette1 = Controls.Find("pnlCasette1", true).FirstOrDefault();
                        var pnlCasette2 = Controls.Find("pnlCasette2", true).FirstOrDefault();
                        var pnlCasette3 = Controls.Find("pnlCasette3", true).FirstOrDefault();
                        var pnlCasette4 = Controls.Find("pnlCasette4", true).FirstOrDefault();
                        var pnlCasette5 = Controls.Find("pnlCasette5", true).FirstOrDefault();
                        var pnlCasette6 = Controls.Find("pnlCasette6", true).FirstOrDefault();
                        var pnlCasette7 = Controls.Find("pnlCasette7", true).FirstOrDefault();
                        var pnlCasette8 = Controls.Find("pnlCasette8", true).FirstOrDefault();

                        if (pnlCasette1 != null && pnlCasette2 != null && pnlCasette3 != null && pnlCasette4 != null && pnlCasette5 != null && pnlCasette6 != null && pnlCasette7 != null && pnlCasette8 != null && pnlCasstteStatus != null && pnlCasette1 is Panel && pnlCasstteStatus is Panel)
                        {
                            BeginInvoke((Action)delegate ()
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
                        LogEvents($"Exception at ProcessCassetteStatus {ex.Message} ");

                    }

                }

                SendSocketMessage($"0022.000..9");
            });
        }

        public async Task ProcessConfigRequest(string ezResponse)
        {
            await Task.Run(() =>
            {
                SendSocketMessage($"0033.000..F.2000000000010000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");
            });
        }

        public async Task ProcessDownRequest(string ezResponse)
        {
            await Task.Run(() =>
            {
                var lblInitial1 = Controls.Find("lblInitial1", true).FirstOrDefault();
                var lblInitial2 = Controls.Find("lblInitial2", true).FirstOrDefault();
                var lblMessage1 = Controls.Find("lblMessage1", true).FirstOrDefault();
                var lblMessage2 = Controls.Find("lblMessage2", true).FirstOrDefault();
                var pnlMessage = Controls.Find("pnlMessage", true).FirstOrDefault();
                IsDown = true;
                if (lblInitial1 != null && lblInitial1 is Label && lblInitial2 != null && lblInitial2 is Label && lblMessage1 != null && lblMessage1 is Label && lblMessage2 != null && lblMessage2 is Label)
                {
                    BeginInvoke((Action)delegate ()
                    {
                        lblInitial1.Text = string.Empty;
                        lblInitial2.Text = string.Empty;
                        lblMessage1.Text = "Sorry, Dispenser is temporarily out of service.";
                        lblMessage1.SetBounds((pnlMessage.ClientSize.Width - lblMessage1.Width) / 2, (pnlMessage.ClientSize.Height - lblMessage1.Height) / 2, 0, 0, BoundsSpecified.Location);
                        lblMessage2.Text = string.Empty;
                    });
                }
                SendSocketMessage($"0022.000..9");
            });
        }

        public async Task ProcessUpRequest(string ezResponse)
        {
            await Task.Run(() =>
            {
                FujitsuProcessorInitCDU();

                var lblInitial1 = Controls.Find("lblInitial1", true).FirstOrDefault();
                var lblInitial2 = Controls.Find("lblInitial2", true).FirstOrDefault();
                var lblMessage1 = Controls.Find("lblMessage1", true).FirstOrDefault();
                var lblMessage2 = Controls.Find("lblMessage2", true).FirstOrDefault();
                var pnlInitialize = Controls.Find("pnlInitialize", true).FirstOrDefault();
                var pnlInitailize2 = Controls.Find("pnlInitailize2", true).FirstOrDefault();
                var pnlMessage = Controls.Find("pnlMessage", true).FirstOrDefault();
                IsDown = false;
                if (lblInitial1 != null && lblInitial1 is Label && lblInitial2 != null && lblInitial2 is Label && lblMessage1 != null && lblMessage1 is Label && lblMessage2 != null && lblMessage2 is Label)
                {
                    IsProcessCompleted = false;
                    Thread initCDU = new Thread(InitCDU);
                    initCDU.Start();

                    Thread.Sleep(20);

                    Thread changedescription = new Thread(ChangeDescription);
                    changedescription.Start();


                }
                SendSocketMessage($"0022.000..9");
            });
        }

        public async Task ProcessDisconnectRequest(string ezResponse)
        {
            await Task.Run(() =>
            {
            });
        }

        public bool CreateEZCashSocket()
        {
            try
            {


                string SERVER_IP = serviceConfiguration.GetFileLocation("EZcashIP");
                string local_IP = serviceConfiguration.GetFileLocation("Ip");

                IPAddress localIP = IPAddress.Parse(local_IP);
                int localPort = Convert.ToInt32(serviceConfiguration.GetFileLocation("Port"));
                IPAddress remoteIP = IPAddress.Parse(SERVER_IP);
                int remotePort = Convert.ToInt32(serviceConfiguration.GetFileLocation("EZcashPort"));
                IPEndPoint remoteEP = new IPEndPoint(remoteIP, remotePort);
                IPEndPoint localEP = new IPEndPoint(localIP, localPort);

                ezCashclient = new TcpClient(localEP);
                ezCashclient.Connect(remoteEP);
                clientStream = ezCashclient.GetStream();

                Thread.Sleep(3000);
                if (ezCashclient != null && ezCashclient.Connected)
                {
                    byte[] bytesToRead = new byte[ezCashclient.ReceiveBufferSize];
                    int bytesRead = clientStream.Read(bytesToRead, 0, ezCashclient.ReceiveBufferSize);
                    var ezCashResponse = utilities.ByteToHexaEZCash(bytesToRead.Skip(2).Take(bytesRead).ToArray(), bytesRead).Split(',');
                    WelcomeScreen1 = ezCashResponse[0];
                    WelcomeScreen2 = ezCashResponse[1].Replace(".", "");

                    listenThread = new Thread(ReceiveMessage);
                    listenThread.Start();
                    SocketConnected = true;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogEvents($"Client socket ReceiveMessage {ex.Message}\r\n{DateTime.Now:MM-dd-yyyy HH:mm:ss.fff} : Attempting to reconnect ...");
                if (ezCashclient != null)
                {
                    if (ezCashclient.Connected)
                        ezCashclient.Client.Shutdown(SocketShutdown.Both);
                    ezCashclient.Client.Close();
                    ezCashclient.Client.Dispose();
                    ezCashclient = null;
                }
                SocketConnected = false;
                Thread.Sleep(45000);
                if (listenThread != null)
                {
                    if (listenThread.IsAlive)
                        listenThread.Abort(100);
                }
                return false;
            }

        }

        public async void ReceiveMessage()
        {
            try
            {
                while (true && ezCashclient != null && ezCashclient.Connected)
                {
                    try
                    {
                        byte[] bytesToRead = new byte[ezCashclient.ReceiveBufferSize];
                        int bytesRead = clientStream.Read(bytesToRead, 0, ezCashclient.ReceiveBufferSize);
                        if (bytesRead > 0)
                        {
                            string ezCashResponse = utilities.ByteToHexaEZCash(bytesToRead.Skip(2).Take(bytesRead).ToArray(), bytesRead);
                            await ProcessSocketMessage(ezCashResponse);
                        }
                        else
                        {
                            if (ezCashclient != null)
                            {
                                if (ezCashclient.Connected)
                                    ezCashclient.Client.Shutdown(SocketShutdown.Both);
                                ezCashclient.Client.Close();
                                ezCashclient.Client.Dispose();
                            }
                            await DisplayErrorMessage("Sorry, Dispenser is temporarily out of service.");
                            SocketConnected = false;
                            InitailConnected = false;
                            await HidePanelsonDisconnnect();
                            await Task.Delay(10000);
                            await Task.Factory.StartNew(() => BackgroundInitializing());
                            if (listenThread != null)
                            {
                                if (listenThread.IsAlive)
                                    listenThread.Abort(100);
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        LogEvents($"Client socket ReceiveMessage {ex.Message} ");
                        if (ezCashclient != null)
                        {
                            if (ezCashclient.Connected)
                                ezCashclient.Client.Shutdown(SocketShutdown.Both);
                            ezCashclient.Client.Close();
                            ezCashclient.Client.Dispose();
                            ezCashclient = null;
                        }
                        await DisplayErrorMessage("Sorry, Dispenser is temporarily out of service.");
                        SocketConnected = false;
                        //Thread initializeThread = new Thread(() => BackgroundInitializing());
                        //initializeThread.Start();

                        await Task.Factory.StartNew(() => BackgroundInitializing());
                        if (listenThread != null)
                        {
                            if (listenThread.IsAlive)
                                listenThread.Abort(100);
                        }
                        break;
                    }
                }

            }
            catch (Exception ex)
            {
                LogEvents($"Client socket ReceiveMessage {ex.Message} ");
                if (ezCashclient != null)
                {
                    if (ezCashclient.Connected)
                        ezCashclient.Client.Shutdown(SocketShutdown.Both);
                    ezCashclient.Client.Close();
                    ezCashclient.Client.Dispose();
                    ezCashclient = null;
                    SocketConnected = false;
                }
                await DisplayErrorMessage("Sorry, Dispenser is temporarily out of service.");
            }
        }

        public void SendSocketMessage(string message)
        {
            try
            {

                byte[] buffer = new byte[ezCashclient.ReceiveBufferSize];
                byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(message); //("1111.000...1 > .38527859440..ABC....");
                LogEvents($"Sending request to EZCash {message} ");
                clientStream.Write(bytesToSend, 0, bytesToSend.Length);

            }
            catch (Exception ex)
            {
                LogEvents($"Client socket SendMessage {ex.Message} ");
            }
        }
        public async Task ProcessSocketMessage(string ezCashMessage)
        {
            try
            {
                var type = ezCashMessage.Substring(0, 3);

                switch (type)
                {
                    case "40.":
                        LogEvents($"Parsing dispense response.");
                        await ProcessBarcodeTransaction(ezCashMessage);
                        break;
                    case "10.": //10.000.000.1
                        LogEvents($"Parsing device monitor commands.");
                        if (ezCashMessage.Equals("10.000.000.2.."))
                        {
                            await ProcessDownRequest(ezCashMessage);

                        }
                        else if (ezCashMessage.Equals("10.000.000.1.."))
                        {
                            await ProcessUpRequest(ezCashMessage);

                        }
                        else if (ezCashMessage.Equals("10.000.000.7.."))
                        {
                            await ProcessConfigRequest(ezCashMessage);

                        }
                        break;
                    case string a when a.Contains("30"):
                    case string b when b.Contains("30."):
                        LogEvents($"Success coin dispense response.");
                        DisplayTransactionCompleteScreen();
                        break;
                    case string a when a.Contains("31"):
                    case string b when b.Contains("31."):
                        LogEvents($"Failure coin dispense response.");
                        TimeOutTransactionForCoins();
                        break;
                    default:
                        LogEvents($"Parsing Cassette status details.");
                        await ProcessCassetteStatus(ezCashMessage);
                        break;
                }
            }
            catch (Exception ex)
            {
                LogEvents($"Client socket ReceiveMessage {ex.Message} ");
            }

        }

        public async Task DisplayErrorMessage(string message)
        {
            try
            {

                await Task.Run(() =>
                {
                    var lblMessage1 = Controls.Find("lblMessage1", true).FirstOrDefault();
                    var lblMessage2 = Controls.Find("lblMessage2", true).FirstOrDefault();
                    var pnlMessage = Controls.Find("pnlMessage", true).FirstOrDefault();

                    this.Invoke(new MethodInvoker(delegate
                    {

                        lblInitial1.Text = String.Empty;
                        lblInitial2.Text = String.Empty;

                        lblInitial1.SetBounds((pnlInitialize.ClientSize.Width - lblInitial1.Width) / 2, (pnlInitialize.ClientSize.Height - lblInitial1.Height) / 2, 0, 0, BoundsSpecified.Location);
                        lblInitial2.SetBounds((pnlInitialize.ClientSize.Width - lblInitial2.Width) / 2, (pnlInitialize.ClientSize.Height - lblInitial2.Height) / 2, 0, 0, BoundsSpecified.Location);


                    }));


                    if (null != lblMessage1 && lblMessage1 is Label && null != pnlMessage && pnlMessage is Panel)
                    {

                        BeginInvoke((Action)delegate ()
                        {
                            lblMessage2.Text = string.Empty;
                            (lblMessage1 as Label).Text = string.Empty;
                            (lblMessage1 as Label).Text = message;
                            (lblMessage1 as Label).Font = new Font("Calibri", 25, FontStyle.Regular);
                            (lblMessage1 as Label).SetBounds(((pnlMessage as Panel).ClientSize.Width - (lblMessage1 as Label).Width) / 2, ((pnlMessage as Panel).ClientSize.Height - (lblMessage1 as Label).Height) / 2, 0, 0, BoundsSpecified.Location);

                        });
                    }
                });
            }
            catch (Exception ex)
            {
                LogEvents($"Client socket ReceiveMessage {ex.Message} ");
            }
        }

        private async Task HidePanelsonDisconnnect()
        {
            var pnlCasette1 = Controls.Find("pnlCasette1", true).FirstOrDefault();
            var pnlCasette2 = Controls.Find("pnlCasette2", true).FirstOrDefault();
            var pnlCasette3 = Controls.Find("pnlCasette3", true).FirstOrDefault();
            var pnlCasette4 = Controls.Find("pnlCasette4", true).FirstOrDefault();
            var pnlCasette5 = Controls.Find("pnlCasette5", true).FirstOrDefault();
            var pnlCasette6 = Controls.Find("pnlCasette6", true).FirstOrDefault();
            var pnlCasette7 = Controls.Find("pnlCasette7", true).FirstOrDefault();
            var pnlCasette8 = Controls.Find("pnlCasette8", true).FirstOrDefault();

            if (pnlCasette1 != null && pnlCasette2 != null && pnlCasette3 != null && pnlCasette4 != null && pnlCasette5 != null && pnlCasette6 != null && pnlCasette7 != null && pnlCasette8 != null && pnlCasstteStatus != null && pnlCasette1 is Panel && pnlCasstteStatus is Panel)
            {
                BeginInvoke((Action)delegate ()
                {
                    pnlCasette1.Visible = false;
                    pnlCasette2.Visible = false;
                    pnlCasette3.Visible = false;
                    pnlCasette4.Visible = false;
                    pnlCasette5.Visible = false;
                    pnlCasette6.Visible = false;
                    pnlCasette7.Visible = false;
                    pnlCasette8.Visible = false;
                });
            }
        }

        private void LogEvents(string input)
        {
            Logger.LogWithNoLock($"{DateTime.Now:MM-dd-yyyy HH:mm:ss.fff} : {input}");
        }
    }
}
