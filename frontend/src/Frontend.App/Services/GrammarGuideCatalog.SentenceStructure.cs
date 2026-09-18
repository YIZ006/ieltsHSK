using Frontend.App.Models;

namespace Frontend.App.Services;

public static partial class GrammarGuideCatalog
{
    private static GrammarGuideSectionDto BuildSentenceStructureSection()
    {
        return new GrammarGuideSectionDto
        {
            Key = "sentences",
            Overline = "MODULE 02 · CẤU TRÚC CÂU",
            Title = "Kiến Trúc Câu & Phân Tích Mệnh Đề",
            Description = "Làm chủ 4 mô hình câu cốt lõi giúp đa dạng hóa câu văn từ câu đơn, câu ghép FANBOYS, câu phức mệnh đề phụ đến câu hỏi chuyên sâu.",
            Icon = "bi-diagram-3",
            ColorTheme = "#4f46e5",
            Topics = new List<GrammarGuideTopicDto>
            {
                new GrammarGuideTopicDto
                {
                    Slug = "cau-truc-cau-co-ban",
                    Title = "Cấu Trúc Câu Cơ Bản",
                    EnglishTitle = "Basic Sentence Structures (SV, SVO, SVC, SVOO, SVOC)",
                    SectionKey = "sentences",
                    SectionTitle = "Cấu Trúc Câu",
                    ShortDescription = "5 mẫu câu nền tảng của tiếng Anh giúp xây dựng câu văn mạch lạc, tránh lỗi thiếu chủ vị (fragment) và câu cụt.",
                    Icon = "bi-diagram-3-fill",
                    IconBgColor = "#4338ca",
                    DifficultyLevel = "Cơ bản",
                    ReadTimeMinutes = 5,
                    OrderIndex = 1,
                    FormulaPreview = "S + V + (O / C / A)",
                    SkillTarget = "Nền tảng kiểm soát ngữ pháp IELTS Writing",
                    Tags = new() { "câu", "sentence structure", "svo", "svc", "svoo", "svoc", "cơ bản" },
                    ConceptExplanation = "Mỗi câu tiếng Anh hoàn chỉnh bắt buộc phải có ít nhất một chủ ngữ (Subject) và một vị ngữ (Predicate). Dựa vào bản chất của động từ (nội động từ hay ngoại động từ), tiếng Anh có 5 mẫu câu cơ bản tạo nên mọi cấu trúc câu phức tạp hơn.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Mẫu 1: SV & SVO", Formula = "S + V (nội động từ) | S + V + O (ngoại động từ)", ColorVariant = "blue", Breakdown = new() { "The sun shines. (SV)", "Governments implement policies. (SVO)" } },
                        new GrammarFormulaBlock { Type = "Mẫu 2: SVC (Linking Verbs)", Formula = "S + Linking Verb + Complement (Tính từ / Danh từ)", ColorVariant = "purple", Breakdown = new() { "The strategy proved effective. (SVC)" } },
                        new GrammarFormulaBlock { Type = "Mẫu 3: SVOO & SVOC", Formula = "S + V + Indirect Object + Direct Object | S + V + O + Complement", ColorVariant = "green", Breakdown = new() { "The university granted students scholarships. (SVOO)", "Technology makes education accessible. (SVOC)" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Lỗi thiếu thành phần (Sentence Fragment)", Example = "Although the economy grew. (SAI) → Although the economy grew, poverty persisted. (ĐÚNG)", Note = "Mệnh đề phụ thuộc không thể đứng một mình như câu hoàn chỉnh" },
                        new GrammarRuleTableItem { Rule = "Chủ ngữ giả (Dummy Subjects It/There)", Example = "It is essential to analyze the data / There are numerous factors...", Note = "Dùng để nhấn mạnh hoặc giới thiệu thông tin mới" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Diễn đạt nhận định học thuật chính xác và cô đọng",
                            Explanation = "Kiểm soát cấu trúc SVO giúp câu văn Writing Task 2 đanh thép và không bị rườm rà.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Rapid industrialization accelerates environmental degradation.", Vietnamese = "Công nghiệp hóa nhanh chóng thúc đẩy sự suy thoái môi trường." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Sử dụng mẫu SVOC để tăng tính biểu cảm học thuật",
                            Explanation = "Đặc biệt hữu ích khi dùng các động từ consider, make, deem, find.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Many policymakers consider vocational training an indispensable solution.", Vietnamese = "Nhiều nhà hoạch định chính sách coi đào tạo nghề là một giải pháp không thể thiếu." },
                            }
                        },
                    },
                    SignalWords = new() { "Subject", "Predicate", "Object", "Complement", "Adverbial" },
                    SignalWordPlacementRule = "Trật tự tự nhiên trong tiếng Anh là S-V-O. Tránh đưa trạng từ chen vào giữa Động từ và Tân ngữ trực tiếp.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Tách rời động từ và tân ngữ bằng trạng từ",
                            WrongExample = "He speaks fluently English.",
                            CorrectExample = "He speaks English fluently.",
                            Explanation = "Trong mẫu SVO, trạng từ chỉ cách thức đứng sau tân ngữ hoặc trước động từ, không xen vào giữa V và O."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Lỗi câu phẩy (Comma Splice)",
                            WrongExample = "The city expanded rapidly, new infrastructure was required.",
                            CorrectExample = "The city expanded rapidly; therefore, new infrastructure was required.",
                            Explanation = "Hai mệnh đề độc lập không thể nối với nhau chỉ bằng một dấu phẩy mà cần liên từ hoặc dấu chấm phẩy."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "Which of the following sentences represents the SVOC (Subject - Verb - Object - Complement) pattern?",
                            Options = new() { "Recent innovations made solar panels highly affordable.", "Solar panels generate clean electrical energy.", "The price of solar panels decreased sharply.", "Engineers provided the village with electricity." },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "Recent innovations made solar panels highly affordable.",
                            Explanation = "'Recent innovations' (S) + 'made' (V) + 'solar panels' (O) + 'highly affordable' (Object Complement)."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Identify the sentence that suffers from a 'Comma Splice' error:",
                            Options = new() { "Tuition fees increased, many students had to take part-time jobs.", "Although tuition fees increased, students continued their studies.", "Tuition fees increased; consequently, many students struggled.", "Because tuition fees increased, enrollments fell." },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "Tuition fees increased, many students had to take part-time jobs.",
                            Explanation = "Nối hai mệnh đề độc lập chỉ bằng dấu phẩy mà không có liên từ là lỗi Comma Splice, bị trừ nặng ở tiêu chí Grammatical Range."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Choose the correctly ordered sentence:",
                            Options = new() { "The committee analyzed thoroughly the experimental results.", "The committee thoroughly analyzed the experimental results.", "The committee analyzed the experimental thoroughly results.", "Thoroughly the committee analyzed the results experimental." },
                            CorrectOptionIndex = 1,
                            CorrectAnswer = "The committee thoroughly analyzed the experimental results.",
                            Explanation = "Trạng từ 'thoroughly' đứng trước động từ chính hoặc sau tân ngữ, không chen giữa động từ 'analyzed' và tân ngữ 'the experimental results'."
                        },
                    },
                    LearningTip = "Đừng cố viết câu quá dài nếu bạn chưa làm chủ được cấu trúc SVO. Viết câu đúng ngữ pháp và mạch lạc luôn được chấm điểm cao hơn câu dài nhưng vướng lỗi Comma Splice!",
                    RelatedBandStructureCodes = new() { "BAS_STRUC_01" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "cau-ghep",
                    Title = "Câu Ghép (Compound Sentences)",
                    EnglishTitle = "Compound Sentences with Coordinating Conjunctions (FANBOYS)",
                    SectionKey = "sentences",
                    SectionTitle = "Cấu Trúc Câu",
                    ShortDescription = "Kết nối hai mệnh đề độc lập bằng FANBOYS, dấu chấm phẩy và trạng từ liên kết.",
                    Icon = "bi-intersect",
                    IconBgColor = "#3730a3",
                    DifficultyLevel = "Cơ bản",
                    ReadTimeMinutes = 5,
                    OrderIndex = 2,
                    FormulaPreview = "Independent Clause, [FANBOYS] Independent Clause",
                    SkillTarget = "IELTS Writing Task 2 Band 6.5+",
                    Tags = new() { "câu", "sentence structure", "câu ghép", "compound sentence", "fanboys" },
                    ConceptExplanation = "Câu ghép (Compound Sentence) được tạo nên từ ít nhất hai mệnh đề độc lập có ý nghĩa ngang hàng, được liên kết chặt chẽ bằng liên từ đẳng lập (FANBOYS), dấu chấm phẩy, hoặc trạng từ liên kết (Conjunctive Adverbs).",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Công thức 1: FANBOYS", Formula = "Mệnh đề 1, [for / and / nor / but / or / yet / so] Mệnh đề 2", ColorVariant = "blue", Breakdown = new() { "Bắt buộc phải có dấu phẩy TRƯỚC liên từ FANBOYS" } },
                        new GrammarFormulaBlock { Type = "Công thức 2: Dấu chấm phẩy", Formula = "Mệnh đề 1; Mệnh đề 2", ColorVariant = "purple", Breakdown = new() { "Dùng khi hai mệnh đề có mối liên hệ ý nghĩa rất gần gũi" } },
                        new GrammarFormulaBlock { Type = "Công thức 3: Trạng từ liên kết", Formula = "Mệnh đề 1; however / furthermore / therefore, Mệnh đề 2", ColorVariant = "green", Breakdown = new() { "Chấm phẩy trước trạng từ, dấu phẩy sau trạng từ" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Ghi nhớ thần chú FANBOYS", Example = "For, And, Nor, But, Or, Yet, So", Note = "'Nor' yêu cầu đảo ngữ ở mệnh đề thứ hai (nor did they agree)" },
                        new GrammarRuleTableItem { Rule = "Dấu phẩy trong câu ghép", Example = "IELTS is rigorous, yet millions take it annually.", Note = "Nếu không có dấu phẩy trước liên từ đẳng lập, câu sẽ phạm lỗi Run-on" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Thể hiện mối quan hệ tương phản và chuyển ý trong Writing Task 2",
                            Explanation = "Dùng 'yet' hoặc 'but' để đối chiếu hai quan điểm đối nghịch.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Public transport is cost-effective, but its irregular schedule discourages daily commuters.", Vietnamese = "Giao thông công cộng tiết kiệm chi phí, nhưng lịch trình không ổn định làm nản lòng người đi làm hàng ngày." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Chỉ nguyên nhân kết quả với 'so' và 'for'",
                            Explanation = "Thiết lập mối quan hệ nhân quả một cách tự nhiên.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "The cost of living in urban areas soared, so many young professionals relocated to rural towns.", Vietnamese = "Chi phí sinh hoạt ở các đô thị tăng vọt, vì vậy nhiều chuyên gia trẻ đã chuyển đến các thị trấn nông thôn." },
                            }
                        },
                    },
                    SignalWords = new() { "for", "and", "nor", "but", "or", "yet", "so", "however", "therefore", "moreover" },
                    SignalWordPlacementRule = "Liên từ FANBOYS đứng ở giữa hai mệnh đề độc lập, luôn có dấu phẩy đi trước.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Bắt đầu câu văn học thuật bằng 'And', 'But' hoặc 'So'",
                            WrongExample = "But many opponents disagree.",
                            CorrectExample = "However, many opponents disagree.",
                            Explanation = "Trong văn viết học thuật IELTS Task 2, tránh đặt FANBOYS ở đầu câu; hãy thay bằng However, In contrast, Therefore."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Quên đảo ngữ sau liên từ 'nor'",
                            WrongExample = "He did not attend the conference, nor he sent a representative.",
                            CorrectExample = "He did not attend the conference, nor did he send a representative.",
                            Explanation = "Sau liên từ phủ định 'nor', mệnh đề bắt buộc phải đảo trợ động từ lên trước chủ ngữ."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "The university reduced tuition fees; _______, enrollment numbers rose by nearly 25%.",
                            Options = new() { "consequently", "but", "so", "because" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "consequently",
                            Explanation = "Sau dấu chấm phẩy ';' và trước dấu phẩy ',' phải là một trạng từ liên kết (Conjunctive Adverb) như 'consequently'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "The management did not approve the proposal, nor _______ any alternative solutions.",
                            Options = new() { "did they suggest", "they suggested", "they did suggest", "suggested they" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "did they suggest",
                            Explanation = "Sau liên từ phủ định 'nor', cấu trúc câu bắt buộc phải đảo trợ động từ lên trước: 'nor did they suggest'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Which sentence correctly punctuates the compound sentence?",
                            Options = new() { "Online learning offers great flexibility, yet some students struggle with self-discipline.", "Online learning offers great flexibility yet, some students struggle with self-discipline.", "Online learning offers great flexibility yet some students struggle with self-discipline.", "Online learning offers great flexibility; yet, some students struggle with self-discipline." },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "Online learning offers great flexibility, yet some students struggle with self-discipline.",
                            Explanation = "Quy tắc câu ghép chuẩn: Mệnh đề 1, + FANBOYS + Mệnh đề 2 (dấu phẩy đặt ngay trước liên từ 'yet')."
                        },
                    },
                    LearningTip = "Kết hợp hài hòa giữa câu ghép (FANBOYS) và câu phức (subordinating conjunctions) là tiêu chí cốt lõi để đạt 7.0+ phần Grammatical Range and Accuracy!",
                    RelatedBandStructureCodes = new() { "BAS_STRUC_02" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "cau-phuc",
                    Title = "Câu Phức (Complex Sentences)",
                    EnglishTitle = "Complex Sentences with Dependent Clauses",
                    SectionKey = "sentences",
                    SectionTitle = "Cấu Trúc Câu",
                    ShortDescription = "Mệnh đề chính kết hợp mệnh đề phụ thuộc (chỉ nguyên nhân, nhượng bộ, điều kiện, quan hệ).",
                    Icon = "bi-layers-fill",
                    IconBgColor = "#312e81",
                    DifficultyLevel = "Trung cấp",
                    ReadTimeMinutes = 6,
                    OrderIndex = 3,
                    FormulaPreview = "Dependent Clause, Independent Clause",
                    SkillTarget = "Tiêu chí bắt buộc cho Band 7.0+ Writing",
                    Tags = new() { "câu", "sentence structure", "câu phức", "complex sentence", "subordinating conjunctions" },
                    ConceptExplanation = "Câu phức (Complex Sentence) gồm một mệnh đề độc lập (Independent Clause) và ít nhất một mệnh đề phụ thuộc (Dependent Clause) được liên kết bởi các liên từ phụ thuộc như although, because, since, while, if, unless hoặc đại từ quan hệ.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Vị trí 1: Mệnh đề phụ đứng trước", Formula = "Subordinating Conjunction + Dependent Clause, Independent Clause", ColorVariant = "blue", Breakdown = new() { "Bắt buộc phải có dấu phẩy ngăn cách hai mệnh đề" } },
                        new GrammarFormulaBlock { Type = "Vị trí 2: Mệnh đề phụ đứng sau", Formula = "Independent Clause + Subordinating Conjunction + Dependent Clause", ColorVariant = "green", Breakdown = new() { "Thường KHÔNG cần dùng dấu phẩy" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Các nhóm liên từ phụ thuộc phổ biến", Example = "Nhượng bộ: although, even though, while / Nguyên nhân: because, since, as / Thời gian: when, while, as soon as", Note = "Không dùng 'Although' và 'But' trong cùng một câu" },
                        new GrammarRuleTableItem { Rule = "Mệnh đề quan hệ trong câu phức", Example = "Students who engage in extracurricular activities develop better soft skills.", Note = "Mệnh đề quan hệ xác định không dùng dấu phẩy" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Thể hiện tư duy phản biện (Critical Thinking) trong Writing Task 2",
                            Explanation = "Cấu trúc Although / While cho phép người viết thừa nhận mặt đối lập trước khi khẳng định luận điểm của mình.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "While remote work offers unparalleled convenience, it can inadvertently diminish team cohesion.", Vietnamese = "Mặc dù làm việc từ xa đem lại sự tiện lợi vô song, nó có thể vô tình làm suy giảm tính gắn kết đội ngũ." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Giải thích nguyên nhân chuyên sâu và thuyết phục",
                            Explanation = "Sử dụng 'Since' hoặc 'Given that' thay vì chỉ dùng 'Because'.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Since clean water is indispensable for public health, governments must allocate substantial funding.", Vietnamese = "Vì nước sạch là thiết yếu đối với sức khỏe cộng đồng, các chính phủ phải phân bổ nguồn ngân sách đáng kể." },
                            }
                        },
                    },
                    SignalWords = new() { "although", "even though", "while", "whereas", "since", "because", "unless", "provided that" },
                    SignalWordPlacementRule = "Khi mệnh đề phụ thuộc đứng đầu câu, cần đặt dấu phẩy ngay cuối mệnh đề đó trước khi bắt đầu mệnh đề chính.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Dùng cả 'Although' lẫn 'But' trong cùng một câu (Lỗi dịch word-by-word)",
                            WrongExample = "Although he worked diligently, but he failed the exam.",
                            CorrectExample = "Although he worked diligently, he failed the exam.",
                            Explanation = "Tiếng Anh chỉ chọn một trong hai: hoặc dùng Although, hoặc dùng But, không bao giờ dùng cả hai."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Nhầm lẫn giữa Despite/In spite of và Although",
                            WrongExample = "Despite he was exhausted, he completed the report.",
                            CorrectExample = "Despite being exhausted, he completed the report.",
                            Explanation = "Sau 'Despite' là Danh từ / V-ing; sau 'Although' mới là một mệnh đề hoàn chỉnh (S + V)."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "_______ the government implemented strict emission guidelines, industrial air pollution continued to rise.",
                            Options = new() { "Although", "Despite", "Because of", "However" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "Although",
                            Explanation = "Phía sau là một mệnh đề hoàn chỉnh (S + V) và mang ý tương phản nhượng bộ → dùng 'Although'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Which of the following sentences correctly avoids the dual conjunction error?",
                            Options = new() { "Although electric cars are eco-friendly, but they remain expensive.", "Even though electric cars are eco-friendly, they remain expensive.", "Despite electric cars are eco-friendly, they remain expensive.", "Although electric cars are eco-friendly, however they remain expensive." },
                            CorrectOptionIndex = 1,
                            CorrectAnswer = "Even though electric cars are eco-friendly, they remain expensive.",
                            Explanation = "Câu chuẩn: 'Even though + Clause, Clause' (không chèn thêm 'but' hay 'however' ở mệnh đề chính)."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Cities will face acute water shortages _______ substantial investments are made in desalination infrastructure.",
                            Options = new() { "unless", "if", "because", "provided that" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "unless",
                            Explanation = "'unless' = 'if not' (trừ khi / nếu không), thể hiện điều kiện phủ định logic: 'Các thành phố sẽ đối mặt thiếu nước nếu không đầu tư...'."
                        },
                    },
                    LearningTip = "Tiêu chí Grammatical Range ở Band 7.0 yêu cầu thí sinh 'uses a variety of complex structures'. Hãy đảm bảo bài viết Task 2 của bạn có ít nhất 40-50% số câu là câu phức chuẩn chỉnh!",
                    RelatedBandStructureCodes = new() { "ADV_STRUC_01" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "cach-dat-cau-hoi",
                    Title = "Cách Đặt Câu Hỏi (Forming Questions)",
                    EnglishTitle = "Forming Direct, Indirect & Embedded Questions",
                    SectionKey = "sentences",
                    SectionTitle = "Cấu Trúc Câu",
                    ShortDescription = "Quy tắc đảo trợ động từ, câu hỏi Yes/No, câu hỏi Wh-, câu hỏi gián tiếp và câu hỏi đuôi.",
                    Icon = "bi-question-circle-fill",
                    IconBgColor = "#1e1b4b",
                    DifficultyLevel = "Cơ bản",
                    ReadTimeMinutes = 5,
                    OrderIndex = 4,
                    FormulaPreview = "(Wh-) + Auxiliary + S + V?",
                    SkillTarget = "IELTS Speaking Part 1, 2 & 3",
                    Tags = new() { "câu", "sentence structure", "câu hỏi", "questions", "speaking" },
                    ConceptExplanation = "Đặt câu hỏi trong tiếng Anh tuân theo nguyên tắc đảo ngữ: trợ động từ (Auxiliary Verb / Modal Verb) hoặc động từ to be phải đứng trước chủ ngữ. Trong giao tiếp học thuật và IELTS Speaking, câu hỏi gián tiếp (Indirect Questions) mang tính lịch sự và tự nhiên hơn rất nhiều.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Câu hỏi Yes/No", Formula = "Auxiliary / Be / Modal + S + Main Verb + O?", ColorVariant = "blue", Breakdown = new() { "Do you agree?", "Is artificial intelligence beneficial?" } },
                        new GrammarFormulaBlock { Type = "Câu hỏi Wh- (Từ để hỏi)", Formula = "Wh-word + Auxiliary + S + Main Verb?", ColorVariant = "purple", Breakdown = new() { "What factors influence career choice?" } },
                        new GrammarFormulaBlock { Type = "Câu hỏi gián tiếp (Embedded)", Formula = "Could you tell me + Wh-word + S + V?", ColorVariant = "green", Breakdown = new() { "KHÔNG đảo ngữ trong mệnh đề gián tiếp" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Quy tắc câu hỏi chủ ngữ (Subject Questions)", Example = "Who invented the telephone? (KHÔNG dùng: Who did invent?)", Note = "Khi từ để hỏi chính là chủ ngữ thì không mượn trợ động từ do/does/did" },
                        new GrammarRuleTableItem { Rule = "Trật tự từ trong câu hỏi gián tiếp", Example = "Do you know where the library is? (KHÔNG dùng: where is the library)", Note = "Mệnh đề sau trở về trật tự câu khẳng định S + V" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Hỏi lại giám khảo một cách lịch sự trong IELTS Speaking",
                            Explanation = "Khi không nghe rõ hoặc muốn làm rõ câu hỏi mà không bị trừ điểm Fluency.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Could you please clarify what you mean by 'sustainable consumption'?", Vietnamese = "Thầy/Cô có thể vui lòng làm rõ ý nghĩa của cụm 'tiêu dùng bền vững' được không ạ?" },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Đặt câu hỏi tu từ (Rhetorical Questions) trong Speaking Part 3",
                            Explanation = "Dùng để dẫn dắt luận điểm và tạo nhịp điệu diễn thuyết cuốn hút.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Why do people still prioritize brand names over functionality? The answer lies in social status.", Vietnamese = "Tại sao con người vẫn ưu tiên thương hiệu hơn công năng? Câu trả lời nằm ở địa vị xã hội." },
                            }
                        },
                    },
                    SignalWords = new() { "Who", "What", "When", "Where", "Why", "How", "Could you tell me", "I wonder if" },
                    SignalWordPlacementRule = "Trợ động từ đứng trước chủ ngữ trong câu trực tiếp; đứng sau chủ ngữ trong câu gián tiếp.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Đảo ngữ sai trong câu hỏi gián tiếp",
                            WrongExample = "Can you explain why did the system crash?",
                            CorrectExample = "Can you explain why the system crashed?",
                            Explanation = "Trong câu hỏi gián tiếp, mệnh đề sau từ để hỏi quay về trật tự câu trần thuật (S + V)."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Quên trợ động từ trong câu hỏi trực tiếp thì quá khứ",
                            WrongExample = "What you did yesterday?",
                            CorrectExample = "What did you do yesterday?",
                            Explanation = "Câu hỏi thì quá khứ với động từ thường bắt buộc phải có trợ động từ 'did'."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "Which of the following indirect questions is grammatically ACCURATE?",
                            Options = new() { "Could you inform me when the flight will depart?", "Could you inform me when will the flight depart?", "Could you inform me when does the flight depart?", "Could you inform me when did the flight depart?" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "Could you inform me when the flight will depart?",
                            Explanation = "Trong câu hỏi gián tiếp bắt đầu bằng 'Could you inform me...', mệnh đề sau 'when' giữ nguyên trật tự khẳng định: S (the flight) + V (will depart)."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Identify the correct subject question for the underlined entity: '<u>Professor Davis</u> formulated this groundbreaking hypothesis.'",
                            Options = new() { "Who formulated this groundbreaking hypothesis?", "Who did formulate this groundbreaking hypothesis?", "Whom did formulate this groundbreaking hypothesis?", "Who was formulate this groundbreaking hypothesis?" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "Who formulated this groundbreaking hypothesis?",
                            Explanation = "Khi từ để hỏi 'Who' thay thế trực tiếp cho chủ ngữ (Professor Davis), không mượn trợ động từ 'did' mà chia trực tiếp V2: 'Who formulated...'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "In IELTS Speaking Part 3, how should you ask the examiner to explain an unfamiliar term?",
                            Options = new() { "I wonder if you could elaborate on what that phrase implies.", "What means that phrase?", "Explain me that phrase please.", "Why you say that phrase?" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "I wonder if you could elaborate on what that phrase implies.",
                            Explanation = "Cấu trúc gián tiếp trang trọng 'I wonder if you could elaborate on...' thể hiện phong thái giao tiếp học thuật Band 8.0+."
                        },
                    },
                    LearningTip = "Trong Speaking, thay vì hỏi cộc lốc 'What do you mean?', hãy sử dụng 'Would you mind rephrasing that question?' để giữ trọn điểm Pronunciation & Lexical!",
                    RelatedBandStructureCodes = new() { "BAS_STRUC_03" },
                },
            }
        };
    }
}
