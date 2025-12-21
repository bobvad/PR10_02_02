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
        static string ClientId = "0199d470-bb93-7ce2-b0df-620ead27395d";
        static string AuthorizationKey = "MDE5OWQ0NzAtYmI5My03Y2UyLWIwZGYtNjIwZWFkMjczOTVkOjZkYWIzODE4LTgyZDQtNGMwZS05NDRjLTQ0MzY1NWVjODg1YQ==";
       
        static async Task Main(string[] args)
        {
            Console.WriteLine("GigaChat Диалог");
            Console.WriteLine("Команды: /clear - очистить историю, /exit - выход\n");

            string Token = await GetToken(ClientId, AuthorizationKey);
               List<Models.Request.Message> messageHistory = new List<Models.Request.Message>();
            if (Token == null)
            {
                Console.WriteLine("Не удалось получить токен");
                return;
            }

            while (true)
            {
                Console.Write("Вы: ");
                string userMessage = Console.ReadLine();

                if (userMessage.ToLower() == "/exit") break;
                if (userMessage.ToLower() == "/clear")
                {
                    messageHistory.Clear();
                    Console.WriteLine("История очищена\n");
                    continue;
                }

                messageHistory.Add(new Models.Request.Message()
                {
                    role = "user",
                    content = userMessage
                });

                ResponseMessage answer = await GetAnswer(Token, messageHistory);

                if (answer?.choices?.Count > 0)
                {
                    string response = answer.choices[0].message.content;

                    messageHistory.Add(new Models.Request.Message()
                    {
                        role = "assistant",
                        content = response
                    });

                    Console.WriteLine($"\nGigaChat: {response}\n");

                    if (messageHistory.Count > 20)
                    {
                        var systemMsg = messageHistory.FirstOrDefault(m => m.role == "system");
                        messageHistory = messageHistory
                            .Where(m => m.role == "system")
                            .Concat(messageHistory
                                .Where(m => m.role != "system"))
                            .ToList();
                    }
                }
                else
                {
                    Console.WriteLine("Ошибка получения ответа\n");
                }
            }
        }

        public static async Task<string> GetToken(string rqUID, string bearer)
        {
            string returnToken = null;
            string url = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";

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
        public static async Task<ResponseMessage> GetAnswer(string token, List<Models.Request.Message> history)
        {
            ResponseMessage responseMessage = null;
            string Url = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";

            using (HttpClientHandler Handler = new HttpClientHandler())
            {
                Handler.ServerCertificateCustomValidationCallback = (messages, cert, chain, sslPolicyErrors) => true;

                using (HttpClient Client = new HttpClient(Handler))
                {
                    HttpRequestMessage Request = new HttpRequestMessage(HttpMethod.Post, Url);

                    Request.Headers.Add("Accept", "application/json");
                    Request.Headers.Add("Authorization", $"Bearer {token}");

                    Models.Request DataRequest = new Models.Request()
                    {
                        model = "GigaChat",
                        stream = false,
                        repetition_penalty = 1,
                        messages = history
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
