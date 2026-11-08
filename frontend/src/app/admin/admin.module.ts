import { NgModule } from '@angular/core';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { BaseChartDirective, provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { SharedModule } from '../shared/shared.module';
import { AdminRoutingModule } from './admin-routing.module';
import { AdminLayoutComponent } from './layout/admin-layout.component';
import { ProductAdminListComponent } from './products/product-admin-list.component';
import { ProductFormComponent } from './products/product-form.component';
import { ReferenceDataAdminComponent } from './references/reference-data-admin.component';
import { ReferenceDataDialogComponent } from './references/reference-data-dialog.component';
import { AdminOrderDetailsComponent } from './orders/admin-order-details.component';
import { AdminOrderListComponent } from './orders/admin-order-list.component';
import { UserAdminListComponent } from './users/user-admin-list.component';
import { ReportsComponent } from './reports/reports.component';

@NgModule({
  declarations: [
    AdminLayoutComponent,
    ProductAdminListComponent,
    ProductFormComponent,
    ReferenceDataAdminComponent,
    ReferenceDataDialogComponent,
    AdminOrderListComponent,
    AdminOrderDetailsComponent,
    UserAdminListComponent,
    ReportsComponent,
  ],
  imports: [
    SharedModule,
    MatDatepickerModule,
    MatNativeDateModule,
    BaseChartDirective,
    AdminRoutingModule,
  ],
  providers: [provideCharts(withDefaultRegisterables())],
})
export class AdminModule {}
