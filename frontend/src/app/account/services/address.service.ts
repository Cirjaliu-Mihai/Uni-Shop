import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Address, AddressWriteRequest } from '../models/account.models';

const apiUrl = 'http://localhost:5000/api/addresses';

@Injectable({ providedIn: 'root' })
export class AddressService {
  constructor(private readonly http: HttpClient) {}

  getAddresses(): Observable<Address[]> {
    return this.http.get<Address[]>(apiUrl);
  }

  createAddress(request: AddressWriteRequest): Observable<Address> {
    return this.http.post<Address>(apiUrl, request);
  }

  updateAddress(id: number, request: AddressWriteRequest): Observable<Address> {
    return this.http.put<Address>(`${apiUrl}/${id}`, request);
  }

  deleteAddress(id: number): Observable<void> {
    return this.http.delete<void>(`${apiUrl}/${id}`);
  }

  setDefault(id: number): Observable<Address> {
    return this.http.put<Address>(`${apiUrl}/${id}/default`, {});
  }
}
