export interface Problem {
  id: number;
  sourceLanguage: string;
  targetLanguage: string;
  sourceCode: string;
  expectedTargetCode: string;
  explanation?: string;
  hint?: string;
}

export interface Submission {
  userCode: string;
  sourceLanguage: string;
  targetLanguage: string;
  originalSourceCode: string;
}

export interface CheckAnswerResponse {
  message: string;
  isCorrect: boolean;
  note?: string;
}
