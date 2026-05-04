import { DatePipe } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { ConsultantApiService } from '../../core/services/consultant-api.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-consultant-jobs',
  standalone: true,
  imports: [DatePipe, ReactiveFormsModule, MatCardModule, MatTableModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  templateUrl: './consultant-jobs.component.html',
  styleUrl: '../daily-activities/consultant-shared.scss'
})
export class ConsultantJobsComponent implements OnInit {
  private readonly api = inject(ConsultantApiService);
  private readonly toast = inject(ToastrService);
  private readonly fb = inject(FormBuilder);
  cols = ['title', 'company', 'client', 'source', 'applied', 'status'];
  rows: Record<string, unknown>[] = [];
  form = this.fb.nonNullable.group({
    jobTitle: ['', Validators.required],
    companyName: [''],
    clientName: [''],
    source: [''],
    jobUrl: [''],
    appliedDate: [new Date().toISOString().slice(0, 10), Validators.required],
    status: ['Applied'],
    notes: ['']
  });
  ngOnInit(): void {
    this.reload();
  }
  reload(): void {
    this.api.jobApplications().subscribe({
      next: (r) => (this.rows = r as Record<string, unknown>[]),
      error: () => this.toast.error('Failed to load')
    });
  }
  save(): void {
    if (this.form.invalid) return this.form.markAllAsTouched();
    const v = this.form.getRawValue();
    this.api
      .saveJob({
        ...v,
        appliedDate: new Date(v.appliedDate).toISOString()
      } as Record<string, unknown>)
      .subscribe({
        next: () => {
          this.toast.success('Saved');
          this.reload();
        },
        error: (e: { error?: { message?: string } }) => this.toast.error(e?.error?.message ?? 'Error')
      });
  }
}
