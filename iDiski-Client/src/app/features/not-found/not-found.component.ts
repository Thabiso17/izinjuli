import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="not-found-container">
      <div class="not-found-content">
        <h1>404</h1>
        <h2>Page Not Found</h2>
        <p>Sorry, the page you're looking for doesn't exist or has been moved.</p>

        <div class="suggestions">
          <p>You might want to try:</p>
          <ul>
            <li><a routerLink="/">Go to Home</a></li>
            <li><a routerLink="/teams">View Teams</a></li>
            <li><a routerLink="/players">View Players</a></li>
            <li><a routerLink="/matches">View Matches</a></li>
            <li><a routerLink="/standings">View Standings</a></li>
          </ul>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .not-found-container {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 60vh;
      padding: 2rem;
    }

    .not-found-content {
      text-align: center;
      max-width: 600px;
    }

    h1 {
      font-size: 8rem;
      font-weight: bold;
      margin: 0;
      color: #dc3545;
      opacity: 0.8;
    }

    h2 {
      font-size: 2.5rem;
      margin: 1rem 0;
      color: #333;
    }

    p {
      font-size: 1.1rem;
      color: #666;
      margin: 1.5rem 0;
    }

    .suggestions {
      margin-top: 3rem;
      padding-top: 2rem;
      border-top: 1px solid #ddd;
    }

    .suggestions p {
      font-size: 1rem;
      color: #555;
      margin-bottom: 1rem;
    }

    ul {
      list-style: none;
      padding: 0;
      display: flex;
      flex-wrap: wrap;
      justify-content: center;
      gap: 1rem;
    }

    li {
      margin: 0;
    }

    a {
      display: inline-block;
      padding: 0.75rem 1.5rem;
      background-color: #007bff;
      color: white;
      text-decoration: none;
      border-radius: 4px;
      transition: background-color 0.3s;
    }

    a:hover {
      background-color: #0056b3;
    }
  `]
})
export class NotFoundComponent {}
