using System;

namespace Redis_Search.Models
{
    public record SearchRequest(int PageSize, int Page, string Text, SearchFacet[] Facets);
}

