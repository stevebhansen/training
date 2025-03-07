import {
  Component,
  ElementRef,
  Input,
  OnDestroy,
  OnInit,
  ViewChild,
  forwardRef,
} from '@angular/core';
import {
  ControlValueAccessor,
  FormsModule,
  NG_VALUE_ACCESSOR,
} from '@angular/forms';
import { CommonModule } from '@angular/common';
import { basicSetup } from 'codemirror';
import { EditorState } from '@codemirror/state';
import { EditorView } from '@codemirror/view';
import { javascript } from '@codemirror/lang-javascript';

@Component({
  selector: 'app-code-editor',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="code-editor-container">
      <div #editor class="code-editor"></div>
    </div>
  `,
  styles: [
    `
      .code-editor-container {
        border: 1px solid #e0e0e0;
        border-radius: 4px;
        overflow: hidden;
        margin-bottom: 0;
      }
      .code-editor {
        height: 100%;
        font-family: 'Fira Code', 'Consolas', monospace;
      }
      :host {
        display: block;
        width: 100%;
        height: 200px;
        margin-bottom: 0;
      }
      ::ng-deep .cm-editor .cm-content {
        white-space: pre !important;
        tab-size: 4 !important;
      }
      ::ng-deep .cm-editor {
        margin-bottom: 0 !important;
      }
    `,
  ],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => CodeEditorComponent),
      multi: true,
    },
  ],
})
export class CodeEditorComponent
  implements OnInit, OnDestroy, ControlValueAccessor
{
  @ViewChild('editor', { static: true }) editorRef!: ElementRef;
  @Input() readOnly = false;
  @Input() language: 'typescript' | 'csharp' = 'typescript';
  @Input() tabSize = 4;

  private view: EditorView | null = null;
  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};
  private value = '';

  ngOnInit() {
    this.initEditor();
  }

  ngOnDestroy() {
    this.view?.destroy();
  }

  writeValue(value: string): void {
    this.value = value || '';

    if (this.view) {
      this.updateContent();
    }
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.readOnly = isDisabled;
    if (this.view) {
      this.initEditor();
    }
  }

  private initEditor() {
    if (this.view) {
      this.view.destroy();
    }

    this.view = new EditorView({
      state: EditorState.create({
        doc: this.value,
        extensions: [
          basicSetup,
          javascript({ typescript: this.language === 'typescript' }),
          EditorState.readOnly.of(this.readOnly),
          EditorView.updateListener.of((update) => {
            if (update.docChanged) {
              const value = update.state.doc.toString();
              this.value = value;
              this.onChange(value);
            }
          }),
        ],
      }),
      parent: this.editorRef.nativeElement,
    });
  }

  private updateContent() {
    if (!this.view) return;

    this.view.dispatch({
      changes: {
        from: 0,
        to: this.view.state.doc.length,
        insert: this.value,
      },
    });
  }
}
