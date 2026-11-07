import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatToolbarModule } from '@angular/material/toolbar';
import { ConfirmDialogComponent } from './components/confirm-dialog/confirm-dialog.component';
import { ProductCardComponent } from './components/product-card/product-card.component';

const materialModules = [
  MatButtonModule,
  MatCheckboxModule,
  MatDialogModule,
  MatFormFieldModule,
  MatIconModule,
  MatInputModule,
  MatMenuModule,
  MatPaginatorModule,
  MatProgressSpinnerModule,
  MatSelectModule,
  MatToolbarModule,
];

@NgModule({
  declarations: [ConfirmDialogComponent, ProductCardComponent],
  imports: [CommonModule, ReactiveFormsModule, RouterModule, ...materialModules],
  exports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    ConfirmDialogComponent,
    ProductCardComponent,
    ...materialModules,
  ],
})
export class SharedModule {}
