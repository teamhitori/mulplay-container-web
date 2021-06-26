using Azure.Storage;
using Azure.Storage.Blobs;
using TeamHitori.Mulplay.Container.Web.Components.Interfaces;
using Microsoft.Extensions.Logging;

using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UA.loops.shared.storage;

namespace TeamHitori.Mulplay.Container.Web.Components
{
    public class StorageConfig : IStorageConfig
    {
        public StorageConfig(DocumentDBRepository dBRepository, BlobServiceClient blobServiceClient, StorageSharedKeyCredential storageSharedKeyCredential, ILogger logger, IDatabase cache)
        {
            //var blobServiceClient = new BlobServiceClient(connectionString);
            DBRepository = dBRepository;
            StorageSharedKeyCredential = storageSharedKeyCredential;
            BlobServiceClient = blobServiceClient;
            Logger = logger;
            Cache = cache;
        }
        public DocumentDBRepository DBRepository { get; private set; }

        public BlobServiceClient BlobServiceClient { get; private set; }

        public StorageSharedKeyCredential StorageSharedKeyCredential { get; private set; }

        public IDatabase Cache { get; set; }

        public ILogger Logger { get; }

        
    }

    
}
