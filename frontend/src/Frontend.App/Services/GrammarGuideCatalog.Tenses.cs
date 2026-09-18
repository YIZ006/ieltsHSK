using Frontend.App.Models;

namespace Frontend.App.Services;

public static partial class GrammarGuideCatalog
{
    private static GrammarGuideSectionDto BuildTensesSection()
    {
        return new GrammarGuideSectionDto
        {
            Key = "tenses",
            Overline = "MODULE 01 · CÁC THÌ TIẾNG ANH",
            Title = "Làm Chủ 12 Thì Tiếng Anh Chuẩn Xác",
            Description = "Nền tảng kiểm soát thời gian trong câu, tối ưu hóa tính chính xác ngữ pháp cho cả 4 kỹ năng Listening, Reading, Writing và Speaking.",
            Icon = "bi-clock-history",
            ColorTheme = "#0284c7",
            Topics = new List<GrammarGuideTopicDto>
            {
                new GrammarGuideTopicDto
                {
                    Slug = "thi-hien-tai-don",
                    Title = "Thì Hiện Tại Đơn",
                    EnglishTitle = "Simple Present Tense",
                    SectionKey = "tenses",
                    SectionTitle = "Các Thì",
                    ShortDescription = "Diễn đạt thói quen, chân lý khoa học, quy trình sản xuất và thời gian biểu cố định.",
                    Icon = "bi-sun-fill",
                    IconBgColor = "#0284c7",
                    DifficultyLevel = "Cơ bản",
                    ReadTimeMinutes = 5,
                    OrderIndex = 1,
                    FormulaPreview = "S + V(s/es) + O",
                    SkillTarget = "Writing Task 1 & Speaking",
                    Tags = new() { "thì", "tenses", "hiện tại đơn", "present simple", "cơ bản" },
                    ConceptExplanation = "Thì hiện tại đơn (Simple Present Tense) là thì ngữ pháp cơ bản nhất trong tiếng Anh, dùng để diễn đạt các hành động lặp đi lặp lại như một thói quen, các chân lý hoặc sự thật hiển nhiên, và các lịch trình hay thời gian biểu cố định trong cuộc sống.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Khẳng định (+)", Formula = "S + V(s/es) + O", ColorVariant = "blue", Breakdown = new() { "I / You / We / They + V (nguyên thể)", "He / She / It + V-s / V-es" } },
                        new GrammarFormulaBlock { Type = "Phủ định (-)", Formula = "S + do/does not + V_inf + O", ColorVariant = "red", Breakdown = new() { "I / You / We / They + don't + V_inf", "He / She / It + doesn't + V_inf" } },
                        new GrammarFormulaBlock { Type = "Nghi vấn (?)", Formula = "Do/Does + S + V_inf + O?", ColorVariant = "green", Breakdown = new() { "Do + I/you/we/they + V_inf?", "Does + he/she/it + V_inf?" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Thêm -s với hầu hết động từ", Example = "work → works, live → lives, read → reads", Note = "Áp dụng cho ngôi thứ 3 số ít (He, She, It)" },
                        new GrammarRuleTableItem { Rule = "Thêm -es với đuôi -o, -s, -ss, -ch, -sh, -x, -z", Example = "go → goes, watch → watches, wash → washes, fix → fixes", Note = "Phát âm đuôi /ɪz/" },
                        new GrammarRuleTableItem { Rule = "Tận cùng Phụ âm + y → đổi thành -ies", Example = "study → studies, fly → flies, try → tries", Note = "Nếu nguyên âm + y thì chỉ thêm -s: play → plays" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Diễn tả thói quen, hành động thường xuyên",
                            Explanation = "Các thói quen sinh hoạt thường ngày, thời khóa biểu của bản thân.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "I walk to the university every morning.", Vietnamese = "Tôi đi bộ đến trường đại học mỗi buổi sáng." },
                                new GrammarBilingualExample { English = "She drinks coffee before starting her work.", Vietnamese = "Cô ấy uống cà phê trước khi bắt đầu làm việc." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Chân lý khoa học, quy luật tự nhiên",
                            Explanation = "Các hiện tượng khoa học luôn luôn đúng ở mọi thời điểm.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "The Earth revolves around the Sun.", Vietnamese = "Trái Đất quay quanh Mặt Trời." },
                                new GrammarBilingualExample { English = "Water boils at 100 degrees Celsius at sea level.", Vietnamese = "Nước sôi ở 100 độ C tại mực nước biển." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 3,
                            Title = "Mô tả quy trình sản xuất (Process) trong IELTS Task 1",
                            Explanation = "Các bước thực hiện trong chuỗi công nghệ hoặc chu trình tự nhiên.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "In the subsequent stage, the raw materials pass through a heating chamber.", Vietnamese = "Ở giai đoạn tiếp theo, các nguyên liệu thô đi qua một buồng nung nhiệt." },
                            }
                        },
                    },
                    SignalWords = new() { "always", "usually", "often", "frequently", "sometimes", "rarely", "never", "every day/week" },
                    SignalWordPlacementRule = "Trạng từ tần suất đứng TRƯỚC động từ thường và SAU động từ 'to be' (He always arrives on time / He is always punctual).",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Quên thêm s/es khi chủ ngữ là ngôi thứ 3 số ít",
                            WrongExample = "He work in a multinational corporation.",
                            CorrectExample = "He works in a multinational corporation.",
                            Explanation = "Với chủ ngữ He/She/It hoặc danh từ số ít, động từ luôn phải chia thêm s/es ở thể khẳng định."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Vẫn chia động từ khi đã có trợ động từ does",
                            WrongExample = "Does she plays badminton every Sunday?",
                            CorrectExample = "Does she play badminton every Sunday?",
                            Explanation = "Khi đã có trợ động từ 'Does/Doesn't', động từ chính bắt buộc phải trở về dạng nguyên thể."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "The solar system _______ eight planets orbiting the central star.",
                            Options = new() { "consist of", "consists of", "is consisting of", "consisted of" },
                            CorrectOptionIndex = 1,
                            CorrectAnswer = "consists of",
                            Explanation = "Diễn tả sự thật khoa học hiển nhiên, chủ ngữ 'The solar system' là ngôi thứ 3 số ít nên động từ chia 'consists of'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "My brother _______ eat seafood because he is severely allergic to it.",
                            Options = new() { "doesn't", "don't", "isn't", "hasn't" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "doesn't",
                            Explanation = "Chủ ngữ 'My brother' (số ít) cần trợ động từ phủ định 'doesn't' kết hợp động từ nguyên thể 'eat'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "In the recycling process, crushed glass _______ melted in a specialized industrial furnace.",
                            Options = new() { "is", "are", "being", "was" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "is",
                            Explanation = "Mô tả quy trình sản xuất IELTS Writing Task 1, chủ ngữ không đếm được 'crushed glass' đi với to be 'is' ở hiện tại đơn."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 4,
                            Question = "How often _______ international conferences on climate change take place?",
                            Options = new() { "do", "does", "are", "have" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "do",
                            Explanation = "Chủ ngữ 'international conferences' là danh từ số nhiều nên dùng trợ động từ 'do' trong câu hỏi."
                        },
                    },
                    LearningTip = "Trong IELTS Writing Task 1, dùng thì Hiện tại đơn để mô tả quy trình (Process) hoặc bản đồ không có mốc thời gian trong quá khứ!",
                    RelatedBandStructureCodes = new() { "BAS_TENSE_01" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "thi-hien-tai-tiep-dien",
                    Title = "Thì Hiện Tại Tiếp Diễn",
                    EnglishTitle = "Present Continuous Tense",
                    SectionKey = "tenses",
                    SectionTitle = "Các Thì",
                    ShortDescription = "Hành động đang diễn ra tại thời điểm nói, xu hướng biến đổi và kế hoạch tương lai chắc chắn.",
                    Icon = "bi-arrow-repeat",
                    IconBgColor = "#0ea5e9",
                    DifficultyLevel = "Cơ bản",
                    ReadTimeMinutes = 5,
                    OrderIndex = 2,
                    FormulaPreview = "S + am/is/are + V-ing",
                    SkillTarget = "Speaking Part 1 & Writing Task 2",
                    Tags = new() { "thì", "tenses", "hiện tại tiếp diễn", "present continuous" },
                    ConceptExplanation = "Thì hiện tại tiếp diễn (Present Continuous Tense) diễn tả một hành động hoặc sự việc đang diễn ra ngay tại thời điểm nói, hoặc một xu hướng biến đổi đang diễn tiến trong xã hội hiện đại.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Khẳng định (+)", Formula = "S + am/is/are + V-ing + O", ColorVariant = "blue", Breakdown = new() { "I + am + V-ing", "He / She / It + is + V-ing", "You / We / They + are + V-ing" } },
                        new GrammarFormulaBlock { Type = "Phủ định (-)", Formula = "S + am/is/are not + V-ing + O", ColorVariant = "red", Breakdown = new() { "I'm not / isn't / aren't + V-ing" } },
                        new GrammarFormulaBlock { Type = "Nghi vấn (?)", Formula = "Am/Is/Are + S + V-ing + O?", ColorVariant = "green", Breakdown = new() { "Am I + V-ing?", "Is he/she/it + V-ing?", "Are you/we/they + V-ing?" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Động từ tận cùng bằng -e câm → bỏ -e thêm -ing", Example = "make → making, write → writing, take → taking", Note = "Ngoại lệ: see → seeing, agree → agreeing" },
                        new GrammarRuleTableItem { Rule = "Động từ 1 âm tiết (1 nguyên âm + 1 phụ âm) → gấp đôi phụ âm", Example = "run → running, stop → stopping, get → getting", Note = "Không gấp đôi với w, x, y (play → playing)" },
                        new GrammarRuleTableItem { Rule = "Động từ tận cùng bằng -ie → đổi thành -y rồi thêm -ing", Example = "lie → lying, die → dying, tie → tying", Note = "Lưu ý không nhầm lẫn với lie/lay" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Hành động đang diễn ra ngay tại thời điểm nói",
                            Explanation = "Sự việc có thể quan sát hoặc cảm nhận trực tiếp lúc này.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Listen! The lecturer is explaining the research methodology.", Vietnamese = "Hãy nghe này! Giảng viên đang giải thích phương pháp nghiên cứu." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Mô tả xu hướng thay đổi xã hội (Writing Task 2)",
                            Explanation = "Diễn tả xu hướng đang gia tăng hoặc suy giảm liên tục trong xã hội ngày nay.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "More and more consumers are shifting towards renewable energy alternatives.", Vietnamese = "Ngày càng nhiều người tiêu dùng đang chuyển dịch sang các giải pháp năng lượng tái tạo." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 3,
                            Title = "Kế hoạch hoặc dự định chắc chắn trong tương lai gần",
                            Explanation = "Đã có sự chuẩn bị hoặc thời gian biểu xác định rõ.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "The university delegation is attending the international symposium tomorrow.", Vietnamese = "Đoàn đại biểu trường đại học sẽ tham dự hội nghị quốc tế vào ngày mai." },
                            }
                        },
                    },
                    SignalWords = new() { "now", "right now", "at the moment", "currently", "at present", "Look!", "Listen!" },
                    SignalWordPlacementRule = "Cụm trạng từ thời gian thường đứng ở cuối câu hoặc đầu câu để nhấn mạnh ngữ cảnh thời gian.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Dùng thì tiếp diễn với động từ chỉ tri giác / trạng thái (Stative Verbs)",
                            WrongExample = "I am understanding the complex chemical equation right now.",
                            CorrectExample = "I understand the complex chemical equation right now.",
                            Explanation = "Các động từ chỉ trạng thái, nhận thức, cảm xúc (know, understand, believe, want, like, belong) không được chia ở dạng tiếp diễn."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Quên động từ to be (am/is/are) trước V-ing",
                            WrongExample = "The economy developing at an unprecedented pace.",
                            CorrectExample = "The economy is developing at an unprecedented pace.",
                            Explanation = "Câu tiếp diễn bắt buộc phải có trợ động từ to be tương ứng với chủ ngữ."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "A substantial proportion of the population _______ to metropolitan regions for better job prospects.",
                            Options = new() { "is migrating", "migrate", "are migrated", "have been migrate" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "is migrating",
                            Explanation = "Mô tả xu hướng biến đổi đương đại ('đang di cư'), dùng thì Hiện tại tiếp diễn."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Please be quiet; the candidates _______ their comprehensive listening examination.",
                            Options = new() { "take", "are taking", "have taken", "took" },
                            CorrectOptionIndex = 1,
                            CorrectAnswer = "are taking",
                            Explanation = "'Please be quiet' là dấu hiệu hành động đang diễn ra ngay lúc nói → 'are taking'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Which sentence is grammatically CORRECT regarding stative verbs?",
                            Options = new() { "She is knowing three foreign languages fluently.", "She knows three foreign languages fluently.", "She is having three foreign languages fluently.", "She knowing three foreign languages fluently." },
                            CorrectOptionIndex = 1,
                            CorrectAnswer = "She knows three foreign languages fluently.",
                            Explanation = "'know' là động từ trạng thái (stative verb), không được chia ở thì tiếp diễn."
                        },
                    },
                    LearningTip = "Khi viết Writing Task 2, cụm 'is becoming increasingly popular' hoặc 'is rapidly growing' là công cụ đắc lực để mở bài xu hướng!",
                    RelatedBandStructureCodes = new() { "BAS_TENSE_02" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "thi-hien-tai-hoan-thanh",
                    Title = "Thì Hiện Tại Hoàn Thành",
                    EnglishTitle = "Present Perfect Tense",
                    SectionKey = "tenses",
                    SectionTitle = "Các Thì",
                    ShortDescription = "Hành động xảy ra trong quá khứ kéo dài đến hiện tại, hoặc để lại kết quả ảnh hưởng tới hiện tại.",
                    Icon = "bi-check-circle-fill",
                    IconBgColor = "#10b981",
                    DifficultyLevel = "Trung cấp",
                    ReadTimeMinutes = 6,
                    OrderIndex = 3,
                    FormulaPreview = "S + have/has + V3/ed",
                    SkillTarget = "IELTS Writing Task 1 & 2 Band 7+",
                    Tags = new() { "thì", "tenses", "hiện tại hoàn thành", "present perfect" },
                    ConceptExplanation = "Thì hiện tại hoàn thành (Present Perfect Tense) tạo cầu nối giữa quá khứ và hiện tại. Nó diễn tả hành động đã xảy ra nhưng không đề cập thời gian cụ thể, hoặc hành động bắt đầu từ quá khứ và vẫn tiếp diễn ở hiện tại.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Khẳng định (+)", Formula = "S + have/has + V3/ed + O", ColorVariant = "blue", Breakdown = new() { "I / You / We / They + have + V3/ed", "He / She / It + has + V3/ed" } },
                        new GrammarFormulaBlock { Type = "Phủ định (-)", Formula = "S + have/has not + V3/ed + O", ColorVariant = "red", Breakdown = new() { "haven't / hasn't + V3/ed" } },
                        new GrammarFormulaBlock { Type = "Nghi vấn (?)", Formula = "Have/Has + S + V3/ed + O?", ColorVariant = "green", Breakdown = new() { "Have I/you/we/they + V3/ed?", "Has he/she/it + V3/ed?" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Phân biệt Since và For", Example = "Since 2010 (mốc thời gian) vs For 15 years (khoảng thời gian)", Note = "Since + mốc/mệnh đề quá khứ; For + khoảng thời gian" },
                        new GrammarRuleTableItem { Rule = "Vị trí của Already, Just, Yet", Example = "have just finished / haven't finished yet", Note = "Yet đứng cuối câu phủ định và nghi vấn" },
                        new GrammarRuleTableItem { Rule = "Ever vs Never trong câu trải nghiệm", Example = "Have you ever been...? / I have never witnessed...", Note = "Ever dùng trong câu hỏi, Never trong khẳng định mang nghĩa phủ định" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Hành động bắt đầu trong quá khứ và kéo dài đến hiện tại",
                            Explanation = "Thường đi cùng since/for hoặc over the last/past decades.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Vietnam has made extraordinary socioeconomic progress over the past three decades.", Vietnamese = "Việt Nam đã đạt được tiến bộ kinh tế xã hội vượt bậc trong suốt 3 thập kỷ qua." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Hành động vừa mới xảy ra để lại kết quả ở hiện tại",
                            Explanation = "Kết quả quan trọng hơn thời điểm xảy ra sự việc.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Scientists have discovered a promising compound that could cure the disease.", Vietnamese = "Các nhà khoa học vừa phát hiện một hợp chất triển vọng có thể chữa trị căn bệnh này." },
                            }
                        },
                    },
                    SignalWords = new() { "already", "yet", "just", "ever", "never", "since", "for", "so far", "recently", "lately", "over the past decade" },
                    SignalWordPlacementRule = "Just, already, never đứng giữa trợ động từ (have/has) và V3. Yet đứng ở cuối câu phủ định hoặc câu hỏi.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Dùng Hiện tại hoàn thành với mốc thời gian đã kết thúc trong quá khứ",
                            WrongExample = "The government has enacted this environmental law in 2015.",
                            CorrectExample = "The government enacted this environmental law in 2015.",
                            Explanation = "Có mốc thời gian quá khứ xác định ('in 2015') bắt buộc phải dùng thì Quá khứ đơn (Past Simple)."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Nhầm lẫn giữa 'have gone to' và 'have been to'",
                            WrongExample = "She has gone to London three times this year.",
                            CorrectExample = "She has been to London three times this year.",
                            Explanation = "'have gone to' nghĩa là đã đi chưa về; 'have been to' nghĩa là đã từng đến và đã trở về (trải nghiệm)."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "Over the last twenty years, technological advances _______ the way humans communicate.",
                            Options = new() { "revolutionized", "have revolutionized", "are revolutionizing", "revolutionize" },
                            CorrectOptionIndex = 1,
                            CorrectAnswer = "have revolutionized",
                            Explanation = "'Over the last twenty years' là dấu hiệu chỉ giai đoạn kéo dài đến hiện tại → dùng thì Hiện tại hoàn thành."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "The university _______ any empirical evidence to corroborate their hypothesis yet.",
                            Options = new() { "has not published", "did not publish", "is not publishing", "will not publish" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "has not published",
                            Explanation = "Từ tín hiệu 'yet' ở cuối câu phủ định đòi hỏi thì Hiện tại hoàn thành: 'has not published'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Dr. Miller _______ at this research institute since he graduated from Oxford.",
                            Options = new() { "worked", "works", "has worked", "is working" },
                            CorrectOptionIndex = 2,
                            CorrectAnswer = "has worked",
                            Explanation = "Mệnh đề 'since + S + V2/ed' đi kèm mệnh đề chính ở thì Hiện tại hoàn thành 'has worked'."
                        },
                    },
                    LearningTip = "Khi viết câu Overview trong IELTS Task 1 nếu biểu đồ có mốc thời gian từ quá khứ đến nay, thì Hiện tại hoàn thành là chìa khóa để đạt điểm Grammar Band 7.5+!",
                    RelatedBandStructureCodes = new() { "BAS_TENSE_03" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "thi-hien-tai-hoan-thanh-tiep-dien",
                    Title = "Thì Hiện Tại Hoàn Thành Tiếp Diễn",
                    EnglishTitle = "Present Perfect Continuous Tense",
                    SectionKey = "tenses",
                    SectionTitle = "Các Thì",
                    ShortDescription = "Nhấn mạnh tính liên tục, kéo dài và chưa hoàn tất của hành động từ quá khứ tới hiện tại.",
                    Icon = "bi-clock-fill",
                    IconBgColor = "#059669",
                    DifficultyLevel = "Trung cấp",
                    ReadTimeMinutes = 6,
                    OrderIndex = 4,
                    FormulaPreview = "S + have/has been + V-ing",
                    SkillTarget = "Speaking Part 1, 2 & Writing Task 2",
                    Tags = new() { "thì", "tenses", "hiện tại hoàn thành tiếp diễn", "present perfect continuous" },
                    ConceptExplanation = "Thì hiện tại hoàn thành tiếp diễn nhấn mạnh vào tính chất liên tục, quá trình kéo dài không gián đoạn của hành động bắt đầu trong quá khứ và có thể còn tiếp tục ở tương lai.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Khẳng định (+)", Formula = "S + have/has been + V-ing + O", ColorVariant = "blue", Breakdown = new() { "I / You / We / They + have been + V-ing", "He / She / It + has been + V-ing" } },
                        new GrammarFormulaBlock { Type = "Phủ định (-)", Formula = "S + haven't / hasn't been + V-ing + O", ColorVariant = "red", Breakdown = new() { "haven't / hasn't been + V-ing" } },
                        new GrammarFormulaBlock { Type = "Nghi vấn (?)", Formula = "Have/Has + S + been + V-ing + O?", ColorVariant = "green", Breakdown = new() { "Have/Has + S + been + V-ing?" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Phân biệt Hoàn thành Đơn vs Hoàn thành Tiếp diễn", Example = "I have read 3 books (nhấn mạnh kết quả) vs I have been reading all morning (nhấn mạnh sự liên tục)", Note = "Hỏi How much/How many dùng Perfect Simple, hỏi How long dùng Perfect Continuous" },
                        new GrammarRuleTableItem { Rule = "Không dùng với động từ trạng thái", Example = "I have known him for years (Đúng) - KHÔNG dùng I have been knowing", Note = "Động từ chỉ trạng thái chỉ dùng thì Hoàn thành đơn" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Hành động liên tục từ quá khứ đến hiện tại chưa dứt",
                            Explanation = "Nhấn mạnh vào thời lượng và tính kiên trì của hành động.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Researchers have been investigating the long-term impact of artificial intelligence for years.", Vietnamese = "Các nhà nghiên cứu đã và đang điều tra tác động lâu dài của trí tuệ nhân tạo trong nhiều năm nay." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Hành động vừa mới dừng lại nhưng để lại dấu vết rõ rệt",
                            Explanation = "Kết quả trực quan ở hiện tại do hành động vừa kéo dài tạo ra.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "The streets are soaking wet because it has been raining heavily.", Vietnamese = "Đường phố ướt sũng vì trời đã mưa tầm tã suốt thời gian qua." },
                            }
                        },
                    },
                    SignalWords = new() { "all day/morning/week", "since", "for", "how long", "lately", "recently" },
                    SignalWordPlacementRule = "Thường đi kèm các cụm từ chỉ độ dài thời gian như 'for five hours straight', 'all afternoon'.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Dùng thì tiếp diễn với câu nêu số lần hoặc số lượng",
                            WrongExample = "I have been writing three essays this morning.",
                            CorrectExample = "I have written three essays this morning.",
                            Explanation = "Khi nói đến số lượng hoàn thành cụ thể (three essays), bắt buộc phải dùng Hiện tại hoàn thành đơn."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Quên 'been' trong cấu trúc thì",
                            WrongExample = "He has working on the report since 7 AM.",
                            CorrectExample = "He has been working on the report since 7 AM.",
                            Explanation = "Cấu trúc bắt buộc là has/have + BEEN + V-ing."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "The global community _______ to negotiate an effective climate agreement for over two decades.",
                            Options = new() { "has been trying", "tried", "is trying", "has tried to been" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "has been trying",
                            Explanation = "Nhấn mạnh quá trình nỗ lực liên tục suốt hơn hai thập kỷ ('for over two decades') → Hiện tại hoàn thành tiếp diễn."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Look at his muddy shoes! He _______ football in the heavy rain.",
                            Options = new() { "has been playing", "has played", "is playing", "played" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "has been playing",
                            Explanation = "Có dấu vết trực quan ở hiện tại ('muddy shoes') do hành động vừa kéo dài gây ra → 'has been playing'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Why is 'She has been typing five business letters' considered inappropriate?",
                            Options = new() { "Because 'type' is a stative verb.", "Because it specifies a completed quantity (five letters), requiring Present Perfect Simple.", "Because 'letters' is plural.", "Because 'since' is missing." },
                            CorrectOptionIndex = 1,
                            CorrectAnswer = "Because it specifies a completed quantity (five letters), requiring Present Perfect Simple.",
                            Explanation = "Khi có số lượng cụ thể hoàn thành (five letters), phải dùng thì Hiện tại hoàn thành đơn (has typed)."
                        },
                    },
                    LearningTip = "Trong IELTS Speaking Part 1, khi giám khảo hỏi 'How long have you been studying English?', hãy trả lời bằng 'I have been learning English for...' để ăn trọn điểm ngữ pháp!",
                    RelatedBandStructureCodes = new() { "BAS_TENSE_04" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "thi-qua-khu-don",
                    Title = "Thì Quá Khứ Đơn",
                    EnglishTitle = "Simple Past Tense",
                    SectionKey = "tenses",
                    SectionTitle = "Các Thì",
                    ShortDescription = "Hành động đã bắt đầu và kết thúc hoàn toàn tại thời điểm xác định trong quá khứ.",
                    Icon = "bi-arrow-left-circle-fill",
                    IconBgColor = "#4f46e5",
                    DifficultyLevel = "Cơ bản",
                    ReadTimeMinutes = 5,
                    OrderIndex = 5,
                    FormulaPreview = "S + V2/ed + O",
                    SkillTarget = "IELTS Writing Task 1 (80% biểu đồ)",
                    Tags = new() { "thì", "tenses", "quá khứ đơn", "past simple" },
                    ConceptExplanation = "Thì quá khứ đơn (Past Simple Tense) dùng để diễn tả các hành động, sự việc đã xảy ra và chấm dứt hoàn toàn trong quá khứ, không còn liên quan gì đến hiện tại.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Khẳng định (+)", Formula = "S + V2/ed + O", ColorVariant = "blue", Breakdown = new() { "Động từ có quy tắc: V + ed", "Động từ bất quy tắc: tra cột V2" } },
                        new GrammarFormulaBlock { Type = "Phủ định (-)", Formula = "S + did not (didn't) + V_inf + O", ColorVariant = "red", Breakdown = new() { "didn't + động từ nguyên thể" } },
                        new GrammarFormulaBlock { Type = "Nghi vấn (?)", Formula = "Did + S + V_inf + O?", ColorVariant = "green", Breakdown = new() { "Did + S + V_inf?" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Quy tắc thêm đuôi -ed", Example = "walk → walked, live → lived, stop → stopped", Note = "Tận cùng e chỉ thêm d; phụ âm + y đổi thành -ied (study → studied)" },
                        new GrammarRuleTableItem { Rule = "Quy tắc phát âm đuôi -ed", Example = "/ɪd/ (t, d), /t/ (p, k, f, s, sh, ch), /d/ (các âm còn lại)", Note = "Rất quan trọng trong bài thi Speaking" },
                        new GrammarRuleTableItem { Rule = "Động từ To Be trong quá khứ", Example = "I/He/She/It + was; You/We/They + were", Note = "Trong câu điều kiện loại 2, 'were' có thể dùng cho mọi ngôi" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Hành động đã hoàn tất tại thời điểm quá khứ xác định",
                            Explanation = "Có mốc thời gian rõ ràng như yesterday, in 1995, last year.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "In 2005, the unemployment rate in Japan dropped to an all-time low of 4.4%.", Vietnamese = "Năm 2005, tỷ lệ thất nghiệp ở Nhật Bản đã giảm xuống mức thấp kỷ lục là 4.4%." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Chuỗi hành động liên tiếp xảy ra trong quá khứ",
                            Explanation = "Thường dùng trong văn kể chuyện (Storytelling) Speaking Part 2.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "He entered the examination room, registered his identification, and began writing.", Vietnamese = "Anh ấy bước vào phòng thi, xuất trình giấy tờ tùy thân và bắt đầu làm bài." },
                            }
                        },
                    },
                    SignalWords = new() { "yesterday", "last night/week/year", "ago", "in 1999", "when I was young", "at that time" },
                    SignalWordPlacementRule = "Trạng từ thời gian quá khứ thường đứng ở đầu câu (kèm dấu phẩy) hoặc cuối câu.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Vẫn chia V2 sau trợ động từ did/didn't",
                            WrongExample = "They didn't went to the meeting yesterday.",
                            CorrectExample = "They didn't go to the meeting yesterday.",
                            Explanation = "Đã có trợ động từ 'did/didn't' thì động từ chính bắt buộc ở dạng nguyên thể."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Nhầm lẫn dạng bất quy tắc sang thêm -ed",
                            WrongExample = "The price of petrol rised dramatically in 2018.",
                            CorrectExample = "The price of petrol rose dramatically in 2018.",
                            Explanation = "Quá khứ của 'rise' là 'rose', không phải 'rised'."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "The industrial sector _______ substantially during the late 1990s.",
                            Options = new() { "expanded", "expands", "has expanded", "is expanding" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "expanded",
                            Explanation = "Mốc thời gian xác định trong quá khứ ('during the late 1990s') yêu cầu chia thì Quá khứ đơn."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Between 2000 and 2010, the volume of exports _______ while domestic consumption declined.",
                            Options = new() { "grew", "growed", "has grown", "was grown" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "grew",
                            Explanation = "'grow' là động từ bất quy tắc, quá khứ đơn là 'grew'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Why _______ the project coordinators submit the progress evaluation on time?",
                            Options = new() { "didn't", "weren't", "haven't", "not" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "didn't",
                            Explanation = "Câu hỏi phủ định quá khứ với động từ thường 'submit' cần trợ động từ 'didn't'."
                        },
                    },
                    LearningTip = "Khi viết Task 1 có năm quá khứ (ví dụ 2000-2020), hãy chắc chắn 100% các động từ miêu tả xu hướng (rose, plunged, fluctuated) đều chia ở thì quá khứ đơn!",
                    RelatedBandStructureCodes = new() { "BAS_TENSE_05" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "thi-qua-khu-tiep-dien",
                    Title = "Thì Quá Khứ Tiếp Diễn",
                    EnglishTitle = "Past Continuous Tense",
                    SectionKey = "tenses",
                    SectionTitle = "Các Thì",
                    ShortDescription = "Hành động đang diễn ra tại một thời điểm quá khứ hoặc bối cảnh cho hành động khác xen vào.",
                    Icon = "bi-play-circle-fill",
                    IconBgColor = "#4338ca",
                    DifficultyLevel = "Cơ bản",
                    ReadTimeMinutes = 5,
                    OrderIndex = 6,
                    FormulaPreview = "S + was/were + V-ing",
                    SkillTarget = "Speaking Storytelling",
                    Tags = new() { "thì", "tenses", "quá khứ tiếp diễn", "past continuous" },
                    ConceptExplanation = "Thì quá khứ tiếp diễn mô tả một hành động đang diễn ra tại một thời điểm xác định trong quá khứ hoặc làm nền cho một sự kiện khác bất ngờ xen vào.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Khẳng định (+)", Formula = "S + was/were + V-ing + O", ColorVariant = "blue", Breakdown = new() { "I / He / She / It + was + V-ing", "You / We / They + were + V-ing" } },
                        new GrammarFormulaBlock { Type = "Phủ định (-)", Formula = "S + was/were not + V-ing + O", ColorVariant = "red", Breakdown = new() { "wasn't / weren't + V-ing" } },
                        new GrammarFormulaBlock { Type = "Nghi vấn (?)", Formula = "Was/Were + S + V-ing + O?", ColorVariant = "green", Breakdown = new() { "Was/Were + S + V-ing?" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Cấu trúc While + Past Continuous", Example = "While I was studying, the phone rang.", Note = "Hành động đang xảy ra dùng tiếp diễn, xen vào dùng quá khứ đơn" },
                        new GrammarRuleTableItem { Rule = "Cấu trúc When + Past Simple", Example = "When the storm hit, they were travelling.", Note = "When thường đi với hành động ngắn cắt ngang" },
                        new GrammarRuleTableItem { Rule = "Hai hành động diễn ra song song", Example = "While mom was cooking, dad was reading.", Note = "Cả 2 mệnh đề đều chia thì quá khứ tiếp diễn" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Hành động đang diễn ra tại mốc giờ cụ thể trong quá khứ",
                            Explanation = "Có mốc giờ xác định như at 8 PM yesterday.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "At 9 PM yesterday, the researchers were still analyzing the genomic data.", Vietnamese = "Vào lúc 9 giờ tối hôm qua, các nhà nghiên cứu vẫn đang phân tích dữ liệu bộ gen." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Làm bối cảnh cho một hành động khác cắt ngang",
                            Explanation = "Thường kết hợp liên từ When hoặc While trong Speaking Part 2.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "While I was preparing for my IELTS exam, an unexpected job offer arrived.", Vietnamese = "Khi tôi đang chuẩn bị cho kỳ thi IELTS thì một lời mời làm việc bất ngờ gửi đến." },
                            }
                        },
                    },
                    SignalWords = new() { "at that moment", "at 7 PM yesterday", "while", "when", "all yesterday morning" },
                    SignalWordPlacementRule = "Mệnh đề While thường đứng trước hoặc sau mệnh đề chính, ngăn cách bằng dấu phẩy nếu đứng đầu.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Dùng thì tiếp diễn cho hành động cắt ngang ngắn",
                            WrongExample = "When the fire alarm was ringing, everyone evacuated.",
                            CorrectExample = "When the fire alarm rang, everyone evacuated.",
                            Explanation = "Tiếng chuông báo cháy reo là hành động ngắn cắt ngang bối cảnh, nên dùng Quá khứ đơn."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Nhầm lẫn giữa was và were theo chủ ngữ",
                            WrongExample = "The team members was debating heatedly.",
                            CorrectExample = "The team members were debating heatedly.",
                            Explanation = "'The team members' là danh từ số nhiều, bắt buộc đi với trợ động từ 'were'."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "At 10 PM last night, the committee _______ the annual financial allocation.",
                            Options = new() { "was discussing", "discussed", "were discussed", "has discussed" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "was discussing",
                            Explanation = "Có thời điểm chính xác trong quá khứ ('At 10 PM last night') → dùng Quá khứ tiếp diễn."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "While the participants _______ the questionnaire, the fire alarm suddenly sounded.",
                            Options = new() { "were completing", "completed", "are completing", "have completed" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "were completing",
                            Explanation = "Hành động đang diễn ra làm bối cảnh đi kèm 'While' dùng Quá khứ tiếp diễn: 'were completing'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "I _______ home when I bumped into my former university professor.",
                            Options = new() { "was walking", "walked", "am walking", "have walked" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "was walking",
                            Explanation = "Hành động đang xảy ra (đang đi bộ về nhà) thì hành động khác xen vào (gặp giáo sư) → 'was walking'."
                        },
                    },
                    LearningTip = "Trong Speaking Part 2 kể về một kỷ niệm, mở đầu bằng 'It happened while I was living in...' sẽ giúp bạn phô diễn ngữ pháp phức tự nhiên!",
                    RelatedBandStructureCodes = new() { "BAS_TENSE_06" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "thi-qua-khu-hoan-thanh",
                    Title = "Thì Quá Khứ Hoàn Thành",
                    EnglishTitle = "Past Perfect Tense",
                    SectionKey = "tenses",
                    SectionTitle = "Các Thì",
                    ShortDescription = "Hành động xảy ra và hoàn tất trước một hành động hoặc thời điểm khác trong quá khứ.",
                    Icon = "bi-rewind-fill",
                    IconBgColor = "#3730a3",
                    DifficultyLevel = "Trung cấp",
                    ReadTimeMinutes = 6,
                    OrderIndex = 7,
                    FormulaPreview = "S + had + V3/ed",
                    SkillTarget = "IELTS Writing Task 2 & Reading",
                    Tags = new() { "thì", "tenses", "quá khứ hoàn thành", "past perfect" },
                    ConceptExplanation = "Thì quá khứ hoàn thành (Past Perfect Tense) diễn tả một hành động xảy ra trước một hành động khác trong quá khứ. Hành động xảy ra trước dùng Past Perfect, hành động xảy ra sau dùng Past Simple.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Khẳng định (+)", Formula = "S + had + V3/ed + O", ColorVariant = "blue", Breakdown = new() { "Áp dụng had cho tất cả các ngôi chủ ngữ" } },
                        new GrammarFormulaBlock { Type = "Phủ định (-)", Formula = "S + had not (hadn't) + V3/ed + O", ColorVariant = "red", Breakdown = new() { "hadn't + V3/ed" } },
                        new GrammarFormulaBlock { Type = "Nghi vấn (?)", Formula = "Had + S + V3/ed + O?", ColorVariant = "green", Breakdown = new() { "Had + S + V3/ed?" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Cấu trúc Before và After", Example = "Before + Past Simple, Past Perfect / After + Past Perfect, Past Simple", Note = "Hành động xảy ra trước luôn là Past Perfect" },
                        new GrammarRuleTableItem { Rule = "Cấu trúc By the time trong quá khứ", Example = "By the time the rescue team arrived, the storm had ceased.", Note = "By the time + Past Simple, Past Perfect" },
                        new GrammarRuleTableItem { Rule = "Cấu trúc No sooner... than / Hardly... when", Example = "No sooner had I arrived than the bell rang.", Note = "Đảo ngữ với No sooner/Hardly mang tính học thuật cao" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Hành động xảy ra trước một mốc thời gian quá khứ",
                            Explanation = "Thường đi cùng 'by 2010', 'by the end of that century'.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "By 2015, the multinational corporation had invested over 500 million dollars into renewable tech.", Vietnamese = "Tính đến năm 2015, tập đoàn đa quốc gia đã đầu tư hơn 500 triệu USD vào công nghệ tái tạo." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Hành động xảy ra trước một hành động khác trong quá khứ",
                            Explanation = "Xác lập thứ tự trước - sau logic của các sự kiện lịch sử hoặc nghiên cứu.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "The ecosystem collapsed because authorities had ignored early warning signs.", Vietnamese = "Hệ sinh thái sụp đổ vì các nhà chức trách đã phớt lờ những dấu hiệu cảnh báo sớm." },
                            }
                        },
                    },
                    SignalWords = new() { "by the time", "before", "after", "by 2000", "hardly... when", "no sooner... than" },
                    SignalWordPlacementRule = "By the time thường đứng đầu câu đi kèm mệnh đề Quá khứ đơn.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Lạm dụng Quá khứ hoàn thành khi không có hành động so sánh thời gian",
                            WrongExample = "The Roman Empire had fallen in 476 AD.",
                            CorrectExample = "The Roman Empire fell in 476 AD.",
                            Explanation = "Chỉ có một mốc thời gian quá khứ đơn lẻ thì chỉ dùng Quá khứ đơn, không dùng Quá khứ hoàn thành."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Chia sai vị trí trước - sau giữa hai mệnh đề After",
                            WrongExample = "After they finished the test, they had left the room.",
                            CorrectExample = "After they had finished the test, they left the room.",
                            Explanation = "Hành động làm xong bài xảy ra trước nên phải chia Past Perfect (had finished)."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "By the time the regulation came into force, many unauthorized factories _______ operations.",
                            Options = new() { "had commenced", "commenced", "have commenced", "were commencing" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "had commenced",
                            Explanation = "Hành động các nhà máy bắt đầu hoạt động xảy ra TRƯỚC thời điểm quy định có hiệu lực → 'had commenced'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "The candidate was confident because he _______ rigorous preparation for months.",
                            Options = new() { "had undergone", "underwent", "has undergone", "was undergoing" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "had undergone",
                            Explanation = "Quá trình ôn luyện diễn ra trước thời điểm cảm thấy tự tin trong quá khứ → 'had undergone'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "No sooner _______ the agreement than disputes broke out.",
                            Options = new() { "had they signed", "they had signed", "did they sign", "have they signed" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "had they signed",
                            Explanation = "Cấu trúc đảo ngữ 'No sooner had + S + V3 than...' biểu thị hành động vừa mới xảy ra thì hành động khác ập tới."
                        },
                    },
                    LearningTip = "Khi viết biểu đồ Task 1 có mốc 'By 2010...', dùng thì Quá khứ hoàn thành cho thấy bạn có khả năng kiểm soát thì ở trình độ Band 8.0!",
                    RelatedBandStructureCodes = new() { "BAS_TENSE_07" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "thi-qua-khu-hoan-thanh-tiep-dien",
                    Title = "Thì Quá Khứ Hoàn Thành Tiếp Diễn",
                    EnglishTitle = "Past Perfect Continuous Tense",
                    SectionKey = "tenses",
                    SectionTitle = "Các Thì",
                    ShortDescription = "Hành động xảy ra liên tục trong một khoảng thời gian trước một thời điểm hoặc sự kiện khác trong quá khứ.",
                    Icon = "bi-stopwatch-fill",
                    IconBgColor = "#1e1b4b",
                    DifficultyLevel = "Nâng cao",
                    ReadTimeMinutes = 6,
                    OrderIndex = 8,
                    FormulaPreview = "S + had been + V-ing",
                    SkillTarget = "Reading & High-band Writing",
                    Tags = new() { "thì", "tenses", "quá khứ hoàn thành tiếp diễn", "past perfect continuous" },
                    ConceptExplanation = "Thì quá khứ hoàn thành tiếp diễn nhấn mạnh vào khoảng thời gian kéo dài liên tục của một hành động trước khi một hành động quá khứ khác xảy ra, hoặc là nguyên nhân dẫn đến một kết quả trong quá khứ.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Khẳng định (+)", Formula = "S + had been + V-ing + O", ColorVariant = "blue", Breakdown = new() { "had been + V-ing áp dụng cho mọi ngôi" } },
                        new GrammarFormulaBlock { Type = "Phủ định (-)", Formula = "S + hadn't been + V-ing + O", ColorVariant = "red", Breakdown = new() { "hadn't been + V-ing" } },
                        new GrammarFormulaBlock { Type = "Nghi vấn (?)", Formula = "Had + S + been + V-ing + O?", ColorVariant = "green", Breakdown = new() { "Had + S + been + V-ing?" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Nhấn mạnh quá trình kéo dài trước hành động quá khứ", Example = "He had been running for an hour before he collapsed.", Note = "Nhấn mạnh tính bền bỉ và thời lượng của hành động 1" },
                        new GrammarRuleTableItem { Rule = "Giải thích nguyên nhân trong quá khứ", Example = "Her eyes were red because she had been crying.", Note = "Hành động khóc kéo dài để lại dấu hiệu ở thời điểm quá khứ" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Hành động tiếp diễn liên tục trước một sự kiện quá khứ",
                            Explanation = "Làm nổi bật mức độ nỗ lực hoặc độ dài thời gian.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Prior to the product launch, the software engineers had been working non-stop for 48 hours.", Vietnamese = "Trước khi ra mắt sản phẩm, các kỹ sư phần mềm đã làm việc liên tục không ngừng trong 48 giờ." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Nguyên nhân của một trạng thái hoặc tình huống quá khứ",
                            Explanation = "Giải thích lý do tại sao một trạng thái xảy ra trong quá khứ.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "The soil was fertile because local farmers had been using compost for years.", Vietnamese = "Đất đai màu mỡ vì nông dân địa phương đã liên tục sử dụng phân hữu cơ trong nhiều năm." },
                            }
                        },
                    },
                    SignalWords = new() { "before", "prior to", "for hours before", "until then", "by that time" },
                    SignalWordPlacementRule = "Thường đi với các cụm chỉ khoảng thời gian 'for + time' đặt trước mệnh đề quá khứ đơn.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Dùng với động từ chỉ trạng thái (stative verbs)",
                            WrongExample = "They had been belonging to the union for years before it closed.",
                            CorrectExample = "They had belonged to the union for years before it closed.",
                            Explanation = "Động từ 'belong' chỉ trạng thái, không dùng ở thì tiếp diễn dù có nhấn mạnh thời gian."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Nhầm lẫn với Quá khứ tiếp diễn đơn thuần",
                            WrongExample = "When the boss arrived, I was working there for 5 years.",
                            CorrectExample = "When the boss arrived, I had been working there for 5 years.",
                            Explanation = "Có khoảng thời gian kéo dài 'for 5 years' trước thời điểm sếp đến phải dùng Past Perfect Continuous."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "The scientific expedition _______ for three months before they finally uncovered the ancient ruins.",
                            Options = new() { "had been excavating", "has been excavating", "were excavating", "had excavated to be" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "had been excavating",
                            Explanation = "Nhấn mạnh quá trình khai quật kéo dài liên tục suốt 3 tháng trước sự kiện khám phá trong quá khứ."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "She failed the final assessment because she _______ classes regularly throughout the semester.",
                            Options = new() { "had not been attending", "was not attending", "has not been attending", "is not attending" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "had not been attending",
                            Explanation = "Hành động không tham dự lớp kéo dài liên tục là nguyên nhân dẫn đến việc trượt bài thi quá khứ."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Before the merger took place, the two enterprises _______ negotiations for nearly a year.",
                            Options = new() { "had been conducting", "were conducting", "conducted", "have been conducting" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "had been conducting",
                            Explanation = "Hành động đàm phán diễn ra liên tục trong gần một năm trước khi vụ sáp nhập xảy ra → 'had been conducting'."
                        },
                    },
                    LearningTip = "Sử dụng chính xác thì này trong IELTS Speaking Part 2 khi giải thích nguyên nhân cho cảm xúc hay kết quả quá khứ sẽ gây ấn tượng cực mạnh với giám khảo!",
                    RelatedBandStructureCodes = new() { "ADV_TENSE_01" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "thi-tuong-lai-don",
                    Title = "Thì Tương Lai Đơn",
                    EnglishTitle = "Simple Future Tense",
                    SectionKey = "tenses",
                    SectionTitle = "Các Thì",
                    ShortDescription = "Dự đoán, phán đoán tương lai và quyết định tự phát đưa ra ngay tại thời điểm nói.",
                    Icon = "bi-fast-forward-fill",
                    IconBgColor = "#059669",
                    DifficultyLevel = "Cơ bản",
                    ReadTimeMinutes = 5,
                    OrderIndex = 9,
                    FormulaPreview = "S + will + V_inf + O",
                    SkillTarget = "Dự đoán Task 1 & Task 2",
                    Tags = new() { "thì", "tenses", "tương lai đơn", "future simple" },
                    ConceptExplanation = "Thì tương lai đơn (Simple Future Tense) được tạo thành bằng will + động từ nguyên mẫu. Chúng ta sử dụng nó để nói về những hành động, sự kiện sẽ xảy ra trong tương lai hoặc để đưa ra dự đoán, lời hứa và quyết định tức thì.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Khẳng định (+)", Formula = "S + will + V (nguyên mẫu)", ColorVariant = "blue", Breakdown = new() { "Áp dụng 'will' cho tất cả các ngôi chủ ngữ", "Trong văn nói/thân mật thường viết tắt: I'll, you'll, they'll" } },
                        new GrammarFormulaBlock { Type = "Phủ định (-)", Formula = "S + will not (won't) + V (nguyên mẫu)", ColorVariant = "red", Breakdown = new() { "Viết tắt: will not = won't", "Sau won't là động từ nguyên mẫu không chia" } },
                        new GrammarFormulaBlock { Type = "Nghi vấn (?)", Formula = "Will + S + V (nguyên mẫu)?", ColorVariant = "green", Breakdown = new() { "Câu hỏi Yes/No: Will you/they...?", "Câu hỏi Wh-: What/Where will + S + V?" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "So sánh Will vs Be Going To (Quyết định)", Example = "Will: Quyết định tức thì tại thời điểm nói ('I'll help you') / Be going to: Kế hoạch đã định trước ('I am going to study abroad next year')", Note = "IELTS Writing rất chú trọng sự khác biệt này" },
                        new GrammarRuleTableItem { Rule = "So sánh Will vs Be Going To (Dự đoán)", Example = "Will: Dự đoán dựa trên linh cảm ('I think it will be sunny') / Be going to: Dự đoán có bằng chứng cụ thể ('Look at those black clouds! It is going to rain')", Note = "Có dấu hiệu thực tế luôn ưu tiên Be Going To" },
                        new GrammarRuleTableItem { Rule = "Mệnh đề trạng ngữ chỉ thời gian (Time Clauses)", Example = "As soon as he arrives, we will start.", Note = "Trong mệnh đề chỉ thời gian (when, as soon as, before, until), dùng Hiện tại đơn thay cho Tương lai đơn" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Dự đoán tương lai (Future Predictions)",
                            Explanation = "Đưa ra phán đoán về những gì có thể diễn ra, thường đi cùng think, believe, expect.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Without decisive environmental policies, global temperatures will increase substantially.", Vietnamese = "Nếu không có các chính sách môi trường dứt khoát, nhiệt độ toàn cầu sẽ tăng đáng kể." },
                                new GrammarBilingualExample { English = "Experts predict that artificial intelligence will transform the education sector.", Vietnamese = "Các chuyên gia dự đoán trí tuệ nhân tạo sẽ làm thay đổi căn bản ngành giáo dục." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Quyết định tức thì tại thời điểm nói (Spontaneous Decisions)",
                            Explanation = "Quyết định đưa ra bất chợt, không hề có sự lên kế hoạch hay chuẩn bị từ trước.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "The phone is ringing. I will answer it.", Vietnamese = "Chuông điện thoại đang reo. Tôi sẽ nghe máy ngay." },
                                new GrammarBilingualExample { English = "I feel exhausted; I will take a short break now.", Vietnamese = "Tôi cảm thấy kiệt sức; tôi sẽ nghỉ ngơi một chút bây giờ." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 3,
                            Title = "Lời hứa & Cam kết (Promises & Commitments)",
                            Explanation = "Cam kết thực hiện hoặc không thực hiện một điều gì đó.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "I promise I will submit the dissertation proposal before Friday.", Vietnamese = "Tôi hứa tôi sẽ nộp đề cương luận văn trước thứ Sáu." },
                                new GrammarBilingualExample { English = "The government will not raise income taxes this year.", Vietnamese = "Chính phủ sẽ không tăng thuế thu nhập trong năm nay." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 4,
                            Title = "Yêu cầu & Đề nghị lịch sự (Requests & Offers)",
                            Explanation = "Ngỏ lời giúp đỡ hoặc yêu cầu ai đó làm gì trong giao tiếp.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Will you please review my thesis introduction?", Vietnamese = "Bạn có thể vui lòng xem qua phần mở đầu luận án giúp tôi được không?" },
                                new GrammarBilingualExample { English = "That suitcase looks heavy; I will carry it for you.", Vietnamese = "Chiếc vali đó trông nặng quá; tôi sẽ xách giúp bạn." },
                            }
                        },
                    },
                    SignalWords = new() { "tomorrow", "next week/month/year", "soon", "later", "in the future", "someday", "tonight", "probably", "I think", "I believe" },
                    SignalWordPlacementRule = "Với tất cả các ngôi (I, you, he, she, it, we, they), chúng ta đều dùng 'will', không phân biệt số ít hay số nhiều.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Thêm 'to' sau will (Will + to V)",
                            WrongExample = "I will to go to school tomorrow.",
                            CorrectExample = "I will go to school tomorrow.",
                            Explanation = "Không được thêm 'to' sau 'will'. Sau 'will' luôn là động từ nguyên mẫu không to."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Chia động từ (s/es/ed) sau will",
                            WrongExample = "She will goes to the international workshop.",
                            CorrectExample = "She will go to the international workshop.",
                            Explanation = "Động từ sau trợ động từ khuyết thiếu 'will' tuyệt đối không bao giờ chia."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "Which of the following is the CORRECT translation of: 'Tôi sẽ gọi điện cho bạn vào tối mai'?",
                            Options = new() { "I will call you tomorrow night.", "I will to call you tomorrow night.", "I will calling you tomorrow night.", "I am call you tomorrow night." },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "I will call you tomorrow night.",
                            Explanation = "Cấu trúc tương lai đơn: S + will + V_inf. Tuyệt đối không thêm 'to' hay đuôi '-ing' sau 'will'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Look at those dark storm clouds gathering! It _______ rain heavily soon.",
                            Options = new() { "will", "is going to", "was raining", "has rained" },
                            CorrectOptionIndex = 1,
                            CorrectAnswer = "is going to",
                            Explanation = "Dự đoán có bằng chứng trực quan cụ thể ngay trước mắt ('dark storm clouds') bắt buộc dùng 'be going to' thay vì 'will'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "As soon as the administrative board _______ the final budget, construction will commence.",
                            Options = new() { "approves", "will approve", "approved", "is approving" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "approves",
                            Explanation = "Trong mệnh đề chỉ thời gian tương lai bắt đầu bằng 'As soon as', động từ chia ở thì Hiện tại đơn ('approves')."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 4,
                            Question = "Don't worry about the heavy luggage; I _______ you carry it up the stairs.",
                            Options = new() { "will help", "am helping", "helped", "will to help" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "will help",
                            Explanation = "Quyết định đề nghị giúp đỡ đưa ra ngay tại thời điểm nói dùng 'will + V_inf': 'will help'."
                        },
                    },
                    LearningTip = "Trong văn viết học thuật IELTS Task 2, thay vì lặp lại 'will', hãy kết hợp các cụm từ dự báo nâng cao như 'is projected to', 'is likely to', 'is anticipated to' để đạt Band 7.5+ Lexical Resource!",
                    RelatedBandStructureCodes = new() { "BAS_TENSE_09" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "thi-tuong-lai-tiep-dien",
                    Title = "Thì Tương Lai Tiếp Diễn",
                    EnglishTitle = "Future Continuous Tense",
                    SectionKey = "tenses",
                    SectionTitle = "Các Thì",
                    ShortDescription = "Hành động đang diễn ra tại một thời điểm hoặc khoảng thời gian xác định trong tương lai.",
                    Icon = "bi-hourglass-split",
                    IconBgColor = "#0d9488",
                    DifficultyLevel = "Trung cấp",
                    ReadTimeMinutes = 5,
                    OrderIndex = 10,
                    FormulaPreview = "S + will be + V-ing",
                    SkillTarget = "Speaking Part 1 & 3 Phỏng đoán",
                    Tags = new() { "thì", "tenses", "tương lai tiếp diễn", "future continuous" },
                    ConceptExplanation = "Thì tương lai tiếp diễn mô tả một hành động đang trong quá trình diễn tiến tại một thời điểm cụ thể trong tương lai, hoặc các sự kiện nằm trong lịch trình định sẵn.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Khẳng định (+)", Formula = "S + will be + V-ing + O", ColorVariant = "blue", Breakdown = new() { "will be + V-ing cho mọi ngôi" } },
                        new GrammarFormulaBlock { Type = "Phủ định (-)", Formula = "S + won't be + V-ing + O", ColorVariant = "red", Breakdown = new() { "won't be + V-ing" } },
                        new GrammarFormulaBlock { Type = "Nghi vấn (?)", Formula = "Will + S + be + V-ing + O?", ColorVariant = "green", Breakdown = new() { "Will + S + be + V-ing?" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Thời điểm cụ thể trong tương lai", Example = "At this time tomorrow, I will be flying to Sydney.", Note = "Có mốc giờ hoặc cụm 'at this time'" },
                        new GrammarRuleTableItem { Rule = "Hành động đang diễn ra trong tương lai thì hành động khác xen vào", Example = "When you arrive, I will be waiting at the lobby.", Note = "Mệnh đề When chia Hiện tại đơn" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Hành động đang diễn ra tại thời điểm tương lai cụ thể",
                            Explanation = "Có mốc giờ hoặc sự kiện tham chiếu rõ ràng.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "At 10 AM tomorrow, the delegates will be attending the opening ceremony.", Vietnamese = "Lúc 10 giờ sáng mai, các đại biểu sẽ đang tham dự lễ khai mạc." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Sự kiện được kỳ vọng diễn ra theo tiến trình bình thường",
                            Explanation = "Không mang ý định cá nhân mà là tiến trình tự nhiên.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Electric autonomous vehicles will be dominating our highways in the coming decades.", Vietnamese = "Xe điện tự hành sẽ đang thống trị các tuyến cao tốc của chúng ta trong những thập kỷ tới." },
                            }
                        },
                    },
                    SignalWords = new() { "at this time tomorrow", "at 8 PM tonight", "in the next decade", "this time next week" },
                    SignalWordPlacementRule = "Cụm trạng từ thời gian thường đứng ở đầu câu để thiết lập khung cảnh tương lai.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Quên 'be' trong cấu trúc will be V-ing",
                            WrongExample = "At 8 PM tonight, they will presenting their research findings.",
                            CorrectExample = "At 8 PM tonight, they will be presenting their research findings.",
                            Explanation = "Cấu trúc bắt buộc là will + BE + V-ing."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Dùng thì tương lai trong mệnh đề When",
                            WrongExample = "When you will arrive, I will be waiting for you.",
                            CorrectExample = "When you arrive, I will be waiting for you.",
                            Explanation = "Mệnh đề thời gian với When chia ở Hiện tại đơn (arrive)."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "This time next year, thousands of university graduates _______ for positions in high-tech industries.",
                            Options = new() { "will be competing", "compete", "will have compete", "are competing to be" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "will be competing",
                            Explanation = "Mốc thời gian xác định trong tương lai ('This time next year') → dùng thì Tương lai tiếp diễn."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "When the inspector visits our laboratory tomorrow, our team _______ experiments on vaccine efficacy.",
                            Options = new() { "will be conducting", "conducts", "will conduct have", "conducted" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "will be conducting",
                            Explanation = "Hành động đang diễn ra trong tương lai khi một sự kiện khác xen vào → 'will be conducting'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Don't phone me between 2 and 4 PM; I _______ a crucial business presentation.",
                            Options = new() { "will be delivering", "deliver", "delivered", "have delivered" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "will be delivering",
                            Explanation = "Khoảng thời gian xác định trong tương lai ('between 2 and 4 PM') → 'will be delivering'."
                        },
                    },
                    LearningTip = "Khi trả lời IELTS Speaking Part 1 về chủ đề công việc tương lai: 'In five years, I envision that I will be managing my own startup' sẽ đem lại điểm ngữ pháp rất cao!",
                    RelatedBandStructureCodes = new() { "ADV_TENSE_02" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "thi-tuong-lai-hoan-thanh",
                    Title = "Thì Tương Lai Hoàn Thành",
                    EnglishTitle = "Future Perfect Tense",
                    SectionKey = "tenses",
                    SectionTitle = "Các Thì",
                    ShortDescription = "Hành động sẽ hoàn tất trước một thời điểm hoặc hành động khác trong tương lai.",
                    Icon = "bi-flag-fill",
                    IconBgColor = "#047857",
                    DifficultyLevel = "Trung cấp",
                    ReadTimeMinutes = 6,
                    OrderIndex = 11,
                    FormulaPreview = "S + will have + V3/ed",
                    SkillTarget = "IELTS Writing Task 1 Dự báo tương lai",
                    Tags = new() { "thì", "tenses", "tương lai hoàn thành", "future perfect" },
                    ConceptExplanation = "Thì tương lai hoàn thành (Future Perfect Tense) diễn tả một hành động hoặc mục tiêu sẽ được hoàn thành trước một mốc thời gian hoặc trước một hành động khác trong tương lai.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Khẳng định (+)", Formula = "S + will have + V3/ed + O", ColorVariant = "blue", Breakdown = new() { "will have + V3/ed cho mọi ngôi" } },
                        new GrammarFormulaBlock { Type = "Phủ định (-)", Formula = "S + won't have + V3/ed + O", ColorVariant = "red", Breakdown = new() { "won't have + V3/ed" } },
                        new GrammarFormulaBlock { Type = "Nghi vấn (?)", Formula = "Will + S + have + V3/ed + O?", ColorVariant = "green", Breakdown = new() { "Will + S + have + V3/ed?" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Cấu trúc By + Mốc tương lai", Example = "By 2030, solar power will have replaced coal.", Note = "Dấu hiệu kinh điển nhất trong biểu đồ Task 1 dự báo tương lai" },
                        new GrammarRuleTableItem { Rule = "Cấu trúc By the time + Present Simple", Example = "By the time you return, we will have completed the report.", Note = "Hành động hoàn thành trước chia Future Perfect" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Hành động hoàn thành trước thời điểm tương lai",
                            Explanation = "Đặc biệt hữu ích cho biểu đồ xu hướng tương lai trong Task 1.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "By 2040, renewable sources will have supplied over 70% of total national energy demand.", Vietnamese = "Đến năm 2040, các nguồn năng lượng tái tạo sẽ đã cung cấp hơn 70% tổng nhu cầu năng lượng quốc gia." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Hành động hoàn thành trước một sự kiện tương lai khác",
                            Explanation = "Xác lập mốc hoàn thành cho các dự án và kế hoạch.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "By the time the new semester begins, the faculty will have installed state-of-the-art laboratory facilities.", Vietnamese = "Trước khi học kỳ mới bắt đầu, nhà trường sẽ đã lắp đặt các trang thiết bị phòng thí nghiệm hiện đại." },
                            }
                        },
                    },
                    SignalWords = new() { "by + future time (by 2030)", "by the time", "by the end of this year", "before next month" },
                    SignalWordPlacementRule = "Cụm từ 'By + mốc thời gian tương lai' thường đứng đầu câu đi kèm dấu phẩy.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Dùng thì tương lai đơn sau cụm từ 'By + mốc tương lai'",
                            WrongExample = "By 2035, the population will reach 9 billion.",
                            CorrectExample = "By 2035, the population will have reached 9 billion.",
                            Explanation = "Có 'By + mốc tương lai' nhấn mạnh việc hoàn thành hoặc đạt đến trước thời điểm đó, phải dùng Future Perfect."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Dùng thì tương lai sau 'By the time'",
                            WrongExample = "By the time he will graduate, he will have published two papers.",
                            CorrectExample = "By the time he graduates, he will have published two papers.",
                            Explanation = "Sau 'By the time' chỉ dùng thì Hiện tại đơn, không dùng will."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "According to official projections, the urbanization rate _______ 65% by the year 2050.",
                            Options = new() { "will have exceeded", "exceeds", "will be exceeding", "is exceeding to have" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "will have exceeded",
                            Explanation = "Có mốc 'by the year 2050' báo hiệu hành động hoàn tất trước thời điểm tương lai → thì Tương lai hoàn thành."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "By the time you finish your doctoral degree, you _______ at this laboratory for five years.",
                            Options = new() { "will have worked", "will work", "work", "are working" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "will have worked",
                            Explanation = "Mệnh đề chính đi với 'By the time + Present Simple' chia Future Perfect: 'will have worked'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Which sentence correctly predicts an IELTS Task 1 trend ending in 2035?",
                            Options = new() { "By 2035, coal consumption will have dropped to negligible levels.", "By 2035, coal consumption dropped to negligible levels.", "By 2035, coal consumption is dropping to negligible levels.", "By 2035, coal consumption will dropping to negligible levels." },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "By 2035, coal consumption will have dropped to negligible levels.",
                            Explanation = "Cấu trúc chuẩn Task 1: 'By [năm tương lai], S + will have + V3/ed'."
                        },
                    },
                    LearningTip = "Khi biểu đồ IELTS Task 1 có trục hoành kéo dài đến năm 2030 hoặc 2050, một câu Future Perfect trong thân bài hoặc kết bài sẽ bảo đảm điểm Grammatical Range Band 8.0+!",
                    RelatedBandStructureCodes = new() { "ADV_TENSE_03" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "thi-tuong-lai-hoan-thanh-tiep-dien",
                    Title = "Thì Tương Lai Hoàn Thành Tiếp Diễn",
                    EnglishTitle = "Future Perfect Continuous Tense",
                    SectionKey = "tenses",
                    SectionTitle = "Các Thì",
                    ShortDescription = "Hành động diễn ra liên tục cho tới một thời điểm hoặc sự kiện nhất định trong tương lai.",
                    Icon = "bi-infinity",
                    IconBgColor = "#065f46",
                    DifficultyLevel = "Nâng cao",
                    ReadTimeMinutes = 6,
                    OrderIndex = 12,
                    FormulaPreview = "S + will have been + V-ing",
                    SkillTarget = "Reading Phức & Band 8.5 Grammar",
                    Tags = new() { "thì", "tenses", "tương lai hoàn thành tiếp diễn", "future perfect continuous" },
                    ConceptExplanation = "Thì tương lai hoàn thành tiếp diễn nhấn mạnh vào khoảng thời gian tiếp diễn liên tục không ngắt quãng của một hành động tính tới một mốc cụ thể trong tương lai.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Khẳng định (+)", Formula = "S + will have been + V-ing + O", ColorVariant = "blue", Breakdown = new() { "will have been + V-ing cho mọi ngôi" } },
                        new GrammarFormulaBlock { Type = "Phủ định (-)", Formula = "S + won't have been + V-ing + O", ColorVariant = "red", Breakdown = new() { "won't have been + V-ing" } },
                        new GrammarFormulaBlock { Type = "Nghi vấn (?)", Formula = "Will + S + have been + V-ing + O?", ColorVariant = "green", Breakdown = new() { "Will + S + have been + V-ing?" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Nhấn mạnh độ dài thời gian liên tục tính đến mốc tương lai", Example = "By next month, I will have been teaching for 10 years.", Note = "Kết hợp cả 'by + mốc thời gian' và 'for + khoảng thời gian'" },
                        new GrammarRuleTableItem { Rule = "Không dùng cho động từ trạng thái", Example = "Dùng Future Perfect Simple thay thế", Note = "Ví dụ: will have known thay vì will have been knowing" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Hành động liên tục tính đến một mốc tương lai",
                            Explanation = "Nhấn mạnh vào kỷ lục thời gian hoặc sự kiên trì bền bỉ.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "By October next year, the chief engineer will have been leading the aerospace program for a decade.", Vietnamese = "Tính đến tháng Mười năm sau, kỹ sư trưởng sẽ đã lãnh đạo chương trình hàng không vũ trụ này tròn một thập kỷ." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Nguyên nhân kéo dài dẫn đến trạng thái tương lai",
                            Explanation = "Giải thích trạng thái tương lai do một hành động diễn ra liên tục gây ra.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "When they arrive in London, they will be exhausted because they will have been travelling for over 20 hours.", Vietnamese = "Khi đến London, họ sẽ kiệt sức vì sẽ đã di chuyển liên tục suốt hơn 20 tiếng đồng hồ." },
                            }
                        },
                    },
                    SignalWords = new() { "by then", "by next year", "for... by the time", "by the end of this decade" },
                    SignalWordPlacementRule = "Cụm 'for + khoảng thời gian' thường đi sau động từ, cụm 'by + mốc' đứng ở đầu hoặc cuối câu.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Quên 'been' trong cấu trúc will have been V-ing",
                            WrongExample = "By December, he will have living here for 5 years.",
                            CorrectExample = "By December, he will have been living here for 5 years.",
                            Explanation = "Cấu trúc bắt buộc phải có cả 'have' và 'been': will have been + V-ing."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Dùng thì tiếp diễn khi nêu số lượng kết quả",
                            WrongExample = "By 2030, the factory will have been producing 1 million units.",
                            CorrectExample = "By 2030, the factory will have produced 1 million units.",
                            Explanation = "Đạt mốc số lượng cụ thể (1 million units) phải dùng Future Perfect Simple."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "By next autumn, Professor Williams _______ geological research in Antarctica for twenty-five consecutive years.",
                            Options = new() { "will have been conducting", "will conduct", "is conducting", "will have conducted to be" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "will have been conducting",
                            Explanation = "Nhấn mạnh quá trình nghiên cứu địa chất liên tục suốt 25 năm tính đến mốc mùa thu năm sau."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "When the project concludes in December, the team _______ on this prototype for eighteen months.",
                            Options = new() { "will have been working", "will work", "are working", "have worked" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "will have been working",
                            Explanation = "Có thời lượng kéo dài 'for eighteen months' tính đến mốc dự án kết thúc trong tương lai."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Why is 'By next month, I will have been knowing him for 10 years' incorrect?",
                            Options = new() { "Because 'know' is a stative verb and cannot be used in continuous tenses.", "Because 'for 10 years' cannot be used with future tenses.", "Because 'by next month' requires Simple Future.", "Because 'knowing' must take 'to know'." },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "Because 'know' is a stative verb and cannot be used in continuous tenses.",
                            Explanation = "'know' là động từ trạng thái (stative verb), bắt buộc dùng Future Perfect Simple: 'will have known'."
                        },
                    },
                    LearningTip = "Đây là thì hiếm gặp nhất nhưng có giá trị phân loại cao nhất. Dùng đúng thì này trong bài luận sẽ chứng minh năng lực ngữ pháp ở cấp độ gần như người bản xứ (Band 8.5+)! ",
                    RelatedBandStructureCodes = new() { "ADV_TENSE_04" },
                },
            }
        };
    }
}
