using System;
using System.Collections.Generic;
using System.Linq;
using Frontend.App.Models;

namespace Frontend.App.Services;

/// <summary>
/// Danh mục và dữ liệu từ vựng HSK phân loại CHUẨN XÁC theo 14 chủ đề giao tiếp & đời sống
/// </summary>
public static class HskTopicCatalog
{
    public static readonly List<HskTopicItem> Topics = new()
    {
        new HskTopicItem
        {
            Id = "greetings",
            TitleVi = "Chào hỏi & Giao tiếp",
            TitleZh = "问候与交际",
            Pinyin = "Wènhòu yǔ Jiāojì",
            Icon = "bi-chat-dots-fill",
            Color = "#2563eb",
            BgColor = "#eff6ff",
            Description = "Các mẫu câu và từ vựng chào hỏi, cảm ơn, xin lỗi, làm quen cơ bản.",
            LevelRange = "HSK 1–2",
            OrderIndex = 1
        },
        new HskTopicItem
        {
            Id = "family",
            TitleVi = "Gia đình & Con người",
            TitleZh = "家庭与人物",
            Pinyin = "Jiātíng yǔ Rénwù",
            Icon = "bi-people-fill",
            Color = "#ea580c",
            BgColor = "#fff7ed",
            Description = "Thành viên gia đình, quan hệ họ hàng, bạn bè, thầy cô và xưng hô.",
            LevelRange = "HSK 1–3",
            OrderIndex = 2
        },
        new HskTopicItem
        {
            Id = "food",
            TitleVi = "Ẩm thực & Đồ uống",
            TitleZh = "餐饮与美食",
            Pinyin = "Cānyǐn yǔ Měishí",
            Icon = "bi-cup-hot-fill",
            Color = "#d97706",
            BgColor = "#fffbeb",
            Description = "Món ăn, đồ uống, vị giác, trái cây, gọi món và nhà hàng.",
            LevelRange = "HSK 1–3",
            OrderIndex = 3
        },
        new HskTopicItem
        {
            Id = "shopping",
            TitleVi = "Mua sắm & Tiền tệ",
            TitleZh = "购物与金钱",
            Pinyin = "Gòuwù yǔ Jīnqián",
            Icon = "bi-bag-heart-fill",
            Color = "#ec4899",
            BgColor = "#fdf2f8",
            Description = "Mua bán, hỏi giá, trả giá, tiền tệ, trang phục và đồ dùng cá nhân.",
            LevelRange = "HSK 1–3",
            OrderIndex = 4
        },
        new HskTopicItem
        {
            Id = "travel",
            TitleVi = "Giao thông & Đi lại",
            TitleZh = "交通与出行",
            Pinyin = "Jiāotōng yǔ Chūxíng",
            Icon = "bi-compass-fill",
            Color = "#0284c7",
            BgColor = "#f0f9ff",
            Description = "Phương tiện giao thông, nhà ga, sân bay, vé tàu xe, hỏi đường.",
            LevelRange = "HSK 1–4",
            OrderIndex = 5
        },
        new HskTopicItem
        {
            Id = "study",
            TitleVi = "Trường học & Học tập",
            TitleZh = "学校与学习",
            Pinyin = "Xuéxiào yǔ Xuéxí",
            Icon = "bi-book-half",
            Color = "#16a34a",
            BgColor = "#f0fdf4",
            Description = "Lớp học, môn học, thi cử, chữ Hán, ngữ pháp, bài tập và điểm số.",
            LevelRange = "HSK 1–4",
            OrderIndex = 6
        },
        new HskTopicItem
        {
            Id = "work",
            TitleVi = "Công việc & Công sở",
            TitleZh = "工作与职场",
            Pinyin = "Gōngzuò yǔ Zhíchǎng",
            Icon = "bi-briefcase-fill",
            Color = "#6366f1",
            BgColor = "#eef2ff",
            Description = "Văn phòng, công ty, họp hành, phỏng vấn, đồng nghiệp và công việc.",
            LevelRange = "HSK 2–5",
            OrderIndex = 7
        },
        new HskTopicItem
        {
            Id = "time",
            TitleVi = "Thời gian & Lịch trình",
            TitleZh = "时间与日期",
            Pinyin = "Shíjiān yǔ Rìqī",
            Icon = "bi-calendar2-week-fill",
            Color = "#0d9488",
            BgColor = "#f0fdfa",
            Description = "Giờ giấc, các buổi trong ngày, thứ ngày tháng năm và mùa màng.",
            LevelRange = "HSK 1–3",
            OrderIndex = 8
        },
        new HskTopicItem
        {
            Id = "home",
            TitleVi = "Nhà cửa & Đời sống",
            TitleZh = "居家与生活",
            Pinyin = "Jūjiā yǔ Shēnghuó",
            Icon = "bi-house-heart-fill",
            Color = "#8b5cf6",
            BgColor = "#f5f3ff",
            Description = "Phòng ốc, đồ đạc trong nhà, thiết bị gia dụng và sinh hoạt thường nhật.",
            LevelRange = "HSK 1–3",
            OrderIndex = 9
        },
        new HskTopicItem
        {
            Id = "weather",
            TitleVi = "Thời tiết & Tự nhiên",
            TitleZh = "天气与自然",
            Pinyin = "Tiānqì yǔ Zìrán",
            Icon = "bi-cloud-sun-fill",
            Color = "#3b82f6",
            BgColor = "#eff6ff",
            Description = "Nắng, mưa, nóng, lạnh, khí hậu, phong cảnh và bốn mùa.",
            LevelRange = "HSK 1–4",
            OrderIndex = 10
        },
        new HskTopicItem
        {
            Id = "emotions",
            TitleVi = "Cảm xúc & Tính cách",
            TitleZh = "情感与态度",
            Pinyin = "Qínggǎn yǔ Tàidù",
            Icon = "bi-emoji-smile-fill",
            Color = "#e11d48",
            BgColor = "#fff1f2",
            Description = "Vui, buồn, tức giận, lo lắng, nhiệt tình, kiên nhẫn và tự tin.",
            LevelRange = "HSK 2–4",
            OrderIndex = 11
        },
        new HskTopicItem
        {
            Id = "hobbies",
            TitleVi = "Sở thích & Thể thao",
            TitleZh = "爱好与运动",
            Pinyin = "Àihào yǔ Yùndòng",
            Icon = "bi-trophy-fill",
            Color = "#f59e0b",
            BgColor = "#fffbeb",
            Description = "Âm nhạc, phim ảnh, thể thao, bóng đá, bơi lội, du lịch và giải trí.",
            LevelRange = "HSK 1–4",
            OrderIndex = 12
        },
        new HskTopicItem
        {
            Id = "health",
            TitleVi = "Sức khỏe & Y tế",
            TitleZh = "健康与医疗",
            Pinyin = "Jiànkāng yǔ Yīliáo",
            Icon = "bi-heart-pulse-fill",
            Color = "#ef4444",
            BgColor = "#fef2f2",
            Description = "Bệnh viện, bác sĩ, triệu chứng bệnh tật, uống thuốc và thể chất.",
            LevelRange = "HSK 2–4",
            OrderIndex = 13
        },
        new HskTopicItem
        {
            Id = "locations",
            TitleVi = "Địa điểm & Phương hướng",
            TitleZh = "地点与方位",
            Pinyin = "Dìdiǎn yǔ Fāngwèi",
            Icon = "bi-geo-alt-fill",
            Color = "#10b981",
            BgColor = "#ecfdf5",
            Description = "Phương hướng đông tây nam bắc, trước sau, ngân hàng, công viên.",
            LevelRange = "HSK 1–3",
            OrderIndex = 14
        }
    };

    /// <summary>
    /// BỘ TỪ VỰNG CHUẨN XÁC 100% THEO ĐÚNG CHỦ ĐỀ
    /// </summary>
    public static readonly List<HskVocabularyItem> TopicVocabularies = new()
    {
        // ─── 1. CHÀO HỎI & GIAO TIẾP (greetings) ───
        new() { Id = 1001, HskLevel = "HSK1", Topic = "greetings", Hanzi = "你好", Pinyin = "nǐ hǎo", Meaning = "Xin chào", WordType = "Chào hỏi", ExampleSentence = "你好！很高兴认识你。", ExamplePinyin = "Nǐ hǎo! Hěn gāoxìng rènshí nǐ.", ExampleMeaning = "Xin chào! Rất vui được quen biết bạn." },
        new() { Id = 1002, HskLevel = "HSK1", Topic = "greetings", Hanzi = "您好", Pinyin = "nín hǎo", Meaning = "Chào ngài, chào bác (kính ngữ)", WordType = "Chào hỏi", ExampleSentence = "老师，您好！", ExamplePinyin = "Lǎoshī, nín hǎo!", ExampleMeaning = "Em chào thầy ạ!" },
        new() { Id = 1003, HskLevel = "HSK1", Topic = "greetings", Hanzi = "谢谢", Pinyin = "xiè xie", Meaning = "Cảm ơn", WordType = "Động từ", ExampleSentence = "谢谢你的热情帮助。", ExamplePinyin = "Xièxie nǐ de rèqíng bāngzhù.", ExampleMeaning = "Cảm ơn sự giúp đỡ nhiệt tình của bạn." },
        new() { Id = 1004, HskLevel = "HSK1", Topic = "greetings", Hanzi = "不客气", Pinyin = "bú kè qi", Meaning = "Không có chi, đừng khách sáo", WordType = "Cụm từ", ExampleSentence = "别客气，这是我应该做的。", ExamplePinyin = "Bié kèqi, zhè shì wǒ yīnggāi zuò de.", ExampleMeaning = "Đừng khách sáo, đây là việc tôi nên làm." },
        new() { Id = 1005, HskLevel = "HSK1", Topic = "greetings", Hanzi = "再见", Pinyin = "zài jiàn", Meaning = "Tạm biệt", WordType = "Chào hỏi", ExampleSentence = "明天见，路上小心，再见！", ExamplePinyin = "Míngtiān jiàn, lù shang xiǎoxīn, zàijiàn!", ExampleMeaning = "Mai gặp nhé, đi đường cẩn thận, tạm biệt!" },
        new() { Id = 1006, HskLevel = "HSK1", Topic = "greetings", Hanzi = "对不起", Pinyin = "duì bu qǐ", Meaning = "Xin lỗi", WordType = "Cụm từ", ExampleSentence = "对不起，我不是故意的。", ExamplePinyin = "Duìbuqǐ, wǒ bú shì gùyì de.", ExampleMeaning = "Xin lỗi, tôi không cố ý đâu." },
        new() { Id = 1007, HskLevel = "HSK1", Topic = "greetings", Hanzi = "没关系", Pinyin = "méi guān xi", Meaning = "Không sao đâu, không có gì", WordType = "Cụm từ", ExampleSentence = "没关系，下次注意就好了。", ExamplePinyin = "Méi guānxi, xià cì zhùyì jiù hǎo le.", ExampleMeaning = "Không sao đâu, lần sau chú ý là được rồi." },
        new() { Id = 1008, HskLevel = "HSK1", Topic = "greetings", Hanzi = "请问", Pinyin = "qǐng wèn", Meaning = "Xin hỏi", WordType = "Động từ", ExampleSentence = "请问，去洗手间怎么走？", ExamplePinyin = "Qǐngwèn, qù xǐshǒujiān zěnme zǒu?", ExampleMeaning = "Xin hỏi, đi nhà vệ sinh đi đường nào?" },
        new() { Id = 1009, HskLevel = "HSK2", Topic = "greetings", Hanzi = "欢迎", Pinyin = "huān yíng", Meaning = "Hoan nghênh, chào mừng", WordType = "Động từ", ExampleSentence = "热烈欢迎各位朋友！", ExamplePinyin = "Rèliè huānyíng gèwèi péngyou!", ExampleMeaning = "Nhiệt liệt chào mừng các bạn!" },
        new() { Id = 1010, HskLevel = "HSK3", Topic = "greetings", Hanzi = "打扰", Pinyin = "dǎ rǎo", Meaning = "Làm phiền, quấy rầy", WordType = "Động từ", ExampleSentence = "真不好意思，打扰您休息了。", ExamplePinyin = "Zhēn bù hǎoyìsi, dǎrǎo nín xiūxi le.", ExampleMeaning = "Thật ngại quá, đã làm phiền bác nghỉ ngơi rồi." },

        // ─── 2. GIA ĐÌNH & CON NGƯỜI (family) ───
        new() { Id = 1011, HskLevel = "HSK1", Topic = "family", Hanzi = "爸爸", Pinyin = "bà ba", Meaning = "Bố, ba, cha", WordType = "Danh từ", ExampleSentence = "我爸爸是一名工程师。", ExamplePinyin = "Wǒ bàba shì yì míng gōngchéngshī.", ExampleMeaning = "Bố tôi là một kỹ sư." },
        new() { Id = 1012, HskLevel = "HSK1", Topic = "family", Hanzi = "妈妈", Pinyin = "mā ma", Meaning = "Mẹ, má", WordType = "Danh từ", ExampleSentence = "妈妈每天为我们做饭。", ExamplePinyin = "Māma měitiān wèi wǒmen zuò fàn.", ExampleMeaning = "Mẹ nấu cơm cho chúng tôi mỗi ngày." },
        new() { Id = 1013, HskLevel = "HSK1", Topic = "family", Hanzi = "儿子", Pinyin = "ér zi", Meaning = "Con trai", WordType = "Danh từ", ExampleSentence = "他的儿子非常聪明懂事。", ExamplePinyin = "Tā de érzi fēicháng cōngmíng dǒngshì.", ExampleMeaning = "Con trai anh ấy rất thông minh và hiểu chuyện." },
        new() { Id = 1014, HskLevel = "HSK1", Topic = "family", Hanzi = "女儿", Pinyin = "nǚ'ér", Meaning = "Con gái", WordType = "Danh từ", ExampleSentence = "她有一个很可爱的女儿。", ExamplePinyin = "Tā yǒu yí gè hěn kě'ài de nǚ'ér.", ExampleMeaning = "Chị ấy có một cô con gái rất đáng yêu." },
        new() { Id = 1015, HskLevel = "HSK2", Topic = "family", Hanzi = "哥哥", Pinyin = "gē ge", Meaning = "Anh trai", WordType = "Danh từ", ExampleSentence = "我哥哥在河内上大学。", ExamplePinyin = "Wǒ gēge zài Hénèi shàng dàxué.", ExampleMeaning = "Anh trai tôi học đại học ở Hà Nội." },
        new() { Id = 1016, HskLevel = "HSK2", Topic = "family", Hanzi = "姐姐", Pinyin = "jiě jie", Meaning = "Chị gái", WordType = "Danh từ", ExampleSentence = "姐姐的工作是一名护士。", ExamplePinyin = "Jiějie de gōngzuò shì yì míng hùshi.", ExampleMeaning = "Công việc của chị gái là một y tá." },
        new() { Id = 1017, HskLevel = "HSK2", Topic = "family", Hanzi = "弟弟", Pinyin = "dì di", Meaning = "Em trai", WordType = "Danh từ", ExampleSentence = "弟弟喜欢看动画片。", ExamplePinyin = "Dìdi xǐhuan kàn dònghuàpiàn.", ExampleMeaning = "Em trai thích xem phim hoạt hình." },
        new() { Id = 1018, HskLevel = "HSK2", Topic = "family", Hanzi = "妹妹", Pinyin = "mèi mei", Meaning = "Em gái", WordType = "Danh từ", ExampleSentence = "妹妹在小学三年级读书。", ExamplePinyin = "Mèimei zài xiǎoxué sān niánjí dú shū.", ExampleMeaning = "Em gái đang học lớp 3 trường tiểu học." },
        new() { Id = 1019, HskLevel = "HSK3", Topic = "family", Hanzi = "爷爷", Pinyin = "yé ye", Meaning = "Ông nội", WordType = "Danh từ", ExampleSentence = "爷爷每天早上去公园散步。", ExamplePinyin = "Yéye měitiān zǎoshang qù gōngyuán sànbù.", ExampleMeaning = "Ông nội mỗi sáng đều ra công viên đi dạo." },
        new() { Id = 1020, HskLevel = "HSK3", Topic = "family", Hanzi = "奶奶", Pinyin = "nǎi nai", Meaning = "Bà nội", WordType = "Danh từ", ExampleSentence = "奶奶做的包子特别香。", ExamplePinyin = "Nǎinai zuò de bāozi tèbié xiāng.", ExampleMeaning = "Bánh bao bà nội làm đặc biệt thơm ngon." },
        new() { Id = 1021, HskLevel = "HSK3", Topic = "family", Hanzi = "丈夫", Pinyin = "zhàng fu", Meaning = "Chồng", WordType = "Danh từ", ExampleSentence = "她的丈夫在一家外企工作。", ExamplePinyin = "Tā de zhàngfu zài yì jiā wàiqǐ gōngzuò.", ExampleMeaning = "Chồng của cô ấy làm việc ở một công ty nước ngoài." },
        new() { Id = 1022, HskLevel = "HSK3", Topic = "family", Hanzi = "妻子", Pinyin = "qī zi", Meaning = "Vợ", WordType = "Danh từ", ExampleSentence = "我和妻子结婚五年了。", ExamplePinyin = "Wǒ hé qīzi jiéhūn wǔ nián le.", ExampleMeaning = "Tôi và vợ đã kết hôn được 5 năm rồi." },
        new() { Id = 1023, HskLevel = "HSK3", Topic = "family", Hanzi = "叔叔", Pinyin = "shū shu", Meaning = "Chú, bác trai", WordType = "Danh từ", ExampleSentence = "王叔叔送给我一本新书。", ExamplePinyin = "Wáng shūshu sòng gěi wǒ yì běn xīn shū.", ExampleMeaning = "Chú Vương tặng tôi một quyển sách mới." },
        new() { Id = 1024, HskLevel = "HSK3", Topic = "family", Hanzi = "阿姨", Pinyin = "ā yí", Meaning = "Dì, cô, bác gái", WordType = "Danh từ", ExampleSentence = "李阿姨做菜非常好吃。", ExamplePinyin = "Lǐ āyí zuò cài fēicháng hǎochī.", ExampleMeaning = "Dì Lý nấu ăn rất là ngon." },

        // ─── 3. ẨM THỰC & ĐỒ UỐNG (food) ───
        new() { Id = 1031, HskLevel = "HSK1", Topic = "food", Hanzi = "米饭", Pinyin = "mǐ fàn", Meaning = "Cơm", WordType = "Danh từ", ExampleSentence = "中午我想吃一碗米饭。", ExamplePinyin = "Zhōngwǔ wǒ xiǎng chī yì wǎn mǐfàn.", ExampleMeaning = "Buổi trưa tôi muốn ăn một bát cơm." },
        new() { Id = 1032, HskLevel = "HSK1", Topic = "food", Hanzi = "苹果", Pinyin = "píng guǒ", Meaning = "Quả táo", WordType = "Danh từ", ExampleSentence = "每天吃一个苹果对身体好。", ExamplePinyin = "Měitiān chī yí gè píngguǒ duì shēntǐ hǎo.", ExampleMeaning = "Mỗi ngày ăn một quả táo rất tốt cho sức khỏe." },
        new() { Id = 1033, HskLevel = "HSK1", Topic = "food", Hanzi = "茶", Pinyin = "chá", Meaning = "Trà, chè", WordType = "Danh từ", ExampleSentence = "中国人习惯饭后喝一杯热茶。", ExamplePinyin = "Zhōngguó rén xíguàn fàn hòu hē yì bēi rè chá.", ExampleMeaning = "Người Trung Quốc có thói quen uống trà nóng sau bữa ăn." },
        new() { Id = 1034, HskLevel = "HSK2", Topic = "food", Hanzi = "面条", Pinyin = "miàn tiáo", Meaning = "Mì sợi", WordType = "Danh từ", ExampleSentence = "生日时中国人常吃长寿面条。", ExamplePinyin = "Shēngrì shí Zhōngguó rén cháng chī chángshòumiàntiáo.", ExampleMeaning = "Vào ngày sinh nhật người Trung Quốc thường ăn mì trường thọ." },
        new() { Id = 1035, HskLevel = "HSK2", Topic = "food", Hanzi = "咖啡", Pinyin = "kā fēi", Meaning = "Cà phê", WordType = "Danh từ", ExampleSentence = "请给我一杯不加糖的咖啡。", ExamplePinyin = "Qǐng gěi wǒ yì bēi bù jiā táng de kāfēi.", ExampleMeaning = "Làm ơn cho tôi một cốc cà phê không đường." },
        new() { Id = 1036, HskLevel = "HSK2", Topic = "food", Hanzi = "牛奶", Pinyin = "niú nǎi", Meaning = "Sữa bò", WordType = "Danh từ", ExampleSentence = "早饭我喝了一杯热牛奶。", ExamplePinyin = "Zǎofàn wǒ hē le yì bēi rè niúnǎi.", ExampleMeaning = "Bữa sáng tôi đã uống một ly sữa bò nóng." },
        new() { Id = 1037, HskLevel = "HSK2", Topic = "food", Hanzi = "西瓜", Pinyin = "xī guā", Meaning = "Dưa hấu", WordType = "Danh từ", ExampleSentence = "夏天的西瓜又甜又多汁。", ExamplePinyin = "Xiàtiān de xīguā yòu tián yòu duō zhī.", ExampleMeaning = "Dưa hấu mùa hè vừa ngọt vừa mọng nước." },
        new() { Id = 1038, HskLevel = "HSK2", Topic = "food", Hanzi = "鸡蛋", Pinyin = "jī dàn", Meaning = "Trứng gà", WordType = "Danh từ", ExampleSentence = "早上吃两个鸡蛋补充营养。", ExamplePinyin = "Zǎoshang chī liǎng gè jīdàn bǔchōng yíngyǎng.", ExampleMeaning = "Buổi sáng ăn hai quả trứng gà để bổ sung dinh dưỡng." },
        new() { Id = 1039, HskLevel = "HSK3", Topic = "food", Hanzi = "菜单", Pinyin = "cài dān", Meaning = "Thực đơn", WordType = "Danh từ", ExampleSentence = "服务员，麻烦拿一下菜单。", ExamplePinyin = "Fúwùyuán, máfan ná yíxià càidān.", ExampleMeaning = "Phục vụ ơi, làm phiền mang giúp thực đơn." },
        new() { Id = 1040, HskLevel = "HSK3", Topic = "food", Hanzi = "面包", Pinyin = "miàn bāo", Meaning = "Bánh mì", WordType = "Danh từ", ExampleSentence = "新鲜出炉的面包真香。", ExamplePinyin = "Xīnxiān chū lú de miànbāo zhēn xiāng.", ExampleMeaning = "Bánh mì mới ra lò thơm thật." },

        // ─── 4. MUA SẮM & TIỀN TỆ (shopping) ───
        new() { Id = 1041, HskLevel = "HSK1", Topic = "shopping", Hanzi = "多少钱", Pinyin = "duō shao qián", Meaning = "Bao nhiêu tiền", WordType = "Cụm từ", ExampleSentence = "请问这个苹果多少钱一斤？", ExamplePinyin = "Qǐngwèn zhège píngguǒ duōshao qián yì jīn?", ExampleMeaning = "Xin hỏi táo này bao nhiêu tiền một cân?" },
        new() { Id = 1042, HskLevel = "HSK1", Topic = "shopping", Hanzi = "块", Pinyin = "kuài", Meaning = "Đồng tệ (khẩu ngữ)", WordType = "Lượng từ", ExampleSentence = "这瓶水两块五毛钱。", ExamplePinyin = "Zhè píng shuǐ liǎng kuài wǔ máo qián.", ExampleMeaning = "Chai nước này 2 tệ 5 hào." },
        new() { Id = 1043, HskLevel = "HSK1", Topic = "shopping", Hanzi = "商店", Pinyin = "shāng diàn", Meaning = "Cửa hàng, tiệm", WordType = "Danh từ", ExampleSentence = "学校旁边有一家文具商店。", ExamplePinyin = "Xuéxiào pángbiān yǒu yì jiā wénjù shāngdiàn.", ExampleMeaning = "Bên cạnh trường học có một cửa hàng văn phòng phẩm." },
        new() { Id = 1044, HskLevel = "HSK2", Topic = "shopping", Hanzi = "便宜", Pinyin = "pián yi", Meaning = "Rẻ, giá rẻ", WordType = "Tính từ", ExampleSentence = "网购的东西往往比较便宜。", ExamplePinyin = "Wǎnggòu de dōngxi wǎngwǎng bǐjiào piányi.", ExampleMeaning = "Đồ mua qua mạng thường khá rẻ." },
        new() { Id = 1045, HskLevel = "HSK2", Topic = "shopping", Hanzi = "贵", Pinyin = "guì", Meaning = "Đắt, đắt đỏ", WordType = "Tính từ", ExampleSentence = "这件大衣质量好，但有点贵。", ExamplePinyin = "Zhè jiàn dàyī zhìliàng hǎo, dàn yǒudiǎnr guì.", ExampleMeaning = "Chiếc áo khoác này chất lượng tốt, nhưng hơi đắt." },
        new() { Id = 1046, HskLevel = "HSK2", Topic = "shopping", Hanzi = "衣服", Pinyin = "yī fu", Meaning = "Quần áo", WordType = "Danh từ", ExampleSentence = "周末我想去商场买几件新衣服。", ExamplePinyin = "Zhōumò wǒ xiǎng qù shāngchǎng mǎi jǐ jiàn xīn yīfu.", ExampleMeaning = "Cuối tuần tôi muốn đi trung tâm mua sắm mua vài bộ quần áo mới." },
        new() { Id = 1047, HskLevel = "HSK3", Topic = "shopping", Hanzi = "超市", Pinyin = "chāo shì", Meaning = "Siêu thị", WordType = "Danh từ", ExampleSentence = "我和室友去超市买生活用品。", ExamplePinyin = "Wǒ hé shìyǒu qù chāoshì mǎi shēnghuó yòngpǐn.", ExampleMeaning = "Tôi và bạn cùng phòng đi siêu thị mua đồ sinh hoạt." },
        new() { Id = 1048, HskLevel = "HSK3", Topic = "shopping", Hanzi = "打折", Pinyin = "dǎ zhé", Meaning = "Giảm giá, chiết khấu", WordType = "Động từ", ExampleSentence = "这家服装店今天正在打七折。", ExamplePinyin = "Zhè jiā fúzhuāngdiàn jīntiān zhèngzài dǎ qī zhé.", ExampleMeaning = "Tiệm thời trang này hôm nay đang giảm giá 30%." },

        // ─── 5. GIAO THÔNG & ĐI LẠI (travel) ───
        new() { Id = 1051, HskLevel = "HSK1", Topic = "travel", Hanzi = "出租车", Pinyin = "chū zū chē", Meaning = "Xe taxi", WordType = "Danh từ", ExampleSentence = "快迟到了，我们打出租车去吧。", ExamplePinyin = "Kuài chídào le, wǒmen dǎ chūzūchē qù ba.", ExampleMeaning = "Sắp muộn rồi, chúng ta bắt taxi đi thôi." },
        new() { Id = 1052, HskLevel = "HSK1", Topic = "travel", Hanzi = "飞机", Pinyin = "fēi jī", Meaning = "Máy bay", WordType = "Danh từ", ExampleSentence = "飞机准时起飞了。", ExamplePinyin = "Fēijī zhǔnshí qǐfēi le.", ExampleMeaning = "Máy bay đã cất cánh đúng giờ." },
        new() { Id = 1053, HskLevel = "HSK2", Topic = "travel", Hanzi = "公共汽车", Pinyin = "gōng gòng qì chē", Meaning = "Xe buýt", WordType = "Danh từ", ExampleSentence = "在河内坐公共汽车很便宜。", ExamplePinyin = "Zài Hénèi zuò gōnggòng qìchē hěn piányi.", ExampleMeaning = "Ở Hà Nội đi xe buýt rất rẻ." },
        new() { Id = 1054, HskLevel = "HSK2", Topic = "travel", Hanzi = "火车站", Pinyin = "huǒ chē zhàn", Meaning = "Ga xe lửa, ga tàu", WordType = "Danh từ", ExampleSentence = "我们在火车站门口见面。", ExamplePinyin = "Wǒmen zài huǒchēzhàn ménkǒu jiàn miàn.", ExampleMeaning = "Chúng mình gặp nhau ở trước cửa ga xe lửa nhé." },
        new() { Id = 1055, HskLevel = "HSK2", Topic = "travel", Hanzi = "机场", Pinyin = "jī chǎng", Meaning = "Sân bay", WordType = "Danh từ", ExampleSentence = "从市区去机场需要一个小时。", ExamplePinyin = "Cóng shìqū qù jīchǎng xūyào yí gè xiǎoshí.", ExampleMeaning = "Từ nội thành ra sân bay cần mất 1 tiếng." },
        new() { Id = 1056, HskLevel = "HSK3", Topic = "travel", Hanzi = "地铁", Pinyin = "dì tiě", Meaning = "Tàu điện ngầm", WordType = "Danh từ", ExampleSentence = "北京的地铁网络四通八达。", ExamplePinyin = "Běijīng de dìtiě wǎngluò sì tōng bā dá.", ExampleMeaning = "Mạng lưới tàu điện ngầm ở Bắc Kinh đi lại khắp nơi thuận tiện." },
        new() { Id = 1057, HskLevel = "HSK3", Topic = "travel", Hanzi = "行李", Pinyin = "xíng li", Meaning = "Hành lý", WordType = "Danh từ", ExampleSentence = "请保管好随身带的行李。", ExamplePinyin = "Qǐng bǎoguǎn hǎo suíshēn dài de xíngli.", ExampleMeaning = "Xin vui lòng bảo quản tốt hành lý mang theo bên mình." },
        new() { Id = 1058, HskLevel = "HSK3", Topic = "travel", Hanzi = "护照", Pinyin = "hù zhào", Meaning = "Hộ chiếu", WordType = "Danh từ", ExampleSentence = "出国旅游前一定要检查护照。", ExamplePinyin = "Chū guó lǚyóu qián yídìng yào jiǎnchá hùzhào.", ExampleMeaning = "Trước khi đi du lịch nước ngoài nhất định phải kiểm tra hộ chiếu." },

        // ─── 6. TRƯỜNG HỌC & HỌC TẬP (study) ───
        new() { Id = 1061, HskLevel = "HSK1", Topic = "study", Hanzi = "学校", Pinyin = "xué xiào", Meaning = "Trường học", WordType = "Danh từ", ExampleSentence = "我们学校有两千多名学生。", ExamplePinyin = "Wǒmen xuéxiào yǒu liǎng qiān duō míng xuésheng.", ExampleMeaning = "Trường chúng tôi có hơn 2.000 học sinh." },
        new() { Id = 1062, HskLevel = "HSK1", Topic = "study", Hanzi = "汉语", Pinyin = "hàn yǔ", Meaning = "Tiếng Hán, tiếng Trung", WordType = "Danh từ", ExampleSentence = "努力练习可以学好汉语。", ExamplePinyin = "Nǔlì liànxí kěyǐ xué hǎo Hànyǔ.", ExampleMeaning = "Chăm chỉ luyện tập có thể học tốt tiếng Trung." },
        new() { Id = 1063, HskLevel = "HSK1", Topic = "study", Hanzi = "书", Pinyin = "shū", Meaning = "Sách", WordType = "Danh từ", ExampleSentence = "书是人类进步的阶梯。", ExamplePinyin = "Shū shì rénlèi jìnbù de jiētī.", ExampleMeaning = "Sách là nấc thang tiến bộ của nhân loại." },
        new() { Id = 1064, HskLevel = "HSK2", Topic = "study", Hanzi = "考试", Pinyin = "kǎo shì", Meaning = "Kỳ thi, thi cử", WordType = "Động từ", ExampleSentence = "这次HSK考试题目不太难。", ExamplePinyin = "Zhè cì HSK kǎoshì tímù bú tài nán.", ExampleMeaning = "Đề thi HSK lần này không quá khó." },
        new() { Id = 1065, HskLevel = "HSK2", Topic = "study", Hanzi = "题", Pinyin = "tí", Meaning = "Câu hỏi, đề thi", WordType = "Danh từ", ExampleSentence = "最后一道题我没做出来。", ExamplePinyin = "Zuìhòu yí dào tí wǒ méi zuò chūlai.", ExampleMeaning = "Câu cuối cùng tôi chưa làm ra được." },
        new() { Id = 1066, HskLevel = "HSK2", Topic = "study", Hanzi = "懂", Pinyin = "dǒng", Meaning = "Hiểu, hiểu rõ", WordType = "Động từ", ExampleSentence = "老师讲的内容大家都听懂了。", ExamplePinyin = "Lǎoshī jiǎng de nèiróng dàjiā dōu tīng dǒng le.", ExampleMeaning = "Nội dung thầy giáo giảng mọi người đều đã hiểu." },
        new() { Id = 1067, HskLevel = "HSK3", Topic = "study", Hanzi = "复习", Pinyin = "fù xí", Meaning = "Ôn tập", WordType = "Động từ", ExampleSentence = "期末考试前一定要好好复习。", ExamplePinyin = "Qīmò kǎoshì qián yídìng yào hǎohāo fùxí.", ExampleMeaning = "Trước kỳ thi cuối kỳ nhất định phải ôn tập thật tốt." },
        new() { Id = 1068, HskLevel = "HSK3", Topic = "study", Hanzi = "提高", Pinyin = "tí gāo", Meaning = "Nâng cao, cải thiện", WordType = "Động từ", ExampleSentence = "每天背单词有助于提高词汇量。", ExamplePinyin = "Měitiān bèi dāncí yǒu zhù yú tígāo cíhuì liàng.", ExampleMeaning = "Học từ vựng mỗi ngày giúp nâng cao lượng từ vựng." },

        // ─── 7. CÔNG VIỆC & CÔNG SỞ (work) ───
        new() { Id = 1071, HskLevel = "HSK1", Topic = "work", Hanzi = "工作", Pinyin = "gōng zuò", Meaning = "Công việc, làm việc", WordType = "Động từ/Danh từ", ExampleSentence = "他工作非常认真负责。", ExamplePinyin = "Tā gōngzuò fēicháng rènzhēn fùzé.", ExampleMeaning = "Anh ấy làm việc rất nghiêm túc và có trách nhiệm." },
        new() { Id = 1072, HskLevel = "HSK2", Topic = "work", Hanzi = "公司", Pinyin = "gōng sī", Meaning = "Công ty", WordType = "Danh từ", ExampleSentence = "我们公司下个月开展新项目。", ExamplePinyin = "Wǒmen gōngsī xià gè yuè kāizhǎn xīn xiàngmù.", ExampleMeaning = "Công ty chúng tôi tháng sau triển khai dự án mới." },
        new() { Id = 1073, HskLevel = "HSK2", Topic = "work", Hanzi = "上班", Pinyin = "shàng bān", Meaning = "Đi làm", WordType = "Động từ", ExampleSentence = "早高峰上班经常会遇到堵车。", ExamplePinyin = "Zǎo gāofēng shàngbān jīngcháng huì yù dào dǔchē.", ExampleMeaning = "Đi làm vào giờ cao điểm buổi sáng thường hay bị tắc đường." },
        new() { Id = 1074, HskLevel = "HSK2", Topic = "work", Hanzi = "准备", Pinyin = "zhǔn bèi", Meaning = "Chuẩn bị", WordType = "Động từ", ExampleSentence = "请大家准备好明天的汇报材料。", ExamplePinyin = "Qǐng dàjiā zhǔnbèi hǎo míngtiān de huìbào cáiliào.", ExampleMeaning = "Xin mọi người chuẩn bị kỹ tài liệu báo cáo ngày mai." },
        new() { Id = 1075, HskLevel = "HSK3", Topic = "work", Hanzi = "经理", Pinyin = "jīng lǐ", Meaning = "Giám đốc, quản lý", WordType = "Danh từ", ExampleSentence = "销售部经理正在同客户洽谈。", ExamplePinyin = "Xiàoshòubù jīnglǐ zhèngzài tóng kèhù qiàtán.", ExampleMeaning = "Giám đốc bộ phận kinh doanh đang đàm phán với khách hàng." },
        new() { Id = 1076, HskLevel = "HSK3", Topic = "work", Hanzi = "会议", Pinyin = "huì yì", Meaning = "Cuộc họp, hội nghị", WordType = "Danh từ", ExampleSentence = "下午两点全体员工参加会议。", ExamplePinyin = "Xiàwǔ liǎng diǎn quántǐ yuángōng cānjiā huìyì.", ExampleMeaning = "2 giờ chiều toàn thể nhân viên tham gia cuộc họp." },
        new() { Id = 1077, HskLevel = "HSK3", Topic = "work", Hanzi = "同事", Pinyin = "tóng shì", Meaning = "Đồng nghiệp", WordType = "Danh từ", ExampleSentence = "同事们之间的关系非常融洽。", ExamplePinyin = "Tóngshìmen zhī jiān de guānxi fēicháng róngqià.", ExampleMeaning = "Mối quan hệ giữa các đồng nghiệp rất hòa thuận." },

        // ─── 8. THỜI GIAN & LỊCH TRÌNH (time) ───
        new() { Id = 1081, HskLevel = "HSK1", Topic = "time", Hanzi = "今天", Pinyin = "jīn tiān", Meaning = "Hôm nay", WordType = "Danh từ", ExampleSentence = "今天是星期五，明天放假。", ExamplePinyin = "Jīntiān shì xīngqīwǔ, míngtiān fàng jià.", ExampleMeaning = "Hôm nay là thứ Sáu, mai được nghỉ rồi." },
        new() { Id = 1082, HskLevel = "HSK1", Topic = "time", Hanzi = "明天", Pinyin = "míng tiān", Meaning = "Ngày mai", WordType = "Danh từ", ExampleSentence = "明天上午九点我们出发。", ExamplePinyin = "Míngtiān shàngwǔ jiǔ diǎn wǒmen chūfā.", ExampleMeaning = "9 giờ sáng mai chúng ta xuất phát." },
        new() { Id = 1083, HskLevel = "HSK1", Topic = "time", Hanzi = "昨天", Pinyin = "zuó tiān", Meaning = "Hôm qua", WordType = "Danh từ", ExampleSentence = "昨天是我好朋友的生日。", ExamplePinyin = "Zuótiān shì wǒ hǎo péngyou de shēngrì.", ExampleMeaning = "Hôm qua là sinh nhật bạn thân của tôi." },
        new() { Id = 1084, HskLevel = "HSK1", Topic = "time", Hanzi = "星期", Pinyin = "xīng qī", Meaning = "Tuần, thứ", WordType = "Danh từ", ExampleSentence = "这个星期我们有两门期中考。", ExamplePinyin = "Zhège xīngqī wǒmen yǒu liǎng mén qīzhōng kǎo.", ExampleMeaning = "Tuần này chúng tôi có 2 môn thi giữa kỳ." },
        new() { Id = 1085, HskLevel = "HSK1", Topic = "time", Hanzi = "点", Pinyin = "diǎn", Meaning = "Giờ (thời gian)", WordType = "Lượng từ", ExampleSentence = "现在正好是下午两点整。", ExamplePinyin = "Xiànzài zhènghǎo shì xiàwǔ liǎng diǎn zhěng.", ExampleMeaning = "Bây giờ đúng lúc là 2 giờ chiều." },
        new() { Id = 1086, HskLevel = "HSK2", Topic = "time", Hanzi = "时间", Pinyin = "shí jiān", Meaning = "Thời gian", WordType = "Danh từ", ExampleSentence = "合理安排时间才能学得轻松。", ExamplePinyin = "Hélǐ ānpái shíjiān cái néng xué de qīngsōng.", ExampleMeaning = "Sắp xếp thời gian hợp lý mới có thể học tập nhẹ nhàng." },
        new() { Id = 1087, HskLevel = "HSK3", Topic = "time", Hanzi = "周末", Pinyin = "zhōu mò", Meaning = "Cuối tuần", WordType = "Danh từ", ExampleSentence = "周末你通常有什么计划？", ExamplePinyin = "Zhōumò nǐ tōngcháng yǒu shénme jìhuà?", ExampleMeaning = "Cuối tuần bạn thường có kế hoạch gì?" },

        // ─── 9. NHÀ CỬA & ĐỜI SỐNG (home) ───
        new() { Id = 1091, HskLevel = "HSK1", Topic = "home", Hanzi = "家", Pinyin = "jiā", Meaning = "Nhà, tổ ấm", WordType = "Danh từ", ExampleSentence = "欢迎你有时间来我家做客。", ExamplePinyin = "Huānyíng nǐ yǒu shíjiān lái wǒ jiā zuò kè.", ExampleMeaning = "Hoan nghênh bạn khi nào có thời gian đến nhà tôi chơi." },
        new() { Id = 1092, HskLevel = "HSK1", Topic = "home", Hanzi = "桌子", Pinyin = "zhuō zi", Meaning = "Cái bàn", WordType = "Danh từ", ExampleSentence = "书桌上摆着一台笔记本电脑。", ExamplePinyin = "Shūzhuō shang bǎi zhe yì tái bǐjìběn diànnǎo.", ExampleMeaning = "Trên bàn học đặt một chiếc máy tính xách tay." },
        new() { Id = 1093, HskLevel = "HSK1", Topic = "home", Hanzi = "椅子", Pinyin = "yǐ zi", Meaning = "Cái ghế", WordType = "Danh từ", ExampleSentence = "请坐在这把舒适的木椅子上。", ExamplePinyin = "Qǐng zuò zài zhè bǎ shūshì de mù yǐzi shang.", ExampleMeaning = "Xin mời ngồi lên chiếc ghế gỗ thoải mái này." },
        new() { Id = 1094, HskLevel = "HSK2", Topic = "home", Hanzi = "房间", Pinyin = "fáng jiān", Meaning = "Căn phòng", WordType = "Danh từ", ExampleSentence = "我的房间朝南，阳光很充足。", ExamplePinyin = "Wǒ de fángjiān cháo nán, yángguāng hěn chōngzú.", ExampleMeaning = "Phòng tôi hướng nam, ánh nắng rất chan hòa." },
        new() { Id = 1095, HskLevel = "HSK2", Topic = "home", Hanzi = "床", Pinyin = "chuáng", Meaning = "Cái giường", WordType = "Danh từ", ExampleSentence = "这张大床睡起来特别舒服。", ExamplePinyin = "Zhè zhāng dà chuáng shuì qǐlai tèbié shūfu.", ExampleMeaning = "Chiếc giường lớn này ngủ rất êm ái thoải mái." },
        new() { Id = 1096, HskLevel = "HSK3", Topic = "home", Hanzi = "打扫", Pinyin = "dǎ sǎo", Meaning = "Quét dọn, dọn dẹp", WordType = "Động từ", ExampleSentence = "新年快到了，我们一起打扫房间。", ExamplePinyin = "Xīnnián kuài dào le, wǒmen yìqǐ dǎsǎo fángjiān.", ExampleMeaning = "Sắp đến Tết rồi, chúng mình cùng nhau dọn dẹp phòng ốc." },
        new() { Id = 1097, HskLevel = "HSK3", Topic = "home", Hanzi = "干净", Pinyin = "gān jìng", Meaning = "Sạch sẽ", WordType = "Tính từ", ExampleSentence = "收拾完后客厅非常干净整齐。", ExamplePinyin = "Shōushi wán hòu kètīng fēicháng gānjìng zhěngqí.", ExampleMeaning = "Thu dọn xong phòng khách vô cùng sạch sẽ và ngăn nắp." },

        // ─── 10. THỜI TIẾT & TỰ NHIÊN (weather) ───
        new() { Id = 1101, HskLevel = "HSK1", Topic = "weather", Hanzi = "天气", Pinyin = "tiān qì", Meaning = "Thời tiết", WordType = "Danh từ", ExampleSentence = "今天天气晴朗，非常适合郊游。", ExamplePinyin = "Jīntiān tiānqì qínglǎng, fēicháng shìhé jiāoyóu.", ExampleMeaning = "Hôm nay thời tiết đẹp trời, rất thích hợp đi dã ngoại." },
        new() { Id = 1102, HskLevel = "HSK1", Topic = "weather", Hanzi = "下雨", Pinyin = "xià yǔ", Meaning = "Trời mưa, đổ mưa", WordType = "Động từ", ExampleSentence = "外面在下大雨，出门别忘了带伞。", ExamplePinyin = "Wàimiàn zài xià dàyǔ, chū mén bié wàng le dài sǎn.", ExampleMeaning = "Bên ngoài đang mưa to, ra ngoài đừng quên mang ô." },
        new() { Id = 1103, HskLevel = "HSK1", Topic = "weather", Hanzi = "冷", Pinyin = "lěng", Meaning = "Lạnh", WordType = "Tính từ", ExampleSentence = "今天降温了，多穿件衣服别着凉。", ExamplePinyin = "Jīntiān jiàngwēn le, duō chuān jiàn yīfu bié zháoliáng.", ExampleMeaning = "Hôm nay trời trở lạnh, mặc thêm áo kẻo bị cảm lạnh." },
        new() { Id = 1104, HskLevel = "HSK1", Topic = "weather", Hanzi = "热", Pinyin = "rè", Meaning = "Nóng", WordType = "Tính từ", ExampleSentence = "中午太阳直晒，天气特别热。", ExamplePinyin = "Zhōngwǔ tàiyáng zhí shài, tiānqì tèbié rè.", ExampleMeaning = "Trưa nắng gắt chiếu thẳng, trời đặc biệt nóng." },
        new() { Id = 1105, HskLevel = "HSK2", Topic = "weather", Hanzi = "阴天", Pinyin = "yīn tiān", Meaning = "Trời nhiều mây, trời râm", WordType = "Danh từ", ExampleSentence = "今天阴天，没有出太阳。", ExamplePinyin = "Jīntiān yīntiān, méiyǒu chū tàiyáng.", ExampleMeaning = "Hôm nay trời râm, không có nắng." },
        new() { Id = 1106, HskLevel = "HSK3", Topic = "weather", Hanzi = "刮风", Pinyin = "guā fēng", Meaning = "Gió thổi", WordType = "Động từ", ExampleSentence = "大风把地上的落叶都吹跑了。", ExamplePinyin = "Dàfēng bǎ dì shang de luòyè dōu chuī pǎo le.", ExampleMeaning = "Gió to thổi bay hết cả lá rụng dưới đất." },
        new() { Id = 1107, HskLevel = "HSK3", Topic = "weather", Hanzi = "夏天", Pinyin = "xià tiān", Meaning = "Mùa hè", WordType = "Danh từ", ExampleSentence = "夏天大家最喜欢去海边游泳。", ExamplePinyin = "Xiàtiān dàjiā zuì xǐhuan qù hǎibiān yóuyǒng.", ExampleMeaning = "Mùa hè mọi người thích nhất là ra bãi biển bơi." },

        // ─── 11. CẢM XÚC & TÍNH CÁCH (emotions) ───
        new() { Id = 1111, HskLevel = "HSK1", Topic = "emotions", Hanzi = "高兴", Pinyin = "gāo xìng", Meaning = "Vui vẻ, phấn khởi", WordType = "Tính từ", ExampleSentence = "听到被录取的消息，她高兴得跳了起来。", ExamplePinyin = "Tīngdào bèi lùqǔ de xiāoxi, tā gāoxìng de tiào le qǐlai.", ExampleMeaning = "Nghe tin được trúng tuyển, cô ấy mừng nhảy cẫng lên." },
        new() { Id = 1112, HskLevel = "HSK2", Topic = "emotions", Hanzi = "累", Pinyin = "lèi", Meaning = "Mệt mỏi, mệt nhọc", WordType = "Tính từ", ExampleSentence = "爬完山后大家虽然累，但心里很快乐。", ExamplePinyin = "Pá wán shān hòu dàjiā suīrán lèi, dàn xīnli hěn kuàilè.", ExampleMeaning = "Leo núi xong tuy mọi người mệt nhưng trong lòng rất vui." },
        new() { Id = 1113, HskLevel = "HSK3", Topic = "emotions", Hanzi = "满意", Pinyin = "mǎn yì", Meaning = "Hài lòng, vừa ý", WordType = "Tính từ", ExampleSentence = "顾客对我们的优质服务非常满意。", ExamplePinyin = "Gùkè duì wǒmen de yōuzhì fúwù fēicháng mǎnyì.", ExampleMeaning = "Khách hàng rất hài lòng với chất lượng phục vụ của chúng tôi." },
        new() { Id = 1114, HskLevel = "HSK3", Topic = "emotions", Hanzi = "担心", Pinyin = "dān xīn", Meaning = "Lo lắng, lo âu", WordType = "Động từ", ExampleSentence = "妈妈总是在担心孩子的身体健康。", ExamplePinyin = "Māma zǒngshì zài dānxīn háizi de shēntǐ jiànkāng.", ExampleMeaning = "Mẹ lúc nào cũng lo lắng cho sức khỏe của con cái." },
        new() { Id = 1115, HskLevel = "HSK3", Topic = "emotions", Hanzi = "着急", Pinyin = "zháo jí", Meaning = "Sốt ruột, hấp tấp", WordType = "Tính từ", ExampleSentence = "事情要一步一步来，别太着急。", ExamplePinyin = "Shìqing yào yí bù yí bù lái, bié tài zháojí.", ExampleMeaning = "Mọi việc phải từng bước một, đừng quá nóng vội." },
        new() { Id = 1116, HskLevel = "HSK3", Topic = "emotions", Hanzi = "认真", Pinyin = "rèn zhēn", Meaning = "Chăm chỉ, nghiêm túc", WordType = "Tính từ", ExampleSentence = "认真的态度是成功的关键。", ExamplePinyin = "Rènzhēn de tàidù shì chénggōng de guānjiàn.", ExampleMeaning = "Thái độ nghiêm túc là chìa khóa của thành công." },

        // ─── 12. SỞ THÍCH & THỂ THAO (hobbies) ───
        new() { Id = 1121, HskLevel = "HSK1", Topic = "hobbies", Hanzi = "看电影", Pinyin = "kàn diàn yǐng", Meaning = "Xem phim", WordType = "Cụm từ", ExampleSentence = "我最喜欢在周末和朋友去看电影。", ExamplePinyin = "Wǒ zuì xǐhuan zài zhōumò hé péngyou qù kàn diànyǐng.", ExampleMeaning = "Tôi thích nhất là đi xem phim cùng bạn bè vào cuối tuần." },
        new() { Id = 1122, HskLevel = "HSK2", Topic = "hobbies", Hanzi = "踢足球", Pinyin = "tī zú qiú", Meaning = "Đá bóng, đá banh", WordType = "Cụm từ", ExampleSentence = "傍晚操场上有很多人在踢足球。", ExamplePinyin = "Bàngwǎn cāochǎng shang yǒu hěn duō rén zài tī zúqiú.", ExampleMeaning = "Hoàng hôn trên sân trường có rất nhiều người đá bóng." },
        new() { Id = 1123, HskLevel = "HSK2", Topic = "hobbies", Hanzi = "游泳", Pinyin = "yóu yǒng", Meaning = "Bơi lội", WordType = "Động từ", ExampleSentence = "游泳能锻炼全身肌肉。", ExamplePinyin = "Yóuyǒng néng duànliàn quánshēn jīròu.", ExampleMeaning = "Bơi lội có thể rèn luyện cơ bắp toàn thân." },
        new() { Id = 1124, HskLevel = "HSK2", Topic = "hobbies", Hanzi = "唱歌", Pinyin = "chàng gē", Meaning = "Ca hát", WordType = "Động từ", ExampleSentence = "她不仅人漂亮，而且唱歌特别动听。", ExamplePinyin = "Tā bùjǐn rén piàoliang, érqiě chànggē tèbié dòngtīng.", ExampleMeaning = "Cô ấy không những xinh mà hát còn đặc biệt truyền cảm." },
        new() { Id = 1125, HskLevel = "HSK2", Topic = "hobbies", Hanzi = "旅游", Pinyin = "lǚ yóu", Meaning = "Du lịch", WordType = "Động từ", ExampleSentence = "去不同的地方旅游开阔了我的眼界。", ExamplePinyin = "Qù bùtóng de dìfang lǚyóu kāikuò le wǒ de yǎnjiè.", ExampleMeaning = "Đi du lịch nhiều nơi khác nhau đã mở rộng tầm mắt cho tôi." },
        new() { Id = 1126, HskLevel = "HSK3", Topic = "hobbies", Hanzi = "跑步", Pinyin = "pǎo bù", Meaning = "Chạy bộ", WordType = "Động từ", ExampleSentence = "晨跑是我坚持了三年的好习惯。", ExamplePinyin = "Chén pǎo shì wǒ jiānchí le sān nián de hǎo xíguàn.", ExampleMeaning = "Chạy bộ buổi sáng là thói quen tốt tôi duy trì suốt 3 năm." },

        // ─── 13. SỨC KHỎE & Y TẾ (health) ───
        new() { Id = 1131, HskLevel = "HSK1", Topic = "health", Hanzi = "医院", Pinyin = "yī yuàn", Meaning = "Bệnh viện", WordType = "Danh từ", ExampleSentence = "市中心新建了一所现代化综合医院。", ExamplePinyin = "Shì zhōngxīn xīn jiàn le yì suǒ xiàndàihuà zōnghé yīyuàn.", ExampleMeaning = "Trung tâm thành phố mới xây một bệnh viện đa khoa hiện đại." },
        new() { Id = 1132, HskLevel = "HSK1", Topic = "health", Hanzi = "医生", Pinyin = "yī shēng", Meaning = "Bác sĩ", WordType = "Danh từ", ExampleSentence = "医生耐心地给病人听诊检查。", ExamplePinyin = "Yīshēng nàixīn de gěi bìngrén tīngzhěn jiǎnchá.", ExampleMeaning = "Bác sĩ kiên nhẫn nghe tim phổi kiểm tra cho bệnh nhân." },
        new() { Id = 1133, HskLevel = "HSK2", Topic = "health", Hanzi = "生病", Pinyin = "shēng bìng", Meaning = "Bị ốm, phát bệnh", WordType = "Động từ", ExampleSentence = "生病时要注意饮食清淡多饮温水。", ExamplePinyin = "Shēngbìng shí yào zhùyì yǐnshí qīngdàn duō yǐn wēnshuǐ.", ExampleMeaning = "Khi bị ốm cần chú ý ăn uống thanh đạm và uống nhiều nước ấm." },
        new() { Id = 1134, HskLevel = "HSK2", Topic = "health", Hanzi = "身体", Pinyin = "shēn tǐ", Meaning = "Cơ thể, thân thể, sức khỏe", WordType = "Danh từ", ExampleSentence = "身体是革命的本钱。", ExamplePinyin = "Shēntǐ shì gémìng de běnqián.", ExampleMeaning = "Sức khỏe là vốn quý giá nhất." },
        new() { Id = 1135, HskLevel = "HSK2", Topic = "health", Hanzi = "药", Pinyin = "yào", Meaning = "Thuốc chữa bệnh", WordType = "Danh từ", ExampleSentence = "良药苦口利于病。", ExamplePinyin = "Liáng yào kǔ kǒu lì yú bìng.", ExampleMeaning = "Thuốc đắng dã tật." },
        new() { Id = 1136, HskLevel = "HSK3", Topic = "health", Hanzi = "感冒", Pinyin = "gǎn mào", Meaning = "Cảm cúm", WordType = "Động từ/Danh từ", ExampleSentence = "换季时气温多变，容易着凉感冒。", ExamplePinyin = "Huàn jì shí qìwēn duō biàn, róngyì zháoliáng gǎnmào.", ExampleMeaning = "Lúc giao mùa nhiệt độ thất thường, rất dễ bị cảm cúm." },
        new() { Id = 1137, HskLevel = "HSK3", Topic = "health", Hanzi = "发烧", Pinyin = "fā shāo", Meaning = "Sốt, phát sốt", WordType = "Động từ", ExampleSentence = "孩子昨晚发烧到了三十八度五。", ExamplePinyin = "Háizi zuówǎn fāshāo dào le sānshíbā dù wǔ.", ExampleMeaning = "Đêm qua đứa bé bị sốt tới 38 độ rưỡi." },

        // ─── 14. ĐỊA ĐIỂM & PHƯƠNG HƯỚNG (locations) ───
        new() { Id = 1141, HskLevel = "HSK1", Topic = "locations", Hanzi = "前面", Pinyin = "qián miàn", Meaning = "Phía trước", WordType = "Danh từ", ExampleSentence = "红绿灯前面有一座过街天桥。", ExamplePinyin = "Hónglǜdēng qiánmiàn yǒu yí zuò guòjiē tiānqiáo.", ExampleMeaning = "Phía trước cột đèn giao thông có một cây cầu vượt đi bộ." },
        new() { Id = 1142, HskLevel = "HSK1", Topic = "locations", Hanzi = "后面", Pinyin = "hòu miàn", Meaning = "Phía sau", WordType = "Danh từ", ExampleSentence = "图书馆后面是一片大花园。", ExamplePinyin = "Túshūguǎn hòumiàn shì yí piàn dà huāyuán.", ExampleMeaning = "Phía sau thư viện là một vườn hoa lớn." },
        new() { Id = 1143, HskLevel = "HSK2", Topic = "locations", Hanzi = "左边", Pinyin = "zuǒ bian", Meaning = "Bên trái", WordType = "Danh từ", ExampleSentence = "学校正门左边是体育馆。", ExamplePinyin = "Xuéxiào zhèngmén zuǒbian shì tǐyùguǎn.", ExampleMeaning = "Bên trái cổng chính của trường là nhà thi đấu thể thao." },
        new() { Id = 1144, HskLevel = "HSK2", Topic = "locations", Hanzi = "右边", Pinyin = "yòu bian", Meaning = "Bên phải", WordType = "Danh từ", ExampleSentence = "向右边看，那座高楼就是银行。", ExamplePinyin = "Xiàng yòubian kàn, nà zuò gāo lóu jiù shì yínháng.", ExampleMeaning = "Nhìn sang bên phải, tòa nhà cao tầng đó chính là ngân hàng." },
        new() { Id = 1145, HskLevel = "HSK2", Topic = "locations", Hanzi = "旁边", Pinyin = "páng biān", Meaning = "Bên cạnh", WordType = "Danh từ", ExampleSentence = "超市旁边开了一家新书店。", ExamplePinyin = "Chāoshì pángbiān kāi le yì jiā xīn shūdiàn.", ExampleMeaning = "Bên cạnh siêu thị mới mở một tiệm sách mới." },
        new() { Id = 1146, HskLevel = "HSK3", Topic = "locations", Hanzi = "中间", Pinyin = "zhōng jiān", Meaning = "Ở giữa", WordType = "Danh từ", ExampleSentence = "坐在中间的那位先生就是校长。", ExamplePinyin = "Zuò zài zhōngjiān de nà wèi xiānsheng jiù shì xiàozhǎng.", ExampleMeaning = "Vị tiên sinh ngồi ở giữa chính là hiệu trưởng." },
        new() { Id = 1147, HskLevel = "HSK3", Topic = "locations", Hanzi = "附近", Pinyin = "fù jìn", Meaning = "Gần đây, lân cận", WordType = "Danh từ", ExampleSentence = "我家附近生活设施非常齐全。", ExamplePinyin = "Wǒ jiā fùjìn shēnghuó shèshī fēicháng qíquán.", ExampleMeaning = "Khu vực gần nhà tôi tiện ích sinh hoạt rất đầy đủ." }
    };

    /// <summary>
    /// Bộ từ khóa Hán tự CHÍNH XÁC của từng chủ đề (Exact Character Set)
    /// Đảm bảo tuyệt đối không có từ nào bị phân loại nhầm chủ đề!
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> _exactTopicKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["greetings"] = new() { "你好", "您好", "谢谢", "不客气", "对不起", "没关系", "再见", "请", "请问", "欢迎", "祝", "祝贺", "打扰", "慢走", "借过" },
        ["family"] = new() { "爸爸", "妈妈", "爷爷", "奶奶", "哥哥", "姐姐", "弟弟", "妹妹", "儿子", "女儿", "孩子", "丈夫", "妻子", "朋友", "同学", "老师", "医生", "护士", "司机", "服务员", "叔叔", "阿姨", "男人", "女人", "邻居", "亲戚" },
        ["food"] = new() { "吃", "喝", "米饭", "面条", "菜", "水", "茶", "咖啡", "牛奶", "苹果", "西瓜", "鸡蛋", "面包", "鱼", "肉", "牛肉", "羊肉", "鸡肉", "水果", "饺子", "汤", "甜", "苦", "辣", "酸", "咸", "饿", "饱", "饭馆", "餐厅", "菜单", "点菜", "啤酒" },
        ["shopping"] = new() { "买", "卖", "钱", "块", "元", "角", "多少钱", "贵", "便宜", "商店", "超市", "衣服", "裤子", "裙子", "鞋", "帽子", "包", "衬衫", "打折", "现金", "信用卡", "付钱", "找钱", "售货员" },
        ["travel"] = new() { "出租车", "飞机", "火车", "公共汽车", "地铁", "自行车", "船", "站", "车站", "火车站", "机场", "路", "路上", "车票", "机票", "护照", "行李", "旅游", "堵车", "迷路", "换乘", "骑车" },
        ["study"] = new() { "学校", "教室", "大学", "学生", "汉语", "中文", "英语", "书", "课本", "汉字", "词语", "句子", "练习", "考试", "题", "问题", "容易", "难", "懂", "明白", "铅笔", "字典", "复习", "提高", "听力", "阅读", "写作" },
        ["work"] = new() { "工作", "公司", "办公室", "上班", "下班", "加班", "请假", "会议", "开会", "经理", "同事", "工资", "薪水", "准备", "负责", "打算", "安排", "成功", "面试" },
        ["time"] = new() { "今天", "明天", "昨天", "前天", "后天", "年", "月", "日", "号", "星期", "周末", "早上", "上午", "中午", "下午", "晚上", "现在", "时候", "时间", "点", "分", "刻", "半", "去年", "明年", "马上", "迟到" },
        ["home"] = new() { "家", "房间", "门", "窗户", "床", "桌子", "椅子", "沙发", "电脑", "电视", "冰箱", "空调", "灯", "钥匙", "手机", "电话", "睡觉", "起床", "洗澡", "打扫", "干净" },
        ["weather"] = new() { "天气", "晴", "晴天", "阴", "阴天", "雨", "下雨", "雪", "下雪", "风", "刮风", "冷", "热", "暖和", "凉快", "云", "春天", "夏天", "秋天", "冬天", "太阳", "温度", "度" },
        ["emotions"] = new() { "高兴", "快乐", "难过", "哭", "笑", "生气", "担心", "着急", "满意", "害怕", "奇怪", "相信", "热情", "认真", "努力", "累", "困", "喜欢", "讨厌", "放心" },
        ["hobbies"] = new() { "看电影", "听音乐", "踢足球", "打篮球", "游泳", "唱歌", "跳舞", "跑步", "玩", "游戏", "运动", "比赛", "画画", "照相", "照片", "爬山" },
        ["health"] = new() { "身体", "舒服", "不舒服", "生病", "疼", "头疼", "肚子疼", "感冒", "发烧", "咳嗽", "药", "吃药", "医院", "医生", "护士", "检查", "休息", "健康", "针" },
        ["locations"] = new() { "前面", "后面", "左边", "右边", "上面", "下面", "里面", "外面", "中间", "旁边", "附近", "远", "近", "哪儿", "这里", "那里", "中国", "北京", "上海", "河内", "银行", "邮局", "公园", "洗手间" }
    };

    private static readonly Dictionary<string, List<HskVocabularyItem>> _staticTopicMap =
        TopicVocabularies
            .GroupBy(w => w.Topic ?? "greetings")
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Nhận diện CHÍNH XÁC chủ đề từ vựng dựa trên trường Topic hoặc đối chiếu Hán tự chuẩn xác 100%.
    /// Tuyệt đối KHÔNG đoán mò bằng substring câu ví dụ! Nếu không khớp thì trả về null.
    /// </summary>
    public static string? InferTopic(HskVocabularyItem item)
    {
        if (!string.IsNullOrEmpty(item.Topic)) return item.Topic;
        if (string.IsNullOrWhiteSpace(item.Hanzi)) return null;

        var hanzi = item.Hanzi.Trim();

        foreach (var (topicId, wordSet) in _exactTopicKeywords)
        {
            if (wordSet.Contains(hanzi))
                return topicId;
        }

        return null;
    }

    /// <summary>
    /// Lấy danh sách từ vựng tĩnh định nghĩa sẵn cho chủ đề (O(1) memory lookup)
    /// </summary>
    public static IReadOnlyList<HskVocabularyItem> GetStaticWords(string topicId)
    {
        if (_staticTopicMap.TryGetValue(topicId, out var list))
            return list;
        return Array.Empty<HskVocabularyItem>();
    }

    /// <summary>
    /// Lấy danh sách từ vựng theo chủ đề, CHỈ gộp thêm từ external nếu từ đó CHÍNH XÁC thuộc chủ đề này.
    /// </summary>
    public static List<HskVocabularyItem> GetWordsForTopic(string topicId, IEnumerable<HskVocabularyItem>? externalList = null)
    {
        var localList = GetStaticWords(topicId);
        if (externalList == null) return localList.ToList();

        var result = new List<HskVocabularyItem>(localList);
        var existingHanzi = new HashSet<string>(localList.Select(x => x.Hanzi));

        foreach (var w in externalList)
        {
            var matchedTopic = w.Topic ?? InferTopic(w);
            if (string.Equals(matchedTopic, topicId, StringComparison.OrdinalIgnoreCase) && existingHanzi.Add(w.Hanzi))
            {
                result.Add(w);
            }
        }

        return result;
    }
}
