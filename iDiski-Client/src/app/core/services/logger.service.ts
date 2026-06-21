import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class LoggerService {
  private logs: string[] = [];

  log(message: string, data?: any) {
    const timestamp = new Date().toISOString();
    const logEntry = `[${timestamp}] ${message}`;
    
    if (data) {
      this.logs.push(logEntry + ' ' + JSON.stringify(data));
    } else {
      this.logs.push(logEntry);
    }
    
    console.log(logEntry, data);
  }

  error(message: string, error?: any) {
    const timestamp = new Date().toISOString();
    const logEntry = `[${timestamp}] ❌ ERROR: ${message}`;
    
    if (error) {
      this.logs.push(logEntry + ' ' + JSON.stringify(error));
    } else {
      this.logs.push(logEntry);
    }
    
    console.error(logEntry, error);
  }

  getLogs(): string {
    return this.logs.join('\n');
  }

  downloadLogs() {
    const element = document.createElement('a');
    element.setAttribute('href', 'data:text/plain;charset=utf-8,' + encodeURIComponent(this.getLogs()));
    element.setAttribute('download', `idiski-logs-${new Date().toISOString()}.txt`);
    element.style.display = 'none';
    document.body.appendChild(element);
    element.click();
    document.body.removeChild(element);
  }

  clearLogs() {
    this.logs = [];
  }
}
