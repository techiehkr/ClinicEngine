using ClinicEngine.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClinicEngine.Web.Pages.Admin;


public sealed class DashboardModel : PageModel
{
    private readonly IDashboardQueryService _dashboardService;

    public DashboardSummaryDto Summary { get; private set; } = default!;
    public IReadOnlyList<TodayScheduleItemDto> TodaySchedule { get; private set; } = [];
    public IReadOnlyList<DepartmentBookingDto> DepartmentStats { get; private set; } = [];

    public DashboardModel(IDashboardQueryService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        Summary = await _dashboardService.GetSummaryAsync(cancellationToken);
        TodaySchedule = await _dashboardService.GetTodayScheduleAsync(cancellationToken);
        DepartmentStats = await _dashboardService.GetDepartmentBookingStatsAsync(cancellationToken);
        return Page();
    }


    public async Task<IActionResult> OnGetScheduleFeedAsync(CancellationToken cancellationToken)
    {
        TodaySchedule = await _dashboardService.GetTodayScheduleAsync(cancellationToken);
        return Partial("_ScheduleFeed", TodaySchedule);
    }
}
