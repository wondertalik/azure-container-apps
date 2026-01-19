using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using FunctionApp1.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp1.Functions;

public class ServiceBusTriggerOne(
    ILogger<ServiceBusTriggerOne> logger,
    FunctionApp1Instrumentation instrumentation)
{
    private readonly ActivitySource _activitySource = instrumentation.ActivitySource;

    [Function(nameof(ServiceBusTriggerOne))]
    public async Task Run(
        [ServiceBusTrigger(queueName: "%ServiceBusOptions:ServiceBusTriggerOne%",
            Connection = "ServiceBusOptions:ConnectionString")]
        ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions)
    {
        Activity? act = Activity.Current;
        if (act != null)
        {
            act.DisplayName = "Func.ServiceBusTriggerOne";
        }

        using Activity? activity = _activitySource.StartActivity("Func.ServiceBusTriggerOne.Start");
        
        string body = message.Body.ToString();

        logger.LogInformation("Received Service Bus message: {Body}", body);
        
        await messageActions.CompleteMessageAsync(message);
    }
}