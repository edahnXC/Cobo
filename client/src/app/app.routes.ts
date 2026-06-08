import { Routes } from '@angular/router';
import { LoginComponent } from './auth/login.component';
import { RegisterComponent } from './auth/register.component';
import { VerifyEmailComponent } from './auth/verify-email.component';
import { ForgotPasswordComponent } from './auth/forgot-password.component';
import { ResetPasswordComponent } from './auth/reset-password.component';
import { WorkspaceListComponent } from './workspace/workspace-list.component';
import { WorkspaceDetailComponent } from './workspace/workspace-detail.component';
import { PlansComponent } from './subscription/plans.component';
import { ComplaintsComponent } from './complaint/complaints.component';
import { AdminComponent } from './admin/admin.component';
import { authGuard, noAuthGuard, adminGuard } from './core/guards';

export const routes: Routes = [
  { path: '', redirectTo: 'workspaces', pathMatch: 'full' },
  { path: 'login', component: LoginComponent, canActivate: [noAuthGuard] },
  { path: 'register', component: RegisterComponent, canActivate: [noAuthGuard] },
  { path: 'verify-email', component: VerifyEmailComponent },
  { path: 'forgot-password', component: ForgotPasswordComponent, canActivate: [noAuthGuard] },
  { path: 'reset-password', component: ResetPasswordComponent, canActivate: [noAuthGuard] },
  { path: 'workspaces', component: WorkspaceListComponent, canActivate: [authGuard] },
  { path: 'workspaces/:wId', component: WorkspaceDetailComponent, canActivate: [authGuard] },
  { path: 'plans', component: PlansComponent },
  { path: 'complaints', component: ComplaintsComponent, canActivate: [authGuard] },
  { path: 'admin', component: AdminComponent, canActivate: [adminGuard] },
  { path: '**', redirectTo: 'workspaces' }
];
