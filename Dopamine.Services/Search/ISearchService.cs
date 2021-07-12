using System;

namespace Dopamine.Services.Search
{
    public interface ISearchService
    {
        string SearchText { get; set; }
        string LastSearchText { get; set; }
        event Action<string> DoSearch;
    }
}
