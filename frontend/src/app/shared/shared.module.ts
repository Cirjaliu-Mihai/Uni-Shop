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
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
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
  MatListModule,
  MatMenuModule,
  MatPaginatorModule,
  MatProgressSpinnerModule,
  MatSidenavModule,
  MatSelectModule,
  MatSnackBarModule,
  MatTableModule,
  MatTabsModule,
  MatTooltipModule,
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
