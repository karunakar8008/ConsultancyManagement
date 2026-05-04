import { Component, inject, OnInit } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatGridListModule } from '@angular/material/grid-list';
import { ManagementService } from '../../core/services/management.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-management-dashboard',
  standalone: true,
  imports: [MatCardModule, MatGridListModule],
  template: `
    <h2 class="page-title">Management Dashboard</h2>
    <mat-grid-list cols="5" rowHeight="110px" gutterSize="12px">
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
        padding: 1rem;
        box-sizing: border-box;
      }
      .label {
        color: #6b7280;
        font-size: 0.85rem;
      }
      .value {
        font-size: 1.5rem;
        font-weight: 700;
        color: var(--cms-primary);
      }
    `
  ]
})
export class ManagementDashboardComponent implements OnInit {
  private readonly api = inject(ManagementService);
  private readonly toast = inject(ToastrService);
  cards: { label: string; value: number }[] = [];
  ngOnInit(): void {
    this.api.dashboard().subscribe({
      next: (d) =>
        (this.cards = [
          { label: 'Consultants', value: d['totalConsultants'] ?? 0 },
          { label: 'Pending onboarding', value: d['pendingOnboarding'] ?? 0 },
          { label: 'Pending documents', value: d['pendingDocuments'] ?? 0 },
          { label: 'Submissions', value: d['totalSubmissions'] ?? 0 },
          { label: 'Interviews scheduled', value: d['interviewsScheduled'] ?? 0 }
        ]),
      error: () => this.toast.error('Failed to load')
    });
  }
}
