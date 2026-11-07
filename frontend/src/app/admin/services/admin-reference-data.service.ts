import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AdminCategory,
  AdminDeliveryMethod,
  AdminSupplier,
  CategoryWriteRequest,
  DeliveryMethodWriteRequest,
  SupplierWriteRequest,
} from '../models/admin.models';

const apiUrl = 'http://localhost:5000/api/admin';

@Injectable({ providedIn: 'root' })
export class AdminReferenceDataService {
  constructor(private readonly http: HttpClient) {}

  getCategories(): Observable<AdminCategory[]> {
    return this.http.get<AdminCategory[]>(`${apiUrl}/categories`);
  }

  saveCategory(request: CategoryWriteRequest, id?: number): Observable<AdminCategory> {
    return id === undefined
      ? this.http.post<AdminCategory>(`${apiUrl}/categories`, request)
      : this.http.put<AdminCategory>(`${apiUrl}/categories/${id}`, request);
  }

  deleteCategory(id: number): Observable<void> {
    return this.http.delete<void>(`${apiUrl}/categories/${id}`);
  }

  getSuppliers(): Observable<AdminSupplier[]> {
    return this.http.get<AdminSupplier[]>(`${apiUrl}/suppliers`);
  }

  saveSupplier(request: SupplierWriteRequest, id?: number): Observable<AdminSupplier> {
    return id === undefined
      ? this.http.post<AdminSupplier>(`${apiUrl}/suppliers`, request)
      : this.http.put<AdminSupplier>(`${apiUrl}/suppliers/${id}`, request);
  }

  deleteSupplier(id: number): Observable<void> {
    return this.http.delete<void>(`${apiUrl}/suppliers/${id}`);
  }

  getDeliveryMethods(): Observable<AdminDeliveryMethod[]> {
    return this.http.get<AdminDeliveryMethod[]>(`${apiUrl}/delivery-methods`);
  }

  saveDeliveryMethod(
    request: DeliveryMethodWriteRequest,
    id?: number,
  ): Observable<AdminDeliveryMethod> {
    return id === undefined
      ? this.http.post<AdminDeliveryMethod>(`${apiUrl}/delivery-methods`, request)
      : this.http.put<AdminDeliveryMethod>(`${apiUrl}/delivery-methods/${id}`, request);
  }

  deleteDeliveryMethod(id: number): Observable<void> {
    return this.http.delete<void>(`${apiUrl}/delivery-methods/${id}`);
  }
}
