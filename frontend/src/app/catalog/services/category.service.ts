import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CategoryListItem } from '../models/catalog.models';

@Injectable({ providedIn: 'root' })
export class CategoryService {
  constructor(private readonly http: HttpClient) {}

  getCategories(): Observable<CategoryListItem[]> {
    return this.http.get<CategoryListItem[]>('http://localhost:5000/api/categories');
  }
}
