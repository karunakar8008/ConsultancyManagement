import { DatePipe } from '@angular/common';
import { AfterViewInit, Component, inject, OnInit } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatSelectModule } from '@angular/material/select';
import { FormsModule } from '@angular/forms';
import { ChartData, ChartOptions, TooltipItem } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';
import { ReportsService } from '../../core/services/reports.service';
import { ManagementService } from '../../core/services/management.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-management-reports',
  standalone: true,
  imports: [
    DatePipe,
    MatCardModule,
    MatButtonModule,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatSelectModule,
    FormsModule,
    BaseChartDirective
  ],
  templateUrl: './management-reports.component.html',
  styles: [
    `
      .panel {
        padding: 1rem;
        margin-bottom: 1rem;
      }
      .row {
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem;
        margin-bottom: 1rem;
        align-items: center;
      }
      .csv-row {
        margin-top: 0.25rem;
      }
      .chart-wrap {
        position: relative;
        height: 320px;
        max-width: 960px;
      }
      .chart-wrap.tall {
        height: 360px;
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
      .full-table {
        width: 100%;
      }
      .subs-notes-cell {
        max-width: 14rem;
        white-space: pre-wrap;
        word-break: break-word;
        font-size: 0.875rem;
        vertical-align: top;
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
export class ManagementReportsComponent implements OnInit, AfterViewInit {
  private readonly reports = inject(ReportsService);
  private readonly mgmt = inject(ManagementService);
  private readonly toast = inject(ToastrService);

  view: 'perf' | 'sales' | 'subs' | 'onb' = 'perf';
  subsCols = ['consultant', 'sales', 'vendor', 'job', 'date', 'status', 'notes'];
  subsRows: Record<string, unknown>[] = [];
  onbCols = ['name', 'total', 'done', 'pend'];
  onbRows: Record<string, unknown>[] = [];

  consultants: { id: number; name: string }[] = [];
  salesRecruiters: { id: number; name: string }[] = [];

  dailySummaryScope: 'all' | 'consultant' | 'sales' = 'all';
  dailySummaryConsultantId: number | null = null;
  dailySummarySalesId: number | null = null;
  weeklySummaryScope: 'all' | 'consultant' | 'sales' = 'all';
  weeklySummaryConsultantId: number | null = null;
  weeklySummarySalesId: number | null = null;
  dayDate = new Date();
  wStartDate = new Date(Date.now() - 7 * 86400000);
  wEndDate = new Date();
  perfConsultantFilterId = 0;
  perfSalesFilterId = 0;

  perfChart: ChartData<'bar'> = { labels: [], datasets: [] };
  salesChart: ChartData<'bar'> = { labels: [], datasets: [] };

  private readonly dailyMetricTitles = [
    'Jobs applied',
    'Vendor reach',
    'Vendor responses',
    'Submissions',
    'Interview calls'
  ];
  private readonly weeklyMetricTitles = [
    'Jobs applied',
    'Vendor reach',
    'Vendor responses',
    'Submissions',
    'Interviews'
  ];

  dailyChart: ChartData<'bar'> = { labels: [], datasets: [] };
  dailyChartOptions: ChartOptions<'bar'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { display: false } },
    scales: { y: { beginAtZero: true, ticks: { stepSize: 5, precision: 0 } } }
  };

  weeklyChart: ChartData<'bar'> = { labels: [], datasets: [] };
  weeklyChartOptions: ChartOptions<'bar'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { display: false } },
    scales: { y: { beginAtZero: true, ticks: { stepSize: 5, precision: 0 } } }
  };

  private readonly perfTitle = 'Consultant performance (submissions)';
  private readonly salesTitle = 'Sales performance (submissions)';

  barOptions: ChartOptions<'bar'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
      tooltip: {
        callbacks: {
          title: (items: TooltipItem<'bar'>[]) => items[0]?.label ?? '',
          afterBody: (items: TooltipItem<'bar'>[]) => {
            const ctx = items[0];
            if (!ctx) return [];
            return [
              this.view === 'perf'
                ? `Report: ${this.perfTitle}. Each bar is total submission rows linked to that consultant (not deleted users).`
                : `Report: ${this.salesTitle}. Each bar is submission rows attributed to that sales recruiter.`
            ];
          }
        }
      }
    },
    scales: { x: { ticks: { maxRotation: 45, minRotation: 20 } } }
  };

  ngOnInit(): void {
    this.mgmt.consultants().subscribe({
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
    this.mgmt.salesRecruiters().subscribe({
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
      this.load('perf');
    });
  }

  private reportDayKey(d: Date): string {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
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

  load(which: 'perf' | 'sales' | 'subs' | 'onb'): void {
    this.view = which;
    const cid = this.perfConsultantFilterId > 0 ? this.perfConsultantFilterId : null;
    const sid = this.perfSalesFilterId > 0 ? this.perfSalesFilterId : null;
    const obs =
      which === 'perf'
        ? this.reports.consultantPerformance(cid)
        : which === 'sales'
          ? this.reports.salesPerformance(sid)
          : which === 'subs'
            ? this.reports.submissions()
            : this.reports.onboardingStatus();

    obs.subscribe({
      next: (x) => {
        if (which === 'perf') this.buildPerfChart(x);
        if (which === 'sales') this.buildSalesChart(x);
        if (which === 'subs') this.subsRows = x as Record<string, unknown>[];
        if (which === 'onb') this.onbRows = x as Record<string, unknown>[];
      },
      error: () => this.toast.error('Failed to load report')
    });
  }

  downloadCsv(which: 'perf' | 'sales' | 'subs' | 'onb'): void {
    const cid = this.perfConsultantFilterId > 0 ? this.perfConsultantFilterId : null;
    const sid = this.perfSalesFilterId > 0 ? this.perfSalesFilterId : null;
    const obs =
      which === 'perf'
        ? this.reports.consultantPerformanceCsv(cid)
        : which === 'sales'
          ? this.reports.salesPerformanceCsv(sid)
          : which === 'subs'
            ? this.reports.submissionsCsv()
            : this.reports.onboardingStatusCsv();
    const name =
      which === 'perf'
        ? cid
          ? `consultant-performance-${cid}.csv`
          : 'consultant-performance.csv'
        : which === 'sales'
          ? sid
            ? `sales-performance-${sid}.csv`
            : 'sales-performance.csv'
          : which === 'subs'
            ? 'submissions-report.csv'
            : 'onboarding-status.csv';
    obs.subscribe({
      next: (blob) => this.saveBlob(blob, name),
      error: () => this.toast.error('Download failed')
    });
  }

  private saveBlob(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
  }

  private buildPerfChart(x: unknown): void {
    const list = x as { name?: string; submissions?: number }[];
    const top = list.slice(0, 14);
    this.perfChart = {
      labels: top.map((r) => r.name ?? ''),
      datasets: [
        {
          label: 'Submissions',
          data: top.map((r) => Number(r.submissions ?? 0)),
          backgroundColor: '#1e3a8a'
        }
      ]
    };
  }

  private buildSalesChart(x: unknown): void {
    const list = x as { name?: string; submissions?: number }[];
    const top = list.slice(0, 14);
    this.salesChart = {
      labels: top.map((r) => r.name ?? ''),
      datasets: [
        {
          label: 'Submissions',
          data: top.map((r) => Number(r.submissions ?? 0)),
          backgroundColor: '#059669'
        }
      ]
    };
  }

  subsNoteCell(value: unknown): string {
    const t = typeof value === 'string' ? value.trim() : '';
    return t ? t : '—';
  }
}
