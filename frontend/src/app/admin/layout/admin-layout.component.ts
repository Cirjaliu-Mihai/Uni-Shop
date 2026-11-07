import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-admin-layout',
  templateUrl: './admin-layout.component.html',
  styleUrl: './admin-layout.component.scss',
  standalone: false,
})
export class AdminLayoutComponent {
  constructor(
    readonly auth: AuthService,
    private readonly router: Router,
  ) {}

  logout(): void {
    this.auth.logout();
    void this.router.navigate(['/account/login']);
  }
}
