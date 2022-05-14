using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Octokit;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace Berlex.Devtools
{
    public static class release
    {
        // Create a single, static HttpClient
        private static HttpClient httpClient = new HttpClient();

        [FunctionName("release")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{target}/{version}")] HttpRequest req,
            string target, string version,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var github = new GitHubClient(new ProductHeaderValue("berlexdevtools"));
            var releases = await github.Repository.Release.GetAll("Nickztar", "berlexdevtools");
            if (releases.Any())
            {
                var latest = releases[0];
                var releaseTag = latest.TagName.Replace("app-v", "");
                if (Version.TryParse(releaseTag, out Version releaseVersion) && Version.TryParse(version, out Version currentVersion) && releaseVersion > currentVersion)
                {
                    if (latest.Assets.Any())
                    {
                        var targetedAsset = latest.Assets.AssetForTarget(target);
                        var assetSignature = latest.Assets.SigForTarget(target);
                        if (targetedAsset != null && assetSignature != null)
                        {
                            var signature = await httpClient.GetStringAsync(assetSignature.BrowserDownloadUrl);
                            var result = new UpdateResult
                            {
                                Version = releaseTag,
                                Url = targetedAsset.BrowserDownloadUrl,
                                Signature = signature,
                                PublishDate = latest.PublishedAt?.DateTime ?? DateTime.UtcNow,
                                Notes = latest.Body
                            };
                            var json = JsonConvert.SerializeObject(result);

                            return new HttpResponseMessage(HttpStatusCode.OK)
                            {
                                Content = new StringContent(json, Encoding.UTF8, "application/json")
                            };

                        }
                    }
                }
            }

            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }

        static Dictionary<string, string> Matches = new(){
            { "windows", "msi.zip" },
            { "macos", "app.tar.gz" },
            { "linux", "appimage.tar.gz" }
        };
        public static ReleaseAsset AssetForTarget(this IReadOnlyList<ReleaseAsset> assets, string target)
        {
            return assets.FirstOrDefault(asset => !asset.Name.EndsWith(".sig") && asset.Name.ToLower().Contains(Matches[target.ToLower()]));
        }
        public static ReleaseAsset SigForTarget(this IReadOnlyList<ReleaseAsset> assets, string target)
        {
            return assets.FirstOrDefault(asset => asset.Name.EndsWith(".sig") && asset.Name.ToLower().Contains(Matches[target.ToLower()]));
        }
    }
}
