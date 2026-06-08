import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Plan, Subscription } from '../models';

@Injectable({ providedIn: 'root' })
export class SubscriptionService {
  private readonly baseUrl = `${environment.apiUrl}`;

  constructor(private http: HttpClient) {}

  getPlans(): Observable<Plan[]> {
    return this.http.get<Plan[]>(`${this.baseUrl}/plans`);
  }

  getMySubscription(): Observable<Subscription> {
    return this.http.get<Subscription>(`${this.baseUrl}/subscriptions/me`);
  }

  createCheckout(priceId: string, successUrl: string, cancelUrl: string): Observable<{ checkoutUrl: string }> {
    return this.http.post<{ checkoutUrl: string }>(`${this.baseUrl}/subscriptions/checkout`, { priceId, successUrl, cancelUrl });
  }

  cancelSubscription(): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/subscriptions/cancel`, null);
  }
}
