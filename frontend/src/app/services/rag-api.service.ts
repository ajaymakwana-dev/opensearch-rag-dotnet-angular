import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface DocumentListItem {
  documentId: string;
  fileName: string;
  contentType: string;
  indexedAtUtc: string;
  chunkCount: number;
}

export interface IngestResult {
  documentId: string;
  fileName: string;
  chunkCount: number;
  indexedAtUtc: string;
}

export interface SearchHit {
  chunkId: string;
  documentId: string;
  fileName: string;
  chunkIndex: number;
  content: string;
  score: number;
}

export interface AskResponse {
  answer: string;
  sources: SearchHit[];
}

@Injectable({ providedIn: 'root' })
export class RagApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  listDocuments(): Observable<DocumentListItem[]> {
    return this.http.get<DocumentListItem[]>(`${this.base}/documents`);
  }

  upload(file: File): Observable<IngestResult> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<IngestResult>(`${this.base}/documents/upload`, form);
  }

  deleteDocument(documentId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/documents/${documentId}`);
  }

  ask(question: string, topK = 5): Observable<AskResponse> {
    return this.http.post<AskResponse>(`${this.base}/rag/ask`, { question, topK });
  }
}
