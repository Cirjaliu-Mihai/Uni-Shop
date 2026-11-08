import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminSalesGroupBy, PopularProductReport, SalesReportPoint } from '../models/admin.models';

const apiUrl = 'http://localhost:5000/api/admin/reports';

@Injectable({ providedIn: 'root' })
export class AdminReportService {
  constructor(private readonly http: HttpClient) {}

  getSales(from: string, to: string, groupBy: AdminSalesGroupBy): Observable<SalesReportPoint[]> {
    const params = new HttpParams().set('from', from).set('to', to).set('groupBy', groupBy);
    return this.http.get<SalesReportPoint[]>(`${apiUrl}/sales`, { params });
  }

  getPopularProducts(from: string, to: string, top = 10): Observable<PopularProductReport[]> {
    const params = new HttpParams().set('from', from).set('to', to).set('top', top);
    return this.http.get<PopularProductReport[]>(`${apiUrl}/popular-products`, { params });
  }
}
