import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../core/services/auth.service';
import { BrandBannerComponent } from '../../shared/brand-banner/brand-banner.component';
import { ToastrService } from 'ngx-toastr';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    BrandBannerComponent
  ],
  templateUrl: './reset-password.component.html',
  styleUrl: '../forgot-password/forgot-password.component.scss'
})
export class ResetPasswordComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastrService);

  loading = false;
  done = false;
  missingToken = false;
  private email = '';
  private token = '';

  form = this.fb.nonNullable.group({
    password: ['', [Validators.required, Validators.minLength(8)]],
    confirm: ['', [Validators.required]]
  });

  ngOnInit(): void {
    const q = this.route.snapshot.queryParamMap;
    this.email = q.get('email')?.trim() ?? '';
    this.token = q.get('token')?.trim() ?? '';
    if (!this.email || !this.token) {
      this.missingToken = true;
    }
  }

  submit(): void {
    if (this.missingToken) return;
    const { password, confirm } = this.form.getRawValue();
    if (password !== confirm) {
      this.toast.error('Passwords do not match');
      return;
    }
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading = true;
    this.auth
      .resetPassword(this.email, this.token, password)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: () => {
          this.done = true;
          this.toast.success('Password updated. You can sign in now.');
          void this.router.navigate(['/login']);
        },
        error: (err: { error?: { message?: string } }) => {
          this.toast.error(err?.error?.message ?? 'Reset failed');
        }
      });
  }
}
