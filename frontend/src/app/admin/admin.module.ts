import { NgModule } from '@angular/core';
import { SharedModule } from '../shared/shared.module';
import { AdminRoutingModule } from './admin-routing.module';
import { AdminLayoutComponent } from './layout/admin-layout.component';
import { ProductAdminListComponent } from './products/product-admin-list.component';
import { ProductFormComponent } from './products/product-form.component';
import { ReferenceDataAdminComponent } from './references/reference-data-admin.component';
import { ReferenceDataDialogComponent } from './references/reference-data-dialog.component';

@NgModule({
  declarations: [
    AdminLayoutComponent,
    ProductAdminListComponent,
    ProductFormComponent,
    ReferenceDataAdminComponent,
    ReferenceDataDialogComponent,
  ],
  imports: [SharedModule, AdminRoutingModule],
})
export class AdminModule {}
