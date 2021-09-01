using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FujitsuCDU.EZCashModule
{
    public class EZCashHandler
    {
        private readonly Logger logger = new Logger();
        Utilities utilities = new Utilities();
        public Thread listenThread;
        TcpClient client = new TcpClient("192.168.254.18", 3450);

        public void ConnectEZCashService()
        {
            try
            {

                NetworkStream nwStream = client.GetStream();
                LogEvents("Got connection from 192.168.254.18.");
                var dispenseReq = new DispenseRequest() { Barcode = "", Ip = "", Port = 3455 };

                var dispenseMessage = JsonConvert.SerializeObject(dispenseReq);
                byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(dispenseMessage);

                Logger.LogWithNoLock($"{DateTime.Now:MM-dd-yyyy HH:mm:ss}: Dev 99 : sending messages .{dispenseReq.Barcode}");
                nwStream.Write(bytesToSend, 0, bytesToSend.Length);


                byte[] bytesToRead = new byte[client.ReceiveBufferSize];
                int bytesRead = nwStream.Read(bytesToRead, 0, client.ReceiveBufferSize);


                Logger.LogWithNoLock($"{DateTime.Now:MM-dd-yyyy HH:mm:ss}: Dev 99 : reading response .");
                string ezCashResponse = utilities.ByteToHexaEZCash(bytesToRead.Take(bytesRead).ToArray(), bytesRead);
                // client.Close();




            }
            catch (Exception ex)
            {
                LogEvents($"There was an error while trying to connect EZCash Service. Exception : {ex.Message}");
            }

        }


        private void EnableEvents()
        {
            try
            {
                LogEvents($" Dev {99}:  Waiting for data in... ");
                listenThread = new Thread(ListenCommunication);
                listenThread.Start();
            }
            catch (Exception)
            {

                throw;
            }

        }

        public void ListenCommunication()
        {
            try
            {
                while (client != null)
                {
                    byte[] bytes = new byte[client.ReceiveBufferSize];
                    int byteCount = ReceiveData(client, bytes);


                    if (byteCount > 0)
                    {
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public int ReceiveData(TcpClient socket, byte[] bytes)
        {
            //int count = socket.Receive(bytes, SocketFlags.None);

            return 0;
        }

        private void LogEvents(string input)
        {
            Logger.LogWithNoLock($"{DateTime.Now:MM-dd-yyyy HH:mm:ss}:{input}");
        }
    }
}
