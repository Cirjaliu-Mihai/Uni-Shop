import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminPagedResult } from '../models/admin.models';
import { Order, OrderListItem, OrderStatus } from '../../orders/models/order.models';

const apiUrl = 'http://localhost:5000/api/admin/orders';

export interface AdminOrderFilters {
  search: string;
  status: OrderStatus | 'all';
  from: string;
  to: string;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class AdminOrderService {
  constructor(private readonly http: HttpClient) {}

  getOrders(filters: AdminOrderFilters): Observable<AdminPagedResult<OrderListItem>> {
    let params = new HttpParams().set('page', filters.page).set('pageSize', filters.pageSize);
    if (filters.search.trim()) params = params.set('search', filters.search.trim());
    if (filters.status !== 'all') params = params.set('status', filters.status);
    if (filters.from) params = params.set('from', filters.from);
    if (filters.to) params = params.set('to', filters.to);
    return this.http.get<AdminPagedResult<OrderListItem>>(apiUrl, { params });
  }

  getOrder(id: number): Observable<Order> {
    return this.http.get<Order>(`${apiUrl}/${id}`);
  }

  updateStatus(id: number, status: OrderStatus): Observable<Order> {
    return this.http.patch<Order>(`${apiUrl}/${id}/status`, { status });
  }
}
