import { DatePipe } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { ManagementService } from '../../core/services/management.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-management-submissions',
  standalone: true,
  imports: [DatePipe, MatCardModule, MatTableModule],
  template: `
    <h2 class="page-title">Submissions</h2>
    <mat-card class="panel">
      <table mat-table [dataSource]="rows" class="full-table">
        <ng-container matColumnDef="consultant">
          <th mat-header-cell *matHeaderCellDef>Consultant</th>
          <td mat-cell *matCellDef="let r">{{ r['consultantName'] }}</td>
        </ng-container>
        <ng-container matColumnDef="sales">
          <th mat-header-cell *matHeaderCellDef>Sales</th>
          <td mat-cell *matCellDef="let r">{{ r['salesRecruiterName'] }}</td>
        </ng-container>
        <ng-container matColumnDef="vendor">
          <th mat-header-cell *matHeaderCellDef>Vendor</th>
          <td mat-cell *matCellDef="let r">{{ r['vendorName'] }}</td>
        </ng-container>
        <ng-container matColumnDef="job">
          <th mat-header-cell *matHeaderCellDef>Job</th>
          <td mat-cell *matCellDef="let r">{{ r['jobTitle'] }}</td>
        </ng-container>
        <ng-container matColumnDef="date">
          <th mat-header-cell *matHeaderCellDef>Date</th>
          <td mat-cell *matCellDef="let r">{{ r['submissionDate'] | date: 'mediumDate' }}</td>
        </ng-container>
        <ng-container matColumnDef="status">
          <th mat-header-cell *matHeaderCellDef>Status</th>
          <td mat-cell *matCellDef="let r">{{ r['status'] }}</td>
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
    `
  ]
})
export class ManagementSubmissionsComponent implements OnInit {
  private readonly api = inject(ManagementService);
  private readonly toast = inject(ToastrService);
  cols = ['consultant', 'sales', 'vendor', 'job', 'date', 'status'];
  rows: Record<string, unknown>[] = [];

  ngOnInit(): void {
    this.api.submissions().subscribe({
      next: (r) => (this.rows = r as Record<string, unknown>[]),
      error: () => this.toast.error('Failed to load submissions')
    });
  }
}
