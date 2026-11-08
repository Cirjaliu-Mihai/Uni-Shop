import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, DestroyRef, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Order, OrderStatus } from '../../orders/models/order.models';
import { AdminOrderService } from '../services/admin-order.service';

@Component({
  selector: 'app-admin-order-details',
  templateUrl: './admin-order-details.component.html',
  styleUrl: './admin-order-details.component.scss',
  standalone: false,
})
export class AdminOrderDetailsComponent implements OnInit {
  order: Order | null = null;
  loading = true;
  saving = false;
  errorMessage = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly orderService: AdminOrderService,
    private readonly snackBar: MatSnackBar,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly destroyRef: DestroyRef,
  ) {}

  ngOnInit(): void {
    this.orderService
      .getOrder(Number(this.route.snapshot.paramMap.get('id')))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (order) => {
          this.order = order;
          this.loading = false;
          this.changeDetectorRef.markForCheck();
        },
        error: () => {
          this.errorMessage = 'Order not found.';
          this.loading = false;
          this.changeDetectorRef.markForCheck();
        },
      });
  }

  canSetStatus(status: OrderStatus): boolean {
    if (!this.order) return false;
    return (
      (this.order.status === 'processing' && (status === 'shipped' || status === 'cancelled')) ||
      (this.order.status === 'shipped' && (status === 'delivered' || status === 'cancelled'))
    );
  }

  setStatus(status: OrderStatus): void {
    if (!this.order || !this.canSetStatus(status) || this.saving) return;
    this.saving = true;
    this.orderService
      .updateStatus(this.order.id, status)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (order) => {
          this.order = order;
          this.saving = false;
          this.snackBar.open('Order status updated.', 'Dismiss', { duration: 3000 });
          this.changeDetectorRef.markForCheck();
        },
        error: (error: unknown) => {
          this.saving = false;
          this.snackBar.open(this.readError(error), 'Dismiss', { duration: 5000 });
          this.changeDetectorRef.markForCheck();
        },
      });
  }

  private readError(error: unknown): string {
    return error instanceof HttpErrorResponse && typeof error.error?.detail === 'string'
      ? error.error.detail
      : 'Order status could not be updated.';
  }
}
