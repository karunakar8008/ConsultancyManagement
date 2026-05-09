import { DatePipe } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { ManagementService } from '../../core/services/management.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-management-interviews',
  standalone: true,
  imports: [DatePipe, MatCardModule, MatTableModule],
  template: `
    <h2 class="page-title">Interviews</h2>
    <mat-card class="panel">
      <table mat-table [dataSource]="rows" class="full-table">
        <ng-container matColumnDef="consultant">
          <th mat-header-cell *matHeaderCellDef>Consultant</th>
          <td mat-cell *matCellDef="let r">{{ r['consultantName'] }}</td>
        </ng-container>
        <ng-container matColumnDef="job">
          <th mat-header-cell *matHeaderCellDef>Job</th>
          <td mat-cell *matCellDef="let r">{{ r['jobTitle'] }}</td>
        </ng-container>
        <ng-container matColumnDef="when">
          <th mat-header-cell *matHeaderCellDef>From</th>
          <td mat-cell *matCellDef="let r">{{ r['interviewDate'] | date: 'short' }}</td>
        </ng-container>
        <ng-container matColumnDef="to">
          <th mat-header-cell *matHeaderCellDef>To</th>
          <td mat-cell *matCellDef="let r">{{ r['interviewEndDate'] ? (r['interviewEndDate'] | date: 'short') : '—' }}</td>
        </ng-container>
        <ng-container matColumnDef="mode">
          <th mat-header-cell *matHeaderCellDef>Mode</th>
          <td mat-cell *matCellDef="let r">{{ r['mode'] }}</td>
        </ng-container>
        <ng-container matColumnDef="status">
          <th mat-header-cell *matHeaderCellDef>Status</th>
          <td mat-cell *matCellDef="let r">{{ r['status'] }}</td>
        </ng-container>
        <ng-container matColumnDef="feedback">
          <th mat-header-cell *matHeaderCellDef>Feedback</th>
          <td mat-cell *matCellDef="let r" class="text-cell">{{ textCell(r['feedback']) }}</td>
        </ng-container>
        <ng-container matColumnDef="notes">
          <th mat-header-cell *matHeaderCellDef>Notes</th>
          <td mat-cell *matCellDef="let r" class="text-cell">{{ textCell(r['notes']) }}</td>
        </ng-container>
        <tr mat-header-row *matHeaderRowDef="cols"></tr>
        <tr mat-row *matRowDef="let row; columns: cols"></tr>
      </table>
    </mat-card>
  `,
  styles: [
    `
      .panel {
        padding: 1rem;
      }
      .full-table {
        width: 100%;
      }
      .text-cell {
        max-width: 14rem;
        white-space: pre-wrap;
        word-break: break-word;
        font-size: 0.875rem;
        vertical-align: top;
      }
    `
  ]
})
export class ManagementInterviewsComponent implements OnInit {
  private readonly api = inject(ManagementService);
  private readonly toast = inject(ToastrService);
  cols = ['consultant', 'job', 'when', 'to', 'mode', 'status', 'feedback', 'notes'];
  rows: Record<string, unknown>[] = [];

  ngOnInit(): void {
    this.api.interviews().subscribe({
      next: (r) => (this.rows = r as Record<string, unknown>[]),
      error: () => this.toast.error('Failed to load interviews')
    });
  }

  textCell(value: unknown): string {
    const t = typeof value === 'string' ? value.trim() : '';
    return t ? t : '—';
  }
}
