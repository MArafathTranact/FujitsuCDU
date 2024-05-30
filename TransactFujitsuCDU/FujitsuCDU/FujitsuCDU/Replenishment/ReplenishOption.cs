using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FujitsuCDU.Replenishment
{
    public partial class frmReplenish : Form
    {
        public List<DenominationInfo> DenomsInformation
        {
            get; set;
        }


        public TcpClient ezCashclient;
        public NetworkStream clientStream;
        Utilities utilities = new Utilities();
        public bool IsCashPositionSelected = false;

        public frmReplenish(List<DenominationInfo> DenomsInformation, TcpClient tcpClient, bool isCashPositionSelected)
        {
            InitializeComponent();
            this.DenomsInformation = DenomsInformation;
            this.ezCashclient = tcpClient;
            clientStream = ezCashclient.GetStream();
            this.IsCashPositionSelected = isCashPositionSelected;
        }

        private void btnAddCash_Click(object sender, EventArgs e)
        {
            AddReplenish addReplenish = new AddReplenish(DenomsInformation, ezCashclient);
            addReplenish.ShowDialog();

            //var denoms = addReplenish.ReturnDenomsInformation;
            //if (denoms != null && denoms.Any())
            //{
            //    Task.Run(async () =>
            //    {
            //        await ProcessResetCash(denoms, false, false);

            //    });
            //}


        }

        private void btnResetCash_Click(object sender, EventArgs e)
        {
            ResetReplenish resetReplenish = new ResetReplenish(DenomsInformation, ezCashclient);
            resetReplenish.ShowDialog();

            //var denoms = resetReplenish.ReturnDenomsInformation;
            //var isCutEnabled = resetReplenish.IsCutEnabled;
            //if (denoms != null && denoms.Any())
            //{
            //    Task.Run(async () =>
            //    {
            //        await ProcessResetCash(denoms, true, isCutEnabled);

            //    });
            //}

        }

        private void btnCashPosition_Click(object sender, EventArgs e)
        {
            ApplicationProperty.CashPositionStarted = true;
            SendSocketMessage($"0032.000..9");
        }

        private void btnDone_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        public async void SendSocketMessage(string message)
        {
            try
            {
                if (ezCashclient != null && ezCashclient.Connected)
                {

                    byte[] buffer = new byte[ezCashclient.ReceiveBufferSize];
                    byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(message);
                    //LogEvents($"Sending request to EZCash {message} ");
                    clientStream.Write(bytesToSend, 0, bytesToSend.Length);
                }

            }
            catch (Exception ex)
            {
                //LogEvents($"Client socket SendMessage {ex.Message} ");
            }
        }

        private async Task ProcessResetCash(List<DenominationInfo> denoms, bool isReset, bool isCutEnabled = false)
        {
            foreach (var denom in denoms)
            {
                var length = denom.host_start_count.ToString().Length;

                var message = $"0011.000...19.;0616071035350001=1234567890?..";
                switch (denom.cassette_id)
                {
                    case "1":
                        message += (isReset == true ? "A HIB   ." : "A HIC   .") + $"{denom.host_start_count}";
                        break;
                    case "2":
                        message += (isReset == true ? "A HHB   ." : "A HHC   .") + $"{denom.host_start_count}";
                        break;
                    case "3":
                        message += (isReset == true ? "A HAB   ." : "A HAC   .") + $"{denom.host_start_count}";
                        //                        message += "A HAB   ." + $"{denom.host_start_count}";
                        break;
                    case "4":
                        message += (isReset == true ? "A HBB   ." : "A HBC   .") + $"{denom.host_start_count}";
                        //message += "A HBB   ." + $"{denom.host_start_count}";
                        break;
                    case "5":
                        message += (isReset == true ? "A HGB   ." : "A HGC   .") + $"{denom.host_start_count}";
                        //message += "A HGB   ." + $"{denom.host_start_count}";
                        break;
                    case "6":
                        message += (isReset == true ? "A HKB   ." : "A HKC   .") + $"{denom.host_start_count}";
                        //message += "A HKB   ." + $"{denom.host_start_count}";
                        break;
                    default:
                        break;
                }

                if (denom.host_start_count != 0)
                {
                    SendSocketMessage(message);
                    await Task.Delay(2000);
                }
            }

            if (isCutEnabled)
            {
                var message = $"0011.000...11.;0616071035350001=1234567890?..A F     .00000400...";
                SendSocketMessage(message);

            }

            await Task.CompletedTask;
        }
    }
}
