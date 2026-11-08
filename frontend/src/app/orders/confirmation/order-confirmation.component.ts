import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { switchMap, takeUntil, takeWhile, tap, timer } from 'rxjs';
import { Order } from '../models/order.models';
import { OrderService } from '../services/order.service';

@Component({
  selector: 'app-order-confirmation',
  templateUrl: './order-confirmation.component.html',
  styleUrl: './order-confirmation.component.scss',
  standalone: false,
})
export class OrderConfirmationComponent implements OnInit {
  order: Order | null = null;
  loading = true;
  errorMessage = '';
  paymentWasCancelled = false;
  retryingPayment = false;
  private readonly destroyRef = inject(DestroyRef);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly orderService: OrderService,
    private readonly changeDetectorRef: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isInteger(id) || id < 1) {
      this.loading = false;
      this.errorMessage = 'This order could not be found.';
      return;
    }
    const shouldPoll = this.route.snapshot.queryParamMap.get('payment') === 'success';
    this.paymentWasCancelled = this.route.snapshot.queryParamMap.get('payment') === 'cancelled';
    const orderUpdates = shouldPoll
      ? timer(0, 2000).pipe(
          switchMap(() => this.orderService.getOrder(id)),
          tap((order) => {
            this.order = order;
            this.loading = false;
            this.changeDetectorRef.markForCheck();
          }),
          takeWhile((order) => order.paymentStatus === 'pending', true),
          takeUntil(timer(120_000)),
        )
      : this.orderService.getOrder(id);
    orderUpdates.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (order) => {
        this.order = order;
        this.loading = false;
        this.changeDetectorRef.markForCheck();
      },
      error: (error) => {
        this.loading = false;
        this.errorMessage =
          error instanceof HttpErrorResponse && typeof error.error?.detail === 'string'
            ? error.error.detail
            : 'This order could not be found.';
        this.changeDetectorRef.markForCheck();
      },
    });
  }

  retryPayment(): void {
    if (
      !this.order ||
      this.order.status !== 'processing' ||
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
        error: () => {
          this.retryingPayment = false;
          this.errorMessage = 'Could not start card payment. Try again shortly.';
          this.changeDetectorRef.markForCheck();
        },
      });
  }

  continueShopping(): void {
    void this.router.navigate(['/']);
  }
}
