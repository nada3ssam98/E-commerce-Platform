import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

/**
 * M0 placeholder page of the store administration (client-only).
 * Staff sign-in and the admin shell arrive in M2.
 */
@Component({
  imports: [RouterOutlet],
  selector: 'app-root',
  template: `
    <main>
      <h1 i18n="Admin placeholder heading@@admin.placeholder.heading">Store administration</h1>
      <p i18n="Admin placeholder text@@admin.placeholder.text">This page is a placeholder for the administration.</p>
    </main>
    <router-outlet />
  `,
})
export class App {}
