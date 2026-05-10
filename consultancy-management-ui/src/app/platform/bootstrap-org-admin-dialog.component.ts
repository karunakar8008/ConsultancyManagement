import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

export interface BootstrapOrgAdminDialogData {
  organizationId: number;
  organizationName: string;
  slug: string;
}

export interface BootstrapOrgAdminDialogResult {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

@Component({
  selector: 'app-bootstrap-org-admin-dialog',
  standalone: true,
  imports: [MatDialogModule, ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  template: `
    <h2 mat-dialog-title>Assign tenant admin</h2>
    <mat-dialog-content>
      <p class="hint">
        Creates the first <strong>Admin</strong> for <strong>{{ data.organizationName }}</strong>. They sign in with the
        organization code <strong>{{ data.slug }}</strong>.
      </p>
      <form [formGroup]="form" class="fields">
        <mat-form-field appearance="outline" class="full">
          <mat-label>Email</mat-label>
          <input matInput type="email" formControlName="email" autocomplete="off" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="full">
          <mat-label>Password</mat-label>
          <input matInput type="password" formControlName="password" autocomplete="new-password" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="half">
          <mat-label>First name</mat-label>
          <input matInput formControlName="firstName" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="half">
          <mat-label>Last name</mat-label>
          <input matInput formControlName="lastName" />
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close type="button">Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" type="button" (click)="submit()">
        Create admin
      </button>
    </mat-dialog-actions>
  `,
  styles: [
    `
      .hint {
        margin: 0 0 1rem;
        color: #5f6368;
        font-size: 0.9rem;
        line-height: 1.4;
      }
      .fields {
        display: flex;
        flex-wrap: wrap;
        gap: 0 1rem;
        min-width: 320px;
      }
      .full {
        flex: 1 1 100%;
        width: 100%;
      }
      .half {
        flex: 1 1 calc(50% - 0.5rem);
        min-width: 140px;
      }
    `
  ]
})
export class BootstrapOrgAdminDialogComponent {
  readonly data = inject<BootstrapOrgAdminDialogData>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);
  private readonly ref = inject(MatDialogRef<BootstrapOrgAdminDialogComponent, BootstrapOrgAdminDialogResult>);

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    firstName: ['', Validators.required],
    lastName: ['', Validators.required]
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.ref.close(this.form.getRawValue());
  }
}
