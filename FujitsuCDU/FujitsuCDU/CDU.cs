using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FujitsuCDU
{
    public partial class CDU : Form
    {
        Thread initializeThread;
        Thread bgThread;
        FujitsuCDUProcessor cduProcessor;
        string descriptionLoad;
        private string _barcode = "";
        public CDU()
        {
            InitializeComponent();
            descriptionLoad = GetFileLocation("DescriptionLoad");
            cduProcessor = new FujitsuCDUProcessor();

        }
        public string GetFileLocation(string name)
        {
            return ConfigurationManager.AppSettings[name];
        }

        private void CDU_Load(object sender, EventArgs e)
        {

            int x = (pnlInitialize.Size.Width - lblInitialize.Size.Width) / 2;
            lblInitialize.Location = new Point(x, lblInitialize.Location.Y);

            //lblInitialize.Text = "Initializing Fujitsu CDU Dispenser...";
            byte[] bytes = Encoding.Default.GetBytes(lblInitialize.Text);
            byte check = 255;
            for (int i = 2; i < bytes.Length - 4; i++)
            {
                check ^= bytes[i];
            }

            initializeThread = new Thread(() => BackgroundInitializing());
            initializeThread.Start();



            //lblInitialize.Text = "Fujitsu CDU is Ready for Processing.";

            // tableLayoutPanel1.Controls.Add(lblInitialize, 1, 0);

        }


        private void BackgroundProcess()
        {
            Thread.Sleep(9000);
            //LoadScreen(1, "Waiting for data in...");
            Thread.Sleep(3000);
            LoadScreen(2, "Dispensing $4000.00 out of $4500.00");
            Thread.Sleep(3000);
            LoadScreen(3, "Dispensed $4500.00 out of $4500.00");
            Thread.Sleep(3000);
            LoadScreen(4, "Please Scan the Barcode.");

        }

        private void BackgroundInitializing()
        {

            Thread cduInit = new Thread(StartCDUInit);
            cduInit.Start();
            Thread.Sleep(20);

            Thread changedescription = new Thread(ChangeDescription);
            changedescription.Start();


        }

        private void ChangeDescription()
        {
            while (true)
            {
                if (cduProcessor.IsProcessCompleted)
                {
                    if (InvokeRequired)
                    {
                        this.Invoke(new MethodInvoker(delegate
                        {
                            lblInitialize.Text = "Fujitsu CDU is Ready for Processing.";
                            if (descriptionLoad == "1")
                                lblProcessMessage.Text = "Please scan the Barcode.";
                            else if (descriptionLoad == "2")
                                lblProcessMessage.Text = "Please scan the Barcode.";
                            else if (descriptionLoad == "3")
                                lblProcessMessage.Text = "Waiting for data in.";
                            lblProcessMessage.Font = new Font("Arial", 20, FontStyle.Regular);

                            lblInitialize.SetBounds((pnlInitialize.ClientSize.Width - lblInitialize.Width) / 2, (pnlInitialize.ClientSize.Height - lblInitialize.Height) / 2, 0, 0, BoundsSpecified.Location);

                            lblProcessMessage.SetBounds((pnlMessage.ClientSize.Width - lblProcessMessage.Width) / 2, (pnlMessage.ClientSize.Height - lblProcessMessage.Height) / 2, 0, 0, BoundsSpecified.Location);


                        }));
                    }

                    bgThread = new Thread(() => BackgroundProcess());
                    bgThread.Start();
                    break;
                }
            }

        }

        private void StartCDUInit()
        {
            cduProcessor.InitCDU();
            if (InvokeRequired)
            {
                this.Invoke(new MethodInvoker(delegate
                {
                    lblInitialize.Text = "Initializing Fujitsu CDU Dispenser...";

                    lblInitialize.SetBounds((pnlInitialize.ClientSize.Width - lblInitialize.Width) / 2, (pnlInitialize.ClientSize.Height - lblInitialize.Height) / 2, 0, 0, BoundsSpecified.Location);


                }));
            }
        }


        private void LoadScreen(int ScreenNumber, string message)
        {
            switch (ScreenNumber)
            {

                case 1:
                    if (InvokeRequired)
                    {
                        this.Invoke(new MethodInvoker(delegate
                        {
                            lblProcessMessage.Text = message;// "CDU is Ready for dispensing.";
                            lblProcessMessage.Font = new Font("Arial", 35, FontStyle.Regular);
                            lblInitialize.SetBounds((pnlInitialize.ClientSize.Width - lblInitialize.Width) / 2, (pnlInitialize.ClientSize.Height - lblInitialize.Height) / 2, 0, 0, BoundsSpecified.Location);

                            lblProcessMessage.SetBounds((pnlMessage.ClientSize.Width - lblProcessMessage.Width) / 2, (pnlMessage.ClientSize.Height - lblProcessMessage.Height) / 2, 0, 0, BoundsSpecified.Location);


                        }));
                        return;
                    }

                    break;
                case 2:
                    if (InvokeRequired)
                    {
                        this.Invoke(new MethodInvoker(delegate
                        {
                            lblProcessMessage.Text = message;// "Dispensing $4000.00 out of $4500.00";
                            lblProcessMessage.Font = new Font("Arial", 35, FontStyle.Regular);
                            lblInitialize.SetBounds((pnlInitialize.ClientSize.Width - lblInitialize.Width) / 2, (pnlInitialize.ClientSize.Height - lblInitialize.Height) / 2, 0, 0, BoundsSpecified.Location);

                            lblProcessMessage.SetBounds((pnlMessage.ClientSize.Width - lblProcessMessage.Width) / 2, (pnlMessage.ClientSize.Height - lblProcessMessage.Height) / 2, 0, 0, BoundsSpecified.Location);



                        }));
                        return;
                    }
                    break;
                case 3:
                    if (InvokeRequired)
                    {
                        this.Invoke(new MethodInvoker(delegate
                        {
                            lblProcessMessage.Text = message;// "Dispensed $4500.00 out of $4500.00";
                            lblProcessMessage.Font = new Font("Arial", 35, FontStyle.Regular);
                            lblInitialize.SetBounds((pnlInitialize.ClientSize.Width - lblInitialize.Width) / 2, (pnlInitialize.ClientSize.Height - lblInitialize.Height) / 2, 0, 0, BoundsSpecified.Location);

                            lblProcessMessage.SetBounds((pnlMessage.ClientSize.Width - lblProcessMessage.Width) / 2, (pnlMessage.ClientSize.Height - lblProcessMessage.Height) / 2, 0, 0, BoundsSpecified.Location);



                        }));
                        return;
                    }
                    break;
                default:
                    if (InvokeRequired)
                    {
                        this.Invoke(new MethodInvoker(delegate
                        {
                            lblProcessMessage.Text = message;// "Waiting for data in...";
                            lblProcessMessage.Font = new Font("Arial", 20, FontStyle.Regular);
                            lblInitialize.SetBounds((pnlInitialize.ClientSize.Width - lblInitialize.Width) / 2, (pnlInitialize.ClientSize.Height - lblInitialize.Height) / 2, 0, 0, BoundsSpecified.Location);

                            lblProcessMessage.SetBounds((pnlMessage.ClientSize.Width - lblProcessMessage.Width) / 2, (pnlMessage.ClientSize.Height - lblProcessMessage.Height) / 2, 0, 0, BoundsSpecified.Location);



                        }));
                        return;
                    }
                    break;
            }
        }

        private void CDU_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (initializeThread != null)
                    initializeThread.Abort();
                if (bgThread != null)
                    bgThread.Abort();

                // this.Close();
            }
            catch (Exception)
            {

                throw;
            }

        }

        private void CDU_SizeChanged(object sender, EventArgs e)
        {
            //lblInitialize.Font = new Font("Arial", 25, FontStyle.Regular);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            char c = (char)keyData;

            if (char.IsNumber(c))
                _barcode += c;

            if (c == (char)Keys.Return)
            {
                // DoSomethingWithBarcode(_barcode);
                // MessageBox.Show(_barcode);
                Thread cduDispense = new Thread(() => ProcessBarcodeAndDispense(_barcode));
                cduDispense.Start();
                cduDispense.Join();

                _barcode = "";
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }


        private void ProcessBarcodeAndDispense(string barcode)
        {
            cduProcessor.CommState = FujitsuCDUProcessor.TCommState.csReady;
            cduProcessor.IsDispenseReqSent = true;
            cduProcessor.State = FujitsuCDUProcessor.TState.stWaitTranReply;
            cduProcessor.SendMessage("6003ff00002cfedcba9830b130b130303030b1303030303030301500000030303030303030303030303030303030000000001c");
        }

        private void CDU_FormClosed(object sender, FormClosedEventArgs e)
        {
            //if (initializeThread != null)
            //    initializeThread.Abort();
            //if (bgThread != null)
            //    bgThread.Abort();
            //this.Close();
        }
    }
}
