using LegalDocSystem.Models;

namespace LegalDocSystem.ViewModels;

/// <summary>
/// Represents a search result with ranking and highlighted text.
/// </summary>
public class SearchResult
{
    /// <summary>
    /// The document found in the search.
    /// </summary>
    public Document Document { get; set; } = null!;

    /// <summary>
    /// Relevance rank from PostgreSQL ts_rank.
    /// </summary>
    public double Rank { get; set; }

    /// <summary>
    /// Highlighted document name with search terms.
    /// </summary>
    public string HighlightedName { get; set; } = string.Empty;

    /// <summary>
    /// Highlighted document type with search terms.
    /// </summary>
    public string? HighlightedType { get; set; }

    /// <summary>
    /// Snippet from document content (if available).
    /// </summary>
    public string? Snippet { get; set; }
}

/// <summary>
/// Represents paginated search results.
/// </summary>
public class PaginatedSearchResult
{
    /// <summary>
    /// List of search results for the current page.
    /// </summary>
    public List<SearchResult> Results { get; set; } = new();

    /// <summary>
    /// Total number of results across all pages.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    public int CurrentPage { get; set; } = 1;

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 1;

    /// <summary>
    /// Whether there is a next page.
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;
}

