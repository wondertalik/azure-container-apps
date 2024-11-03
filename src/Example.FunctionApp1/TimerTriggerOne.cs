using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Example.FunctionApp1;

public class TimerTriggerOne(ILogger<TimerTriggerOne> logger)
{
    [Function("TimerTriggerOne")]
    public void Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
    {
        logger.LogInformation("C# Timer trigger function executed at: {Now}", DateTime.Now);

        if (myTimer.ScheduleStatus is not null)
        {
            logger.LogInformation("Next timer schedule at: {ScheduleStatusNext}", myTimer.ScheduleStatus.Next);
        }
    }
}