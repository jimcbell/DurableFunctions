using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace IsolatedFunctions
{
    public static class FunctionChaining
    {
        [Function(nameof(FunctionChaining))]
        public static async Task<int> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(FunctionChaining));
            logger.LogInformation("Function Chaining Beginning");

            int x = await context.CallActivityAsync<int>(nameof(Start), null);
            int y = await context.CallActivityAsync<int>(nameof(Times2), x);
            int z = await context.CallActivityAsync<int>(nameof(Minus1), y);

            return z;
        }
        /// <summary>
        /// A Durable Actity Function that responds to an ActivityTrigger - (i.e. being called by an orchestrator)
        /// Returns the input or 10 if the input is null.
        /// </summary>
        /// <param name="n"></param>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        [Function(nameof(Start))]
        public static int Start([ActivityTrigger] int? n, FunctionContext executionContext)
        {
            int defaultValue = 10;
            return n.HasValue ? n.Value : defaultValue;
        }
        /// <summary>
        /// A Durable Actity Function that responds to an ActivityTrigger - (i.e. being called by an orchestrator)
        /// Returns the input * 2
        /// </summary>
        /// <param name="n"></param>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        [Function(nameof(Times2))]
        public static int Times2([ActivityTrigger] int n, FunctionContext executionContext) => n * 2;
        /// <summary>
        /// A Durable Actity Function that responds to an ActivityTrigger - (i.e. being called by an orchestrator)
        /// Returns the input - 1
        /// </summary>
        /// <param name="n"></param>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        [Function(nameof(Minus1))]
        public static int Minus1([ActivityTrigger] int n, FunctionContext executionContext) => n - 1;



        /// <summary>
        /// This is the client of the Durable Function.
        /// It controls starting it and is an Http Trigger.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="client"></param>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        [Function("FunctionChaining_HttpStart")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("Function_HttpStart");

            // Function input comes from the request content.
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(FunctionChaining));

            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            // Returns an HTTP 202 response with an instance management payload.
            // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
            return await client.CreateCheckStatusResponseAsync(req, instanceId);
        }
    }
}
