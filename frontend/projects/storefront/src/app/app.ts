import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

/**
 * M0 placeholder page of the public storefront (server-rendered).
 * Store resolution, status pages, and the shell arrive in M1.
 */
@Component({
  imports: [RouterOutlet],
  selector: 'app-root',
  template: `
    <main>
      <h1 i18n="Storefront placeholder heading@@storefront.placeholder.heading">Welcome</h1>
      <p i18n="Storefront placeholder text@@storefront.placeholder.text">This page is a placeholder for the storefront.</p>
    </main>
    <router-outlet />
  `,
})
export class App {}
