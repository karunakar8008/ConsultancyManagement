import { DatePipe } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
import { ChartData, ChartOptions, TooltipItem } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';
import { ReportsService } from '../../core/services/reports.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-management-reports',
  standalone: true,
  imports: [DatePipe, MatCardModule, MatButtonModule, MatTableModule, BaseChartDirective],
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
      }
      .csv-row {
        margin-top: 0.25rem;
      }
      .chart-wrap {
        position: relative;
        height: 360px;
        max-width: 960px;
      }
      .full-table {
        width: 100%;
      }
    `
  ]
})
export class ManagementReportsComponent implements OnInit {
  private readonly reports = inject(ReportsService);
  private readonly toast = inject(ToastrService);

  view: 'perf' | 'sales' | 'subs' | 'onb' = 'perf';
  subsCols = ['consultant', 'sales', 'vendor', 'job', 'date', 'status'];
  subsRows: Record<string, unknown>[] = [];
  onbCols = ['name', 'total', 'done', 'pend'];
  onbRows: Record<string, unknown>[] = [];

  perfChart: ChartData<'bar'> = { labels: [], datasets: [] };
  salesChart: ChartData<'bar'> = { labels: [], datasets: [] };

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
    this.load('perf');
  }

  load(which: 'perf' | 'sales' | 'subs' | 'onb'): void {
    this.view = which;
    const obs =
      which === 'perf'
        ? this.reports.consultantPerformance()
        : which === 'sales'
          ? this.reports.salesPerformance()
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
    const obs =
      which === 'perf'
        ? this.reports.consultantPerformanceCsv()
        : which === 'sales'
          ? this.reports.salesPerformanceCsv()
          : which === 'subs'
            ? this.reports.submissionsCsv()
            : this.reports.onboardingStatusCsv();
    const name =
      which === 'perf'
        ? 'consultant-performance.csv'
        : which === 'sales'
          ? 'sales-performance.csv'
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
}
