using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace FujitsuCDU.Common
{
    public class API
    {
        private readonly ServiceConfiguration serviceConfiguration = new ServiceConfiguration();
        private readonly Logger logger = new Logger();
        public T GetRequest<T>(string param, string devId, out bool databaseError)
        {
            string responseBody = string.Empty;

            try
            {
                //var httpClientHandler = new HttpClientHandler();
                //httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) =>
                //{
                //    return true;
                //};
                // ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3 | SecurityProtocolType.SystemDefault;
                // ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => { return true; };
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                //var handler = new HttpClientHandler();
                //handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                //handler.ServerCertificateCustomValidationCallback =
                //    (httpRequestMessage, cert, cetChain, policyErrors) =>
                //    {
                //        return true;
                //    };


                using (var client = new HttpClient())
                {
                    //client.DefaultRequestHeaders.Add("Token", serviceConfiguration.GetFileLocation("EZCashToken"));

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", serviceConfiguration.GetFileLocation("EZCashToken"));
                    client.Timeout = TimeSpan.FromSeconds(10);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var method = serviceConfiguration.GetFileLocation("EZCashAPI") + param;
                    using (HttpResponseMessage response = client.GetAsync(method).Result)
                    {
                        response.EnsureSuccessStatusCode();
                        if (response.IsSuccessStatusCode)
                        {
                            responseBody = response.Content.ReadAsStringAsync().Result;
                            //LogEvents($"Response : {responseBody}");
                        }
                        else
                            LogEvents($"Dev {devId} :There was an error while trying to access the database.{response.ReasonPhrase}");


                    }
                }
            }
            catch (HttpRequestException ex)
            {
                databaseError = true;
                LogEvents($"There was an error while trying to access the database. Parameters : {param} , Exception : {ex.Message}");
                return JsonConvert.DeserializeObject<T>(responseBody);

            }
            catch (TaskCanceledException ex)
            {
                databaseError = true;
                LogEvents($"There was an error while trying to access the database. Parameters : {param} , Exception : {ex.Message}");
                return JsonConvert.DeserializeObject<T>(responseBody);

            }
            catch (Exception ex)
            {
                databaseError = true;
                LogEvents($"There was an error while trying to access the database. Parameters : {param} , Exception : {ex.Message}");
                return JsonConvert.DeserializeObject<T>(responseBody);

            }
            databaseError = false;
            return JsonConvert.DeserializeObject<T>(responseBody);


        }

        public T PutRequest<T>(T updateItem, string param, out bool databaseError)
        {
            string responseBody = string.Empty;

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                //ServicePointManager.ServerCertificateValidationCallback +=
                //    (sender, cert, chain, sslPolicyErrors) => { return true; };

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", serviceConfiguration.GetFileLocation("EZCashToken"));
                    //client.DefaultRequestHeaders.Add("Token", serviceConfiguration.GetFileLocation("EZCashToken"));
                    client.Timeout = TimeSpan.FromSeconds(10);
                    var method = serviceConfiguration.GetFileLocation("EZCashAPI") + param;
                    using (HttpResponseMessage response = client.PutAsync(method, updateItem, new JsonMediaTypeFormatter()).Result)
                    {
                        response.EnsureSuccessStatusCode();
                        if (response.IsSuccessStatusCode)
                        {
                            responseBody = response.Content.ReadAsStringAsync().Result;
                        }
                        else
                            LogEvents($"There was an error while trying to access the database.{response.ReasonPhrase}");
                    }

                }
            }
            catch (HttpRequestException ex)
            {
                databaseError = true;
                LogEvents($"There was an error while trying to access the database. Parameters : {param} , Exception : {ex.Message}");
                return JsonConvert.DeserializeObject<T>(responseBody);

            }
            catch (TaskCanceledException ex)
            {
                databaseError = true;
                LogEvents($"There was an error while trying to access the database. Parameters : {param} , Exception : {ex.Message}");
                return JsonConvert.DeserializeObject<T>(responseBody);

            }
            catch (Exception ex)
            {
                databaseError = true;
                LogEvents($"There was an error while trying to access the database. Parameters : {param} , Exception : {ex.Message}");
                return JsonConvert.DeserializeObject<T>(responseBody);

            }
            databaseError = false;
            return JsonConvert.DeserializeObject<T>(responseBody);


        }

        public T PostRequest<T>(T insertItem, string param, out bool databaseError, string apiName = "EZCashAPI")
        {
            string responseBody = string.Empty;
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                //ServicePointManager.ServerCertificateValidationCallback +=
                //    (sender, cert, chain, sslPolicyErrors) => { return true; };
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", serviceConfiguration.GetFileLocation("EZCashToken"));
                    client.Timeout = TimeSpan.FromSeconds(120);
                    var method = apiName == "CoinDispenserAPI" ? serviceConfiguration.GetFileLocation("CoinDispenserAPI") + param : serviceConfiguration.GetFileLocation("EZCashAPI") + param;
                    //LogEvents($"Method : {method}");
                    //var response = client.PostAsync(method, insertItem, new JsonMediaTypeFormatter()).Result;
                    //LogEvents($"{JsonConvert.SerializeObject(insertItem)}");
                    using (HttpResponseMessage response = client.PostAsync(method, insertItem, new JsonMediaTypeFormatter()).Result)
                    {
                        response.EnsureSuccessStatusCode();
                        if (response.IsSuccessStatusCode)
                        {
                            responseBody = response.Content.ReadAsStringAsync().Result;
                        }
                        else
                            LogEvents($"There was an error while trying to access the database.{response.ReasonPhrase}");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                databaseError = true;
                LogEvents($"There was an error while trying to access the database. Parameters : {param} , Exception : {ex.Message}");
                return JsonConvert.DeserializeObject<T>(responseBody);

            }
            catch (TaskCanceledException ex)
            {
                databaseError = true;
                LogEvents($"There was an error while trying to access the database. Parameters : {param} , Exception : {ex.Message}");
                return JsonConvert.DeserializeObject<T>(responseBody);

            }
            catch (Exception ex)
            {
                databaseError = true;
                LogEvents($"There was an error while trying to access the database. Parameters : {param} , Exception : {ex.Message}");
                return JsonConvert.DeserializeObject<T>(responseBody);

            }
            databaseError = false;
            return JsonConvert.DeserializeObject<T>(responseBody);
        }

        private void LogEvents(string input)
        {
            logger.Log($"{DateTime.Now:MM-dd-yyyy HH:mm:ss}:{input}");
        }
    }
}
