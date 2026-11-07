import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { adminGuard } from './core/auth/auth.guards';
import { AdminHomeComponent } from './core/components/admin-home/admin-home.component';
import { HomeComponent } from './core/components/home/home.component';

const routes: Routes = [
  { path: '', component: HomeComponent, pathMatch: 'full' },
  {
    path: 'account',
    loadChildren: () => import('./account/account.module').then((module) => module.AccountModule),
  },
  { path: 'admin', component: AdminHomeComponent, canActivate: [adminGuard] },
  { path: '**', redirectTo: '' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes, { scrollPositionRestoration: 'enabled' })],
  exports: [RouterModule],
})
export class AppRoutingModule {}
