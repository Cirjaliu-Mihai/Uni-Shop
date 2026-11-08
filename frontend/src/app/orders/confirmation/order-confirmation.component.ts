import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
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
    this.orderService.getOrder(id).subscribe({
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

  continueShopping(): void {
    void this.router.navigate(['/']);
  }
}
