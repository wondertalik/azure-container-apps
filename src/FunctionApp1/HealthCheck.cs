using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FunctionApp1;

public class HealthCheck(ILogger<HealthCheck> logger)
{
    [Function("HealthCheck")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
    {
        logger.LogInformation("Health check requested");

        try
        {
            HealthCheckResponse healthStatus = GetHealthStatus();
            return new OkObjectResult(healthStatus);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Health check failed: {ExMessage}", ex.Message);

            return new ObjectResult(new HealthCheckResponseError(
                Status: "Unhealthy",
                Version: Environment.GetEnvironmentVariable("OTEL_SERVICE_VERSION") ?? "Unknown",
                Timestamp: DateTimeOffset.UtcNow,
                Service: "OptiLeads.LeadsProcessor",
                ErrorMessage: ex.Message
            ))
            {
                StatusCode = (int?) HttpStatusCode.ServiceUnavailable
            };
        }
    }

    private static HealthCheckResponse GetHealthStatus()
    {
        string version = Environment.GetEnvironmentVariable("OTEL_SERVICE_VERSION") ?? "Unknown";

        return new HealthCheckResponse(
            Status: "Healthy",
            Version: version,
            Timestamp: DateTimeOffset.UtcNow,
            Service: "OptiLeads.LeadsProcessor"
        );
    }

    private record HealthCheckResponse(string Status, string Version, DateTimeOffset Timestamp, string Service);

    private record HealthCheckResponseError(
        string Status,
        string Version,
        DateTimeOffset Timestamp,
        string Service,
        string ErrorMessage
    );
}