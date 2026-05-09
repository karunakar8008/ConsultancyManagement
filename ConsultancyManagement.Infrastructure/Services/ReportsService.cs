using ConsultancyManagement.Core.DTOs;
using ConsultancyManagement.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using ConsultancyManagement.Infrastructure.Data;

namespace ConsultancyManagement.Infrastructure.Services;

public class ReportsService : IReportsService
{
    private readonly ApplicationDbContext _db;

    public ReportsService(ApplicationDbContext db) => _db = db;

    public async Task<DailySummaryReportDto?> GetDailySummaryAsync(
        DateTime date, int? consultantId = null, int? salesRecruiterId = null)
    {
        if (consultantId.HasValue && salesRecruiterId.HasValue)
            throw new ArgumentException("Specify only one of consultantId or salesRecruiterId.");

        var day = ToUtcDate(date);
        var next = day.AddDays(1);

        if (consultantId.HasValue)
        {
            var c = await _db.Consultants.AsNoTracking()
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == consultantId.Value);
            if (c is null) return null;
            var label = $"{c.FirstName} {c.LastName}".Trim();

            var activities = await _db.DailyActivities.AsNoTracking()
                .Where(d => d.ConsultantId == consultantId.Value && d.ActivityDate >= day && d.ActivityDate < next)
                .ToListAsync();

            var submissionRowsToday = await _db.Submissions.CountAsync(s =>
                s.ConsultantId == consultantId.Value && s.SubmissionDate >= day && s.SubmissionDate < next);
            var interviewsToday = await _db.Interviews.CountAsync(i =>
                i.Submission.ConsultantId == consultantId.Value && i.InterviewDate >= day && i.InterviewDate < next);

            var active = c.Status == "Active" && !c.User.IsDeleted ? 1 : 0;

            return new DailySummaryReportDto
            {
                Date = day,
                TotalJobsApplied = activities.Sum(a => a.JobsAppliedCount),
                TotalVendorReachOuts = activities.Sum(a => a.VendorReachedOutCount),
                TotalVendorResponses = activities.Sum(a => a.VendorResponseCount),
                TotalSubmissions = submissionRowsToday,
                TotalInterviewCalls = interviewsToday,
                ActiveConsultants = active,
                ScopeConsultantId = consultantId,
                ScopeConsultantName = label
            };
        }

        if (salesRecruiterId.HasValue)
        {
            var sr = await _db.SalesRecruiters.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == salesRecruiterId.Value);
            if (sr is null) return null;
            var sLabel = $"{sr.FirstName} {sr.LastName}".Trim();

            var assignedIds = await _db.ConsultantSalesAssignments.AsNoTracking()
                .Where(a => a.SalesRecruiterId == salesRecruiterId.Value && a.IsActive)
                .Select(a => a.ConsultantId)
                .ToListAsync();

            var activities = await _db.DailyActivities.AsNoTracking()
                .Where(d => assignedIds.Contains(d.ConsultantId) && d.ActivityDate >= day && d.ActivityDate < next)
                .ToListAsync();

            var submissionRowsToday = await _db.Submissions.CountAsync(s =>
                s.SalesRecruiterId == salesRecruiterId.Value && s.SubmissionDate >= day && s.SubmissionDate < next);
            var interviewsToday = await _db.Interviews.CountAsync(i =>
                i.Submission.SalesRecruiterId == salesRecruiterId.Value && i.InterviewDate >= day &&
                i.InterviewDate < next);

            return new DailySummaryReportDto
            {
                Date = day,
                TotalJobsApplied = activities.Sum(a => a.JobsAppliedCount),
                TotalVendorReachOuts = activities.Sum(a => a.VendorReachedOutCount),
                TotalVendorResponses = activities.Sum(a => a.VendorResponseCount),
                TotalSubmissions = submissionRowsToday,
                TotalInterviewCalls = interviewsToday,
                ActiveConsultants = assignedIds.Count,
                ScopeSalesRecruiterId = salesRecruiterId,
                ScopeSalesRecruiterName = sLabel
            };
        }

        var allActivities = await _db.DailyActivities.AsNoTracking()
            .Where(d => d.ActivityDate >= day && d.ActivityDate < next)
            .ToListAsync();

        var subsToday = await _db.Submissions.CountAsync(s => s.SubmissionDate >= day && s.SubmissionDate < next);
        var intsToday = await _db.Interviews.CountAsync(i => i.InterviewDate >= day && i.InterviewDate < next);

        return new DailySummaryReportDto
        {
            Date = day,
            TotalJobsApplied = allActivities.Sum(a => a.JobsAppliedCount),
            TotalVendorReachOuts = allActivities.Sum(a => a.VendorReachedOutCount),
            TotalVendorResponses = allActivities.Sum(a => a.VendorResponseCount),
            TotalSubmissions = subsToday,
            TotalInterviewCalls = intsToday,
            ActiveConsultants = await _db.Consultants.CountAsync(c =>
                c.Status == "Active" && !c.User.IsDeleted)
        };
    }

    public async Task<WeeklySummaryReportDto?> GetWeeklySummaryAsync(
        DateTime startDate, DateTime endDate, int? consultantId = null, int? salesRecruiterId = null)
    {
        if (consultantId.HasValue && salesRecruiterId.HasValue)
            throw new ArgumentException("Specify only one of consultantId or salesRecruiterId.");

        var start = ToUtcDate(startDate);
        var endExclusive = ToUtcDate(endDate).AddDays(1);

        if (consultantId.HasValue)
        {
            var c = await _db.Consultants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == consultantId.Value);
            if (c is null) return null;
            var label = $"{c.FirstName} {c.LastName}".Trim();

            var activities = await _db.DailyActivities.AsNoTracking()
                .Where(d => d.ConsultantId == consultantId.Value && d.ActivityDate >= start && d.ActivityDate < endExclusive)
                .ToListAsync();

            var interviews = await _db.Interviews.CountAsync(i =>
                i.Submission.ConsultantId == consultantId.Value && i.InterviewDate >= start && i.InterviewDate < endExclusive);

            return new WeeklySummaryReportDto
            {
                StartDate = start,
                EndDate = ToUtcDate(endDate),
                TotalJobsApplied = activities.Sum(a => a.JobsAppliedCount),
                TotalVendorReachOuts = activities.Sum(a => a.VendorReachedOutCount),
                TotalVendorResponses = activities.Sum(a => a.VendorResponseCount),
                TotalSubmissions = await _db.Submissions.CountAsync(s =>
                    s.ConsultantId == consultantId.Value && s.SubmissionDate >= start && s.SubmissionDate < endExclusive),
                TotalInterviews = interviews,
                ScopeConsultantId = consultantId,
                ScopeConsultantName = label
            };
        }

        if (salesRecruiterId.HasValue)
        {
            var sr = await _db.SalesRecruiters.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == salesRecruiterId.Value);
            if (sr is null) return null;
            var sLabel = $"{sr.FirstName} {sr.LastName}".Trim();

            var assignedIds = await _db.ConsultantSalesAssignments.AsNoTracking()
                .Where(a => a.SalesRecruiterId == salesRecruiterId.Value && a.IsActive)
                .Select(a => a.ConsultantId)
                .ToListAsync();

            var activities = await _db.DailyActivities.AsNoTracking()
                .Where(d => assignedIds.Contains(d.ConsultantId) && d.ActivityDate >= start && d.ActivityDate < endExclusive)
                .ToListAsync();

            var interviews = await _db.Interviews.CountAsync(i =>
                i.Submission.SalesRecruiterId == salesRecruiterId.Value && i.InterviewDate >= start &&
                i.InterviewDate < endExclusive);

            return new WeeklySummaryReportDto
            {
                StartDate = start,
                EndDate = ToUtcDate(endDate),
                TotalJobsApplied = activities.Sum(a => a.JobsAppliedCount),
                TotalVendorReachOuts = activities.Sum(a => a.VendorReachedOutCount),
                TotalVendorResponses = activities.Sum(a => a.VendorResponseCount),
                TotalSubmissions = await _db.Submissions.CountAsync(s =>
                    s.SalesRecruiterId == salesRecruiterId.Value && s.SubmissionDate >= start && s.SubmissionDate < endExclusive),
                TotalInterviews = interviews,
                ScopeSalesRecruiterId = salesRecruiterId,
                ScopeSalesRecruiterName = sLabel
            };
        }

        var allActivities = await _db.DailyActivities.AsNoTracking()
            .Where(d => d.ActivityDate >= start && d.ActivityDate < endExclusive)
            .ToListAsync();

        var interviewsAll = await _db.Interviews.CountAsync(i => i.InterviewDate >= start && i.InterviewDate < endExclusive);

        return new WeeklySummaryReportDto
        {
            StartDate = start,
            EndDate = ToUtcDate(endDate),
            TotalJobsApplied = allActivities.Sum(a => a.JobsAppliedCount),
            TotalVendorReachOuts = allActivities.Sum(a => a.VendorReachedOutCount),
            TotalVendorResponses = allActivities.Sum(a => a.VendorResponseCount),
            TotalSubmissions = await _db.Submissions.CountAsync(s => s.SubmissionDate >= start && s.SubmissionDate < endExclusive),
            TotalInterviews = interviewsAll
        };
    }

    private static DateTime ToUtcDate(DateTime value)
    {
        var dateOnly = value.Date;
        return DateTime.SpecifyKind(dateOnly, DateTimeKind.Utc);
    }

    public async Task<IReadOnlyList<ConsultantPerformanceDto>> GetConsultantPerformanceAsync(int? consultantId = null)
    {
        var list = await _db.Consultants.AsNoTracking()
            .Where(c => !c.User.IsDeleted)
            .Where(c => !consultantId.HasValue || c.Id == consultantId.Value)
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

    public async Task<IReadOnlyList<SalesPerformanceDto>> GetSalesPerformanceAsync(int? salesRecruiterId = null)
    {
        var list = await _db.SalesRecruiters.AsNoTracking()
            .Where(s => !s.User.IsDeleted)
            .Where(s => !salesRecruiterId.HasValue || s.Id == salesRecruiterId.Value)
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
                Status = s.Status,
                Notes = s.Notes
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
                InterviewEndDate = i.InterviewEndDate,
                Mode = i.InterviewMode ?? string.Empty,
                Status = i.Status,
                Feedback = i.Feedback,
                Notes = i.Notes
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
