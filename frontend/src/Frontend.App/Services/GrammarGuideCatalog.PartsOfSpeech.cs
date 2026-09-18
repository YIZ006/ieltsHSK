using Frontend.App.Models;

namespace Frontend.App.Services;

public static partial class GrammarGuideCatalog
{
    private static GrammarGuideSectionDto BuildPartsOfSpeechSection()
    {
        return new GrammarGuideSectionDto
        {
            Key = "parts-of-speech",
            Overline = "MODULE 03 · TỪ LOẠI CỐT LÕI",
            Title = "12 Chuyên Đề Từ Loại Cốt Lõi",
            Description = "Toàn diện 12 từ loại nền tảng từ danh từ, động từ, tính từ, trạng từ, mạo từ đến các kỹ thuật học thuật cao cấp như Danh từ hóa (Nominalization).",
            Icon = "bi-boxes",
            ColorTheme = "#ea580c",
            Topics = new List<GrammarGuideTopicDto>
            {
                new GrammarGuideTopicDto
                {
                    Slug = "danh-tu",
                    Title = "Danh Từ (Nouns)",
                    EnglishTitle = "Nouns (Countable, Uncountable & Nominalization)",
                    SectionKey = "parts-of-speech",
                    SectionTitle = "Từ Loại",
                    ShortDescription = "Phân loại danh từ đếm được, không đếm được và kỹ thuật danh từ hóa (Nominalization) nâng tầm Band 7.5+.",
                    Icon = "bi-tag-fill",
                    IconBgColor = "#d97706",
                    DifficultyLevel = "Cơ bản",
                    ReadTimeMinutes = 5,
                    OrderIndex = 1,
                    FormulaPreview = "Countable vs. Uncountable Nouns",
                    SkillTarget = "Tránh lỗi mạo từ & Danh từ hóa Task 2",
                    Tags = new() { "từ loại", "parts of speech", "danh từ", "nouns", "nominalization" },
                    ConceptExplanation = "Danh từ (Noun) là từ chỉ người, sự vật, hiện tượng, khái niệm hoặc địa điểm. Trong tiếng Anh học thuật, danh từ hóa (Nominalization) biến động từ hoặc tính từ thành danh từ để tăng tính khách quan, trang trọng cho bài luận.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Danh từ đếm được (Countable)", Formula = "a/an + N (số ít) | N-s/es (số nhiều)", ColorVariant = "blue", Breakdown = new() { "Bắt buộc phải có mạo từ hoặc từ hạn định đi kèm danh từ số ít" } },
                        new GrammarFormulaBlock { Type = "Danh từ không đếm được (Uncountable)", Formula = "Uncountable Noun + Động từ số ít", ColorVariant = "purple", Breakdown = new() { "KHÔNG đi với a/an, KHÔNG thêm s/es (information, advice, research)" } },
                        new GrammarFormulaBlock { Type = "Kỹ thuật Danh từ hóa (Nominalization)", Formula = "Động từ / Tính từ → Cụm danh từ học thuật", ColorVariant = "green", Breakdown = new() { "The economy grew → The economic growth (tăng tính học thuật)" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Danh từ không đếm được phổ biến trong IELTS", Example = "information, research, equipment, knowledge, accommodation, advice", Note = "Tuyệt đối không viết 'researches' hay 'equipments'" },
                        new GrammarRuleTableItem { Rule = "Cụm danh từ ghép (Compound Nouns)", Example = "climate change policies, water consumption rates", Note = "Danh từ đứng trước đóng vai trò bổ nghĩa và thường ở dạng số ít" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Danh từ hóa để biến câu văn thành chuẩn văn phong nghiên cứu",
                            Explanation = "Chuyển đổi câu hành động mang tính cá nhân thành câu nhận định khách quan.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Instead of saying 'People migrate to cities', write: 'Urban migration has escalated significantly.'", Vietnamese = "Thay vì viết 'Mọi người di cư đến thành phố', hãy viết: 'Sự di cư đô thị đã gia tăng đáng kể.'" },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Sử dụng chính xác lượng từ với danh từ không đếm được",
                            Explanation = "Dùng 'a piece of advice', 'an item of equipment' khi muốn đếm số lượng.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "The research provides compelling pieces of empirical evidence.", Vietnamese = "Nghiên cứu này cung cấp những bằng chứng thực nghiệm vô cùng thuyết phục." },
                            }
                        },
                    },
                    SignalWords = new() { "advice", "information", "research", "evidence", "equipment", "knowledge", "traffic", "pollution" },
                    SignalWordPlacementRule = "Danh từ làm chủ ngữ đứng đầu câu, làm tân ngữ đứng sau động từ hoặc giới từ.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Thêm s/es vào danh từ không đếm được (Lỗi mất điểm Task 1 & 2)",
                            WrongExample = "Scholars carried out many researches on this topic.",
                            CorrectExample = "Scholars carried out much research on this topic.",
                            Explanation = "'Research' là danh từ không đếm được, không thêm s và đi kèm với 'much' hoặc 'pieces of research'."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Để danh từ đếm được số ít đứng 'trơ trọi' không có từ hạn định",
                            WrongExample = "Student must submit essay on time.",
                            CorrectExample = "Students must submit essays on time. / A student must submit an essay on time.",
                            Explanation = "Danh từ đếm được số ít bắt buộc phải có mạo từ (a/an/the) hoặc từ sở hữu (my/their) đi kèm."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "Which of the following sentences uses uncountable nouns CORRECTLY in academic writing?",
                            Options = new() { "Recent scientific research has provided valuable evidence regarding climate patterns.", "Recent scientific researches have provided valuable evidences regarding climate patterns.", "Recent scientific research have provided valuable evidences regarding climate patterns.", "Recent scientific researches has provided valuable evidence regarding climate patterns." },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "Recent scientific research has provided valuable evidence regarding climate patterns.",
                            Explanation = "Cả 'research' và 'evidence' đều là danh từ không đếm được, không thêm đuôi -es và đi với động từ số ít 'has provided'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Apply nominalization to rewrite: 'Because people consume excessive fossil fuels, carbon emissions have risen.'",
                            Options = new() { "Excessive consumption of fossil fuels has driven the rise in carbon emissions.", "People consuming excessive fossil fuels rising carbon emissions.", "Fossil fuels consumed excessively so carbon emissions rise.", "Because excessive fossil fuels consumption, emissions rise." },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "Excessive consumption of fossil fuels has driven the rise in carbon emissions.",
                            Explanation = "Biến đổi động từ 'consume' thành cụm danh từ 'Excessive consumption of fossil fuels' là minh chứng cho kỹ thuật Nominalization Band 8.0."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "The university purchased high-tech _______ for the biochemistry laboratory.",
                            Options = new() { "equipment", "equipments", "an equipment", "equip" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "equipment",
                            Explanation = "'equipment' là danh từ không đếm được, không có dạng số nhiều 'equipments' và không dùng 'an equipment'."
                        },
                    },
                    LearningTip = "Nominalization (danh từ hóa) là một trong những đặc trưng lớn nhất phân biệt bài viết Band 6.0 và Band 8.0 trong IELTS Academic Writing!",
                    RelatedBandStructureCodes = new() { "BAS_POS_01" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "dai-tu",
                    Title = "Đại Từ (Pronouns)",
                    EnglishTitle = "Pronouns (Personal, Relative, Reflexive & Academic Referencing)",
                    SectionKey = "parts-of-speech",
                    SectionTitle = "Từ Loại",
                    ShortDescription = "Kiểm soát quy chiếu đại từ (Cohesion) giúp đoạn văn mạch lạc, tránh lặp từ và lỗi quy chiếu mơ hồ.",
                    Icon = "bi-people-fill",
                    IconBgColor = "#b45309",
                    DifficultyLevel = "Cơ bản",
                    ReadTimeMinutes = 5,
                    OrderIndex = 2,
                    FormulaPreview = "Personal, Possessive, Reflexive & Relative Pronouns",
                    SkillTarget = "Cohesion & Coherence Band 7.0+",
                    Tags = new() { "từ loại", "parts of speech", "đại từ", "pronouns", "referencing", "cohesion" },
                    ConceptExplanation = "Đại từ (Pronoun) là từ dùng để thay thế cho danh từ nhằm tránh việc lặp lại. Trong bài thi IELTS, kỹ năng quy chiếu đại từ (Referencing) là tiêu chí cốt lõi trong Coherence & Cohesion (Tính mạch lạc và liên kết).",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Đại từ nhân xưng & Sở hữu", Formula = "Subject (I, they) → Object (me, them) → Possessive (their, theirs)", ColorVariant = "blue", Breakdown = new() { "Hòa hợp về số và giống với danh từ mà nó thay thế" } },
                        new GrammarFormulaBlock { Type = "Đại từ quan hệ", Formula = "Who (người), Which (vật), That (thay thế who/which), Whose (sở hữu)", ColorVariant = "purple", Breakdown = new() { "Tạo mệnh đề quan hệ để kết nối câu phức" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Hạn chế ngôi thứ nhất trong Writing Task 2", Example = "Thay vì 'I believe that...', hãy dùng 'It is widely believed that...' hoặc 'From my perspective...'", Note = "Bài viết học thuật cần giọng văn khách quan" },
                        new GrammarRuleTableItem { Rule = "Lỗi quy chiếu mơ hồ (Ambiguous Reference)", Example = "When the government met the protesters, they announced the reform. (Họ ở đây là ai?)", Note = "Luôn đảm bảo đại từ thay thế cho danh từ rõ ràng" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Tạo tính liên kết mạch lạc giữa các câu văn trong đoạn",
                            Explanation = "Sử dụng this, these, such để quy chiếu lại ý tưởng ở câu trước.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Many young people leave rural areas for big cities. This demographic trend places immense pressure on metropolitan housing.", Vietnamese = "Nhiều người trẻ rời nông thôn lên các thành phố lớn. Xu hướng nhân khẩu học này đặt áp lực nặng nề lên nhà ở đô thị." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Sử dụng đại từ phản thân để nhấn mạnh chủ thể hành động",
                            Explanation = "Nhấn mạnh chính chủ thể tự mình thực hiện hành động.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Developing nations must themselves initiate sustainable conservation programs.", Vietnamese = "Chính các quốc gia đang phát triển phải tự mình khởi xướng các chương trình bảo tồn bền vững." },
                            }
                        },
                    },
                    SignalWords = new() { "it", "they", "them", "this", "these", "such", "which", "who", "whom", "whose" },
                    SignalWordPlacementRule = "Đại từ quy chiếu thường đứng đầu câu thứ hai để liên kết trực tiếp với thông tin câu thứ nhất.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Bất đồng nhất giữa Đại từ và Danh từ (Pronoun-Antecedent Disagreement)",
                            WrongExample = "Every student must submit their paper. (Văn viết trang trọng truyền thống)",
                            CorrectExample = "Every student must submit his or her paper. / All students must submit their papers.",
                            Explanation = "Trong văn viết học thuật khắt khe, chuyển danh từ về số nhiều 'All students' để dùng 'their' an toàn nhất."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Nhầm lẫn giữa 'its' (tính từ sở hữu) và 'it's' (viết tắt của it is)",
                            WrongExample = "The company increased it's market share in Asia.",
                            CorrectExample = "The company increased its market share in Asia.",
                            Explanation = "'its' không có dấu nháy là tính từ sở hữu; 'it's' là viết tắt của it is / it has."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "Select the sentence that demonstrates proper academic referencing:",
                            Options = new() { "Renewable energy production doubled last decade. This remarkable growth stimulated green investment.", "Renewable energy production doubled last decade. It stimulated it.", "Renewable energy production doubled last decade, which they stimulated green investment.", "Renewable energy production doubled last decade so this stimulated them." },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "Renewable energy production doubled last decade. This remarkable growth stimulated green investment.",
                            Explanation = "Sử dụng 'This + noun' (This remarkable growth) là kỹ thuật quy chiếu đại từ đỉnh cao của tiêu chí Cohesion Band 8.0."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "The multinational corporation expanded _______ international operations into Southeast Asia.",
                            Options = new() { "its", "it's", "their", "they're" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "its",
                            Explanation = "'The multinational corporation' là danh từ tập hợp số ít, tính từ sở hữu tương ứng là 'its' (không có dấu nháy đơn)."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Students _______ achieve high scores on standardized assessments often demonstrate disciplined study habits.",
                            Options = new() { "who", "whom", "which", "whose" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "who",
                            Explanation = "'Students' chỉ người đóng vai trò chủ ngữ của mệnh đề quan hệ nên dùng đại từ 'who'."
                        },
                    },
                    LearningTip = "Khi viết đoạn thân bài Task 2, dùng cụm 'This phenomenon', 'These measures' hoặc 'Such policies' ở đầu câu thứ hai giúp đoạn văn kết dính chặt chẽ mà không bị máy móc!",
                    RelatedBandStructureCodes = new() { "BAS_POS_02" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "dong-tu",
                    Title = "Động Từ (Verbs)",
                    EnglishTitle = "Verbs (Transitive, Intransitive & Academic Collocations)",
                    SectionKey = "parts-of-speech",
                    SectionTitle = "Từ Loại",
                    ShortDescription = "Ngoại động từ, nội động từ, linking verbs và kho động từ học thuật thay thế get/make/have.",
                    Icon = "bi-lightning-charge-fill",
                    IconBgColor = "#ea580c",
                    DifficultyLevel = "Cơ bản",
                    ReadTimeMinutes = 5,
                    OrderIndex = 3,
                    FormulaPreview = "Transitive vs. Intransitive Verbs",
                    SkillTarget = "Lexical Resource & Accuracy",
                    Tags = new() { "từ loại", "parts of speech", "động từ", "verbs", "academic verbs" },
                    ConceptExplanation = "Động từ (Verb) là trái tim của mọi câu tiếng Anh. Phân biệt chính xác giữa Ngoại động từ (cần tân ngữ đi kèm) và Nội động từ (không cần tân ngữ) giúp bạn không bao giờ sai cấu trúc câu hay câu bị động.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Ngoại động từ (Transitive)", Formula = "S + V + Direct Object", ColorVariant = "blue", Breakdown = new() { "The government enacted new legislation. (Có thể chuyển sang bị động)" } },
                        new GrammarFormulaBlock { Type = "Nội động từ (Intransitive)", Formula = "S + V (+ Adverbial)", ColorVariant = "purple", Breakdown = new() { "The unemployment rate decreased significantly. (KHÔNG BAO GIỜ có dạng bị động)" } },
                        new GrammarFormulaBlock { Type = "Liên động từ (Linking Verbs)", Formula = "S + Linking Verb + Adjective", ColorVariant = "green", Breakdown = new() { "remain, seem, appear, become, prove + Tính từ" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Nội động từ không có dạng bị động", Example = "occur, happen, exist, arrive, rise, fall", Note = "SAI: The accident was occurred / The price was fallen" },
                        new GrammarRuleTableItem { Rule = "Nâng cấp động từ cơ bản sang học thuật", Example = "get → obtain / acquire; make → generate / fabricate; help → facilitate", Note = "Tăng điểm Lexical Resource ngay lập tức" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Mô tả xu hướng trong Task 1 với nội động từ chuẩn xác",
                            Explanation = "Dùng các nội động từ như rise, plunge, soar, deteriorate kèm trạng từ.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "The production volume soared unexpectedly during the third quarter.", Vietnamese = "Sản lượng sản xuất tăng vọt bất ngờ trong suốt quý thứ ba." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Sử dụng Linking Verbs để đưa ra nhận định học thuật khách quan",
                            Explanation = "Dùng remain, appear, prove đi cùng tính từ thay cho to be đơn điệu.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Traditional retail models appear vulnerable in the face of burgeoning e-commerce.", Vietnamese = "Các mô hình bán lẻ truyền thống dường như dễ bị tổn thương trước sự bùng nổ của thương mại điện tử." },
                            }
                        },
                    },
                    SignalWords = new() { "acquire", "facilitate", "implement", "diminish", "fluctuate", "deteriorate", "remain", "prove" },
                    SignalWordPlacementRule = "Động từ đứng ngay sau chủ ngữ và hòa hợp về thì, thể, ngôi với chủ ngữ đó.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Chia bị động với nội động từ (Lỗi sai kinh điển Task 1)",
                            WrongExample = "The expenditure was increased sharply in 2010.",
                            CorrectExample = "The expenditure increased sharply in 2010.",
                            Explanation = "Động từ 'increase' khi mang nghĩa tự thân tăng lên là nội động từ, tuyệt đối không dùng bị động."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Dùng trạng từ sau Linking Verb thay vì tính từ",
                            WrongExample = "The strategy proved successfully.",
                            CorrectExample = "The strategy proved successful.",
                            Explanation = "Sau linking verb 'prove' / 'remain' bắt buộc dùng tính từ (successful) để bổ nghĩa cho chủ ngữ."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "Which of the following sentences correctly avoids the passive voice error with intransitive verbs?",
                            Options = new() { "A critical breakdown occurred during the initial phase of testing.", "A critical breakdown was occurred during the initial phase of testing.", "A critical breakdown has been occurred during the testing phase.", "A critical breakdown was happening by engineers." },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "A critical breakdown occurred during the initial phase of testing.",
                            Explanation = "'occur' là nội động từ thuần túy, không bao giờ có dạng bị động ('was occurred' là sai nghiêm trọng)."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Replace the informal verb in: 'The government should <u>make</u> the process easier for startups.'",
                            Options = new() { "facilitate", "get", "do", "perform" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "facilitate",
                            Explanation = "'facilitate' (tạo điều kiện thuận lợi, làm cho dễ dàng hơn) là động từ học thuật xuất sắc thay thế cho 'make easier'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Despite rigorous austerity measures, the financial situation remained _______.",
                            Options = new() { "precarious", "precariously", "precariousness", "in precariousness" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "precarious",
                            Explanation = "Sau linking verb 'remained', ta dùng tính từ 'precarious' (bấp bênh, nguy hiểm) để làm bổ ngữ cho chủ ngữ."
                        },
                    },
                    LearningTip = "Hạn chế tối đa các động từ đời thường như get, make, do, have trong Task 2. Hãy học theo cặp collocations học thuật như 'generate revenue', 'mitigate consequences'!",
                    RelatedBandStructureCodes = new() { "BAS_POS_03" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "dang-dong-tu",
                    Title = "Dạng Động Từ (Verb Forms)",
                    EnglishTitle = "Verb Forms: Gerunds (V-ing), Infinitives (To-V) & Participles",
                    SectionKey = "parts-of-speech",
                    SectionTitle = "Từ Loại",
                    ShortDescription = "Quy tắc chọn V-ing hay To-V sau động từ, tính từ và giới từ; phân từ hiện tại và quá khứ.",
                    Icon = "bi-code-square",
                    IconBgColor = "#c2410c",
                    DifficultyLevel = "Trung cấp",
                    ReadTimeMinutes = 6,
                    OrderIndex = 4,
                    FormulaPreview = "Gerund vs. Infinitive",
                    SkillTarget = "Grammar Accuracy & Sentence Variety",
                    Tags = new() { "từ loại", "parts of speech", "dạng động từ", "gerund", "infinitive", "to v", "v-ing" },
                    ConceptExplanation = "Trong tiếng Anh, một động từ đứng sau một động từ khác có thể ở dạng Danh động từ (Gerund - V-ing), Động từ nguyên mẫu có To (To-Infinitive), hoặc Động từ nguyên mẫu không To (Bare Infinitive). Nắm vững các động từ đặc biệt đổi nghĩa theo dạng đi kèm là tiêu chí kiểm tra phổ biến.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Nhóm đi với V-ing (Gerund)", Formula = "admit, avoid, consider, delay, enjoy, involve, suggest + V-ing", ColorVariant = "blue", Breakdown = new() { "Giới từ luôn đi cùng V-ing: interested in learning" } },
                        new GrammarFormulaBlock { Type = "Nhóm đi với To-V (Infinitive)", Formula = "agree, decide, hope, manage, promise, refuse, tend + To-V", ColorVariant = "purple", Breakdown = new() { "Tính từ đi với To-V: difficult to achieve" } },
                        new GrammarFormulaBlock { Type = "Nhóm đổi nghĩa theo dạng", Formula = "remember / forget / stop / try / regret + V-ing hoặc To-V", ColorVariant = "green", Breakdown = new() { "stop to smoke (dừng lại để hút) vs stop smoking (bỏ hút thuốc)" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Sau giới từ luôn là V-ing", Example = "by implementing, without compromising, of achieving", Note = "Kể cả cụm 'look forward to', 'in addition to' thì 'to' ở đây là giới từ nên vẫn + V-ing" },
                        new GrammarRuleTableItem { Rule = "V-ing đứng đầu câu làm chủ ngữ", Example = "Adopting renewable energy is imperative.", Note = "Động từ chia số ít (is)" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Dùng V-ing làm chủ ngữ câu văn học thuật",
                            Explanation = "Tạo câu mở đầu đoạn súc tích và đĩnh đạc.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Implementing strict environmental regulations requires substantial financial resources.", Vietnamese = "Việc thực thi các quy định môi trường nghiêm ngặt đòi hỏi nguồn lực tài chính đáng kể." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Chỉ mục đích hành động với To-Infinitive",
                            Explanation = "Thay thế cho cụm từ dài 'in order to' hoặc 'so as to'.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Governments must subsidize green technologies to accelerate decarbonization.", Vietnamese = "Chính phủ phải trợ cấp cho các công nghệ xanh nhằm đẩy nhanh quá trình phi carbon hóa." },
                            }
                        },
                    },
                    SignalWords = new() { "avoid", "consider", "suggest", "look forward to", "tend to", "aim to", "by", "without" },
                    SignalWordPlacementRule = "V-ing có thể đứng làm chủ ngữ, tân ngữ sau động từ hoặc tân ngữ của giới từ.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Dùng nguyên thể sau giới từ 'to' trong các cụm cố định",
                            WrongExample = "We look forward to receive your feedback.",
                            CorrectExample = "We look forward to receiving your feedback.",
                            Explanation = "Trong cụm 'look forward to', 'to' là giới từ, bắt buộc động từ đi sau phải ở dạng V-ing."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Nhầm lẫn nghĩa của Stop V-ing và Stop To-V",
                            WrongExample = "He stopped to smoke because of severe lung complications.",
                            CorrectExample = "He stopped smoking because of severe lung complications.",
                            Explanation = "'Stop smoking' là từ bỏ thói quen hút thuốc; 'stop to smoke' là tạm dừng việc đang làm để châm thuốc."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "The newly elected mayor committed himself to _______ public transportation infrastructure across the city.",
                            Options = new() { "improving", "improve", "improved", "improvement of" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "improving",
                            Explanation = "Cấu trúc 'commit oneself to + V-ing' (to ở đây là giới từ), do đó động từ theo sau bắt buộc là 'improving'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Choose the sentence where 'remember' signifies recollecting a past action:",
                            Options = new() { "I distinctly remember submitting the final dissertation before the deadline.", "Please remember to submit the final dissertation before the deadline.", "Did you remember to submit the final dissertation?", "Remember submit the final dissertation tomorrow." },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "I distinctly remember submitting the final dissertation before the deadline.",
                            Explanation = "'remember + V-ing' diễn tả việc nhớ lại một hành động đã làm trong quá khứ; 'remember + To-V' là nhớ để chuẩn bị làm một việc."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "_______ excessive amounts of sugar is correlated with various chronic metabolic disorders.",
                            Options = new() { "Consuming", "Consume", "Consumed", "To be consumed" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "Consuming",
                            Explanation = "Đứng đầu câu làm chủ ngữ của mệnh đề, ta dùng danh động từ 'Consuming' (đi với động từ số ít 'is')."
                        },
                    },
                    LearningTip = "Mở đầu câu bằng V-ing phrase (ví dụ: 'Fostering cultural diversity fosters tolerance...') là mẹo kinh điển giúp bạn ăn trọn điểm mở bài trong IELTS Task 2!",
                    RelatedBandStructureCodes = new() { "ADV_POS_01" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "tinh-tu",
                    Title = "Tính Từ (Adjectives)",
                    EnglishTitle = "Adjectives (Attributive, Predicative, -ed vs -ing)",
                    SectionKey = "parts-of-speech",
                    SectionTitle = "Từ Loại",
                    ShortDescription = "Bổ nghĩa cho danh từ, vị trí đứng trước danh từ hoặc sau linking verb; phân biệt tính từ -ed và -ing.",
                    Icon = "bi-palette-fill",
                    IconBgColor = "#0284c7",
                    DifficultyLevel = "Cơ bản",
                    ReadTimeMinutes = 5,
                    OrderIndex = 5,
                    FormulaPreview = "Adjectives & Participle Adjectives",
                    SkillTarget = "Lexical Resource & Academic Tone",
                    Tags = new() { "từ loại", "parts of speech", "tính từ", "adjectives", "academic vocabulary" },
                    ConceptExplanation = "Tính từ (Adjective) dùng để miêu tả đặc điểm, tính chất, tình trạng của danh từ hoặc đại từ. Trong văn phong IELTS, sử dụng các tính từ học thuật chính xác giúp tăng độ đanh thép và thuyết phục của lập luận.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Vị trí 1: Trước danh từ (Attributive)", Formula = "Adjective + Noun", ColorVariant = "blue", Breakdown = new() { "a viable alternative, substantial investments" } },
                        new GrammarFormulaBlock { Type = "Vị trí 2: Sau Linking Verb (Predicative)", Formula = "Subject + Linking Verb + Adjective", ColorVariant = "purple", Breakdown = new() { "The hypothesis appears flawed." } },
                        new GrammarFormulaBlock { Type = "Tính từ tận cùng -ed vs. -ing", Formula = "-ed (bị tác động / cảm xúc) vs -ing (bản chất / tạo ra tác động)", ColorVariant = "green", Breakdown = new() { "an interested student (học sinh có hứng thú) vs an interesting lecture (bài giảng thú vị)" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Phân biệt tính từ -ed và -ing", Example = "exhausted (cảm thấy kiệt sức) vs exhausting (gây kiệt sức)", Note = "Người cũng có thể dùng -ing nếu người đó gây ra cảm giác cho người khác" },
                        new GrammarRuleTableItem { Rule = "Nâng cấp tính từ 'good / bad / big'", Example = "good → beneficial / lucrative; bad → detrimental / adverse; big → monumental / substantial", Note = "Tránh dùng good/bad/big trong bài viết học thuật" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Miêu tả xu hướng biến động dữ liệu Task 1 bằng tính từ chuẩn xác",
                            Explanation = "Kết hợp tính từ chỉ biên độ với danh từ xu hướng.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "There was a dramatic surge in crude oil expenditure in 2012.", Vietnamese = "Đã có một sự tăng vọt đáng kể trong chi tiêu dầu thô vào năm 2012." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Đưa ra đánh giá quan điểm trong Task 2",
                            Explanation = "Dùng tính từ học thuật để thể hiện mức độ khả thi hoặc nguy hại.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Excessive reliance on automated algorithms can yield detrimental outcomes.", Vietnamese = "Sự phụ thuộc quá mức vào các thuật toán tự động có thể mang lại những hệ quả tai hại." },
                            }
                        },
                    },
                    SignalWords = new() { "substantial", "detrimental", "imperative", "viable", "lucrative", "flawed", "drastic", "profound" },
                    SignalWordPlacementRule = "Đứng trước danh từ hoặc đứng sau linking verbs (be, become, seem, appear, remain, look).",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Nhầm lẫn giữa tính từ -ed và -ing khi mô tả cảm xúc",
                            WrongExample = "The students were very boring during the long lecture.",
                            CorrectExample = "The students were very bored during the long lecture.",
                            Explanation = "Sinh viên cảm thấy buồn ngủ/chán nản nên phải dùng tính từ -ed ('bored'); dùng 'boring' mang nghĩa các bạn sinh viên là người tẻ nhạt."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Dùng tính từ bổ nghĩa cho động từ thường thay vì trạng từ",
                            WrongExample = "The economic indicators shifted dramatic.",
                            CorrectExample = "The economic indicators shifted dramatically.",
                            Explanation = "Bổ nghĩa cho động từ thường 'shifted' bắt buộc phải dùng trạng từ 'dramatically'."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "The recent archaeological discovery was truly _______; it stunned the entire scientific community.",
                            Options = new() { "astonishing", "astonished", "astonish", "astonishment" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "astonishing",
                            Explanation = "Chủ ngữ là sự việc mang tính chất gây ngạc nhiên cho người khác ('The archaeological discovery') nên dùng tính từ đuôi -ing: 'astonishing'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Select the sentence with the most academic lexical choice to replace 'bad effects':",
                            Options = new() { "Deforestation has detrimental consequences for biodiversity.", "Deforestation has very bad consequences for biodiversity.", "Deforestation has un-good consequences for biodiversity.", "Deforestation has terrible bad effects on nature." },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "Deforestation has detrimental consequences for biodiversity.",
                            Explanation = "'detrimental consequences' là collocation học thuật chuẩn xác Band 8.0 thay thế cho 'bad effects'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "The new environmental protection measures proved _______ in curbing vehicular emissions.",
                            Options = new() { "effective", "effectively", "effect", "effectiveness" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "effective",
                            Explanation = "Sau linking verb 'proved', ta cần tính từ 'effective' làm bổ ngữ cho chủ ngữ 'The new measures'."
                        },
                    },
                    LearningTip = "Hãy tạo một danh sách các tính từ học thuật thay thế: beneficial (tốt), detrimental (xấu), substantial (lớn), negligible (nhỏ). Sử dụng chúng đúng chỗ sẽ kéo điểm Lexical lên 7.5+!",
                    RelatedBandStructureCodes = new() { "BAS_POS_04" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "quy-tac-thu-tu-tinh-tu",
                    Title = "Thứ Tự Tính Từ (OSASCOMP)",
                    EnglishTitle = "Order of Adjectives (OSASCOMP Rule)",
                    SectionKey = "parts-of-speech",
                    SectionTitle = "Từ Loại",
                    ShortDescription = "Thần chú OSASCOMP sắp xếp nhiều tính từ cùng bổ nghĩa cho một danh từ một cách tự nhiên nhất.",
                    Icon = "bi-sort-alpha-down",
                    IconBgColor = "#0369a1",
                    DifficultyLevel = "Cơ bản",
                    ReadTimeMinutes = 5,
                    OrderIndex = 6,
                    FormulaPreview = "Opinion → Size → Age → Shape → Color → Origin → Material → Purpose",
                    SkillTarget = "Speaking Part 2 & Writing Accuracy",
                    Tags = new() { "từ loại", "parts of speech", "thứ tự tính từ", "osascomp", "adjective order" },
                    ConceptExplanation = "Khi có từ 2 tính từ trở lên cùng đứng trước bổ nghĩa cho một danh từ, người bản xứ sắp xếp chúng theo một quy luật cố định gọi là trật tự OSASCOMP: Opinion (Ý kiến) → Size (Kích thước) → Age (Tuổi thọ) → Shape (Hình dáng) → Color (Màu sắc) → Origin (Nguồn gốc) → Material (Chất liệu) → Purpose (Mục đích).",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Công thức thần chú OSASCOMP", Formula = "O (Opinion) - S (Size) - A (Age) - S (Shape) - C (Color) - O (Origin) - M (Material) - P (Purpose) + Noun", ColorVariant = "blue", Breakdown = new() { "Ví dụ: a beautiful (O) small (S) ancient (A) round (S) brown (C) Vietnamese (O) wooden (M) dining (P) table" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Dấu phẩy giữa các tính từ cùng loại", Example = "a large, spacious laboratory", Note = "Nếu các tính từ thuộc cùng một nhóm (Coordinate Adjectives) thì ngăn cách bằng dấu phẩy" },
                        new GrammarRuleTableItem { Rule = "Không lạm dụng quá 3 tính từ trước danh từ", Example = "Chỉ nên dùng tối đa 2-3 tính từ tự nhiên", Note = "Ghép quá nhiều tính từ sẽ khiến câu văn học thuật trở nên gượng gạo" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Miêu tả đồ vật hoặc địa điểm trong IELTS Speaking Part 2",
                            Explanation = "Giúp bài nói tự nhiên và trôi chảy như người bản xứ khi mô tả quà tặng, nhà cửa.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "My grandfather gifted me a valuable antique Swiss gold watch.", Vietnamese = "Ông tôi đã tặng tôi một chiếc đồng hồ vàng Thụy Sĩ cổ kính vô cùng giá trị." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Định danh các đối tượng nghiên cứu trong Writing Task 1",
                            Explanation = "Mô tả máy móc, vật liệu hoặc thiết bị thí nghiệm.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "The facility installed a modern automated solar energy system.", Vietnamese = "Cơ sở này đã lắp đặt một hệ thống năng lượng mặt trời tự động hóa hiện đại." },
                            }
                        },
                    },
                    SignalWords = new() { "Opinion", "Size", "Age", "Shape", "Color", "Origin", "Material", "Purpose" },
                    SignalWordPlacementRule = "Tính từ mang tính chủ quan (Opinion) luôn đi trước các tính từ mang tính khách quan và nguồn gốc/chất liệu.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Đưa nguồn gốc (Origin) hoặc chất liệu (Material) lên trước Opinion/Size",
                            WrongExample = "a wooden beautiful desk / a Japanese modern device",
                            CorrectExample = "a beautiful wooden desk / a modern Japanese device",
                            Explanation = "Tính từ ý kiến chủ quan (beautiful/modern) bắt buộc phải đứng trước tính từ chất liệu (wooden) và nguồn gốc (Japanese)."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Đặt tính từ mục đích (Purpose) xa danh từ chính",
                            WrongExample = "a sleeping comfortable bag",
                            CorrectExample = "a comfortable sleeping bag",
                            Explanation = "Tính từ chỉ mục đích (sleeping, dining, cooking) luôn đứng ngay sát cạnh danh từ mà nó bổ nghĩa."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "Which of the following phrases strictly follows the OSASCOMP order?",
                            Options = new() { "an elegant small antique Italian marble sculpture", "an Italian antique elegant small marble sculpture", "a marble elegant small antique Italian sculpture", "a small Italian elegant antique marble sculpture" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "an elegant small antique Italian marble sculpture",
                            Explanation = "Thứ tự chuẩn: elegant (Opinion) → small (Size) → antique (Age) → Italian (Origin) → marble (Material) + sculpture (Noun)."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Arrange the adjectives to describe a traditional gift: (wooden / lovely / handcrafted / Vietnamese)",
                            Options = new() { "a lovely handcrafted Vietnamese wooden souvenir", "a Vietnamese lovely wooden handcrafted souvenir", "a wooden handcrafted lovely Vietnamese souvenir", "a handcrafted wooden Vietnamese lovely souvenir" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "a lovely handcrafted Vietnamese wooden souvenir",
                            Explanation = "lovely (Opinion) → handcrafted (Age/Condition) → Vietnamese (Origin) → wooden (Material)."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Why is 'a leather brown vintage jacket' unnatural?",
                            Options = new() { "Because leather (Material) must come after brown (Color) and vintage (Age).", "Because vintage must be placed at the end.", "Because leather cannot describe a jacket.", "Because color must come before age." },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "Because leather (Material) must come after brown (Color) and vintage (Age).",
                            Explanation = "Trật tự đúng: vintage (Age) → brown (Color) → leather (Material) jacket."
                        },
                    },
                    LearningTip = "Ghi nhớ thần chú: 'Ông Sáu Ăn Súp Cua Ở Mọi Phố' (O-S-A-S-C-O-M-P) để không bao giờ quên trật tự tính từ khi bước vào phòng thi Speaking!",
                    RelatedBandStructureCodes = new() { "BAS_POS_05" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "trang-tu",
                    Title = "Trạng Từ (Adverbs)",
                    EnglishTitle = "Adverbs (Manner, Degree, Frequency & Conjunctive Adverbs)",
                    SectionKey = "parts-of-speech",
                    SectionTitle = "Từ Loại",
                    ShortDescription = "Bổ nghĩa cho động từ, tính từ và trạng từ khác; các trạng từ liên kết nối đoạn logic trong IELTS Writing.",
                    Icon = "bi-sliders",
                    IconBgColor = "#0284c7",
                    DifficultyLevel = "Cơ bản",
                    ReadTimeMinutes = 5,
                    OrderIndex = 7,
                    FormulaPreview = "Adverbs of Manner, Degree & Linking Adverbs",
                    SkillTarget = "IELTS Writing Task 1 & Coherence",
                    Tags = new() { "từ loại", "parts of speech", "trạng từ", "adverbs", "linking words" },
                    ConceptExplanation = "Trạng từ (Adverb) dùng để bổ nghĩa cho động từ, tính từ, hoặc một trạng từ khác, nhằm cung cấp thông tin về cách thức, mức độ, thời gian hoặc nơi chốn. Đặc biệt, Trạng từ liên kết (Conjunctive Adverbs) là công cụ kết nối đoạn văn học thuật hàng đầu.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Trạng từ chỉ cách thức", Formula = "Tính từ + đuôi -ly (quick → quickly, drastic → drastically)", ColorVariant = "blue", Breakdown = new() { "Bổ nghĩa cho hành động: The expenditure increased drastically." } },
                        new GrammarFormulaBlock { Type = "Trạng từ chỉ mức độ (Degree)", Formula = "Adverb + Adjective (substantially higher, marginally lower)", ColorVariant = "purple", Breakdown = new() { "Dùng so sánh số liệu Task 1: significantly, slightly, considerably" } },
                        new GrammarFormulaBlock { Type = "Trạng từ liên kết (Conjunctive Adverbs)", Formula = "; furthermore / however / nevertheless / consequently, ", ColorVariant = "green", Breakdown = new() { "Đứng đầu câu hoặc sau dấu chấm phẩy để chuyển ý đoạn văn" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Các tính từ tận cùng bằng -ly", Example = "friendly, lovely, lonely, costly, timely", Note = "Đây là tính từ, không phải trạng từ. Muốn dùng trạng từ: 'in a friendly manner'" },
                        new GrammarRuleTableItem { Rule = "Trạng từ bất quy tắc", Example = "good → well, fast → fast, hard → hard, early → early", Note = "'hardly' mang nghĩa là 'hầu như không', không phải nghĩa là 'chăm chỉ'" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Mô tả biên độ thay đổi số liệu trong IELTS Writing Task 1",
                            Explanation = "Kết hợp động từ xu hướng với trạng từ chỉ mức độ.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Car manufacturing in the UK dropped substantially between 2008 and 2012.", Vietnamese = "Hoạt động sản xuất ô tô ở Anh đã sụt giảm đáng kể trong khoảng thời gian từ năm 2008 đến 2012." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Chuyển ý logic và lập luận trong Writing Task 2",
                            Explanation = "Sử dụng các trạng từ liên kết trang trọng như Consequently, Conversely, Furthermore.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Conversely, opponents argue that excessive surveillance infringes upon personal liberties.", Vietnamese = "Ngược lại, những người phản đối lập luận rằng sự giám sát quá mức sẽ xâm phạm quyền tự do cá nhân." },
                            }
                        },
                    },
                    SignalWords = new() { "dramatically", "substantially", "significantly", "marginally", "conversely", "consequently", "furthermore", "nonetheless" },
                    SignalWordPlacementRule = "Trạng từ tần suất đứng trước V thường; trạng từ cách thức đứng sau tân ngữ; trạng từ liên kết đứng đầu câu kèm dấu phẩy.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Nhầm lẫn giữa 'hard' và 'hardly'",
                            WrongExample = "He worked hardly to achieve IELTS Band 8.0.",
                            CorrectExample = "He worked hard to achieve IELTS Band 8.0.",
                            Explanation = "'work hard' nghĩa là làm việc chăm chỉ, nỗ lực; 'hardly' là trạng từ mang nghĩa phủ định (hầu như không bao giờ)."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Dùng trạng từ bổ nghĩa cho danh từ",
                            WrongExample = "There was a dramatically decrease in exports.",
                            CorrectExample = "There was a dramatic decrease in exports.",
                            Explanation = "Trước danh từ 'decrease' bắt buộc phải dùng tính từ 'dramatic', không dùng trạng từ."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "Between 2015 and 2020, investment in solar power increased _______ across European nations.",
                            Options = new() { "exponentially", "exponential", "exponentiality", "in exponential" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "exponentially",
                            Explanation = "Bổ nghĩa cho động từ 'increased' ta dùng trạng từ chỉ mức độ 'exponentially' (theo cấp số nhân)."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Choose the sentence with the correct usage of 'hardly':",
                            Options = new() { "The applicant hardly had any relevant experience in financial auditing.", "The applicant worked hardly during the audit inspection.", "The team prepared hardly for the final presentation.", "She studied hardly all night for the test." },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "The applicant hardly had any relevant experience in financial auditing.",
                            Explanation = "'hardly' mang nghĩa 'hầu như không có' kinh nghiệm. Các câu còn lại muốn nói chăm chỉ phải dùng 'hard'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "The experimental trial showed negative outcomes; _______, researchers decided to refine the protocol.",
                            Options = new() { "consequently", "although", "despite", "whereas" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "consequently",
                            Explanation = "Đứng sau dấu chấm phẩy ';' và trước dấu phẩy ',' để chỉ mối quan hệ nhân quả là trạng từ liên kết 'consequently'."
                        },
                    },
                    LearningTip = "Trong Task 1, hãy xen kẽ giữa cấu trúc (V + Adv) như 'rose dramatically' và cấu trúc (Adj + N) như 'saw a dramatic rise' để tối đa hóa điểm biến hóa ngữ pháp!",
                    RelatedBandStructureCodes = new() { "BAS_POS_06" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "gioi-tu",
                    Title = "Giới Từ (Prepositions)",
                    EnglishTitle = "Prepositions (Time, Place, Movement & Dependent Prepositions)",
                    SectionKey = "parts-of-speech",
                    SectionTitle = "Từ Loại",
                    ShortDescription = "Giới từ chỉ thời gian, nơi chốn, phương hướng và các cặp giới từ phụ thuộc (Dependent Prepositions) học thuật.",
                    Icon = "bi-signpost-2-fill",
                    IconBgColor = "#059669",
                    DifficultyLevel = "Cơ bản",
                    ReadTimeMinutes = 5,
                    OrderIndex = 8,
                    FormulaPreview = "Prepositions & Dependent Prepositions",
                    SkillTarget = "Grammar Accuracy & Collocations",
                    Tags = new() { "từ loại", "parts of speech", "giới từ", "prepositions", "dependent prepositions" },
                    ConceptExplanation = "Giới từ (Preposition) là từ nối thiết lập mối quan hệ về không gian, thời gian, phương hướng hoặc logic giữa danh từ/đại từ với các thành phần khác trong câu. Trong IELTS, giới từ phụ thuộc (đi kèm cố định với động từ, danh từ hoặc tính từ) là nơi thí sinh dễ bị mất điểm lặt vặt nhất.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Giới từ đi với Tính từ", Formula = "Adjective + Preposition (responsible for, capable of, prone to)", ColorVariant = "blue", Breakdown = new() { "Sau giới từ luôn là Danh từ hoặc V-ing" } },
                        new GrammarFormulaBlock { Type = "Giới từ đi với Động từ", Formula = "Verb + Preposition (contribute to, rely on, adhere to)", ColorVariant = "purple", Breakdown = new() { "Thường đi kèm một giới từ cố định không thể thay đổi" } },
                        new GrammarFormulaBlock { Type = "Giới từ đi với Danh từ", Formula = "Noun + Preposition (an increase in, an impact on, a solution to)", ColorVariant = "green", Breakdown = new() { "Dùng cực nhiều trong miêu tả biểu đồ IELTS Task 1" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Cụm cố định trong IELTS Task 1", Example = "an increase OF 15% (chỉ khoảng chênh lệch) vs a rise IN consumption (chỉ đối tượng tăng)", Note = "Khác biệt rất quan trọng giữa OF và IN" },
                        new GrammarRuleTableItem { Rule = "Cụm 'at the expense of' và 'in terms of'", Example = "Economic growth occurred at the expense of environmental quality.", Note = "Cụm giới từ học thuật cực kỳ ăn điểm trong Task 2" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Sử dụng chính xác giới từ phụ thuộc trong bài luận IELTS Task 2",
                            Explanation = "Đảm bảo tính chuẩn xác cho các liên kết động từ - giới từ.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Fossil fuel emissions directly contribute to catastrophic climate change.", Vietnamese = "Khí thải từ nhiên liệu hóa thạch trực tiếp góp phần gây ra biến đổi khí hậu thảm khốc." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Mô tả số liệu chính xác với giới từ trong Task 1",
                            Explanation = "Sử dụng 'from... to...', 'stood at', 'peaked at'.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "The proportion of smartphone users peaked at 85% in 2019.", Vietnamese = "Tỷ lệ người dùng điện thoại thông minh đã đạt đỉnh ở mức 85% vào năm 2019." },
                            }
                        },
                    },
                    SignalWords = new() { "contribute to", "capable of", "solution to", "impact on", "increase in", "at the expense of", "in terms of" },
                    SignalWordPlacementRule = "Giới từ đứng trước cụm danh từ hoặc danh động từ (V-ing).",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Dùng sai giới từ đi kèm 'solution' hoặc 'approach'",
                            WrongExample = "a viable solution for this issue",
                            CorrectExample = "a viable solution to this issue",
                            Explanation = "Danh từ 'solution', 'key', 'answer', 'approach' đi kèm với giới từ 'to', không đi với 'for'."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Thêm giới từ sau động từ 'discuss' hoặc 'reach'",
                            WrongExample = "The essay will discuss about two opposing views.",
                            CorrectExample = "The essay will discuss two opposing views.",
                            Explanation = "'discuss' là ngoại động từ trực tiếp, theo sau là tân ngữ trực tiếp không có giới từ 'about'."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "The exponential surge in urban population has exerted immense pressure _______ municipal waste management systems.",
                            Options = new() { "on", "in", "to", "with" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "on",
                            Explanation = "Cụm collocation chuẩn: 'exert pressure ON something' (gây áp lực lên điều gì)."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Which sentence contains the CORRECT dependent preposition for 'solution'?",
                            Options = new() { "Investing in public transit is an effective solution to traffic congestion.", "Investing in public transit is an effective solution for traffic congestion.", "Investing in public transit is an effective solution of traffic congestion.", "Investing in public transit is an effective solution with traffic congestion." },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "Investing in public transit is an effective solution to traffic congestion.",
                            Explanation = "Danh từ 'solution' đi với giới từ 'to': 'a solution to a problem'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "The sales figures for electric cars stood _______ 45,000 units before rising sharply.",
                            Options = new() { "at", "in", "on", "to" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "at",
                            Explanation = "Trong IELTS Writing Task 1, mô tả mốc giá trị số liệu tại một thời điểm ta dùng 'stand at': 'stood at 45,000 units'."
                        },
                    },
                    LearningTip = "Đừng bao giờ viết 'discuss about' trong phần mở bài Task 2! Hãy viết 'This essay discusses both perspectives...' để tránh mất điểm lỗi ngữ pháp cơ bản!",
                    RelatedBandStructureCodes = new() { "BAS_POS_07" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "cach-dung-gioi-tu-in-on-at",
                    Title = "Giới Từ: In, On, At",
                    EnglishTitle = "Prepositions of Time & Place: In, On, At (Pyramid Rule)",
                    SectionKey = "parts-of-speech",
                    SectionTitle = "Từ Loại",
                    ShortDescription = "Mô hình kim tự tháp đảo ngược giúp phân biệt In (rộng), On (trung bình) và At (cụ thể chính xác) cho cả thời gian và địa điểm.",
                    Icon = "bi-triangle-half",
                    IconBgColor = "#047857",
                    DifficultyLevel = "Cơ bản",
                    ReadTimeMinutes = 5,
                    OrderIndex = 9,
                    FormulaPreview = "In (General) → On (Specific) → At (Exact)",
                    SkillTarget = "Speaking & Writing Task 1 Dates",
                    Tags = new() { "từ loại", "parts of speech", "in on at", "giới từ thời gian nơi chốn", "kim tự tháp" },
                    ConceptExplanation = "Quy tắc kim tự tháp đảo ngược (In-On-At) là phương pháp trực quan nhất: 'In' nằm ở đáy tháp (rộng nhất, chung nhất), 'On' nằm ở tầng giữa (cụ thể hơn), và 'At' nằm ở đỉnh tháp nhọn (chính xác và hẹp nhất), áp dụng cho cả trục Thời gian lẫn Không gian.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Thời gian (Time Pyramid)", Formula = "IN (thế kỷ, năm, mùa, tháng) → ON (thứ trong tuần, ngày tháng) → AT (giờ giấc cụ thể, thời điểm)", ColorVariant = "blue", Breakdown = new() { "in 2020, in summer → on Monday, on July 15th → at 7 PM, at midnight" } },
                        new GrammarFormulaBlock { Type = "Địa điểm (Place Pyramid)", Formula = "IN (quốc gia, thành phố, không gian kín) → ON (bề mặt, tên đường) → AT (địa chỉ số nhà, điểm dừng)", ColorVariant = "purple", Breakdown = new() { "in Vietnam, in Hanoi → on Oxford Street, on the wall → at 221B Baker St, at the bus stop" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Các trường hợp đặc biệt với IN và AT", Example = "in the morning/afternoon/evening NHƯNG at night, at noon, at the weekend", Note = "Người Mỹ có thể dùng 'on the weekend'" },
                        new GrammarRuleTableItem { Rule = "Phương tiện giao thông: In vs On", Example = "get IN a car / taxi (phương tiện nhỏ, phải khom lưng) vs get ON a bus / train / plane (phương tiện lớn, có thể đứng thẳng)", Note = "Quy tắc chuyển động hình thể" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Ghi mốc thời gian chuẩn xác trong IELTS Writing Task 1",
                            Explanation = "Đảm bảo sự chuẩn xác tuyệt đối khi dẫn chứng thời gian từ biểu đồ.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "In 2015, consumer spending on apparel peaked, while on weekends, recreational spending rose.", Vietnamese = "Vào năm 2015, chi tiêu người dùng cho may mặc đạt đỉnh, trong khi vào các ngày cuối tuần, chi tiêu giải trí lại tăng." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Mô tả nơi chốn trong IELTS Speaking Part 1",
                            Explanation = "Nói về quê hương, nơi làm việc hoặc địa điểm yêu thích.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "I currently reside in Da Nang, located on the central coast of Vietnam.", Vietnamese = "Tôi hiện đang sinh sống tại Đà Nẵng, tọa lạc ở bờ biển miền Trung Việt Nam." },
                            }
                        },
                    },
                    SignalWords = new() { "in the 21st century", "in 2025", "on weekdays", "on October 10th", "at 8:30 AM", "at the intersection" },
                    SignalWordPlacementRule = "Cụm giới từ thời gian thường đặt ở đầu câu kèm dấu phẩy hoặc ở cuối câu.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Dùng 'in' cho ngày tháng có đầy đủ ngày và tháng",
                            WrongExample = "The treaty was ratified in May 12th, 2018.",
                            CorrectExample = "The treaty was ratified on May 12th, 2018.",
                            Explanation = "Khi có ngày cụ thể ('May 12th'), giới từ bắt buộc phải là 'ON', không được dùng 'in'."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Dùng 'at the morning' thay vì 'in the morning'",
                            WrongExample = "Interviews are scheduled at the morning.",
                            CorrectExample = "Interviews are scheduled in the morning.",
                            Explanation = "Các buổi trong ngày dùng 'in': in the morning, in the afternoon, in the evening (chỉ có 'at night' là ngoại lệ)."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "The international trade accord was signed _______ November 15th, 2022 _______ Geneva.",
                            Options = new() { "on / in", "in / in", "at / on", "on / at" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "on / in",
                            Explanation = "Có ngày cụ thể 'November 15th' dùng 'on'; thành phố lớn 'Geneva' dùng 'in'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Which of the following phrases correctly uses prepositions of time?",
                            Options = new() { "The symposium commences at 9:00 AM on Wednesday in the autumn.", "The symposium commences in 9:00 AM at Wednesday on the autumn.", "The symposium commences on 9:00 AM in Wednesday at the autumn.", "The symposium commences at 9:00 AM in Wednesday on the autumn." },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "The symposium commences at 9:00 AM on Wednesday in the autumn.",
                            Explanation = "at + giờ cụ thể (at 9:00 AM); on + thứ (on Wednesday); in + mùa (in the autumn)."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "The survey was conducted _______ the campus library _______ 123 University Boulevard.",
                            Options = new() { "at / at", "in / on", "on / in", "at / in" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "at / at",
                            Explanation = "Tại một địa điểm chức năng cụ thể ('at the campus library') và địa chỉ có số nhà cụ thể ('at 123 University Boulevard') đều dùng 'at'."
                        },
                    },
                    LearningTip = "Hãy nhớ: IN là khối lớn (năm/tháng/nước/thành phố), ON là mặt phẳng và ngày (ngày/thứ/đường), AT là điểm ngắm bắn (giờ/số nhà/tọa độ)! Áp dụng mẹo này sẽ không bao giờ sai.",
                    RelatedBandStructureCodes = new() { "BAS_POS_08" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "mao-tu",
                    Title = "Mạo Từ (Articles: A, An, The)",
                    EnglishTitle = "Articles (Definite 'The', Indefinite 'A/An' & Zero Article)",
                    SectionKey = "parts-of-speech",
                    SectionTitle = "Từ Loại",
                    ShortDescription = "Quy tắc kiểm soát mạo từ xác định The, không xác định A/An và các trường hợp không dùng mạo từ (Zero Article).",
                    Icon = "bi-bookmark-check-fill",
                    IconBgColor = "#065f46",
                    DifficultyLevel = "Trung cấp",
                    ReadTimeMinutes = 6,
                    OrderIndex = 10,
                    FormulaPreview = "A / An / The / Ø (Zero Article)",
                    SkillTarget = "Grammar Band 8.0 Error-free Requirement",
                    Tags = new() { "từ loại", "parts of speech", "mạo từ", "articles", "a an the", "zero article" },
                    ConceptExplanation = "Mạo từ (Article) đứng trước danh từ để cho biết đối tượng được nhắc đến là xác định (Definite - The) hay không xác định (Indefinite - A/An). Trong tiêu chí chấm Grammatical Range and Accuracy ở Band 8.0, giám khảo đặc biệt chú ý xem thí sinh có kiểm soát hoàn hảo việc dùng 'The' và 'Zero Article' hay không.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Mạo từ không xác định (A / An)", Formula = "a + phụ âm / an + nguyên âm (u, e, o, a, i)", ColorVariant = "blue", Breakdown = new() { "Dùng cho danh từ số ít đếm được nhắc đến lần đầu tiên (a university, an hour)" } },
                        new GrammarFormulaBlock { Type = "Mạo từ xác định (The)", Formula = "the + N (đã biết rõ hoặc là duy nhất)", ColorVariant = "purple", Breakdown = new() { "the environment, the internet, the government, the Sun" } },
                        new GrammarFormulaBlock { Type = "Không dùng mạo từ (Zero Article - Ø)", Formula = "Ø + Danh từ số nhiều / Danh từ không đếm được mang nghĩa chung chung", ColorVariant = "green", Breakdown = new() { "Ø Education is essential / Ø Students should read books" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Phiên âm quyết định A hay An (không phải chữ cái)", Example = "an hour (/aʊər/ - âm câm) vs a university (/ˌjuːnɪˈvɜːsəti/ - bán phụ âm /j/)", Note = "Căn cứ vào âm phát ra đầu tiên" },
                        new GrammarRuleTableItem { Rule = "The với tên quốc gia", Example = "the United Kingdom, the United States, the Philippines (quốc gia số nhiều hoặc liên bang) vs Vietnam, France, Japan (không có the)", Note = "Rất phổ biến trong bài thi Writing" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Nói về các khái niệm phổ quát trong Writing Task 2 (Zero Article)",
                            Explanation = "Không dùng mạo từ khi bàn luận về danh từ mang tính đại diện toàn thể.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Technology plays an indispensable role in modern society. (KHÔNG dùng: The technology)", Vietnamese = "Công nghệ đóng một vai trò không thể thiếu trong xã hội hiện đại." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Sử dụng 'The' với các thực thể độc nhất và nhóm đối tượng",
                            Explanation = "Dùng với môi trường, chính phủ, người nghèo (the poor), người già (the elderly).",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "The government must implement measures to protect the environment.", Vietnamese = "Chính phủ phải thực hiện các biện pháp để bảo vệ môi trường." },
                            }
                        },
                    },
                    SignalWords = new() { "the environment", "the government", "the elderly", "the Internet", "an hour", "a university" },
                    SignalWordPlacementRule = "Mạo từ đứng trước tính từ và danh từ trong cụm danh từ (The + Adj + N).",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Thêm 'The' vào trước danh từ mang nghĩa khái quát",
                            WrongExample = "The children need love and care from parents.",
                            CorrectExample = "Children need love and care from parents.",
                            Explanation = "Nói về trẻ em nói chung trên toàn thế giới, không dùng 'The' (Zero Article)."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Quên 'The' trước 'environment' hoặc 'internet'",
                            WrongExample = "Pollution severely damages environment.",
                            CorrectExample = "Pollution severely damages the environment.",
                            Explanation = "Các thực thể độc nhất vô nhị ('the environment', 'the internet') luôn bắt buộc phải có mạo từ 'The'."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "Which of the following sentences correctly utilizes articles in an academic context?",
                            Options = new() { "Education is essential for combating poverty in developing nations.", "The education is essential for combating the poverty in developing nations.", "An education is essential for combating a poverty in developing nations.", "The education is essential for combating poverty in the developing nations." },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "Education is essential for combating poverty in developing nations.",
                            Explanation = "'Education' và 'poverty' ở đây được dùng với nghĩa khái quát toàn thể, do đó áp dụng quy tắc Zero Article (không dùng mạo từ)."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Researchers from _______ University of Cambridge collaborated with scientists in _______ Netherlands.",
                            Options = new() { "the / the", "a / the", "the / Ø", "Ø / the" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "the / the",
                            Explanation = "Cụm danh từ 'the University of...' bắt buộc có 'the'; 'the Netherlands' là quốc gia có tên ở dạng số nhiều nên luôn đi cùng 'the'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "It takes _______ hour of daily physical exercise to maintain cardiovascular health.",
                            Options = new() { "an", "a", "the", "Ø" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "an",
                            Explanation = "Từ 'hour' bắt đầu bằng âm câm /aʊər/ (nguyên âm), nên dùng mạo từ 'an'."
                        },
                    },
                    LearningTip = "Mẹo vàng Writing Task 2: 'The environment', 'The government', 'The economy' luôn có THE; nhưng 'Pollution', 'Technology', 'Education', 'Society' khi nói chung thì KHÔNG có THE!",
                    RelatedBandStructureCodes = new() { "BAS_POS_09" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "tu-han-dinh",
                    Title = "Từ Hạn Định (Determiners)",
                    EnglishTitle = "Determiners (Quantifiers: Much/Many, Few/Little, Each/Every)",
                    SectionKey = "parts-of-speech",
                    SectionTitle = "Từ Loại",
                    ShortDescription = "Từ chỉ số lượng phân biệt giữa danh từ đếm được và không đếm được; sắc thái khẳng định/phủ định của Few vs A few.",
                    Icon = "bi-pie-chart-fill",
                    IconBgColor = "#0f766e",
                    DifficultyLevel = "Cơ bản",
                    ReadTimeMinutes = 5,
                    OrderIndex = 11,
                    FormulaPreview = "Much / Many / Little / Few / Each / Every",
                    SkillTarget = "Data Description Task 1 & Accuracy",
                    Tags = new() { "từ loại", "parts of speech", "từ hạn định", "determiners", "quantifiers" },
                    ConceptExplanation = "Từ hạn định (Determiners) là từ đứng trước danh từ để chỉ định hoặc định lượng đối tượng được nói đến. Phân biệt chính xác giữa Much/Many, Little/Few, và đặc biệt là sắc thái nghĩa giữa A few (một vài - tích cực) và Few (hầu như không có - tiêu cực) là yếu tố quyết định độ chính xác ngữ nghĩa.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Danh từ đếm được số nhiều", Formula = "Many / A few / Few / Several + Plural Countable Noun", ColorVariant = "blue", Breakdown = new() { "Few students (hầu như không có ai - tiêu cực) vs A few students (có một vài người - tích cực)" } },
                        new GrammarFormulaBlock { Type = "Danh từ không đếm được", Formula = "Much / A little / Little / A great deal of + Uncountable Noun", ColorVariant = "purple", Breakdown = new() { "Little hope (gần như vô vọng) vs A little hope (vẫn còn chút hy vọng)" } },
                        new GrammarFormulaBlock { Type = "Each và Every", Formula = "Each / Every + Singular Countable Noun + Động từ số ít", ColorVariant = "green", Breakdown = new() { "Each participant was assigned a mentor." } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Sắc thái Few/Little vs A Few/A Little", Example = "Few scholars agree (gần như không ai đồng tình) vs A few scholars agree (có một số học giả đồng tình)", Note = "Không có 'a' mang sắc thái gần như bằng không (mang tính phủ định)" },
                        new GrammarRuleTableItem { Rule = "Từ hạn định dùng cho cả hai loại danh từ", Example = "all, some, any, most, a lot of, plenty of", Note = "Đi với danh từ không đếm được thì động từ chia số ít; đi với danh từ số nhiều thì động từ chia số nhiều" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Mô tả tỷ trọng và phân bố dữ liệu trong IELTS Writing Task 1",
                            Explanation = "Sử dụng most, the majority of, a significant amount of.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "A substantial amount of funding was allocated to biomedical research.", Vietnamese = "Một lượng ngân sách đáng kể đã được phân bổ cho nghiên cứu y sinh." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Nhấn mạnh tính hiếm hoi hoặc tiêu cực trong lập luận Task 2",
                            Explanation = "Dùng 'few' hoặc 'little' để chỉ sự thiếu thốn hoặc hạn chế.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "There is little empirical evidence supporting this contentious claim.", Vietnamese = "Hầu như không có bằng chứng thực nghiệm nào ủng hộ tuyên bố gây tranh cãi này." },
                            }
                        },
                    },
                    SignalWords = new() { "many", "much", "few", "a few", "little", "a little", "each", "every", "the majority of", "a vast amount of" },
                    SignalWordPlacementRule = "Đứng trước danh từ hoặc cụm danh từ.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Dùng 'amount of' với danh từ đếm được số nhiều",
                            WrongExample = "A large amount of people attended the conference.",
                            CorrectExample = "A large number of people attended the conference.",
                            Explanation = "'amount of' chỉ đi với danh từ không đếm được; danh từ đếm được số nhiều (people, students) phải dùng 'number of'."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Chia động từ số nhiều sau 'Each' hoặc 'Every'",
                            WrongExample = "Every participant were given a certificate.",
                            CorrectExample = "Every participant was given a certificate.",
                            Explanation = "Sau 'Each' và 'Every', danh từ luôn ở số ít và động từ bắt buộc phải chia ở số ít (was given)."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "Due to strict censorship, _______ independent journalists were allowed to attend the proceedings.",
                            Options = new() { "few", "a few", "little", "a little" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "few",
                            Explanation = "'journalists' là danh từ đếm được số nhiều; ngữ cảnh kiểm duyệt ngặt nghèo ('strict censorship') mang sắc thái tiêu cực (hầu như không có ai) nên dùng 'few'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Which of the following sentences correctly matches quantifiers with noun types?",
                            Options = new() { "A considerable number of students submitted their dissertations early.", "A considerable amount of students submitted their dissertations early.", "Much students submitted their dissertations early.", "Every students submitted their dissertations early." },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "A considerable number of students submitted their dissertations early.",
                            Explanation = "'students' là danh từ đếm được số nhiều nên đi kèm 'a number of', không đi với 'amount of' hay 'much'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Every citizen _______ entitled to basic healthcare and compulsory education under the constitution.",
                            Options = new() { "is", "are", "were", "being" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "is",
                            Explanation = "Chủ ngữ đi kèm 'Every citizen' luôn chia động từ ở ngôi thứ 3 số ít ('is')."
                        },
                    },
                    LearningTip = "Quy tắc vàng Task 1: Dùng 'number of' cho người và vật đếm được (students, cars); dùng 'amount of' cho tiền, rác thải, năng lượng (money, waste, electricity)!",
                    RelatedBandStructureCodes = new() { "BAS_POS_10" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "lien-tu",
                    Title = "Liên Từ (Conjunctions)",
                    EnglishTitle = "Conjunctions (Coordinating, Subordinating & Correlative)",
                    SectionKey = "parts-of-speech",
                    SectionTitle = "Từ Loại",
                    ShortDescription = "Liên từ kết hợp, liên từ phụ thuộc và liên từ tương hỗ (not only... but also, either... or, neither... nor).",
                    Icon = "bi-link-45deg",
                    IconBgColor = "#0e7490",
                    DifficultyLevel = "Trung cấp",
                    ReadTimeMinutes = 6,
                    OrderIndex = 12,
                    FormulaPreview = "Not only... but also | Either... or | Neither... nor",
                    SkillTarget = "Band 7.5+ Grammatical Range",
                    Tags = new() { "từ loại", "parts of speech", "liên từ", "conjunctions", "correlative", "not only but also" },
                    ConceptExplanation = "Liên từ (Conjunction) là chất keo liên kết các từ, cụm từ hoặc mệnh đề lại với nhau. Đặc biệt, Liên từ tương hỗ (Correlative Conjunctions) như not only... but also, both... and, neither... nor thể hiện trình độ cấu trúc song song và kiểm soát câu phức tạp ở trình độ cao.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Liên từ tương hỗ: Not only... but also", Formula = "Not only + A + but also + B (A và B phải đồng đẳng ngữ pháp)", ColorVariant = "blue", Breakdown = new() { "Đảo ngữ nếu đưa Not only lên đầu câu: Not only does it reduce pollution, but it also saves costs." } },
                        new GrammarFormulaBlock { Type = "Liên từ tương hỗ: Neither... nor / Either... or", Formula = "Neither + A + nor + B + V (chia theo chủ ngữ B)", ColorVariant = "purple", Breakdown = new() { "Neither the manager nor the employees were informed." } },
                        new GrammarFormulaBlock { Type = "Liên từ chỉ sự nhượng bộ & tương phản", Formula = "Whereas / While + Clause, Clause", ColorVariant = "green", Breakdown = new() { "Dùng để đối chiếu 2 số liệu hoặc 2 nhóm đối tượng trong Task 1 & 2" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Cấu trúc song song (Parallelism)", Example = "The program helps students develop critical thinking and improve communication skills. (Hai động từ cùng dạng nguyên mẫu)", Note = "Hai vế liên từ tương hỗ bắt buộc phải cùng từ loại" },
                        new GrammarRuleTableItem { Rule = "Quy tắc hòa hợp thì theo chủ ngữ gần nhất", Example = "Neither the teacher nor the students were present.", Note = "Động từ chia theo 'students' (số nhiều)" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Nhân đôi sức nặng của luận điểm với 'Not only... but also'",
                            Explanation = "Đưa ra 2 luận cứ hỗ trợ mạnh mẽ trong cùng một câu văn.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Adopting electric mobility not only mitigates urban pollution but also enhances public health.", Vietnamese = "Áp dụng phương tiện giao thông điện không chỉ giảm thiểu ô nhiễm đô thị mà còn nâng cao sức khỏe cộng đồng." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Đối chiếu tương phản trực tiếp trong IELTS Writing Task 1",
                            Explanation = "Sử dụng 'whereas' hoặc 'while' ở giữa câu.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "The consumption of beef plummeted, whereas poultry demand grew steadily.", Vietnamese = "Mức tiêu thụ thịt bò sụt giảm mạnh, trong khi nhu cầu thịt gia cầm lại tăng đều đặn." },
                            }
                        },
                    },
                    SignalWords = new() { "not only... but also", "either... or", "neither... nor", "both... and", "whereas", "while", "as well as" },
                    SignalWordPlacementRule = "Liên từ tương hỗ đi theo cặp kẹp hai thành phần ngữ pháp đồng cấp.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Vi phạm cấu trúc song song (Faulty Parallelism)",
                            WrongExample = "The course teaches both writing essays and to analyze data.",
                            CorrectExample = "The course teaches both writing essays and analyzing data.",
                            Explanation = "Sau 'both... and', hai vế phải cùng là V-ing: 'writing' và 'analyzing'."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Chia sai động từ theo chủ ngữ thứ nhất trong Neither... nor",
                            WrongExample = "Neither the principal nor the teachers was present.",
                            CorrectExample = "Neither the principal nor the teachers were present.",
                            Explanation = "Động từ hòa hợp với chủ ngữ gần nó nhất ('the teachers' là số nhiều → 'were')."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "Not only _______ substantial operational costs, but it also improved employee satisfaction.",
                            Options = new() { "did the new system reduce", "the new system reduced", "reduced the new system", "the new system was reduce" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "did the new system reduce",
                            Explanation = "Khi cụm phủ định 'Not only' đứng ở đầu câu, ta bắt buộc phải đảo trợ động từ lên trước chủ ngữ: 'did the new system reduce'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Which of the following sentences maintains STRICT grammatical parallelism?",
                            Options = new() { "The initiative aims at reducing poverty and promoting sustainable agriculture.", "The initiative aims at reducing poverty and to promote sustainable agriculture.", "The initiative aims at reducing poverty and sustainable agriculture promotion.", "The initiative aims at reduce poverty and promoting sustainable agriculture." },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "The initiative aims at reducing poverty and promoting sustainable agriculture.",
                            Explanation = "Cả hai vế sau 'and' đều giữ nguyên cấu trúc danh động từ V-ing ('reducing' và 'promoting')."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Neither the head of department nor the laboratory assistants _______ aware of the chemical leak.",
                            Options = new() { "were", "was", "is", "being" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "were",
                            Explanation = "Trong cấu trúc 'Neither A nor B', động từ chia theo danh từ gần nhất 'the laboratory assistants' (số nhiều) ở thì quá khứ → 'were'."
                        },
                    },
                    LearningTip = "Đảo ngữ với 'Not only did S + V, but S also...' ở câu chốt thân bài Task 2 là tuyệt chiêu ghi điểm Band 8.0 cho tiêu chí Grammatical Range!",
                    RelatedBandStructureCodes = new() { "ADV_POS_02" },
                },
            }
        };
    }
}
