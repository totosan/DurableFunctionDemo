﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
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
			var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
			var builder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile($"appsettings.json", optional: false, reloadOnChange: true)
				.AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
				.AddEnvironmentVariables();

			IConfigurationRoot configuration = builder.Build();

            var functionCode = configuration.GetSection("codes:fctCode").Value;
			var userdataPhone = configuration.GetSection("userdata:phonenr").Value;
			var userdataEmail = configuration.GetSection("userdata:email").Value;

            var uri = args[1]+ "/api";

            try
            {
                MakeAsync(functionCode, uri, $"{userdataPhone},{userdataEmail}").GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
                Console.ReadKey();
            }
        }

        private static async Task MakeAsync(string functionCode, string uri, string usersData)
        {
            var client = new RestClient(uri);

            Console.WriteLine("Starting call to orchestrator...");

            var response = await SendFirstCallAsync(client, functionCode, usersData);

            if (response.IsSuccessful)
            {
                var responseUrl =(SimpleJson.JsonObject)SimpleJson.SimpleJson.DeserializeObject(response.Content);

                var url = (string)responseUrl["statusQueryGetUri"];
                var restResponse = await GetStatus(url);
                Console.WriteLine(url);
                Console.WriteLine(restResponse.Content);

                Console.WriteLine("Please enter the code: ");
                var code = Console.ReadLine();
                var respSecond = SendSecondCall(code, uri.Replace("/api",""), response.Content);

            }
            else
            {
                Console.WriteLine("Error: "+response.Content);
                Console.Write("Press any key!");
                Console.ReadKey();
            }
        }


        private static Task<IRestResponse> GetStatus(string url)
        {
            var client = new RestClient(url);
            var req1 = new RestRequest(Method.GET);
            req1.AddHeader("Cache-Control", "no-cache");
            req1.AddHeader("Content-Type", "application/json");


            IRestResponse response = client.Execute(req1);
            return Task.FromResult(response);
        }

        private static Task<IRestResponse> SendFirstCallAsync(RestClient client, string functionCode, string usersData)
        {
            var req1 = new RestRequest("orchestrators/O_EMailPhoneVerification", Method.POST);
            req1.AddHeader("x-functions-key", functionCode);
            req1.AddHeader("Cache-Control", "no-cache");
            req1.AddHeader("Content-Type", "application/json");

            req1.AddParameter("undefined", SimpleJson.SimpleJson.SerializeObject(usersData), ParameterType.RequestBody);

            IRestResponse response = client.Execute(req1);
            return Task.FromResult(response);
        }

        private static IRestResponse SendSecondCall(string code,string uri, string returnUris)
        {
			RestClient client = new RestClient();
			var req1 = new RestRequest(uri+"/approve/{code}", Method.GET);
            req1.AddUrlSegment("code", code);
            req1.AddHeader("Cache-Control", "no-cache");
            //req1.AddHeader("Content-Type", "application/json");

            IRestResponse response = client.Execute(req1);
            return response;
        }
    }
}
