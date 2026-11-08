import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpParams } from '@angular/common/http';
import {
  CreateOrderRequest,
  DeliveryOption,
  Order,
  OrderPage,
  OrderStatus,
} from '../models/order.models';

const apiUrl = 'http://localhost:5000/api';

@Injectable({ providedIn: 'root' })
export class OrderService {
  constructor(private readonly http: HttpClient) {}

  getDeliveryOptions(): Observable<DeliveryOption[]> {
    return this.http.get<DeliveryOption[]>(`${apiUrl}/checkout/delivery-options`);
  }

  createOrder(request: CreateOrderRequest): Observable<Order> {
    return this.http.post<Order>(`${apiUrl}/orders`, request);
  }

  getOrder(id: number): Observable<Order> {
    return this.http.get<Order>(`${apiUrl}/orders/${id}`);
  }

  getOrders(page: number, pageSize: number, status: OrderStatus | 'all'): Observable<OrderPage> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (status !== 'all') params = params.set('status', status);
    return this.http.get<OrderPage>(`${apiUrl}/orders`, { params });
  }

  cancelOrder(id: number): Observable<Order> {
    return this.http.post<Order>(`${apiUrl}/orders/${id}/cancel`, {});
  }
}
