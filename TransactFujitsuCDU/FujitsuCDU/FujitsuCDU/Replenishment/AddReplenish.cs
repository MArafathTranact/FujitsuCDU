using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using TextBox = System.Windows.Forms.TextBox;

namespace FujitsuCDU.Replenishment
{
    public partial class AddReplenish : Form
    {
        private IDisposable _cassette1Subscription = null;
        private IDisposable _cassette2Subscription = null;
        private IDisposable _cassette3Subscription = null;
        private IDisposable _cassette4Subscription = null;
        private IDisposable _cassette5Subscription = null;
        private IDisposable _cassette6Subscription = null;
        public List<DenominationInfo> DenomsInformation
        {
            get; set;
        }

        public TcpClient ezCashclient;
        public NetworkStream clientStream;


        public List<DenominationInfo> ReturnDenomsInformation
        {
            get; set;
        }

        public AddReplenish(List<DenominationInfo> DenomsInformation, TcpClient tcpClient)
        {
            InitializeComponent();
            this.DenomsInformation = DenomsInformation;
            this.ezCashclient = tcpClient;
            clientStream = ezCashclient.GetStream();

        }

        public async Task SetCassetteValue()
        {
            await Task.Run(() =>
            {
                if (DenomsInformation != null)
                {
                    foreach (var denom in DenomsInformation)
                    {
                        switch (denom.cassette_id)
                        {
                            case "1":
                                var lblCassette1 = Controls.Find("lblCassette1", true).FirstOrDefault();

                                if (lblCassette1 != null && lblCassette1 is Label)
                                {
                                    BeginInvoke((Action)delegate ()
                                    {
                                        lblCassette1.Text = "Cassette 1 ($" + denom.denomination + ")";

                                    });
                                }
                                break;
                            case "2":
                                var lblCassette2 = Controls.Find("lblCassette2", true).FirstOrDefault();
                                if (lblCassette2 != null && lblCassette2 is Label)
                                {
                                    BeginInvoke((Action)delegate ()
                                    {
                                        lblCassette2.Text = "Cassette 2 ($" + denom.denomination + ")";

                                    });
                                }
                                break;
                            case "3":
                                var lblCassette3 = Controls.Find("lblCassette3", true).FirstOrDefault();
                                if (lblCassette3 != null && lblCassette3 is Label)
                                {
                                    BeginInvoke((Action)delegate ()
                                    {
                                        lblCassette3.Text = "Cassette 3 ($" + denom.denomination + ")";

                                    });
                                }
                                break;
                            case "4":
                                var lblCassette4 = Controls.Find("lblCassette4", true).FirstOrDefault();
                                if (lblCassette4 != null && lblCassette4 is Label)
                                {
                                    BeginInvoke((Action)delegate ()
                                    {
                                        lblCassette4.Text = "Cassette 4 ($" + denom.denomination + ")";

                                    });
                                }
                                break;
                            case "5":
                                var lblCassette5 = Controls.Find("lblCassette5", true).FirstOrDefault();
                                if (lblCassette5 != null && lblCassette5 is Label)
                                {
                                    BeginInvoke((Action)delegate ()
                                    {
                                        lblCassette5.Text = "Cassette 5 ($" + denom.denomination + ")";

                                    });
                                }
                                break;
                            case "6":
                                var lblCassette6 = Controls.Find("lblCassette6", true).FirstOrDefault();
                                if (lblCassette6 != null && lblCassette6 is Label)
                                {
                                    BeginInvoke((Action)delegate ()
                                    {
                                        lblCassette6.Text = "Cassette 6 ($" + denom.denomination + ")";

                                    });
                                }
                                break;
                        }

                    }

                }
            });


        }

        private void AddReplenish_Load(object sender, EventArgs e)
        {
            _ = this.SetCassetteValue();
            SubscribeCassette1();
            SubscribeCassette2();
            SubscribeCassette3();
            SubscribeCassette4();
            SubscribeCassette5();
            SubscribeCassette6();

        }
        private void SubscribeCassette1()
        {
            _cassette1Subscription =
           Observable
               .FromEventPattern(h => txtCassette1.TextChanged += h, h => txtCassette1.TextChanged -= h)
               .Throttle(TimeSpan.FromMilliseconds(400.0))
               .Select(ep => txtCassette1.Text)
               .Select(text => Observable.FromAsync(ct => DoSearchAsync(text, 1, ct)))
               .Switch()
               .ObserveOn(txtCassette1)
               .Subscribe(results =>
               {
                   var lblCassette1Result = Controls.Find("lblCassette1Result", true).FirstOrDefault();
                   var denoms = "0";
                   if (!string.IsNullOrEmpty(results))
                   {
                       if (lblCassette1Result != null && lblCassette1Result is Label)
                       {
                           denoms = DenomsInformation.Where(x => x.cassette_id == "1").Select(x => x.denomination).FirstOrDefault();//100;
                           if (string.IsNullOrEmpty(denoms))
                           {
                               denoms = "0";
                           }
                           else
                           {
                               if (denoms.Contains('.'))
                               {
                                   var dollar = denoms.Split('.');
                                   if (dollar.Length > 1)
                                       denoms = dollar[0];
                               }
                           }
                           BeginInvoke((Action)delegate ()
                           {
                               lblCassette1Result.Text = "$" + (int.Parse(denoms) * int.Parse(results));

                           });

                       }

                   }
                   else
                   {
                       if (lblCassette1Result != null && lblCassette1Result is Label)
                       {
                           BeginInvoke((Action)delegate ()
                           {
                               lblCassette1Result.Text = "$" + denoms;

                           });
                       }
                   }

                   CalculateTotalAmount();


               });
        }

        private void SubscribeCassette2()
        {
            _cassette2Subscription =
           Observable
               .FromEventPattern(h => txtCassette2.TextChanged += h, h => txtCassette2.TextChanged -= h)
               .Throttle(TimeSpan.FromMilliseconds(400.0))
               .Select(ep => txtCassette2.Text)
               .Select(text => Observable.FromAsync(ct => DoSearchAsync(text, 2, ct)))
               .Switch()
               .ObserveOn(txtCassette2)
               .Subscribe(results =>
               {
                   var denoms = "0";
                   var lblCassette2Result = Controls.Find("lblCassette2Result", true).FirstOrDefault();
                   if (!string.IsNullOrEmpty(results))
                   {
                       if (lblCassette2Result != null && lblCassette2Result is Label)
                       {
                           denoms = DenomsInformation.Where(x => x.cassette_id == "2").Select(x => x.denomination).FirstOrDefault();
                           if (string.IsNullOrEmpty(denoms))
                           {
                               denoms = "0";
                           }
                           else
                           {
                               if (denoms.Contains('.'))
                               {
                                   var dollar = denoms.Split('.');
                                   if (dollar.Length > 1)
                                       denoms = dollar[0];
                               }
                           }
                           BeginInvoke((Action)delegate ()
                           {
                               lblCassette2Result.Text = "$" + (int.Parse(denoms) * int.Parse(results));

                           });
                       }

                   }
                   else
                   {
                       if (lblCassette2Result != null && lblCassette2Result is Label)
                       {
                           BeginInvoke((Action)delegate ()
                           {
                               lblCassette2Result.Text = "$" + denoms;

                           });
                       }
                   }

                   CalculateTotalAmount();


               });
        }
        private void SubscribeCassette3()
        {
            _cassette3Subscription =
           Observable
               .FromEventPattern(h => txtCassette3.TextChanged += h, h => txtCassette3.TextChanged -= h)
               .Throttle(TimeSpan.FromMilliseconds(400.0))
               .Select(ep => txtCassette3.Text)
               .Select(text => Observable.FromAsync(ct => DoSearchAsync(text, 3, ct)))
               .Switch()
               .ObserveOn(txtCassette3)
               .Subscribe(results =>
               {
                   var denoms = "0";
                   var lblCassette3Result = Controls.Find("lblCassette3Result", true).FirstOrDefault();

                   if (!string.IsNullOrEmpty(results))
                   {
                       if (lblCassette3Result != null && lblCassette3Result is Label)
                       {
                           denoms = DenomsInformation.Where(x => x.cassette_id == "3").Select(x => x.denomination).FirstOrDefault();
                           if (string.IsNullOrEmpty(denoms))
                           {
                               denoms = "0";
                           }
                           else
                           {
                               if (denoms.Contains('.'))
                               {
                                   var dollar = denoms.Split('.');
                                   if (dollar.Length > 1)
                                       denoms = dollar[0];
                               }
                           }
                           BeginInvoke((Action)delegate ()
                           {
                               lblCassette3Result.Text = "$" + (int.Parse(denoms) * int.Parse(results));

                           });
                       }
                   }
                   else
                   {
                       if (lblCassette3Result != null && lblCassette3Result is Label)
                       {
                           BeginInvoke((Action)delegate ()
                           {
                               lblCassette3Result.Text = "$" + denoms;

                           });
                       }
                   }

                   CalculateTotalAmount();

               });
        }

        private void SubscribeCassette4()
        {
            _cassette4Subscription =
           Observable
               .FromEventPattern(h => txtCassette4.TextChanged += h, h => txtCassette4.TextChanged -= h)
               .Throttle(TimeSpan.FromMilliseconds(400.0))
               .Select(ep => txtCassette4.Text)
               .Select(text => Observable.FromAsync(ct => DoSearchAsync(text, 4, ct)))
               .Switch()
               .ObserveOn(txtCassette4)
               .Subscribe(results =>
               {
                   var lblCassette4Result = Controls.Find("lblCassette4Result", true).FirstOrDefault();
                   var denoms = "0";
                   if (!string.IsNullOrEmpty(results))
                   {
                       if (lblCassette4Result != null && lblCassette4Result is Label)
                       {
                           denoms = DenomsInformation.Where(x => x.cassette_id == "4").Select(x => x.denomination).FirstOrDefault();
                           if (string.IsNullOrEmpty(denoms))
                           {
                               denoms = "0";
                           }
                           else
                           {
                               if (denoms.Contains('.'))
                               {
                                   var dollar = denoms.Split('.');
                                   if (dollar.Length > 1)
                                       denoms = dollar[0];
                               }
                           }
                           BeginInvoke((Action)delegate ()
                           {
                               lblCassette4Result.Text = "$" + (int.Parse(denoms) * int.Parse(results));

                           });
                       }
                   }
                   else
                   {
                       if (lblCassette4Result != null && lblCassette4Result is Label)
                       {
                           BeginInvoke((Action)delegate ()
                           {
                               lblCassette4Result.Text = "$" + denoms;

                           });
                       }
                   }

                   CalculateTotalAmount();

               });
        }

        private void SubscribeCassette5()
        {
            _cassette1Subscription =
           Observable
               .FromEventPattern(h => txtCassette5.TextChanged += h, h => txtCassette5.TextChanged -= h)
               .Throttle(TimeSpan.FromMilliseconds(400.0))
               .Select(ep => txtCassette5.Text)
               .Select(text => Observable.FromAsync(ct => DoSearchAsync(text, 1, ct)))
               .Switch()
               .ObserveOn(txtCassette5)
               .Subscribe(results =>
               {
                   var lblCassette5Result = Controls.Find("lblCassette5Result", true).FirstOrDefault();
                   var denoms = "0";
                   if (!string.IsNullOrEmpty(results))
                   {
                       if (lblCassette5Result != null && lblCassette5Result is Label)
                       {
                           denoms = DenomsInformation.Where(x => x.cassette_id == "5").Select(x => x.denomination).FirstOrDefault();
                           if (string.IsNullOrEmpty(denoms))
                           {
                               denoms = "0";
                           }
                           else
                           {
                               if (denoms.Contains('.'))
                               {
                                   var dollar = denoms.Split('.');
                                   if (dollar.Length > 1)
                                       denoms = dollar[0];
                               }
                           }
                           BeginInvoke((Action)delegate ()
                           {
                               lblCassette5Result.Text = "$" + (int.Parse(denoms) * int.Parse(results));

                           });
                       }
                   }
                   else
                   {
                       if (lblCassette5Result != null && lblCassette5Result is Label)
                       {
                           BeginInvoke((Action)delegate ()
                           {
                               lblCassette5Result.Text = "$" + denoms;

                           });
                       }
                   }

                   CalculateTotalAmount();

               });
        }

        private void SubscribeCassette6()
        {
            _cassette1Subscription =
           Observable
               .FromEventPattern(h => txtCassette6.TextChanged += h, h => txtCassette6.TextChanged -= h)
               .Throttle(TimeSpan.FromMilliseconds(400.0))
               .Select(ep => txtCassette6.Text)
               .Select(text => Observable.FromAsync(ct => DoSearchAsync(text, 1, ct)))
               .Switch()
               .ObserveOn(txtCassette6)
               .Subscribe(results =>
               {
                   var lblCassette6Result = Controls.Find("lblCassette6Result", true).FirstOrDefault();
                   var denoms = "0";
                   if (!string.IsNullOrEmpty(results))
                   {
                       if (lblCassette6Result != null && lblCassette6Result is Label)
                       {
                           denoms = DenomsInformation.Where(x => x.cassette_id == "6").Select(x => x.denomination).FirstOrDefault();
                           if (string.IsNullOrEmpty(denoms))
                           {
                               denoms = "0";
                           }
                           else
                           {
                               if (denoms.Contains('.'))
                               {
                                   var dollar = denoms.Split('.');
                                   if (dollar.Length > 1)
                                       denoms = dollar[0];
                               }
                           }
                           BeginInvoke((Action)delegate ()
                           {
                               lblCassette6Result.Text = "$" + (int.Parse(denoms) * int.Parse(results));

                           });
                       }
                   }
                   else
                   {
                       if (lblCassette6Result != null && lblCassette6Result is Label)
                       {
                           BeginInvoke((Action)delegate ()
                           {
                               lblCassette6Result.Text = "$" + denoms;

                           });
                       }
                   }

                   CalculateTotalAmount();

               });
        }

        private Task CalculateTotalAmount()
        {
            var lblCassettesTotal = Controls.Find("lblCassettesTotal", true).FirstOrDefault();
            if (lblCassettesTotal != null && lblCassettesTotal is Label)
            {
                BeginInvoke((Action)delegate ()
                {

                    var lblCassette1Result = Controls.Find("lblCassette1Result", true).FirstOrDefault();
                    var lblCassette2Result = Controls.Find("lblCassette2Result", true).FirstOrDefault();
                    var lblCassette3Result = Controls.Find("lblCassette3Result", true).FirstOrDefault();
                    var lblCassette4Result = Controls.Find("lblCassette4Result", true).FirstOrDefault();
                    var lblCassette5Result = Controls.Find("lblCassette5Result", true).FirstOrDefault();
                    var lblCassette6Result = Controls.Find("lblCassette6Result", true).FirstOrDefault();
                    var total = 0;
                    if (!string.IsNullOrEmpty(lblCassette1Result.Text))
                    {
                        var cassette1Total = int.Parse(lblCassette1Result.Text.Replace("$", ""));
                        total += cassette1Total;
                        ;
                    }

                    if (!string.IsNullOrEmpty(lblCassette2Result.Text))
                    {
                        var cassette1Total = int.Parse(lblCassette2Result.Text.Replace("$", ""));
                        total += cassette1Total;
                    }

                    if (!string.IsNullOrEmpty(lblCassette3Result.Text))
                    {
                        var cassette1Total = int.Parse(lblCassette3Result.Text.Replace("$", ""));
                        total += cassette1Total;
                    }

                    if (!string.IsNullOrEmpty(lblCassette4Result.Text))
                    {
                        var cassette1Total = int.Parse(lblCassette4Result.Text.Replace("$", ""));
                        total += cassette1Total;
                    }

                    if (!string.IsNullOrEmpty(lblCassette5Result.Text))
                    {
                        var cassette1Total = int.Parse(lblCassette5Result.Text.Replace("$", ""));
                        total += cassette1Total;
                    }

                    if (!string.IsNullOrEmpty(lblCassette6Result.Text))
                    {
                        var cassette1Total = int.Parse(lblCassette6Result.Text.Replace("$", ""));
                        total += cassette1Total;
                    }

                    lblCassettesTotal.Text = total.ToString();

                });
            }

            return Task.CompletedTask;
        }
        private async Task<string> DoSearchAsync(string text, int cassette, CancellationToken ct)
        {

            if (System.Text.RegularExpressions.Regex.IsMatch(text, "[^0-9]"))
            {
                var txtCassette = Controls.Find($"txtCassette{cassette}", true).FirstOrDefault();
                if (txtCassette != null && txtCassette is TextBox)
                {
                    BeginInvoke((Action)delegate ()
                    {
                        txtCassette.Text = text.Remove(text.Length - 1);
                    });
                }

                return text.Remove(text.Length - 1);
            }


            return text;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (_cassette1Subscription != null)
                _cassette1Subscription.Dispose();
            if (_cassette2Subscription != null)
                _cassette2Subscription.Dispose();
            if (_cassette3Subscription != null)
                _cassette3Subscription.Dispose();
            if (_cassette4Subscription != null)
                _cassette4Subscription.Dispose();
            if (_cassette5Subscription != null)
                _cassette5Subscription.Dispose();
            if (_cassette6Subscription != null)
                _cassette6Subscription.Dispose();
            this.Close();
        }

        private void btnSubmit_Click(object sender, EventArgs e)
        {
            var denoms = new List<DenominationInfo>();

            if (!string.IsNullOrEmpty(txtCassette1.Text))
                denoms.Add(new DenominationInfo() { cassette_id = "1", host_start_count = int.Parse(txtCassette1.Text) });
            if (!string.IsNullOrEmpty(txtCassette2.Text))
                denoms.Add(new DenominationInfo() { cassette_id = "2", host_start_count = int.Parse(txtCassette2.Text) });
            if (!string.IsNullOrEmpty(txtCassette3.Text))
                denoms.Add(new DenominationInfo() { cassette_id = "3", host_start_count = int.Parse(txtCassette3.Text) });
            if (!string.IsNullOrEmpty(txtCassette4.Text))
                denoms.Add(new DenominationInfo() { cassette_id = "4", host_start_count = int.Parse(txtCassette4.Text) });
            if (!string.IsNullOrEmpty(txtCassette5.Text))
                denoms.Add(new DenominationInfo() { cassette_id = "5", host_start_count = int.Parse(txtCassette5.Text) });
            if (!string.IsNullOrEmpty(txtCassette6.Text))
                denoms.Add(new DenominationInfo() { cassette_id = "6", host_start_count = int.Parse(txtCassette6.Text) });

            ReturnDenomsInformation = denoms;
            //foreach (var denom in denoms)
            //{
            //    var length = denom.host_start_count.ToString().Length;

            //    var message = $"0011.000...19.;0616071035350001=1234567890?..";
            //    //var message = $"11.000...19.;0616071035350001=1234567890?..A HIB   .000002500000... ";
            //    switch (denom.cassette_id)
            //    {
            //        case "1":
            //            message += "A HIC   ." + $"{denom.host_start_count}";
            //            break;
            //        case "2":
            //            message += "A HHC   ." + $"{denom.host_start_count}";
            //            break;
            //        case "3":
            //            message += "A HAC   ." + $"{denom.host_start_count}";
            //            break;
            //        case "4":
            //            message += "A HBC   ." + $"{denom.host_start_count}";
            //            break;
            //        case "5":
            //            message += "A HGC   ." + $"{denom.host_start_count}";
            //            break;
            //        case "6":
            //            message += "A HKC   ." + $"{denom.host_start_count}";
            //            break;
            //        default:
            //            break;
            //    }

            //    if (denom.host_start_count != 0)
            //    {
            //        LogEvents($"Adding {denom.host_start_count} for cassette {denom.cassette_id}");
            //        SendSocketMessage(message);
            //    }
            //}
        }

        public void SendSocketMessage(string message)
        {
            try
            {
                if (ezCashclient != null && ezCashclient.Connected)
                {

                    byte[] buffer = new byte[ezCashclient.ReceiveBufferSize];
                    byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(message); //("1111.000...1 > .38527859440..ABC....");
                    LogEvents($"Sending message to EZCash {message} ");
                    clientStream.Write(bytesToSend, 0, bytesToSend.Length);

                }
            }
            catch (Exception ex)
            {
                LogEvents($"Client socket SendMessage {ex.Message} ");
            }
        }

        private void LogEvents(string input)
        {
            Logger.LogWithNoLock($"{DateTime.Now:MM-dd-yyyy HH:mm:ss.fff} : {input}");
        }
    }
}
