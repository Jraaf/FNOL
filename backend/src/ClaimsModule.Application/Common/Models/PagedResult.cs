namespace ClaimsModule.Application.Common.Models;

public record PagedResult<T>(IReadOnlyCollection<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}
