import { Component, Input } from '@angular/core';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { map } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { WishlistService } from '../../../shopping/services/wishlist.service';

@Component({
  selector: 'app-wishlist-toggle',
  templateUrl: './wishlist-toggle.component.html',
  styleUrl: './wishlist-toggle.component.scss',
  standalone: false,
})
export class WishlistToggleComponent {
  @Input({ required: true }) productId!: number;
  busy = false;

  constructor(
    readonly wishlist: WishlistService,
    private readonly auth: AuthService,
    private readonly router: Router,
    private readonly snackBar: MatSnackBar,
  ) {}

  toggle(): void {
    if (!this.auth.isAuthenticated) {
      void this.router.navigate(['/account/login'], {
        queryParams: { returnUrl: this.router.url },
      });
      return;
    }

    this.busy = true;
    const request = this.wishlist.has(this.productId)
      ? this.wishlist.remove(this.productId).pipe(map(() => undefined))
      : this.wishlist.add(this.productId).pipe(map(() => undefined));
    request.subscribe({
      next: () => {
        this.busy = false;
        this.snackBar.open(
          this.wishlist.has(this.productId) ? 'Added to wishlist.' : 'Removed from wishlist.',
          'Dismiss',
          { duration: 2200 },
        );
      },
      error: () => {
        this.busy = false;
        this.snackBar.open('Could not update the wishlist.', 'Dismiss', { duration: 3500 });
      },
    });
  }
}
