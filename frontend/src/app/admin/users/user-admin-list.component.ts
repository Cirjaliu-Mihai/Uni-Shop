import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, DestroyRef, OnInit } from '@angular/core';
import { FormControl } from '@angular/forms';
import { PageEvent } from '@angular/material/paginator';
import { MatSnackBar } from '@angular/material/snack-bar';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { AdminUser } from '../models/admin.models';
import { AdminUserService } from '../services/admin-user.service';

@Component({
  selector: 'app-user-admin-list',
  templateUrl: './user-admin-list.component.html',
  styleUrl: './user-admin-list.component.scss',
  standalone: false,
})
export class UserAdminListComponent implements OnInit {
  readonly search = new FormControl('', { nonNullable: true });
  readonly displayedColumns = ['user', 'role', 'orders', 'created', 'access', 'actions'];
  users: AdminUser[] = [];
  page = 1;
  pageSize = 20;
  totalCount = 0;
  loading = true;
  errorMessage = '';
  savingId: string | null = null;

  constructor(
    private readonly userService: AdminUserService,
    private readonly snackBar: MatSnackBar,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly destroyRef: DestroyRef,
  ) {}

  ngOnInit(): void {
    this.search.valueChanges
      .pipe(debounceTime(250), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.page = 1;
        this.loadUsers();
      });
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading = true;
    this.errorMessage = '';
    this.userService
      .getUsers(this.search.value, this.page, this.pageSize)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.users = result.items;
          this.page = result.page;
          this.pageSize = result.pageSize;
          this.totalCount = result.totalCount;
          this.loading = false;
          this.changeDetectorRef.markForCheck();
        },
        error: (error: unknown) => {
          this.loading = false;
          this.errorMessage = this.readError(error, 'Users could not be loaded.');
          this.changeDetectorRef.markForCheck();
        },
      });
  }

  changePage(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadUsers();
  }

  primaryRole(user: AdminUser): 'Admin' | 'Client' {
    return user.roles.includes('Admin') ? 'Admin' : 'Client';
  }

  setRole(user: AdminUser, role: 'Admin' | 'Client'): void {
    if (this.savingId || role === this.primaryRole(user)) return;
    this.savingId = user.id;
    this.userService
      .setRole(user.id, role)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updated) => {
          this.replaceUser(updated);
          this.savingId = null;
          this.snackBar.open('User role updated.', 'Dismiss', { duration: 3000 });
        },
        error: (error: unknown) => this.handleError(user.id, error, 'Could not update the role.'),
      });
  }

  setLocked(user: AdminUser, locked: boolean): void {
    if (this.savingId) return;
    this.savingId = user.id;
    const request = locked ? this.userService.lock(user.id) : this.userService.unlock(user.id);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (updated) => {
        this.replaceUser(updated);
        this.savingId = null;
        this.snackBar.open(locked ? 'User locked.' : 'User unlocked.', 'Dismiss', {
          duration: 3000,
        });
      },
      error: (error: unknown) =>
        this.handleError(
          user.id,
          error,
          locked ? 'Could not lock the user.' : 'Could not unlock the user.',
        ),
    });
  }

  private replaceUser(user: AdminUser): void {
    this.users = this.users.map((item) => (item.id === user.id ? user : item));
    this.changeDetectorRef.markForCheck();
  }

  private handleError(id: string, error: unknown, fallback: string): void {
    this.savingId = null;
    this.snackBar.open(this.readError(error, fallback), 'Dismiss', { duration: 5000 });
    this.changeDetectorRef.markForCheck();
  }

  private readError(error: unknown, fallback: string): string {
    return error instanceof HttpErrorResponse && typeof error.error?.detail === 'string'
      ? error.error.detail
      : fallback;
  }
}
