import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { adminGuard } from './core/auth/auth.guards';

const routes: Routes = [
  {
    path: 'account',
    loadChildren: () => import('./account/account.module').then((module) => module.AccountModule),
  },
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadChildren: () => import('./admin/admin.module').then((module) => module.AdminModule),
  },
  {
    path: '',
    loadChildren: () => import('./catalog/catalog.module').then((module) => module.CatalogModule),
  },
  { path: '**', redirectTo: '' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes, { scrollPositionRestoration: 'enabled' })],
  exports: [RouterModule],
})
export class AppRoutingModule {}
