using Azure.Storage;
using Azure.Storage.Blobs;
using TeamHitori.Mulplay.Container.Web.Components.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Polly;
using StackExchange.Redis;
using System;
using System.Linq;
using UA.loops.shared.storage;

namespace TeamHitori.Mulplay.Container.Web.Components
{
    public static class StorageExtensions
    {
        public static IStorageConfig CreateStorage(
           //this IConfiguration configuration,
           string blobConnectionString,
           string containerName,
           string cacheConnectionString,
           string endpoint,
           string key,
           string databaseId,
           string collectionId,

           ILogger log)
        {

            if (String.IsNullOrEmpty(blobConnectionString))
            {
                throw new ArgumentException("blobConnectionString not set");
            }

            if (String.IsNullOrEmpty(containerName))
            {
                throw new ArgumentException("StorageContainerName not set");
            }

            if (String.IsNullOrEmpty(cacheConnectionString))
            {
                throw new ArgumentException("RedisConnectionString not set");
            }

            var cache = ConnectionMultiplexer.Connect(cacheConnectionString).GetDatabase();

            return CreateStorage(blobConnectionString, endpoint, key, databaseId, collectionId, log, cache);
        }

        public static IStorageConfig CreateStorage(
           string blobConnectionString,
           string endpoint,
           string key,
           string databaseId,
           string collectionId,
           ILogger log,
           IDatabase cache)
        {
            var blobServiceClient = new BlobServiceClient(blobConnectionString);
            var blobConnDict = blobConnectionString.Split(";")
                .Select(x => x.Split("=", 2))
                .ToDictionary(s => s[0], s => s[1]);
            var sharedKeyCred = new StorageSharedKeyCredential(blobConnDict["AccountName"], blobConnDict["AccountKey"]);

            Random jitterer = new Random();
            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetry(6, (retryAttempt, timespan) =>
                {
                    return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                              + TimeSpan.FromMilliseconds(jitterer.Next(0, 100));
                }, (ex, timespan, retryCount, retryContext) =>
                {
                    object methodThatRaisedException = retryContext["methodName"];
                    log.LogError(ex, $"{ methodThatRaisedException }, retry: {retryCount}, timespan: {timespan}");
                });

            var repository = new DocumentDBRepository(
                endpoint,
                key,
                databaseId,
                collectionId,
                log);


            //create storage
            var storageConfig = new StorageConfig(repository, blobServiceClient, sharedKeyCred, log, cache);

            return storageConfig;
        }

        public static Storage ToUserStorage(this IStorageConfig storageConfig, HttpContext httpContext)
        {
            var asUserId = httpContext.User.Claims.FirstOrDefault(claim =>
                    claim.Type == "azp"
                )?.Value ??
                "1111-1111-1111-1111-1111";

            return new Storage(
                storageConfig.DBRepository,
                storageConfig.BlobServiceClient,
                storageConfig.StorageSharedKeyCredential,
                asUserId,
                storageConfig.Logger,
                storageConfig.Cache);
        }

        public static Storage ToUserStorage(this IStorageConfig storageConfig, String asUserId)
        {
            return new Storage(
                storageConfig.DBRepository,
                storageConfig.BlobServiceClient,
                storageConfig.StorageSharedKeyCredential,
                asUserId,
                storageConfig.Logger,
                storageConfig.Cache);
        }
    }
}
