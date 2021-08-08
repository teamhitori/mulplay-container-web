using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TeamHitori.Mulplay.shared.storage;


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
