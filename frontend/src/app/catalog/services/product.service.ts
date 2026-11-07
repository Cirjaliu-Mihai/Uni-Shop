import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  PagedResult,
  ProductDetails,
  ProductListItem,
  ProductQuery,
} from '../models/catalog.models';

const apiUrl = 'http://localhost:5000/api/products';

@Injectable({ providedIn: 'root' })
export class ProductService {
  constructor(private readonly http: HttpClient) {}

  getProducts(query: ProductQuery): Observable<PagedResult<ProductListItem>> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize)
      .set('sort', query.sort);

    if (query.search.trim()) params = params.set('search', query.search.trim());
    if (query.categoryId !== null) params = params.set('categoryId', query.categoryId);
    if (query.minPrice !== null) params = params.set('minPrice', query.minPrice);
    if (query.maxPrice !== null) params = params.set('maxPrice', query.maxPrice);
    if (query.inStock !== null) params = params.set('inStock', query.inStock);

    return this.http.get<PagedResult<ProductListItem>>(apiUrl, { params });
  }

  getProduct(id: number): Observable<ProductDetails> {
    return this.http.get<ProductDetails>(`${apiUrl}/${id}`);
  }
}
