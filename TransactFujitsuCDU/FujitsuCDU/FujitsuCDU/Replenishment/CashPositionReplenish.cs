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
            var cashPositionData = new DataTable();
            cashPositionData.Columns.Add(".");
            cashPositionData.Columns.Add("Bin 1");

            cashPositionData.Columns.Add("Bin 2");
            cashPositionData.Columns.Add("Bin 3");
            cashPositionData.Columns.Add("Bin 4");
            cashPositionData.Columns.Add("Bin 5");
            cashPositionData.Columns.Add("Bin 6");
            cashPositionData.Columns.Add("Bin 7");
            cashPositionData.Columns.Add("Bin 8");

            DataRow dr1 = cashPositionData.NewRow();
            dr1[0] = "Host Start Count";
            cashPositionData.Rows.Add(dr1);

            DataRow dr2 = cashPositionData.NewRow();
            dr2[0] = "Device Start Count";
            cashPositionData.Rows.Add(dr2);

            DataRow dr3 = cashPositionData.NewRow();
            dr3[0] = "Host Cycle Count";
            cashPositionData.Rows.Add(dr3);

            DataRow dr4 = cashPositionData.NewRow();
            dr4[0] = "Device Cycle Count";
            cashPositionData.Rows.Add(dr4);

            DataRow dr5 = cashPositionData.NewRow();
            dr5[0] = "Added Count";
            cashPositionData.Rows.Add(dr5);

            DataRow dr6 = cashPositionData.NewRow();
            dr6[0] = "Old Added Count";
            cashPositionData.Rows.Add(dr6);

            DataRow dr7 = cashPositionData.NewRow();
            dr7[0] = "Status";
            cashPositionData.Rows.Add(dr7);
            cashPositionData.AcceptChanges();

            if (DenomsInformation != null && DenomsInformation.Any())
            {
                int j = 1;
                foreach (DenominationInfo denom in DenomsInformation)
                {
                    var status = denom.status == "0" ? "OK" : denom.status == "2" ? "FATAL" : "EMPTY";
                    cashPositionData.Rows[0][$"Bin {j}"] = denom.host_start_count;
                    cashPositionData.Rows[1][$"Bin {j}"] = denom.dev_start_count;
                    cashPositionData.Rows[2][$"Bin {j}"] = denom.host_cycle_count;
                    cashPositionData.Rows[3][$"Bin {j}"] = denom.dev_cycle_count;
                    cashPositionData.Rows[4][$"Bin {j}"] = denom.added_count;
                    cashPositionData.Rows[5][$"Bin {j}"] = denom.old_added;
                    cashPositionData.Rows[6][$"Bin {j}"] = status;
                    j++;
                }
            }


            cashPositionData.AcceptChanges();
            dgcashPosition.DataSource = cashPositionData;
            dgcashPosition.Columns[0].Width = 150;
            dgcashPosition.Columns[0].HeaderText = "";
            dgcashPosition.Columns[0].DefaultCellStyle.Font = new Font("Calibri", 10, FontStyle.Bold);
            dgcashPosition.RowTemplate.MinimumHeight = 30;
            dgcashPosition.Columns[1].Width = 85;
            dgcashPosition.Columns[2].Width = 85;
            dgcashPosition.Columns[3].Width = 85;
            dgcashPosition.Columns[4].Width = 85;
            dgcashPosition.Columns[5].Width = 85;
            dgcashPosition.Columns[6].Width = 85;
            dgcashPosition.Columns[7].Width = 85;
            dgcashPosition.Columns[8].Width = 85;

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
