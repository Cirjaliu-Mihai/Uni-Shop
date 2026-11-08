import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CheckoutComponent } from './checkout/checkout.component';
import { OrderConfirmationComponent } from './confirmation/order-confirmation.component';
import { OrderDetailsComponent } from './history/order-details.component';
import { OrderHistoryComponent } from './history/order-history.component';

const routes: Routes = [
  { path: '', component: OrderHistoryComponent },
  { path: 'checkout', component: CheckoutComponent },
  { path: 'confirmation/:id', component: OrderConfirmationComponent },
  { path: ':id', component: OrderDetailsComponent },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class OrdersRoutingModule {}
