using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aliyun.Acs.Core;
using Aliyun.Acs.Core.Exceptions;
using Aliyun.Acs.Core.Profile;
using Aliyun.Acs.dcdn.Model.V20180115;
using Beholder.Models.PathTree;
using Beholder.Options.Handler;
using Cysharp.Text;
using Microsoft.Extensions.Logging;

namespace Beholder.Services.Handler
{
    public class AliYunCdnHandler : IHandler, IDisposable, IAsyncDisposable
    {
        private readonly DefaultAcsClient client;
        private readonly ILogger logger;
        private readonly AliYunCdnOptions options;

        public AliYunCdnHandler(AliYunCdnOptions options, ILogger<AliYunCdnHandler> logger)
        {
            this.options = options;

            this.logger = logger;

            var profile = DefaultProfile.GetProfile(options.RegionId, options.AccessKeyId, options.Secret);
            client = new DefaultAcsClient(profile);

            logger.LogInformation("AliYunCdn Acs Client initialized at: {time}", DateTimeOffset.Now);
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public void Dispose()
        {
        }

        public async Task<bool> RefreshAsync(IEnumerable<PathNode> paths, CancellationToken stoppingToken = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            cts.CancelAfter(options.TotalTimeout);

            var allSuccess = true;
            var pathList = paths.ToList();

            var filePaths = pathList.Where(p => !p.IsDirectory).ToList();
            if (filePaths.Any())
            {
                var fileRequest = new RefreshDcdnObjectCachesRequest
                {
                    ObjectPath = ZString.Join('\n', filePaths.Select(p => string.Concat(options.Prefix, p.Path))),
                    ObjectType = "File"
                };

                allSuccess = await CallAcsApiAsync(fileRequest, "Refresh files", cts.Token);
            }

            if (cts.IsCancellationRequested || !allSuccess)
            {
                logger.LogInformation("Refresh task canceled due to stoppingToken");
                return false;
            }

            var dirPaths = pathList.Where(p => p.IsDirectory && p.WillForceUpdate).ToList();
            if (dirPaths.Any())
            {
                var dirRequest = new RefreshDcdnObjectCachesRequest
                {
                    // Ensure trailing /.
                    ObjectPath = ZString.Join('\n',
                        dirPaths.Select(p =>
                            Path.EndsInDirectorySeparator(p.Path)
                                ? string.Concat(options.Prefix, p.Path)
                                : string.Concat(options.Prefix, p.Path, Path.AltDirectorySeparatorChar))),
                    ObjectType = "Directory"
                };

                // If files request failed, then skip calling for the directory request.
                allSuccess = allSuccess && await CallAcsApiAsync(dirRequest, "Refresh directories", cts.Token);
            }

            return allSuccess;
        }

        public async Task<bool> PreloadAsync(IEnumerable<PathNode> paths, CancellationToken stoppingToken = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            cts.CancelAfter(options.TotalTimeout);

            var success = true;
            var pathList = paths.ToList();

            // Can only preload files.
            var filePaths = pathList.Where(p => !p.IsDirectory).ToList();
            if (filePaths.Any())
            {
                var fileRequest = new PreloadDcdnObjectCachesRequest
                {
                    ObjectPath = ZString.Join('\n', filePaths.Select(p => string.Concat(options.Prefix, p.Path)))
                };

                success = await CallAcsApiAsync(fileRequest, "Preload files", cts.Token);
            }

            return success;
        }

        private async Task<bool> CallAcsApiAsync<T>(AcsRequest<T> request,
            string taskName,
            CancellationToken stoppingToken = default)
            where T : AcsResponse
        {
            var success = false;

            // Pay attention to thread safety check here.
            try
            {
                success = await Task.Run(() => CallAcsApi(request, taskName), stoppingToken);
            }
            catch (OperationCanceledException oce)
            {
                logger.LogError(oce, "Calling Acs Api for {taskName} timeout at: {time}", taskName, DateTimeOffset.Now);
            }

            return success;
        }

        private bool CallAcsApi<T>(AcsRequest<T> request, string taskName)
            where T : AcsResponse
        {
            var fail = false;

            try
            {
                var response = client.GetAcsResponse(request, true, options.MaxRetry);
                var taskId = response switch
                {
                    RefreshDcdnObjectCachesResponse refreshResp => refreshResp.RefreshTaskId,
                    PreloadDcdnObjectCachesResponse preloadResp => preloadResp.PreloadTaskId,
                    _ => "Unknown Response"
                };

                logger.LogInformation("{taskName} with task id: {id}", taskName, taskId);
            }
            catch (ServerException sEx)
            {
                fail = true;
                logger.LogError(sEx, "{taskName} but server errors", taskName);
            }
            catch (ClientException cEx)
            {
                fail = true;
                logger.LogError(cEx, "{taskName} but client errors", taskName);
            }

            return !fail;
        }
    }
}
