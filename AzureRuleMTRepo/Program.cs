using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus.Administration;
using EventContracts;
using MassTransit;

namespace EventContracts
{
    public interface ValueEntered
    {
        string Value { get; }
    }
}
namespace AzureRuleMTRepo
{
    public class Program
    {
        public static async Task Main()
        {
            var queueName = "mt-test";
            var busConnectionString = ""; // put an azure service bus connection string here

            var busControl = Bus.Factory.CreateUsingAzureServiceBus(cfg =>
            {
                cfg.Message<ValueEntered>(x => x.SetEntityName("values"));
                cfg.Host(busConnectionString);
                cfg.ReceiveEndpoint(queueName, rcfg =>
                {
                    rcfg.Subscribe("values", "test-sub", cb =>
                    {
                        // TODO: Run the application once and send a message, stop the console app, change the criteria in the SqlRuleFilter and run again, inspect the topic subscription
                        // in the azure portal and note that it has not updated
                        cb.Rule = new CreateRuleOptions("test-rule", new SqlRuleFilter("1=1"));
                    });
                });
            });
            
            var source = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            await busControl.StartAsync(source.Token);
            try
            {
                while (true)
                {
                    string value = await Task.Run(() =>
                    {
                        Console.WriteLine("Enter message (or quit to exit)");
                        Console.Write("> ");
                        return Console.ReadLine();
                    });

                    if("quit".Equals(value, StringComparison.OrdinalIgnoreCase))
                        break;

                    await busControl.Publish<ValueEntered>(new
                    {
                        Value = value
                    });
                }
            }
            finally
            {
                await busControl.StopAsync();
            }
        }
    }
}
