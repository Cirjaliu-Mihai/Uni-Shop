import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Profile, UpdateProfileRequest } from '../models/account.models';

const apiUrl = 'http://localhost:5000/api/profile';

@Injectable({ providedIn: 'root' })
export class ProfileService {
  constructor(private readonly http: HttpClient) {}

  getProfile(): Observable<Profile> {
    return this.http.get<Profile>(apiUrl);
  }

  updateProfile(request: UpdateProfileRequest): Observable<Profile> {
    return this.http.put<Profile>(apiUrl, request);
  }
}
