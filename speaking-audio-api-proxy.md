# Speaking Audio API Proxy Implementation

## Goal

Store learner recordings securely while the application is being completed:

```text
Blazor browser
  -> Backend API (JWT, validation, storage key creation)
    -> Cloudflare R2 private bucket
  -> SQL Server (metadata, transcript, scores)
```

The browser must never receive Cloudflare R2 credentials. Audio files must not be public.

## Existing Project Context

- `backend/src/Backend.Infrastructure/Services/R2StorageService.cs` already uploads files to R2.
- `backend/src/Backend.Api/Program.cs` already exposes `/api/mock-tests/upload` for public exam assets.
- `frontend/src/Frontend.App/wwwroot/js/speaking-interop.js` records audio with `MediaRecorder`, but currently discards `audioChunks` after recording stops.
- JWT user id is available in the `sub` claim.

Do not reuse `/api/mock-tests/upload` for learner recordings. It is currently unauthenticated and returns a public URL.

## R2 Setup

Create a separate private bucket:

```text
ielts-user-audio
```

Keep the existing public R2 bucket for exam JSON, videos, and other public assets.

Add configuration through User Secrets or environment variables, never a committed settings file:

```json
{
  "CloudflareR2": {
    "Endpoint": "https://<account-id>.r2.cloudflarestorage.com",
    "UserAudioBucketName": "ielts-user-audio"
  }
}
```

The R2 API token must only have access to this bucket.

## Database Design

Use two tables because one test attempt has multiple spoken responses.

### SpeakingAttempts

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | Primary key |
| `UserId` | `int` | Foreign key to `Users` |
| `MockTestId` | `int?` | Foreign key to `MockTests`, nullable while the route uses `ExamUrl` |
| `ExamUrl` | `nvarchar(max)` | Temporary source reference |
| `Status` | `nvarchar(32)` | `InProgress`, `Completed`, or `Failed` |
| `StartedAt` | `datetime2` | UTC |
| `CompletedAt` | `datetime2?` | UTC |

### SpeakingResponses

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `uniqueidentifier` | Primary key |
| `AttemptId` | `uniqueidentifier` | Foreign key to `SpeakingAttempts` |
| `QuestionId` | `int` | Question id from the speaking JSON |
| `PartNumber` | `int` | IELTS part number |
| `StorageKey` | `nvarchar(512)` | R2 object key, not a public URL |
| `ContentType` | `nvarchar(128)` | Normally `audio/webm` |
| `FileSizeBytes` | `bigint` | Uploaded file size |
| `DurationMs` | `int` | Recording duration |
| `Transcript` | `nvarchar(max)?` | Speech-to-text output |
| `Fluency` | `decimal(3,1)?` | Scoring result |
| `Lexical` | `decimal(3,1)?` | Scoring result |
| `Grammar` | `decimal(3,1)?` | Scoring result |
| `Coherence` | `decimal(3,1)?` | Scoring result |
| `Pronunciation` | `decimal(3,1)?` | Scoring result |
| `Overall` | `decimal(3,1)?` | Scoring result |
| `ScoringStatus` | `nvarchar(32)` | `Uploading`, `Uploaded`, `Transcribing`, `Scored`, or `Failed` |
| `AnalysisJson` | `nvarchar(max)?` | Detailed structured feedback |
| `CreatedAt` | `datetime2` | UTC |
| `DeletedAt` | `datetime2?` | Soft-delete timestamp |

Add indexes for `UserId + CreatedAt`, `AttemptId + QuestionId`, and a unique index on `AttemptId + QuestionId` if each question accepts one final response.

## R2 Storage Service

Extend `IR2StorageService` with private-audio operations:

```csharp
Task UploadPrivateAudioAsync(Stream stream, string key, string contentType, CancellationToken cancellationToken);
Task<StoredFile> GetPrivateFileAsync(string key, CancellationToken cancellationToken);
Task DeletePrivateFileAsync(string key, CancellationToken cancellationToken);
```

Use a server-generated key only:

```text
speaking/{userId}/{attemptId}/{questionId}-{responseId}.webm
```

Never use a learner-provided filename as the R2 key. Do not return `PublicUrlBase` for this bucket.

## API Contract

Every speaking endpoint requires `[Authorize]`. The backend gets the user id from JWT `sub`; never accept `userId` from the request body.

### Create an attempt

```http
POST /api/speaking/attempts
Authorization: Bearer <jwt>
Content-Type: application/json
```

```json
{
  "mockTestId": 12,
  "examUrl": "https://example.com/speaking-test.json"
}
```

```json
{
  "attemptId": "b9d4e5ea-6f94-45b3-a09a-62a4d44a377e"
}
```

### Upload one response

```http
POST /api/speaking/attempts/{attemptId}/responses
Authorization: Bearer <jwt>
Content-Type: multipart/form-data
```

Form fields:

```text
file: answer.webm
questionId: 4599
partNumber: 1
durationMs: 42300
```

Response:

```json
{
  "responseId": "845d4ec5-3a5b-49bd-bf04-d27dcaf246c5",
  "status": "Uploaded"
}
```

### Other endpoints

```text
GET    /api/speaking/attempts/{attemptId}
GET    /api/speaking/responses/{responseId}/audio
DELETE /api/speaking/responses/{responseId}
POST   /api/speaking/attempts/{attemptId}/complete
```

Each endpoint must confirm that the current JWT user owns the attempt or response before returning data, streaming audio, or deleting anything.

## Upload Endpoint Workflow

1. Validate JWT and load the attempt by id.
2. Reject if the attempt does not belong to the current user.
3. Validate `questionId` and `partNumber` against the exam data when available.
4. Allow only audio MIME types such as `audio/webm`, `audio/ogg`, and `audio/mp4`.
5. Enforce a maximum request/file size, for example 25 MB.
6. Generate `responseId` and R2 key on the server.
7. Insert a `SpeakingResponse` row with `ScoringStatus = Uploading`.
8. Stream `IFormFile.OpenReadStream()` to R2; do not load the complete file into memory.
9. On success, store key, byte size, content type, and `ScoringStatus = Uploaded`.
10. On failure, set `ScoringStatus = Failed` and return a safe error message.

If R2 succeeds but the SQL update fails, delete the newly uploaded R2 object as compensation. This prevents orphaned audio files.

## Frontend Changes

After `MediaRecorder` stops, create a blob from the existing `audioChunks`:

```javascript
const blob = new Blob(audioChunks, {
  type: mediaRecorder.mimeType || 'audio/webm'
});
```

Build a `FormData` payload with the blob, `questionId`, `partNumber`, and `durationMs`, then upload it to the backend API. The request goes to the application backend, not to R2.

UI behavior:

1. Create an attempt when the learner starts the speaking test.
2. Upload each response after the learner presses `Done speaking`.
3. Show `Uploading recording...` before evaluation.
4. On a transient error, keep the blob in memory and show `Retry upload`.
5. Only show final evaluation after the response upload succeeds.

The frontend currently stores `authToken` in local storage but its backend `HttpClient` instances do not automatically attach it. Add a scoped `DelegatingHandler` that reads the token and adds:

```http
Authorization: Bearer <jwt>
```

The upload request must include this header as well.

## Scoring Plan

### Phase 1: Complete product flow

1. Upload and retain the audio.
2. Persist the current transcript and heuristic feedback.
3. Display the score as `Estimated practice feedback`, not an official IELTS band.
4. Set `ScoringStatus = Scored` once current client-side feedback is stored.

### Phase 2: Server-side scoring

Use an asynchronous worker/job:

```text
Uploaded -> Transcribing -> Scored
                    \-> Failed
```

The worker reads private audio from R2, calls the transcription/scoring provider, and updates the transcript and score columns. Upload requests should not wait for AI processing.

## Audio Access and Retention

- Playback uses `GET /api/speaking/responses/{responseId}/audio`.
- The API checks ownership, then streams the private R2 object to the browser.
- Delete operation removes the R2 object and soft-deletes the database row.
- Configure an R2 lifecycle rule to delete recordings after a retention period, such as 90 days.
- Ask for learner consent before the first persistent recording.

## Implementation Order

1. Add `SpeakingAttempt` and `SpeakingResponse` entities, DbSets, mappings, and migration.
2. Add private upload, read, and delete methods to R2 storage service.
3. Implement authenticated attempt and response upload endpoints.
4. Add a frontend auth header handler.
5. Update `speaking-interop.js` to preserve and upload recorded blobs.
6. Add upload progress, retry, and failure states to `IeltsSpeaking.razor`.
7. Persist current feedback and response history.
8. Add private playback and deletion.
9. Replace heuristic scoring with background transcription/AI scoring later.

## Acceptance Checklist

- [ ] A logged-in learner can create a speaking attempt.
- [ ] Each finished response uploads as `audio/webm` through the backend API.
- [ ] The R2 bucket/object is private and has no public URL.
- [ ] SQL stores only metadata, transcript, scores, and R2 key.
- [ ] A learner cannot access another learner's recording.
- [ ] Upload errors allow retry without forcing another recording.
- [ ] Audio and database row are both deleted on learner deletion/retention cleanup.
- [ ] Public exam asset uploads remain separate from learner-audio uploads.
