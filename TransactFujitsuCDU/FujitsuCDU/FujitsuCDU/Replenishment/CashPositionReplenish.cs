using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FujitsuCDU.Replenishment
{
    public partial class CashPositionReplenish : Form
    {
        public List<DenominationInfo> DenomsInformation
        {
            get; set;
        }


        public TcpClient ezCashclient;
        public NetworkStream clientStream;
        Utilities utilities = new Utilities();

        public CashPositionReplenish(TcpClient tcpClient)
        {
            InitializeComponent();
            this.ezCashclient = tcpClient;
            clientStream = ezCashclient.GetStream();
        }

        private void CashPositionReplenish_Load(object sender, EventArgs e)
        {
            //SendSocketMessage($"0032.000..9");
            dgcashPosition.DataSource = DenomsInformation;
            dgcashPosition.Refresh();
        }



        private void LogEvents(string input)
        {
            Logger.LogWithNoLock($"{DateTime.Now:MM-dd-yyyy HH:mm:ss.fff} : {input}");
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void CashPositionReplenish_FormClosing(object sender, FormClosingEventArgs e)
        {
            ApplicationProperty.CashPositionStarted = false;
        }
    }
}
