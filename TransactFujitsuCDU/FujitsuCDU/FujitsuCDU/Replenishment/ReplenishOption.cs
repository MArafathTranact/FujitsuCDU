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

        public frmReplenish(List<DenominationInfo> DenomsInformation, TcpClient tcpClient)
        {
            InitializeComponent();
            this.DenomsInformation = DenomsInformation;
            this.ezCashclient = tcpClient;
            clientStream = ezCashclient.GetStream();
        }

        private void btnAddCash_Click(object sender, EventArgs e)
        {
            AddReplenish addReplenish = new AddReplenish(DenomsInformation, ezCashclient);
            addReplenish.ShowDialog();
        }

        private void btnResetCash_Click(object sender, EventArgs e)
        {
            ResetReplenish resetReplenish = new ResetReplenish(DenomsInformation, ezCashclient);
            resetReplenish.ShowDialog();
        }

        private void btnCashPosition_Click(object sender, EventArgs e)
        {
            SendSocketMessage($"0032.000..9");
            //CashPositionReplenish cashPositionReplensh = new CashPositionReplenish(ezCashclient);
            //cashPositionReplensh.ShowDialog();
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
    }
}
