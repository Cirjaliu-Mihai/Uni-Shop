import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminPagedResult, AdminUser } from '../models/admin.models';

const apiUrl = 'http://localhost:5000/api/admin/users';

@Injectable({ providedIn: 'root' })
export class AdminUserService {
  constructor(private readonly http: HttpClient) {}

  getUsers(
    search: string,
    page: number,
    pageSize: number,
  ): Observable<AdminPagedResult<AdminUser>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search.trim()) params = params.set('search', search.trim());
    return this.http.get<AdminPagedResult<AdminUser>>(apiUrl, { params });
  }

  lock(id: string): Observable<AdminUser> {
    return this.http.patch<AdminUser>(`${apiUrl}/${id}/lock`, {});
  }

  unlock(id: string): Observable<AdminUser> {
    return this.http.patch<AdminUser>(`${apiUrl}/${id}/unlock`, {});
  }

  setRole(id: string, role: 'Admin' | 'Client'): Observable<AdminUser> {
    return this.http.patch<AdminUser>(`${apiUrl}/${id}/role`, { role });
  }
}
