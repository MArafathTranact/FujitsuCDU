using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FujitsuCDU.Common;

namespace FujitsuCDU
{
    public class EZCashSocket
    {
        public TcpClient ezCashclient;
        readonly ServiceConfiguration serviceConfiguration = new ServiceConfiguration();
        public NetworkStream clientStream;
        private readonly Utilities utilities = new Utilities();
        readonly Logger logger = new Logger();
        public Thread listenThread;
        FujitsuCDUProcessor cduProcessor;
        public EZCashSocket(FujitsuCDUProcessor fujitsuCDUProcessor)
        {
            cduProcessor = fujitsuCDUProcessor;
        }

        public bool CreateEZCashSocket()
        {
            try
            {
                string SERVER_IP = serviceConfiguration.GetFileLocation("EZcashIP");
                ezCashclient = new TcpClient(SERVER_IP, Convert.ToInt32(serviceConfiguration.GetFileLocation("EZcashPort")));
                clientStream = ezCashclient.GetStream();
                listenThread = new Thread(ReceiveMessage);
                listenThread.Start();
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogWithNoLock($"Client socket CreateEZCashSocket {ex.Message} ");
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
                            //var responsefromHost = Encoding.ASCII.GetString(bytesToRead, 0, bytesRead);
                            string ezCashResponse = utilities.ByteToHexaEZCash(bytesToRead.Skip(2).Take(bytesRead).ToArray(), bytesRead);
                            await ProcessSocketMessage(ezCashResponse);
                            // return ezCashResponse;
                        }
                        else
                        {
                            if (ezCashclient != null)
                            {
                                ezCashclient.Client.Shutdown(SocketShutdown.Both);
                                ezCashclient.Client.Close();
                            }
                            await DisplayErrorMessage("Sorry, Dispenser is temporarily out of service.");
                            cduProcessor.SocketConnected = false;
                            Thread initializeThread = new Thread(() => cduProcessor.cdu.BackgroundInitializing());
                            initializeThread.Start();
                            if (listenThread != null)
                            {
                                if (listenThread.IsAlive)
                                    listenThread.Abort(100);
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        Logger.LogWithNoLock($"Client socket ReceiveMessage {ex.Message} ");
                        if (ezCashclient != null)
                        {
                            ezCashclient.Client.Shutdown(SocketShutdown.Both);
                            ezCashclient.Client.Close();
                        }
                        // ezCashclient = null;
                        await DisplayErrorMessage("Sorry, Dispenser is temporarily out of service.");
                        cduProcessor.SocketConnected = false;
                        Thread initializeThread = new Thread(() => cduProcessor.cdu.BackgroundInitializing());
                        initializeThread.Start();
                        if (listenThread != null)
                        {
                            if (listenThread.IsAlive)
                                listenThread.Abort(100);
                        }
                        //listenThread.Abort();
                        break;
                        // return string.Empty;
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.LogWithNoLock($"Client socket ReceiveMessage {ex.Message} ");
                if (ezCashclient != null)
                {
                    ezCashclient.Client.Shutdown(SocketShutdown.Both);
                    ezCashclient.Client.Close();
                }
                //listenThread.Abort();
                await DisplayErrorMessage("Sorry, Dispenser is temporarily out of service.");
            }
        }

        public void SendMessage(string message)
        {
            try
            {

                byte[] buffer = new byte[ezCashclient.ReceiveBufferSize];
                byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(message); //("1111.000...1 > .38527859440..ABC....");
                clientStream.Write(bytesToSend, 0, bytesToSend.Length);

            }
            catch (Exception ex)
            {
                Logger.LogWithNoLock($"Client socket SendMessage {ex.Message} ");
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
                        await cduProcessor.ProcessBarcodeTransaction(ezCashMessage);
                        break;
                    case "10.": //10.000.000.1
                        if (ezCashMessage.Equals("10.000.000.2.."))
                        {
                            await cduProcessor.ProcessDownRequest(ezCashMessage);

                        }
                        else if (ezCashMessage.Equals("10.000.000.1.."))
                        {
                            await cduProcessor.ProcessUpRequest(ezCashMessage);

                        }
                        else if (ezCashMessage.Equals("10.000.000.7.."))
                        {
                            await cduProcessor.ProcessConfigRequest(ezCashMessage);

                        }
                        break;
                    default:
                        await cduProcessor.ProcessCassetteStatus(ezCashMessage);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWithNoLock($"Client socket ReceiveMessage {ex.Message} ");
                //10.000.000.7
            }

        }

        public async Task DisplayErrorMessage(string message)
        {
            try
            {

                await Task.Run(() =>
                {
                    var lblMessage1 = cduProcessor.cdu.Controls.Find("lblMessage1", true).FirstOrDefault();
                    var lblMessage2 = cduProcessor.cdu.Controls.Find("lblMessage2", true).FirstOrDefault();
                    var pnlMessage = cduProcessor.cdu.Controls.Find("pnlMessage", true).FirstOrDefault();

                    if (null != lblMessage1 && lblMessage1 is Label && null != pnlMessage && pnlMessage is Panel)
                    {

                        cduProcessor.cdu.BeginInvoke((Action)delegate ()
                        {
                            lblMessage2.Text = string.Empty;
                            (lblMessage1 as Label).Text = string.Empty;
                            (lblMessage1 as Label).Text = message;
                            (lblMessage1 as Label).Font = new Font("Calibri", 20, FontStyle.Regular);
                            (lblMessage1 as Label).SetBounds(((pnlMessage as Panel).ClientSize.Width - (lblMessage1 as Label).Width) / 2, ((pnlMessage as Panel).ClientSize.Height - (lblMessage1 as Label).Height) / 2, 0, 0, BoundsSpecified.Location);

                        });
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.LogWithNoLock($"Client socket ReceiveMessage {ex.Message} ");
            }
        }
    }
}
