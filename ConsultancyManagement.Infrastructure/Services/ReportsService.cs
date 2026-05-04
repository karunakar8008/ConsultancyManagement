using ConsultancyManagement.Core.DTOs;
using ConsultancyManagement.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using ConsultancyManagement.Infrastructure.Data;

namespace ConsultancyManagement.Infrastructure.Services;

public class ReportsService : IReportsService
{
    private readonly ApplicationDbContext _db;

    public ReportsService(ApplicationDbContext db) => _db = db;

    public async Task<DailySummaryReportDto?> GetDailySummaryAsync(DateTime date)
    {
        var day = ToUtcDate(date);
        var next = day.AddDays(1);

        var activities = await _db.DailyActivities.AsNoTracking()
            .Where(d => d.ActivityDate >= day && d.ActivityDate < next)
            .ToListAsync();

        var submissionRowsToday = await _db.Submissions.CountAsync(s => s.SubmissionDate >= day && s.SubmissionDate < next);
        var interviewsToday = await _db.Interviews.CountAsync(i => i.InterviewDate >= day && i.InterviewDate < next);

        return new DailySummaryReportDto
        {
            Date = day,
            TotalJobsApplied = activities.Sum(a => a.JobsAppliedCount),
            TotalVendorReachOuts = activities.Sum(a => a.VendorReachedOutCount),
            TotalVendorResponses = activities.Sum(a => a.VendorResponseCount),
            TotalSubmissions = submissionRowsToday,
            TotalInterviewCalls = interviewsToday,
            ActiveConsultants = await _db.Consultants.CountAsync(c =>
                c.Status == "Active" && !c.User.IsDeleted)
        };
    }

    public async Task<WeeklySummaryReportDto?> GetWeeklySummaryAsync(DateTime startDate, DateTime endDate)
    {
        var start = ToUtcDate(startDate);
        var end = ToUtcDate(endDate).AddDays(1);

        var activities = await _db.DailyActivities.AsNoTracking()
            .Where(d => d.ActivityDate >= start && d.ActivityDate < end)
            .ToListAsync();

        var interviews = await _db.Interviews.CountAsync(i => i.InterviewDate >= start && i.InterviewDate < end);
        var vendorResponses = activities.Sum(a => a.VendorResponseCount);

        return new WeeklySummaryReportDto
        {
            StartDate = start,
            EndDate = ToUtcDate(endDate),
            TotalJobsApplied = activities.Sum(a => a.JobsAppliedCount),
            TotalVendorReachOuts = activities.Sum(a => a.VendorReachedOutCount),
            TotalVendorResponses = vendorResponses,
            TotalSubmissions = await _db.Submissions.CountAsync(s => s.SubmissionDate >= start && s.SubmissionDate < end),
            TotalInterviews = interviews
        };
    }

    private static DateTime ToUtcDate(DateTime value)
    {
        var dateOnly = value.Date;
        return DateTime.SpecifyKind(dateOnly, DateTimeKind.Utc);
    }

    public async Task<IReadOnlyList<ConsultantPerformanceDto>> GetConsultantPerformanceAsync()
    {
        var list = await _db.Consultants.AsNoTracking()
            .Where(c => !c.User.IsDeleted)
            .Include(c => c.JobApplications)
            .Include(c => c.DailyActivities)
            .Include(c => c.Submissions)
            .ThenInclude(s => s.Interviews)
            .OrderBy(c => c.LastName)
            .ToListAsync();

        return list.Select(c => new ConsultantPerformanceDto
        {
            ConsultantId = c.Id,
            Name = c.FirstName + " " + c.LastName,
            JobsApplied = c.DailyActivities.Sum(d => d.JobsAppliedCount) + c.JobApplications.Count,
            Submissions = c.Submissions.Count,
            Interviews = c.Submissions.Sum(s => s.Interviews.Count)
        }).OrderByDescending(x => x.Submissions).ToList();
    }

    public async Task<IReadOnlyList<SalesPerformanceDto>> GetSalesPerformanceAsync()
    {
        var list = await _db.SalesRecruiters.AsNoTracking()
            .Where(s => !s.User.IsDeleted)
            .Include(s => s.Submissions)
            .ThenInclude(x => x.Interviews)
            .Include(s => s.ConsultantAssignments)
            .OrderBy(s => s.LastName)
            .ToListAsync();

        return list.Select(s => new SalesPerformanceDto
        {
            SalesRecruiterId = s.Id,
            Name = s.FirstName + " " + s.LastName,
            Submissions = s.Submissions.Count,
            Interviews = s.Submissions.Sum(x => x.Interviews.Count),
            AssignedConsultants = s.ConsultantAssignments.Count(a => a.IsActive)
        }).OrderByDescending(x => x.Submissions).ToList();
    }

    public async Task<IReadOnlyList<SubmissionReportRowDto>> GetSubmissionsReportAsync()
    {
        return await _db.Submissions.AsNoTracking()
            .OrderByDescending(s => s.SubmissionDate)
            .Take(500)
            .Select(s => new SubmissionReportRowDto
            {
                Id = s.Id,
                SubmissionCode = s.SubmissionCode,
                ConsultantName = s.Consultant.FirstName + " " + s.Consultant.LastName,
                SalesRecruiterName = s.SalesRecruiter.FirstName + " " + s.SalesRecruiter.LastName,
                VendorName = s.Vendor.VendorName,
                JobTitle = s.JobTitle,
                SubmissionDate = s.SubmissionDate,
                Status = s.Status
            }).ToListAsync();
    }

    public async Task<IReadOnlyList<InterviewReportRowDto>> GetInterviewsReportAsync()
    {
        return await _db.Interviews.AsNoTracking()
            .OrderByDescending(i => i.InterviewDate)
            .Take(500)
            .Select(i => new InterviewReportRowDto
            {
                Id = i.Id,
                ConsultantName = i.Submission.Consultant.FirstName + " " + i.Submission.Consultant.LastName,
                JobTitle = i.Submission.JobTitle,
                InterviewDate = i.InterviewDate,
                Mode = i.InterviewMode ?? string.Empty,
                Status = i.Status
            }).ToListAsync();
    }

    public async Task<IReadOnlyList<OnboardingStatusReportDto>> GetOnboardingStatusAsync()
    {
        return await _db.Consultants.AsNoTracking()
            .Where(c => !c.User.IsDeleted)
            .OrderBy(c => c.LastName)
            .Select(c => new OnboardingStatusReportDto
            {
                ConsultantId = c.Id,
                ConsultantName = c.FirstName + " " + c.LastName,
                TotalTasks = c.OnboardingTasks.Count,
                CompletedTasks = c.OnboardingTasks.Count(t => t.Status == "Completed"),
                PendingTasks = c.OnboardingTasks.Count(t => t.Status != "Completed")
            })
            .ToListAsync();
    }
}
