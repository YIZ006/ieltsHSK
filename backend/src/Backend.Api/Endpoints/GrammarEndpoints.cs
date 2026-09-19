using Backend.Api.Common;
using Backend.Application.Abstractions;
using Backend.Application.DTOs;
using Backend.Domain.Entities;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Backend.Api.Endpoints;

public static class GrammarEndpoints
{
    public static IEndpointRouteBuilder MapGrammarEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/grammar-structures", async (
            string? search,
            string? bandLevel,
            string? category,
            string? grammarTopic,
            bool? isActive,
            Backend.Infrastructure.Persistence.AppDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var query = dbContext.GrammarStructures.AsQueryable();

            if (isActive.HasValue)
            {
                query = query.Where(g => g.IsActive == isActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(bandLevel) && !bandLevel.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(g => g.BandLevel == bandLevel || g.BandLevel.Contains(bandLevel));
            }

            if (!string.IsNullOrWhiteSpace(category) && !category.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(g => g.Category == category || g.Category.Contains(category));
            }

            if (!string.IsNullOrWhiteSpace(grammarTopic) && !grammarTopic.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(g => g.GrammarTopic == grammarTopic || g.GrammarTopic.Contains(grammarTopic));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLowerInvariant().Trim();
                query = query.Where(g =>
                    g.StructureCode.ToLower().Contains(s) ||
                    g.GrammarTopic.ToLower().Contains(s) ||
                    g.Formula.ToLower().Contains(s) ||
                    g.UsageFunction.ToLower().Contains(s) ||
                    g.AdvancedExample.ToLower().Contains(s) ||
                    g.VietnameseMeaning.ToLower().Contains(s) ||
                    (g.KeyCollocations != null && g.KeyCollocations.ToLower().Contains(s)) ||
                    (g.Tags != null && g.Tags.ToLower().Contains(s)));
            }

            var list = await query
                .OrderBy(g => g.DisplayOrder)
                .ThenByDescending(g => g.Id)
                .Select(g => new Backend.Application.DTOs.GrammarStructureDto
                {
                    Id = g.Id,
                    StructureCode = g.StructureCode,
                    BandLevel = g.BandLevel,
                    Category = g.Category,
                    GrammarTopic = g.GrammarTopic,
                    Formula = g.Formula,
                    UsageFunction = g.UsageFunction,
                    BasicExample = g.BasicExample,
                    AdvancedExample = g.AdvancedExample,
                    VietnameseMeaning = g.VietnameseMeaning,
                    KeyCollocations = g.KeyCollocations,
                    CommonMistakes = g.CommonMistakes,
                    PracticeExercise = g.PracticeExercise,
                    Tags = g.Tags,
                    ReferenceSource = g.ReferenceSource,
                    DisplayOrder = g.DisplayOrder,
                    IsActive = g.IsActive,
                    CreatedAt = g.CreatedAt,
                    UpdatedAt = g.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(list);
        });


        app.MapGet("/api/grammar-structures/{id:int}", async (
            int id,
            Backend.Infrastructure.Persistence.AppDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var g = await dbContext.GrammarStructures.FindAsync(new object[] { id }, cancellationToken);
            if (g == null) return Results.NotFound("Không tìm thấy cấu trúc ngữ pháp.");

            var dto = new Backend.Application.DTOs.GrammarStructureDto
            {
                Id = g.Id,
                StructureCode = g.StructureCode,
                BandLevel = g.BandLevel,
                Category = g.Category,
                GrammarTopic = g.GrammarTopic,
                Formula = g.Formula,
                UsageFunction = g.UsageFunction,
                BasicExample = g.BasicExample,
                AdvancedExample = g.AdvancedExample,
                VietnameseMeaning = g.VietnameseMeaning,
                KeyCollocations = g.KeyCollocations,
                CommonMistakes = g.CommonMistakes,
                PracticeExercise = g.PracticeExercise,
                Tags = g.Tags,
                ReferenceSource = g.ReferenceSource,
                DisplayOrder = g.DisplayOrder,
                IsActive = g.IsActive,
                CreatedAt = g.CreatedAt,
                UpdatedAt = g.UpdatedAt
            };
            return Results.Ok(dto);
        });


        app.MapPost("/api/admin/grammar-structures", async (
            Backend.Application.DTOs.CreateGrammarStructureDto req,
            Backend.Infrastructure.Persistence.AppDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(req.StructureCode) || string.IsNullOrWhiteSpace(req.Formula))
            {
                return Results.BadRequest("Mã cấu trúc và Công thức là bắt buộc.");
            }

            var entity = new Backend.Domain.Entities.GrammarStructure
            {
                StructureCode = req.StructureCode.Trim(),
                BandLevel = string.IsNullOrWhiteSpace(req.BandLevel) ? "7.0 - 8.0" : req.BandLevel.Trim(),
                Category = string.IsNullOrWhiteSpace(req.Category) ? "Writing Task 2" : req.Category.Trim(),
                GrammarTopic = req.GrammarTopic.Trim(),
                Formula = req.Formula.Trim(),
                UsageFunction = req.UsageFunction.Trim(),
                BasicExample = req.BasicExample?.Trim(),
                AdvancedExample = req.AdvancedExample.Trim(),
                VietnameseMeaning = req.VietnameseMeaning.Trim(),
                KeyCollocations = req.KeyCollocations?.Trim(),
                CommonMistakes = req.CommonMistakes?.Trim(),
                PracticeExercise = req.PracticeExercise?.Trim(),
                Tags = req.Tags?.Trim(),
                ReferenceSource = req.ReferenceSource?.Trim(),
                DisplayOrder = req.DisplayOrder,
                IsActive = req.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.GrammarStructures.Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Created($"/api/grammar-structures/{entity.Id}", entity);
        });


        app.MapPut("/api/admin/grammar-structures/{id:int}", async (
            int id,
            Backend.Application.DTOs.UpdateGrammarStructureDto req,
            Backend.Infrastructure.Persistence.AppDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var entity = await dbContext.GrammarStructures.FindAsync(new object[] { id }, cancellationToken);
            if (entity == null) return Results.NotFound("Không tìm thấy cấu trúc ngữ pháp.");

            entity.StructureCode = req.StructureCode.Trim();
            entity.BandLevel = req.BandLevel.Trim();
            entity.Category = req.Category.Trim();
            entity.GrammarTopic = req.GrammarTopic.Trim();
            entity.Formula = req.Formula.Trim();
            entity.UsageFunction = req.UsageFunction.Trim();
            entity.BasicExample = req.BasicExample?.Trim();
            entity.AdvancedExample = req.AdvancedExample.Trim();
            entity.VietnameseMeaning = req.VietnameseMeaning.Trim();
            entity.KeyCollocations = req.KeyCollocations?.Trim();
            entity.CommonMistakes = req.CommonMistakes?.Trim();
            entity.PracticeExercise = req.PracticeExercise?.Trim();
            entity.Tags = req.Tags?.Trim();
            entity.ReferenceSource = req.ReferenceSource?.Trim();
            entity.DisplayOrder = req.DisplayOrder;
            entity.IsActive = req.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Ok(entity);
        });


        app.MapDelete("/api/admin/grammar-structures/{id:int}", async (
            int id,
            Backend.Infrastructure.Persistence.AppDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var entity = await dbContext.GrammarStructures.FindAsync(new object[] { id }, cancellationToken);
            if (entity == null) return Results.NotFound("Không tìm thấy cấu trúc ngữ pháp.");

            dbContext.GrammarStructures.Remove(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { success = true, message = $"Đã xóa cấu trúc '{entity.StructureCode}'." });
        });


        app.MapPost("/api/admin/grammar-structures/bulk-delete", async (
            Backend.Application.DTOs.GrammarBulkDeleteDto req,
            Backend.Infrastructure.Persistence.AppDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            if (req.Ids == null || req.Ids.Count == 0)
            {
                return Results.BadRequest("Danh sách ID không được rỗng.");
            }

            var items = await dbContext.GrammarStructures
                .Where(g => req.Ids.Contains(g.Id))
                .ToListAsync(cancellationToken);

            dbContext.GrammarStructures.RemoveRange(items);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(new { success = true, deletedCount = items.Count });
        });

        // ─── Bulk Import Multiple Excel files (.xlsx) ───

        app.MapPost("/api/admin/grammar-structures/import-multiple", async (
            Microsoft.AspNetCore.Http.IFormFileCollection files,
            Backend.Infrastructure.Persistence.AppDbContext dbContext,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (files == null || files.Count == 0)
            {
                return Results.BadRequest("Không có file nào được upload.");
            }

            var mode = httpContext.Request.Form.TryGetValue("mode", out var modeValue) &&
                       modeValue.ToString().Trim().Equals("upsert", StringComparison.OrdinalIgnoreCase)
                ? "upsert"
                : "skip";

            var allExisting = await dbContext.GrammarStructures.ToListAsync(cancellationToken);
            var existingDict = allExisting.ToDictionary(g => g.StructureCode.Trim(), g => g, StringComparer.OrdinalIgnoreCase);

            int totalRows = 0, success = 0, updated = 0, skipped = 0, fail = 0;
            var errors = new List<string>();

            foreach (var file in files)
            {
                if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"File '{file.FileName}' bị bỏ qua vì không phải định dạng .xlsx");
                    continue;
                }

                try
                {
                    using var stream = file.OpenReadStream();
                    using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
                    var worksheet = workbook.Worksheet(1);
                    var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                    if (lastRow < 2) continue;

                    // Đọc Header hàng 1 để map cột linh hoạt
                    var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    var headerRow = worksheet.Row(1);
                    var lastCol = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 13;
                    for (int col = 1; col <= lastCol; col++)
                    {
                        var hName = headerRow.Cell(col).GetString()?.Trim().ToLowerInvariant() ?? "";
                        if (!string.IsNullOrEmpty(hName))
                        {
                            headerMap[hName] = col;
                        }
                    }

                    int GetCol(string[] possibleNames, int defaultCol)
                    {
                        foreach (var name in possibleNames)
                        {
                            if (headerMap.TryGetValue(name.ToLowerInvariant(), out var colIdx)) return colIdx;
                        }
                        return defaultCol;
                    }

                    int colCode = GetCol(new[] { "StructureCode", "Mã cấu trúc", "Mã", "Code" }, 1);
                    int colBand = GetCol(new[] { "BandLevel", "Band", "Level", "Mức Band" }, 2);
                    int colCat  = GetCol(new[] { "Category", "Kỹ năng", "Dạng bài", "Phần thi" }, 3);
                    int colTopic = GetCol(new[] { "GrammarTopic", "Chủ điểm", "Topic", "Chủ điểm ngữ pháp" }, 4);
                    int colFormula = GetCol(new[] { "Formula", "Công thức", "Cấu trúc" }, 5);
                    int colUsage = GetCol(new[] { "UsageFunction", "Chức năng", "Mục đích", "Usage" }, 6);
                    int colExample = GetCol(new[] { "Example", "Ví dụ", "Ví dụ minh họa", "Câu ví dụ", "BasicExample", "Câu gốc", "Ví dụ gốc" }, 7);
                    int colAdvEx = GetCol(new[] { "AdvancedExample", "Câu nâng cấp", "Band 8.0", "Ví dụ nâng cao" }, -1);
                    int colMeaning = GetCol(new[] { "VietnameseMeaning", "Nghĩa tiếng Việt", "Dịch nghĩa", "Meaning" }, 8);
                    int colMistakes = GetCol(new[] { "CommonMistakes", "Lỗi sai", "Lỗi thường gặp", "Pitfalls" }, 9);
                    int colExercise = GetCol(new[] { "PracticeExercise", "Bài tập", "Exercise", "Luyện tập" }, 10);
                    int colTags = GetCol(new[] { "Tags", "Tag", "Từ khóa lọc" }, 11);
                    int colColloc = GetCol(new[] { "KeyCollocations", "Collocations", "Từ vựng", "Từ khóa" }, -1);
                    int colRefSource = GetCol(new[] { "ReferenceSource", "Nguồn tham khảo", "Nguồn", "Reference", "Source" }, -1);

                    for (int r = 2; r <= lastRow; r++)
                    {
                        totalRows++;
                        var row = worksheet.Row(r);

                        string code = row.Cell(colCode).GetString()?.Trim() ?? "";
                        string formula = row.Cell(colFormula).GetString()?.Trim() ?? "";

                        if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(formula))
                        {
                            continue; // Hàng trống
                        }

                        if (string.IsNullOrWhiteSpace(code))
                        {
                            code = $"STR_{DateTime.UtcNow.Ticks % 1000000:D6}";
                        }

                        string band = row.Cell(colBand).GetString()?.Trim() ?? "7.0 - 8.0";
                        string category = row.Cell(colCat).GetString()?.Trim() ?? "Writing Task 2";
                        string topic = row.Cell(colTopic).GetString()?.Trim() ?? "Ngữ pháp nâng cao";
                        string usage = row.Cell(colUsage).GetString()?.Trim() ?? "Nâng cao điểm Grammatical Range & Accuracy";
                        string ex = row.Cell(colExample).GetString()?.Trim() ?? "";
                        string advEx = (colAdvEx > 0 ? row.Cell(colAdvEx).GetString()?.Trim() : "") ?? "";
                        string basicEx = ex;
                        if (string.IsNullOrWhiteSpace(advEx)) advEx = ex;
                        if (string.IsNullOrWhiteSpace(advEx)) advEx = formula;
                        string meaning = row.Cell(colMeaning).GetString()?.Trim() ?? "";
                        string colloc = (colColloc > 0 ? row.Cell(colColloc).GetString()?.Trim() : "") ?? "";
                        string mistakes = row.Cell(colMistakes).GetString()?.Trim() ?? "";
                        string exercise = row.Cell(colExercise).GetString()?.Trim() ?? "";
                        string tags = row.Cell(colTags).GetString()?.Trim() ?? "";
                        string refSource = (colRefSource > 0 ? row.Cell(colRefSource).GetString()?.Trim() : "") ?? "";

                        if (string.IsNullOrWhiteSpace(meaning)) meaning = topic;

                        if (existingDict.TryGetValue(code, out var existing))
                        {
                            if (mode == "upsert")
                            {
                                existing.BandLevel = band;
                                existing.Category = category;
                                existing.GrammarTopic = topic;
                                existing.Formula = formula;
                                existing.UsageFunction = usage;
                                existing.BasicExample = string.IsNullOrWhiteSpace(basicEx) ? null : basicEx;
                                existing.AdvancedExample = advEx;
                                existing.VietnameseMeaning = meaning;
                                existing.KeyCollocations = string.IsNullOrWhiteSpace(colloc) ? null : colloc;
                                existing.CommonMistakes = string.IsNullOrWhiteSpace(mistakes) ? null : mistakes;
                                existing.PracticeExercise = string.IsNullOrWhiteSpace(exercise) ? null : exercise;
                                existing.Tags = string.IsNullOrWhiteSpace(tags) ? null : tags;
                                if (!string.IsNullOrWhiteSpace(refSource)) existing.ReferenceSource = refSource;
                                existing.UpdatedAt = DateTime.UtcNow;
                                updated++;
                            }
                            else
                            {
                                skipped++;
                            }
                        }
                        else
                        {
                            var newStructure = new Backend.Domain.Entities.GrammarStructure
                            {
                                StructureCode = code,
                                BandLevel = band,
                                Category = category,
                                GrammarTopic = topic,
                                Formula = formula,
                                UsageFunction = usage,
                                BasicExample = string.IsNullOrWhiteSpace(basicEx) ? null : basicEx,
                                AdvancedExample = advEx,
                                VietnameseMeaning = meaning,
                                KeyCollocations = string.IsNullOrWhiteSpace(colloc) ? null : colloc,
                                CommonMistakes = string.IsNullOrWhiteSpace(mistakes) ? null : mistakes,
                                PracticeExercise = string.IsNullOrWhiteSpace(exercise) ? null : exercise,
                                Tags = string.IsNullOrWhiteSpace(tags) ? null : tags,
                                ReferenceSource = string.IsNullOrWhiteSpace(refSource) ? null : refSource,
                                CreatedAt = DateTime.UtcNow
                            };
                            dbContext.GrammarStructures.Add(newStructure);
                            existingDict[code] = newStructure;
                            success++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    fail++;
                    errors.Add($"Lỗi xử lý file '{file.FileName}': {ex.Message}");
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(new Backend.Application.DTOs.GrammarImportExcelResponse
            {
                TotalRows = totalRows,
                Success = success,
                Updated = updated,
                Skipped = skipped,
                Fail = fail,
                Errors = errors
            });
        }).DisableAntiforgery();

        // ─── Download Template Excel File (.xlsx) ───

        app.MapGet("/api/admin/grammar-structures/template", () =>
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("Grammar_Structures");

            // Header Titles (11 columns chuẩn)
            var headers = new[]
            {
                "StructureCode", "BandLevel", "Category", "GrammarTopic",
                "Formula", "UsageFunction", "Example",
                "VietnameseMeaning", "CommonMistakes", "PracticeExercise", "Tags"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#0284C7");
                cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            }

            // Demo Rows (4 dòng đại diện các cấp độ)
            var demoData = new[]
            {
                new[] {
                    "FND_TENSE_01", "4.0 - 5.0", "General", "Thì hiện tại đơn (Present Simple)",
                    "S + V(s/es) + O | S + do/does + not + V",
                    "Diễn tả chân lý, sự thật hiển nhiên, thói quen lặp lại; diễn tả số liệu cố định trong Task 1.",
                    "He walks to work every morning.",
                    "Anh ấy đi bộ đi làm mỗi buổi sáng.",
                    "Quên thêm s/es khi chủ ngữ là ngôi thứ 3 số ít (He, She, It, danh từ số ít).",
                    "Chia động từ: The data (indicate) that urban areas (consume) more electricity than rural regions.",
                    "present_simple, tenses, foundation, task1"
                },
                new[] {
                    "FND_PASS_01", "5.0 - 6.0", "Writing Task 1 & 2", "Câu bị động (Passive Voice)",
                    "S + be + V3/ed (+ by O)",
                    "Tạo phong cách học thuật khách quan trong IELTS Writing, đặc biệt khi tả bài quy trình (Process Task 1).",
                    "Raw tea leaves are harvested by hand and transported to processing facilities.",
                    "Lá chè tươi được thu hoạch thủ công và sau đó được vận chuyển đến các cơ sở chế biến.",
                    "Dùng sai dạng phân từ 2 (V3) hoặc quên động từ 'to be' chia theo thì của câu.",
                    "Chuyển sang câu bị động: Workers clean and dry the coffee beans.",
                    "passive_voice, process, task1, academic"
                },
                new[] {
                    "FND_COND_02", "5.5 - 6.5", "Writing Task 2", "Câu điều kiện loại 2 (Conditional Type 2)",
                    "If + S + V2/were, S + would/could/might + V",
                    "Diễn tả giả định trái với thực tế hiện tại; lập luận biện chứng phản đề trong Task 2.",
                    "If governments subsidized renewable energy infrastructure, fossil fuel dependence would diminish.",
                    "Nếu các chính phủ trợ cấp cho cơ sở hạ tầng năng lượng tái tạo, sự phụ thuộc vào nhiên liệu hóa thạch sẽ giảm.",
                    "Dùng 'was' thay vì 'were' trong văn phong học thuật trang trọng; nhầm lẫn thì ở mệnh đề chính.",
                    "Viết lại câu giả định: Because petrol is cheap, people drive private cars too much.",
                    "conditional, type2, task2, hypothesis"
                },
                new[] {
                    "W_INV_01", "7.5 - 8.5", "Writing Task 2", "Đảo ngữ (Inversion)",
                    "Not only + Aux + S + V, but S + (also) + V",
                    "Nhấn mạnh 2 tác động song hành, tạo ấn tượng học thuật mạnh ở mở đoạn hoặc câu chủ đề.",
                    "Not only does technological adoption facilitate learning, but it also enhances productivity.",
                    "Không chỉ việc áp dụng công nghệ tạo điều kiện cho học tập, mà nó còn nâng cao năng suất.",
                    "Quên đảo trợ động từ lên trước chủ ngữ sau 'Not only' (ví dụ viết sai: Not only computers help...).",
                    "Rewrite: Tourism creates jobs and it also introduces local culture.",
                    "inversion, emphasis, task2, academic"
                }
            };

            for (int r = 0; r < demoData.Length; r++)
            {
                for (int c = 0; c < demoData[r].Length; c++)
                {
                    ws.Cell(r + 2, c + 1).Value = demoData[r][c];
                }
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var bytes = stream.ToArray();

            return Results.File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "IELTS_Grammar_Structures_Template.xlsx"
            );
        });

        // ─── Export All Grammar Structures to Excel (.xlsx) ───

        app.MapGet("/api/admin/grammar-structures/export", async (
            Backend.Infrastructure.Persistence.AppDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var list = await dbContext.GrammarStructures
                .OrderBy(g => g.DisplayOrder)
                .ThenBy(g => g.Id)
                .ToListAsync(cancellationToken);

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("Grammar_Structures");

            var headers = new[]
            {
                "StructureCode", "BandLevel", "Category", "GrammarTopic",
                "Formula", "UsageFunction", "Example",
                "VietnameseMeaning", "CommonMistakes", "PracticeExercise", "Tags", "ReferenceSource"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#0284C7");
                cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            }

            for (int r = 0; r < list.Count; r++)
            {
                var item = list[r];
                string ex = !string.IsNullOrWhiteSpace(item.BasicExample) ? item.BasicExample : item.AdvancedExample;
                ws.Cell(r + 2, 1).Value = item.StructureCode;
                ws.Cell(r + 2, 2).Value = item.BandLevel;
                ws.Cell(r + 2, 3).Value = item.Category;
                ws.Cell(r + 2, 4).Value = item.GrammarTopic;
                ws.Cell(r + 2, 5).Value = item.Formula;
                ws.Cell(r + 2, 6).Value = item.UsageFunction;
                ws.Cell(r + 2, 7).Value = ex;
                ws.Cell(r + 2, 8).Value = item.VietnameseMeaning;
                ws.Cell(r + 2, 9).Value = item.CommonMistakes ?? "";
                ws.Cell(r + 2, 10).Value = item.PracticeExercise ?? "";
                ws.Cell(r + 2, 11).Value = item.Tags ?? "";
                ws.Cell(r + 2, 12).Value = item.ReferenceSource ?? "";
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var bytes = stream.ToArray();

            var filename = $"IELTS_Grammar_Structures_Export_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx";
            return Results.File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename
            );
        });

        // ── NOTIFICATION APIS ──
        // 1. Get notifications for user (broadcast + specific user)

        return app;
    }
}
