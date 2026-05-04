import { DatePipe } from '@angular/common';
import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { distinctUntilChanged, Subscription } from 'rxjs';
import { ConsultantApiService } from '../../core/services/consultant-api.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-consultant-daily',
  standalone: true,
  imports: [DatePipe, ReactiveFormsModule, MatCardModule, MatTableModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  templateUrl: './consultant-daily.component.html',
  styleUrl: './consultant-shared.scss'
})
export class ConsultantDailyComponent implements OnInit, OnDestroy {
  private readonly api = inject(ConsultantApiService);
  private readonly toast = inject(ToastrService);
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private sub?: Subscription;

  cols = ['date', 'jobs', 'reach', 'resp', 'subs', 'ints'];
  rows: Record<string, unknown>[] = [];
  /** When true, jobs / reach / submissions / interview counts are driven from system data (read-only). */
  consultantDerivedLock = false;

  form = this.fb.nonNullable.group({
    activityDate: [new Date().toISOString().slice(0, 10), Validators.required],
    jobsAppliedCount: [0],
    vendorReachedOutCount: [0],
    vendorResponseCount: [0],
    submissionsCount: [0],
    interviewCallsCount: [0],
    notes: ['']
  });

  ngOnInit(): void {
    this.consultantDerivedLock =
      this.auth.hasRole('Consultant') && !this.auth.hasRole('Admin') && !this.auth.hasRole('Management');
    this.setDerivedControlsDisabled(this.consultantDerivedLock);
    this.reload();
    if (this.consultantDerivedLock) {
      this.refreshSuggestions(this.form.controls.activityDate.value);
      this.sub = this.form.controls.activityDate.valueChanges.pipe(distinctUntilChanged()).subscribe((d) => {
        this.refreshSuggestions(d);
      });
    }
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  private setDerivedControlsDisabled(disabled: boolean): void {
    const keys = ['jobsAppliedCount', 'vendorReachedOutCount', 'submissionsCount', 'interviewCallsCount'] as const;
    for (const k of keys) {
      const c = this.form.get(k);
      if (!c) continue;
      if (disabled) c.disable({ emitEvent: false });
      else c.enable({ emitEvent: false });
    }
  }

  private refreshSuggestions(activityDate: string): void {
    const iso = new Date(`${activityDate}T12:00:00`).toISOString();
    this.api.dailyActivitySuggestions(iso).subscribe({
      next: (s) => {
        this.form.patchValue(
          {
            jobsAppliedCount: s.jobsAppliedCount,
            vendorReachedOutCount: s.vendorReachedOutCount,
            submissionsCount: s.submissionsCount,
            interviewCallsCount: s.interviewCallsCount
          },
          { emitEvent: false }
        );
      },
      error: () => {}
    });
  }

  reload(): void {
    this.api.dailyActivities().subscribe({
      next: (r) => (this.rows = r as Record<string, unknown>[]),
      error: () => this.toast.error('Failed to load')
    });
  }

  save(): void {
    const v = this.form.getRawValue();
    this.api
      .saveDaily({
        ...v,
        activityDate: new Date(v.activityDate).toISOString()
      } as Record<string, unknown>)
      .subscribe({
        next: () => {
          this.toast.success('Saved');
          this.reload();
          if (this.consultantDerivedLock) this.refreshSuggestions(v.activityDate);
        },
        error: (e: { error?: { message?: string } }) => this.toast.error(e?.error?.message ?? 'Error')
      });
  }
}
