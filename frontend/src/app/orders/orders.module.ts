import { NgModule } from '@angular/core';
import { SharedModule } from '../shared/shared.module';
import { CheckoutComponent } from './checkout/checkout.component';
import { OrderConfirmationComponent } from './confirmation/order-confirmation.component';
import { OrdersRoutingModule } from './orders-routing.module';

@NgModule({
  declarations: [CheckoutComponent, OrderConfirmationComponent],
  imports: [SharedModule, OrdersRoutingModule],
})
export class OrdersModule {}
