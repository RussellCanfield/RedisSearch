using System;
using Redis_Search.Models;

namespace Redis_Search.Services
{
    public interface ISearchService
    {
        public Task<SearchResult> Search(SearchRequest searchRequest);
    }
}

