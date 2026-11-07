import { NgModule } from '@angular/core';
import { SharedModule } from '../shared/shared.module';
import { AccountRoutingModule } from './account-routing.module';
import { AccountProfileComponent } from './profile/account-profile.component';
import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';

@NgModule({
  declarations: [AccountProfileComponent, LoginComponent, RegisterComponent],
  imports: [SharedModule, AccountRoutingModule],
  exports: [AccountProfileComponent, LoginComponent, RegisterComponent],
})
export class AccountModule {}
