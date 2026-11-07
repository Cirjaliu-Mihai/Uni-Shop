import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ShoppingModule } from '../shopping.module';
import { WishlistPageComponent } from './wishlist-page.component';

const routes: Routes = [{ path: '', component: WishlistPageComponent }];

@NgModule({
  imports: [ShoppingModule, RouterModule.forChild(routes)],
})
export class WishlistRouteModule {}