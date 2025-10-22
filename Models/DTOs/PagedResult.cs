namespace ClarityDesk.Models.DTOs;

/// <summary>
/// 分頁結果泛型類別
/// </summary>
/// <typeparam name="T">資料項目型別</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// 資料項目清單
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// 資料總筆數
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 當前頁碼
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// 每頁筆數
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 總頁數
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// 是否有上一頁
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 1;

    /// <summary>
    /// 是否有下一頁
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;

    /// <summary>
    /// 建立分頁結果
    /// </summary>
    public static PagedResult<T> Create(List<T> items, int totalCount, int currentPage, int pageSize)
    {
        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = currentPage,
            PageSize = pageSize
        };
    }
}
