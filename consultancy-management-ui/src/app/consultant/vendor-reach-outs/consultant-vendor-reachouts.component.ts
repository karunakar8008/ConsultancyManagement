import { DatePipe } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
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
    MatButtonModule,
    MatIconModule
  ],
  templateUrl: './consultant-vendor-reachouts.component.html',
  styleUrls: ['./consultant-vendor-reachouts.component.scss', '../daily-activities/consultant-shared.scss']
})
export class ConsultantVendorReachoutsComponent implements OnInit {
  private readonly api = inject(ConsultantApiService);
  private readonly toast = inject(ToastrService);
  private readonly fb = inject(FormBuilder);

  cols = ['date', 'vendor', 'contact', 'email', 'response', 'notes', 'actions'];
  rows: ConsultantVendorReachOutRow[] = [];
  editingId: number | null = null;

  form = this.fb.nonNullable.group({
    reachedDate: [new Date().toISOString().slice(0, 10), Validators.required],
    vendorName: ['', Validators.required],
    contactPerson: [''],
    contactEmail: [''],
    vendorResponseNotes: [''],
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

  textOrDash(value: string | null | undefined): string {
    const t = value?.trim();
    return t ? t : '—';
  }

  startEdit(r: ConsultantVendorReachOutRow): void {
    this.editingId = r.id;
    this.form.patchValue({
      reachedDate: new Date(r.reachedDate).toISOString().slice(0, 10),
      vendorName: r.vendorName,
      contactPerson: r.contactPerson ?? '',
      contactEmail: r.contactEmail ?? '',
      vendorResponseNotes: r.vendorResponseNotes ?? '',
      notes: r.notes ?? ''
    });
  }

  cancelEdit(): void {
    this.editingId = null;
    this.form.patchValue({
      reachedDate: new Date().toISOString().slice(0, 10),
      vendorName: '',
      contactPerson: '',
      contactEmail: '',
      vendorResponseNotes: '',
      notes: ''
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const body = {
      reachedDate: new Date(v.reachedDate).toISOString(),
      vendorName: v.vendorName.trim(),
      contactPerson: v.contactPerson.trim() || null,
      contactEmail: v.contactEmail.trim() || null,
      vendorResponseNotes: v.vendorResponseNotes.trim() || null,
      notes: v.notes.trim() || null
    };

    if (this.editingId != null) {
      this.api.updateVendorReachOut(this.editingId, body).subscribe({
        next: () => {
          this.toast.success('Updated');
          this.cancelEdit();
          this.reload();
        },
        error: (e: { error?: { message?: string } }) => this.toast.error(e?.error?.message ?? 'Error')
      });
      return;
    }

    this.api.createVendorReachOut(body).subscribe({
      next: () => {
        this.toast.success('Saved');
        this.form.patchValue({
          vendorName: '',
          contactPerson: '',
          contactEmail: '',
          vendorResponseNotes: '',
          notes: ''
        });
        this.reload();
      },
      error: (e: { error?: { message?: string } }) => this.toast.error(e?.error?.message ?? 'Error')
    });
  }
}
