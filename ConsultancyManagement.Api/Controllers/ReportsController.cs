using System.Globalization;
using System.Text;
using ConsultancyManagement.Core.DTOs;
using ConsultancyManagement.Core.Enums;
using ConsultancyManagement.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsultancyManagement.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Management)}")]
public class ReportsController : ControllerBase
{
    private readonly IReportsService _reports;

    public ReportsController(IReportsService reports) => _reports = reports;

    [HttpGet("daily-summary")]
    public async Task<IActionResult> DailySummary([FromQuery] DateTime date) =>
        Ok(await _reports.GetDailySummaryAsync(date));

    [HttpGet("daily-summary/csv")]
    public async Task<IActionResult> DailySummaryCsv([FromQuery] DateTime date)
    {
        var r = await _reports.GetDailySummaryAsync(date);
        if (r is null) return NotFound();
        var sb = new StringBuilder();
        sb.AppendLine("Metric,Value");
        sb.AppendLine($"Date,{Csv(r.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}");
        sb.AppendLine($"TotalJobsApplied,{r.TotalJobsApplied.ToString(CultureInfo.InvariantCulture)}");
        sb.AppendLine($"TotalVendorReachOuts,{r.TotalVendorReachOuts.ToString(CultureInfo.InvariantCulture)}");
        sb.AppendLine($"TotalVendorResponses,{r.TotalVendorResponses.ToString(CultureInfo.InvariantCulture)}");
        sb.AppendLine($"TotalSubmissions,{r.TotalSubmissions.ToString(CultureInfo.InvariantCulture)}");
        sb.AppendLine($"TotalInterviewCalls,{r.TotalInterviewCalls.ToString(CultureInfo.InvariantCulture)}");
        sb.AppendLine($"ActiveConsultants,{r.ActiveConsultants.ToString(CultureInfo.InvariantCulture)}");
        sb.AppendLine();
        sb.AppendLine("Context");
        sb.AppendLine(
            Csv("Daily summary aggregates daily activity rows, submission rows dated this day, and interviews dated this day."));
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv",
            $"daily-summary-{r.Date:yyyy-MM-dd}.csv");
    }

    [HttpGet("weekly-summary")]
    public async Task<IActionResult> WeeklySummary([FromQuery] DateTime startDate, [FromQuery] DateTime endDate) =>
        Ok(await _reports.GetWeeklySummaryAsync(startDate, endDate));

    [HttpGet("weekly-summary/csv")]
    public async Task<IActionResult> WeeklySummaryCsv([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var r = await _reports.GetWeeklySummaryAsync(startDate, endDate);
        if (r is null) return NotFound();
        var sb = new StringBuilder();
        sb.AppendLine("Metric,Value");
        sb.AppendLine($"StartDate,{Csv(r.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}");
        sb.AppendLine($"EndDate,{Csv(r.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}");
        sb.AppendLine($"TotalJobsApplied,{r.TotalJobsApplied.ToString(CultureInfo.InvariantCulture)}");
        sb.AppendLine($"TotalVendorReachOuts,{r.TotalVendorReachOuts.ToString(CultureInfo.InvariantCulture)}");
        sb.AppendLine($"TotalVendorResponses,{r.TotalVendorResponses.ToString(CultureInfo.InvariantCulture)}");
        sb.AppendLine($"TotalSubmissions,{r.TotalSubmissions.ToString(CultureInfo.InvariantCulture)}");
        sb.AppendLine($"TotalInterviews,{r.TotalInterviews.ToString(CultureInfo.InvariantCulture)}");
        sb.AppendLine();
        sb.AppendLine("Context");
        sb.AppendLine(Csv("Weekly summary sums daily activities and counts submissions and interviews in the inclusive date range."));
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv",
            $"weekly-summary-{r.StartDate:yyyy-MM-dd}-to-{r.EndDate:yyyy-MM-dd}.csv");
    }

    [HttpGet("consultant-performance")]
    public async Task<IActionResult> ConsultantPerformance() =>
        Ok(await _reports.GetConsultantPerformanceAsync());

    [HttpGet("consultant-performance/csv")]
    public async Task<IActionResult> ConsultantPerformanceCsv()
    {
        var rows = await _reports.GetConsultantPerformanceAsync();
        var sb = new StringBuilder();
        sb.AppendLine("ConsultantId,Name,JobsApplied,Submissions,Interviews");
        foreach (var x in rows)
        {
            sb.AppendLine(string.Join(',',
                x.ConsultantId.ToString(CultureInfo.InvariantCulture),
                Csv(x.Name),
                x.JobsApplied.ToString(CultureInfo.InvariantCulture),
                x.Submissions.ToString(CultureInfo.InvariantCulture),
                x.Interviews.ToString(CultureInfo.InvariantCulture)));
        }

        sb.AppendLine();
        sb.AppendLine("Context");
        sb.AppendLine(Csv("Jobs applied = sum of daily activity job counts plus job application rows. Interviews counted via submission linkage."));
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "consultant-performance.csv");
    }

    [HttpGet("sales-performance")]
    public async Task<IActionResult> SalesPerformance() =>
        Ok(await _reports.GetSalesPerformanceAsync());

    [HttpGet("sales-performance/csv")]
    public async Task<IActionResult> SalesPerformanceCsv()
    {
        var rows = await _reports.GetSalesPerformanceAsync();
        var sb = new StringBuilder();
        sb.AppendLine("SalesRecruiterId,Name,Submissions,Interviews,AssignedConsultants");
        foreach (var x in rows)
        {
            sb.AppendLine(string.Join(',',
                x.SalesRecruiterId.ToString(CultureInfo.InvariantCulture),
                Csv(x.Name),
                x.Submissions.ToString(CultureInfo.InvariantCulture),
                x.Interviews.ToString(CultureInfo.InvariantCulture),
                x.AssignedConsultants.ToString(CultureInfo.InvariantCulture)));
        }

        sb.AppendLine();
        sb.AppendLine("Context");
        sb.AppendLine(Csv("Sales performance is derived from submissions and interviews linked to each recruiter, plus active consultant assignments."));
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "sales-performance.csv");
    }

    [HttpGet("submissions")]
    public async Task<IActionResult> Submissions() =>
        Ok(await _reports.GetSubmissionsReportAsync());

    [HttpGet("submissions/csv")]
    public async Task<IActionResult> SubmissionsCsv()
    {
        var rows = await _reports.GetSubmissionsReportAsync();
        var sb = new StringBuilder();
        sb.AppendLine("Id,ConsultantName,SalesRecruiterName,VendorName,JobTitle,SubmissionDate,Status");
        foreach (SubmissionReportRowDto x in rows)
        {
            sb.AppendLine(string.Join(',',
                x.Id.ToString(CultureInfo.InvariantCulture),
                Csv(x.ConsultantName),
                Csv(x.SalesRecruiterName),
                Csv(x.VendorName),
                Csv(x.JobTitle),
                Csv(x.SubmissionDate.ToString("o", CultureInfo.InvariantCulture)),
                Csv(x.Status)));
        }

        sb.AppendLine();
        sb.AppendLine("Context");
        sb.AppendLine(Csv("Submission rows from the Submissions table (latest 500), joined to consultant, sales recruiter, and vendor."));
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "submissions-report.csv");
    }

    [HttpGet("interviews")]
    public async Task<IActionResult> Interviews() =>
        Ok(await _reports.GetInterviewsReportAsync());

    [HttpGet("interviews/csv")]
    public async Task<IActionResult> InterviewsCsv()
    {
        var rows = await _reports.GetInterviewsReportAsync();
        var sb = new StringBuilder();
        sb.AppendLine("Id,ConsultantName,JobTitle,InterviewDate,Mode,Status");
        foreach (InterviewReportRowDto x in rows)
        {
            sb.AppendLine(string.Join(',',
                x.Id.ToString(CultureInfo.InvariantCulture),
                Csv(x.ConsultantName),
                Csv(x.JobTitle),
                Csv(x.InterviewDate.ToString("o", CultureInfo.InvariantCulture)),
                Csv(x.Mode),
                Csv(x.Status)));
        }

        sb.AppendLine();
        sb.AppendLine("Context");
        sb.AppendLine(Csv("Interview rows linked to submissions (latest 500)."));
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "interviews-report.csv");
    }

    [HttpGet("onboarding-status")]
    public async Task<IActionResult> OnboardingStatus() =>
        Ok(await _reports.GetOnboardingStatusAsync());

    [HttpGet("onboarding-status/csv")]
    public async Task<IActionResult> OnboardingStatusCsv()
    {
        var rows = await _reports.GetOnboardingStatusAsync();
        var sb = new StringBuilder();
        sb.AppendLine("ConsultantId,ConsultantName,TotalTasks,CompletedTasks,PendingTasks");
        foreach (OnboardingStatusReportDto x in rows)
        {
            sb.AppendLine(string.Join(',',
                x.ConsultantId.ToString(CultureInfo.InvariantCulture),
                Csv(x.ConsultantName),
                x.TotalTasks.ToString(CultureInfo.InvariantCulture),
                x.CompletedTasks.ToString(CultureInfo.InvariantCulture),
                x.PendingTasks.ToString(CultureInfo.InvariantCulture)));
        }

        sb.AppendLine();
        sb.AppendLine("Context");
        sb.AppendLine(Csv("Per-consultant onboarding task counts for users that are not soft-deleted."));
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "onboarding-status.csv");
    }

    private static string Csv(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var t = s.Replace("\"", "\"\"");
        if (t.Contains(',') || t.Contains('\n') || t.Contains('\r'))
            return $"\"{t}\"";
        return t;
    }
}
