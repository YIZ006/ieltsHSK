# HSK Module Design Specification

## Overview

Module HSK 3.0 cho nền tảng ieltsHSK. Hỗ trợ 4 kỹ năng (Nghe, Đọc, Viết, Nói) theo hệ thống 9 cấp mới, tích hợp từ vựng và dashboard kết hợp.

---

## HSK 3.0 Level Structure

### 9 Cấp Độ (3 Bậc)

| Bậc | Cấp | Tên | Điểm đỗ tối thiểu | Thang điểm |
|-----|-----|-----|-------------------|------------|
| Sơ cấp | HSK1 | 一级 | 120/200 | 0-200 |
| Sơ cấp | HSK2 | 二级 | 120/200 | 0-200 |
| Sơ cấp | HSK3 | 三级 | 180/300 | 0-300 |
| Trung cấp | HSK4 | 四级 | 180/300 | 0-300 |
| Trung cấp | HSK5 | 五级 | 180/300 | 0-300 |
| Trung cấp | HSK6 | 六级 | 180/300 | 0-300 |
| Cao cấp | HSK7 | 七级 | TBD | TBD |
| Cao cấp | HSK8 | 八级 | TBD | TBD |
| Cao cấp | HSK9 | 九级 | TBD | TBD |

> **Note**: HSK7-9 đang trong giai đoạn phát triển bởi Bộ Giáo dục TQ. Cấu trúc đề thi chưa ổn định. Hệ thống sẽ hỗ trợ placeholder cho 3 cấp này, dữ liệu thực tế thêm sau.

### Lưu trữ Level
- Chuỗi `"HSK1"` đến `"HSK9"`
- Group tier: `level.StartsWith("HSK") && int.Parse(level[3..]) <= 3` → Sơ cấp, v.v.
- User level lưu trong `User.Level` field (đã có sẵn) + localStorage key `hsk_level`

---

## Exam JSON Schema

### Cấu trúc chung (áp dụng cho cả 4 kỹ năng)

```json
{
  "title": "HSK3 Mock Test 1 - Listening",
  "hskLevel": "HSK3",
  "skill": "listening",
  "totalMinutes": 35,
  "audioUrl": "https://r2.../hsk/audio/hsk3-listening-1.mp3",
  "parts": [
    {
      "partNumber": 1,
      "instruction": "第一部分：听句子，选择正确答案",
      "instructionHtml": null,
      "questions": [
        {
          "id": "1",
          "type": "mcq",
          "text": "Câu hỏi hiển thị ở đây",
          "audioUrl": null,
          "imageUrl": null,
          "options": [
            { "id": "A", "label": "A", "text": "Đáp án A" },
            { "id": "B", "label": "B", "text": "Đáp án B" },
            { "id": "C", "label": "C", "text": "Đáp án C" }
          ],
          "correctAnswer": "B"
        }
      ]
    }
  ]
}
```

### Question Types

| Type | Mô tả | Áp dụng | Fields bổ sung |
|------|-------|----------|----------------|
| `mcq` | Trắc nghiệm chọn đáp án | Nghe, Đọc | `options`, `correctAnswer` |
| `fill` | Điền từ vào chỗ trống | Nghe, Đọc | `correctAnswer` (string hoặc string[]) |
| `order` | Sắp xếp câu/từ đúng thứ tự | Đọc, Viết | `items` (string[]), `correctOrder` (int[]) |
| `pinyin-write` | Viết pinyin cho chữ Hán | Viết | `hanzi`, `correctPinyin` |
| `char-write` | Viết chữ Hán từ pinyin/nghĩa | Viết | `pinyin`, `meaning`, `correctHanzi` |
| `match` | Nối cặp từ/câu | Đọc | `leftItems`, `rightItems`, `correctPairs` |
| `speak-read` | Đọc to đoạn văn bản | Nói | `passageText`, `referenceAudioUrl?` |
| `speak-describe` | Miêu tả tranh/chủ đề | Nói | `imageUrl?`, `topicPrompt`, `prepSeconds` |
| `html-block` | Khối HTML tùy chỉnh (input embedded) | Nghe | `groupHtml` (chứa `<input data-q="...">`) |

### Answer Key Format

Tương tự IELTS, tách riêng thành file `.answers.json`:

```json
{
  "title": "HSK3 Mock Test 1 - Listening Answers",
  "answers": {
    "1": ["B"],
    "2": ["A"],
    "15": ["图书馆", "圖書館"],
    "20": ["tú shū guǎn"]
  }
}
```

- Key = question ID (string)
- Value = mảng các đáp án chấp nhận được
- Chấm điểm: so sánh sau khi trim + lowercase + chuẩn hóa Unicode

---

## Vocabulary Data Schema

### Entity: HskVocabulary

| Field | Type | Required | Mô tả |
|-------|------|----------|-------|
| Id | int | PK | Auto-increment |
| HskLevel | string | Yes | "HSK1"-"HSK9" |
| Hanzi | string | Yes | Chữ Hán giản thể |
| Pinyin | string | Yes | Phiên âm có thanh điệu |
| Meaning | string | Yes | Nghĩa tiếng Việt |
| WordType | string | No | noun/verb/adj/adv/prep/conj/classifier/phrase |
| ExampleSentence | string | No | Câu ví dụ tiếng Trung |
| ExamplePinyin | string | No | Pinyin câu ví dụ |
| ExampleMeaning | string | No | Dịch nghĩa câu ví dụ |
| AudioUrl | string | No | URL file phát âm trên R2 |
| DisplayOrder | int | Yes | Thứ tự hiển thị trong level |
| IsActive | bool | Yes | Mặc định true |
| CreatedAt | DateTime | Auto | Timestamp tạo |

### Unique Constraint
- `(HskLevel, Hanzi)` — không trùng chữ trong cùng level

### Excel Import Template Columns
| Column | Required | Note |
|--------|----------|------|
| HskLevel | Yes | HSK1-HSK9 |
| Hanzi | Yes | |
| Pinyin | Yes | |
| Meaning | Yes | |
| WordType | No | |
| ExampleSentence | No | |
| ExamplePinyin | No | |
| ExampleMeaning | No | |
| AudioUrl | No | |
| DisplayOrder | No | Auto-assign nếu bỏ trống |

---

## Dashboard Design

### Layout
Sử dụng `HskLayout` (sidebar trái + topbar + nội dung chính). Mirror `IeltsLayout`.

### Sidebar Sections (loaded từ API `/api/hsk/sections`)
1. Luyện đề (`/hsk/luyen-de`)
2. Nghe (`/hsk/listening`)
3. Đọc (`/hsk/reading`)
4. Viết (`/hsk/writing`)
5. Nói (`/hsk/speaking`)
6. Từ vựng (`/hsk/tu-vung`)

### Dashboard Widgets

#### 1. Header
- Ngày hiện tại (tiếng Việt)
- Badge cấp độ HSK hiện tại
- Tier indicator (Sơ cấp / Trung cấp / Cao cấp)

#### 2. Hero Banner
- Nhắc nhở mục tiêu hàng ngày
- Streak counter (từ User.Streak)

#### 3. Vocabulary Progress Widget
- Progress bar: % từ đã học của cấp độ hiện tại
- Quick flashcard preview (3-5 từ ngẫu nhiên chưa thuộc)
- Link đến trang từ vựng đầy đủ

#### 4. Latest Results Summary
- Bảng tóm tắt kết quả gần nhất theo từng kỹ năng
- Điểm số + pass/fail indicator

#### 5. Learning Modules Grid
- Card grid từ sections API
- Mỗi card: icon + tên + mô tả ngắn + link

---

## Route Map

### Frontend Routes

| Route | Page | Layout | Auth |
|-------|------|--------|------|
| `/hsk` | HskPortal (level selection) | MainLayout | No |
| `/hsk/dashboard` | HskDashboard | HskLayout | Yes |
| `/hsk/luyen-de` | HskMockTests | HskLayout | No |
| `/hsk/listening` | HskListening (default sample) | ExamLayout | No |
| `/hsk/listening/{*ExamUrl}` | HskListening (from URL) | ExamLayout | No |
| `/hsk/reading` | HskReading (default sample) | ExamLayout | No |
| `/hsk/reading/{*ExamUrl}` | HskReading (from URL) | ExamLayout | No |
| `/hsk/writing` | HskWriting (default sample) | ExamLayout | No |
| `/hsk/writing/{*ExamUrl}` | HskWriting (from URL) | ExamLayout | No |
| `/hsk/speaking` | HskSpeaking (default sample) | ExamLayout | No |
| `/hsk/speaking/{*ExamUrl}` | HskSpeaking (from URL) | ExamLayout | No |
| `/hsk/tu-vung` | HskVocabulary | HskLayout | No |
| `/admin/hsk-mock-tests` | AdminHskMockTests | AdminLayout | Admin |
| `/admin/hsk-vocab` | AdminHskVocab | AdminLayout | Admin |

### Backend API Endpoints

| Method | Route | Mô tả |
|--------|-------|-------|
| GET | `/api/hsk/sections` | Learning sections cho HSK |
| POST | `/api/hsk/upload-media` | Upload image/audio lên R2 |
| POST | `/api/hsk/save-exam` | Lưu exam JSON lên R2 + tạo/cập nhật MockTest |
| GET | `/api/hsk/vocab?level={level}` | Danh sách từ vựng theo level |
| POST | `/api/hsk/vocab` | Tạo từ vựng mới |
| PUT | `/api/hsk/vocab/{id}` | Cập nhật từ vựng |
| DELETE | `/api/hsk/vocab/{id}` | Xóa từ vựng |
| GET | `/api/hsk/vocab/template-excel` | Tải template Excel import |
| POST | `/api/hsk/vocab/import-excel` | Import từ vựng từ Excel |

---

## R2 Storage Convention

| Folder | Nội dung |
|--------|----------|
| `hsk/exams/` | Exam JSON files |
| `hsk/audio/` | Audio files cho listening/speaking |
| `hsk/images/` | Images cho reading/writing/speaking |
| `hsk/vocab-audio/` | Audio phát âm từ vựng |

---

## Scoring Logic

### Chấm điểm tự động (client-side)
- **Listening & Reading**: Dùng `AnswerKeyService.Grade()` tái sử dụng từ IELTS
- So sánh đáp án sau normalize (trim, lowercase, Unicode NFC)
- Tính raw score → đối chiếu threshold pass/fail theo level
- **Writing & Speaking**: Không chấm tự động. Lưu bài làm, admin chấm thủ công hoặc tích hợp AI sau.

### Pass/Fail Thresholds
Lưu trong frontend constants hoặc config JSON:
```json
{
  "HSK1": { "total": 200, "pass": 120 },
  "HSK2": { "total": 200, "pass": 120 },
  "HSK3": { "total": 300, "pass": 180 },
  "HSK4": { "total": 300, "pass": 180 },
  "HSK5": { "total": 300, "pass": 180 },
  "HSK6": { "total": 300, "pass": 180 }
}
```

---

## Reuse Strategy

### Tái sử dụng trực tiếp (không sửa đổi)
- `ExamLayout` — layout làm bài thi
- `AnswerKeyService` — load + grade answer keys
- `ExamSessionService` — timer + session persistence
- `ExamSubmissionService.SaveToDbAsync()` — lưu kết quả
- `AdminLayout` — đã có HSK tab/sidebar
- `IR2StorageService` — upload/delete R2
- Auth infrastructure

### Adapt nhẹ (copy + modify)
- `IeltsLayout` → `HskLayout` (thay API call, branding)
- `ToeicService` → `HskService` (thay endpoint, model type)
- `ToeicBuilderService` → pattern tương tự cho HSK save-exam
- `AdminToeicMockTests` → `AdminHskMockTests` (filter by HskUrl)

### Mới hoàn toàn
- `HskVocabulary` entity + CRUD
- `HskPortal` (level selection gate)
- `HskDashboard` (combined widgets)
- `HskVocabulary` browser page
- `AdminHskVocab` manager

</content>