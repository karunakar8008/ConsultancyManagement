import { AfterViewInit, Component, inject, OnInit } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatSelectModule } from '@angular/material/select';
import { FormsModule } from '@angular/forms';
import { ChartData, ChartOptions, TooltipItem } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';
import { ReportsService } from '../../core/services/reports.service';
import { AdminService } from '../../core/services/admin.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-admin-reports',
  standalone: true,
  imports: [
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatDatepickerModule,
    MatSelectModule,
    FormsModule,
    BaseChartDirective
  ],
  templateUrl: './admin-reports.component.html',
  styles: [
    `
      .panel {
        padding: 1rem;
        margin-bottom: 1rem;
      }
      .row {
        display: flex;
        flex-wrap: wrap;
        gap: 0.75rem;
        align-items: center;
        margin-bottom: 1rem;
      }
      .chart-wrap {
        position: relative;
        height: 320px;
        max-width: 900px;
        margin-bottom: 1rem;
      }
      .chart-wrap.tall {
        height: 380px;
      }
      @media (max-width: 599px) {
        .row {
          flex-direction: column;
          align-items: stretch;
        }
        .chart-wrap,
        .chart-wrap.tall {
          max-width: 100%;
          height: 280px;
        }
        .chart-wrap.tall {
          height: 300px;
        }
      }
      .hint {
        margin: 0 0 0.75rem;
        font-size: 0.875rem;
        color: #475569;
        max-width: 52rem;
      }
    `
  ]
})
export class AdminReportsComponent implements OnInit, AfterViewInit {
  private readonly reports = inject(ReportsService);
  private readonly admin = inject(AdminService);
  private readonly toast = inject(ToastrService);

  dayDate = new Date();
  wStartDate = new Date(Date.now() - 7 * 86400000);
  wEndDate = new Date();
  /** Independent scope for daily summary vs weekly summary. */
  dailySummaryScope: 'all' | 'consultant' | 'sales' = 'all';
  dailySummaryConsultantId: number | null = null;
  dailySummarySalesId: number | null = null;
  weeklySummaryScope: 'all' | 'consultant' | 'sales' = 'all';
  weeklySummaryConsultantId: number | null = null;
  weeklySummarySalesId: number | null = null;
  consultants: { id: number; name: string }[] = [];
  salesRecruiters: { id: number; name: string }[] = [];

  /** 0 = all consultants; otherwise filter performance chart/CSV to one consultant. */
  perfConsultantFilterId = 0;
  /** 0 = all sales; otherwise one recruiter. */
  perfSalesFilterId = 0;

  private readonly dailyMetricTitles = [
    'Jobs applied',
    'Vendor reach',
    'Vendor responses',
    'Submissions',
    'Interview calls'
  ];
  private readonly dailyMetricHints = [
    'Source: sum of DailyActivities.JobsAppliedCount for the selected calendar date.',
    'Source: sum of DailyActivities.VendorReachedOutCount for the selected calendar date.',
    'Source: sum of DailyActivities.VendorResponseCount for the selected calendar date.',
    'Source: count of Submissions rows with SubmissionDate on the selected calendar date.',
    'Source: count of Interviews with InterviewDate on the selected calendar date (as proxy for interview calls).'
  ];

  private readonly weeklyMetricTitles = [
    'Jobs applied',
    'Vendor reach',
    'Vendor responses',
    'Submissions',
    'Interviews'
  ];
  private readonly weeklyMetricHints = [
    'Source: sum of DailyActivities.JobsAppliedCount across the date range.',
    'Source: sum of DailyActivities.VendorReachedOutCount across the date range.',
    'Source: sum of DailyActivities.VendorResponseCount across the date range.',
    'Source: count of Submissions with SubmissionDate in the inclusive range.',
    'Source: count of Interviews with InterviewDate in the inclusive range.'
  ];

  dailyChart: ChartData<'bar'> = { labels: [], datasets: [] };
  dailyChartOptions: ChartOptions<'bar'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
      tooltip: {
        callbacks: {
          title: (items: TooltipItem<'bar'>[]) => {
            const i = items[0]?.dataIndex ?? 0;
            return this.dailyMetricTitles[i] ?? '';
          },
          label: (ctx: TooltipItem<'bar'>) => {
            const i = ctx.dataIndex ?? 0;
            const v = ctx.parsed.y ?? 0;
            return [`Value: ${v}`, this.dailyMetricHints[i] ?? ''];
          }
        }
      }
    },
    scales: {
      y: {
        beginAtZero: true,
        ticks: { stepSize: 5, precision: 0 }
      }
    }
  };

  weeklyChart: ChartData<'bar'> = { labels: [], datasets: [] };
  weeklyChartOptions: ChartOptions<'bar'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
      tooltip: {
        callbacks: {
          title: (items: TooltipItem<'bar'>[]) => {
            const i = items[0]?.dataIndex ?? 0;
            return this.weeklyMetricTitles[i] ?? '';
          },
          label: (ctx: TooltipItem<'bar'>) => {
            const i = ctx.dataIndex ?? 0;
            const v = ctx.parsed.y ?? 0;
            return [`Value: ${v}`, this.weeklyMetricHints[i] ?? ''];
          }
        }
      }
    },
    scales: {
      y: {
        beginAtZero: true,
        ticks: { stepSize: 5, precision: 0 }
      }
    }
  };

  perfChart: ChartData<'bar'> = { labels: [], datasets: [] };
  perfChartOptions: ChartOptions<'bar'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: true, position: 'top' },
      tooltip: {
        callbacks: {
          afterBody: (items: TooltipItem<'bar'>[]) => {
            const label = items[0]?.dataset.label ?? '';
            return [
              `Dataset: ${label}.`,
              'Consultant performance: jobs from daily activities + job applications; interviews counted via submissions.'
            ];
          }
        }
      }
    },
    scales: {
      x: { ticks: { maxRotation: 45, minRotation: 20 } },
      y: {
        beginAtZero: true,
        ticks: { stepSize: 5, precision: 0 }
      }
    }
  };

  salesChart: ChartData<'bar'> = { labels: [], datasets: [] };
  salesChartOptions: ChartOptions<'bar'> = {
    ...this.perfChartOptions,
    plugins: {
      legend: { display: true, position: 'top' },
      tooltip: {
        callbacks: {
          afterBody: (items: TooltipItem<'bar'>[]) => {
            const label = items[0]?.dataset.label ?? '';
            return [
              `Dataset: ${label}.`,
              'Sales performance: submissions and interviews linked to each recruiter; assigned consultants counts active assignments.'
            ];
          }
        }
      }
    }
  };

  ngOnInit(): void {
    this.admin.consultants().subscribe({
      next: (rows) => {
        const list = rows as { id: number; firstName: string; lastName: string }[];
        this.consultants = list
          .map((r) => ({
            id: r.id,
            name: `${r.firstName} ${r.lastName}`.trim()
          }))
          .sort((a, b) => a.name.localeCompare(b.name));
      },
      error: () => this.toast.error('Failed to load consultants')
    });
    this.admin.salesRecruiters().subscribe({
      next: (rows) => {
        const list = rows as { id: number; firstName: string; lastName: string }[];
        this.salesRecruiters = list
          .map((r) => ({
            id: r.id,
            name: `${r.firstName} ${r.lastName}`.trim()
          }))
          .sort((a, b) => a.name.localeCompare(b.name));
      },
      error: () => this.toast.error('Failed to load sales recruiters')
    });
  }

  ngAfterViewInit(): void {
    queueMicrotask(() => {
      this.loadDaily();
      this.loadWeekly();
      this.loadPerf();
      this.loadSales();
    });
  }

  private dailyScopeConsultantId(): number | null {
    if (this.dailySummaryScope !== 'consultant') return null;
    return this.dailySummaryConsultantId && this.dailySummaryConsultantId > 0
      ? this.dailySummaryConsultantId
      : null;
  }

  private dailyScopeSalesId(): number | null {
    if (this.dailySummaryScope !== 'sales') return null;
    return this.dailySummarySalesId && this.dailySummarySalesId > 0 ? this.dailySummarySalesId : null;
  }

  private validateDailySummarySelection(): boolean {
    if (this.dailySummaryScope === 'consultant' && !this.dailyScopeConsultantId()) {
      this.toast.warning('Select a consultant for the daily report.');
      return false;
    }
    if (this.dailySummaryScope === 'sales' && !this.dailyScopeSalesId()) {
      this.toast.warning('Select a sales recruiter for the daily report.');
      return false;
    }
    return true;
  }

  onDailySummaryScopeChange(): void {
    this.dailySummaryConsultantId = null;
    this.dailySummarySalesId = null;
  }

  private weeklyScopeConsultantId(): number | null {
    if (this.weeklySummaryScope !== 'consultant') return null;
    return this.weeklySummaryConsultantId && this.weeklySummaryConsultantId > 0
      ? this.weeklySummaryConsultantId
      : null;
  }

  private weeklyScopeSalesId(): number | null {
    if (this.weeklySummaryScope !== 'sales') return null;
    return this.weeklySummarySalesId && this.weeklySummarySalesId > 0 ? this.weeklySummarySalesId : null;
  }

  private validateWeeklySummarySelection(): boolean {
    if (this.weeklySummaryScope === 'consultant' && !this.weeklyScopeConsultantId()) {
      this.toast.warning('Select a consultant for the weekly report.');
      return false;
    }
    if (this.weeklySummaryScope === 'sales' && !this.weeklyScopeSalesId()) {
      this.toast.warning('Select a sales recruiter for the weekly report.');
      return false;
    }
    return true;
  }

  onWeeklySummaryScopeChange(): void {
    this.weeklySummaryConsultantId = null;
    this.weeklySummarySalesId = null;
  }

  /** Local calendar YYYY-MM-DD (datepicker day matches API filter; avoids UTC shift from toISOString). */
  private reportDayKey(d: Date): string {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }

  private saveBlob(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
  }

  downloadDailyCsv(): void {
    if (!this.validateDailySummarySelection()) return;
    this.reports
      .dailySummaryCsv(this.reportDayKey(this.dayDate), this.dailyScopeConsultantId(), this.dailyScopeSalesId())
      .subscribe({
        next: (b) => this.saveBlob(b, `daily-summary-${this.reportDayKey(this.dayDate)}.csv`),
        error: () => this.toast.error('Download failed')
      });
  }

  downloadWeeklyCsv(): void {
    if (!this.validateWeeklySummarySelection()) return;
    this.reports
      .weeklySummaryCsv(
        this.reportDayKey(this.wStartDate),
        this.reportDayKey(this.wEndDate),
        this.weeklyScopeConsultantId(),
        this.weeklyScopeSalesId()
      )
      .subscribe({
        next: (b) =>
          this.saveBlob(
            b,
            `weekly-summary-${this.reportDayKey(this.wStartDate)}-to-${this.reportDayKey(this.wEndDate)}.csv`
          ),
        error: () => this.toast.error('Download failed')
      });
  }

  downloadPerfCsv(): void {
    const id = this.perfConsultantFilterId > 0 ? this.perfConsultantFilterId : null;
    this.reports.consultantPerformanceCsv(id).subscribe({
      next: (b) =>
        this.saveBlob(
          b,
          id ? `consultant-performance-${id}.csv` : 'consultant-performance.csv'
        ),
      error: () => this.toast.error('Download failed')
    });
  }

  downloadSalesCsv(): void {
    const id = this.perfSalesFilterId > 0 ? this.perfSalesFilterId : null;
    this.reports.salesPerformanceCsv(id).subscribe({
      next: (b) =>
        this.saveBlob(b, id ? `sales-performance-${id}.csv` : 'sales-performance.csv'),
      error: () => this.toast.error('Download failed')
    });
  }

  loadDaily(): void {
    if (!this.validateDailySummarySelection()) return;
    this.reports
      .dailySummary(this.reportDayKey(this.dayDate), this.dailyScopeConsultantId(), this.dailyScopeSalesId())
      .subscribe({
        next: (x) => {
          const d = x as Record<string, unknown>;
          const scope =
            (d['scopeConsultantName'] as string | undefined) ||
            (d['scopeSalesRecruiterName'] as string | undefined) ||
            'Organization (all)';
          this.dailyChart = {
            labels: this.dailyMetricTitles,
            datasets: [
              {
                label: scope,
                data: [
                  Number(d['totalJobsApplied'] ?? 0),
                  Number(d['totalVendorReachOuts'] ?? 0),
                  Number(d['totalVendorResponses'] ?? 0),
                  Number(d['totalSubmissions'] ?? 0),
                  Number(d['totalInterviewCalls'] ?? 0)
                ],
                backgroundColor: ['#1e3a8a', '#2563eb', '#3b82f6', '#60a5fa', '#93c5fd']
              }
            ]
          };
        },
        error: () => this.toast.error('Failed to load daily report')
      });
  }

  loadWeekly(): void {
    if (!this.validateWeeklySummarySelection()) return;
    this.reports
      .weeklySummary(
        this.reportDayKey(this.wStartDate),
        this.reportDayKey(this.wEndDate),
        this.weeklyScopeConsultantId(),
        this.weeklyScopeSalesId()
      )
      .subscribe({
        next: (x) => {
          const d = x as Record<string, unknown>;
          const scope =
            (d['scopeConsultantName'] as string | undefined) ||
            (d['scopeSalesRecruiterName'] as string | undefined) ||
            'Organization (all)';
          this.weeklyChart = {
            labels: this.weeklyMetricTitles,
            datasets: [
              {
                label: scope,
                data: [
                  Number(d['totalJobsApplied'] ?? 0),
                  Number(d['totalVendorReachOuts'] ?? 0),
                  Number(d['totalVendorResponses'] ?? 0),
                  Number(d['totalSubmissions'] ?? 0),
                  Number(d['totalInterviews'] ?? 0)
                ],
                backgroundColor: '#2563eb'
              }
            ]
          };
        },
        error: () => this.toast.error('Failed to load weekly report')
      });
  }

  loadPerf(): void {
    const cid = this.perfConsultantFilterId > 0 ? this.perfConsultantFilterId : null;
    this.reports.consultantPerformance(cid).subscribe({
      next: (rows) => {
        const list = rows as { name?: string; submissions?: number; interviews?: number }[];
        const top = list.slice(0, 12);
        this.perfChart = {
          labels: top.map((r) => r.name ?? ''),
          datasets: [
            {
              label: 'Submissions',
              data: top.map((r) => Number(r.submissions ?? 0)),
              backgroundColor: '#1e3a8a'
            },
            {
              label: 'Interviews',
              data: top.map((r) => Number(r.interviews ?? 0)),
              backgroundColor: '#059669'
            }
          ]
        };
      },
      error: () => this.toast.error('Failed to load consultant performance')
    });
  }

  loadSales(): void {
    const sid = this.perfSalesFilterId > 0 ? this.perfSalesFilterId : null;
    this.reports.salesPerformance(sid).subscribe({
      next: (rows) => {
        const list = rows as { name?: string; submissions?: number; interviews?: number }[];
        const top = list.slice(0, 12);
        this.salesChart = {
          labels: top.map((r) => r.name ?? ''),
          datasets: [
            {
              label: 'Submissions',
              data: top.map((r) => Number(r.submissions ?? 0)),
              backgroundColor: '#059669'
            },
            {
              label: 'Interviews',
              data: top.map((r) => Number(r.interviews ?? 0)),
              backgroundColor: '#1e3a8a'
            }
          ]
        };
      },
      error: () => this.toast.error('Failed to load sales performance')
    });
  }
}
