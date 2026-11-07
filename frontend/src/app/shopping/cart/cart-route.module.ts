import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ShoppingModule } from '../shopping.module';
import { CartPageComponent } from './cart-page.component';

const routes: Routes = [{ path: '', component: CartPageComponent }];

@NgModule({
  imports: [ShoppingModule, RouterModule.forChild(routes)],
})
export class CartRouteModule {}