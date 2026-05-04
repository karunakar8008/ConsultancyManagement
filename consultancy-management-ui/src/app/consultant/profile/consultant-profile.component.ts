import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { ConsultantApiService } from '../../core/services/consultant-api.service';
import { ToastrService } from 'ngx-toastr';

interface ConsultantProfileVm {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
  visaStatus?: string | null;
  technology?: string | null;
  skillsNotes?: string | null;
  experienceYears?: number | null;
  currentLocation?: string | null;
  status: string;
}

@Component({
  selector: 'app-consultant-profile',
  standalone: true,
  imports: [ReactiveFormsModule, MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <h2 class="page-title">Profile</h2>
    @if (profile) {
      <mat-card class="panel">
        <h3>Contact (editable)</h3>
        <form [formGroup]="contactForm" (ngSubmit)="saveContact()" class="contact-grid">
          <mat-form-field appearance="outline" class="wide">
            <mat-label>Email</mat-label>
            <input matInput type="email" formControlName="email" autocomplete="email" />
          </mat-form-field>
          <mat-form-field appearance="outline" class="wide">
            <mat-label>Phone</mat-label>
            <input matInput type="tel" formControlName="phoneNumber" autocomplete="tel" />
          </mat-form-field>
          <button mat-flat-button color="primary" type="submit" [disabled]="contactForm.invalid || contactForm.pristine">
            Save contact
          </button>
        </form>
      </mat-card>
      <mat-card class="panel">
        <h3>Professional</h3>
        <p><strong>Name:</strong> {{ profile.firstName }} {{ profile.lastName }}</p>
        <p><strong>Visa:</strong> {{ profile.visaStatus || '—' }}</p>
        <p><strong>Technology:</strong> {{ profile.technology || '—' }}</p>
        <p><strong>Skills / stack notes:</strong> {{ profile.skillsNotes || '—' }}</p>
        <p><strong>Experience (years):</strong> {{ profile.experienceYears ?? '—' }}</p>
        <p><strong>Location:</strong> {{ profile.currentLocation || '—' }}</p>
        <p><strong>Status:</strong> {{ profile.status }}</p>
        <p class="muted">To change visa, technology, or assignment details, contact your administrator.</p>
      </mat-card>
    }
  `,
  styles: [
    `
      .panel {
        padding: 1rem;
        margin-bottom: 1rem;
      }
      p {
        margin: 0.35rem 0;
      }
      h3 {
        margin-top: 0;
      }
      .contact-grid {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
        max-width: 420px;
      }
      .wide {
        width: 100%;
      }
      .muted {
        color: rgba(0, 0, 0, 0.55);
        font-size: 0.85rem;
        margin-top: 0.75rem;
      }
    `
  ]
})
export class ConsultantProfileComponent implements OnInit {
  private readonly api = inject(ConsultantApiService);
  private readonly toast = inject(ToastrService);
  private readonly fb = inject(FormBuilder);
  profile: ConsultantProfileVm | null = null;

  contactForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: ['', [Validators.required, Validators.minLength(7)]]
  });

  ngOnInit(): void {
    this.api.profile().subscribe({
      next: (p) => {
        this.profile = p as ConsultantProfileVm;
        this.contactForm.patchValue({
          email: this.profile.email,
          phoneNumber: this.profile.phoneNumber ?? ''
        });
        this.contactForm.markAsPristine();
      },
      error: () => this.toast.error('Failed to load profile')
    });
  }

  saveContact(): void {
    if (this.contactForm.invalid) {
      this.contactForm.markAllAsTouched();
      return;
    }
    const v = this.contactForm.getRawValue();
    this.api.updateProfileContact({ email: v.email.trim(), phoneNumber: v.phoneNumber.trim() }).subscribe({
      next: () => {
        this.toast.success('Contact updated');
        this.contactForm.markAsPristine();
        if (this.profile) {
          this.profile.email = v.email.trim();
          this.profile.phoneNumber = v.phoneNumber.trim();
        }
      },
      error: (e: { error?: { message?: string } }) => this.toast.error(e?.error?.message ?? 'Update failed')
    });
  }
}
