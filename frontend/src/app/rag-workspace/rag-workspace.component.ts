import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  AskResponse,
  DocumentListItem,
  RagApiService
} from '../services/rag-api.service';

@Component({
  selector: 'app-rag-workspace',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './rag-workspace.component.html',
  styleUrl: './rag-workspace.component.scss'
})
export class RagWorkspaceComponent implements OnInit {
  private readonly api = inject(RagApiService);

  readonly documents = signal<DocumentListItem[]>([]);
  readonly busy = signal(false);
  readonly status = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly answer = signal<AskResponse | null>(null);

  question = '';
  selectedFile: File | null = null;

  ngOnInit(): void {
    this.refreshDocuments();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
  }

  upload(): void {
    if (!this.selectedFile) {
      this.error.set('Choose a UTF-8 text file first.');
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    this.status.set(`Indexing ${this.selectedFile.name}…`);

    this.api.upload(this.selectedFile).subscribe({
      next: (result) => {
        this.status.set(`Indexed ${result.fileName} into ${result.chunkCount} chunk(s).`);
        this.selectedFile = null;
        this.busy.set(false);
        this.refreshDocuments();
      },
      error: (err) => this.fail(err)
    });
  }

  ask(): void {
    if (!this.question.trim()) {
      this.error.set('Enter a question.');
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    this.status.set('Retrieving context and generating answer…');

    this.api.ask(this.question.trim()).subscribe({
      next: (response) => {
        this.answer.set(response);
        this.status.set(`Retrieved ${response.sources.length} source chunk(s).`);
        this.busy.set(false);
      },
      error: (err) => this.fail(err)
    });
  }

  remove(doc: DocumentListItem): void {
    this.busy.set(true);
    this.api.deleteDocument(doc.documentId).subscribe({
      next: () => {
        this.status.set(`Deleted ${doc.fileName}.`);
        this.busy.set(false);
        this.refreshDocuments();
      },
      error: (err) => this.fail(err)
    });
  }

  private refreshDocuments(): void {
    this.api.listDocuments().subscribe({
      next: (docs) => this.documents.set(docs),
      error: (err) => this.fail(err)
    });
  }

  private fail(err: unknown): void {
    const message =
      typeof err === 'object' && err !== null && 'message' in err
        ? String((err as { message: string }).message)
        : 'Request failed. Is the API and OpenSearch running?';
    this.error.set(message);
    this.busy.set(false);
  }
}
