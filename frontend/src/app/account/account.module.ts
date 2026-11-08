import { NgModule } from '@angular/core';
import { SharedModule } from '../shared/shared.module';
import { AccountRoutingModule } from './account-routing.module';
import { AddressDialogComponent } from './addresses/address-dialog.component';
import { AddressListComponent } from './addresses/address-list.component';
import { LoginComponent } from './login/login.component';
import { ProfileComponent } from './profile/profile.component';
import { RegisterComponent } from './register/register.component';

@NgModule({
  declarations: [
    AddressDialogComponent,
    AddressListComponent,
    LoginComponent,
    ProfileComponent,
    RegisterComponent,
  ],
  imports: [SharedModule, AccountRoutingModule],
  exports: [LoginComponent, ProfileComponent, RegisterComponent],
})
export class AccountModule {}
