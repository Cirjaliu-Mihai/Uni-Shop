import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../core/auth/auth.service';
import { ProfileService } from '../services/profile.service';
import { Profile } from '../models/account.models';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss',
  standalone: false,
})
export class ProfileComponent implements OnInit {
  readonly form;
  profile: Profile | null = null;
  loading = true;
  saving = false;
  errorMessage = '';

  constructor(
    formBuilder: FormBuilder,
    private readonly profileService: ProfileService,
    private readonly auth: AuthService,
    private readonly snackBar: MatSnackBar,
    private readonly changeDetectorRef: ChangeDetectorRef,
  ) {
    this.form = formBuilder.nonNullable.group({
      fullName: ['', [Validators.required, Validators.maxLength(200)]],
      phone: ['', [Validators.pattern(/^[+0-9()\-\s]{7,40}$/)]],
    });
  }

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.loading = true;
    this.errorMessage = '';
    this.profileService.getProfile().subscribe({
      next: (profile) => {
        this.profile = profile;
        this.form.patchValue({ fullName: profile.fullName, phone: profile.phone ?? '' });
        this.loading = false;
        this.changeDetectorRef.markForCheck();
      },
      error: (error) => {
        this.loading = false;
        this.errorMessage = this.readError(error, 'Could not load your profile.');
        this.changeDetectorRef.markForCheck();
      },
    });
  }

  save(): void {
    if (this.form.invalid || this.saving) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving = true;
    const values = this.form.getRawValue();
    this.profileService
      .updateProfile({
        fullName: values.fullName.trim(),
        phone: values.phone.trim() || null,
      })
      .subscribe({
        next: (profile) => {
          this.profile = profile;
          this.saving = false;
          void this.auth.initializeSession();
          this.snackBar.open('Profile updated.', 'Dismiss', { duration: 3000 });
          this.changeDetectorRef.markForCheck();
        },
        error: (error) => {
          this.saving = false;
          this.errorMessage = this.readError(error, 'Could not update your profile.');
          this.changeDetectorRef.markForCheck();
        },
      });
  }

  private readError(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse && typeof error.error?.detail === 'string') {
      return error.error.detail;
    }
    return fallback;
  }
}
