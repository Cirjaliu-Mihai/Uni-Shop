import { NgModule } from '@angular/core';
import { SharedModule } from '../shared/shared.module';
import { CheckoutComponent } from './checkout/checkout.component';
import { OrderConfirmationComponent } from './confirmation/order-confirmation.component';
import { OrderDetailsComponent } from './history/order-details.component';
import { OrderHistoryComponent } from './history/order-history.component';
import { OrdersRoutingModule } from './orders-routing.module';

@NgModule({
  declarations: [
    CheckoutComponent,
    OrderConfirmationComponent,
    OrderHistoryComponent,
    OrderDetailsComponent,
  ],
  imports: [SharedModule, OrdersRoutingModule],
})
export class OrdersModule {}
