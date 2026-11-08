import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CheckoutComponent } from './checkout/checkout.component';
import { OrderConfirmationComponent } from './confirmation/order-confirmation.component';

const routes: Routes = [
  { path: '', component: CheckoutComponent },
  { path: 'confirmation/:id', component: OrderConfirmationComponent },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class OrdersRoutingModule {}
