using Microsoft.AspNetCore.Http;
using NRediSearch;
using StackExchange.Redis;

namespace Redis_Search.Services
{
    public class SearchQuery
    {
        private IDatabase _database;
        private Client _client;

        private SearchQuery(IDatabase database, Client client)
        {
            _database = database;
            _client = client;
        }

        public static SearchQuery Create(IDatabase database, string index)
        {
            return new SearchQuery(database, new Client(index, database));
        }

        public async Task<SearchResult> Find(string input, int offset = 0, int limit = 10000)
        {
            var query = new Query(input);
            query.Limit(offset, limit);

            return await _client.SearchAsync(query);
        }

        public void Store(SearchResult searchResult, string key)
        {
            var searchBatch = _database.CreateBatch();

            for (var i = 0; i < searchResult.Documents.Count; i++)
            {
                int lastIndex = searchResult.Documents[i].Id.LastIndexOf(":");

                var d = searchResult.Documents[i].Id.AsSpan().Slice(lastIndex + 1).ToString();

                searchBatch.SortedSetAddAsync(key, d, i);
                searchBatch.KeyExpireAsync(key, TimeSpan.FromSeconds(120));
            }

            searchBatch.Execute();
        }
    }
}

