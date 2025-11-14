using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TrustFlow.Core.Communication;

namespace TrustFlow.Core.Services
{
    public class RedisCacheService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _database;
        private readonly ILogger<RedisCacheService> _logger;

        public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
        {
            var _redis = ConnectionMultiplexer.Connect(
                new ConfigurationOptions
                {
                    EndPoints = { { "redis-19452.crce206.ap-south-1-1.ec2.cloud.redislabs.com", 19452 } },
                    User = "default",
                    Password = "Md2UxLt2AIpcsT4pPb5LHUfhHkf9r9Ca"
                }
            );
            var db = _redis.GetDatabase();
            //_redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _database = _redis.GetDatabase();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ServiceResult> SetCacheAsync(string key, string value, TimeSpan? expiry = null)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                {
                    throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));
                }

                await _database.StringSetAsync(key, value, expiry);

                return new ServiceResult(true, "Successfully Setted the data in Redis");
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogError(ex, "Redis connection error while setting cache for key: {Key}", key);
                return new ServiceResult(false, "Failed to connect to Redis server.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while setting cache for key: {Key}", key);
                return new ServiceResult(false, "An unexpected error occurred while setting cache.");
            }
        }

        public async Task<ServiceResult> GetCacheAsync(string key)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));

                var data = await _database.StringGetAsync(key);
                if (data.IsNullOrEmpty)
                {
                    return new ServiceResult(false, "Cache miss: No data found for the given key.");
                }
                return new ServiceResult(true, "Cache hit: Data retrieved successfully.", data.ToString());
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogError(ex, "Redis connection error while getting cache for key: {Key}", key);
                return new ServiceResult(false, "Failed to connect to Redis server.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while setting cache for key: {Key}", key);
                return new ServiceResult(false, "An unexpected error occurred while setting cache.");
            }
        }

        public async Task<ServiceResult> RemoveCacheAsync(string key)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));

                await _database.KeyDeleteAsync(key);
                return new ServiceResult(true, "Successfully removed the data from Redis");
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogError(ex, "Redis connection error while removing cache for key: {Key}", key);
                return new ServiceResult(false, "Failed to connect to Redis server.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while removing cache for key: {Key}", key);
                return new ServiceResult(false, "An unexpected error occurred while removing cache.");
            }
        }
    }
}
