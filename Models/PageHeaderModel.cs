namespace MerceditasStore.Models;

public sealed class PageHeaderModel
{
    public string Title { get; set; } = "";
    public string? Subtitle { get; set; }
    public Microsoft.AspNetCore.Html.IHtmlContent? Actions { get; set; }
}

