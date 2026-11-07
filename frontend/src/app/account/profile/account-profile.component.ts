import { Component } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-account-profile',
  templateUrl: './account-profile.component.html',
  styleUrl: './account-profile.component.scss',
  standalone: false,
})
export class AccountProfileComponent {
  readonly currentUser$;

  constructor(private readonly auth: AuthService) {
    this.currentUser$ = auth.currentUser$;
  }
}
