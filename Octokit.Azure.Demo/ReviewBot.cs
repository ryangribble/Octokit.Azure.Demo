using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace Octokit.Azure.Demo
{
    public static class ReviewBot
    {
        [FunctionName("ReviewBot")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(WebHookType = "github")]HttpRequestMessage request, TraceWriter log)
        {
            log.Info($"Webhook function executing");

            try
            {
                var message = await ReviewBot.ProcessWebHook(request, log);
                log.Info($"SUCCESS: {message}");
                return request.CreateResponse(HttpStatusCode.OK, message);
            }
            catch (Exception ex)
            {
                log.Info($"ERROR: {ex.Message}");
                return request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        public static async Task<string> ProcessWebHook(HttpRequestMessage request, TraceWriter log)
        {
            // Configure GitHub Access
            var token = Environment.GetEnvironmentVariable("github_token");

            var github = new GitHubClient(new ProductHeaderValue("octokit_azure"))
            {
                Credentials = new Credentials(token)
            };

            log.Info($"Octokit connection initialized");

            // Get webhook event type from header
            var eventType = "unknown";
            try
            {
                eventType = request.Headers.GetValues("X-GitHub-Event").First();
            }
            catch { }

            // Get request body
            // Deserialize webhook payload from body
            dynamic data = null;
            try
            {
                data = await request.Content.ReadAsAsync<object>();
            }
            catch { }

            // Get webhook action from payload
            var action = data?.action ?? "unknown";

            log.Info($"Received GitHub WebHook event '{eventType}' action '{action}'");

            // Process events
            var message = "";
            if (eventType == "issues" && action == "opened")
            {
                // Extract repo/issue details from request body
                string owner = data?.repository?.owner?.login;
                string repo = data?.repository?.name;
                int issueNumber = data?.issue?.number ?? 0;

                log.Info($"Processing {owner}/{repo}#{issueNumber}");

                // Add "to_be_reviewed" label to the issue
                var labelResponse = await github.Issue.Labels.AddToIssue(owner, repo, issueNumber, new[] { "to_be_reviewed" });

                // Add a comment to the issue
                var comment = "## :rotating_light: Review Pending :rotating_light:\nThankyou for your issue, someone will be taking a :eyes: shortly!";
                var commentResponse = await github.Issue.Comment.Create(owner, repo, issueNumber, comment);

                message = $"Issue {owner}/{repo}#{issueNumber} is now under review";
            }
            else
            {
                message = $"No processing required for event '{eventType}' action '{action}'";
            }

            return message;
        }
    }
}
