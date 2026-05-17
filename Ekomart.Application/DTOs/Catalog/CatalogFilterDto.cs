using System.ComponentModel.DataAnnotations;
using Ekomart.Application.Common;

namespace Ekomart.Application.DTOs.Catalog;

public class CatalogFilterDto : PagedRequest
{
    [StringLength(120)]
    public string? Search { get; set; }

    [StringLength(120)]
    public string? CategorySlug { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? MinPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? MaxPrice { get; set; }

    [StringLength(40)]
    public string? Sort { get; set; }
}
