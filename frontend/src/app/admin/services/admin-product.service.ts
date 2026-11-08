import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AdminPagedResult,
  AdminProduct,
  AdminProductListItem,
  ProductWriteRequest,
} from '../models/admin.models';

const apiUrl = 'http://localhost:5000/api/admin/products';

@Injectable({ providedIn: 'root' })
export class AdminProductService {
  constructor(private readonly http: HttpClient) {}

  getProducts(
    search: string,
    page: number,
    pageSize: number,
  ): Observable<AdminPagedResult<AdminProductListItem>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search.trim()) params = params.set('search', search.trim());
    return this.http.get<AdminPagedResult<AdminProductListItem>>(apiUrl, { params });
  }

  getProduct(id: number): Observable<AdminProduct> {
    return this.http.get<AdminProduct>(`${apiUrl}/${id}`);
  }

  createProduct(request: ProductWriteRequest): Observable<AdminProduct> {
    return this.http.post<AdminProduct>(apiUrl, request);
  }

  updateProduct(id: number, request: ProductWriteRequest): Observable<AdminProduct> {
    return this.http.put<AdminProduct>(`${apiUrl}/${id}`, request);
  }

  deleteProduct(id: number): Observable<void> {
    return this.http.delete<void>(`${apiUrl}/${id}`);
  }

  deactivateProduct(id: number): Observable<void> {
    return this.http.patch<void>(`${apiUrl}/${id}/active`, {});
  }

  updateStock(id: number, stock: number): Observable<void> {
    return this.http.patch<void>(`${apiUrl}/${id}/stock`, { stock });
  }
}
