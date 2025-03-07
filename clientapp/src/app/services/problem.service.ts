import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { Problem, Submission, CheckAnswerResponse } from '../models/problem';
import { tap, catchError } from 'rxjs/operators';

@Injectable({
  providedIn: 'root',
})
export class ProblemService {
  private apiUrl = '/api/problems';
  private httpOptions = {
    headers: new HttpHeaders({
      'Content-Type': 'application/json',
    }),
  };

  constructor(private http: HttpClient) {}

  getProblem(sourceLang: string, targetLang: string): Observable<Problem> {
    const url = `${this.apiUrl}?sourceLang=${encodeURIComponent(
      sourceLang
    )}&targetLang=${encodeURIComponent(targetLang)}`;

    console.log('Fetching problem from:', url);

    return this.http.get<Problem>(url).pipe(
      tap(
        (response) => console.log('Problem API response:', response),
        (error) => {
          console.error('Problem API error details:', {
            status: error.status,
            statusText: error.statusText,
            url: error.url,
            message: error.message,
            error: error.error,
          });

          if (error.status === 0) {
            console.error(
              'Network error: Could not connect to server. Check if the API is running at:',
              this.apiUrl
            );
          } else if (error.status === 404) {
            console.error(
              'Problem not found for the selected languages. Available problems might be limited.'
            );
          } else if (error.status >= 500) {
            console.error(
              'Server error: The API encountered an internal error.'
            );
          }
        }
      )
    );
  }

  checkAnswer(submission: Submission): Observable<CheckAnswerResponse> {
    // Validate the submission object before sending
    if (!submission.userCode) {
      console.error('Missing userCode in submission');
      return throwError(() => new Error('User code is required'));
    }

    if (!submission.sourceLanguage || !submission.targetLanguage) {
      console.error('Missing language information in submission');
      return throwError(
        () => new Error('Source and target languages are required')
      );
    }

    if (!submission.originalSourceCode) {
      console.error('Missing originalSourceCode in submission');
      return throwError(() => new Error('Original source code is required'));
    }

    console.log('Sending submission to API:', {
      userCode: submission.userCode?.substring(0, 20) + '...',
      sourceLanguage: submission.sourceLanguage,
      targetLanguage: submission.targetLanguage,
      originalSourceCode:
        submission.originalSourceCode?.substring(0, 20) + '...',
    });

    return this.http
      .post<CheckAnswerResponse>(
        `${this.apiUrl}/check`,
        submission,
        this.httpOptions
      )
      .pipe(
        tap(
          (response) => console.log('Check answer response:', response),
          (error) => {
            console.error('Check answer error:', error);
            if (error.status === 400) {
              console.error(
                'Bad request. The server rejected the submission:',
                error.error
              );
            }
          }
        ),
        catchError((error) => {
          // Add specific error handling here
          if (error.status === 400) {
            return throwError(
              () =>
                new Error(
                  `Bad request: ${
                    error.error?.message || 'The server rejected the submission'
                  }`
                )
            );
          }
          return throwError(
            () => new Error(`Error ${error.status}: ${error.message}`)
          );
        })
      );
  }

  checkApiHealth(): Observable<any> {
    const url = `${this.apiUrl}/health`;
    console.log('Checking API health at:', url);

    return this.http.get<{ status: string; message: string }>(url).pipe(
      tap(
        (response) => console.log('API health check response:', response),
        (error) => console.error('API health check failed:', error)
      )
    );
  }

  getRandomProblem(
    sourceLang: string,
    targetLang: string
  ): Observable<Problem> {
    const encodedSourceLang = encodeURIComponent(sourceLang);
    const encodedTargetLang = encodeURIComponent(targetLang);

    console.log(
      `Fetching random problem from: ${this.apiUrl}/random?sourceLang=${encodedSourceLang}&targetLang=${encodedTargetLang}`
    );

    return this.http
      .get<Problem>(
        `${this.apiUrl}/random?sourceLang=${encodedSourceLang}&targetLang=${encodedTargetLang}`
      )
      .pipe(
        tap(
          (response) => console.log('Random problem API response:', response),
          (error) => {
            console.error('Random problem API error:', error);
            if (error.status === 0) {
              console.error('Network error: Could not connect to server');
            } else if (error.status === 404) {
              console.error('Endpoint not found. Check the URL path');
            } else if (error.status === 400) {
              console.error('Bad request. Check the parameters');
            } else if (error.status >= 500) {
              console.error('Server error:', error.error);
            }
          }
        )
      );
  }

  getHint(
    sourceCode: string,
    targetLang: string
  ): Observable<{ hint: string }> {
    const encodedSource = encodeURIComponent(sourceCode);
    const encodedTargetLang = encodeURIComponent(targetLang);

    console.log(
      `Fetching hint from: ${this.apiUrl}/hint?targetLang=${encodedTargetLang}`
    );

    return this.http
      .get<{ hint: string }>(
        `${this.apiUrl}/hint?sourceCode=${encodedSource}&targetLang=${encodedTargetLang}`
      )
      .pipe(
        tap(
          (response) => console.log('Hint API response:', response),
          (error) => {
            console.error('Hint API error:', error);
            if (error.status === 0) {
              console.error('Network error: Could not connect to server');
            } else if (error.status === 404) {
              console.error('Endpoint not found. Check the URL path');
            } else if (error.status === 400) {
              console.error('Bad request. Check the parameters');
            } else if (error.status >= 500) {
              console.error('Server error:', error.error);
            }
          }
        )
      );
  }

  getAnswerExplanation(
    sourceCode: string,
    targetCode: string,
    sourceLanguage: string,
    targetLanguage: string
  ): Observable<{ explanation: string }> {
    const encodedSourceCode = encodeURIComponent(sourceCode);
    const encodedTargetCode = encodeURIComponent(targetCode);
    const encodedSourceLang = encodeURIComponent(sourceLanguage);
    const encodedTargetLang = encodeURIComponent(targetLanguage);

    console.log(
      `Fetching answer explanation for ${sourceLanguage} to ${targetLanguage} translation`
    );

    return this.http
      .get<{ explanation: string }>(
        `${this.apiUrl}/explain?sourceCode=${encodedSourceCode}&targetCode=${encodedTargetCode}&sourceLang=${encodedSourceLang}&targetLang=${encodedTargetLang}`
      )
      .pipe(
        tap(
          (response) => console.log('Answer explanation response:', response),
          (error) => {
            console.error('Answer explanation error:', error);
            if (error.status === 404) {
              console.error(
                'Endpoint not found. The explain API may not be implemented.'
              );
            }
          }
        ),
        catchError((error) => {
          return throwError(
            () => new Error(`Error getting explanation: ${error.message}`)
          );
        })
      );
  }
}
