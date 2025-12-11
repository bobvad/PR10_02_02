using APIGigaChat.Models.Response;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace APIGigaChat
{
    internal class Program
    {
        static string ClientId = "***";
        static string AuthorizationKey = "***";
        static async Task Main(string[] args)
        {
            string Token = await GetToken(ClientId, AuthorizationKey);

            if (Token == null)
            {
                Console.WriteLine("Не удалось получить токен");
                return;
            }

            while (true)
            {
                Console.Write("Сообщение: ");
                string Message = Console.ReadLine();
                ResponseMessage Answer = await GetAnswer(Token, Message);
                Console.WriteLine("Ответ: " + Answer.choices[0].message.content);
            }
        }

        public static async Task<string> GetToken(string rqUID, string bearer)
        {
            string returnToken = null;
            string url = "https://ngw.devices.sberbank.ru:9WU3/api/v2/oauth";

            using (HttpClientHandler handler = new HttpClientHandler())
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;

                using (HttpClient client = new HttpClient(handler))
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);

                    request.Headers.Add("Accept", "application/json");
                    request.Headers.Add("RqUID", rqUID);
                    request.Headers.Add("Authorization", $"Bearer {bearer}");

                    var data = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("scope", "GIGACHAT_API_PERS")
            };

                    request.Content = new FormUrlEncodedContent(data);

                    HttpResponseMessage response = await client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        ResponseToken token = JsonConvert.DeserializeObject<ResponseToken>(responseContent);
                        returnToken = token.access_token;
                    }
                }
            }
            return returnToken;
        }
        public static async Task<ResponseMessage> GetAnswer(string token, string message)
        {
            ResponseMessage responseMessage = null;
            string Url = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";

            using (HttpClientHandler Handler = new HttpClientHandler())
            {
                Handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;

                using (HttpClient Client = new HttpClient(Handler))
                {
                    HttpRequestMessage Request = new HttpRequestMessage(HttpMethod.Post, Url);

                    Request.Headers.Add("Accept", "application/json");
                    Request.Headers.Add("Authorization", $"Bearer {token}");

                    Request DataRequest = new Request()
                    {
                        model = "GigaChat",
                        stream = false,
                        repetition_penalty = 1,
                        messages = new List<Request.Message>()
                {
                    new Request.Message()
                    {
                        role = "user",
                        content = message
                    }
                }
                    };

                    string JsonContent = JsonConvert.SerializeObject(DataRequest);
                    Request.Content = new StringContent(JsonContent, Encoding.UTF8, "application/json");

                    HttpResponseMessage Response = await Client.SendAsync(Request);

                    if (Response.IsSuccessStatusCode)
                    {
                        string ResponseContent = await Response.Content.ReadAsStringAsync();
                        responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(ResponseContent);
                    }
                }
            }

            return responseMessage;
        }
    }
}
