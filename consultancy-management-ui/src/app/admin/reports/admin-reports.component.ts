import { Component, inject, OnInit } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatExpansionModule } from '@angular/material/expansion';
import { FormsModule } from '@angular/forms';
import { JsonPipe } from '@angular/common';
import { ChartData, ChartOptions, TooltipItem } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';
import { ReportsService } from '../../core/services/reports.service';
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
    MatExpansionModule,
    FormsModule,
    JsonPipe,
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
      .json {
        background: #0f172a;
        color: #e5e7eb;
        padding: 1rem;
        border-radius: 8px;
        overflow: auto;
        max-height: 280px;
        font-size: 0.8rem;
      }
    `
  ]
})
export class AdminReportsComponent implements OnInit {
  private readonly reports = inject(ReportsService);
  private readonly toast = inject(ToastrService);

  dayDate = new Date();
  wStartDate = new Date(Date.now() - 7 * 86400000);
  wEndDate = new Date();
  payload: unknown = null;

  private readonly dailyMetricTitles = [
    'Jobs applied',
    'Vendor reach',
    'Vendor responses',
    'Submissions',
    'Interview calls'
  ];
  private readonly dailyMetricHints = [
    'Source: sum of DailyActivities.JobsAppliedCount for the selected UTC date.',
    'Source: sum of DailyActivities.VendorReachedOutCount for the selected UTC date.',
    'Source: sum of DailyActivities.VendorResponseCount for the selected UTC date.',
    'Source: count of Submissions rows with SubmissionDate on the selected UTC date.',
    'Source: count of Interviews with InterviewDate on the selected UTC date (as proxy for interview calls).'
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
    this.loadDaily();
    this.loadWeekly();
    this.loadPerf();
    this.loadSales();
  }

  private iso(d: Date): string {
    return d.toISOString().slice(0, 10);
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
    this.reports.dailySummaryCsv(this.iso(this.dayDate)).subscribe({
      next: (b) => this.saveBlob(b, `daily-summary-${this.iso(this.dayDate)}.csv`),
      error: () => this.toast.error('Download failed')
    });
  }

  downloadWeeklyCsv(): void {
    this.reports.weeklySummaryCsv(this.iso(this.wStartDate), this.iso(this.wEndDate)).subscribe({
      next: (b) =>
        this.saveBlob(b, `weekly-summary-${this.iso(this.wStartDate)}-to-${this.iso(this.wEndDate)}.csv`),
      error: () => this.toast.error('Download failed')
    });
  }

  downloadPerfCsv(): void {
    this.reports.consultantPerformanceCsv().subscribe({
      next: (b) => this.saveBlob(b, 'consultant-performance.csv'),
      error: () => this.toast.error('Download failed')
    });
  }

  downloadSalesCsv(): void {
    this.reports.salesPerformanceCsv().subscribe({
      next: (b) => this.saveBlob(b, 'sales-performance.csv'),
      error: () => this.toast.error('Download failed')
    });
  }

  loadDaily(): void {
    this.reports.dailySummary(this.iso(this.dayDate)).subscribe({
      next: (x) => {
        this.payload = x;
        const d = x as Record<string, unknown>;
        this.dailyChart = {
          labels: this.dailyMetricTitles,
          datasets: [
            {
              label: 'Daily totals',
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
    this.reports.weeklySummary(this.iso(this.wStartDate), this.iso(this.wEndDate)).subscribe({
      next: (x) => {
        const d = x as Record<string, unknown>;
        this.weeklyChart = {
          labels: this.weeklyMetricTitles,
          datasets: [
            {
              label: 'Week range',
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
    this.reports.consultantPerformance().subscribe({
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
    this.reports.salesPerformance().subscribe({
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
