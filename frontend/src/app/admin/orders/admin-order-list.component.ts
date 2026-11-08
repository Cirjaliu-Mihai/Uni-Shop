import { ChangeDetectorRef, Component, DestroyRef, OnInit } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { PageEvent } from '@angular/material/paginator';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { OrderListItem, OrderStatus } from '../../orders/models/order.models';
import { AdminOrderFilters, AdminOrderService } from '../services/admin-order.service';

@Component({
  selector: 'app-admin-order-list',
  templateUrl: './admin-order-list.component.html',
  styleUrl: './admin-order-list.component.scss',
  standalone: false,
})
export class AdminOrderListComponent implements OnInit {
  readonly search = new FormControl('', { nonNullable: true });
  readonly dateRange = new FormGroup({
    start: new FormControl<Date | null>(null),
    end: new FormControl<Date | null>(null),
  });
  readonly statuses: Array<{ value: OrderStatus | 'all'; label: string }> = [
    { value: 'all', label: 'All statuses' },
    { value: 'processing', label: 'Processing' },
    { value: 'shipped', label: 'Shipped' },
    { value: 'delivered', label: 'Delivered' },
    { value: 'cancelled', label: 'Cancelled' },
  ];
  readonly displayedColumns = ['order', 'customer', 'date', 'status', 'payment', 'total'];
  orders: OrderListItem[] = [];
  status: OrderStatus | 'all' = 'all';
  page = 1;
  pageSize = 20;
  totalCount = 0;
  loading = true;
  errorMessage = '';

  constructor(
    private readonly orderService: AdminOrderService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly destroyRef: DestroyRef,
  ) {}

  ngOnInit(): void {
    this.search.valueChanges
      .pipe(debounceTime(250), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.page = 1;
        this.loadOrders();
      });
    this.loadOrders();
  }

  loadOrders(): void {
    this.loading = true;
    this.errorMessage = '';
    const filters: AdminOrderFilters = {
      search: this.search.value,
      status: this.status,
      from: this.dateBound(this.dateRange.controls.start.value, false),
      to: this.dateBound(this.dateRange.controls.end.value, true),
      page: this.page,
      pageSize: this.pageSize,
    };
    this.orderService
      .getOrders(filters)
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
          this.errorMessage = 'Orders could not be loaded.';
          this.changeDetectorRef.markForCheck();
        },
      });
  }

  changeFilters(): void {
    this.page = 1;
    this.loadOrders();
  }

  changeDateRange(): void {
    if (this.dateRange.valid) this.changeFilters();
  }

  clearDateRange(): void {
    this.dateRange.reset();
    this.changeFilters();
  }

  changePage(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadOrders();
  }

  private dateBound(date: Date | null, endOfDay: boolean): string {
    if (!date || Number.isNaN(date.getTime())) return '';
    const bound = new Date(date);
    bound.setHours(endOfDay ? 23 : 0, endOfDay ? 59 : 0, endOfDay ? 59 : 0, endOfDay ? 999 : 0);
    return bound.toISOString();
  }
}
