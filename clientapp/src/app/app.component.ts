import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ProblemService } from './services/problem.service';
import { Problem, Submission } from './models/problem';
import { CodeEditorComponent } from './components/code-editor/code-editor.component';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],
  standalone: true,
  imports: [CommonModule, FormsModule, CodeEditorComponent],
})
export class AppComponent implements OnInit {
  languages = ['TypeScript', 'C#'];
  sourceLang = 'TypeScript';
  targetLang = 'C#';
  problem: Problem | null = null;
  userCode = '';
  feedback = '';
  showExplanation = false;
  hint = '';
  showHint = false;
  showSolution = false;
  solutionExplanation = '';

  constructor(private problemService: ProblemService) {}

  ngOnInit() {
    this.checkApiHealth();
  }

  checkApiHealth() {
    this.problemService.checkApiHealth().subscribe({
      next: (response) => {
        console.log('API status:', response);
        this.loadProblem();
      },
      error: (error) => {
        console.error('API health check failed:', error);
        this.feedback =
          'Could not connect to the server. Please check if the API is running.';
      },
    });
  }

  loadProblem() {
    this.feedback = 'Loading problem...';

    this.problemService.getProblem(this.sourceLang, this.targetLang).subscribe({
      next: (problem) => {
        this.problem = problem;
        this.userCode = '';
        this.feedback = '';
      },
      error: (error) => {
        console.error('Failed to load problem:', error);

        if (error.status === 0) {
          this.feedback =
            'Could not connect to the server. Please check if the API is running.';
        } else if (error.status === 404) {
          this.feedback = `No problem found for translating from ${this.sourceLang} to ${this.targetLang}.`;
        } else {
          this.feedback = `Error loading problem: ${error.status} ${error.statusText}`;
        }

        // Clear problem if there was an error
        this.problem = null;
      },
    });
  }

  getQuestion() {
    this.feedback = 'Loading question...';
    this.showExplanation = false;
    this.showHint = false;
    this.hint = '';

    this.problemService
      .getRandomProblem(this.sourceLang, this.targetLang)
      .subscribe({
        next: (problem) => {
          this.problem = problem;
          this.userCode = '';
          this.feedback = '';
        },
        error: (error) => {
          console.error('Failed to load problem:', error);

          if (error.status === 0) {
            this.feedback =
              'Could not connect to the server. Please check if the API is running.';
          } else if (error.status === 404) {
            this.feedback = `No problem found for translating from ${this.sourceLang} to ${this.targetLang}.`;
          } else {
            this.feedback = `Error loading problem: ${error.status} ${error.statusText}`;
          }

          // Clear problem if there was an error
          this.problem = null;
        },
      });
  }

  onLanguageChange() {
    if (this.problem) {
      this.getQuestion();
    }
  }

  submitAnswer() {
    if (!this.problem) {
      this.feedback = 'No problem loaded. Please get a question first.';
      return;
    }

    this.feedback = 'Submitting...';

    const submission: Submission = {
      userCode: this.userCode,
      sourceLanguage: this.problem.sourceLanguage,
      targetLanguage: this.problem.targetLanguage,
      originalSourceCode: this.problem.sourceCode,
    };

    this.problemService.checkAnswer(submission).subscribe({
      next: (response) => {
        this.feedback = response.message;
        if (response.isCorrect) {
          this.showExplanation = true;
        }
        if (response.note) {
          this.feedback += `\n\nNote: ${response.note}`;
        }
      },
      error: (error) => {
        console.error('Error submitting answer:', error);
        this.feedback =
          'Error checking answer: ' +
          (error.error?.message || error.message || 'Unknown error');
      },
    });
  }

  getHint() {
    if (!this.problem) {
      this.feedback = 'No problem loaded. Please get a question first.';
      return;
    }

    this.problemService
      .getHint(this.problem.sourceCode, this.problem.targetLanguage)
      .subscribe({
        next: (response) => {
          this.hint = response.hint;
          this.showHint = true;
        },
        error: (error) => {
          console.error('Error getting hint:', error);
          this.hint =
            'Error getting hint: ' +
            (error.error?.message || error.message || 'Unknown error');
          this.showHint = true;
        },
      });
  }

  getAnswer() {
    if (!this.problem) {
      this.feedback = 'No problem loaded. Please get a question first.';
      return;
    }

    // Fill in the answer
    this.userCode = this.problem.expectedTargetCode;
    this.showSolution = true;

    // Generate explanation if not already available
    if (this.problem.explanation) {
      this.solutionExplanation = this.problem.explanation;
    } else {
      this.solutionExplanation =
        'This is the expected translation following best practices for the target language.';

      // Try to get a detailed explanation from Grok
      this.problemService
        .getAnswerExplanation(
          this.problem.sourceCode,
          this.problem.expectedTargetCode,
          this.problem.sourceLanguage,
          this.problem.targetLanguage
        )
        .subscribe({
          next: (response: { explanation: string }) => {
            if (response && response.explanation) {
              this.solutionExplanation = response.explanation;
            }
          },
          error: (error: any) => {
            console.error('Error getting solution explanation:', error);
          },
        });
    }
  }
}
