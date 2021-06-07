using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FujitsuCDU.Common;

namespace FujitsuCDU.BarcodeProcess
{
    public class FujitsuBarcode
    {
        FujitsuCDUProcessor cduProcessor;//= new FujitsuCDUProcessor();
        static decimal? dispensed = 0;
        static string cassetteCount = string.Empty;
        static readonly List<string> dispensedCoin = new List<string>();
        static readonly List<int> dispenseonFirstTry = new List<int>();
        readonly Logger logger = new Logger();
        readonly Utilities utilities = new Utilities();
        readonly API api = new API();
        readonly ServiceConfiguration serviceConfiguration = new ServiceConfiguration();
        bool databaseIssue = false;
        string TranId = string.Empty;

        public FujitsuBarcode(FujitsuCDUProcessor fujitsuCDUProcessor)
        {
            cduProcessor = fujitsuCDUProcessor;
        }

        public async Task ProcessBarcodeTransaction(string barcode)
        {
            await Task.Run(() =>
            {
                try
                {
                    logger.Log($"Sending Socket transaction request {barcode}..");
                    cduProcessor.CommState = FujitsuCDUProcessor.TCommState.csReady;
                    cduProcessor.IsDispenseReqSent = true;
                    cduProcessor.State = FujitsuCDUProcessor.TState.stWaitTranReply;
                    cduProcessor.ezcashSocket.SendMessage($"1111.000...1 > .{barcode}..ABC....");

                }
                catch (Exception ex)
                {
                    logger.Log($"ParseBarcodeMessage {ex.Message} ");
                }
            });
        }

        public void SendSocketResponse()
        {
            try
            {
                logger.Log($"Sending Socket response..");
                var message = "1111.000...1 > .097846993..ABC....";
                cduProcessor.ezcashSocket.SendMessage(message);
            }
            catch (Exception ex)
            {
                logger.Log($"SendSocketResponse {ex.Message} ");
            }
        }


    }
}
