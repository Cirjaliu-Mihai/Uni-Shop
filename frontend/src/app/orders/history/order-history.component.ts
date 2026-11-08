import { ChangeDetectorRef, Component, DestroyRef, OnInit } from '@angular/core';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { OrderListItem, OrderStatus } from '../models/order.models';
import { OrderService } from '../services/order.service';

@Component({
  selector: 'app-order-history',
  templateUrl: './order-history.component.html',
  styleUrl: './order-history.component.scss',
  standalone: false,
})
export class OrderHistoryComponent implements OnInit {
  readonly statuses: Array<{ value: OrderStatus | 'all'; label: string }> = [
    { value: 'all', label: 'All orders' },
    { value: 'processing', label: 'Processing' },
    { value: 'shipped', label: 'Shipped' },
    { value: 'delivered', label: 'Delivered' },
    { value: 'cancelled', label: 'Cancelled' },
  ];
  readonly displayedColumns = ['order', 'date', 'status', 'payment', 'total'];
  orders: OrderListItem[] = [];
  status: OrderStatus | 'all' = 'all';
  page = 1;
  pageSize = 10;
  totalCount = 0;
  loading = true;
  errorMessage = '';

  constructor(
    private readonly orderService: OrderService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly destroyRef: DestroyRef,
  ) {}

  ngOnInit(): void {
    this.loadOrders();
  }

  loadOrders(): void {
    this.loading = true;
    this.errorMessage = '';
    this.orderService
      .getOrders(this.page, this.pageSize, this.status)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.orders = result.items;
          this.page = result.page;
          this.pageSize = result.pageSize;
          this.totalCount = result.totalCount;
          this.loading = false;
          this.changeDetectorRef.markForCheck();
        },
        error: () => {
          this.loading = false;
          this.errorMessage = 'Your orders could not be loaded.';
          this.changeDetectorRef.markForCheck();
        },
      });
  }

  changeStatus(status: OrderStatus | 'all'): void {
    this.status = status;
    this.page = 1;
    this.loadOrders();
  }

  changePage(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadOrders();
  }
}
