using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BR.StorageSync.Service.Classes;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace BR.StorageSync.Service
{
    public class MonitorInstance
    {

        public string Name => _monitoredPath.Path;
        private readonly MonitoredPath _monitoredPath;
        private PhysicalFileProvider _fileProvider;
        private ILogger _logger;

        internal MonitorInstance(MonitoredPath monitoredPath, ILogger<MonitorInstance> logger)
        {
            _monitoredPath = monitoredPath;
            _logger = logger;
        }

        internal async Task<int> Initialize()
        {
            //Create the FileManager
            _fileProvider = new PhysicalFileProvider(_monitoredPath.Path);
            var newToken = _fileProvider.Watch("**/*");
            newToken.RegisterChangeCallback(async _ =>
            {
                await Run();
            }, null);

            return 0;
        }

        private async Task Run()
        {

            await CheckChanges("", true);

            //Re-attach, may redo with a timer and polling instead
            {
                var newToken = _fileProvider.Watch("**/*");
                newToken.RegisterChangeCallback(async x =>
                {
                    await Run();
                }, null);
            }
        }

        private async Task CheckChanges(string path, bool recursive = true)
        {

            _logger.LogTrace($"Checking Path #{path}#");
            try
            {
                //Grab the contents of this path
                var contents = _fileProvider.GetDirectoryContents(path);

                string root = string.IsNullOrWhiteSpace(path) ? "" : $"{path}/";


                //Did files change?
                {
                    //Grab the Blobs in the matching path
                    var files = contents.Where(f => !f.IsDirectory).ToList();
                    BlobServiceClient blobServiceClient = new BlobServiceClient(_monitoredPath.Settings.ConnectionString);
                    var containerClient = blobServiceClient.GetBlobContainerClient(_monitoredPath.Settings.DestinationContainer);
                    List<BlobItem> blobItems = new();
                    await foreach (BlobHierarchyItem blobItem in containerClient.GetBlobsByHierarchyAsync(prefix: root, delimiter: "/"))
                    {
                        if (blobItem.IsBlob) { blobItems.Add(blobItem.Blob); }
                    }

                    //Add files that are missing
                    var provider = new FileExtensionContentTypeProvider();
                    var missingFiles = files.Where(f => blobItems.All(b => b.Name != $"{root}{f.Name}")).ToList();
                    _logger.LogTrace($"Found {missingFiles.Count} Missing Files");
                    foreach (var missingFile in missingFiles)
                    {

                        //Get and Validate the content type
                        if (!provider.TryGetContentType(missingFile.PhysicalPath, out string contentType))
                        {
                            continue;
                        }

                        //Upload
                        _logger.LogTrace($"uploading #{missingFile.Name}#");
                        using var readStream = missingFile.CreateReadStream();
                        var blob = containerClient.GetBlobClient($"{root}{missingFile.Name}");
                        var uploadResult = await blob.UploadAsync(
                            readStream,
                            new BlobHttpHeaders
                            {
                                ContentType = contentType
                            },
                            conditions: null);
                        _logger.LogTrace($"Uploaded - {uploadResult.Value.LastModified}");
                    }
                }

                //Continue down the tree, if we should
                if (recursive)
                {
                    foreach (var directory in contents.Where(fi => fi.IsDirectory))
                    {
                        await CheckChanges($"{root}{directory.Name}");
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, null);
            }
            

        }

    }
}
