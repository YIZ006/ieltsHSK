using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

// Dummy classes to match Frontend Models
public class ExamData
{
    public string Title { get; set; } = "";
    public string AudioUrl { get; set; } = "";
    public List<ExamPart> Parts { get; set; } = new();
}

public class WritingExamData
{
    public string Title { get; set; } = "";
    public int TotalMinutes { get; set; } = 60;
    public List<WritingTask> Tasks { get; set; } = new();
}

public class WritingTask
{
    public int TaskNumber { get; set; }
    public string TaskTitle { get; set; } = "";
    public int TimeRecommended { get; set; }
    public int MinWords { get; set; }
    public string Instruction { get; set; } = "";
    public string Prompt { get; set; } = "";
    public string RequireWords { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string? ImageAlt { get; set; }
}

public class SpeakingExamData
{
    public string Title { get; set; } = "";
    public List<SpeakingPart> Parts { get; set; } = new();
}

public class SpeakingPart
{
    public string Title { get; set; } = "";
    public string Caption { get; set; } = "";
    public int Time { get; set; }
    public int TimeToThink { get; set; }
    public string? CueCardHtml { get; set; }
    public List<SpeakingQuestion> Questions { get; set; } = new();
}

public class SpeakingQuestion
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
    public string VideoUrl { get; set; } = "";
}

public class ExamPart
{
    public int PartNumber { get; set; }
    public string PassageTitle { get; set; } = "";
    public string PassageHtml { get; set; } = "";
    public List<QuestionGroup> QuestionGroups { get; set; } = new();
}
public class QuestionGroup
{
    public string Instruction { get; set; } = "";
    public string GroupType { get; set; } = "Normal";
    public string GroupHtml { get; set; } = "";
    public List<QuestionData> Questions { get; set; } = new();
}
public class QuestionData
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
    public List<OptionData> Options { get; set; } = new();
    public int CorrectOptionId { get; set; }
    public string FillAnswer { get; set; } = "";
}
public class OptionData
{
    public int Id { get; set; }
    public string Label { get; set; } = "";
    public string Text { get; set; } = "";
}

class Program
{
    static void Main(string[] args)
    {
        string mode = "speaking"; // "listening", "reading", "writing", "speaking"

        if (mode == "listening") ParseListeningTest();
        else if (mode == "writing") ParseWritingTest();
        else if (mode == "speaking") ParseSpeakingTest();
    }

    static void ParseSpeakingTest()
    {
        string filePath = "IELTS Mock Test 2025 December Speaking Practise Test 1 _ IELTS Online Tests.html";
        if (!File.Exists(filePath))
        {
            // Fallback search for any speaking html file
            var files = Directory.GetFiles(".", "*Speaking*.html");
            if (files.Length > 0) filePath = files[0];
            else
            {
                Console.WriteLine("Khong tim thay file Speaking HTML!");
                return;
            }
        }

        string html = File.ReadAllText(filePath);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var exam = new SpeakingExamData
        {
            Title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText.Replace(" | IELTS Online Tests", "").Trim() ?? "Speaking Mock Test"
        };

        // Extract drupal-settings-json
        var scriptNode = doc.DocumentNode.SelectSingleNode("//script[@data-drupal-selector='drupal-settings-json']");
        if (scriptNode != null)
        {
            try
            {
                using var jsonDoc = JsonDocument.Parse(scriptNode.InnerText);
                var root = jsonDoc.RootElement;
                if (root.TryGetProperty("sot", out var sot) && sot.TryGetProperty("speakingObjectData", out var speakingData))
                {
                    foreach (var partElem in speakingData.EnumerateArray())
                    {
                        var part = new SpeakingPart
                        {
                            Title = partElem.GetProperty("title").GetString() ?? "",
                            Caption = partElem.TryGetProperty("caption", out var cap) ? cap.GetString() ?? "" : "",
                            Time = partElem.TryGetProperty("time", out var t) ? t.GetInt32() : 0,
                            TimeToThink = partElem.TryGetProperty("timeToThink", out var ttt) ? ttt.GetInt32() : 0,
                            CueCardHtml = partElem.TryGetProperty("videoDesc", out var vd) ? vd.GetString() : null
                        };

                        if (partElem.TryGetProperty("videoUrl", out var videoList))
                        {
                            foreach (var v in videoList.EnumerateArray())
                            {
                                var q = new SpeakingQuestion
                                {
                                    Id = v.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                                    VideoUrl = v.TryGetProperty("src", out var src) ? src.GetString() ?? "" : ""
                                };

                                if (v.TryGetProperty("question", out var qText) && !string.IsNullOrWhiteSpace(qText.GetString()))
                                {
                                    q.Text = qText.GetString()!.Trim();
                                }
                                else if (v.TryGetProperty("text_question", out var tqText))
                                {
                                    q.Text = Regex.Replace(tqText.GetString() ?? "", "<.*?>", "").Trim();
                                }

                                part.Questions.Add(q);
                            }
                        }

                        exam.Parts.Add(part);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Loi doc JSON Speaking: {ex.Message}");
            }
        }

        var json = JsonSerializer.Serialize(exam, new JsonSerializerOptions { WriteIndented = true });
        string outputPath = "IELTS_Mock_Test_2025_December_Speaking_Practise_Test_1.json";
        File.WriteAllText(outputPath, json);
        Console.WriteLine($"Parse hoan tat! Da luu vao {outputPath}");
    }

    static void ParseWritingTest()
    {
        string filePath = "IELTS_Mock_Test_2025_December_Writing_Practise_Test_1.html";
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Khong tim thay {filePath}");
            return;
        }

        string html = File.ReadAllText(filePath);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var exam = new WritingExamData
        {
            Title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText.Replace("Take test | ", "").Trim() ?? "Writing Mock Test",
        };

        var sections = doc.DocumentNode.SelectNodes("//section[contains(@class, 'test-contents')]");
        if (sections != null)
        {
            int taskNum = 1;
            foreach (var section in sections)
            {
                var task = new WritingTask { TaskNumber = taskNum };
                
                task.TaskTitle = section.SelectSingleNode(".//h1[contains(@class, 'test-contents__title')]")?.InnerText.Trim() ?? $"Writing Task {taskNum}";
                
                var pNodes = section.SelectNodes(".//p");
                if (pNodes != null)
                {
                    foreach (var p in pNodes)
                    {
                        var text = p.InnerText.Trim();
                        if (text.Contains("You should spend about"))
                        {
                            task.Instruction = p.OuterHtml;
                            var minMatch = Regex.Match(text, @"(\d+)\s*minutes");
                            if (minMatch.Success) task.TimeRecommended = int.Parse(minMatch.Groups[1].Value);
                        }
                        else if (text.Contains("You should write") && text.Contains("at least"))
                        {
                            task.RequireWords = text.Replace("You should write", "").Trim().TrimEnd('.');
                            var wordMatch = Regex.Match(text, @"(\d+)\s*words");
                            if (wordMatch.Success) task.MinWords = int.Parse(wordMatch.Groups[1].Value);
                        }
                        else if (!string.IsNullOrWhiteSpace(text))
                        {
                            task.Prompt += p.OuterHtml;
                        }
                    }
                }

                var img = section.SelectSingleNode(".//img[not(contains(@class, 'lazyload'))]");
                if (img != null)
                {
                    task.ImageUrl = img.GetAttributeValue("src", "");
                    task.ImageAlt = img.GetAttributeValue("alt", "");
                }

                exam.Tasks.Add(task);
                taskNum++;
            }
        }

        var json = JsonSerializer.Serialize(exam, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath.Replace(".html", ".json"), json);
        Console.WriteLine($"Parse hoan tat! Da luu vao {filePath.Replace(".html", ".json")}");
    }

    static void ParseListeningTest()
    {
        string filePath = "listening_source.html";
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Khong tim thay {filePath}. Vui long luu file HTML vao ScraperTool.");
            return;
        }

        string html = File.ReadAllText(filePath);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var exam = new ExamData
        {
            Title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText.Trim() ?? "Listening Mock Test",
        };

        // Extract Youtube Video ID
        var match = Regex.Match(html, @"waS74McMxUY");
        if (match.Success)
        {
            exam.AudioUrl = "https://www.youtube.com/embed/waS74McMxUY";
        }
        else
        {
            // Try to find generic iframe
            var iframe = doc.DocumentNode.SelectSingleNode("//iframe[contains(@src, 'youtube')]");
            if (iframe != null) {
                var src = iframe.GetAttributeValue("src", "");
                var idMatch = Regex.Match(src, @"([a-zA-Z0-9_-]{11})");
                if (idMatch.Success)
                    exam.AudioUrl = $"https://www.youtube.com/embed/{idMatch.Groups[1].Value}";
            }
        }

        var parts = doc.DocumentNode.SelectNodes("//section[contains(@class, 'test-panel')]");
        if (parts == null) {
            Console.WriteLine("Khong tim thay <section class='test-panel'>");
            return;
        }

        int partNum = 1;
        foreach (var partNode in parts)
        {
            var part = new ExamPart
            {
                PartNumber = partNum++,
                PassageTitle = partNode.SelectSingleNode(".//h2[contains(@class, 'test-panel__title')]")?.InnerText.Trim() ?? $"Part {partNum - 1}"
            };

            var items = partNode.SelectNodes(".//div[contains(@class, 'test-panel__item')]");
            if (items != null)
            {
                foreach (var itemNode in items)
                {
                    var group = new QuestionGroup();
                    
                    var qTitleNode = itemNode.SelectSingleNode(".//h4[contains(@class, 'test-panel__question-title')]");
                    var qDescNode = itemNode.SelectSingleNode(".//div[contains(@class, 'test-panel__question-desc')]");
                    
                    if (qTitleNode != null) group.Instruction += qTitleNode.InnerText.Trim() + "\n";
                    if (qDescNode != null) group.Instruction += qDescNode.InnerText.Trim();

                    // Check if it's multiple choice
                    var smGroups = itemNode.SelectNodes(".//div[contains(@class, 'test-panel__question-sm-group')]");
                    if (smGroups != null && smGroups.Count > 0)
                    {
                        group.GroupType = "MultipleChoice";
                        foreach (var sm in smGroups)
                        {
                            var qData = new QuestionData();
                            var titleNode = sm.SelectSingleNode(".//div[contains(@class, 'test-panel__question-sm-title')]");
                            if (titleNode != null)
                            {
                                string title = titleNode.InnerText.Trim();
                                var numMatch = Regex.Match(title, @"^(\d+)\.");
                                if (numMatch.Success) qData.Id = int.Parse(numMatch.Groups[1].Value);
                                qData.Text = title;
                            }

                            var options = sm.SelectNodes(".//div[contains(@class, 'test-panel__answer-item')]");
                            if (options != null)
                            {
                                int optId = 1;
                                foreach (var opt in options)
                                {
                                    var labelNode = opt.SelectSingleNode(".//span[contains(@class, 'test-panel__answer-option')]");
                                    var textNode = opt.SelectSingleNode(".//label");
                                    qData.Options.Add(new OptionData
                                    {
                                        Id = optId++,
                                        Label = labelNode?.InnerText.Trim() ?? "",
                                        Text = textNode?.InnerText.Trim() ?? ""
                                    });
                                }
                            }
                            group.Questions.Add(qData);
                        }
                    }
                    else
                    {
                        // HtmlBlock (Table, Form, Dropdown inside paragraphs)
                        group.GroupType = "HtmlBlock";
                        var answerNode = itemNode.SelectSingleNode(".//div[contains(@class, 'test-panel__answer')]") 
                                         ?? itemNode.SelectSingleNode(".//div[contains(@class, 'test-panel__answers-wrap')]");
                        
                        if (answerNode != null)
                        {
                            // Clean up HTML: replace complex inputs with simple normalized inputs
                            var inputs = answerNode.SelectNodes(".//input[@data-num] | .//select[@data-num]");
                            if (inputs != null)
                            {
                                foreach (var input in inputs)
                                {
                                    string numStr = input.GetAttributeValue("data-num", "0");
                                    if (int.TryParse(numStr, out int qId))
                                    {
                                        group.Questions.Add(new QuestionData { Id = qId });
                                        
                                        // Replace input with clean Blazor-friendly input
                                        var cleanInput = doc.CreateElement("input");
                                        cleanInput.SetAttributeValue("type", "text");
                                        cleanInput.SetAttributeValue("class", "ielts-input");
                                        cleanInput.SetAttributeValue("data-q", qId.ToString());
                                        cleanInput.SetAttributeValue("id", $"q-{qId}");
                                        
                                        // Preserve select if it was a dropdown? 
                                        // Actually, for dropdown, rendering a text input is also fine, or we can keep it as select.
                                        if (input.Name == "select") {
                                            cleanInput = doc.CreateElement("select");
                                            cleanInput.SetAttributeValue("class", "ielts-input");
                                            cleanInput.SetAttributeValue("data-q", qId.ToString());
                                            cleanInput.SetAttributeValue("id", $"q-{qId}");
                                            // Add empty option
                                            var opt = doc.CreateElement("option");
                                            opt.SetAttributeValue("value", "");
                                            opt.InnerHtml = "";
                                            cleanInput.AppendChild(opt);
                                            // Add A, B, C options
                                            var origOptions = input.SelectNodes(".//option");
                                            if(origOptions != null) {
                                                foreach(var o in origOptions) {
                                                    if(string.IsNullOrWhiteSpace(o.InnerText)) continue;
                                                    var newOpt = doc.CreateElement("option");
                                                    newOpt.SetAttributeValue("value", o.GetAttributeValue("value", ""));
                                                    newOpt.InnerHtml = o.InnerHtml;
                                                    cleanInput.AppendChild(newOpt);
                                                }
                                            }
                                        }

                                        // If input is wrapped in test-panel__iotquestion, replace the whole wrapper to remove clutter
                                        if (input.ParentNode.HasClass("test-panel__iotquestion"))
                                        {
                                            var qNumSpan = doc.CreateElement("span");
                                            qNumSpan.SetAttributeValue("class", "ielts-qnum");
                                            qNumSpan.InnerHtml = qId.ToString();
                                            
                                            var container = doc.CreateElement("span");
                                            container.SetAttributeValue("class", "ielts-input-container");
                                            container.AppendChild(qNumSpan);
                                            container.AppendChild(cleanInput);
                                            
                                            input.ParentNode.ParentNode.ReplaceChild(container, input.ParentNode);
                                        }
                                        else
                                        {
                                            input.ParentNode.ReplaceChild(cleanInput, input);
                                        }
                                    }
                                }
                            }
                            group.GroupHtml = answerNode.InnerHtml;
                        }
                    }
                    part.QuestionGroups.Add(group);
                }
            }
            exam.Parts.Add(part);
        }

        var json = JsonSerializer.Serialize(exam, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText("listening_output.json", json);
        Console.WriteLine("Parse hoan tat! Da luu vao listening_output.json");
    }
}
