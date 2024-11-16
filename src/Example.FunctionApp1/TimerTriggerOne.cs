using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Example.FunctionApp1;

public class TimerTriggerOne(ILogger<TimerTriggerOne> logger)
{
    [Function("TimerTriggerOne")]
    public void Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
    {
        logger.LogDebug("1_Testing Log Debug");

        logger.LogTrace("1_Testing Log Trace");

        logger.LogInformation("1_Testing Log Information");

        logger.LogWarning("1_Testing Log Warning");

        logger.LogError("1_Testing Log Error");

        logger.LogCritical("1_Testing Log Critical");
        
        if (myTimer.ScheduleStatus is not null)
        {
            logger.LogInformation("Next timer schedule at: {ScheduleStatusNext}", myTimer.ScheduleStatus.Next);
        }
    }
}