import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminLayoutComponent } from './layout/admin-layout.component';
import { ProductAdminListComponent } from './products/product-admin-list.component';
import { ProductFormComponent } from './products/product-form.component';
import { ReferenceDataAdminComponent } from './references/reference-data-admin.component';
import { AdminOrderDetailsComponent } from './orders/admin-order-details.component';
import { AdminOrderListComponent } from './orders/admin-order-list.component';
import { UserAdminListComponent } from './users/user-admin-list.component';
import { ReportsComponent } from './reports/reports.component';

const routes: Routes = [
  {
    path: '',
    component: AdminLayoutComponent,
    children: [
      { path: '', component: ProductAdminListComponent },
      { path: 'products/new', component: ProductFormComponent },
      { path: 'products/:id/edit', component: ProductFormComponent },
      { path: 'orders', component: AdminOrderListComponent },
      { path: 'orders/:id', component: AdminOrderDetailsComponent },
      { path: 'users', component: UserAdminListComponent },
      { path: 'reports', component: ReportsComponent },
      { path: 'references', component: ReferenceDataAdminComponent },
      { path: '**', redirectTo: '' },
    ],
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class AdminRoutingModule {}
