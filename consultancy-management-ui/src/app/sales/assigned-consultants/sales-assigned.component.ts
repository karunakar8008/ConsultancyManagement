import { Component, inject, OnInit } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { AssignedConsultantOption, SalesService } from '../../core/services/sales.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-sales-assigned',
  standalone: true,
  imports: [MatCardModule, MatTableModule],
  template: `
    <h2 class="page-title">Assigned Consultants</h2>
    <mat-card class="panel">
      <table mat-table [dataSource]="rows" class="full-table">
        <ng-container matColumnDef="name"><th mat-header-cell *matHeaderCellDef>Name</th><td mat-cell *matCellDef="let r">{{ r.firstName }} {{ r.lastName }}</td></ng-container>
        <ng-container matColumnDef="tech"><th mat-header-cell *matHeaderCellDef>Tech</th><td mat-cell *matCellDef="let r">{{ r.technology }}</td></ng-container>
        <ng-container matColumnDef="visa"><th mat-header-cell *matHeaderCellDef>Visa</th><td mat-cell *matCellDef="let r">{{ r.visaStatus }}</td></ng-container>
        <ng-container matColumnDef="loc"><th mat-header-cell *matHeaderCellDef>Location</th><td mat-cell *matCellDef="let r">{{ r.currentLocation }}</td></ng-container>
        <ng-container matColumnDef="status"><th mat-header-cell *matHeaderCellDef>Status</th><td mat-cell *matCellDef="let r">{{ r.status }}</td></ng-container>
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
export class SalesAssignedComponent implements OnInit {
  private readonly api = inject(SalesService);
  private readonly toast = inject(ToastrService);
  cols = ['name', 'tech', 'visa', 'loc', 'status'];
  rows: AssignedConsultantOption[] = [];
  ngOnInit(): void {
    this.api.assignedConsultants().subscribe({
      next: (r) => (this.rows = r),
      error: () => this.toast.error('Failed to load')
    });
  }
}
