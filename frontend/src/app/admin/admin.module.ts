import { NgModule } from '@angular/core';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { SharedModule } from '../shared/shared.module';
import { AdminRoutingModule } from './admin-routing.module';
import { AdminLayoutComponent } from './layout/admin-layout.component';
import { ProductAdminListComponent } from './products/product-admin-list.component';
import { ProductFormComponent } from './products/product-form.component';
import { ReferenceDataAdminComponent } from './references/reference-data-admin.component';
import { ReferenceDataDialogComponent } from './references/reference-data-dialog.component';
import { AdminOrderDetailsComponent } from './orders/admin-order-details.component';
import { AdminOrderListComponent } from './orders/admin-order-list.component';

@NgModule({
  declarations: [
    AdminLayoutComponent,
    ProductAdminListComponent,
    ProductFormComponent,
    ReferenceDataAdminComponent,
    ReferenceDataDialogComponent,
    AdminOrderListComponent,
    AdminOrderDetailsComponent,
  ],
  imports: [SharedModule, MatDatepickerModule, MatNativeDateModule, AdminRoutingModule],
})
export class AdminModule {}
