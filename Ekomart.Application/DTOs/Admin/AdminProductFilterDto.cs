using System.ComponentModel.DataAnnotations;
using Ekomart.Application.Common;

namespace Ekomart.Application.DTOs.Admin;

public class AdminProductFilterDto : PagedRequest
{
    [StringLength(120)]
    public string? Search { get; set; }

    public int? CategoryId { get; set; }
    public bool? IsActive { get; set; }
}
