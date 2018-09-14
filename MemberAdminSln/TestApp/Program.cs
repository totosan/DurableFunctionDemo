using System;
using System.Net.Http;
using System.Net.Mime;
using Microsoft.Extensions.Configuration;
using RestSharp;

namespace TestApp
{
    class Program
    {

        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: TestApp.exe -uri <URI>");
                return;
            }

            var uri = args[1] + "/api";

            var client = new RestClient(uri);

            Console.WriteLine("Starting call to orchestrator...");

            IRestResponse response = SendFirstCall(client);

            if (response.IsSuccessful)
            {
                Console.WriteLine(response.Content);
                Console.WriteLine("Please enter the code: ");
                var code = Console.ReadLine();
                var respSecond = SendSecondCall(code, client, response.Content);

            }
        }


        private static IRestResponse SendFirstCall(RestClient client)
        {
            var req1 = new RestRequest("orchestrators/E4_SmsPhoneVerification", Method.POST);
            req1.AddHeader("x-functions-key", "lw3mEfy8zXlQj3cRnstiXqYkyJ5yzCz0eHrLTEcLvsp37dz9lbTH6Q==");
            req1.AddHeader("Cache-Control", "no-cache");
            req1.AddHeader("Content-Type", "application/json");

            req1.AddParameter("undefined", SimpleJson.SimpleJson.SerializeObject("+4915153811045,toto_san@live.com"), ParameterType.RequestBody);
            //req1.AddHeader("Postman-Token", "414d87e7-e9a1-4cc9-9501-c6db9c96f130");

            IRestResponse response = client.Execute(req1);
            return response;
        }

        private static IRestResponse SendSecondCall(string code, RestClient client, string returnUris)
        {
            var responseUrl = SimpleJson.SimpleJson.DeserializeObject(returnUris);
            var req1 = new RestRequest("approve/{code}", Method.POST);
            req1.AddUrlSegment("code", code);
            req1.AddHeader("Cache-Control", "no-cache");
            //req1.AddHeader("Content-Type", "application/json");

            IRestResponse response = client.Execute(req1);
            return response;
        }
    }
}
