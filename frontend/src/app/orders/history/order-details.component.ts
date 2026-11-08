import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, DestroyRef, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { Order } from '../models/order.models';
import { OrderService } from '../services/order.service';

@Component({
  selector: 'app-order-details',
  templateUrl: './order-details.component.html',
  styleUrl: './order-details.component.scss',
  standalone: false,
})
export class OrderDetailsComponent implements OnInit {
  order: Order | null = null;
  loading = true;
  errorMessage = '';
  saving = false;
  retryingPayment = false;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly orderService: OrderService,
    private readonly dialog: MatDialog,
    private readonly snackBar: MatSnackBar,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly destroyRef: DestroyRef,
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.orderService
      .getOrder(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (order) => {
          this.order = order;
          this.loading = false;
          this.changeDetectorRef.markForCheck();
        },
        error: () => {
          this.errorMessage = 'This order could not be found.';
          this.loading = false;
          this.changeDetectorRef.markForCheck();
        },
      });
  }

  cancelOrder(): void {
    if (!this.order || this.order.status !== 'processing' || this.saving) return;
    this.dialog
      .open(ConfirmDialogComponent, {
        data: {
          title: 'Cancel order?',
          message: `Order #${this.order.id} will be cancelled.`,
          confirmLabel: 'Cancel order',
        },
      })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((confirmed) => {
        if (!confirmed || !this.order) return;
        this.saving = true;
        this.orderService
          .cancelOrder(this.order.id)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: (order) => {
              this.order = order;
              this.saving = false;
              this.snackBar.open('Order cancelled.', 'Dismiss', { duration: 3000 });
              this.changeDetectorRef.markForCheck();
            },
            error: (error: unknown) => {
              this.saving = false;
              this.snackBar.open(this.readError(error), 'Dismiss', { duration: 5000 });
              this.changeDetectorRef.markForCheck();
            },
          });
      });
  }

  retryPayment(): void {
    if (
      !this.order ||
      this.order.paymentMethod !== 'card' ||
      this.order.paymentStatus !== 'pending' ||
      this.retryingPayment
    )
      return;
    this.retryingPayment = true;
    this.orderService
      .createCheckoutSession(this.order.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (session) => window.location.assign(session.url),
        error: (error: unknown) => {
          this.retryingPayment = false;
          this.snackBar.open(this.readError(error), 'Dismiss', { duration: 5000 });
          this.changeDetectorRef.markForCheck();
        },
      });
  }

  private readError(error: unknown): string {
    return error instanceof HttpErrorResponse && typeof error.error?.detail === 'string'
      ? error.error.detail
      : 'The order could not be cancelled.';
  }
}
