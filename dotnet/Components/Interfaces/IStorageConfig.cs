using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UA.loops.shared.storage;

namespace TeamHitori.Mulplay.Container.Web.Components.Interfaces
{
    public interface IStorageConfig
    {
        public DocumentDBRepository DBRepository { get; }

        public BlobServiceClient BlobServiceClient { get; }

        public StorageSharedKeyCredential StorageSharedKeyCredential { get;  }

        public IDatabase Cache { get; }

        public ILogger Logger { get; }
    }
}
