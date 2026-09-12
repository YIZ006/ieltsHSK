using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence;

public static class GrammarSeedData
{
    public static async Task SeedGrammarStructuresAsync(AppDbContext dbContext)
    {
        if (!await dbContext.GrammarStructures.AnyAsync())
        {
            dbContext.GrammarStructures.AddRange(GetAllStructures());
            await dbContext.SaveChangesAsync();
        }
        else
        {
            var existingCodes = await dbContext.GrammarStructures.Select(g => g.StructureCode).ToListAsync();
            var missing = GetAllStructures().Where(g => !existingCodes.Contains(g.StructureCode)).ToList();
            if (missing.Any())
            {
                dbContext.GrammarStructures.AddRange(missing);
                await dbContext.SaveChangesAsync();
            }
        }
    }

    public static List<GrammarStructure> GetAllStructures()
    {
        return new List<GrammarStructure>
        {
            new GrammarStructure
            {
                StructureCode = "W_INV_01",
                BandLevel = "7.5 - 8.5",
                Category = "Writing Task 2",
                GrammarTopic = "Đảo ngữ (Inversion)",
                Formula = "Not only + Aux + S + V, but S + (also) + V",
                UsageFunction = "Nhấn mạnh 2 tác động song hành, tạo ấn tượng học thuật mạnh ở mở đoạn hoặc câu chủ đề",
                BasicExample = "Computers help students study and they make work easier.",
                AdvancedExample = "Not only does technological adoption facilitate self-directed learning, but it also enhances workforce productivity.",
                VietnameseMeaning = "Không chỉ việc áp dụng công nghệ tạo điều kiện cho việc tự học, mà nó còn nâng cao năng suất của lực lượng lao động.",
                KeyCollocations = "technological adoption, facilitate self-directed learning, workforce productivity",
                CommonMistakes = "Quên đảo trợ động từ lên trước chủ ngữ sau 'Not only' (ví dụ viết sai: Not only computers help...)",
                PracticeExercise = "Rewrite: Tourism creates jobs and it also introduces local culture.",
                Tags = "inversion, emphasis, task2, academic",
                DisplayOrder = 1
            },
            new GrammarStructure
            {
                StructureCode = "W_NOM_01",
                BandLevel = "7.0 - 8.0",
                Category = "Writing Task 1 & 2",
                GrammarTopic = "Danh từ hóa (Nominalisation)",
                Formula = "The [Noun phrase] + led to / resulted in + a [Adj] [Noun]",
                UsageFunction = "Biến đổi câu văn nói chứa động từ thành văn phong học thuật khách quan, trang trọng",
                BasicExample = "People used more renewable energy so emissions decreased rapidly.",
                AdvancedExample = "The widespread adoption of renewable energy resulted in a substantial reduction in carbon emissions.",
                VietnameseMeaning = "Việc áp dụng rộng rãi năng lượng tái tạo đã dẫn đến sự sụt giảm đáng kể lượng phát thải carbon.",
                KeyCollocations = "widespread adoption, substantial reduction, carbon emissions",
                CommonMistakes = "Dùng sai giới từ đi kèm với danh từ (ví dụ: reduction of thay vì reduction in)",
                PracticeExercise = "Rewrite: Cars increased rapidly so air became heavily polluted.",
                Tags = "nominalisation, academic_style, task1, task2",
                DisplayOrder = 2
            },
            new GrammarStructure
            {
                StructureCode = "W_CLEFT_01",
                BandLevel = "7.5 - 8.5",
                Category = "Writing Task 2",
                GrammarTopic = "Câu chẻ (Cleft Sentence)",
                Formula = "It is/was + [Thành phần nhấn mạnh] + that/who + [Mệnh đề]",
                UsageFunction = "Nhấn mạnh chính xác chủ thể chịu trách nhiệm hoặc giải pháp cốt lõi cho một vấn đề",
                BasicExample = "The government should solve this problem, not citizens.",
                AdvancedExample = "It is the municipal authorities, rather than individuals, that must take decisive action against urban pollution.",
                VietnameseMeaning = "Chính các cơ quan chính quyền đô thị, chứ không phải các cá nhân, mới là bên phải hành động quyết liệt để chống lại ô nhiễm.",
                KeyCollocations = "municipal authorities, take decisive action, urban pollution",
                CommonMistakes = "Dùng nhầm 'which' thay vì 'that' khi thành phần nhấn mạnh là danh từ chỉ vật",
                PracticeExercise = "Rewrite: Early education shapes a child's future, not higher education.",
                Tags = "cleft_sentence, emphasis, solutions, task2",
                DisplayOrder = 3
            },
            new GrammarStructure
            {
                StructureCode = "W_PART_01",
                BandLevel = "7.0 - 8.0",
                Category = "Writing Task 1",
                GrammarTopic = "Mệnh đề phân từ (Participle Clause)",
                Formula = "[Main Clause], thereby + V-ing / leading to + [Noun phrase]",
                UsageFunction = "Diễn tả chuỗi biến động kết quả liên hoàn trong bài mô tả biểu đồ Task 1",
                BasicExample = "The car sales rose to 50,000 and this made it the most popular product.",
                AdvancedExample = "Car sales surged to 50,000 units in 2020, thereby overtaking motorbikes as the leading vehicle category.",
                VietnameseMeaning = "Doanh số ô tô tăng vọt lên 50.000 chiếc vào năm 2020, qua đó vượt qua xe máy để trở thành nhóm phương tiện dẫn đầu.",
                KeyCollocations = "surge to, overtake, leading vehicle category",
                CommonMistakes = "Dùng 'thereby + V nguyên mẫu' thay vì 'thereby + V-ing'",
                PracticeExercise = "Rewrite: Company revenue doubled in Q3 and this allowed further expansion.",
                Tags = "participle, task1, trend, cause_effect",
                DisplayOrder = 4
            },
            new GrammarStructure
            {
                StructureCode = "FND_TENSE_01",
                BandLevel = "4.0 - 5.0",
                Category = "General",
                GrammarTopic = "Thì hiện tại đơn (Present Simple)",
                Formula = "S + V(s/es) + O | S + do/does + not + V",
                UsageFunction = "Diễn tả chân lý, sự thật hiển nhiên, thói quen lặp lại; diễn tả số liệu/xu hướng cố định trong Task 1 & quan điểm trong Task 2.",
                BasicExample = "He walks to work every morning.",
                AdvancedExample = "The pie chart illustrates that agriculture accounts for the largest proportion of total water consumption.",
                VietnameseMeaning = "Biểu đồ tròn minh họa rằng ngành nông nghiệp chiếm tỷ trọng lớn nhất trong tổng mức tiêu thụ nước.",
                KeyCollocations = "account for, illustrate that, proportion of",
                CommonMistakes = "Quên thêm s/es khi chủ ngữ là ngôi thứ 3 số ít (He, She, It, danh từ số ít).",
                PracticeExercise = "Chia động từ: The data (indicate) that urban areas (consume) more electricity than rural regions.",
                Tags = "present_simple, tenses, foundation, task1",
                DisplayOrder = 10
            },
            new GrammarStructure
            {
                StructureCode = "FND_TENSE_02",
                BandLevel = "4.0 - 5.0",
                Category = "General",
                GrammarTopic = "Thì hiện tại tiếp diễn (Present Continuous)",
                Formula = "S + am/is/are + V-ing",
                UsageFunction = "Diễn tả hành động đang diễn ra hoặc xu hướng đang biến chuyển trong xã hội hiện đại.",
                BasicExample = "I am studying English right now.",
                AdvancedExample = "An increasing number of developing countries are adopting digital payment systems to modernize their economies.",
                VietnameseMeaning = "Ngày càng có nhiều quốc gia đang phát triển áp dụng hệ thống thanh toán số để hiện đại hóa nền kinh tế.",
                KeyCollocations = "an increasing number of, adopt systems, modernize economy",
                CommonMistakes = "Dùng thì tiếp diễn với các động từ trạng thái (stative verbs: know, understand, believe, love).",
                PracticeExercise = "Viết lại câu: Society changes quickly because technology develops fast.",
                Tags = "present_continuous, tenses, trend, foundation",
                DisplayOrder = 11
            },
            new GrammarStructure
            {
                StructureCode = "FND_TENSE_03",
                BandLevel = "5.0 - 6.0",
                Category = "General",
                GrammarTopic = "Thì hiện tại hoàn thành (Present Perfect)",
                Formula = "S + have/has + V3/ed",
                UsageFunction = "Diễn tả sự việc bắt đầu trong quá khứ kéo dài đến hiện tại; mở đầu câu Introduction/Overview trong IELTS Writing.",
                BasicExample = "I have lived here for five years.",
                AdvancedExample = "Over the past two decades, globalization has transformed international trade patterns considerably.",
                VietnameseMeaning = "Trong hai thập kỷ qua, toàn cầu hóa đã thay đổi đáng kể các mô hình thương mại quốc tế.",
                KeyCollocations = "over the past decades, transform patterns, international trade",
                CommonMistakes = "Nhầm lẫn mốc thời gian cụ thể (in 2010 -> dùng Quá khứ đơn) với khoảng thời gian (since/for).",
                PracticeExercise = "Viết câu mở đoạn: Công nghệ đã làm thay đổi cách con người giao tiếp trong những năm gần đây.",
                Tags = "present_perfect, tenses, writing, overview",
                DisplayOrder = 12
            },
            new GrammarStructure
            {
                StructureCode = "FND_TENSE_04",
                BandLevel = "4.0 - 5.0",
                Category = "Writing Task 1",
                GrammarTopic = "Thì quá khứ đơn (Past Simple)",
                Formula = "S + V2/ed + O",
                UsageFunction = "Thì quan trọng nhất trong IELTS Writing Task 1 khi biểu đồ có năm diễn ra trong quá khứ.",
                BasicExample = "I visited London last summer.",
                AdvancedExample = "Between 2000 and 2015, public expenditure on education dropped slightly before stabilizing at 15%.",
                VietnameseMeaning = "Từ năm 2000 đến năm 2015, chi tiêu công cho giáo dục giảm nhẹ trước khi ổn định ở mức 15%.",
                KeyCollocations = "public expenditure, drop slightly, stabilize at",
                CommonMistakes = "Quên chia động từ bất quy tắc (ví dụ: write -> wrote, fall -> fell, rise -> rose).",
                PracticeExercise = "Viết lại số liệu: In 2010, the car price (fall) to $5,000.",
                Tags = "past_simple, task1, chart, trend",
                DisplayOrder = 13
            },
            new GrammarStructure
            {
                StructureCode = "FND_TENSE_05",
                BandLevel = "5.5 - 6.5",
                Category = "General",
                GrammarTopic = "Thì quá khứ hoàn thành (Past Perfect)",
                Formula = "S + had + V3/ed",
                UsageFunction = "Diễn tả một hành động xảy ra và hoàn tất trước một mốc thời gian hoặc hành động khác trong quá khứ.",
                BasicExample = "When I arrived, the train had already left.",
                AdvancedExample = "By the time the new regulation was enacted, carbon emissions had already peaked and started to decline.",
                VietnameseMeaning = "Vào thời điểm quy định mới được ban hành, lượng khí thải carbon đã đạt đỉnh và bắt đầu giảm xuống.",
                KeyCollocations = "by the time, enact regulation, carbon emissions peak",
                CommonMistakes = "Lạm dụng quá khứ hoàn thành khi cả hai hành động xảy ra song song hoặc không có thứ tự trước sau rõ ràng.",
                PracticeExercise = "Kết hợp 2 câu: The factory closed. Then pollution levels decreased.",
                Tags = "past_perfect, tenses, sequence",
                DisplayOrder = 14
            },
            new GrammarStructure
            {
                StructureCode = "FND_TENSE_06",
                BandLevel = "5.0 - 6.0",
                Category = "Writing Task 1",
                GrammarTopic = "Thì tương lai & Dự báo (Future & Predictions)",
                Formula = "S + is/are projected to + V | S + will + V",
                UsageFunction = "Dùng để miêu tả số liệu dự báo tương lai trong Task 1 (năm 2030, 2050).",
                BasicExample = "It will rain tomorrow.",
                AdvancedExample = "The global population is projected to exceed nine billion individuals by the middle of the century.",
                VietnameseMeaning = "Dân số toàn cầu được dự báo sẽ vượt quá chín tỷ người vào giữa thế kỷ này.",
                KeyCollocations = "is projected to, exceed individuals, by the middle of",
                CommonMistakes = "Khẳng định chắc chắn 'will' cho số liệu biểu đồ thay vì dùng các từ học thuật dự báo (is predicted/forecast/expected to).",
                PracticeExercise = "Viết lại câu dự báo năm 2040: Renewable energy will produce 60% of power.",
                Tags = "future, projection, task1, prediction",
                DisplayOrder = 15
            },
            new GrammarStructure
            {
                StructureCode = "FND_PASS_01",
                BandLevel = "5.0 - 6.0",
                Category = "Writing Task 1 & 2",
                GrammarTopic = "Câu bị động (Passive Voice)",
                Formula = "S + be + V3/ed (+ by O)",
                UsageFunction = "Tạo phong cách học thuật khách quan trong IELTS Writing, đặc biệt khi tả bài quy trình (Process Task 1).",
                BasicExample = "The chef cooked the meal.",
                AdvancedExample = "Raw tea leaves are harvested by hand and subsequently transported to processing facilities for fermentation.",
                VietnameseMeaning = "Lá chè tươi được thu hoạch thủ công và sau đó được vận chuyển đến các cơ sở chế biến để lên men.",
                KeyCollocations = "harvest by hand, subsequently transported to, processing facilities",
                CommonMistakes = "Dùng sai dạng phân từ 2 (V3) hoặc quên động từ 'to be' chia theo thì của câu.",
                PracticeExercise = "Chuyển sang câu bị động: Workers clean and dry the coffee beans.",
                Tags = "passive_voice, process, task1, academic",
                DisplayOrder = 16
            },
            new GrammarStructure
            {
                StructureCode = "FND_COND_01",
                BandLevel = "4.0 - 5.0",
                Category = "Speaking",
                GrammarTopic = "Câu điều kiện loại 1 (Conditional Type 1)",
                Formula = "If + S + V(present), S + will/can/may + V",
                UsageFunction = "Diễn tả điều kiện có thật ở hiện tại hoặc tương lai; đưa ra giải pháp và hệ quả trong Speaking Part 3.",
                BasicExample = "If it rains, I will stay at home.",
                AdvancedExample = "If authorities implement stricter environmental standards, air quality in metropolitan areas will improve markedly.",
                VietnameseMeaning = "Nếu các cơ quan chức năng thực thi các tiêu chuẩn môi trường nghiêm ngặt hơn, chất lượng không khí ở các đô thị sẽ cải thiện rõ rệt.",
                KeyCollocations = "implement stricter standards, environmental standards, improve markedly",
                CommonMistakes = "Dùng 'will' ở mệnh đề If (ví dụ sai: If authorities will implement...).",
                PracticeExercise = "Hoàn thành câu: If students (receive) career guidance early, they (choose) better majors.",
                Tags = "conditional, type1, speaking, solutions",
                DisplayOrder = 17
            },
            new GrammarStructure
            {
                StructureCode = "FND_COND_02",
                BandLevel = "5.5 - 6.5",
                Category = "Writing Task 2",
                GrammarTopic = "Câu điều kiện loại 2 (Conditional Type 2)",
                Formula = "If + S + V2/were, S + would/could/might + V",
                UsageFunction = "Diễn tả giả định trái với thực tế hiện tại; lập luận biện chứng phản đề trong Task 2.",
                BasicExample = "If I were you, I would study harder.",
                AdvancedExample = "If governments subsidized renewable energy infrastructure adequately, fossil fuel dependence would diminish rapidly.",
                VietnameseMeaning = "Nếu các chính phủ trợ cấp thỏa đáng cho cơ sở hạ tầng năng lượng tái tạo, sự phụ thuộc vào nhiên liệu hóa thạch sẽ giảm nhanh chóng.",
                KeyCollocations = "subsidize infrastructure, fossil fuel dependence, diminish rapidly",
                CommonMistakes = "Dùng 'was' thay vì 'were' trong văn phong học thuật trang trọng; nhầm lẫn thì ở mệnh đề chính.",
                PracticeExercise = "Viết lại câu giả định: Because petrol is cheap, people drive private cars too much.",
                Tags = "conditional, type2, task2, hypothesis",
                DisplayOrder = 18
            },
            new GrammarStructure
            {
                StructureCode = "FND_COND_03",
                BandLevel = "6.5 - 7.5",
                Category = "Writing Task 2",
                GrammarTopic = "Câu điều kiện loại 3 (Conditional Type 3)",
                Formula = "If + S + had + V3/ed, S + would/could + have + V3/ed",
                UsageFunction = "Diễn tả giả định trái ngược với quá khứ, dùng để đánh giá các quyết định lịch sử hoặc chính sách trong quá khứ.",
                BasicExample = "If I had known the answer, I would have told you.",
                AdvancedExample = "Had municipal planners invested earlier in underground transit, severe gridlock could have been averted.",
                VietnameseMeaning = "Nếu các nhà quy hoạch đô thị đầu tư sớm hơn vào giao thông ngầm, tình trạng tắc nghẽn giao thông nghiêm trọng đã có thể được ngăn chặn.",
                KeyCollocations = "municipal planners, underground transit, avert gridlock",
                CommonMistakes = "Dùng nhầm would have ở mệnh đề If (ví dụ sai: If planners would have invested...).",
                PracticeExercise = "Viết câu điều kiện loại 3: The company did not innovate, so it lost market share.",
                Tags = "conditional, type3, past_hypothesis",
                DisplayOrder = 19
            },
            new GrammarStructure
            {
                StructureCode = "FND_REL_01",
                BandLevel = "5.0 - 6.0",
                Category = "General",
                GrammarTopic = "Mệnh đề quan hệ (Relative Clauses)",
                Formula = "Noun + who / which / that / whose + Clause / Verb",
                UsageFunction = "Cấu trúc cốt lõi để tạo câu phức (Complex Sentence) nâng điểm tiêu chí Grammatical Range.",
                BasicExample = "The woman who lives next door is a doctor.",
                AdvancedExample = "Students who engage in extracurricular activities develop superior interpersonal skills that benefit their careers.",
                VietnameseMeaning = "Những sinh viên tham gia vào các hoạt động ngoại khóa sẽ phát triển các kỹ năng giao tiếp vượt trội có ích cho sự nghiệp của họ.",
                KeyCollocations = "engage in activities, extracurricular activities, interpersonal skills",
                CommonMistakes = "Dùng nhầm 'which' cho người hoặc dùng dấu phẩy sai trong mệnh đề quan hệ xác định.",
                PracticeExercise = "Nối 2 câu bằng mệnh đề quan hệ: Some countries invest in solar energy. These countries reduce emissions.",
                Tags = "relative_clause, complex_sentence, foundation",
                DisplayOrder = 20
            },
            new GrammarStructure
            {
                StructureCode = "FND_COMP_01",
                BandLevel = "4.0 - 5.0",
                Category = "Writing Task 1",
                GrammarTopic = "Câu so sánh hơn & nhất (Comparisons)",
                Formula = "S + V + comparative Adj/Adv + than / the + superlative Adj",
                UsageFunction = "Bắt buộc phải có trong IELTS Writing Task 1 để đạt tiêu chí Task Achievement (so sánh số liệu).",
                BasicExample = "City life is busier than country life.",
                AdvancedExample = "Expenditure on healthcare in the US was significantly higher than that in European nations throughout the period.",
                VietnameseMeaning = "Chi tiêu cho y tế ở Mỹ cao hơn đáng kể so với ở các quốc gia châu Âu trong suốt giai đoạn này.",
                KeyCollocations = "expenditure on, significantly higher than, throughout the period",
                CommonMistakes = "Quên dùng 'that of' / 'those of' khi so sánh đối tượng gián tiếp.",
                PracticeExercise = "Viết câu so sánh: Car sales (40%) and Motorbike sales (20%) in 2020.",
                Tags = "comparison, task1, comparative, superlative",
                DisplayOrder = 21
            },
            new GrammarStructure
            {
                StructureCode = "FND_COMP_02",
                BandLevel = "6.5 - 7.5",
                Category = "Writing Task 2",
                GrammarTopic = "Cấu trúc so sánh kép (Double Comparative)",
                Formula = "The + comparative [Adj/Adv] + S + V, the + comparative [Adj/Adv] + S + V",
                UsageFunction = "Biểu đạt mối tương quan nhân quả chặt chẽ, tạo cấu trúc câu cân xứng rất được giám khảo IELTS đánh giá cao.",
                BasicExample = "The more you practice, the better you become.",
                AdvancedExample = "The more interconnected global financial markets become, the more susceptible national economies are to external shocks.",
                VietnameseMeaning = "Thị trường tài chính toàn cầu càng liên kết chặt chẽ thì các nền kinh tế quốc gia càng dễ bị tổn thương trước những cú sốc bên ngoài.",
                KeyCollocations = "interconnected markets, susceptible to external shocks, national economies",
                CommonMistakes = "Thiếu mạo từ 'The' ở một trong hai vế hoặc chia sai dạng so sánh hơn.",
                PracticeExercise = "Viết lại: When children read more books, their vocabulary expands faster.",
                Tags = "double_comparative, task2, correlation",
                DisplayOrder = 22
            },
            new GrammarStructure
            {
                StructureCode = "FND_SVA_01",
                BandLevel = "4.0 - 5.0",
                Category = "General",
                GrammarTopic = "Sự hòa hợp Chủ - Vị (Subject-Verb Agreement)",
                Formula = "Singular Subject + Singular Verb | Plural Subject + Plural Verb",
                UsageFunction = "Quy tắc nền tảng số 1 để tránh lỗi ngữ pháp cơ bản (nguyên nhân hàng đầu khiến học viên kẹt ở Band 5.0 - 5.5).",
                BasicExample = "The teacher and students are in the room.",
                AdvancedExample = "The growing reliance on automated algorithms and artificial intelligence poses profound ethical challenges.",
                VietnameseMeaning = "Sự phụ thuộc ngày càng tăng vào các thuật toán tự động và trí tuệ nhân tạo đặt ra những thách thức sâu sắc về đạo đức.",
                KeyCollocations = "growing reliance on, automated algorithms, pose ethical challenges",
                CommonMistakes = "Nhầm chủ ngữ chính khi có cụm giới từ xen giữa (nhìn thấy algorithms tưởng số nhiều chia 'pose' thay vì 'poses' theo reliance).",
                PracticeExercise = "Chia động từ: A wide range of educational resources (be) accessible online.",
                Tags = "subject_verb_agreement, grammar_accuracy, foundation",
                DisplayOrder = 23
            },
            new GrammarStructure
            {
                StructureCode = "FND_MODAL_01",
                BandLevel = "5.0 - 6.0",
                Category = "General",
                GrammarTopic = "Động từ khuyết thiếu (Modal Verbs)",
                Formula = "S + can/could/should/must/may/might + V (bare-infinitive)",
                UsageFunction = "Dùng để đưa ra khuyến nghị (should/ought to) hoặc làm giảm sắc thái khẳng định tuyệt đối (hedging: may/might/could).",
                BasicExample = "You should drink more water.",
                AdvancedExample = "Policymakers ought to prioritize sustainable urban development to curb excessive resource depletion.",
                VietnameseMeaning = "Các nhà hoạch định chính sách nên ưu tiên phát triển đô thị bền vững để hạn chế sự cạn kiệt tài nguyên quá mức.",
                KeyCollocations = "policymakers, prioritize development, curb depletion",
                CommonMistakes = "Thêm 'to' sau modal verbs thông thường (can to do, must to go) hoặc chia đuôi s/ed cho modal verb.",
                PracticeExercise = "Viết lại với sắc thái giảm nhẹ (hedging): Overwork definitely causes mental health issues.",
                Tags = "modal_verbs, hedging, recommendation",
                DisplayOrder = 24
            },
            new GrammarStructure
            {
                StructureCode = "FND_CONJ_01",
                BandLevel = "5.5 - 6.5",
                Category = "Writing Task 2",
                GrammarTopic = "Liên từ & Từ nối lập luận (Cohesive Devices)",
                Formula = "Clause 1; however, Clause 2 | Although + Clause 1, Clause 2",
                UsageFunction = "Tối ưu hóa điểm tiêu chí Coherence & Cohesion (tính mạch lạc và liên kết) trong bài thi IELTS.",
                BasicExample = "Although it was cold, we went outside.",
                AdvancedExample = "Although remote working offers unprecedented flexibility, it may diminish team cohesion and workplace camaraderie.",
                VietnameseMeaning = "Mặc dù làm việc từ xa mang lại sự linh hoạt chưa từng có, nhưng nó có thể làm giảm tính gắn kết của nhóm và tình đồng nghiệp tại nơi làm việc.",
                KeyCollocations = "unprecedented flexibility, diminish cohesion, workplace camaraderie",
                CommonMistakes = "Dùng đồng thời cả 'Although' và 'But' trong cùng một câu (Lỗi kinh điển của người Việt).",
                PracticeExercise = "Nối 2 câu: Higher taxes reduce smoking. Some people still buy cigarettes.",
                Tags = "conjunctions, cohesion, contrast, task2",
                DisplayOrder = 25
            },
            new GrammarStructure
            {
                StructureCode = "FND_ART_01",
                BandLevel = "4.0 - 5.0",
                Category = "General",
                GrammarTopic = "Mạo từ (Articles: A, An, The)",
                Formula = "A/An + Danh từ số ít đếm được | The + Danh từ xác định | Zero article + Danh từ số nhiều chung",
                UsageFunction = "Tránh lỗi trừ điểm vặt nhưng xuất hiện dày đặc trong cả bài viết Writing Task 1 và Task 2.",
                BasicExample = "I saw an elephant at the zoo.",
                AdvancedExample = "Access to quality education remains the single most powerful catalyst for socio-economic mobility.",
                VietnameseMeaning = "Quyền tiếp cận giáo dục chất lượng vẫn là chất xúc tác mạnh mẽ nhất cho sự dịch chuyển kinh tế - xã hội.",
                KeyCollocations = "access to education, catalyst for mobility, socio-economic mobility",
                CommonMistakes = "Thêm 'the' trước các danh từ mang nghĩa chung (ví dụ sai: The society should protect the children).",
                PracticeExercise = "Điền mạo từ thích hợp: (...) technology plays (...) crucial role in modern life.",
                Tags = "articles, grammar_accuracy, foundation",
                DisplayOrder = 26
            },
            new GrammarStructure
            {
                StructureCode = "FND_PREP_01",
                BandLevel = "4.0 - 5.0",
                Category = "Writing Task 1",
                GrammarTopic = "Giới từ mô tả số liệu (Prepositions in Data)",
                Formula = "increase from X to Y | increase by Z | stand at N | reach a peak of N",
                UsageFunction = "Chính xác hóa việc miêu tả số liệu mốc và độ chênh lệch trong IELTS Task 1.",
                BasicExample = "The meeting starts at 9 AM on Monday.",
                AdvancedExample = "The unemployment rate rose from 4.2% in 2018 to 6.8% in 2020, representing an increase of 2.6%.",
                VietnameseMeaning = "Tỷ lệ thất nghiệp đã tăng từ 4,2% năm 2018 lên 6,8% năm 2020, tương ứng với mức tăng 2,6%.",
                KeyCollocations = "unpreductory rate, rise from... to..., increase of",
                CommonMistakes = "Nhầm lẫn giữa 'increase to' (tăng ĐẾN mức) và 'increase by' (tăng THÊM một lượng).",
                PracticeExercise = "Điền giới từ: Oil prices climbed (...) $50 (...) $80 per barrel.",
                Tags = "prepositions, task1, data_description",
                DisplayOrder = 27
            },
            new GrammarStructure
            {
                StructureCode = "FND_PARALLEL_01",
                BandLevel = "6.0 - 7.0",
                Category = "Writing Task 2",
                GrammarTopic = "Cấu trúc song hành (Parallel Structure)",
                Formula = "X, Y, and Z (Các thành phần liệt kê phải cùng từ loại hoặc cùng dạng ngữ pháp)",
                UsageFunction = "Tạo tính nhịp nhàng, chặt chẽ và chuẩn học thuật khi liệt kê các giải pháp, nguyên nhân hoặc lợi ích.",
                BasicExample = "She likes swimming, running, and reading.",
                AdvancedExample = "Effective leadership requires inspiring team members, fostering transparent communication, and resolving conflicts promptly.",
                VietnameseMeaning = "Lãnh đạo hiệu quả đòi hỏi phải truyền cảm hứng cho các thành viên trong nhóm, thúc đẩy giao tiếp minh bạch và giải quyết xung đột kịp thời.",
                KeyCollocations = "effective leadership, foster communication, resolve conflicts promptly",
                CommonMistakes = "Phá vỡ cấu trúc song hành (ví dụ viết sai: requires inspiring, communication, and to resolve conflicts).",
                PracticeExercise = "Sửa lỗi câu: The program aims to educate citizens, reducing waste, and cleaner parks.",
                Tags = "parallel_structure, academic_style, writing",
                DisplayOrder = 28
            },
            new GrammarStructure
            {
                StructureCode = "FND_LINK_01",
                BandLevel = "4.0 - 5.0",
                Category = "Writing Task 2",
                GrammarTopic = "Động từ nối (Linking Verbs)",
                Formula = "S + remain / become / appear / seem + Adj (không dùng Adv)",
                UsageFunction = "Diễn đạt trạng thái ổn định hoặc biến đổi trong bài viết và nói.",
                BasicExample = "The soup tastes delicious.",
                AdvancedExample = "Housing affordability remains a pressing concern for low-income households in urban centers.",
                VietnameseMeaning = "Khả năng chi trả nhà ở vẫn là một mối quan tâm cấp bách đối với các hộ gia đình có thu nhập thấp tại các trung tâm đô thị.",
                KeyCollocations = "housing affordability, remain a pressing concern, low-income households",
                CommonMistakes = "Dùng trạng từ (Adverb) sau linking verb (ví dụ viết sai: remains pressingly concern).",
                PracticeExercise = "Chọn dạng đúng: The situation becomes (critical / critically) if no action is taken.",
                Tags = "linking_verbs, state, writing",
                DisplayOrder = 29
            },
            new GrammarStructure
            {
                StructureCode = "FND_TAG_01",
                BandLevel = "4.0 - 5.0",
                Category = "Speaking",
                GrammarTopic = "Câu hỏi đuôi (Tag Questions)",
                Formula = "Statement (+), aux (-) + subject? | Statement (-), aux (+) + subject?",
                UsageFunction = "Tạo sự tương tác tự nhiên, kiểm tra thông tin hoặc duy trì mạch hội thoại trong IELTS Speaking Part 1 & 3.",
                BasicExample = "You are a student, aren't you?",
                AdvancedExample = "Investing in public infrastructure ultimately benefits all demographics of society, doesn't it?",
                VietnameseMeaning = "Đầu tư vào cơ sở hạ tầng công cộng cuối cùng sẽ mang lại lợi ích cho mọi tầng lớp nhân khẩu học trong xã hội, đúng không?",
                KeyCollocations = "invest in infrastructure, benefit all demographics, demographics of society",
                CommonMistakes = "Nhầm trợ động từ hoặc quên đảo dấu khẳng định - phủ định giữa hai vế.",
                PracticeExercise = "Hoàn thành câu hỏi đuôi: Most citizens agree with the new policy, (...) ?",
                Tags = "tag_questions, speaking, interactive",
                DisplayOrder = 30
            }
        };
    }
}
