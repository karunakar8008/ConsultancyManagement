import { DatePipe } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { ConsultantApiService, ConsultantVendorReachOutRow } from '../../core/services/consultant-api.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-consultant-vendor-reachouts',
  standalone: true,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatCardModule,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule
  ],
  template: `
    <h2 class="page-title">Vendors reached</h2>
    <p class="hint">Log each vendor you contacted. These entries feed the <strong>Vendors reached</strong> count on your daily activity for that date.</p>
    <mat-card class="panel">
      <form [formGroup]="form" (ngSubmit)="save()" class="grid-form">
        <mat-form-field appearance="outline"><mat-label>Date</mat-label><input matInput type="date" formControlName="reachedDate" /></mat-form-field>
        <mat-form-field appearance="outline" class="wide"><mat-label>Vendor name</mat-label><input matInput formControlName="vendorName" /></mat-form-field>
        <mat-form-field appearance="outline" class="wide"><mat-label>Notes</mat-label><input matInput formControlName="notes" /></mat-form-field>
        <button mat-flat-button color="primary" type="submit">Add</button>
      </form>
    </mat-card>
    <mat-card class="panel">
      <table mat-table [dataSource]="rows" class="full-table">
        <ng-container matColumnDef="date">
          <th mat-header-cell *matHeaderCellDef>Date</th>
          <td mat-cell *matCellDef="let r">{{ r.reachedDate | date }}</td>
        </ng-container>
        <ng-container matColumnDef="vendor">
          <th mat-header-cell *matHeaderCellDef>Vendor</th>
          <td mat-cell *matCellDef="let r">{{ r.vendorName }}</td>
        </ng-container>
        <ng-container matColumnDef="notes">
          <th mat-header-cell *matHeaderCellDef>Notes</th>
          <td mat-cell *matCellDef="let r">{{ r.notes }}</td>
        </ng-container>
        <tr mat-header-row *matHeaderRowDef="cols"></tr>
        <tr mat-row *matRowDef="let row; columns: cols"></tr>
      </table>
    </mat-card>
  `,
  styleUrls: ['./consultant-vendor-reachouts.component.scss', '../daily-activities/consultant-shared.scss']
})
export class ConsultantVendorReachoutsComponent implements OnInit {
  private readonly api = inject(ConsultantApiService);
  private readonly toast = inject(ToastrService);
  private readonly fb = inject(FormBuilder);
  cols = ['date', 'vendor', 'notes'];
  rows: ConsultantVendorReachOutRow[] = [];

  form = this.fb.nonNullable.group({
    reachedDate: [new Date().toISOString().slice(0, 10), Validators.required],
    vendorName: ['', Validators.required],
    notes: ['']
  });

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.api.vendorReachOuts().subscribe({
      next: (r) => (this.rows = r),
      error: () => this.toast.error('Failed to load')
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    this.api
      .createVendorReachOut({
        reachedDate: new Date(v.reachedDate).toISOString(),
        vendorName: v.vendorName.trim(),
        notes: v.notes.trim() || null
      })
      .subscribe({
        next: () => {
          this.toast.success('Saved');
          this.form.patchValue({ vendorName: '', notes: '' });
          this.reload();
        },
        error: (e: { error?: { message?: string } }) => this.toast.error(e?.error?.message ?? 'Error')
      });
  }
}
