import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: `<main><router-outlet /></main>`,
  styles: [
    `
      main {
        min-height: 100vh;
      }
    `
  ]
})
export class AppComponent {}
