using System;

namespace Redis_Search.Models
{
    public record SearchResult(string CacheKey, int Count, long Total, ICollection<SearchResultItem> Results);
}

