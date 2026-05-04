import { DatePipe } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ConsultantApiService, ConsultantSubmissionRow } from '../../core/services/consultant-api.service';
import { openBlobInNewTab, triggerBlobDownload } from '../../core/utils/blob-actions';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-consultant-submissions',
  standalone: true,
  imports: [DatePipe, MatCardModule, MatTableModule, MatButtonModule, MatIconModule, MatTooltipModule],
  template: `
    <h2 class="page-title">Submissions</h2>
    <p class="hint">Created by your sales team. Read-only here. Download submission proof when your recruiter attached one.</p>
    <mat-card class="panel">
      <table mat-table [dataSource]="rows" class="full-table">
        <ng-container matColumnDef="code"><th mat-header-cell *matHeaderCellDef>Submission ID</th><td mat-cell *matCellDef="let r">{{ r.submissionCode }}</td></ng-container>
        <ng-container matColumnDef="job"><th mat-header-cell *matHeaderCellDef>Job</th><td mat-cell *matCellDef="let r">{{ r.jobTitle }}</td></ng-container>
        <ng-container matColumnDef="client"><th mat-header-cell *matHeaderCellDef>Client</th><td mat-cell *matCellDef="let r">{{ r.clientName }}</td></ng-container>
        <ng-container matColumnDef="vendor"><th mat-header-cell *matHeaderCellDef>Vendor</th><td mat-cell *matCellDef="let r">{{ r.vendorName }}</td></ng-container>
        <ng-container matColumnDef="sales"><th mat-header-cell *matHeaderCellDef>Sales</th><td mat-cell *matCellDef="let r">{{ r.salesRecruiterName }}</td></ng-container>
        <ng-container matColumnDef="date"><th mat-header-cell *matHeaderCellDef>Date</th><td mat-cell *matCellDef="let r">{{ r.submissionDate | date }}</td></ng-container>
        <ng-container matColumnDef="status"><th mat-header-cell *matHeaderCellDef>Status</th><td mat-cell *matCellDef="let r">{{ r.status }}</td></ng-container>
        <ng-container matColumnDef="proof">
          <th mat-header-cell *matHeaderCellDef>Proof</th>
          <td mat-cell *matCellDef="let r">
            @if (r.hasProof) {
              <span class="icon-actions">
                <button mat-icon-button type="button" color="primary" matTooltip="View" (click)="viewProof(r)"><mat-icon>visibility</mat-icon></button>
                <button mat-icon-button type="button" matTooltip="Download" (click)="downloadProof(r)"><mat-icon>download</mat-icon></button>
              </span>
            } @else {
              —
            }
          </td>
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
      .hint {
        color: rgba(0, 0, 0, 0.65);
        margin: 0 0 0.75rem;
        font-size: 0.9rem;
      }
      .icon-actions {
        display: inline-flex;
        gap: 0.15rem;
        align-items: center;
      }
    `
  ]
})
export class ConsultantSubmissionsComponent implements OnInit {
  private readonly api = inject(ConsultantApiService);
  private readonly toast = inject(ToastrService);
  cols = ['code', 'job', 'client', 'vendor', 'sales', 'date', 'status', 'proof'];
  rows: ConsultantSubmissionRow[] = [];

  ngOnInit(): void {
    this.api.submissions().subscribe({
      next: (r) => (this.rows = r),
      error: () => this.toast.error('Failed to load')
    });
  }

  viewProof(r: ConsultantSubmissionRow): void {
    this.api.downloadProof('submission', r.id, true).subscribe({
      next: (blob) => openBlobInNewTab(blob),
      error: () => this.toast.error('Could not open file')
    });
  }

  downloadProof(r: ConsultantSubmissionRow): void {
    this.api.downloadProof('submission', r.id, false).subscribe({
      next: (blob) => triggerBlobDownload(blob, `${r.submissionCode}-proof`),
      error: () => this.toast.error('Download failed')
    });
  }
}
