import { NgModule } from '@angular/core';
import { SharedModule } from '../shared/shared.module';
import { CartPageComponent } from './cart/cart-page.component';
import { WishlistPageComponent } from './wishlist/wishlist-page.component';

@NgModule({
  declarations: [CartPageComponent, WishlistPageComponent],
  imports: [SharedModule],
})
export class ShoppingModule {}
