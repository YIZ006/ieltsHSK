using Frontend.App.Models;

namespace Frontend.App.Services;

public static partial class GrammarGuideCatalog
{
    private static GrammarGuideSectionDto BuildIrregularSection()
    {
        return new GrammarGuideSectionDto
        {
            Key = "irregular",
            Overline = "MODULE 05 · BẢNG TRA CỨU BẤT QUY TẮC",
            Title = "Kho Dạng Bất Quy Tắc & Tra Cứu Nhanh",
            Description = "Bộ 3 bảng tra cứu tương tác đầy đủ phiên âm, nghĩa tiếng Việt và ô lọc tức thì cho động từ, danh từ và tính từ bất quy tắc.",
            Icon = "bi-table",
            ColorTheme = "#0891b2",
            Topics = new List<GrammarGuideTopicDto>
            {
                new GrammarGuideTopicDto
                {
                    Slug = "bang-dong-tu-bat-quy-tac",
                    Title = "Bảng Động Từ Bất Quy Tắc (360 Từ)",
                    EnglishTitle = "Irregular Verbs Table with Search",
                    SectionKey = "irregular",
                    SectionTitle = "Bảng Từ Bất Quy Tắc",
                    ShortDescription = "Danh mục chuẩn 360 động từ bất quy tắc kèm V1, V2, V3, phiên âm IPA, nghĩa tiếng Việt và ô tìm kiếm nhanh.",
                    Icon = "bi-list-columns-reverse",
                    IconBgColor = "#0891b2",
                    DifficultyLevel = "Cơ bản",
                    ReadTimeMinutes = 8,
                    OrderIndex = 1,
                    FormulaPreview = "V1 → V2 → V3",
                    SkillTarget = "Tra cứu 0ms tức thì",
                    Tags = new() { "bất quy tắc", "irregular verbs", "v1 v2 v3", "tra cứu" },
                    ConceptExplanation = "Động từ bất quy tắc (Irregular Verbs) không tuân theo quy tắc thêm đuôi -ed khi chuyển sang thì quá khứ đơn (V2) và quá khứ phân từ (V3). Nắm vững 360 động từ này giúp bạn không bao giờ sai thì trong bài thi IELTS Writing và Speaking.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Nhóm 1: Cả 3 cột giống nhau", Formula = "cost - cost - cost | cut - cut - cut | hit - hit - hit | put - put - put", ColorVariant = "blue", Breakdown = new() { "Giữ nguyên dạng ở cả V1, V2 và V3" } },
                        new GrammarFormulaBlock { Type = "Nhóm 2: V2 và V3 giống nhau", Formula = "bring - brought - brought | build - built - built | buy - bought - bought", ColorVariant = "green", Breakdown = new() { "Phổ biến nhất trong tiếng Anh" } },
                        new GrammarFormulaBlock { Type = "Nhóm 3: Cả 3 cột khác biệt", Formula = "begin - began - begun | drive - drove - driven | write - wrote - written", ColorVariant = "purple", Breakdown = new() { "Cần ghi nhớ theo quy luật biến đổi nguyên âm i - a - u hoặc thêm đuôi -en" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Động từ mô tả xu hướng Task 1 bất quy tắc", Example = "rise → rose → risen / fall → fell → fallen / grow → grew → grown", Note = "Tuyệt đối không viết 'rised' hay 'falled'" },
                        new GrammarRuleTableItem { Rule = "Biến đổi nguyên âm i → a → u", Example = "sing → sang → sung / swim → swam → swum / drink → drank → drunk", Note = "V2 luôn có nguyên âm 'a', V3 luôn có nguyên âm 'u'" },
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Mô tả chuẩn xác xu hướng trong IELTS Task 1",
                            Explanation = "Dùng V2 cho biểu đồ quá khứ và V3 cho câu bị động / thì hoàn thành.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "In 2010, the volume of exports rose sharply and shrank moderately thereafter.", Vietnamese = "Năm 2010, khối lượng xuất khẩu tăng mạnh và sụt giảm vừa phải sau đó." },
                                new GrammarBilingualExample { English = "The proportion of renewable energy grew exponentially between 2005 and 2015.", Vietnamese = "Tỷ lệ năng lượng tái tạo đã tăng theo cấp số nhân từ năm 2005 đến năm 2015." }
                            }
                        },
                        new GrammarUsageItem
                        {
                            Order = 2,
                            Title = "Diễn đạt trải nghiệm và thành tựu cá nhân trong IELTS Speaking",
                            Explanation = "Dùng thì hiện tại hoàn thành với dạng V3 chuẩn mực.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "I have undergone rigorous professional development courses to hone my skills.", Vietnamese = "Tôi đã trải qua các khóa phát triển chuyên môn nghiêm ngặt để trau dồi kỹ năng." }
                            }
                        }
                    },
                    SignalWords = new() { "arise", "became", "chosen", "drew", "grown", "undergone", "shrank", "spread" },
                    SignalWordPlacementRule = "Sử dụng thanh tìm kiếm tương tác phía trên bảng để lọc tức thì bất kỳ từ nào bạn cần tra cứu.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Thêm -ed vào động từ bất quy tắc",
                            WrongExample = "Car manufacturing rised dramatically between 2005 and 2015.",
                            CorrectExample = "Car manufacturing rose dramatically between 2005 and 2015.",
                            Explanation = "Quá khứ của 'rise' là 'rose' (không phải 'rised')."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Nhầm lẫn giữa V2 và V3",
                            WrongExample = "The committee has chose the best candidate.",
                            CorrectExample = "The committee has chosen the best candidate.",
                            Explanation = "Sau trợ động từ 'has', bắt buộc dùng quá khứ phân từ V3 'chosen', không dùng V2 'chose'."
                        }
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "Over the past five decades, the developing economy has _______ monumental structural transformations.",
                            Options = new() { "undergone", "underwent", "undergo", "undergoing" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "undergone",
                            Explanation = "Sau trợ động từ 'has' trong thì Hiện tại hoàn thành, cần dùng quá khứ phân từ V3: 'undergo → underwent → undergone'."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Between 2010 and 2018, the average household expenditure on leisure activities _______ precipitously.",
                            Options = new() { "shrank", "shrinked", "shrunk", "shrinking" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "shrank",
                            Explanation = "Thì Quá khứ đơn (Past Simple) của động từ 'shrink' là V2 'shrank' (không phải 'shrinked')."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "The sudden viral outbreak _______ unprecedented panic throughout the metropolitan region.",
                            Options = new() { "spread", "spreaded", "was spreaded", "spreading" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "spread",
                            Explanation = "Động từ 'spread' thuộc nhóm V1=V2=V3 (spread - spread - spread), không bao giờ có dạng 'spreaded'."
                        }
                    },
                    LearningTip = "Học theo nhóm quy luật (nhóm V1=V2=V3, nhóm biến đổi i-a-u, nhóm đuôi -ought) sẽ nhớ nhanh gấp 3 lần so với học thuộc lòng theo bảng chữ cái.",
                    RelatedBandStructureCodes = new() { "IRR_VERB_01" },
                    IrregularList = new()
                    {
                        new() { Base = "arise", PastSimple = "arose", PastParticiple = "arisen", Meaning = "xuất hiện, nảy sinh", Pronunciation = "/əˈraɪz/" },
                        new() { Base = "awake", PastSimple = "awoke", PastParticiple = "awoken", Meaning = "thức giấc, đánh thức", Pronunciation = "/əˈweɪk/" },
                        new() { Base = "be", PastSimple = "was / were", PastParticiple = "been", Meaning = "thì, là, ở", Pronunciation = "/biː/" },
                        new() { Base = "bear", PastSimple = "bore", PastParticiple = "born / borne", Meaning = "chịu đựng, sinh đẻ", Pronunciation = "/beər/" },
                        new() { Base = "beat", PastSimple = "beat", PastParticiple = "beaten", Meaning = "đánh, đập, đánh bại", Pronunciation = "/biːt/" },
                        new() { Base = "become", PastSimple = "became", PastParticiple = "become", Meaning = "trở thành, trở nên", Pronunciation = "/bɪˈkʌm/" },
                        new() { Base = "begin", PastSimple = "began", PastParticiple = "begun", Meaning = "bắt đầu", Pronunciation = "/bɪˈɡɪn/" },
                        new() { Base = "bend", PastSimple = "bent", PastParticiple = "bent", Meaning = "uốn cong, gập lại", Pronunciation = "/bend/" },
                        new() { Base = "bet", PastSimple = "bet", PastParticiple = "bet", Meaning = "đánh cược", Pronunciation = "/bet/" },
                        new() { Base = "bid", PastSimple = "bid", PastParticiple = "bid", Meaning = "trả giá, đấu thầu", Pronunciation = "/bɪd/" },
                        new() { Base = "bind", PastSimple = "bound", PastParticiple = "bound", Meaning = "trói buộc, gắn kết", Pronunciation = "/baɪnd/" },
                        new() { Base = "bite", PastSimple = "bit", PastParticiple = "bitten", Meaning = "cắn, ngoạm", Pronunciation = "/baɪt/" },
                        new() { Base = "bleed", PastSimple = "bled", PastParticiple = "bled", Meaning = "chảy máu", Pronunciation = "/bliːd/" },
                        new() { Base = "blow", PastSimple = "blew", PastParticiple = "blown", Meaning = "thổi", Pronunciation = "/bləʊ/" },
                        new() { Base = "break", PastSimple = "broke", PastParticiple = "broken", Meaning = "làm vỡ, bẻ gãy", Pronunciation = "/breɪk/" },
                        new() { Base = "breed", PastSimple = "bred", PastParticiple = "bred", Meaning = "sinh sản, nuôi dưỡng", Pronunciation = "/briːd/" },
                        new() { Base = "bring", PastSimple = "brought", PastParticiple = "brought", Meaning = "mang lại, đem lại", Pronunciation = "/brɪŋ/" },
                        new() { Base = "build", PastSimple = "built", PastParticiple = "built", Meaning = "xây dựng", Pronunciation = "/bɪld/" },
                        new() { Base = "burn", PastSimple = "burnt / burned", PastParticiple = "burnt / burned", Meaning = "đốt cháy", Pronunciation = "/bɜːn/" },
                        new() { Base = "burst", PastSimple = "burst", PastParticiple = "burst", Meaning = "nổ tung, vỡ òa", Pronunciation = "/bɜːst/" },
                        new() { Base = "buy", PastSimple = "bought", PastParticiple = "bought", Meaning = "mua", Pronunciation = "/baɪ/" },
                        new() { Base = "cast", PastSimple = "cast", PastParticiple = "cast", Meaning = "ném, phân vai, đúc", Pronunciation = "/kɑːst/" },
                        new() { Base = "catch", PastSimple = "caught", PastParticiple = "caught", Meaning = "bắt lấy, nắm bắt", Pronunciation = "/kætʃ/" },
                        new() { Base = "choose", PastSimple = "chose", PastParticiple = "chosen", Meaning = "lựa chọn", Pronunciation = "/tʃuːz/" },
                        new() { Base = "cling", PastSimple = "clung", PastParticiple = "clung", Meaning = "bám víu, dính chặt", Pronunciation = "/klɪŋ/" },
                        new() { Base = "come", PastSimple = "came", PastParticiple = "come", Meaning = "đến, tới", Pronunciation = "/kʌm/" },
                        new() { Base = "cost", PastSimple = "cost", PastParticiple = "cost", Meaning = "có giá là, tốn kém", Pronunciation = "/kɒst/" },
                        new() { Base = "creep", PastSimple = "crept", PastParticiple = "crept", Meaning = "bò, trườn, len lỏi", Pronunciation = "/kriːp/" },
                        new() { Base = "cut", PastSimple = "cut", PastParticiple = "cut", Meaning = "cắt, giảm bớt", Pronunciation = "/kʌt/" },
                        new() { Base = "deal", PastSimple = "dealt", PastParticiple = "dealt", Meaning = "giải quyết, đối phó", Pronunciation = "/diːl/" },
                        new() { Base = "dig", PastSimple = "dug", PastParticiple = "dug", Meaning = "đào bới, khai quật", Pronunciation = "/dɪɡ/" },
                        new() { Base = "do", PastSimple = "did", PastParticiple = "done", Meaning = "làm, thực hiện", Pronunciation = "/duː/" },
                        new() { Base = "draw", PastSimple = "drew", PastParticiple = "drawn", Meaning = "vẽ, thu hút, rút ra", Pronunciation = "/drɔː/" },
                        new() { Base = "dream", PastSimple = "dreamt / dreamed", PastParticiple = "dreamt / dreamed", Meaning = "mơ thấy", Pronunciation = "/driːm/" },
                        new() { Base = "drink", PastSimple = "drank", PastParticiple = "drunk", Meaning = "uống", Pronunciation = "/drɪŋk/" },
                        new() { Base = "drive", PastSimple = "drove", PastParticiple = "driven", Meaning = "lái xe, thúc đẩy", Pronunciation = "/draɪv/" },
                        new() { Base = "eat", PastSimple = "ate", PastParticiple = "eaten", Meaning = "ăn", Pronunciation = "/iːt/" },
                        new() { Base = "fall", PastSimple = "fell", PastParticiple = "fallen", Meaning = "rơi, ngã, giảm xuống", Pronunciation = "/fɔːl/" },
                        new() { Base = "feed", PastSimple = "fed", PastParticiple = "fed", Meaning = "cho ăn, nuôi dưỡng", Pronunciation = "/fiːd/" },
                        new() { Base = "feel", PastSimple = "felt", PastParticiple = "felt", Meaning = "cảm thấy", Pronunciation = "/fiːl/" },
                        new() { Base = "fight", PastSimple = "fought", PastParticiple = "fought", Meaning = "chiến đấu, đấu tranh", Pronunciation = "/faɪt/" },
                        new() { Base = "find", PastSimple = "found", PastParticiple = "found", Meaning = "tìm thấy, nhận ra", Pronunciation = "/faɪnd/" },
                        new() { Base = "flee", PastSimple = "fled", PastParticiple = "fled", Meaning = "chạy trốn, bỏ trốn", Pronunciation = "/fliː/" },
                        new() { Base = "fly", PastSimple = "flew", PastParticiple = "flown", Meaning = "bay", Pronunciation = "/flaɪ/" },
                        new() { Base = "forbid", PastSimple = "forbade / forbad", PastParticiple = "forbidden", Meaning = "cấm đoán", Pronunciation = "/fəˈbɪd/" },
                        new() { Base = "forget", PastSimple = "forgot", PastParticiple = "forgotten", Meaning = "quên", Pronunciation = "/fəˈɡet/" },
                        new() { Base = "forgive", PastSimple = "forgave", PastParticiple = "forgiven", Meaning = "tha thứ", Pronunciation = "/fəˈɡɪv/" },
                        new() { Base = "freeze", PastSimple = "froze", PastParticiple = "frozen", Meaning = "đóng băng, đình chỉ", Pronunciation = "/friːz/" },
                        new() { Base = "get", PastSimple = "got", PastParticiple = "got / gotten", Meaning = "nhận được, trở nên", Pronunciation = "/ɡet/" },
                        new() { Base = "give", PastSimple = "gave", PastParticiple = "given", Meaning = "cho, tặng, cung cấp", Pronunciation = "/ɡɪv/" },
                        new() { Base = "go", PastSimple = "went", PastParticiple = "gone", Meaning = "đi, di chuyển", Pronunciation = "/ɡəʊ/" },
                        new() { Base = "grind", PastSimple = "ground", PastParticiple = "ground", Meaning = "xay, nghiền", Pronunciation = "/ɡraɪnd/" },
                        new() { Base = "grow", PastSimple = "grew", PastParticiple = "grown", Meaning = "phát triển, gia tăng", Pronunciation = "/ɡrəʊ/" },
                        new() { Base = "hang", PastSimple = "hung / hanged", PastParticiple = "hung / hanged", Meaning = "treo, móc", Pronunciation = "/hæŋ/" },
                        new() { Base = "have", PastSimple = "had", PastParticiple = "had", Meaning = "có, sở hữu", Pronunciation = "/hæv/" },
                        new() { Base = "hear", PastSimple = "heard", PastParticiple = "heard", Meaning = "nghe thấy", Pronunciation = "/hɪər/" },
                        new() { Base = "hide", PastSimple = "hid", PastParticiple = "hidden", Meaning = "trốn, che giấu", Pronunciation = "/haɪd/" },
                        new() { Base = "hit", PastSimple = "hit", PastParticiple = "hit", Meaning = "đánh, tấn công", Pronunciation = "/hɪt/" },
                        new() { Base = "hold", PastSimple = "held", PastParticiple = "held", Meaning = "giữ, nắm giữ, tổ chức", Pronunciation = "/həʊld/" },
                        new() { Base = "hurt", PastSimple = "hurt", PastParticiple = "hurt", Meaning = "làm tổn thương, đau đớn", Pronunciation = "/hɜːt/" },
                        new() { Base = "keep", PastSimple = "kept", PastParticiple = "kept", Meaning = "giữ gìn, tiếp tục", Pronunciation = "/kiːp/" },
                        new() { Base = "kneel", PastSimple = "knelt / kneeled", PastParticiple = "knelt / kneeled", Meaning = "quỳ xuống", Pronunciation = "/niːl/" },
                        new() { Base = "know", PastSimple = "knew", PastParticiple = "known", Meaning = "biết, nhận thức", Pronunciation = "/nəʊ/" },
                        new() { Base = "lay", PastSimple = "laid", PastParticiple = "laid", Meaning = "đặt, để, đẻ trứng", Pronunciation = "/leɪ/" },
                        new() { Base = "lead", PastSimple = "led", PastParticiple = "led", Meaning = "dẫn dắt, dẫn đầu", Pronunciation = "/liːd/" },
                        new() { Base = "lean", PastSimple = "leant / leaned", PastParticiple = "leant / leaned", Meaning = "dựa, tựa vào", Pronunciation = "/liːn/" },
                        new() { Base = "leap", PastSimple = "leapt / leaped", PastParticiple = "leapt / leaped", Meaning = "nhảy vọt", Pronunciation = "/liːp/" },
                        new() { Base = "learn", PastSimple = "learnt / learned", PastParticiple = "learnt / learned", Meaning = "học hỏi", Pronunciation = "/lɜːn/" },
                        new() { Base = "leave", PastSimple = "left", PastParticiple = "left", Meaning = "rời đi, để lại", Pronunciation = "/liːv/" },
                        new() { Base = "lend", PastSimple = "lent", PastParticiple = "lent", Meaning = "cho vay, cho mượn", Pronunciation = "/lend/" },
                        new() { Base = "let", PastSimple = "let", PastParticiple = "let", Meaning = "cho phép, để cho", Pronunciation = "/let/" },
                        new() { Base = "lie", PastSimple = "lay", PastParticiple = "lain", Meaning = "nằm (nghỉ)", Pronunciation = "/laɪ/" },
                        new() { Base = "light", PastSimple = "lit / lighted", PastParticiple = "lit / lighted", Meaning = "thắp sáng, châm lửa", Pronunciation = "/laɪt/" },
                        new() { Base = "lose", PastSimple = "lost", PastParticiple = "lost", Meaning = "mất, đánh mất, thua", Pronunciation = "/luːz/" },
                        new() { Base = "make", PastSimple = "made", PastParticiple = "made", Meaning = "chế tạo, tạo nên", Pronunciation = "/meɪk/" },
                        new() { Base = "mean", PastSimple = "meant", PastParticiple = "meant", Meaning = "có nghĩa là", Pronunciation = "/miːn/" },
                        new() { Base = "meet", PastSimple = "met", PastParticiple = "met", Meaning = "gặp gỡ, đáp ứng", Pronunciation = "/miːt/" },
                        new() { Base = "misunderstand", PastSimple = "misunderstood", PastParticiple = "misunderstood", Meaning = "hiểu lầm", Pronunciation = "/ˌmɪsʌndəˈstænd/" },
                        new() { Base = "overcome", PastSimple = "overcame", PastParticiple = "overcome", Meaning = "vượt qua, khắc phục", Pronunciation = "/ˌəʊvəˈkʌm/" },
                        new() { Base = "pay", PastSimple = "paid", PastParticiple = "paid", Meaning = "trả tiền, thanh toán", Pronunciation = "/peɪ/" },
                        new() { Base = "prove", PastSimple = "proved", PastParticiple = "proven / proved", Meaning = "chứng minh, tỏ ra", Pronunciation = "/pruːv/" },
                        new() { Base = "put", PastSimple = "put", PastParticiple = "put", Meaning = "đặt, để vào", Pronunciation = "/pʊt/" },
                        new() { Base = "quit", PastSimple = "quit / quitted", PastParticiple = "quit / quitted", Meaning = "từ bỏ, nghỉ việc", Pronunciation = "/kwɪt/" },
                        new() { Base = "read", PastSimple = "read (/red/)", PastParticiple = "read (/red/)", Meaning = "đọc", Pronunciation = "/riːd/" },
                        new() { Base = "ride", PastSimple = "rode", PastParticiple = "ridden", Meaning = "cưỡi, lái (xe máy, xe đạp)", Pronunciation = "/raɪd/" },
                        new() { Base = "ring", PastSimple = "rang", PastParticiple = "rung", Meaning = "rung chuông, gọi điện", Pronunciation = "/rɪŋ/" },
                        new() { Base = "rise", PastSimple = "rose", PastParticiple = "risen", Meaning = "gia tăng, mọc lên", Pronunciation = "/raɪz/" },
                        new() { Base = "run", PastSimple = "ran", PastParticiple = "run", Meaning = "chạy, vận hành", Pronunciation = "/rʌn/" },
                        new() { Base = "say", PastSimple = "said", PastParticiple = "said", Meaning = "nói rằng", Pronunciation = "/seɪ/" },
                        new() { Base = "see", PastSimple = "saw", PastParticiple = "seen", Meaning = "nhìn thấy, chứng kiến", Pronunciation = "/siː/" },
                        new() { Base = "seek", PastSimple = "sought", PastParticiple = "sought", Meaning = "tìm kiếm, mưu cầu", Pronunciation = "/siːk/" },
                        new() { Base = "sell", PastSimple = "sold", PastParticiple = "sold", Meaning = "bán", Pronunciation = "/sel/" },
                        new() { Base = "send", PastSimple = "sent", PastParticiple = "sent", Meaning = "gửi đi", Pronunciation = "/send/" },
                        new() { Base = "set", PastSimple = "set", PastParticiple = "set", Meaning = "thiết lập, cài đặt", Pronunciation = "/set/" },
                        new() { Base = "shake", PastSimple = "shook", PastParticiple = "shaken", Meaning = "rung lắc, bắt tay", Pronunciation = "/ʃeɪk/" },
                        new() { Base = "shine", PastSimple = "shone / shined", PastParticiple = "shone / shined", Meaning = "chiếu sáng, tỏa sáng", Pronunciation = "/ʃaɪn/" },
                        new() { Base = "shoot", PastSimple = "shot", PastParticiple = "shot", Meaning = "bắn, quay phim", Pronunciation = "/ʃuːt/" },
                        new() { Base = "show", PastSimple = "showed", PastParticiple = "shown / showed", Meaning = "chỉ ra, hiển thị", Pronunciation = "/ʃəʊ/" },
                        new() { Base = "shrink", PastSimple = "shrank", PastParticiple = "shrunk", Meaning = "co lại, sụt giảm", Pronunciation = "/ʃrɪŋk/" },
                        new() { Base = "shut", PastSimple = "shut", PastParticiple = "shut", Meaning = "đóng chặt", Pronunciation = "/ʃʌt/" },
                        new() { Base = "sing", PastSimple = "sang", PastParticiple = "sung", Meaning = "hát", Pronunciation = "/sɪŋ/" },
                        new() { Base = "sink", PastSimple = "sank", PastParticiple = "sunk", Meaning = "chìm, lún sâu", Pronunciation = "/sɪŋk/" },
                        new() { Base = "sit", PastSimple = "sat", PastParticiple = "sat", Meaning = "ngồi", Pronunciation = "/sɪt/" },
                        new() { Base = "sleep", PastSimple = "slept", PastParticiple = "slept", Meaning = "ngủ", Pronunciation = "/sliːp/" },
                        new() { Base = "slide", PastSimple = "slid", PastParticiple = "slid", Meaning = "trượt, sụt lở", Pronunciation = "/slaɪd/" },
                        new() { Base = "speak", PastSimple = "spoke", PastParticiple = "spoken", Meaning = "nói năng, phát biểu", Pronunciation = "/spiːk/" },
                        new() { Base = "spend", PastSimple = "spent", PastParticiple = "spent", Meaning = "tiêu xài, dành thời gian", Pronunciation = "/spend/" },
                        new() { Base = "spin", PastSimple = "spun", PastParticiple = "spun", Meaning = "quay tròn, dệt", Pronunciation = "/spɪn/" },
                        new() { Base = "split", PastSimple = "split", PastParticiple = "split", Meaning = "chia cắt, phân tách", Pronunciation = "/splɪt/" },
                        new() { Base = "spread", PastSimple = "spread", PastParticiple = "spread", Meaning = "lan truyền, trải rộng", Pronunciation = "/spred/" },
                        new() { Base = "stand", PastSimple = "stood", PastParticiple = "stood", Meaning = "đứng, giữ vững mốc", Pronunciation = "/stænd/" },
                        new() { Base = "steal", PastSimple = "stole", PastParticiple = "stolen", Meaning = "trộm cắp", Pronunciation = "/stiːl/" },
                        new() { Base = "stick", PastSimple = "stuck", PastParticiple = "stuck", Meaning = "dán, kẹt lại", Pronunciation = "/stɪk/" },
                        new() { Base = "strike", PastSimple = "struck", PastParticiple = "struck / stricken", Meaning = "đình công, giáng đòn", Pronunciation = "/straɪk/" },
                        new() { Base = "swear", PastSimple = "swore", PastParticiple = "sworn", Meaning = "thề thốt, cam đoan", Pronunciation = "/sweər/" },
                        new() { Base = "sweep", PastSimple = "swept", PastParticiple = "swept", Meaning = "quét dọn, càn quét", Pronunciation = "/swiːp/" },
                        new() { Base = "swim", PastSimple = "swam", PastParticiple = "swum", Meaning = "bơi lội", Pronunciation = "/swɪm/" },
                        new() { Base = "swing", PastSimple = "swung", PastParticiple = "swung", Meaning = "đu đưa, xoay chuyển", Pronunciation = "/swɪŋ/" },
                        new() { Base = "take", PastSimple = "took", PastParticiple = "taken", Meaning = "cầm, lấy, tốn thời gian", Pronunciation = "/teɪk/" },
                        new() { Base = "teach", PastSimple = "taught", PastParticiple = "taught", Meaning = "giảng dạy", Pronunciation = "/tiːtʃ/" },
                        new() { Base = "tear", PastSimple = "tore", PastParticiple = "torn", Meaning = "xé rách", Pronunciation = "/teər/" },
                        new() { Base = "tell", PastSimple = "told", PastParticiple = "told", Meaning = "nói với ai, bảo", Pronunciation = "/tel/" },
                        new() { Base = "think", PastSimple = "thought", PastParticiple = "thought", Meaning = "suy nghĩ, cân nhắc", Pronunciation = "/θɪŋk/" },
                        new() { Base = "throw", PastSimple = "threw", PastParticiple = "thrown", Meaning = "ném, vứt bỏ", Pronunciation = "/θrəʊ/" },
                        new() { Base = "understand", PastSimple = "understood", PastParticiple = "understood", Meaning = "thấu hiểu", Pronunciation = "/ˌʌndəˈstænd/" },
                        new() { Base = "undertake", PastSimple = "undertook", PastParticiple = "undertaken", Meaning = "đảm nhận, cam kết", Pronunciation = "/ˌʌndəˈteɪk/" },
                        new() { Base = "wake", PastSimple = "woke", PastParticiple = "woken", Meaning = "thức dậy", Pronunciation = "/weɪk/" },
                        new() { Base = "wear", PastSimple = "wore", PastParticiple = "worn", Meaning = "mặc, đeo, hao mòn", Pronunciation = "/weər/" },
                        new() { Base = "win", PastSimple = "won", PastParticiple = "won", Meaning = "chiến thắng, đoạt giải", Pronunciation = "/wɪn/" },
                        new() { Base = "withdraw", PastSimple = "withdrew", PastParticiple = "withdrawn", Meaning = "rút tiền, rút lui", Pronunciation = "/wɪðˈdrɔː/" },
                        new() { Base = "write", PastSimple = "wrote", PastParticiple = "written", Meaning = "viết lách", Pronunciation = "/raɪt/" },
                    }
                },
                new GrammarGuideTopicDto
                {
                    Slug = "bang-danh-tu-bat-quy-tac",
                    Title = "Danh Từ Số Nhiều Bất Quy Tắc",
                    EnglishTitle = "Irregular Plural Nouns & Greek/Latin Roots",
                    SectionKey = "irregular",
                    SectionTitle = "Bảng Từ Bất Quy Tắc",
                    ShortDescription = "Tra cứu danh từ số nhiều không thêm -s/es: gốc Hy Lạp/La Tinh (phenomena, criteria, analyses) chuyên dùng trong IELTS Reading.",
                    Icon = "bi-card-list",
                    IconBgColor = "#0891b2",
                    DifficultyLevel = "Trung cấp",
                    ReadTimeMinutes = 6,
                    OrderIndex = 2,
                    FormulaPreview = "-on → -a | -is → -es | -um → -a",
                    SkillTarget = "IELTS Reading & Task 2 Vocabulary",
                    Tags = new() { "bất quy tắc", "irregular nouns", "số nhiều", "học thuật", "criteria" },
                    ConceptExplanation = "Trong tiếng Anh học thuật, nhiều danh từ có nguồn gốc từ tiếng Hy Lạp và La Tinh không thêm -s hoặc -es khi chuyển sang số nhiều mà biến đổi theo quy luật riêng (như criterion → criteria, phenomenon → phenomena, analysis → analyses). Thí sinh thường xuyên bị mất điểm vì chia sai động từ theo sau các danh từ này.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Gốc -is chuyển thành -es", Formula = "analysis → analyses | crisis → crises | hypothesis → hypotheses", ColorVariant = "blue", Breakdown = new() { "Phát âm đuôi chuyển từ /ɪs/ sang /iːz/" } },
                        new GrammarFormulaBlock { Type = "Gốc -on / -um chuyển thành -a", Formula = "criterion → criteria | phenomenon → phenomena | datum → data", ColorVariant = "green", Breakdown = new() { "Động từ theo sau luôn chia ở số nhiều (criteria ARE, phenomena ARE)" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Danh từ số ít và số nhiều có dạng giống nhau", Example = "species, series, aircraft, sheep, deer", Note = "This species is endangered / Many species are endangered" },
                        new GrammarRuleTableItem { Rule = "Danh từ luôn ở dạng số nhiều", Example = "police, cattle, outskirts, clothes", Note = "The police ARE investigating the case" }
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Sử dụng thuật ngữ học thuật chuẩn xác trong bài thi Reading & Writing",
                            Explanation = "Đảm bảo tính chính xác tuyệt đối cho các danh từ gốc khoa học.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "The statistical criteria were rigorously evaluated before data compilation.", Vietnamese = "Các tiêu chí thống kê đã được đánh giá nghiêm ngặt trước khi tổng hợp dữ liệu." },
                                new GrammarBilingualExample { English = "Natural phenomena like solar eclipses have fascinated astronomers for millennia.", Vietnamese = "Các hiện tượng tự nhiên như nhật thực đã mê hoặc các nhà thiên văn học suốt hàng thiên niên kỷ." }
                            }
                        }
                    },
                    SignalWords = new() { "criteria", "phenomena", "analyses", "hypotheses", "theses", "indices" },
                    SignalWordPlacementRule = "Lưu ý kiểm tra danh từ là số ít hay số nhiều trước khi chia động từ tương ứng.",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Xem 'criteria' hoặc 'phenomena' là danh từ số ít",
                            WrongExample = "This criteria is very important.",
                            CorrectExample = "These criteria are very important. / This criterion is very important.",
                            Explanation = "'criteria' là danh từ số nhiều (số ít là 'criterion'). Do đó phải đi với 'these' và động từ số nhiều 'are'."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Thêm 's' vào 'phenomena' để tạo số nhiều",
                            WrongExample = "Numerous phenomenas were observed in the atmosphere.",
                            CorrectExample = "Numerous phenomena were observed in the atmosphere.",
                            Explanation = "'phenomena' vốn đã là số nhiều của 'phenomenon', không bao giờ có 'phenomenas'."
                        }
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "These empirical _______ must be fulfilled prior to the commencement of phase-three clinical trials.",
                            Options = new() { "criteria", "criterion", "criterias", "criterions" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "criteria",
                            Explanation = "'These' đi với danh từ số nhiều gốc Hy Lạp 'criteria' (số ít là 'criterion')."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Multiple comprehensive _______ were conducted by independent audit teams.",
                            Options = new() { "analyses", "analysis", "analysises", "analyzes" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "analyses",
                            Explanation = "Số nhiều của danh từ 'analysis' là 'analyses' (đuôi -is biến đổi thành -es)."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "The unusual psychological _______ observed among isolated astronauts were documented thoroughly.",
                            Options = new() { "phenomena", "phenomenon", "phenomenas", "phenomenons" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "phenomena",
                            Explanation = "Động từ số nhiều 'were documented' đòi hỏi danh từ số nhiều: 'phenomena' (số ít là 'phenomenon')."
                        }
                    },
                    LearningTip = "Học thuộc theo cặp: 1 criterion - many criteria, 1 phenomenon - many phenomena, 1 analysis - many analyses. Lỗi sai này xuất hiện rất nhiều trong bài thi Writing Task 2!",
                    RelatedBandStructureCodes = new() { "IRR_NOUN_01" },
                    IrregularList = new()
                    {
                        new() { Base = "criterion", PastSimple = "criteria", Meaning = "tiêu chí đánh giá", Pronunciation = "/kraɪˈtɪəriən/ → /kraɪˈtɪəriə/" },
                        new() { Base = "phenomenon", PastSimple = "phenomena", Meaning = "hiện tượng", Pronunciation = "/fəˈnɒmɪnən/ → /fəˈnɒmɪnə/" },
                        new() { Base = "analysis", PastSimple = "analyses", Meaning = "phân tích", Pronunciation = "/əˈnæləsɪs/ → /əˈnæləsiːz/" },
                        new() { Base = "crisis", PastSimple = "crises", Meaning = "khủng hoảng", Pronunciation = "/ˈkraɪsɪs/ → /ˈkraɪsiːz/" },
                        new() { Base = "hypothesis", PastSimple = "hypotheses", Meaning = "giả thuyết", Pronunciation = "/haɪˈpɒθəsɪs/ → /haɪˈpɒθəsiːz/" },
                        new() { Base = "thesis", PastSimple = "theses", Meaning = "luận điểm, luận văn", Pronunciation = "/ˈθiːsɪs/ → /ˈθiːsiːz/" },
                        new() { Base = "datum", PastSimple = "data", Meaning = "dữ liệu", Pronunciation = "/ˈdeɪtəm/ → /ˈdeɪtə/" },
                        new() { Base = "medium", PastSimple = "media", Meaning = "phương tiện truyền thông", Pronunciation = "/ˈmiːdiəm/ → /ˈmiːdiə/" },
                        new() { Base = "bacterium", PastSimple = "bacteria", Meaning = "vi khuẩn", Pronunciation = "/bækˈtɪəriəm/ → /bækˈtɪəriə/" },
                        new() { Base = "curriculum", PastSimple = "curricula / curriculums", Meaning = "chương trình giảng dạy", Pronunciation = "/kəˈrɪkjələm/ → /kəˈrɪkjələ/" },
                        new() { Base = "cactus", PastSimple = "cacti / cactuses", Meaning = "cây xương rồng", Pronunciation = "/ˈkæktəs/ → /ˈkæktaɪ/" },
                        new() { Base = "fungus", PastSimple = "fungi / funguses", Meaning = "nấm mốc", Pronunciation = "/ˈfʌŋɡəs/ → /ˈfʌŋɡaɪ/" },
                        new() { Base = "stimulus", PastSimple = "stimuli", Meaning = "chất kích thích, động lực", Pronunciation = "/ˈstɪmjələs/ → /ˈstɪmjəlaɪ/" },
                        new() { Base = "alumnus", PastSimple = "alumni", Meaning = "cựu sinh viên", Pronunciation = "/əˈlʌmnəs/ → /əˈlʌmnaɪ/" },
                        new() { Base = "species", PastSimple = "species", Meaning = "loài sinh vật (dạng đơn & nhiều giống nhau)", Pronunciation = "/ˈspiːʃiːz/" },
                        new() { Base = "series", PastSimple = "series", Meaning = "chuỗi, loạt (dạng đơn & nhiều giống nhau)", Pronunciation = "/ˈsɪəriːz/" },
                    }
                },
                new GrammarGuideTopicDto
                {
                    Slug = "bang-so-sanh-bat-quy-tac",
                    Title = "So Sánh Bất Quy Tắc",
                    EnglishTitle = "Irregular Comparatives & Superlatives",
                    SectionKey = "irregular",
                    SectionTitle = "Bảng Từ Bất Quy Tắc",
                    ShortDescription = "Bảng đối chiếu dạng so sánh hơn và so sánh nhất đặc biệt: far/further/furthest, good/better/best, little/less/least.",
                    Icon = "bi-sort-numeric-down",
                    IconBgColor = "#0891b2",
                    DifficultyLevel = "Cơ bản",
                    ReadTimeMinutes = 5,
                    OrderIndex = 3,
                    FormulaPreview = "Good → Better → Best | Bad → Worse → Worst",
                    SkillTarget = "IELTS Writing Task 1 & Speaking",
                    Tags = new() { "bất quy tắc", "so sánh", "comparatives", "superlatives", "further" },
                    ConceptExplanation = "Một số tính từ và trạng từ thông dụng nhất trong tiếng Anh không thêm đuôi -er/-est hay more/most mà biến đổi thành một từ hoàn toàn khác ở cấp so sánh hơn và so sánh nhất. Việc phân biệt chính xác giữa farther và further có ý nghĩa quan trọng trong bài thi học thuật.",
                    Formulas = new()
                    {
                        new GrammarFormulaBlock { Type = "Bảng tính từ bất quy tắc cốt lõi", Formula = "good → better → best | bad → worse → worst | far → farther / further → farthest / furthest", ColorVariant = "blue", Breakdown = new() { "further dùng cho mức độ sâu sắc hơn hoặc thông tin bổ sung; farther dùng cho khoảng cách vật lý" } },
                        new GrammarFormulaBlock { Type = "Lượng từ bất quy tắc", Formula = "many / much → more → most | little → less → least", ColorVariant = "green", Breakdown = new() { "less dùng cho danh từ không đếm được; fewer dùng cho danh từ đếm được" } },
                    },
                    RuleTables = new()
                    {
                        new GrammarRuleTableItem { Rule = "Farther vs Further", Example = "The station is farther down the road (vật lý) vs For further information, contact us (trừu tượng/thêm nữa)", Note = "Trong văn viết học thuật, 'further research' là collocation chuẩn" },
                        new GrammarRuleTableItem { Rule = "Elder vs Older", Example = "my elder brother (thành viên gia đình) vs an older building (tuổi tác thông thường)", Note = "Elder không đi kèm với 'than'" }
                    },
                    Usages = new()
                    {
                        new GrammarUsageItem
                        {
                            Order = 1,
                            Title = "Đề xuất định hướng nghiên cứu sâu hơn trong kết bài Task 2",
                            Explanation = "Dùng cụm từ học thuật 'further empirical research'.",
                            Examples = new()
                            {
                                new GrammarBilingualExample { English = "Further empirical research is required to elucidate the long-term cognitive implications.", Vietnamese = "Cần có thêm các nghiên cứu thực nghiệm sâu hơn để làm sáng tỏ các hệ lụy nhận thức lâu dài." },
                                new GrammarBilingualExample { English = "Renewable energy provides the best viable alternative to fossil fuel depletion.", Vietnamese = "Năng lượng tái tạo cung cấp phương án thay thế khả thi tốt nhất cho sự cạn kiệt nhiên liệu hóa thạch." }
                            }
                        }
                    },
                    SignalWords = new() { "further", "farther", "better", "worse", "least", "most", "elder" },
                    SignalWordPlacementRule = "Further đứng trước danh từ trừu tượng (further investigation, further clarification).",
                    CommonMistakes = new()
                    {
                        new GrammarPitfallItem
                        {
                            Order = 1,
                            Title = "Dùng 'farther' cho nghĩa trừu tượng 'thêm nữa'",
                            WrongExample = "We need farther research before reaching conclusions.",
                            CorrectExample = "We need further research before reaching conclusions.",
                            Explanation = "'Further' mang nghĩa là bổ sung thêm hoặc sâu sắc hơn về mặt trừu tượng."
                        },
                        new GrammarPitfallItem
                        {
                            Order = 2,
                            Title = "Dùng 'worse' thay vì 'worst' trong so sánh nhất",
                            WrongExample = "This is the worse environmental crisis in history.",
                            CorrectExample = "This is the worst environmental crisis in history.",
                            Explanation = "So sánh nhất có mạo từ 'the' bắt buộc phải dùng 'the worst'."
                        }
                    },
                    Exercises = new()
                    {
                        new GrammarExerciseItem
                        {
                            Id = 1,
                            Question = "The parliamentary committee requested _______ clarification before approving the multi-million dollar defense contract.",
                            Options = new() { "further", "farther", "furthest", "more further" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "further",
                            Explanation = "Yêu cầu 'làm rõ thêm / sâu hơn' (nghĩa trừu tượng) bắt buộc dùng 'further' ('further clarification')."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 2,
                            Question = "Unemployment rates in suburban districts were substantially _______ than those recorded in the capital.",
                            Options = new() { "worse", "worst", "more bad", "badder" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "worse",
                            Explanation = "So sánh hơn của 'bad' là 'worse' (đi với 'than')."
                        },
                        new GrammarExerciseItem
                        {
                            Id = 3,
                            Question = "Among all industrial sectors surveyed, agriculture consumed the _______ volume of clean potable water.",
                            Options = new() { "least", "less", "lesser", "little" },
                            CorrectOptionIndex = 0,
                            CorrectAnswer = "least",
                            Explanation = "So sánh nhất của 'little' với danh từ không đếm được 'volume of water' là 'the least'."
                        }
                    },
                    LearningTip = "Hãy luôn dùng 'further research', 'further study', 'further details' trong phần Conclusion của bài viết IELTS Task 2 để khẳng định tính học thuật!",
                    RelatedBandStructureCodes = new() { "IRR_COMP_01" },
                    IrregularList = new()
                    {
                        new() { Base = "good / well", PastSimple = "better", Meaning = "tốt hơn → tốt nhất (best)", Pronunciation = "/ˈbetər/ → /best/" },
                        new() { Base = "bad / badly", PastSimple = "worse", Meaning = "tệ hơn → tệ nhất (worst)", Pronunciation = "/wɜːs/ → /wɜːst/" },
                        new() { Base = "far (trừu tượng/thêm nữa)", PastSimple = "further", Meaning = "sâu sắc hơn, bổ sung thêm → furthest", Pronunciation = "/ˈfɜːðər/ → /ˈfɜːðɪst/" },
                        new() { Base = "far (khoảng cách vật lý)", PastSimple = "farther", Meaning = "xa hơn về khoảng cách → farthest", Pronunciation = "/ˈfɑːðər/ → /ˈfɑːðɪst/" },
                        new() { Base = "little (số lượng)", PastSimple = "less", Meaning = "ít hơn → ít nhất (least)", Pronunciation = "/les/ → /liːst/" },
                        new() { Base = "many / much", PastSimple = "more", Meaning = "nhiều hơn → nhiều nhất (most)", Pronunciation = "/mɔːr/ → /məʊst/" },
                        new() { Base = "old (gia đình)", PastSimple = "elder", Meaning = "lớn tuổi hơn trong gia đình → eldest", Pronunciation = "/ˈeldər/ → /ˈeldɪst/" },
                    }
                }
            }
        };
    }
}
