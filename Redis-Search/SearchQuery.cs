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

        public async Task FindAndStore(string key, string input, double expiryInSeconds = 120, int offset = 0, int limit = 10000)
        {
            if (await _database.KeyExistsAsync(key))
            {
                await _database.KeyExpireAsync(key, TimeSpan.FromSeconds(expiryInSeconds));
                return;
            }

            var query = new Query(input);
            query.Limit(offset, limit);

            var searchResult = await _client.SearchAsync(query);

            var searchBatch = _database.CreateBatch();

            IList<Task> tasks = new List<Task>(searchResult.Documents.Count);

            for (var i = 0; i < searchResult.Documents.Count; i++)
            {
                int lastIndex = searchResult.Documents[i].Id.LastIndexOf(":");

                var d = searchResult.Documents[i].Id.AsSpan().Slice(lastIndex + 1).ToString();

                tasks.Add(searchBatch.SortedSetAddAsync(key, d, i));
                tasks.Add(searchBatch.KeyExpireAsync(key, TimeSpan.FromSeconds(120)));
            }

            searchBatch.Execute();

            await Task.WhenAll(tasks);
        }
    }
}

