import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { SalesService, VendorRow } from '../../core/services/sales.service';
import { openBlobInNewTab, triggerBlobDownload } from '../../core/utils/blob-actions';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-sales-vendors',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule
  ],
  templateUrl: './sales-vendors.component.html',
  styleUrls: ['./sales-vendors.component.scss', '../../consultant/daily-activities/consultant-shared.scss']
})
export class SalesVendorsComponent implements OnInit {
  private readonly api = inject(SalesService);
  private readonly toast = inject(ToastrService);
  private readonly fb = inject(FormBuilder);

  cols = ['code', 'vendor', 'contact', 'email', 'phone', 'company', 'proof', 'actions'];
  rows: VendorRow[] = [];
  editingId: number | null = null;
  contactProofFile: File | null = null;

  form = this.fb.nonNullable.group({
    vendorName: ['', Validators.required],
    contactPerson: [''],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: ['', [Validators.required, Validators.minLength(7)]],
    companyName: [''],
    linkedInUrl: [''],
    notes: ['']
  });

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.api.vendors().subscribe({
      next: (r) => (this.rows = r),
      error: () => this.toast.error('Failed to load vendors')
    });
  }

  onProofSelected(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    this.contactProofFile = input.files?.[0] ?? null;
  }

  cancelEdit(): void {
    this.editingId = null;
    this.contactProofFile = null;
    this.form.reset({
        vendorName: '',
        contactPerson: '',
        email: '',
        phoneNumber: '',
        companyName: '',
        linkedInUrl: '',
        notes: ''
      });
  }

  startEdit(row: VendorRow): void {
    this.editingId = row.id;
    this.contactProofFile = null;
    this.form.patchValue({
      vendorName: row.vendorName,
      contactPerson: row.contactPerson ?? '',
      email: row.email,
      phoneNumber: row.phoneNumber,
      companyName: row.companyName ?? '',
      linkedInUrl: row.linkedInUrl ?? '',
      notes: row.notes ?? ''
    });
  }

  viewProof(row: VendorRow): void {
    this.api.downloadProof('vendor', row.id, true).subscribe({
      next: (blob) => openBlobInNewTab(blob),
      error: () => this.toast.error('Could not open file')
    });
  }

  downloadProof(row: VendorRow): void {
    this.api.downloadProof('vendor', row.id, false).subscribe({
      next: (blob) => triggerBlobDownload(blob, `${row.vendorCode}-contact`),
      error: () => this.toast.error('Download failed')
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const payload = {
      vendorName: v.vendorName.trim(),
      contactPerson: v.contactPerson.trim(),
      email: v.email.trim(),
      phoneNumber: v.phoneNumber.trim(),
      companyName: v.companyName.trim(),
      linkedInUrl: v.linkedInUrl.trim(),
      notes: v.notes.trim()
    };

    if (this.editingId != null) {
      this.api.updateVendor(this.editingId, payload, this.contactProofFile).subscribe({
        next: () => {
          this.toast.success('Vendor updated');
          this.cancelEdit();
          this.reload();
        },
        error: (e: { error?: { message?: string } }) => this.toast.error(e?.error?.message ?? 'Update failed')
      });
    } else {
      this.api.createVendor(payload, this.contactProofFile).subscribe({
        next: () => {
          this.toast.success('Vendor created');
          this.contactProofFile = null;
          this.form.reset({
            vendorName: '',
            contactPerson: '',
            email: '',
            phoneNumber: '',
            companyName: '',
            linkedInUrl: '',
            notes: ''
          });
          this.reload();
        },
        error: (e: { error?: { message?: string } }) => this.toast.error(e?.error?.message ?? 'Save failed')
      });
    }
  }
}
