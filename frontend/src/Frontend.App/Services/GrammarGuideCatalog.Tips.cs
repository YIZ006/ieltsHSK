using Frontend.App.Models;

namespace Frontend.App.Services;

public static partial class GrammarGuideCatalog
{
    private static GrammarGuideSectionDto BuildTipsSection()
    {
        return new GrammarGuideSectionDto
        {
            Key = "tips",
            Overline = "MODULE 06 · CHIẾN THUẬT & PHÂN BIỆT BẪY ĐIỂM",
            Title = "Chiến Thuật Học Tập & Phân Biệt Bẫy Điểm",
            Description = "5 chuyên đề thực chiến phân biệt cụm từ dễ nhầm lẫn (Make vs Do, Join vs Attend), làm chủ cụm động từ, thành ngữ và phương pháp ghi nhớ ngữ pháp siêu tốc.",
            Icon = "bi-lightbulb-fill",
            ColorTheme = "#eab308",
            Topics = new List<GrammarGuideTopicDto>
            {
                new GrammarGuideTopicDto
                {
                    Slug = "tips-hoc-tu-vung-ngu-phap",
                    Title = "Chiến Thuật Học Ngữ Pháp & Từ Vựng",
                    EnglishTitle = "Spaced Repetition & Contextual Sentence Mining",
                    SectionKey = "tips",
                    SectionTitle = "Mẹo & Chiến Thuật",
                    ShortDescription = "Phương pháp lặp lại ngắt quãng (Spaced Repetition) và ghi nhớ ngữ pháp qua câu ngữ cảnh học thuật thay vì học vẹt quy tắc.",
                    Icon = "bi-lightning-fill",
                    IconBgColor = "#eab308",
                    DifficultyLevel = "Cơ bản",
                    ReadTimeMinutes = 5,
                    OrderIndex = 1,
                    FormulaPreview = "Review Interval: 1d → 3d → 7d → 14d → 30d",
                    SkillTarget = "Phương pháp ghi nhớ vĩnh viễn",
                    Tags = new() { "mẹo học", "spaced repetition", "chiến thuật", "active recall" },
                    ConceptExplanation = "Ghi nhớ ngữ pháp bằng cách học thuộc lòng công thức trơ trọi (như S + V + O) thường thất bại vì não bộ không tạo được liên kết ngữ cảnh (Contextual Association). Phương pháp Spaced Repetition (Lặp lại ngắt quãng) kết hợp Active Recall (Chủ động truy xuất) và Sentence Mining (Thu thập câu trọn vẹn) giúp biến ngữ pháp từ trí nhớ ngắn hạn thành phản xạ tự nhiên.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Chu kỳ lặp lại ngắt quãng tối ưu", Formula = "Lần 1 (Ngay sau học) → Lần 2 (Sau 24h) → Lần 3 (Sau 3 ngày) → Lần 4 (Sau 1 tuần) → Lần 5 (Sau 1 tháng)", ColorVariant = "blue", Breakdown = new() { "Đánh bại đường cong quên lãng Ebbinghaus" } },
                        new GrammarFormulaBlock { Type = "Phương pháp Sentence Mining", Formula = "Chỉ học từ vựng và ngữ pháp KÈM THEO CÂU VĂN HOÀN CHỈNH từ tài liệu uy tín (Cambridge, The Economist)", ColorVariant = "green", Breakdown = new() { "Nắm bắt được cả ngữ pháp, collocations và giới từ đi kèm" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Tự đặt câu với chính bản thân (Personalization)", Example = "Áp dụng cấu trúc mới viết về thói quen, công việc của chính mình", Note = "Não bộ ưu tiên ghi nhớ thông tin liên quan đến bản thân" },
                        new GrammarRuleTableItem { Rule = "Phân tích lỗi sai hàng tuần (Error Log)", Example = "Ghi chép lại các lỗi sai mạo từ, thì, hòa hợp chủ vị vào sổ tay", Note = "Tránh lặp lại lỗi sai tương tự trong bài thi thật" }
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Xây dựng ngân hàng câu mẫu học thuật cá nhân (IELTS Phrase Bank)",
                            Explanation = "Học theo cụm từ và cấu trúc câu hoàn chỉnh thay vì từng từ đơn lẻ.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "The adoption of renewable energy plays an indispensable role in sustainable development.", Vietnamese = "Việc áp dụng năng lượng tái tạo đóng một vai trò không thể thiếu trong phát triển bền vững." }
                            }
                        }
                    },
                    SignalWords = new() { "spaced repetition", "active recall", "sentence mining", "error log", "collocation" },
                    SignalWordPlacementRule = "Áp dụng nguyên tắc ôn tập ngắt quãng đều đặn 15 phút mỗi ngày thay vì dồn ép học 5 tiếng vào cuối tuần.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Học thuộc lòng quy tắc ngữ pháp một cách thụ động",
                            WrongExample = "Chỉ đọc đi đọc lại sách lý thuyết mà không làm bài tập hoặc không tự viết câu.",
                            CorrectExample = "Chủ động làm bài tập tương tác, kiểm tra phản xạ và viết câu áp dụng ngay lập tức.",
                            Explanation = "Thụ động đọc lại không tạo ra kết nối nơ-ron mạnh mẽ bằng việc chủ động giải bài tập và truy xuất thông tin."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Học từ vựng dạng danh sách liệt kê nghĩa tiếng Việt",
                            WrongExample = "acquire = đạt được; mitigate = giảm thiểu; viable = khả thi (không kèm ngữ cảnh).",
                            CorrectExample = "Học cả cụm: 'acquire specialized knowledge', 'mitigate adverse impacts', 'a financially viable alternative'.",
                            Explanation = "Học từ đơn lẻ dẫn đến việc không biết cách kết hợp từ và dùng sai giới từ trong bài thi viết."
                        }
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "According to cognitive science, what is the most effective approach to mastering complex academic grammar?",
                            Options = [
                                "Active retrieval practice combined with spaced interval reviews and full-sentence contextual mining.",
                                "Passive re-reading of grammar rules multiple times right before the exam.",
                                "Memorizing lists of isolated grammar formulas without practicing writing.",
                                "Studying grammar for eight consecutive hours once every month."
                            ],
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "Active retrieval practice combined with spaced interval reviews and full-sentence contextual mining.",
                            Explanation = "Nghiên cứu khoa học nhận thức chứng minh việc chủ động truy xuất (Active Retrieval) kết hợp lặp lại ngắt quãng (Spaced Interval) và học qua câu ngữ cảnh là phương pháp hiệu quả nhất."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Why is maintaining an 'Academic Error Log' critical for breaking the Band 6.5 ceiling in IELTS Writing?",
                            Options = [
                                "It systematically identifies recurring systematic mistakes (articles, prepositions, agreement) for targeted eradication.",
                                "It allows students to copy essays from sample band 9 books word-for-word.",
                                "It eliminates the need to practice Speaking Part 2.",
                                "It guarantees an automatic score increase without further study."
                            ],
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "It systematically identifies recurring systematic mistakes (articles, prepositions, agreement) for targeted eradication.",
                            Explanation = "Sổ tay theo dõi lỗi sai giúp người học nhận diện chính xác các 'lỗ hổng' ngữ pháp cố hữu để khắc phục triệt để."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Which study technique best embodies 'Sentence Mining' for IELTS preparation?",
                            Options = [
                                "Extracting authentic, well-structured sentences from reputable publications like The Economist and analyzing their grammar syntax.",
                                "Translating Vietnamese sentences word-by-word into English using online software.",
                                "Memorizing 50 disconnected vocabulary words from a dictionary each morning.",
                                "Writing essays without reviewing or receiving feedback."
                            ],
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "Extracting authentic, well-structured sentences from reputable publications like The Economist and analyzing their grammar syntax.",
                            Explanation = "Sentence Mining là kỹ thuật trích xuất các câu văn mẫu mực từ tài liệu chuẩn ngữ để phân tích và sao chép cấu trúc tự nhiên."
                        }
                    },
                    LearningTip = "Hãy dành 10 phút sau mỗi buổi học để tự đặt 2 câu ví dụ gắn liền với trải nghiệm cá nhân của bạn. Điều này sẽ giúp bạn nhớ cấu trúc suốt đời!",
                    RelatedBandStructureCodes = new() { "TIP_LEARN_01" }
                },
                new GrammarGuideTopicDto
                {
                    Slug = "dong-tu-cum",
                    Title = "Cụm Động Từ (Phrasal Verbs)",
                    EnglishTitle = "Phrasal Verbs in Academic IELTS (Appropriateness & Tone)",
                    SectionKey = "tips",
                    SectionTitle = "Mẹo & Chiến Thuật",
                    ShortDescription = "Chiến lược dùng phrasal verbs: Nâng tầm điểm Speaking tự nhiên và thay thế bằng động từ Latin học thuật trong Writing Task 2.",
                    Icon = "bi-puzzle-fill",
                    IconBgColor = "#ca8a04",
                    DifficultyLevel = "Trung cấp",
                    ReadTimeMinutes = 6,
                    OrderIndex = 2,
                    FormulaPreview = "Verb + Particle (Carry out, Account for, Bring about)",
                    SkillTarget = "Speaking Lexical Resource & Writing Register",
                    Tags = new() { "phrasal verbs", "cụm động từ", "academic register", "speaking" },
                    ConceptExplanation = "Cụm động từ (Phrasal Verbs) gồm một động từ kết hợp với một hoặc hai tiểu từ (giới từ hoặc phó từ). Trong IELTS Speaking, phrasal verbs giúp bài nói tự nhiên và đạt điểm Lexical Resource Band 7.0+. Tuy nhiên trong Academic Writing, các phrasal verbs thông tục cần được chuyển đổi sang động từ đơn gốc Latin trang trọng hơn (ví dụ: carry out → conduct, look into → investigate).",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Cụm động từ học thuật được chấp nhận trong Writing", Formula = "account for (chiếm/giải thích) | carry out (tiến hành) | bring about (mang lại) | stem from (bắt nguồn từ)", ColorVariant = "blue", Breakdown = new() { "Đây là các phrasal verbs mang tính trung tính và học thuật cao" } },
                        new GrammarFormulaBlock { Type = "Bảng đối chiếu Informal vs Academic", Formula = "find out → discover | give up → abandon | put off → postpone | set up → establish", ColorVariant = "purple", Breakdown = new() { "Ưu tiên dùng động từ đơn trong Writing Task 2" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Cụm động từ tách được (Separable)", Example = "turn on / turn off / figure out", Note = "Nếu tân ngữ là đại từ (it/them) bắt buộc phải đứng ở giữa: figure it out (KHÔNG DÙNG: figure out it)" },
                        new GrammarRuleTableItem { Rule = "Cụm động từ không tách được (Inseparable)", Example = "look into, account for, cope with", Note = "Tân ngữ luôn luôn đứng sau giới từ: account for 45% of total sales" }
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Sử dụng 'account for' trong IELTS Writing Task 1",
                            Explanation = "Diễn đạt tỷ lệ phần trăm hoặc giải thích nguyên nhân.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Coal and natural gas accounted for approximately 60% of total energy production.", Vietnamese = "Than đá và khí đốt tự nhiên chiếm khoảng 60% tổng sản lượng năng lượng." }
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Diễn đạt nguồn gốc và nguyên nhân với 'stem from'",
                            Explanation = "Chỉ ra căn nguyên sâu xa của các vấn đề xã hội trong Task 2.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Much juvenile delinquency stems from inadequate parental supervision.", Vietnamese = "Phần lớn tình trạng phạm pháp ở tuổi vị thành niên bắt nguồn từ sự thiếu giám sát của phụ huynh." }
                            }
                        }
                    },
                    SignalWords = new() { "account for", "carry out", "bring about", "stem from", "result in", "cope with" },
                    SignalWordPlacementRule = "Lưu ý vị trí của tân ngữ đại từ (it, them, him, her) phải kẹp ở giữa các cụm động từ tách được.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Đặt đại từ sau tiểu từ của cụm động từ tách được",
                            WrongExample = "The researchers figured out it eventually.",
                            CorrectExample = "The researchers figured it out eventually.",
                            Explanation = "Với cụm động từ tách được (separable), đại từ 'it/them' bắt buộc phải kẹp giữa động từ và tiểu từ."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Dùng phrasal verbs quá thông tục trong IELTS Writing Task 2",
                            WrongExample = "The government should get rid of old coal plants.",
                            CorrectExample = "The government should decommission / phase out antiquated coal plants.",
                            Explanation = "'get rid of' là cụm từ khẩu ngữ thân mật; trong văn viết học thuật nên dùng 'decommission' hoặc 'phase out'."
                        }
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "In 2015, manufacturing and heavy industry _______ nearly 40% of national carbon emissions.",
                            Options = [ "accounted for", "brought about", "carried on", "stemmed from" ],
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "accounted for",
                            Explanation = "Trong IELTS Writing Task 1, cụm 'account for + [số %]' mang nghĩa là 'chiếm bao nhiêu phần trăm'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Which sentence uses the pronoun 'it' with a separable phrasal verb CORRECTLY?",
                            Options = [
                                "The engineering team took weeks to figure it out.",
                                "The engineering team took weeks to figure out it.",
                                "The engineering team took weeks to figure it about.",
                                "The engineering team took weeks to out figure it."
                            ],
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "The engineering team took weeks to figure it out.",
                            Explanation = "Với phrasal verb tách được như 'figure out', đại từ 'it' bắt buộc phải đứng ở giữa: 'figure it out'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Select the most suitable academic replacement for 'carry out' in: 'The team will <u>carry out</u> a clinical trial.'",
                            Options = [ "conduct", "make", "do", "take" ],
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "conduct",
                            Explanation = "'conduct a clinical trial' là collocation học thuật chuẩn mực nhất thay thế cho cụm 'carry out'."
                        }
                    },
                    LearningTip = "Ghi nhớ quy tắc: Trong Speaking hãy tự tin dùng phrasal verbs để thể hiện độ tự nhiên; nhưng trong Writing Task 2 hãy ưu tiên các động từ Latin như investigate, establish, accelerate!",
                    RelatedBandStructureCodes = new() { "TIP_PHRAS_01" }
                },
                new GrammarGuideTopicDto
                {
                    Slug = "thanh-ngu",
                    Title = "Thành Ngữ Tiếng Anh (Idioms)",
                    EnglishTitle = "Idiomatic Language in IELTS (Speaking Goldmine vs. Writing Taboo)",
                    SectionKey = "tips",
                    SectionTitle = "Mẹo & Chiến Thuật",
                    ShortDescription = "Quy tắc vàng: Sử dụng thành ngữ tự nhiên để chạm Band 7.5+ trong Speaking, nhưng TUYỆT ĐỐI tránh trong Academic Writing.",
                    Icon = "bi-chat-heart-fill",
                    IconBgColor = "#a16207",
                    DifficultyLevel = "Trung cấp",
                    ReadTimeMinutes = 6,
                    OrderIndex = 3,
                    FormulaPreview = "Idioms for Speaking (Once in a blue moon, Cost an arm and a leg)",
                    SkillTarget = "Speaking Band 7.5+ Lexical Resource",
                    Tags = new() { "idioms", "thành ngữ", "speaking", "bẫy thi cử", "lexical resource" },
                    ConceptExplanation = "Thành ngữ (Idioms) là cụm từ có nghĩa bóng không thể suy ra trực tiếp từ nghĩa đen của từng từ cấu thành. Tiêu chí Lexical Resource ở Band 7.0+ Speaking yêu cầu thí sinh 'uses some less common and idiomatic vocabulary'. Tuy nhiên, một sai lầm chết người của nhiều thí sinh là nhồi nhét thành ngữ vào bài viết Academic Writing Task 1 và Task 2 (nơi đòi hỏi văn phong trang trọng, khách quan).",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Thành ngữ tự nhiên cho IELTS Speaking", Formula = "once in a blue moon (rất hiếm khi) | cost an arm and a leg (rất đắt đỏ) | see eye to eye (đồng thuận) | a double-edged sword (con dao hai lưỡi)", ColorVariant = "blue", Breakdown = new() { "Dùng đúng ngữ cảnh, không gượng ép" } },
                        new GrammarFormulaBlock { Type = "Quy tắc cấm kỵ trong Writing Task 1 & 2", Formula = "KHÔNG DÙNG: at the end of the day, raining cats and dogs, every coin has two sides", ColorVariant = "red", Breakdown = new() { "Bị trừ điểm văn phong học thuật (Inappropriate Academic Register)" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Thành ngữ mang tính khái niệm được chấp nhận trong Writing", Example = "a double-edged sword, a vicious cycle, pave the way for", Note = "Các cụm từ mang tính ẩn dụ học thuật cao" },
                        new GrammarRuleTableItem { Rule = "Nguyên tắc 'Less is More'", Example = "Chỉ nên dùng 1-2 thành ngữ tự nhiên trong cả bài Speaking Part 1 & 2", Note = "Lạm dụng thành ngữ gượng gạo sẽ bị giám khảo đánh giá là học vẹt" }
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Mô tả tần suất hoặc chi phí trong IELTS Speaking Part 1 & 2",
                            Explanation = "Thể hiện phản xạ ngôn ngữ tự nhiên như người bản xứ.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "To be completely candid, I only dine out at luxury restaurants once in a blue moon.", Vietnamese = "Thành thật mà nói, tôi chỉ đi ăn ở các nhà hàng sang trọng rất hiếm khi mà thôi." }
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Sử dụng các ẩn dụ học thuật trong Writing Task 2",
                            Explanation = "Chỉ dùng các cụm từ ẩn dụ triết học/kinh tế trang trọng.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Poverty and illiteracy often perpetuate a vicious cycle in underprivileged communities.", Vietnamese = "Nghèo đói và nạn mù chữ thường tạo nên một vòng luẩn quẩn trong các cộng đồng kém may mắn." }
                            }
                        }
                    },
                    SignalWords = new() { "once in a blue moon", "see eye to eye", "a double-edged sword", "vicious cycle", "pave the way for" },
                    SignalWordPlacementRule = "Luôn đặt thành ngữ vào đúng ngữ cảnh hội thoại, tránh bắt đầu câu trả lời bằng một thành ngữ trơ trọi.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Dùng thành ngữ khẩu ngữ trong bài luận Academic Writing",
                            WrongExample = "In conclusion, every coin has two sides and we should find a balance.",
                            CorrectExample = "In conclusion, this controversial phenomenon presents both distinct merits and drawbacks.",
                            Explanation = "'every coin has two sides' là thành ngữ dân gian sáo rỗng, bị cấm trong bài thi viết học thuật."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Biến đổi sai cấu trúc cố định của thành ngữ",
                            WrongExample = "It cost me an arm and legs.",
                            CorrectExample = "It cost an arm and a leg.",
                            Explanation = "Thành ngữ là cấu trúc cố định (Fixed Expression), không được tự ý đổi số ít/nhiều hoặc thay từ đồng nghĩa."
                        }
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "Which of the following phrases is appropriate for IELTS Academic Writing Task 2?",
                            Options = [
                                "The phenomenon represents a vicious cycle of poverty and lack of education.",
                                "As everyone knows, it is raining cats and dogs outside.",
                                "At the end of the day, people must choose what they like.",
                                "Every coin has two sides, so we should look at both."
                            ],
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "The phenomenon represents a vicious cycle of poverty and lack of education.",
                            Explanation = "'a vicious cycle' (vòng luẩn quẩn) là ẩn dụ học thuật được công nhận; các thành ngữ khác mang tính khẩu ngữ dân gian bị cấm trong Writing Task 2."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "In IELTS Speaking Part 1, how can you express that you very rarely play video games?",
                            Options = [
                                "I only play video games once in a blue moon because of my busy academic schedule.",
                                "I play video games once in a blue sun.",
                                "I play video games one time in blue moon.",
                                "I never ever never play video games at all blue moon."
                            ],
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "I only play video games once in a blue moon because of my busy academic schedule.",
                            Explanation = "'once in a blue moon' là thành ngữ cố định chỉ tần suất cực kỳ hiếm hoi, dùng rất tự nhiên trong Speaking."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Why should candidates strictly avoid idioms like 'at the end of the day' in IELTS Writing?",
                            Options = [
                                "Because it is an informal conversational cliché that undermines academic objectivity and register.",
                                "Because it is grammatically incorrect.",
                                "Because it can only be used at 5:00 PM.",
                                "Because examiners do not understand idiomatic phrases."
                            ],
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "Because it is an informal conversational cliché that undermines academic objectivity and register.",
                            Explanation = "Các thành ngữ sáo rỗng (clichés) làm mất đi tính khách quan và văn phong học thuật của bài viết IELTS."
                        }
                    },
                    LearningTip = "Nếu bạn muốn Band 7.5+ Speaking, hãy học 10 thành ngữ thông dụng về cảm xúc, thời gian và công việc; nhưng TUYỆT ĐỐI cất chúng đi khi bước vào phòng thi Viết!",
                    RelatedBandStructureCodes = new() { "TIP_IDIOM_01" }
                },
                new GrammarGuideTopicDto
                {
                    Slug = "phan-biet-make-va-do",
                    Title = "Phân Biệt Make và Do",
                    EnglishTitle = "Collocations with Make vs. Do (Academic Precision)",
                    SectionKey = "tips",
                    SectionTitle = "Mẹo & Chiến Thuật",
                    ShortDescription = "Quy tắc bản chất: Make (sáng tạo, tạo ra cái mới) vs Do (hành động, nhiệm vụ, nghĩa vụ) và bảng collocations học thuật.",
                    Icon = "bi-check2-circle",
                    IconBgColor = "#854d0e",
                    DifficultyLevel = "Cơ bản",
                    ReadTimeMinutes = 5,
                    OrderIndex = 4,
                    FormulaPreview = "Make (Create / Produce) vs. Do (Action / Task / Duty)",
                    SkillTarget = "IELTS Writing Collocation Accuracy",
                    Tags = new() { "make vs do", "collocations", "phân biệt", "lỗi sai kinh điển" },
                    ConceptExplanation = "Cả 'Make' và 'Do' đều dịch sang tiếng Việt là 'làm', nhưng trong tiếng Anh chúng có bản chất tư duy hoàn toàn khác biệt: 'MAKE' thiên về việc sáng tạo, sản xuất ra một sản phẩm hữu hình hoặc tạo ra một kết quả/quyết định mới; trong khi 'DO' tập trung vào việc thực hiện một quá trình, nhiệm vụ, công việc hoặc nghĩa vụ sẵn có.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Bản chất của MAKE (Sáng tạo / Tạo ra)", Formula = "make a decision | make a contribution | make a difference | make an attempt | make a mistake", ColorVariant = "blue", Breakdown = new() { "Luôn gắn liền với việc tạo ra một cái mới" } },
                        new GrammarFormulaBlock { Type = "Bản chất của DO (Thực thi / Nhiệm vụ)", Formula = "do research | do an experiment | do harm / damage | do one's best | do business", ColorVariant = "purple", Breakdown = new() { "Luôn gắn liền với quá trình thực hiện một hoạt động" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Collocations học thuật với MAKE", Example = "make an effort, make a prediction, make progress, make adjustments", Note = "Rất hay dùng trong phân tích giải pháp Task 2" },
                        new GrammarRuleTableItem { Rule = "Collocations học thuật với DO", Example = "do research, do an assessment, do more harm than good", Note = "SAI PHỔ BIẾN: make research (PHẢI DÙNG: do research / conduct research)" }
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Sử dụng 'do research' và 'do harm' trong Writing Task 2",
                            Explanation = "Tránh lỗi sai kết hợp từ phổ biến nhất của thí sinh Việt Nam.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Excessive academic pressure can do substantial psychological harm to young students.", Vietnamese = "Áp lực học tập quá mức có thể gây ra những tổn hại tâm lý đáng kể cho học sinh nhỏ tuổi." },
                                new GrammarBilingualExample { English = "Scholars did extensive research before publishing the monograph.", Vietnamese = "Các học giả đã tiến hành nghiên cứu sâu rộng trước khi xuất bản cuốn chuyên khảo." }
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Sử dụng 'make a contribution' và 'make a decision'",
                            Explanation = "Đưa ra đánh giá về vai trò hoặc quyết sách chính phủ.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Technological innovation makes an indispensable contribution to national economic growth.", Vietnamese = "Đổi mới công nghệ tạo ra đóng góp không thể thiếu cho sự tăng trưởng kinh tế quốc gia." }
                            }
                        }
                    },
                    SignalWords = new() { "make a decision", "make a contribution", "do research", "do harm", "make progress", "do an experiment" },
                    SignalWordPlacementRule = "Học thuộc theo cụm (Collocation) cố định, không dịch word-by-word từ tiếng Việt.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Dùng 'make research' thay vì 'do research'",
                            WrongExample = "Scientists make research to find vaccine candidates.",
                            CorrectExample = "Scientists do research / conduct research to find vaccine candidates.",
                            Explanation = "'research' chỉ đi với động từ 'do' hoặc 'conduct/undertake', tuyệt đối không đi với 'make'."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Dùng 'do a decision' thay vì 'make a decision'",
                            WrongExample = "The committee must do a critical decision today.",
                            CorrectExample = "The committee must make a critical decision today.",
                            Explanation = "Quyết định là sự tạo ra một lựa chọn mới, luôn đi với 'make': 'make a decision'."
                        }
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "Environmental economists argue that unfettered industrial growth often _______ more harm than good to local communities.",
                            Options = [ "does", "makes", "creates", "produces" ],
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "does",
                            Explanation = "Collocation chuẩn xác: 'do more harm than good' (gây hại nhiều hơn mang lại lợi ích)."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Before investing venture capital, the financial analysts _______ extensive empirical research into market viability.",
                            Options = [ "did", "made", "fabricated", "put" ],
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "did",
                            Explanation = "Danh từ 'research' đi với động từ 'do': 'did extensive research' (hoặc conducted research)."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Civic participation _______ a profound contribution to democratic stability.",
                            Options = [ "makes", "does", "executes", "acts" ],
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "makes",
                            Explanation = "Collocation chuẩn xác: 'make a contribution to something' (đóng góp vào điều gì)."
                        }
                    },
                    LearningTip = "Quy tắc bất biến: Tạo ra cái mới hoặc đưa ra kết luận dùng MAKE (make decision, progress, contribution); thực hiện công việc, trách nhiệm hoặc gây hại dùng DO (do research, harm, business)!",
                    RelatedBandStructureCodes = new() { "TIP_MAKEDO_01" }
                },
                new GrammarGuideTopicDto
                {
                    Slug = "phan-biet-join-attend-participate",
                    Title = "Phân Biệt Join, Attend, Participate",
                    EnglishTitle = "Join vs. Attend vs. Participate in vs. Take part in",
                    SectionKey = "tips",
                    SectionTitle = "Mẹo & Chiến Thuật",
                    ShortDescription = "Phân biệt sắc thái: Join (gia nhập tổ chức), Attend (có mặt tại sự kiện), Participate in (chủ động tham gia đóng góp hoạt động).",
                    Icon = "bi-person-plus-fill",
                    IconBgColor = "#713f12",
                    DifficultyLevel = "Cơ bản",
                    ReadTimeMinutes = 5,
                    OrderIndex = 5,
                    FormulaPreview = "Join + Org | Attend + Event | Participate IN + Activity",
                    SkillTarget = "IELTS Writing Task 2 Precision",
                    Tags = new() { "join attend participate", "phân biệt từ", "collocations", "giới từ" },
                    ConceptExplanation = "Trong tiếng Việt, cả ba từ này đều có thể hiểu là 'tham gia'. Tuy nhiên trong tiếng Anh, chúng diễn đạt ba mức độ và tính chất hành động khác nhau hoàn toàn: JOIN (trở thành thành viên của một hội nhóm, tổ chức), ATTEND (có mặt, hiện diện tại một sự kiện, khóa học, hội thảo mang tính thụ động hơn), và PARTICIPATE IN / TAKE PART IN (chủ động đóng góp, can dự và có vai trò tích cực trong hoạt động).",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "JOIN + Danh từ (Không có giới từ)", Formula = "join a club / join the army / join an organization", ColorVariant = "blue", Breakdown = new() { "Mang nghĩa chính thức trở thành một thành viên" } },
                        new GrammarFormulaBlock { Type = "ATTEND + Danh từ (Không có giới từ)", Formula = "attend a conference / attend school / attend a workshop / attend university", ColorVariant = "purple", Breakdown = new() { "Mang nghĩa có mặt tại một sự kiện hoặc theo học" } },
                        new GrammarFormulaBlock { Type = "PARTICIPATE IN + Danh từ / V-ing (BẮT BUỘC CÓ 'IN')", Formula = "participate in extracurricular activities / participate in clinical trials", ColorVariant = "green", Breakdown = new() { "Chủ động can dự, đóng góp và có hành động thực tế" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Giới từ của Participate và Take part", Example = "participate IN / take part IN", Note = "Nếu thiếu giới từ 'in', câu sẽ sai ngữ pháp nghiêm trọng" },
                        new GrammarRuleTableItem { Rule = "Attend không đi với giới từ 'to' khi mang nghĩa tham dự", Example = "attend the seminar (KHÔNG DÙNG: attend to the seminar)", Note = "'attend to' mang nghĩa khác: chăm sóc, giải quyết (attend to a matter)" }
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Bàn luận về giáo dục và hoạt động ngoại khóa trong Task 2",
                            Explanation = "Sử dụng chính xác giữa việc đến trường và việc chủ động sinh hoạt.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Students who actively participate in community service develop superior leadership competencies.", Vietnamese = "Những học sinh chủ động tham gia vào hoạt động phục vụ cộng đồng phát triển năng lực lãnh đạo vượt trội." },
                                new GrammarBilingualExample { English = "More than 500 delegates from 40 nations attended the climate conference in Geneva.", Vietnamese = "Hơn 500 đại biểu từ 40 quốc gia đã tham dự hội nghị khí hậu tại Geneva." }
                            }
                        }
                    },
                    SignalWords = new() { "join an association", "attend a symposium", "participate in", "take part in", "partake in" },
                    SignalWordPlacementRule = "Hãy luôn nhớ: Participate bắt buộc phải đi kèm với giới từ IN trước danh từ.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Quên giới từ 'in' sau động từ 'participate'",
                            WrongExample = "All citizens should participate the municipal election.",
                            CorrectExample = "All citizens should participate in the municipal election.",
                            Explanation = "'Participate' là nội động từ, bắt buộc phải có giới từ 'in' đi kèm trước tân ngữ."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Dùng 'join' để chỉ việc tham dự một buổi hội thảo",
                            WrongExample = "Dr. Miller joined the medical conference yesterday.",
                            CorrectExample = "Dr. Miller attended the medical conference yesterday.",
                            Explanation = "Có mặt tại sự kiện, hội nghị dùng 'attend'; 'join' mang nghĩa trở thành hội viên hoặc tham gia vào một nhóm người đang cùng làm gì."
                        }
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "Over two thousand scholars from prestigious institutions _______ the international symposium on artificial intelligence.",
                            Options = [ "attended", "joined in", "participated", "took part" ],
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "attended",
                            Explanation = "Tham dự một sự kiện, hội nghị lớn dùng ngoại động từ trực tiếp 'attended' (không có giới từ)."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Undergraduate students are strongly encouraged to _______ collaborative research projects during their final semester.",
                            Options = [ "participate in", "attend", "join to", "participate" ],
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "participate in",
                            Explanation = "Chủ động can dự và đóng góp vào một hoạt động hay dự án đòi hỏi cấu trúc 'participate IN'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "After graduating with distinction, she decided to _______ a non-governmental organization dedicated to marine conservation.",
                            Options = [ "join", "attend", "participate", "enter in" ],
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "join",
                            Explanation = "Trở thành thành viên của một tổ chức (NGO) dùng động từ 'join': 'join a non-governmental organization'."
                        }
                    },
                    LearningTip = "Thần chú ghi nhớ: Trở thành hội viên → JOIN; Có mặt ở hội thảo/lớp học → ATTEND; Chủ động nhúng tay vào làm việc → PARTICIPATE IN!",
                    RelatedBandStructureCodes = new() { "TIP_JAP_01" }
                }
            }
        };
    }
}
