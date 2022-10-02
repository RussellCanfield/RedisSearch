using Redis_Search.Models;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Redis_Search.Services
{
    public class SearchService : ISearchService
    {
        private readonly Hasher _hasher;
        private readonly IRedisClient _redisClient;

        public SearchService(Hasher hasher,
                             IRedisClient redisClient)
        {
            _hasher = hasher;
            _redisClient = redisClient;
        }

        public async Task<SearchResult> Search(SearchRequest searchRequest)
        {
            var redisDb = _redisClient.GetDb(0);

            IList<RedisKey> keys = new List<RedisKey>();

            var facetHash = new
            {
                Text = searchRequest.Text,
                Facets = searchRequest.Facets
            };

            string cacheKey = $"cache:searchdata:{_hasher.Hash(facetHash)}";
            string searchCacheKey = $"cache:textdata:{_hasher.Hash(searchRequest.Text)}";

            long total = 0;

            bool hasFuzzySearch = !string.IsNullOrWhiteSpace(searchRequest.Text);

            if (hasFuzzySearch)
            {
                var searchQuery = SearchQuery.Create(redisDb.Database, "indexes:vehicles");
                var results = await searchQuery.Find(searchRequest.Text);
                searchQuery.Store(results, searchCacheKey);
            }

            if (!await redisDb.ExistsAsync(cacheKey) &&
                searchRequest.Facets.Length > 0)
            {
                var batch = redisDb.Database.CreateBatch();

                for (var i = 0; i < searchRequest.Facets.Length; i++)
                {
                    var filter = searchRequest.Facets[i];

                    string cacheKeyUnion = $"cache:facetdata:{Guid.NewGuid()}";
                    keys.Add(cacheKeyUnion);

                    IList<RedisKey> unions = new List<RedisKey>(filter.Values.Length);

                    for (var ii = 0; ii < filter.Values.Length; ii++)
                    {
                        var kv = filter.Values[ii];
                        unions.Add($"facets:{filter.Name}:{kv}");
                    }

                    batch.SortedSetCombineAndStoreAsync(SetOperation.Union,
                        cacheKeyUnion,
                        unions.ToArray());

                    batch.KeyExpireAsync(cacheKeyUnion, TimeSpan.FromSeconds(120));
                }

                if (hasFuzzySearch)
                    keys.Add(searchCacheKey);

                var redisKeys = keys.ToArray();

                var weights = new double[redisKeys.Length];
                Array.Fill(weights, 1);

                var setResult = batch.SortedSetCombineAndStoreAsync(SetOperation.Intersect,
                    cacheKey,
                    redisKeys,
                    weights);

                batch.KeyExpireAsync(cacheKey, TimeSpan.FromSeconds(120));

                batch.Execute();

                total = await setResult;
            }
            else
            {
                total = await redisDb.Database.SortedSetLengthAsync(cacheKey);
            }

            if (total > 0)
            {
                var skipAmount = (searchRequest.Page * searchRequest.PageSize) - searchRequest.PageSize;
                var takeAmount = (skipAmount + searchRequest.PageSize) - 1;

                var vins = await redisDb.Database.SortedSetRangeByScoreAsync(cacheKey, skip: skipAmount, take: takeAmount);
                var vinKeys = vins.Select((v) => $"json:vehicles:{v}");
                var result = await redisDb.GetAllAsync<SearchResultItem>(vinKeys.ToArray());

                return new SearchResult(cacheKey, result.Count, total, result.Values);
            }

            return new SearchResult(cacheKey, 0, 0, Array.Empty<SearchResultItem>());
        }
    }
}

