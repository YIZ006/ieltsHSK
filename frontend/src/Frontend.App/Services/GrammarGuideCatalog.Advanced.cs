using Frontend.App.Models;

namespace Frontend.App.Services;

public static partial class GrammarGuideCatalog
{
    private static GrammarGuideSectionDto BuildAdvancedTopicsSection()
    {
        return new GrammarGuideSectionDto
        {
            Key = "advanced",
            Overline = "MODULE 04 · NGỮ PHÁP HỌC THUẬT NÂNG CAO",
            Title = "Ngữ Pháp Học Thuật Nâng Cao (Band 7.0 – 8.5+)",
            Description = "18 chủ điểm cấu trúc phức tạp nhất để bứt phá band điểm Grammatical Range: câu điều kiện hỗn hợp, đảo ngữ, câu chẻ, bị động khách quan, hedging...",
            Icon = "bi-mortarboard-fill",
            ColorTheme = "#4338ca",
            Topics = new List<GrammarGuideTopicDto>
            {
                new GrammarGuideTopicDto
                {
                    Slug = "cau-dieu-kien-loai-0",
                    Title = "Câu Điều Kiện Loại 0",
                    EnglishTitle = "Zero Conditional (General Truths & Scientific Facts)",
                    SectionKey = "advanced",
                    SectionTitle = "Ngữ Pháp Nâng Cao",
                    ShortDescription = "Diễn đạt chân lý hiển nhiên, quy luật vật lý và phản xạ tự nhiên trong nghiên cứu khoa học.",
                    Icon = "bi-shield-check",
                    IconBgColor = "#0284c7",
                    DifficultyLevel = "Cơ bản",
                    ReadTimeMinutes = 5,
                    OrderIndex = 1,
                    FormulaPreview = "If + S + V(s/es), S + V(s/es)",
                    SkillTarget = "Scientific Description Task 1 & 2",
                    Tags = new() { "nâng cao", "advanced", "câu điều kiện", "conditional 0" },
                    ConceptExplanation = "Câu điều kiện loại 0 (Zero Conditional) dùng để diễn tả các sự thật hiển nhiên, chân lý khoa học, hoặc thói quen xảy ra tự động khi có điều kiện tương ứng.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Công thức chuẩn", Formula = "If / When + S + V(s/es), S + V(s/es)", ColorVariant = "blue", Breakdown = new() { "Cả hai mệnh đề đều chia ở thì Hiện tại đơn", "'When' hoàn toàn có thể thay thế 'If'" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Thay thế If bằng When", Example = "When ambient temperatures plummet below freezing, water solidifies.", Note = "Mang tính quy luật hiển nhiên 100%" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Miêu tả nguyên lý vận hành trong quy trình sản xuất (Task 1 Process)",
                            Explanation = "Nêu rõ phản ứng tự nhiên của vật liệu hoặc cơ chế.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "When heat is applied to the chemical mixture, it dissolves instantly.", Vietnamese = "Khi nhiệt độ được tác động vào hỗn hợp hóa chất, nó lập tức tan chảy." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Khẳng định mối quan hệ nhân quả tất yếu trong Writing Task 2",
                            Explanation = "Đưa ra các sự thật tâm lý hoặc quy luật kinh tế học.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "If prices soar uncontrollably, consumer purchasing power contracts automatically.", Vietnamese = "Nếu giá cả leo thang mất kiểm soát, sức mua của người tiêu dùng sẽ tự động co hẹp." },
                            }
                        },
                    },
                    SignalWords = new() { "if", "when", "whenever", "every time" },
                    SignalWordPlacementRule = "Mệnh đề If đứng đầu có dấu phẩy; đứng sau không cần dấu phẩy.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Dùng 'will' trong mệnh đề chính của câu điều kiện loại 0",
                            WrongExample = "If water boils, it will turn into steam.",
                            CorrectExample = "If water boils, it turns into steam.",
                            Explanation = "Chân lý khoa học hiển nhiên không dùng 'will' mà dùng Hiện tại đơn ở cả hai vế."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Chia thì quá khứ cho quy luật tự nhiên",
                            WrongExample = "When temperatures dropped, ice melted.",
                            CorrectExample = "When temperatures rise, ice melts.",
                            Explanation = "Quy luật hiển nhiên luôn dùng thì Hiện tại đơn."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "When atmospheric pressure _______ sharply, weather patterns become turbulent.",
                            Options = new() { "drops", "will drop", "dropped", "is dropping" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "drops",
                            Explanation = "Câu điều kiện loại 0 diễn tả quy luật tự nhiên, cả hai mệnh đề đều chia thì Hiện tại đơn."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Which sentence correctly articulates a scientific fact using the Zero Conditional?",
                            Options = new() { "If iron is exposed to moisture and oxygen, it rusts.", "If iron is exposed to moisture and oxygen, it will rust.", "If iron was exposed to moisture and oxygen, it rusted.", "If iron will be exposed to moisture, it rusts." },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "If iron is exposed to moisture and oxygen, it rusts.",
                            Explanation = "Quy luật hóa học luôn đúng dùng Hiện tại đơn ở cả hai vế ('is exposed' và 'rusts')."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Whenever employees feel valued, overall institutional productivity _______ significantly.",
                            Options = new() { "increases", "will increase", "increased", "increase" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "increases",
                            Explanation = "'Whenever' báo hiệu quy luật nhân quả thường trực, chủ ngữ 'productivity' số ít đi với 'increases'."
                        },
                    },
                    LearningTip = "Dùng Zero Conditional trong IELTS Task 1 khi giải thích chu trình Process khoa học để thể hiện tính khách quan tuyệt đối!",
                    RelatedBandStructureCodes = new() { "ADV_COND_00" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "cau-dieu-kien-loai-1",
                    Title = "Câu Điều Kiện Loại 1",
                    EnglishTitle = "First Conditional (Real Present & Future Possibility)",
                    SectionKey = "advanced",
                    SectionTitle = "Ngữ Pháp Nâng Cao",
                    ShortDescription = "Điều kiện có thể xảy ra ở hiện tại hoặc tương lai; cấu trúc đảo ngữ Should thay cho If ở Band 7.5+.",
                    Icon = "bi-signpost-split-fill",
                    IconBgColor = "#0284c7",
                    DifficultyLevel = "Cơ bản",
                    ReadTimeMinutes = 5,
                    OrderIndex = 2,
                    FormulaPreview = "If + S + V(s/es), S + will / can / may + V_inf",
                    SkillTarget = "Writing Task 2 Giải pháp & Speaking",
                    Tags = new() { "nâng cao", "advanced", "câu điều kiện", "conditional 1", "should inversion" },
                    ConceptExplanation = "Câu điều kiện loại 1 (First Conditional) diễn tả một điều kiện hoàn toàn có thật và có thể xảy ra ở hiện tại hoặc tương lai cùng kết quả tương ứng của nó. Đảo ngữ loại 1 với 'Should' là điểm nhấn Band 8.0.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Công thức cơ bản", Formula = "If + S + V(s/es), S + will / can / must + V_inf", ColorVariant = "blue", Breakdown = new() { "Mệnh đề If chia Hiện tại đơn, mệnh đề chính dùng Modal + V_inf" } },
                        new GrammarFormulaBlock { Type = "Đảo ngữ Loại 1 (Band 8.0 Inversion)", Formula = "Should + S + (not) + V_inf, S + will + V_inf", ColorVariant = "purple", Breakdown = new() { "Thay If bằng Should và đưa động từ về nguyên thể: Should governments intervene..." } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Unless = If not", Example = "Unless decisive action is taken, the ecosystem will collapse.", Note = "Mệnh đề sau Unless luôn ở thể khẳng định" },
                        new GrammarRuleTableItem { Rule = "Đảo ngữ loại 1 với Should", Example = "Should inflation accelerate, central banks will hike interest rates.", Note = "Không dùng 'If' khi đã dùng 'Should' đảo ngữ" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Đề xuất giải pháp và hệ quả trong IELTS Writing Task 2",
                            Explanation = "Chứng minh tính khả thi của chính sách đề xuất.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "If municipal authorities subsidize public transport, traffic congestion will diminish significantly.", Vietnamese = "Nếu chính quyền thành phố trợ cấp giao thông công cộng, ùn tắc giao thông sẽ giảm đáng kể." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Đảo ngữ trang trọng cho thư tín hoặc bài luận phản biện",
                            Explanation = "Tăng cường tính học thuật và sự trang trọng.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Should the proposal receive parliamentary approval, implementation will begin immediately.", Vietnamese = "Nếu bản đề xuất nhận được sự phê chuẩn của quốc hội, việc triển khai sẽ bắt đầu ngay lập tức." },
                            }
                        },
                    },
                    SignalWords = new() { "if", "unless", "provided that", "as long as", "should + S + V" },
                    SignalWordPlacementRule = "Mệnh đề điều kiện có thể đứng trước hoặc sau mệnh đề kết quả.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Dùng 'will' trong mệnh đề If",
                            WrongExample = "If the government will subsidize solar energy, costs will fall.",
                            CorrectExample = "If the government subsidizes solar energy, costs will fall.",
                            Explanation = "Tuyệt đối không dùng 'will' trong mệnh đề If của câu điều kiện loại 1."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Dùng phủ định kép sau 'Unless'",
                            WrongExample = "Unless decisive action is not taken, pollution will worsen.",
                            CorrectExample = "Unless decisive action is taken, pollution will worsen.",
                            Explanation = "'Unless' đã mang nghĩa phủ định, mệnh đề theo sau phải ở dạng khẳng định."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "_______ renewable subsidies be curtailed, investment in green tech will decline precipitously.",
                            Options = new() { "Should", "If", "Were", "Had" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "Should",
                            Explanation = "Đảo ngữ điều kiện loại 1 dùng 'Should + S + V_inf' thay cho 'If + S + V(s/es)'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Biodiversity will suffer irreversible losses _______ urgent conservation protocols are enacted.",
                            Options = new() { "unless", "if not", "provided", "as soon as" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "unless",
                            Explanation = "'unless' = 'if not' (trừ khi / nếu không), thể hiện điều kiện phòng ngừa thảm họa sinh thái."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "If urban infrastructure _______ to modern standards, living conditions will improve.",
                            Options = new() { "is upgraded", "will be upgraded", "upgrades", "was upgraded" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "is upgraded",
                            Explanation = "Mệnh đề If câu điều kiện loại 1 chia ở Hiện tại đơn bị động ('is upgraded')."
                        },
                    },
                    LearningTip = "Thay vì viết 'If governments take action', hãy viết 'Should governments take decisive action' để gây ấn tượng mạnh với giám khảo ngay trong câu giải pháp Task 2!",
                    RelatedBandStructureCodes = new() { "ADV_COND_01" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "cau-dieu-kien-loai-2",
                    Title = "Câu Điều Kiện Loại 2",
                    EnglishTitle = "Second Conditional (Unreal Present / Future & Were Inversion)",
                    SectionKey = "advanced",
                    SectionTitle = "Ngữ Pháp Nâng Cao",
                    ShortDescription = "Giả định trái ngược với thực tế ở hiện tại; dùng 'were' cho mọi ngôi và đảo ngữ Were Band 8.0.",
                    Icon = "bi-shuffle",
                    IconBgColor = "#0ea5e9",
                    DifficultyLevel = "Trung cấp",
                    ReadTimeMinutes = 6,
                    OrderIndex = 3,
                    FormulaPreview = "If + S + V2/were, S + would / could + V_inf",
                    SkillTarget = "Speaking Part 2/3 & Writing Task 2",
                    Tags = new() { "nâng cao", "advanced", "câu điều kiện", "conditional 2", "were inversion" },
                    ConceptExplanation = "Câu điều kiện loại 2 (Second Conditional) giả định một tình huống không có thật hoặc trái ngược với thực tế ở hiện tại. Động từ to be luôn dùng 'were' cho mọi ngôi trong văn phong học thuật.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Công thức cơ bản", Formula = "If + S + V2/ed (To be: were), S + would / could / might + V_inf", ColorVariant = "blue", Breakdown = new() { "If I were you..., If society had unlimited funds..." } },
                        new GrammarFormulaBlock { Type = "Đảo ngữ Loại 2 (Were Inversion)", Formula = "Were + S + (to V_inf), S + would + V_inf", ColorVariant = "purple", Breakdown = new() { "Were the government to invest billions, the benefits would be immense." } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Luôn dùng WERE cho mọi ngôi", Example = "If he were more qualified, he would secure the position.", Note = "Trong văn viết học thuật, 'was' bị coi là văn nói thân mật" },
                        new GrammarRuleTableItem { Rule = "Đảo ngữ Were + S + to V", Example = "Were countries to eliminate trade barriers, global commerce would flourish.", Note = "Cấu trúc giả định tương lai tinh tế" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Phản biện một giả thuyết không tưởng trong Writing Task 2",
                            Explanation = "Bác bỏ luận điểm đối phương bằng cách chỉ ra hệ quả phi thực tế nếu điều đó xảy ra.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "If higher education were entirely cost-free, universities would struggle to maintain academic excellence.", Vietnamese = "Nếu giáo dục đại học hoàn toàn miễn phí, các trường đại học sẽ phải chật vật để duy trì chất lượng học thuật." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Trình bày các mong muốn giả định trong Speaking Part 2 & 3",
                            Explanation = "Trả lời các câu hỏi tưởng tượng: 'If you could change one law...'.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "If I had the authority to reshape urban transit, I would establish dedicated cycling expressways.", Vietnamese = "Nếu tôi có thẩm quyền tái cấu trúc giao thông đô thị, tôi sẽ thiết lập các tuyến cao tốc riêng cho xe đạp." },
                            }
                        },
                    },
                    SignalWords = new() { "if I were", "were to", "would rather", "as if", "supposing" },
                    SignalWordPlacementRule = "Mệnh đề đảo ngữ 'Were + S + to V' thường đứng đầu câu để nhấn mạnh giả định.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Dùng 'was' thay vì 'were' trong văn viết IELTS",
                            WrongExample = "If the government was more proactive, the crisis would abate.",
                            CorrectExample = "If the government were more proactive, the crisis would abate.",
                            Explanation = "Trong văn viết học thuật trang trọng, thể giả định (Subjunctive) bắt buộc dùng 'were' cho tất cả các ngôi."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Dùng 'would' ngay trong mệnh đề If",
                            WrongExample = "If he would study harder, he would pass.",
                            CorrectExample = "If he studied harder, he would pass.",
                            Explanation = "Mệnh đề If chỉ chia thì quá khứ đơn (V2/ed), không chứa 'would'."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "_______ the international community to allocate adequate financial reserves, famine could be eradicated.",
                            Options = new() { "Were", "Had", "Should", "If were" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "Were",
                            Explanation = "Cấu trúc đảo ngữ câu điều kiện loại 2 với động từ thường: 'Were + S + to V_inf'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "If every nation _______ stringent deforestation quotas, biodiversity loss would decelerate noticeably.",
                            Options = new() { "enacted", "enacts", "will enact", "would enact" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "enacted",
                            Explanation = "Mệnh đề If câu điều kiện loại 2 chia ở Quá khứ đơn (Past Subjunctive) → 'enacted'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Which sentence demonstrates the most formal academic tone?",
                            Options = new() { "If the project were financially viable, stakeholders would support it.", "If the project was financially viable, stakeholders would support it.", "If the project would be financially viable, stakeholders supported it.", "If the project is financially viable, stakeholders would support it." },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "If the project were financially viable, stakeholders would support it.",
                            Explanation = "Dùng 'were' cho chủ ngữ số ít 'the project' là tiêu chuẩn của phong cách văn bản học thuật."
                        },
                    },
                    LearningTip = "Sử dụng cấu trúc 'Were S + to V, S + would...' khi bàn luận về các kịch bản chính sách trong Task 2 để gây ấn tượng mạnh về trình độ kiểm soát ngữ pháp phức!",
                    RelatedBandStructureCodes = new() { "ADV_COND_02" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "cau-dieu-kien-loai-3",
                    Title = "Câu Điều Kiện Loại 3",
                    EnglishTitle = "Third Conditional (Unreal Past & Had Inversion)",
                    SectionKey = "advanced",
                    SectionTitle = "Ngữ Pháp Nâng Cao",
                    ShortDescription = "Giả định điều trái ngược với sự thật trong quá khứ; cấu trúc đảo ngữ Had + S + V3 Band 8.5.",
                    Icon = "bi-clock-history",
                    IconBgColor = "#10b981",
                    DifficultyLevel = "Nâng cao",
                    ReadTimeMinutes = 6,
                    OrderIndex = 4,
                    FormulaPreview = "If + S + had + V3/ed, S + would have + V3/ed",
                    SkillTarget = "Reading Analysis & Writing Task 2",
                    Tags = new() { "nâng cao", "advanced", "câu điều kiện", "conditional 3", "had inversion" },
                    ConceptExplanation = "Câu điều kiện loại 3 (Third Conditional) giả định một điều trái ngược với những gì đã thực sự diễn ra trong quá khứ. Đây là cấu trúc hoàn hảo để phân tích nguyên nhân - bài học lịch sử hoặc đánh giá chính sách đã qua.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Công thức chuẩn", Formula = "If + S + had + V3/ed, S + would / could / might have + V3/ed", ColorVariant = "blue", Breakdown = new() { "If researchers had recognized the flaw earlier, the catastrophe would have been averted." } },
                        new GrammarFormulaBlock { Type = "Đảo ngữ Loại 3 (Had Inversion)", Formula = "Had + S + (not) + V3/ed, S + would have + V3/ed", ColorVariant = "purple", Breakdown = new() { "Bỏ 'If', đảo 'Had' lên trước chủ ngữ: Had the authorities intervened..." } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Cấu trúc But for / Without", Example = "But for the timely bailout, the financial system would have collapsed.", Note = "But for / Without + Noun Phrase thay thế cho mệnh đề If" },
                        new GrammarRuleTableItem { Rule = "Đảo ngữ phủ định loại 3", Example = "Had it not been for their dedication, the mission would have failed.", Note = "Had it not been for = Nếu không nhờ có" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Phân tích bài học lịch sử và đánh giá chính sách quá khứ",
                            Explanation = "Đánh giá tác động của những quyết định trong quá khứ đối với cục diện hiện tại.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Had emergency responders arrived sooner, numerous casualties would have been prevented.", Vietnamese = "Nếu lực lượng cứu hộ khẩn cấp đến sớm hơn, nhiều thương vong đã có thể được ngăn chặn." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Sử dụng cấu trúc 'Had it not been for' để biểu đạt lòng biết ơn hoặc nguyên nhân cốt lõi",
                            Explanation = "Cấu trúc đẳng cấp Band 8.5 cho Writing và Speaking.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Had it not been for massive government subsidies, the green energy industry would not have survived the recession.", Vietnamese = "Nếu không nhờ có các khoản trợ cấp khổng lồ của chính phủ, ngành năng lượng xanh đã không thể sống sót qua cuộc suy thoái." },
                            }
                        },
                    },
                    SignalWords = new() { "had + V3", "would have + V3", "had it not been for", "but for", "without" },
                    SignalWordPlacementRule = "Cấu trúc 'Had it not been for + Noun' thường đứng đầu câu để làm rõ điều kiện tiên quyết.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Dùng 'would have' trong mệnh đề If",
                            WrongExample = "If they would have known the risks, they would have halted the trial.",
                            CorrectExample = "If they had known the risks, they would have halted the trial.",
                            Explanation = "Mệnh đề If của câu điều kiện loại 3 dùng Past Perfect (had + V3), tuyệt đối không dùng 'would have'."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Quên 'have' trong mệnh đề kết quả",
                            WrongExample = "Had the warnings been heeded, the flood had not damaged homes.",
                            CorrectExample = "Had the warnings been heeded, the flood would not have damaged homes.",
                            Explanation = "Mệnh đề kết quả bắt buộc phải có dạng 'would have + V3'."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "_______ the regulatory agency conducted rigorous environmental inspections, the toxic spill would have been averted.",
                            Options = new() { "Had", "Were", "Should", "If had" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "Had",
                            Explanation = "Cấu trúc đảo ngữ câu điều kiện loại 3: 'Had + S + V3/ed' thay cho 'If + S + had + V3/ed'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Had it not been for financial donations, the community center _______ its doors permanently.",
                            Options = new() { "would have closed", "would close", "had closed", "closed" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "would have closed",
                            Explanation = "'Had it not been for...' giả định quá khứ, mệnh đề chính dùng 'would have + V3'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "If the historical documents _______ properly preserved, scholars would have deciphered the ancient dialect.",
                            Options = new() { "had been", "were", "have been", "would have been" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "had been",
                            Explanation = "Mệnh đề If câu điều kiện loại 3 chia Quá khứ hoàn thành bị động: 'had been properly preserved'."
                        },
                    },
                    LearningTip = "Cụm 'Had it not been for X, Y would not have materialized' là một trong những vũ khí tối thượng để chốt hạ đoạn văn phân tích nguyên nhân quá khứ trong Task 2!",
                    RelatedBandStructureCodes = new() { "ADV_COND_03" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "cau-dieu-kien-hon-hop",
                    Title = "Câu Điều Kiện Hỗn Hợp",
                    EnglishTitle = "Mixed Conditionals (Type 3 + Type 2 & Type 2 + Type 3)",
                    SectionKey = "advanced",
                    SectionTitle = "Ngữ Pháp Nâng Cao",
                    ShortDescription = "Kết hợp quá khứ và hiện tại: Quá khứ ảnh hưởng kết quả hiện tại, hoặc bản chất hiện tại chi phối hành động quá khứ.",
                    Icon = "bi-shuffle",
                    IconBgColor = "#059669",
                    DifficultyLevel = "Nâng cao",
                    ReadTimeMinutes = 7,
                    OrderIndex = 5,
                    FormulaPreview = "If + S + had V3, S + would V_inf (Past cause → Present result)",
                    SkillTarget = "Band 8.0 - 8.5 Grammatical Range",
                    Tags = new() { "nâng cao", "advanced", "câu điều kiện", "mixed conditional" },
                    ConceptExplanation = "Câu điều kiện hỗn hợp (Mixed Conditionals) xảy ra khi thời gian của mệnh đề điều kiện và mệnh đề kết quả không trùng khớp nhau. Phổ biến nhất là: Giả định quá khứ dẫn đến kết quả ở hiện tại (Type 3 + Type 2), hoặc Bản chất hiện tại ảnh hưởng đến hành động trong quá khứ (Type 2 + Type 3).",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Hỗn hợp 1: Quá khứ → Hiện tại (Phổ biến nhất)", Formula = "If + S + had + V3/ed, S + would / could + V_inf (now)", ColorVariant = "blue", Breakdown = new() { "If I had accepted the scholarship last year, I would be studying in London now." } },
                        new GrammarFormulaBlock { Type = "Hỗn hợp 2: Hiện tại → Quá khứ", Formula = "If + S + V2/were, S + would have + V3/ed", ColorVariant = "purple", Breakdown = new() { "If he were not so arrogant, he would have listened to their advice yesterday." } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Dấu hiệu thời gian ở hai mệnh đề", Example = "Mệnh đề If có 'yesterday / last year', mệnh đề chính có 'now / today'", Note = "Căn cứ vào từ chỉ thời gian để phối hợp thì chính xác" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Bàn luận về ảnh hưởng lâu dài của chính sách quá khứ lên hiện tại",
                            Explanation = "Rất hữu ích khi viết về các quyết định lịch sử hoặc quy hoạch đô thị.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "If urban planners had anticipated population growth three decades ago, traffic gridlock would not be such a chronic issue today.", Vietnamese = "Nếu các nhà quy hoạch đô thị dự đoán được sự gia tăng dân số 3 thập kỷ trước, ùn tắc giao thông đã không trở thành một vấn đề nan giải ngày nay." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Chỉ ra cách phẩm chất hiện tại tác động đến hành động quá khứ",
                            Explanation = "Đánh giá tính cách hoặc đặc tính cố hữu của tổ chức.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "If the administration were truly dedicated to transparency, they would have disclosed the audit figures last month.", Vietnamese = "Nếu ban quản lý thực sự tận tâm với sự minh bạch, họ đã công khai các số liệu kiểm toán vào tháng trước." },
                            }
                        },
                    },
                    SignalWords = new() { "had V3... would V (now)", "were... would have V3 (yesterday)" },
                    SignalWordPlacementRule = "Thường có các từ tín hiệu 'now', 'currently', 'today' ở mệnh đề kết quả để nhấn mạnh tính chất hỗn hợp.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Tự động ghép 'would have + V3' khi mệnh đề If có Past Perfect dù có chữ 'now'",
                            WrongExample = "If they had invested early, they would have been market leaders today.",
                            CorrectExample = "If they had invested early, they would be market leaders today.",
                            Explanation = "Vì có từ 'today' (kết quả ở hiện tại) nên mệnh đề chính bắt buộc phải dùng 'would + V_inf' (Type 2)."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Nhầm lẫn chiều tác động thời gian giữa quá khứ và hiện tại",
                            WrongExample = "If I passed the exam yesterday, I would celebrate now.",
                            CorrectExample = "If I had passed the exam yesterday, I would celebrate now.",
                            Explanation = "Hành động thi xảy ra trong quá khứ nên phải dùng Past Perfect ('had passed')."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "If the municipal council _______ adequate reserves a decade ago, our city would not face severe water rationing today.",
                            Options = new() { "had constructed", "constructed", "would construct", "constructs" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "had constructed",
                            Explanation = "Hành động xây dựng xảy ra trong quá khứ ('a decade ago') dẫn đến kết quả ở hiện tại ('today') → mệnh đề If chia 'had constructed'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "If Professor Harrison were not so dedicated to research, he _______ the prestigious administrative post last year.",
                            Options = new() { "would have accepted", "would accept", "accepted", "will accept" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "would have accepted",
                            Explanation = "Bản chất hiện tại ('If he were not so dedicated') tác động tới quyết định trong quá khứ ('last year') → mệnh đề chính chia 'would have accepted'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Which of the following demonstrates a flawless mixed conditional sentence?",
                            Options = new() { "If she had taken the medication yesterday, she would feel much better now.", "If she took the medication yesterday, she would feel much better now.", "If she had taken the medication yesterday, she would have felt much better now.", "If she takes the medication yesterday, she feels much better now." },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "If she had taken the medication yesterday, she would feel much better now.",
                            Explanation = "Quá khứ ('yesterday' → had taken) kết hợp hiện tại ('now' → would feel)."
                        },
                    },
                    LearningTip = "Sử dụng đúng một câu điều kiện hỗn hợp trong bài Task 2 là minh chứng rõ ràng nhất cho thấy bạn làm chủ được ngữ pháp ở trình độ Band 8.5+!",
                    RelatedBandStructureCodes = new() { "ADV_COND_04" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "cau-bi-dong",
                    Title = "Câu Bị Động Khách Quan",
                    EnglishTitle = "Impersonal Passive & Academic Impersonality",
                    SectionKey = "advanced",
                    SectionTitle = "Ngữ Pháp Nâng Cao",
                    ShortDescription = "Bị động khách quan với It is said that / S is thought to be... tạo văn phong học thuật chuẩn mực.",
                    Icon = "bi-arrow-left-right",
                    IconBgColor = "#4338ca",
                    DifficultyLevel = "Trung cấp",
                    ReadTimeMinutes = 6,
                    OrderIndex = 6,
                    FormulaPreview = "It is widely believed that... | S + is reported to + V_inf",
                    SkillTarget = "IELTS Writing Task 2 Cốt lõi",
                    Tags = new() { "nâng cao", "advanced", "bị động", "impersonal passive", "passive voice" },
                    ConceptExplanation = "Câu bị động khách quan (Impersonal Passive) là cấu trúc học thuật kinh điển giúp người viết trình bày quan điểm chung của dư luận hoặc giới chuyên môn mà không cần viện dẫn ngôi thứ nhất (I / We). Cực kỳ phổ biến trong các đề bài Task 2 dạng 'Some people think...'.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Dạng 1: Chủ ngữ giả 'It'", Formula = "It is widely believed / argued / recognized / acknowledged that + S + V", ColorVariant = "blue", Breakdown = new() { "It is estimated that renewable sources will dominate by 2040." } },
                        new GrammarFormulaBlock { Type = "Dạng 2: Biến chủ ngữ của mệnh đề con thành chủ ngữ chính", Formula = "S + is/are said / believed / thought + to V_inf (cùng thì) / to have V3 (trước thì)", ColorVariant = "purple", Breakdown = new() { "Deforestation is acknowledged to exacerbate climate instability." } },
                        new GrammarFormulaBlock { Type = "Dạng 3: Bị động của động từ giác quan & mệnh lệnh", Formula = "make / see / hear ở bị động bắt buộc có 'to'", ColorVariant = "green", Breakdown = new() { "He was made to resign (Chủ động: made him resign)" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Chuyển đổi thì trong cấu trúc 'to have V3'", Example = "People believe he stole the money → He is believed TO HAVE STOLEN the money.", Note = "Hành động trộm xảy ra trước thời điểm tin tưởng nên dùng 'to have V3'" },
                        new GrammarRuleTableItem { Rule = "Động từ nội động từ không dùng bị động", Example = "happen, occur, die, appear, deteriorate", Note = "Tuyệt đối không viết 'was happened'" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Mở đầu các đoạn thân bài Task 2 dạng Discussion",
                            Explanation = "Trình bày luận điểm của một luồng ý kiến mà không mang tính chủ quan cá nhân.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "It is frequently asserted that technical vocational schools provide greater economic utility than universities.", Vietnamese = "Người ta thường khẳng định rằng các trường dạy nghề kỹ thuật mang lại giá trị kinh tế lớn hơn so với các trường đại học." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Mô tả quy trình sản xuất (Process) Task 1",
                            Explanation = "Nhấn mạnh vào chuỗi hành động và sản phẩm thay vì con người.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "After being harvested, cocoa beans are fermented, dried in the sun, and transported to processing factories.", Vietnamese = "Sau khi được thu hoạch, hạt ca cao được lên men, phơi khô dưới ánh nắng mặt trời và vận chuyển đến các nhà máy chế biến." },
                            }
                        },
                    },
                    SignalWords = new() { "it is believed that", "it is argued that", "it is widely acknowledged that", "is reported to", "is estimated to" },
                    SignalWordPlacementRule = "Thường đặt ở câu đầu tiên (Topic Sentence) của đoạn thân bài Task 2.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Quên 'to' sau động từ 'made' ở thể bị động",
                            WrongExample = "Employees were made work overtime.",
                            CorrectExample = "Employees were made to work overtime.",
                            Explanation = "Trong thể chủ động: make someone DO something; nhưng ở thể bị động: be made TO DO something."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Dùng sai 'to V' thay vì 'to have V3' khi hành động xảy ra trước",
                            WrongExample = "The ancient temple is believed to collapse in 1200 AD.",
                            CorrectExample = "The ancient temple is believed to have collapsed in 1200 AD.",
                            Explanation = "Ngôi đền sụp đổ trong quá khứ trước thời điểm hiện tại người ta tin vào điều đó, nên phải dùng 'to have collapsed'."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "The newly discovered asteroid is calculated _______ extremely close to Earth's gravitational field next decade.",
                            Options = new() { "to pass", "passing", "passed", "to be passed" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "to pass",
                            Explanation = "Cấu trúc bị động khách quan: 'S + is calculated + to V_inf' ('to pass')."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "The legendary manuscript is rumored _______ during the turbulent library fire of 1845.",
                            Options = new() { "to have been destroyed", "to destroy", "having destroyed", "to be destroyed" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "to have been destroyed",
                            Explanation = "Hành động bị tiêu hủy xảy ra trong quá khứ (năm 1845) trước thời điểm hiện tại đang đồn đại, và mang nghĩa bị động → 'to have been destroyed'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Select the ideal academic sentence for introducing an opposing view in Task 2:",
                            Options = new() { "It is widely contended that capital punishment fails to deter violent crime.", "Some people think that capital punishment fails to deter violent crime.", "I believe that capital punishment fails to deter violent crime.", "They say that capital punishment fails to deter violent crime." },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "It is widely contended that capital punishment fails to deter violent crime.",
                            Explanation = "'It is widely contended that...' là cấu trúc bị động khách quan học thuật bậc nhất để giới thiệu luận điểm."
                        },
                    },
                    LearningTip = "Mở đầu đoạn thân bài 1 bằng 'It is widely contended that...' và mở đầu thân bài 2 bằng 'Conversely, it is recognized that...' sẽ tạo nên bố cục cân đối hoàn hảo cho bài luận Task 2!",
                    RelatedBandStructureCodes = new() { "ADV_PASS_01" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "cau-tuong-thuat",
                    Title = "Câu Tường Thuật (Reported Speech)",
                    EnglishTitle = "Reported Speech & Academic Reporting Verbs",
                    SectionKey = "advanced",
                    SectionTitle = "Ngữ Pháp Nâng Cao",
                    ShortDescription = "Quy tắc lùi thì, biến đổi trạng từ thời gian và nâng cấp động từ tường thuật học thuật (asserted, claimed, posited).",
                    Icon = "bi-chat-quote-fill",
                    IconBgColor = "#3730a3",
                    DifficultyLevel = "Trung cấp",
                    ReadTimeMinutes = 6,
                    OrderIndex = 7,
                    FormulaPreview = "S + claimed / asserted / maintained that + Clause",
                    SkillTarget = "Literature Review & Reading",
                    Tags = new() { "nâng cao", "advanced", "tường thuật", "reported speech" },
                    ConceptExplanation = "Câu tường thuật (Reported Speech) dùng để thuật lại lời nói, nhận định hoặc phát hiện của người khác. Trong văn phong học thuật, thay vì dùng 'said that', thí sinh sử dụng các reporting verbs học thuật như contend, claim, argue, assert, demonstrate.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Tường thuật nhận định học thuật", Formula = "Scholar + argued / asserted / maintained / posited that + S + V (lùi thì)", ColorVariant = "blue", Breakdown = new() { "Dr. Watson asserted that the initial findings were inconclusive." } },
                        new GrammarFormulaBlock { Type = "Động từ tường thuật đi với To-V hoặc V-ing", Formula = "advise / urge / encourage someone to V | admit / deny / apologize for + V-ing", ColorVariant = "purple", Breakdown = new() { "The committee urged governments to take decisive action." } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Không lùi thì với chân lý vĩnh cửu", Example = "Galileo declared that the Earth moves around the Sun.", Note = "Sự thật khoa học giữ nguyên thì Hiện tại đơn" },
                        new GrammarRuleTableItem { Rule = "Quy tắc đổi trạng từ thời gian và nơi chốn", Example = "now → then, today → that day, yesterday → the day before, tomorrow → the following day", Note = "Cần thận trọng trong bài đọc và viết" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Trích dẫn nghiên cứu và quan điểm của các chuyên gia",
                            Explanation = "Sử dụng động từ tường thuật đa dạng và chính xác về sắc thái.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Recent clinical trials demonstrated that regular mindfulness meditation alleviated chronic anxiety.", Vietnamese = "Các thử nghiệm lâm sàng gần đây đã chứng minh rằng thiền chánh niệm đều đặn giúp làm giảm lo âu mãn tính." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Tường thuật lời đề xuất chính sách",
                            Explanation = "Dùng các động từ recommend, propose, advocate.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Environmentalists advocated imposing carbon tariffs on heavy manufacturing imports.", Vietnamese = "Các nhà môi trường ủng hộ việc áp thuế carbon đối với hàng nhập khẩu công nghiệp nặng." },
                            }
                        },
                    },
                    SignalWords = new() { "asserted that", "claimed that", "demonstrated that", "urged to", "advocated V-ing" },
                    SignalWordPlacementRule = "Động từ tường thuật đi liền sau chủ ngữ danh xưng chuyên gia.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Lùi thì với chân lý khoa học hiển nhiên",
                            WrongExample = "The teacher said that water boiled at 100 degrees Celsius.",
                            CorrectExample = "The teacher said that water boils at 100 degrees Celsius.",
                            Explanation = "Quy luật vật lý khoa học luôn đúng, không lùi thì về quá khứ."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Dùng sai cấu trúc sau 'suggest' hoặc 'recommend'",
                            WrongExample = "He suggested me to read the academic journal.",
                            CorrectExample = "He suggested that I should read the academic journal. / He suggested reading the journal.",
                            Explanation = "'suggest' không đi với 'someone to V', mà đi với 'suggest V-ing' hoặc 'suggest that S (should) V'."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "The lead epidemiologist _______ that the mutation was substantially more virulent than earlier variants.",
                            Options = new() { "asserted", "told", "spoke", "talked" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "asserted",
                            Explanation = "'asserted that' (khẳng định rằng) là động từ tường thuật học thuật hoàn hảo cho nghiên cứu khoa học."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "The scientific panel recommended _______ international research funding for renewable battery technology.",
                            Options = new() { "increasing", "to increase", "increased", "increase to" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "increasing",
                            Explanation = "Sau động từ 'recommend' khi không có tân ngữ chỉ người, ta dùng trực tiếp danh động từ: 'recommend increasing'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Which sentence correctly preserves the tense for an immutable scientific fact?",
                            Options = new() { "The astronomer explained that the Earth orbits the Sun once every 365 days.", "The astronomer explained that the Earth orbited the Sun once every 365 days.", "The astronomer explained that the Earth would orbit the Sun once every 365 days.", "The astronomer explained that the Earth has orbited the Sun once every 365 days." },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "The astronomer explained that the Earth orbits the Sun once every 365 days.",
                            Explanation = "Chân lý thiên văn học bất biến luôn giữ nguyên ở thì Hiện tại đơn ('orbits')."
                        },
                    },
                    LearningTip = "Hạn chế dùng 'said' hay 'stated'. Hãy luân phiên sử dụng: posited, argued, contended, claimed, maintained để tăng điểm Lexical Resource!",
                    RelatedBandStructureCodes = new() { "ADV_REP_01" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "menh-de-quan-he",
                    Title = "Mệnh Đề Quan Hệ & Rút Gọn",
                    EnglishTitle = "Relative Clauses & Reduced Participle Clauses",
                    SectionKey = "advanced",
                    SectionTitle = "Ngữ Pháp Nâng Cao",
                    ShortDescription = "Mệnh đề quan hệ xác định/không xác định; kỹ thuật rút gọn thành cụm phân từ V-ing và V3/ed cực kỳ trang trọng.",
                    Icon = "bi-bezier2",
                    IconBgColor = "#312e81",
                    DifficultyLevel = "Trung cấp",
                    ReadTimeMinutes = 6,
                    OrderIndex = 8,
                    FormulaPreview = "N + V-ing / V3 (Rút gọn mệnh đề)",
                    SkillTarget = "Band 7.5+ Grammatical Range",
                    Tags = new() { "nâng cao", "advanced", "mệnh đề quan hệ", "relative clause", "reduced relative" },
                    ConceptExplanation = "Mệnh đề quan hệ (Relative Clause) bổ nghĩa cho danh từ đứng trước. Trong tiếng Anh học thuật cao cấp, kỹ thuật Rút gọn mệnh đề quan hệ (Reduced Relative Clauses) bằng Phân từ hiện tại (V-ing) hoặc Quá khứ phân từ (V3/ed) giúp câu văn trở nên cô đọng, súc tích và đĩnh đạc.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Rút gọn chủ động (Active)", Formula = "N + who/which + V → N + V-ing", ColorVariant = "blue", Breakdown = new() { "Students who seek academic excellence → Students seeking academic excellence" } },
                        new GrammarFormulaBlock { Type = "Rút gọn bị động (Passive)", Formula = "N + which/who + be + V3/ed → N + V3/ed", ColorVariant = "purple", Breakdown = new() { "The policies which were implemented last year → The policies implemented last year" } },
                        new GrammarFormulaBlock { Type = "Rút gọn chỉ mục đích hoặc thứ tự", Formula = "the first / last / only + N + To-V", ColorVariant = "green", Breakdown = new() { "Neil Armstrong was the first man to walk on the moon." } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Mệnh đề quan hệ không xác định (Non-defining)", Example = "Oxford University, which was established centuries ago, is prestigious.", Note = "Bắt buộc có dấu phẩy và TUYỆT ĐỐI không dùng 'that'" },
                        new GrammarRuleTableItem { Rule = "Đại từ 'which' thay thế cho cả mệnh đề đứng trước", Example = "He passed the test with honors, which delighted his parents.", Note = "Luôn có dấu phẩy đi trước 'which'" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Rút gọn mệnh đề để tăng mật độ thông tin học thuật trong Writing Task 2",
                            Explanation = "Giúp câu văn cô đọng mà không bị rườm rà đại từ quan hệ.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Data gathered from multiple demographic surveys illustrates a sharp urban migration trend.", Vietnamese = "Dữ liệu thu thập được từ nhiều cuộc khảo sát nhân khẩu học minh chứng cho xu hướng di cư đô thị mạnh mẽ." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Sử dụng 'which' để bình luận về hệ quả của một sự việc",
                            Explanation = "Tạo câu phức liên kết nhân quả thanh lịch.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Many companies adopted automated manufacturing, which boosted operational productivity.", Vietnamese = "Nhiều công ty đã áp dụng sản xuất tự động, điều này đã thúc đẩy năng suất vận hành." },
                            }
                        },
                    },
                    SignalWords = new() { "who", "whom", "whose", "which", "that", "V-ing phrase", "V3/ed phrase" },
                    SignalWordPlacementRule = "Cụm phân từ rút gọn đứng ngay sau danh từ mà nó bổ nghĩa.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Dùng 'that' trong mệnh đề quan hệ có dấu phẩy",
                            WrongExample = "Solar power, that is renewable, should be subsidized.",
                            CorrectExample = "Solar power, which is renewable, should be subsidized.",
                            Explanation = "Trong mệnh đề quan hệ không xác định (ngăn cách bởi dấu phẩy), không bao giờ dùng 'that'."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Nhầm lẫn giữa rút gọn chủ động (V-ing) và bị động (V3)",
                            WrongExample = "The participants selecting for the trial underwent medical screening.",
                            CorrectExample = "The participants selected for the trial underwent medical screening.",
                            Explanation = "Các tình nguyện viên được lựa chọn (bị động) nên phải dùng quá khứ phân từ 'selected'."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "The comprehensive strategies _______ by the environmental committee generated unprecedented reductions in emissions.",
                            Options = new() { "formulated", "formulating", "which formulated", "were formulated" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "formulated",
                            Explanation = "Rút gọn mệnh đề quan hệ bị động: 'The strategies (which were) formulated by...' → 'formulated'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Scholars _______ the impacts of artificial intelligence emphasize the necessity of ethical governance.",
                            Options = new() { "investigating", "investigated", "which investigate", "who investigating" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "investigating",
                            Explanation = "Rút gọn mệnh đề quan hệ chủ động: 'Scholars (who investigate)...' → 'investigating'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Government expenditures on public transport increased sharply, _______ alleviated chronic suburban congestion.",
                            Options = new() { "which", "that", "what", "whereby" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "which",
                            Explanation = "Đại từ quan hệ 'which' đứng sau dấu phẩy để thay thế cho toàn bộ mệnh đề hành động đi trước."
                        },
                    },
                    LearningTip = "Thường xuyên biến đổi các mệnh đề quan hệ dài dòng (who/which + V) thành cụm phân từ ngắn (V-ing / V3) là dấu ấn rõ ràng nhất của ngòi bút Band 8.0+!",
                    RelatedBandStructureCodes = new() { "ADV_REL_01" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "cau-che",
                    Title = "Câu Chẻ (Cleft Sentences)",
                    EnglishTitle = "Cleft Sentences (It-Cleft & Wh-Cleft for Emphasis)",
                    SectionKey = "advanced",
                    SectionTitle = "Ngữ Pháp Nâng Cao",
                    ShortDescription = "Kỹ thuật câu chẻ nhấn mạnh thành phần chủ ngữ, tân ngữ hoặc trạng ngữ: It is... that... và What... is...",
                    Icon = "bi-bullseye",
                    IconBgColor = "#1e1b4b",
                    DifficultyLevel = "Nâng cao",
                    ReadTimeMinutes = 6,
                    OrderIndex = 9,
                    FormulaPreview = "It is X that... | What S + V is...",
                    SkillTarget = "IELTS Writing Task 2 Luận điểm đanh thép",
                    Tags = new() { "nâng cao", "advanced", "câu chẻ", "cleft sentence", "nhấn mạnh" },
                    ConceptExplanation = "Câu chẻ (Cleft Sentence) tách một câu đơn thành hai mệnh đề để làm nổi bật một thành phần thông tin cụ thể (chủ ngữ, tân ngữ, hoặc trạng ngữ chỉ thời gian/nơi chốn/nguyên nhân). Đây là công cụ đắc lực để khẳng định luận điểm đanh thép trong tranh biện học thuật.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "It-Cleft (Nhấn mạnh đối tượng / nguyên nhân)", Formula = "It is / was + Thành phần nhấn mạnh + that + Mệnh đề còn lại", ColorVariant = "blue", Breakdown = new() { "It is technological innovation that drives economic prosperity." } },
                        new GrammarFormulaBlock { Type = "Wh-Cleft / Pseudo-cleft (Nhấn mạnh hành động / điều cốt lõi)", Formula = "What + S + V + is/was + Cụm danh từ / Mệnh đề", ColorVariant = "purple", Breakdown = new() { "What developing nations urgently require is technological infrastructure." } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Dùng 'that' cho mọi thành phần được nhấn mạnh", Example = "It is in metropolises THAT opportunities concentrate.", Note = "Kể cả trạng từ nơi chốn/thời gian, dùng 'that' chuẩn hơn 'where/when'" },
                        new GrammarRuleTableItem { Rule = "Hòa hợp thì giữa to be và câu gốc", Example = "It WAS in 1945 THAT the UN was founded.", Note = "Sự việc quá khứ dùng 'It was... that'" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Khẳng định nhân tố cốt lõi trong giải quyết vấn đề Task 2",
                            Explanation = "Nhấn mạnh vào nguyên nhân hoặc giải pháp duy nhất hiệu quả.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "It is comprehensive public awareness, rather than punitive legislation, that fosters enduring environmental stewardship.", Vietnamese = "Chính nhận thức sâu sắc của cộng đồng, chứ không phải pháp luật trừng phạt, mới là điều nuôi dưỡng ý thức bảo vệ môi trường lâu dài." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Tạo điểm nhấn thuyết phục trong bài nói IELTS Speaking Part 3",
                            Explanation = "Dẫn dắt câu trả lời mang tính chiều sâu suy nghĩ.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "What concerns young job seekers the most is job stability and career progression.", Vietnamese = "Điều khiến những người trẻ tìm việc quan tâm nhất chính là sự ổn định công việc và lộ trình thăng tiến." },
                            }
                        },
                    },
                    SignalWords = new() { "it is... that", "it was... that", "what S requires is", "all that S needs is" },
                    SignalWordPlacementRule = "Cấu trúc It-cleft đứng đầu câu, thành phần nhấn mạnh đặt ngay sau 'It is/was'.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Dùng 'which' thay cho 'that' trong It-cleft",
                            WrongExample = "It is education which transforms society.",
                            CorrectExample = "It is education that transforms society.",
                            Explanation = "Trong cấu trúc câu chẻ It-cleft nhấn mạnh, từ liên kết bắt buộc là 'that', không dùng 'which'."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Chia thì hiện tại 'It is' cho sự kiện đã xảy ra trong quá khứ",
                            WrongExample = "It is in the 18th century that the Industrial Revolution began.",
                            CorrectExample = "It was in the 18th century that the Industrial Revolution began.",
                            Explanation = "Sự kiện lịch sử quá khứ phải chia 'It was... that'."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "It is rigorous empirical research, rather than anecdotal evidence, _______ validates scientific theories.",
                            Options = new() { "that", "which", "what", "where" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "that",
                            Explanation = "Cấu trúc chuẩn của It-cleft sentence: 'It is + Focus + that + Verb'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "_______ the government should prioritize immediately is modernizing public hospital facilities.",
                            Options = new() { "What", "That", "It is", "Which" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "What",
                            Explanation = "Cấu trúc Wh-cleft (Pseudo-cleft): 'What + S + V + is + ...' nhấn mạnh điều cần ưu tiên."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Transform for emphasis: 'Human activities caused global warming.'",
                            Options = new() { "It was human activities that caused global warming.", "It is human activities which caused global warming.", "What caused global warming were human activities.", "It was global warming that human activities caused." },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "It was human activities that caused global warming.",
                            Explanation = "Nhấn mạnh chủ thể 'human activities' trong quá khứ: 'It was human activities that caused global warming'."
                        },
                    },
                    LearningTip = "Đặt một câu It-cleft ở câu mở đầu phần phản biện trong Task 2: 'It is X, not Y, that holds the key to...' sẽ khiến bài viết lập tức mang phong thái của một học giả!",
                    RelatedBandStructureCodes = new() { "ADV_CLEFT_01" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "dao-ngu",
                    Title = "Đảo Ngữ (Inversion)",
                    EnglishTitle = "Inversion with Negative Adverbials, Only, & Conditionals",
                    SectionKey = "advanced",
                    SectionTitle = "Ngữ Pháp Nâng Cao",
                    ShortDescription = "Đảo trợ động từ lên trước chủ ngữ khi đứng đầu câu bằng từ phủ định (Rarely, Seldom, Under no circumstances, Not only).",
                    Icon = "bi-arrow-down-up",
                    IconBgColor = "#4f46e5",
                    DifficultyLevel = "Nâng cao",
                    ReadTimeMinutes = 7,
                    OrderIndex = 10,
                    FormulaPreview = "Negative Adverb + Auxiliary + S + V",
                    SkillTarget = "Band 8.0 - 9.0 Writing Range",
                    Tags = new() { "nâng cao", "advanced", "đảo ngữ", "inversion", "negative adverbials" },
                    ConceptExplanation = "Đảo ngữ (Inversion) là việc đưa trợ động từ (Auxiliary Verb / Modal Verb / Be) lên đứng trước chủ ngữ nhằm tạo sắc thái nhấn mạnh đặc biệt, kịch tính hoặc trang trọng tối đa. Đây là cấu trúc phân hóa mạnh nhất ở dải điểm Band 8.0 - 9.0 trong IELTS Academic Writing.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Đảo ngữ với Trạng từ phủ định", Formula = "Rarely / Seldom / Never / Little + Trợ động từ + S + V", ColorVariant = "blue", Breakdown = new() { "Rarely do scholars encounter such profound anomalies." } },
                        new GrammarFormulaBlock { Type = "Đảo ngữ với cụm từ giới hạn 'Only'", Formula = "Only when / Only after / Only by + Mệnh đề/V-ing + Trợ động từ + S + V", ColorVariant = "purple", Breakdown = new() { "Only by adopting sustainable habits can humanity avert climate catastrophe." } },
                        new GrammarFormulaBlock { Type = "Đảo ngữ với cụm từ cấm đoán", Formula = "Under no circumstances / On no account + Trợ động từ + S + V", ColorVariant = "green", Breakdown = new() { "Under no circumstances should basic civil liberties be compromised." } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Vị trí đảo ngữ trong cụm Only when / Only after", Example = "Only when S1 + V1 + ĐẢO NGỮ Ở MỆNH ĐỀ 2 (can we achieve...)", Note = "Không đảo ngữ ở mệnh đề ngay sau Only when, mà đảo ở mệnh đề chính" },
                        new GrammarRuleTableItem { Rule = "Đảo ngữ với No sooner... than", Example = "No sooner had the protocol been implemented than efficiency improved.", Note = "Đi kèm liên từ 'than'" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Nhấn mạnh giải pháp duy nhất có thể giải quyết vấn đề (Task 2)",
                            Explanation = "Dùng cấu trúc 'Only by + V-ing can we...'.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Only by imposing heavier penalties on corporate polluters can governments restore environmental integrity.", Vietnamese = "Chỉ bằng cách áp đặt các mức phạt nặng hơn lên các doanh nghiệp gây ô nhiễm, các chính phủ mới có thể khôi phục sự toàn vẹn của môi trường." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Khẳng định điều cấm kỵ hoặc nguyên tắc bất khả xâm phạm",
                            Explanation = "Dùng 'Under no circumstances should...'.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Under no circumstances should ethical standards in biomedical research be relaxed.", Vietnamese = "Trong bất kỳ hoàn cảnh nào, các tiêu chuẩn đạo đức trong nghiên cứu y sinh cũng không được phép nới lỏng." },
                            }
                        },
                    },
                    SignalWords = new() { "seldom", "rarely", "hardly", "not only", "under no circumstances", "only by", "only when", "at no time" },
                    SignalWordPlacementRule = "Cụm từ phủ định hoặc hạn định bắt buộc phải đứng ở vị trí đầu tiên của câu.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Quên đảo trợ động từ lên trước chủ ngữ",
                            WrongExample = "Rarely people realize the long-term dangers of microplastics.",
                            CorrectExample = "Rarely do people realize the long-term dangers of microplastics.",
                            Explanation = "Đã đưa từ phủ định 'Rarely' lên đầu câu thì bắt buộc phải mượn trợ động từ 'do' đảo trước 'people'."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Đảo ngữ nhầm vị trí trong cấu trúc Only when",
                            WrongExample = "Only when did the crisis worsen, the government reacted.",
                            CorrectExample = "Only when the crisis worsened did the government react.",
                            Explanation = "Mệnh đề ngay sau 'Only when' giữ nguyên; đảo ngữ nằm ở mệnh đề chính phía sau ('did the government react')."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "Seldom _______ such unprecedented levels of public engagement in environmental policy reforms.",
                            Options = new() { "have sociologists observed", "sociologists have observed", "sociologists observed have", "did sociologists observed" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "have sociologists observed",
                            Explanation = "Đảo ngữ với từ phủ định 'Seldom': Trợ động từ 'have' đảo lên trước chủ ngữ 'sociologists'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Only by investing aggressively in green infrastructure _______ greenhouse gas emissions by 2035.",
                            Options = new() { "can developing nations reduce", "developing nations can reduce", "nations developing can reduce", "reduce can developing nations" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "can developing nations reduce",
                            Explanation = "Cấu trúc 'Only by + V-ing + Modal verb (can) + Subject + Verb'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Under no circumstances _______ personal data be commercialized without explicit user consent.",
                            Options = new() { "should", "could not", "must not", "that" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "should",
                            Explanation = "'Under no circumstances' đã mang nghĩa phủ định tuyệt đối (trong bất kỳ hoàn cảnh nào cũng không), nên trợ động từ đi sau ở dạng khẳng định 'should'."
                        },
                    },
                    LearningTip = "Đừng lạm dụng quá nhiều đảo ngữ trong một bài viết. Chỉ cần xuất hiện ĐÚNG MỘT CÂU đảo ngữ 'Only by... can...' ở đoạn kết bài hoặc thân bài 2 là đủ để giám khảo chấm điểm 8.0+ cho bạn!",
                    RelatedBandStructureCodes = new() { "ADV_INV_01" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "su-hoa-hop-chu-ngu-dong-tu",
                    Title = "Sự Hòa Hợp Chủ Ngữ & Động Từ",
                    EnglishTitle = "Subject-Verb Agreement (Complex & Tricky Cases)",
                    SectionKey = "advanced",
                    SectionTitle = "Ngữ Pháp Nâng Cao",
                    ShortDescription = "Quy tắc hòa hợp chủ ngữ số ít/số nhiều trong các cấu trúc bẫy: either/neither, as well as, along with, danh từ tập hợp.",
                    Icon = "bi-check2-all",
                    IconBgColor = "#4338ca",
                    DifficultyLevel = "Trung cấp",
                    ReadTimeMinutes = 6,
                    OrderIndex = 11,
                    FormulaPreview = "Singular vs. Plural Agreement Rules",
                    SkillTarget = "Grammar Accuracy Band 7.5+",
                    Tags = new() { "nâng cao", "advanced", "hòa hợp chủ vị", "subject verb agreement" },
                    ConceptExplanation = "Sự hòa hợp giữa Chủ ngữ và Động từ (Subject-Verb Agreement) quy định rằng động từ phải luôn tương thích về số (số ít hoặc số nhiều) và ngôi với chủ ngữ chính của câu. Thí sinh rất dễ mất điểm khi gặp các chủ ngữ phức tạp có cụm giới từ chen ngang hoặc các liên từ đặc biệt.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Chủ ngữ kèm cụm chen ngang", Formula = "S1 + (as well as / together with / along with / in addition to) + S2 + V (chia theo S1)", ColorVariant = "blue", Breakdown = new() { "The professor, along with his research assistants, is conducting field work." } },
                        new GrammarFormulaBlock { Type = "Cấu trúc Either... or / Neither... nor", Formula = "Neither S1 nor S2 + V (chia theo S2 gần nhất)", ColorVariant = "purple", Breakdown = new() { "Neither the manager nor the employees were informed." } },
                        new GrammarFormulaBlock { Type = "Danh từ đo lường, tiền bạc, khoảng cách", Formula = "Khoảng cách, tiền bạc, thời gian + Động từ số ít", ColorVariant = "green", Breakdown = new() { "Ten million dollars is a substantial investment." } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "A number of vs The number of", Example = "A number of students ARE participating (số nhiều) vs The number of students IS increasing (số ít)", Note = "Bẫy điểm kinh điển nhất trong IELTS Writing Task 1" },
                        new GrammarRuleTableItem { Rule = "Đại từ bất định luôn chia số ít", Example = "Everyone, somebody, nobody, each, every + Động từ số ít", Note = "Nobody is exempted from the law" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Mô tả số liệu chính xác tuyệt đối trong IELTS Writing Task 1",
                            Explanation = "Dùng 'The number of + N số nhiều + V số ít'.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "The number of foreign tourists visiting Vietnam rose exponentially between 2015 and 2019.", Vietnamese = "Số lượng khách du lịch nước ngoài đến thăm Việt Nam đã tăng theo cấp số nhân trong giai đoạn 2015 - 2019." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Xử lý chủ ngữ có mệnh đề quan hệ chen giữa trong Task 2",
                            Explanation = "Xác định đúng danh từ gốc để chia động từ chính.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "The proliferation of digital entertainment platforms that stream content has fundamentally altered consumer leisure habits.", Vietnamese = "Sự nở rộ của các nền tảng giải trí kỹ thuật số phát trực tuyến nội dung đã làm thay đổi căn bản thói quen thư giãn của người tiêu dùng." },
                            }
                        },
                    },
                    SignalWords = new() { "the number of", "a number of", "as well as", "together with", "neither... nor", "each of", "majority of" },
                    SignalWordPlacementRule = "Động từ hòa hợp với danh từ trung tâm đứng trước các cụm giới từ (prepositional phrases).",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Chia động từ số nhiều sau 'The number of'",
                            WrongExample = "The number of motorbikes in metropolises are increasing.",
                            CorrectExample = "The number of motorbikes in metropolises is increasing.",
                            Explanation = "Chủ ngữ ở đây là 'The number' (con số - số ít), nên động từ bắt buộc phải chia số ít 'is increasing'."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Bị 'lạc' chủ ngữ do cụm giới từ chen ngang",
                            WrongExample = "The discovery of ancient artifacts were reported yesterday.",
                            CorrectExample = "The discovery of ancient artifacts was reported yesterday.",
                            Explanation = "Chủ ngữ chính là 'The discovery' (số ít), cụm 'of ancient artifacts' chỉ là bổ nghĩa giới từ."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "The number of international applicants seeking asylum _______ steadily over the past three years.",
                            Options = new() { "has escalated", "have escalated", "are escalating", "were escalating" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "has escalated",
                            Explanation = "'The number of...' làm chủ ngữ thì động từ luôn chia ở ngôi thứ 3 số ít ('has escalated')."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "The lead researcher, together with several postgraduate lab assistants, _______ finalizing the clinical trial report.",
                            Options = new() { "is", "are", "were", "have been" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "is",
                            Explanation = "Khi chủ ngữ nối bằng 'together with', động từ chia theo chủ ngữ đầu tiên 'The lead researcher' (số ít) → 'is'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "A substantial proportion of municipal revenue _______ allocated to upgrading sewage and sanitation networks.",
                            Options = new() { "is", "are", "were", "being" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "is",
                            Explanation = "'revenue' là danh từ không đếm được, nên phân số/tỷ lệ của nó vẫn đi kèm động từ số ít 'is'."
                        },
                    },
                    LearningTip = "Mẹo vàng Task 1: Hãy nhớ 'A number of' = Many (đi với động từ số nhiều); còn 'The number of' = Con số (luôn đi với động từ số ít)!",
                    RelatedBandStructureCodes = new() { "ADV_SVA_01" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "thuc-gia-dinh",
                    Title = "Thức Giả Định (Subjunctive)",
                    EnglishTitle = "Present & Past Subjunctive in Academic English",
                    SectionKey = "advanced",
                    SectionTitle = "Ngữ Pháp Nâng Cao",
                    ShortDescription = "Động từ nguyên thể không chia sau demand, recommend, crucial, imperative that S + (should) + V_bare.",
                    Icon = "bi-exclamation-octagon-fill",
                    IconBgColor = "#3730a3",
                    DifficultyLevel = "Nâng cao",
                    ReadTimeMinutes = 6,
                    OrderIndex = 12,
                    FormulaPreview = "It is essential that S + (should) + V_bare",
                    SkillTarget = "Policy Recommendation Writing Task 2",
                    Tags = new() { "nâng cao", "advanced", "giả định thức", "subjunctive", "mandative" },
                    ConceptExplanation = "Thức giả định (Subjunctive Mood) dùng để thể hiện tính cấp bách, mệnh lệnh, yêu cầu hoặc tầm quan trọng thiết yếu của một hành động. Trong tiếng Anh học thuật, giả định thức hiện tại yêu cầu động từ ở mệnh đề 'that' phải ở dạng nguyên thể không chia (Bare Infinitive) cho tất cả các ngôi.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Giả định với Tính từ cấp bách", Formula = "It is vital / crucial / essential / imperative that + S + (should) + V_inf", ColorVariant = "blue", Breakdown = new() { "It is imperative that every citizen have access to clean drinking water." } },
                        new GrammarFormulaBlock { Type = "Giả định với Động từ yêu cầu, đề xuất", Formula = "S + demand / recommend / propose / insist that + S + (should) + V_inf", ColorVariant = "purple", Breakdown = new() { "Experts recommend that the government subsidize renewable energy." } },
                        new GrammarFormulaBlock { Type = "Thể bị động trong thức giả định", Formula = "that + S + be + V3/ed", ColorVariant = "green", Breakdown = new() { "It is essential that medical supplies be distributed equitably." } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Động từ luôn ở dạng nguyên mẫu không chia", Example = "It is essential that he BE present. (KHÔNG dùng 'is')", Note = "Kể cả chủ ngữ số ít (he, she, it) động từ vẫn giữ nguyên thể" },
                        new GrammarRuleTableItem { Rule = "Thể phủ định trong thức giả định", Example = "It is recommended that the company NOT expand into unproven markets.", Note = "Chỉ cần thêm 'not' trước V nguyên mẫu, không mượn trợ động từ does/did" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Đưa ra khuyến nghị chính sách đanh thép trong kết bài hoặc giải pháp Task 2",
                            Explanation = "Thể hiện văn phong học thuật trang trọng bậc nhất.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "It is imperative that international organizations coordinate cohesive emergency responses to regional disasters.", Vietnamese = "Điều cấp thiết là các tổ chức quốc tế phải phối hợp các phản ứng khẩn cấp gắn kết trước các thảm họa khu vực." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Trình bày yêu cầu của các cơ quan quản lý",
                            Explanation = "Dùng trong các văn bản pháp lý hoặc phân tích chính sách.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Health authorities insist that clinical trials undergo rigorous peer review before public rollout.", Vietnamese = "Các cơ quan y tế kiên quyết yêu cầu các thử nghiệm lâm sàng phải trải qua quá trình bình duyệt nghiêm ngặt trước khi triển khai rộng rãi." },
                            }
                        },
                    },
                    SignalWords = new() { "it is imperative that", "it is vital that", "it is crucial that", "recommend that", "demand that", "insist that" },
                    SignalWordPlacementRule = "Nằm trong mệnh đề 'that' đi sau các tính từ hoặc động từ chỉ sự cấp thiết.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Thêm s/es vào động từ sau thức giả định",
                            WrongExample = "It is crucial that the student submits the application on time.",
                            CorrectExample = "It is crucial that the student submit the application on time.",
                            Explanation = "Trong thức giả định chuẩn, động từ ở dạng nguyên thể không chia ('submit')."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Mượn trợ động từ 'doesn't' trong câu phủ định của thức giả định",
                            WrongExample = "Doctors recommend that a patient doesn't consume excessive sugar.",
                            CorrectExample = "Doctors recommend that a patient not consume excessive sugar.",
                            Explanation = "Dạng phủ định của Subjunctive chỉ là 'not + V_inf', không dùng 'doesn't' hay 'don't'."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "It is essential that each participant _______ thoroughly informed of the experimental risks beforehand.",
                            Options = new() { "be", "is", "was", "to be" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "be",
                            Explanation = "Thức giả định bị động: 'It is essential that + S + be + V3/ed' ('be informed')."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "The advisory board recommended that the CEO _______ redundant administrative divisions to streamline operations.",
                            Options = new() { "dissolve", "dissolves", "dissolved", "to dissolve" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "dissolve",
                            Explanation = "Sau động từ 'recommended that', động từ chính giữ nguyên mẫu không chia ('dissolve')."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Public health specialists insist that the pharmaceutical conglomerate _______ premature data to the media.",
                            Options = new() { "not release", "doesn't release", "not releases", "won't release" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "not release",
                            Explanation = "Phủ định của thức giả định là 'not + V_bare': 'not release'."
                        },
                    },
                    LearningTip = "Sử dụng 'It is imperative that governments take...' ở câu đề xuất giải pháp Task 2 sẽ giúp bạn ghi điểm tuyệt đối về tính trang trọng và ngữ pháp nâng cao!",
                    RelatedBandStructureCodes = new() { "ADV_SUBJ_01" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "dong-tu-khiem-khuyet",
                    Title = "Động Từ Khiếm Khuyết & Hedging",
                    EnglishTitle = "Modal Verbs & Academic Hedging (Cautionary Language)",
                    SectionKey = "advanced",
                    SectionTitle = "Ngữ Pháp Nâng Cao",
                    ShortDescription = "Kỹ thuật ngôn ngữ thận trọng (Hedging) tránh khẳng định tuyệt đối cực đoan, bảo vệ luận điểm khoa học vững chắc.",
                    Icon = "bi-shield-shaded",
                    IconBgColor = "#312e81",
                    DifficultyLevel = "Trung cấp",
                    ReadTimeMinutes = 6,
                    OrderIndex = 13,
                    FormulaPreview = "may / might / could / tend to / appear to be",
                    SkillTarget = "Academic Tone Band 8.0+",
                    Tags = new() { "nâng cao", "advanced", "hedging", "modal verbs", "ngôn ngữ thận trọng" },
                    ConceptExplanation = "Hedging (Ngôn ngữ thận trọng / Ngôn ngữ phòng vệ) là kỹ thuật sử dụng động từ khiếm khuyết (may, might, could), động từ phỏng đoán (tend to, seem to, appear to) và trạng từ chỉ xác suất (arguably, potentially, plausibly) để đưa ra nhận định mang tính khoa học, tránh sự khẳng định giáo điều hoặc tuyệt đối hóa.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Hedging bằng Modal Verbs", Formula = "S + may / might / could + V_inf", ColorVariant = "blue", Breakdown = new() { "Excessive screen time may contribute to juvenile sleep deprivation." } },
                        new GrammarFormulaBlock { Type = "Hedging bằng Động từ xu hướng", Formula = "S + tend to / appear to / seem to + V_inf", ColorVariant = "purple", Breakdown = new() { "Older demographics tend to be more risk-averse in financial investments." } },
                        new GrammarFormulaBlock { Type = "Hedging bằng Cụm từ khả năng", Formula = "It is likely / plausible / probable that + Clause", ColorVariant = "green", Breakdown = new() { "It is highly probable that automated systems will enhance workplace safety." } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Tránh các từ tuyệt đối hóa cực đoan trong Task 2", Example = "always, never, all people, completely, impossible", Note = "Tuyệt đối hóa sẽ khiến lập luận bị phản biện và trừ điểm Task Response" },
                        new GrammarRuleTableItem { Rule = "Mức độ chắc chắn của Modal Verbs", Example = "must (chắc chắn 95%) > should/will (75%) > may/could (50%) > might (30%)", Note = "Lựa chọn từ phù hợp với độ tin cậy của dẫn chứng" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Đưa ra nhận định khoa học khách quan trong bài luận IELTS Task 2",
                            Explanation = "Bảo vệ quan điểm khỏi bị bắt bẻ tính tuyệt đối.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Prolonged exposure to air pollution can arguably impair cognitive faculties in developing children.", Vietnamese = "Việc tiếp xúc lâu dài với ô nhiễm không khí rất có thể làm suy giảm năng lực nhận thức ở trẻ đang phát triển." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Suy đoán nguyên nhân trong Speaking Part 3",
                            Explanation = "Thể hiện tư duy đa chiều và sự khiêm tốn học thuật.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "One plausible reason could be that economic uncertainty discourages young adults from purchasing homes.", Vietnamese = "Một lý do hợp lý có thể là sự bấp bênh về kinh tế làm nản lòng những người trẻ tuổi mua nhà." },
                            }
                        },
                    },
                    SignalWords = new() { "tend to", "is likely to", "arguably", "potentially", "could possibly", "it appears that" },
                    SignalWordPlacementRule = "Trạng từ hedging đứng trước động từ thường hoặc sau động từ to be.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Khẳng định tuyệt đối hóa gây sơ hở lập luận",
                            WrongExample = "Video games make all teenagers violent.",
                            CorrectExample = "Excessive gaming may increase aggressive tendencies among certain adolescents.",
                            Explanation = "Viết 'make all teenagers' là sai thực tế khoa học; phải dùng 'may increase... among certain adolescents'."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Dùng 'to' sau modal verb",
                            WrongExample = "The policy may to create unintended consequences.",
                            CorrectExample = "The policy may create unintended consequences.",
                            Explanation = "Sau các modal verbs thuần túy (may, might, could, must, should) luôn là động từ nguyên thể không to."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "Which of the following revisions demonstrates the best academic hedging to replace: 'Social media destroys real human communication'?",
                            Options = new() { "Excessive social media consumption may potentially weaken face-to-face interpersonal interactions.", "Social media always destroys all kinds of human conversations.", "Social media will certainly eliminate real human communication forever.", "Social media is completely impossible for communication." },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "Excessive social media consumption may potentially weaken face-to-face interpersonal interactions.",
                            Explanation = "Sử dụng 'may potentially weaken' là ví dụ kinh điển của kỹ thuật Academic Hedging Band 8.5."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Urban populations _______ consume more processed foodstuffs than their rural counterparts.",
                            Options = new() { "tend to", "must always", "are definitely to", "never" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "tend to",
                            Explanation = "'tend to' (có xu hướng) là động từ hedging khách quan để diễn tả hành vi xã hội mà không vơ đũa cả nắm."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "It is _______ that sustained investment in green technology will yield economic dividends.",
                            Options = new() { "highly probable", "100% true", "absolutely undeniable totally", "surely impossible" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "highly probable",
                            Explanation = "'highly probable' (rất có khả năng) là cụm từ học thuật chuẩn xác để thể hiện mức độ xác suất cao nhưng vẫn thận trọng."
                        },
                    },
                    LearningTip = "Thành thạo kỹ thuật Hedging (dùng 'is likely to', 'tends to', 'could potentially') là chìa khóa then chốt để đưa điểm Task Response và Lexical Resource của bạn từ 6.5 lên 8.0!",
                    RelatedBandStructureCodes = new() { "ADV_HEDGE_01" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "menh-de-trang-ngu",
                    Title = "Mệnh Đề Trạng Ngữ (Adverbial Clauses)",
                    EnglishTitle = "Adverbial Clauses of Time, Cause, Contrast & Condition",
                    SectionKey = "advanced",
                    SectionTitle = "Ngữ Pháp Nâng Cao",
                    ShortDescription = "Mệnh đề phụ chỉ thời gian, nguyên nhân, tương phản, điều kiện và nhượng bộ để câu văn giàu tính kết nối.",
                    Icon = "bi-sign-intersection-y-fill",
                    IconBgColor = "#1e1b4b",
                    DifficultyLevel = "Trung cấp",
                    ReadTimeMinutes = 6,
                    OrderIndex = 14,
                    FormulaPreview = "Although / Inasmuch as / Whereas / Provided that",
                    SkillTarget = "Cohesion & Sentence Complexity",
                    Tags = new() { "nâng cao", "advanced", "mệnh đề trạng ngữ", "adverbial clause" },
                    ConceptExplanation = "Mệnh đề trạng ngữ (Adverbial Clause) là mệnh đề phụ thuộc đóng vai trò như một trạng từ bổ nghĩa cho mệnh đề chính, giải thích về thời gian, lý do, sự tương phản, điều kiện, mục đích hoặc kết quả.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Mệnh đề chỉ sự tương phản / nhượng bộ", Formula = "Although / Even though / Whereas / While + S + V, Main Clause", ColorVariant = "blue", Breakdown = new() { "While industrial output contracted, agricultural revenue expanded." } },
                        new GrammarFormulaBlock { Type = "Mệnh đề chỉ nguyên nhân học thuật", Formula = "Inasmuch as / Given that / Since + S + V, Main Clause", ColorVariant = "purple", Breakdown = new() { "Given that fossil fuels are finite, transition to renewables is unavoidable." } },
                        new GrammarFormulaBlock { Type = "Mệnh đề chỉ kết quả", Formula = "so + adj + that / such + a/an + adj + noun + that + Clause", ColorVariant = "green", Breakdown = new() { "The temperature was so high that machinery overheated." } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Dấu phẩy khi mệnh đề trạng ngữ đứng đầu", Example = "Since resources are scarce, allocation must be disciplined.", Note = "Bắt buộc có dấu phẩy khi mệnh đề phụ đứng trước mệnh đề chính" },
                        new GrammarRuleTableItem { Rule = "Phân biệt Because of (cụm danh từ) và Because (mệnh đề)", Example = "Because of rising costs (SAI: Because of costs rose)", Note = "Because + S + V" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Thiết lập bối cảnh và lý do trong bài viết Task 2",
                            Explanation = "Dùng 'Given that' hoặc 'Inasmuch as' để lập luận trang trọng.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Given that artificial intelligence is advancing rapidly, comprehensive ethical frameworks must be drafted.", Vietnamese = "Xét thấy trí tuệ nhân tạo đang tiến bộ nhanh chóng, các khuôn khổ đạo đức toàn diện cần phải được soạn thảo." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Đối chiếu dữ liệu tương phản trong Task 1",
                            Explanation = "Dùng 'whereas' hoặc 'while' kẹp giữa hai số liệu.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "The percentage of rail travelers declined by 12%, whereas bus passengers increased substantially.", Vietnamese = "Tỷ lệ hành khách đi tàu hỏa giảm 12%, trong khi lượng khách đi xe buýt lại tăng đáng kể." },
                            }
                        },
                    },
                    SignalWords = new() { "inasmuch as", "given that", "whereas", "while", "provided that", "so... that" },
                    SignalWordPlacementRule = "Đứng ở đầu câu (kèm dấu phẩy) hoặc đứng ở cuối câu.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Dùng mệnh đề sau 'Despite' hoặc 'In spite of'",
                            WrongExample = "Despite the government intervened, unemployment rose.",
                            CorrectExample = "Despite government intervention, unemployment rose. / Although the government intervened, unemployment rose.",
                            Explanation = "Sau 'Despite' chỉ dùng cụm danh từ hoặc V-ing; sau 'Although' mới dùng mệnh đề S + V."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Nhầm lẫn giữa 'so... that' và 'such... that'",
                            WrongExample = "It was so cold weather that schools closed.",
                            CorrectExample = "It was such cold weather that schools closed.",
                            Explanation = "Có danh từ không đếm được 'weather' đi sau tính từ 'cold', phải dùng 'such', không dùng 'so'."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "_______ online learning offers flexible schedules, it often lacks the interpersonal dynamism of traditional seminars.",
                            Options = new() { "While", "Despite", "Because of", "In spite of" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "While",
                            Explanation = "'While' đứng đầu câu làm liên từ phụ thuộc nhượng bộ mang nghĩa 'Mặc dù...', theo sau là một mệnh đề hoàn chỉnh (S + V)."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "The experimental prototype generated _______ promising results that venture capitalists offered immediate seed funding.",
                            Options = new() { "such", "so", "too", "very" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "such",
                            Explanation = "Cấu trúc: 'such + adj + plural noun + that' ('such promising results that')."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "_______ water scarcity is becoming acute in arid regions, water desalination plants have been constructed.",
                            Options = new() { "Given that", "In spite of", "Due to", "Regardless of" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "Given that",
                            Explanation = "'Given that' (xét thấy thực tế là) đi với mệnh đề chỉ nguyên nhân học thuật."
                        },
                    },
                    LearningTip = "Sử dụng các mệnh đề trạng ngữ mở đầu bằng 'Given that...' hoặc 'While it is true that...' để mở rộng câu văn đa tầng một cách tự nhiên!",
                    RelatedBandStructureCodes = new() { "ADV_ADVCL_01" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "danh-dong-tu-va-dong-tu-nguyen-mau",
                    Title = "Danh Động Từ & To-V",
                    EnglishTitle = "Gerunds & Infinitives in Complex Sentence Construction",
                    SectionKey = "advanced",
                    SectionTitle = "Ngữ Pháp Nâng Cao",
                    ShortDescription = "Ứng dụng nâng cao của V-ing và To-V trong các cụm cấu trúc phức tạp và đảo ngữ.",
                    Icon = "bi-braces",
                    IconBgColor = "#4338ca",
                    DifficultyLevel = "Trung cấp",
                    ReadTimeMinutes = 6,
                    OrderIndex = 15,
                    FormulaPreview = "Subject Gerunds & Complex Infinitives",
                    SkillTarget = "Style & Sentence Variety",
                    Tags = new() { "nâng cao", "advanced", "gerund", "infinitive", "danh động từ" },
                    ConceptExplanation = "Khi kết hợp danh động từ (Gerund) và động từ nguyên mẫu (Infinitive) vào các cấu trúc phức tạp như chủ ngữ kép, tân ngữ nhận thức, hoặc đi cùng các cấu trúc bị động hoàn thành (having been V3), thí sinh sẽ tạo nên những câu văn có độ tinh tế ngữ pháp vượt trội.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Perfect Gerund (Danh động từ hoàn thành)", Formula = "having + V3/ed", ColorVariant = "blue", Breakdown = new() { "He admitted having falsified the laboratory data." } },
                        new GrammarFormulaBlock { Type = "Passive Infinitive (Nguyên mẫu bị động)", Formula = "to be + V3/ed / to have been + V3/ed", ColorVariant = "purple", Breakdown = new() { "The proposal deserves to be considered seriously." } },
                        new GrammarFormulaBlock { Type = "Chủ ngữ danh động từ ghép", Formula = "Balancing career ambitions and familial duties is challenging.", ColorVariant = "green", Breakdown = new() { "Động từ luôn chia số ít" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Danh từ sở hữu trước danh động từ (Possessive with Gerund)", Example = "I appreciate your helping me. (Trang trọng hơn: your help/helping thay vì you helping)", Note = "Văn phong học thuật ưa chuộng tính từ sở hữu trước V-ing" },
                        new GrammarRuleTableItem { Rule = "Động từ theo sau tính từ đánh giá", Example = "It is customary / conventional / imperative to V", Note = "Cấu trúc chủ ngữ giả It" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Tạo chủ ngữ trừu tượng cho các luận điểm xã hội",
                            Explanation = "Dùng V-ing phrase làm chủ ngữ chính của câu văn.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Fostering mutual cultural understanding is essential to global conflict de-escalation.", Vietnamese = "Nuôi dưỡng sự hiểu biết văn hóa lẫn nhau là điều thiết yếu để hạ nhiệt các xung đột toàn cầu." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Chỉ rõ thứ tự trước sau bằng Perfect Gerund",
                            Explanation = "Nhấn mạnh hành động đã hoàn tất trong quá khứ.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Having completed the comprehensive feasibility study, the corporation greenlit the project.", Vietnamese = "Sau khi đã hoàn thành nghiên cứu khả thi toàn diện, tập đoàn đã bật đèn xanh cho dự án." },
                            }
                        },
                    },
                    SignalWords = new() { "having completed", "to be considered", "fostering", "balancing", "appreciate your V-ing" },
                    SignalWordPlacementRule = "Cụm danh động từ hoàn thành (Having + V3) thường đứng đầu câu làm trạng ngữ rút gọn.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Chia động từ số nhiều cho cụm chủ ngữ V-ing",
                            WrongExample = "Implementing comprehensive sustainability policies are costly.",
                            CorrectExample = "Implementing comprehensive sustainability policies is costly.",
                            Explanation = "Chủ ngữ là việc thực thi ('Implementing' - danh động từ số ít), nên động từ chia 'is', không chia theo 'policies'."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Lỗi phân từ lơ lửng (Dangling Participle)",
                            WrongExample = "Having finished the report, the computer was turned off.",
                            CorrectExample = "Having finished the report, the researcher turned off the computer.",
                            Explanation = "Chủ ngữ sau dấu phẩy phải chính là đối tượng thực hiện hành động hoàn thành báo cáo (the researcher)."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "_______ extensive fieldwork in remote equatorial forests, the botanists published groundbreaking discoveries.",
                            Options = new() { "Having conducted", "Conducted", "To conduct", "Being conduct" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "Having conducted",
                            Explanation = "Cụm phân từ hoàn thành 'Having conducted' nhấn mạnh việc hoàn tất nghiên cứu thực địa trước khi công bố phát hiện."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Reconciling rapid industrial development with rigorous environmental conservation _______ a multifaceted challenge.",
                            Options = new() { "remains", "remain", "are remaining", "have remained" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "remains",
                            Explanation = "Chủ ngữ là hành động điều hòa ('Reconciling' - danh động từ số ít), động từ chia 'remains'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "The historic architectural relic deserves _______ with utmost care and specialized craftsmanship.",
                            Options = new() { "to be preserved", "preserving to be", "to preserve", "to have preserve" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "to be preserved",
                            Explanation = "Cấu trúc nguyên mẫu bị động: 'deserves to be preserved' (xứng đáng được bảo tồn)."
                        },
                    },
                    LearningTip = "Mở đầu câu bằng phân từ hoàn thành 'Having analyzed both viewpoints, I am convinced that...' là cách mở đoạn kết bài Task 2 cực kỳ ấn tượng!",
                    RelatedBandStructureCodes = new() { "ADV_GER_01" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "so-sanh",
                    Title = "Cấu Trúc So Sánh Đa Dạng",
                    EnglishTitle = "Comparative & Superlative Structures (Double & Proportional Comparatives)",
                    SectionKey = "advanced",
                    SectionTitle = "Ngữ Pháp Nâng Cao",
                    ShortDescription = "So sánh kép 'The more... the more...', so sánh gấp bội (twice as much as) và so sánh nâng cao trong IELTS Task 1.",
                    Icon = "bi-bar-chart-steps",
                    IconBgColor = "#3730a3",
                    DifficultyLevel = "Trung cấp",
                    ReadTimeMinutes = 6,
                    OrderIndex = 16,
                    FormulaPreview = "The more S + V, the more S + V | Three times as high as",
                    SkillTarget = "IELTS Writing Task 1 & Task 2 Band 7+",
                    Tags = new() { "nâng cao", "advanced", "so sánh", "comparatives", "double comparatives" },
                    ConceptExplanation = "So sánh là linh hồn của bài thi IELTS Writing Task 1 (vốn yêu cầu 'make comparisons where relevant'). Nắm vững các cấu trúc so sánh kép (The more... the more...), so sánh gấp bội (three times higher than / three times as much as) giúp bài viết Task 1 biến hóa đa dạng và đạt điểm cao tiêu chí Task Achievement.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "So sánh kép đồng tiến (Proportional)", Formula = "The + Comparative + S + V, the + Comparative + S + V", ColorVariant = "blue", Breakdown = new() { "The more educated citizens are, the more prosperous the nation becomes." } },
                        new GrammarFormulaBlock { Type = "So sánh gấp bội (Multiple Numbers)", Formula = "S + V + multiple (twice / three times) + as + adj/much/many + as + O", ColorVariant = "purple", Breakdown = new() { "The UK consumed three times as much coal as Germany did." } },
                        new GrammarFormulaBlock { Type = "So sánh bậc hơn biến hóa", Formula = "significantly / substantially / marginally + higher/lower than", ColorVariant = "green", Breakdown = new() { "Thêm trạng từ chỉ biên độ để làm sắc nét sự chênh lệch" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Gấp 2 lần dùng TWICE (không dùng two times)", Example = "twice as high as (ĐÚNG) - two times as high as (KHÔNG TỰ NHIÊN)", Note = "Từ 3 lần trở lên mới dùng three times, four times" },
                        new GrammarRuleTableItem { Rule = "Cấu trúc kép 'The more... the more...'", Example = "The higher the tariff, the lower the import volume.", Note = "Hai vế phải cân xứng về cấu trúc so sánh" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Đối chiếu số liệu đa chiều trong IELTS Writing Task 1",
                            Explanation = "Sử dụng so sánh gấp bội và so sánh hơn kém có trạng từ chỉ biên độ.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "In 2018, expenditure on renewable infrastructure was approximately four times as high as that on fossil fuels.", Vietnamese = "Năm 2018, chi tiêu cho cơ sở hạ tầng tái tạo cao gấp khoảng 4 lần so với chi tiêu cho nhiên liệu hóa thạch." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Thể hiện mối quan hệ tỷ lệ thuận/nghịch trong Task 2",
                            Explanation = "Dùng so sánh kép 'The more... the more...'.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "The more stringent the environmental regulations become, the more innovative green enterprises appear.", Vietnamese = "Các quy định về môi trường càng trở nên nghiêm ngặt thì các doanh nghiệp xanh càng trở nên đổi mới sáng tạo hơn." },
                            }
                        },
                    },
                    SignalWords = new() { "the more... the more...", "twice as high as", "three times as much as", "substantially higher than", "in sharp contrast to" },
                    SignalWordPlacementRule = "So sánh kép kẹp hai mệnh đề cân xứng bằng dấu phẩy.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Dùng 'two times' thay vì 'twice' trong so sánh gấp đôi",
                            WrongExample = "Car sales were two times higher than truck sales.",
                            CorrectExample = "Car sales were twice as high as truck sales.",
                            Explanation = "Gấp đôi dùng 'twice', không dùng 'two times'."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Quên đại từ quy chiếu 'that of' hoặc 'those of' khi so sánh khập khiễng",
                            WrongExample = "The population of Tokyo is larger than London.",
                            CorrectExample = "The population of Tokyo is larger than that of London.",
                            Explanation = "So sánh dân số Tokyo với dân số London, nên phải dùng 'that of London' (tránh so sánh dân số với thành phố)."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "The more urban areas expand, _______ natural habitats become fragmented.",
                            Options = new() { "the more", "more", "the most", "much more" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "the more",
                            Explanation = "Cấu trúc so sánh kép chuẩn: 'The + comparative..., the + comparative...' ('The more...')."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "In 2020, solar power generation in Germany was nearly three times _______ that of neighboring Austria.",
                            Options = new() { "as high as", "higher than", "as higher as", "higher as" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "as high as",
                            Explanation = "Cấu trúc so sánh gấp bội chuẩn: 'three times + as high as + that of'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Which of the following avoids the faulty comparison error?",
                            Options = new() { "The carbon footprint of meat production is far higher than that of grain cultivation.", "The carbon footprint of meat production is far higher than grain cultivation.", "The carbon footprint of meat production is far higher than grains.", "Carbon footprint in meat production is far higher grain cultivation." },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "The carbon footprint of meat production is far higher than that of grain cultivation.",
                            Explanation = "Sử dụng 'that of' để so sánh dấu chân carbon với dấu chân carbon, tránh so sánh khập khiễng dấu chân với hạt ngũ cốc."
                        },
                    },
                    LearningTip = "Khi viết Task 1, luôn nhớ dùng 'that of' (cho danh từ số ít) hoặc 'those of' (cho danh từ số nhiều) khi so sánh giữa hai quốc gia hoặc hai nhóm đối tượng!",
                    RelatedBandStructureCodes = new() { "ADV_COMP_01" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "cac-tu-de-hoi",
                    Title = "Từ Để Hỏi Chuyên Sâu",
                    EnglishTitle = "Wh- Words in Nominal & Relative Structures",
                    SectionKey = "advanced",
                    SectionTitle = "Ngữ Pháp Nâng Cao",
                    ShortDescription = "Khảo sát chuyên sâu Wh- words trong mệnh đề danh từ (Nominal Clauses) và cấu trúc câu phức.",
                    Icon = "bi-question-diamond-fill",
                    IconBgColor = "#312e81",
                    DifficultyLevel = "Trung cấp",
                    ReadTimeMinutes = 5,
                    OrderIndex = 17,
                    FormulaPreview = "How / Why / Where / What in Nominal Clauses",
                    SkillTarget = "Reading & Writing Complexity",
                    Tags = new() { "nâng cao", "advanced", "từ để hỏi", "wh words", "nominal clause" },
                    ConceptExplanation = "Từ để hỏi (Wh- Words) không chỉ dùng để đặt câu hỏi trực tiếp mà còn đóng vai trò đầu tàu dẫn dắt Mệnh đề danh từ (Nominal Clauses). Mệnh đề danh từ có thể làm chủ ngữ, tân ngữ cho động từ hoặc tân ngữ cho giới từ, tạo nên những câu văn học thuật đa tầng bậc cao.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Mệnh đề danh từ làm Chủ ngữ", Formula = "How / Why / What + S + V + Main Verb + Complement", ColorVariant = "blue", Breakdown = new() { "How civilizations adapt to climatic shifts determines their longevity." } },
                        new GrammarFormulaBlock { Type = "Mệnh đề danh từ làm Tân ngữ", Formula = "S + Verb + whether / what / why + S + V", ColorVariant = "purple", Breakdown = new() { "Economists debate whether artificial intelligence will displace manual labor." } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Trật tự câu khẳng định trong mệnh đề danh từ", Example = "What he discovered changed medicine. (KHÔNG dùng: What did he discover)", Note = "Không mượn trợ động từ đảo ngữ" },
                        new GrammarRuleTableItem { Rule = "Whether vs If trong mệnh đề danh từ", Example = "Debating WHETHER (trang trọng hơn IF), sau giới từ bắt buộc dùng whether", Note = "interested in WHETHER to proceed" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Mở đầu luận điểm bằng mệnh đề danh từ học thuật",
                            Explanation = "Đưa cả câu hỏi nghiên cứu thành chủ ngữ của bài luận.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Why remote working succeeded during the pandemic continues to fascinate organizational psychologists.", Vietnamese = "Lý do tại sao làm việc từ xa lại thành công trong thời kỳ đại dịch tiếp tục thu hút sự quan tâm của các nhà tâm lý học tổ chức." },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Bàn luận về các câu hỏi chính sách chưa có lời giải",
                            Explanation = "Dùng whether... or... để thể hiện tính đa chiều.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Policymakers must determine whether fiscal stimulus outweighs inflation risks.", Vietnamese = "Các nhà hoạch định chính sách phải xác định liệu gói kích thích tài khóa có lớn hơn rủi ro lạm phát hay không." },
                            }
                        },
                    },
                    SignalWords = new() { "how", "why", "what", "whether", "to what extent", "where" },
                    SignalWordPlacementRule = "Mệnh đề danh từ có thể đứng đầu câu làm chủ ngữ hoặc sau động từ làm tân ngữ.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Đảo ngữ trong mệnh đề danh từ",
                            WrongExample = "Why did the civilization collapse remains a mystery.",
                            CorrectExample = "Why the civilization collapsed remains a mystery.",
                            Explanation = "Mệnh đề danh từ làm chủ ngữ phải giữ nguyên trật tự câu khẳng định (Why + S + V), không mượn 'did'."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Dùng 'if' ngay sau giới từ",
                            WrongExample = "Scholars are concerned about if the treaty will be upheld.",
                            CorrectExample = "Scholars are concerned about whether the treaty will be upheld.",
                            Explanation = "Sau giới từ (about, in, of), bắt buộc phải dùng 'whether', không dùng 'if'."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "_______ genetic modifications will induce unintended ecological ramifications remains hotly debated.",
                            Options = new() { "Whether", "If", "Why did", "That whether" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "Whether",
                            Explanation = "'Whether + S + V' đứng đầu câu làm mệnh đề danh từ đóng vai trò chủ ngữ của động từ 'remains'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Select the grammatically correct sentence where a nominal clause acts as the subject:",
                            Options = new() { "How modern cities allocate green spaces fundamentally impacts public well-being.", "How do modern cities allocate green spaces fundamentally impacts public well-being.", "How modern cities allocate green spaces fundamentally impact public well-being.", "How are modern cities allocating green spaces fundamentally impacts." },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "How modern cities allocate green spaces fundamentally impacts public well-being.",
                            Explanation = "Mệnh đề danh từ 'How modern cities allocate green spaces' giữ trật tự câu khẳng định và đi với động từ số ít 'impacts'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Financial analysts are currently debating _______ international sanctions will accelerate currency devaluation.",
                            Options = new() { "whether", "if that", "why did", "that if" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "whether",
                            Explanation = "Sau động từ 'debating' diễn tả hai chiều khả năng, ta dùng 'whether'."
                        },
                    },
                    LearningTip = "Bắt đầu câu luận điểm bằng 'How governments tackle X determines Y...' sẽ giúp bạn phô diễn khả năng kiểm soát mệnh đề danh từ ở cấp độ Band 8.5!",
                    RelatedBandStructureCodes = new() { "ADV_WH_01" },
                },
                new GrammarGuideTopicDto
                {
                    Slug = "cau-hoi-duoi",
                    Title = "Câu Hỏi Đuôi (Tag Questions)",
                    EnglishTitle = "Tag Questions in Academic Dialogues & Speaking Nuance",
                    SectionKey = "advanced",
                    SectionTitle = "Ngữ Pháp Nâng Cao",
                    ShortDescription = "Quy tắc hòa hợp trợ động từ, đại từ và ngữ điệu câu hỏi đuôi trong các bài thi IELTS Speaking tương tác.",
                    Icon = "bi-patch-question-fill",
                    IconBgColor = "#1e1b4b",
                    DifficultyLevel = "Cơ bản",
                    ReadTimeMinutes = 5,
                    OrderIndex = 18,
                    FormulaPreview = "Statement (+), Auxiliary (-) + Pronoun?",
                    SkillTarget = "IELTS Speaking Part 3 Nuance",
                    Tags = new() { "nâng cao", "advanced", "câu hỏi đuôi", "tag questions", "speaking" },
                    ConceptExplanation = "Câu hỏi đuôi (Tag Question) là một câu hỏi ngắn gắn ở cuối câu trần thuật nhằm tìm kiếm sự đồng thuận hoặc xác nhận thông tin. Trong IELTS Speaking, sử dụng câu hỏi đuôi đúng ngữ điệu (Intonation) thể hiện sự tự nhiên, linh hoạt và phong thái đàm thoại tự tin.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Nguyên tắc ngược dấu", Formula = "Khẳng định (+), Phủ định (-)? | Phủ định (-), Khẳng định (+)?", ColorVariant = "blue", Breakdown = new() { "The evidence is compelling, isn't it?", "The policy hasn't failed, has it?" } },
                        new GrammarFormulaBlock { Type = "Các trường hợp đặc biệt", Formula = "I am → aren't I? | Let's → shall we? | Imperative → will you?", ColorVariant = "purple", Breakdown = new() { "I am eligible for the scholarship, aren't I?" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Các từ bán phủ định trong mệnh đề chính", Example = "seldom, rarely, hardly, scarcely, neither, nobody", Note = "Mệnh đề chính coi như phủ định, câu hỏi đuôi phải ở dạng KHẲNG ĐỊNH" },
                        new GrammarRuleTableItem { Rule = "Chủ ngữ Nobody / Everyone", Example = "Everyone participated enthusiastically, didn't they?", Note = "Đại từ ở đuôi luôn là 'they'" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Tạo sự tương tác tự nhiên với giám khảo trong IELTS Speaking Part 3",
                            Explanation = "Kêu gọi sự đồng thuận hoặc mời thảo luận thêm một cách lịch thiệp.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Technology certainly broadens educational access, doesn't it?", Vietnamese = "Công nghệ chắc chắn mở rộng khả năng tiếp cận giáo dục, phải không ạ?" },
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Sử dụng ngữ điệu xuống giọng để khẳng định ý kiến cá nhân",
                            Explanation = "Hạ giọng ở đuôi thể hiện sự tin chắc vào lập luận.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Urban life offers abundant employment opportunities, doesn't it? (Falling tone)", Vietnamese = "Đời sống đô thị mang lại cơ hội việc làm dồi dào, đúng không nào? (Giọng đi xuống để khẳng định)" },
                            }
                        },
                    },
                    SignalWords = new() { "isn't it", "aren't they", "didn't they", "has it", "shall we", "aren't I" },
                    SignalWordPlacementRule = "Nằm ở cuối câu trần thuật, ngăn cách bởi dấu phẩy.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Dùng đuôi phủ định khi câu chính có từ bán phủ định",
                            WrongExample = "He rarely exercises, doesn't he?",
                            CorrectExample = "He rarely exercises, does he?",
                            Explanation = "'Rarely' mang nghĩa phủ định, nên câu hỏi đuôi phải ở thể khẳng định ('does he')."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Dùng đại từ số ít cho 'everyone' ở câu hỏi đuôi",
                            WrongExample = "Everyone attended the lecture, didn't he?",
                            CorrectExample = "Everyone attended the lecture, didn't they?",
                            Explanation = "Trong câu hỏi đuôi, đại từ quy chiếu cho everyone/everybody bắt buộc là 'they'."
                        },
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "Scholars rarely encounter such pristine archival documents, _______?",
                            Options = new() { "do they", "don't they", "did they", "didn't they" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "do they",
                            Explanation = "Mệnh đề chính chứa từ bán phủ định 'rarely' và động từ hiện tại 'encounter' → câu hỏi đuôi ở thể khẳng định: 'do they?'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Nobody in the administrative division was informed of the impending restructuring, _______?",
                            Options = new() { "were they", "was they", "weren't they", "wasn't it" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "were they",
                            Explanation = "'Nobody' mang nghĩa phủ định và quy chiếu bằng đại từ 'they' ở đuôi; 'they' đi với to be 'were' ở thể khẳng định → 'were they?'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "I am responsible for delivering the keynote address tomorrow, _______?",
                            Options = new() { "aren't I", "am not I", "amn't I", "don't I" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "aren't I",
                            Explanation = "Trường hợp ngoại lệ đặc biệt của tiếng Anh chuẩn: 'I am...' có câu hỏi đuôi là 'aren't I?'."
                        },
                    },
                    LearningTip = "Trong Speaking Part 3, dùng câu hỏi đuôi với ngữ điệu hạ giọng cuối câu thể hiện bạn có khả năng kiểm soát ngữ điệu và sắc thái tự nhiên như người bản xứ!",
                    RelatedBandStructureCodes = new() { "ADV_TAG_01" },
                },
            }
        };
    }
}
