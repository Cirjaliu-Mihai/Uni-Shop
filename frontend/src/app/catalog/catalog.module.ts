import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../shared/shared.module';
import { ProductDetailsComponent } from './product-details/product-details.component';
import { ProductListComponent } from './product-list/product-list.component';

const routes: Routes = [
  { path: '', component: ProductListComponent },
  { path: 'products/:id', component: ProductDetailsComponent },
];

@NgModule({
  declarations: [ProductDetailsComponent, ProductListComponent],
  imports: [SharedModule, RouterModule.forChild(routes)],
  exports: [ProductDetailsComponent, ProductListComponent],
})
export class CatalogModule {}
