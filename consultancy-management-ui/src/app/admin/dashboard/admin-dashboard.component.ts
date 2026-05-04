import { Component, inject, OnInit } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatGridListModule } from '@angular/material/grid-list';
import { AdminService } from '../../core/services/admin.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [MatCardModule, MatGridListModule],
  template: `
    <h2 class="page-title">Admin Dashboard</h2>
    <mat-grid-list cols="5" rowHeight="120px" gutterSize="12px">
      @for (c of cards; track c.label) {
        <mat-grid-tile>
          <mat-card class="metric">
            <div class="label">{{ c.label }}</div>
            <div class="value">{{ c.value }}</div>
          </mat-card>
        </mat-grid-tile>
      }
    </mat-grid-list>
  `,
  styles: [
    `
      .metric {
        width: 100%;
        height: 100%;
        box-sizing: border-box;
        padding: 1rem;
      }
      .label {
        color: #6b7280;
        font-size: 0.85rem;
      }
      .value {
        font-size: 1.75rem;
        font-weight: 700;
        color: var(--cms-primary);
      }
    `
  ]
})
export class AdminDashboardComponent implements OnInit {
  private readonly api = inject(AdminService);
  private readonly toast = inject(ToastrService);

  cards: { label: string; value: number }[] = [];

  ngOnInit(): void {
    this.api.dashboard().subscribe({
      next: (d) => {
        this.cards = [
          { label: 'Consultants', value: d['totalConsultants'] ?? 0 },
          { label: 'Sales Recruiters', value: d['totalSalesRecruiters'] ?? 0 },
          { label: 'Management', value: d['totalManagementUsers'] ?? 0 },
          { label: 'Today Applications', value: d['todayApplications'] ?? 0 },
          { label: 'Today Submissions', value: d['todaySubmissions'] ?? 0 },
          { label: 'Pending Documents', value: d['pendingDocuments'] ?? 0 }
        ];
      },
      error: () => this.toast.error('Failed to load dashboard')
    });
  }
}
