using Backend.Domain.Entities;
using System.Text.Json;

namespace Backend.Infrastructure.Persistence;

public static class StorySeedData
{
    public static async Task SeedStoriesAsync(AppDbContext dbContext, Backend.Application.Abstractions.IR2StorageService? r2Storage = null)
    {
        var seedStories = new List<Story>
        {
            new Story
            {
                Title = "The School Race",
                Slug = "the-school-race",
                Level = "A1",
                IeltsBand = "3.0 - 3.5",
                Category = "Đời sống",
                Summary = "Một ngày hội thể thao đầy cảm xúc của Tom khi vượt qua cơn đau để hoàn thành đường chạy cùng sự cổ vũ của mẹ.",
                ThumbnailUrl = "https://images.unsplash.com/photo-1461896836934-ffe607ba8211?w=800&auto=format&fit=crop&q=80",
                WordCount = 135,
                EstimatedMinutes = 3,
                ContentJson = JsonSerializer.Serialize(new[]
                {
                    new {
                        en = "It is Sports Day at Tom's school. Tom's mother comes to watch him. \"Run well, Tom!\" she says with a big smile.",
                        vi = "Hôm nay là Ngày hội Thể thao ở trường của Tom. Mẹ của Tom đến xem cậu thi đấu. \"Chạy tốt nhé Tom!\" mẹ cười rạng rỡ và nói."
                    },
                    new {
                        en = "Tom puts on his red shoes. Mrs. Green, his teacher, says, \"Ready, boys?\" Then she fires a small gun. Bang! The race starts.",
                        vi = "Tom đi đôi giày màu đỏ của mình vào. Cô Green, giáo viên của cậu, hỏi: \"Các em sẵn sàng chưa?\". Rồi cô bắn phát súng nhỏ. Đoàng! Cuộc đua bắt đầu."
                    },
                    new {
                        en = "Tom runs fast. Then he feels a sharp pain in his leg. It hurts! He wants to stop. But he sees his mother. She opens her mouth and shouts, \"Go, Tom, go!\"",
                        vi = "Tom chạy rất nhanh. Rồi cậu cảm thấy một cơn đau nhói ở chân. Đau quá! Cậu muốn dừng lại. Nhưng cậu nhìn thấy mẹ. Mẹ mở miệng và hét lớn: \"Cố lên Tom, tiến lên!\""
                    },
                    new {
                        en = "Tom runs slowly, but he does not stop. He finishes the race. He is third! Mrs. Green puts a shiny medal around his neck. \"Good job! You did not give up,\" she says.",
                        vi = "Tom chạy chậm lại, nhưng cậu không hề dừng bước. Cậu hoàn thành đường chạy. Cậu về đích thứ ba! Cô Green đeo một tấm huy chương sáng lấp lánh vào cổ cậu. \"Làm tốt lắm! Em đã không bỏ cuộc,\" cô nói."
                    },
                    new {
                        en = "Tom's leg still hurts a little, but he is happy. His mother gives him water and a warm hug. \"Rest now,\" she says. \"Then you can play again.\"",
                        vi = "Chân của Tom vẫn còn hơi đau, nhưng cậu rất vui vẻ. Mẹ đưa nước cho cậu và ôm cậu thật ấm áp. \"Nghỉ ngơi đi con,\" mẹ nói. \"Sau đó con có thể chơi tiếp.\""
                    }
                }),
                VocabularyJson = JsonSerializer.Serialize(new[]
                {
                    new {
                        word = "pain",
                        phonetic = "/peɪn/",
                        pos = "noun",
                        meaning = "cơn đau, sự đau đớn",
                        example = "He feels a sharp pain in his leg.",
                        collocations = new[] { "sharp pain", "feel pain", "relieve pain" }
                    },
                    new {
                        word = "medal",
                        phonetic = "/ˈmed.əl/",
                        pos = "noun",
                        meaning = "huy chương",
                        example = "She puts a shiny medal around his neck.",
                        collocations = new[] { "gold medal", "win a medal" }
                    },
                    new {
                        word = "give up",
                        phonetic = "/ɡɪv ʌp/",
                        pos = "phrasal verb",
                        meaning = "bỏ cuộc, từ bỏ",
                        example = "You did not give up.",
                        collocations = new[] { "never give up", "give up hope" }
                    },
                    new {
                        word = "smile",
                        phonetic = "/smaɪl/",
                        pos = "noun / verb",
                        meaning = "nụ cười, mỉm cười",
                        example = "She says with a big smile.",
                        collocations = new[] { "big smile", "smile warmly" }
                    }
                }),
                QuestionsJson = JsonSerializer.Serialize(new[]
                {
                    new {
                        question = "Why did Tom want to stop running during the race?",
                        options = new[] { "Because he was tired", "Because his leg hurt", "Because he lost his red shoes", "Because it was raining" },
                        correctIndex = 1,
                        explanation = "Trong bài có câu: 'Then he feels a sharp pain in his leg. It hurts! He wants to stop.' (Cậu cảm thấy đau ở chân nên muốn dừng lại)."
                    },
                    new {
                        question = "Who encouraged Tom to keep running?",
                        options = new[] { "His friends", "Mrs. Green", "His mother", "Nobody" },
                        correctIndex = 2,
                        explanation = "Mẹ của Tom đã hét lớn: 'Go, Tom, go!' để cổ vũ cậu tiếp tục chạy."
                    },
                    new {
                        question = "What place did Tom finish in the race?",
                        options = new[] { "First place", "Second place", "Third place", "Fourth place" },
                        correctIndex = 2,
                        explanation = "Trong bài nêu rõ: 'He finishes the race. He is third!' (Cậu về đích thứ ba)."
                    }
                }),
                IsPublished = true,
                CreatedAt = DateTime.UtcNow
            },
            new Story
            {
                Title = "The Mystery of the Old Lighthouse",
                Slug = "the-mystery-of-the-old-lighthouse",
                Level = "B1",
                IeltsBand = "5.0 - 5.5",
                Category = "Phiêu lưu",
                Summary = "Bí ẩn về ngọn hải đăng cổ kính trên bờ biển phía bắc và câu chuyện về người gác hải đăng kiên cường trước mọi cơn bão.",
                ThumbnailUrl = "https://images.unsplash.com/photo-1506953823976-52e1fdc0149a?w=800&auto=format&fit=crop&q=80",
                WordCount = 210,
                EstimatedMinutes = 4,
                ContentJson = JsonSerializer.Serialize(new[]
                {
                    new {
                        en = "Every evening, the solitary keeper climbed the winding spiral stairs of the weathered stone lighthouse. He looked out at the restless sea, where waves crashed violently against the jagged cliffs.",
                        vi = "Mỗi buổi chiều tà, người gác hải đăng đơn độc lại leo lên những bậc thang xoắn ốc quanh co của ngọn hải đăng bằng đá đã dãi dầu sương gió. Ông phóng tầm mắt ra vùng biển cuộn sóng dữ dội, nơi những con sóng đập mạnh vào vách đá lởm chởm."
                    },
                    new {
                        en = "For fifty years, this bright beacon had guided countless ships safely through treacherous storms. The local mariners regarded the lighthouse as their guardian angel in the dark.",
                        vi = "Suốt năm mươi năm qua, ngọn hải đăng rực sáng này đã dẫn đường an toàn cho vô số chuyến tàu vượt qua những cơn bão hiểm trở. Những người đi biển địa phương coi ngọn hải đăng như thiên thần hộ mệnh của họ trong màn đêm."
                    },
                    new {
                        en = "One stormy midnight, the powerful generator suddenly failed, plunging the coast into utter darkness. A heavy cargo vessel was approaching dangerously close to the rocky reef.",
                        vi = "Vào một đêm bão tuyết lúc nửa đêm, máy phát điện công suất lớn đột ngột hỏng, đẩy cả bờ biển vào bóng tối mịt mù. Một con tàu chở hàng nặng đang tiến nguy hiểm lại gần rạn đá ngầm."
                    },
                    new {
                        en = "Without hesitation, the keeper hauled antique kerosene lamps to the glass tower and lit them one by one. Their warm golden glow illuminated the misty sky, signaling the captain just in time to steer away from destruction.",
                        vi = "Không chút do dự, người gác hải đăng kéo những chiếc đèn dầu cổ lên tháp kính và thắp sáng từng chiếc một. Ánh sáng vàng ấm áp của chúng đã soi rọi bầu trời đầy sương, kịp thời báo hiệu cho thuyền trưởng bẻ lái tránh khỏi thảm họa."
                    }
                }),
                VocabularyJson = JsonSerializer.Serialize(new[]
                {
                    new {
                        word = "solitary",
                        phonetic = "/ˈsɒl.ɪ.tər.i/",
                        pos = "adjective",
                        meaning = "cô độc, một mình",
                        example = "The solitary keeper climbed the spiral stairs.",
                        collocations = new[] { "solitary life", "solitary figure" }
                    },
                    new {
                        word = "beacon",
                        phonetic = "/ˈbiː.kən/",
                        pos = "noun",
                        meaning = "ngọn hải đăng, tín hiệu dẫn đường",
                        example = "This bright beacon had guided countless ships.",
                        collocations = new[] { "beacon of hope", "light beacon" }
                    },
                    new {
                        word = "treacherous",
                        phonetic = "/ˈtretʃ.ər.əs/",
                        pos = "adjective",
                        meaning = "nguy hiểm, đầy trắc trở (thường dùng cho thời tiết/địa hình)",
                        example = "Ships safely passed through treacherous storms.",
                        collocations = new[] { "treacherous waters", "treacherous conditions" }
                    },
                    new {
                        word = "illuminate",
                        phonetic = "/ɪˈluː.mɪ.neɪt/",
                        pos = "verb",
                        meaning = "chiếu sáng, soi rọi",
                        example = "Their warm glow illuminated the misty sky.",
                        collocations = new[] { "illuminate the path", "brightly illuminated" }
                    },
                    new {
                        word = "mariner",
                        phonetic = "/ˈmær.ɪ.nər/",
                        pos = "noun",
                        meaning = "người đi biển, thủy thủ",
                        example = "The local mariners regarded the lighthouse as their guardian.",
                        collocations = new[] { "ancient mariner", "seasoned mariner" }
                    }
                }),
                QuestionsJson = JsonSerializer.Serialize(new[]
                {
                    new {
                        question = "What was the main purpose of the lighthouse?",
                        options = new[] { "To observe sea wildlife", "To guide ships safely through storms", "To generate electricity for the town", "To store antique kerosene lamps" },
                        correctIndex = 1,
                        explanation = "Ngọn hải đăng có mục đích dẫn đường cho tàu bè qua những cơn bão an toàn ('guided countless ships safely through treacherous storms')."
                    },
                    new {
                        question = "What emergency happened during the stormy midnight?",
                        options = new[] { "The stairs collapsed", "The lighthouse keeper fell asleep", "The generator failed and cut off the light", "The cargo vessel hit the lighthouse" },
                        correctIndex = 2,
                        explanation = "Máy phát điện đột ngột bị hỏng khiến toàn bộ ngọn đèn phụt tắt ('the powerful generator suddenly failed')."
                    },
                    new {
                        question = "How did the keeper save the approaching cargo vessel?",
                        options = new[] { "He radioed the coast guard", "He lit antique kerosene lamps by hand", "He built a bonfire on the beach", "He repaired the generator immediately" },
                        correctIndex = 1,
                        explanation = "Người gác hải đăng đã mang và thắp những ngọn đèn dầu cổ ('hauled antique kerosene lamps... and lit them one by one')."
                    }
                }),
                IsPublished = true,
                CreatedAt = DateTime.UtcNow
            },
            new Story
            {
                Title = "The Mountain That Learned to Speak",
                Slug = "the-mountain-that-learned-to-speak",
                Level = "B2",
                IeltsBand = "6.0 - 6.5",
                Category = "Khoa học & Tự nhiên",
                Summary = "Một hiện tượng địa chất kỳ lạ khi dãy núi Alps phát ra những âm thanh kỳ bí được các nhà khoa học giải mã.",
                ThumbnailUrl = "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?w=800&auto=format&fit=crop&q=80",
                WordCount = 270,
                EstimatedMinutes = 5,
                ContentJson = JsonSerializer.Serialize(new[]
                {
                    new {
                        en = "Deep in the heart of the Swiss Alps, villagers began hearing an extraordinary acoustic phenomenon. At twilight, the massive granite peaks seemed to emit a low, harmonic hum that reverberated across the entire valley.",
                        vi = "Nằm sâu trong lòng dãy núi Alps của Thụy Sĩ, dân làng bắt đầu nghe thấy một hiện tượng âm thanh phi thường. Vào lúc hoàng hôn, những đỉnh núi đá granit đồ sộ dường như phát ra một âm thanh ngân nga trầm ấm, vang vọng khắp thung lũng."
                    },
                    new {
                        en = "Intrigued by widespread rumors of a 'singing mountain', a team of geophysicists and seismologists arrived equipped with high-precision sensors. They sought to unravel the scientific mechanism behind this peculiar auditory enigma.",
                        vi = "Bị cuốn hút bởi những lời đồn đại rộng rãi về 'ngọn núi biết hát', một nhóm các nhà địa vật lý và địa chấn học đã đến với các cảm biến độ chính xác cao. Họ tìm cách giải mã cơ chế khoa học đằng sau bí ẩn thính giác kỳ lạ này."
                    },
                    new {
                        en = "After weeks of continuous recording, the data revealed a fascinating discovery. Subtle geothermal shifts caused pressurized air and subterranean thermal currents to whistle through narrow micro-fissures within the rock formation.",
                        vi = "Sau nhiều tuần ghi chép liên tục, dữ liệu đã tiết lộ một khám phá đầy mê hoặc. Những chuyển dịch địa nhiệt tinh tế đã khiến không khí áp suất cao và các luồng nhiệt ngầm dưới lòng đất rít qua những khe nứt vi mô hẹp trong cấu trúc đá."
                    },
                    new {
                        en = "The natural resonance transformed the mountain into a colossal wind instrument. This breakthrough demonstrated how geological forces can interact in unexpected ways to generate breathtaking natural wonders.",
                        vi = "Sự cộng hưởng tự nhiên này đã biến cả ngọn núi thành một nhạc cụ gió khổng lồ. Đột phá này chứng minh cách các lực địa chất có thể tương tác theo những cách đầy bất ngờ để tạo ra những kỳ quan thiên nhiên ngoạn mục."
                    }
                }),
                VocabularyJson = JsonSerializer.Serialize(new[]
                {
                    new {
                        word = "phenomenon",
                        phonetic = "/fəˈnɒm.ɪ.nən/",
                        pos = "noun",
                        meaning = "hiện tượng (tự nhiên hoặc xã hội)",
                        example = "Villagers began hearing an extraordinary acoustic phenomenon.",
                        collocations = new[] { "natural phenomenon", "rare phenomenon" }
                    },
                    new {
                        word = "reverberate",
                        phonetic = "/rɪˈvɜː.bər.eɪt/",
                        pos = "verb",
                        meaning = "vang vọng, dội lại",
                        example = "The sound reverberated across the entire valley.",
                        collocations = new[] { "reverberate through", "loudly reverberate" }
                    },
                    new {
                        word = "unravel",
                        phonetic = "/ʌnˈræv.əl/",
                        pos = "verb",
                        meaning = "làm sáng tỏ, giải mã (bí ẩn)",
                        example = "They sought to unravel the scientific mechanism.",
                        collocations = new[] { "unravel a mystery", "unravel secrets" }
                    },
                    new {
                        word = "subterranean",
                        phonetic = "/ˌsʌb.təˈreɪ.ni.ən/",
                        pos = "adjective",
                        meaning = "dưới lòng đất",
                        example = "Subterranean thermal currents flowed through the rock.",
                        collocations = new[] { "subterranean caves", "subterranean passage" }
                    },
                    new {
                        word = "colossal",
                        phonetic = "/kəˈlɒs.əl/",
                        pos = "adjective",
                        meaning = "khổng lồ, vĩ đại",
                        example = "The mountain became a colossal wind instrument.",
                        collocations = new[] { "colossal scale", "colossal achievement" }
                    }
                }),
                QuestionsJson = JsonSerializer.Serialize(new[]
                {
                    new {
                        question = "What caused the mountain to emit the harmonic sound?",
                        options = new[] { "High-speed tourist trains", "Pressurized air moving through micro-fissures in rocks", "Water flowing through a large dam", "Local musical instruments echoing" },
                        correctIndex = 1,
                        explanation = "Dữ liệu chỉ ra: 'pressurized air and subterranean thermal currents to whistle through narrow micro-fissures within the rock formation'."
                    },
                    new {
                        question = "Which scientific field was primarily involved in investigating the mountain?",
                        options = new[] { "Astronomy and Physics", "Geophysics and Seismology", "Botany and Marine Biology", "Meteorology and Oceanography" },
                        correctIndex = 1,
                        explanation = "Nhóm nghiên cứu gồm các nhà địa vật lý và địa chấn học ('a team of geophysicists and seismologists')."
                    },
                    new {
                        question = "What metaphor did the author use to describe the mountain?",
                        options = new[] { "A sleeping giant", "A stone fortress", "A colossal wind instrument", "A roaring lion" },
                        correctIndex = 2,
                        explanation = "Tác giả miêu tả ngọn núi biến thành một nhạc cụ gió khổng lồ ('a colossal wind instrument')."
                    }
                }),
                IsPublished = true,
                CreatedAt = DateTime.UtcNow
            },
            new Story
            {
                Title = "Everywhere at Once: The Quantum Era",
                Slug = "everywhere-at-once-the-quantum-era",
                Level = "C1",
                IeltsBand = "7.5 - 8.5",
                Category = "Công nghệ & Tương lai",
                Summary = "Khám phá cách tính toán lượng tử đang tái định hình ranh giới của khoa học máy tính và mở ra cuộc cách mạng công nghệ mới.",
                ThumbnailUrl = "https://images.unsplash.com/photo-1635070041078-e363dbe005cb?w=800&auto=format&fit=crop&q=80",
                WordCount = 310,
                EstimatedMinutes = 6,
                ContentJson = JsonSerializer.Serialize(new[]
                {
                    new {
                        en = "For decades, classical computing adhered to binary principles, processing discrete bits of information constrained strictly to states of zero or one. However, the emergence of quantum computing represents an unprecedented paradigm shift in computational capability.",
                        vi = "Trong nhiều thập kỷ, điện toán cổ điển tuân thủ các nguyên lý nhị phân, xử lý các bit thông tin rời rạc bị giới hạn nghiêm ngặt ở các trạng thái 0 hoặc 1. Tuy nhiên, sự xuất hiện của điện toán lượng tử đại diện cho một sự chuyển dịch mô hình chưa từng có trong năng lực tính toán."
                    },
                    new {
                        en = "By harnessing the enigmatic properties of quantum superposition and entanglement, quantum processors can evaluate an exponential multitude of permutations simultaneously. Problems that once demanded millennia for supercomputers can now be resolved within seconds.",
                        vi = "Bằng cách khai thác các đặc tính bí ẩn của sự chồng chập lượng tử và liên kết lượng tử, các bộ xử lý lượng tử có thể đánh giá đồng thời vô số hoán vị theo cấp số nhân. Những bài toán từng đòi hỏi hàng thiên niên kỷ đối với siêu máy tính giờ đây có thể được giải quyết trong vòng vài giây."
                    },
                    new {
                        en = "The ramifications of this technology extend far beyond theoretical mathematics. In pharmaceuticals, quantum simulations expedite molecular modeling to discover life-saving therapeutics at unprecedented velocity, while revolutionizing encryption protocols worldwide.",
                        vi = "Những hệ lụy và tác động sâu rộng của công nghệ này vượt xa khuôn khổ toán học lý thuyết. Trong ngành dược phẩm, các mô phỏng lượng tử thúc đẩy nhanh quá trình lập mô hình phân tử để tìm ra các liệu pháp cứu mạng với tốc độ chưa từng có, đồng thời cách mạng hóa các giao thức mã hóa trên toàn cầu."
                    }
                }),
                VocabularyJson = JsonSerializer.Serialize(new[]
                {
                    new {
                        word = "paradigm shift",
                        phonetic = "/ˈpær.ə.daɪm ʃɪft/",
                        pos = "noun phrase",
                        meaning = "sự chuyển dịch mô hình, thay đổi căn bản trong tư duy/công nghệ",
                        example = "Quantum computing represents an unprecedented paradigm shift.",
                        collocations = new[] { "fundamental paradigm shift", "experience a paradigm shift" }
                    },
                    new {
                        word = "superposition",
                        phonetic = "/ˌsuː.pə.pəˈzɪʃ.ən/",
                        pos = "noun",
                        meaning = "trạng thái chồng chập (vật lý lượng tử)",
                        example = "By harnessing quantum superposition, processors compute simultaneously.",
                        collocations = new[] { "quantum superposition", "state of superposition" }
                    },
                    new {
                        word = "ramification",
                        phonetic = "/ˌræm.ɪ.fɪˈkeɪ.ʃən/",
                        pos = "noun",
                        meaning = "hệ quả, tác động phức tạp",
                        example = "The ramifications of this technology extend far beyond mathematics.",
                        collocations = new[] { "broad ramifications", "legal ramifications" }
                    },
                    new {
                        word = "expedite",
                        phonetic = "/ˈek.spə.daɪt/",
                        pos = "verb",
                        meaning = "xúc tiến, đẩy nhanh tiến độ",
                        example = "Simulations expedite molecular modeling.",
                        collocations = new[] { "expedite the process", "expedite delivery" }
                    },
                    new {
                        word = "therapeutic",
                        phonetic = "/ˌθer.əˈpjuː.tɪk/",
                        pos = "noun / adj",
                        meaning = "liệu pháp điều trị, mang tính trị liệu",
                        example = "Discover life-saving therapeutics at unprecedented velocity.",
                        collocations = new[] { "therapeutic approach", "therapeutic effect" }
                    }
                }),
                QuestionsJson = JsonSerializer.Serialize(new[]
                {
                    new {
                        question = "What is the primary difference between classical and quantum computing according to the text?",
                        options = new[] { "Quantum computers consume significantly more electricity", "Quantum computers process information using superposition instead of strictly binary bits", "Classical computers can solve pharmaceutical problems faster", "Quantum computers were invented in the 19th century" },
                        correctIndex = 1,
                        explanation = "Đoạn 1 và 2 giải thích máy tính cổ điển bị giới hạn ở bit nhị phân (0 hoặc 1), còn máy tính lượng tử khai thác sự chồng chập (superposition) để đánh giá đồng thời vô số hoán vị."
                    },
                    new {
                        question = "How does quantum computing benefit the pharmaceutical industry?",
                        options = new[] { "By manufacturing plastic bottles", "By training doctors through video games", "By speeding up molecular modeling to discover new therapeutics", "By replacing all human chemists" },
                        correctIndex = 2,
                        explanation = "Trong bài có câu: 'In pharmaceuticals, quantum simulations expedite molecular modeling to discover life-saving therapeutics at unprecedented velocity'."
                    }
                }),
                IsPublished = true,
                CreatedAt = DateTime.UtcNow
            },
            new Story
            {
                Title = "The Secret of the Willow Garden",
                Slug = "the-secret-of-the-willow-garden",
                Level = "A2",
                IeltsBand = "4.0 - 4.5",
                Category = "Phiêu lưu",
                Summary = "Một buổi chiều ấm áp, Lily và chú mèo Oliver bất ngờ tìm thấy chiếc chìa khóa cũ mở ra cánh cổng khu vườn bí mật.",
                ThumbnailUrl = "https://images.unsplash.com/photo-1585320806297-9794b3e4eeae?w=800&auto=format&fit=crop&q=80",
                WordCount = 175,
                EstimatedMinutes = 4,
                ContentJson = JsonSerializer.Serialize(new[]
                {
                    new {
                        en = "Behind grandfather's house, there was an ancient wooden fence covered with thick green ivy. Lily often played there with her mischievous cat, Oliver.",
                        vi = "Phía sau ngôi nhà của ông nội có một hàng rào gỗ cổ kính phủ đầy dây thường xuân xanh mướt. Lily thường chơi ở đó cùng chú mèo tinh nghịch Oliver."
                    },
                    new {
                        en = "One sunny afternoon, Oliver chased a bright butterfly and disappeared into a narrow gap in the fence. Lily called his name, but the cat did not return.",
                        vi = "Vào một buổi chiều đầy nắng, Oliver đuổi theo một chú bướm rực rỡ và biến mất vào khe hẹp của hàng rào. Lily gọi tên nó, nhưng chú mèo không quay lại."
                    },
                    new {
                        en = "She gently pushed the old planks aside and crawled through the opening. To her great surprise, she discovered a peaceful hidden garden full of colorful wild roses and singing birds.",
                        vi = "Cô bé nhẹ nhàng đẩy những thanh gỗ cũ sang một bên và bò qua lối mở. Thật ngạc nhiên, cô phát hiện ra một khu vườn bí mật yên bình tràn ngập hoa hồng dại rực rỡ và tiếng chim hót líu lo."
                    },
                    new {
                        en = "In the center of the garden, next to an old stone fountain, sat Oliver, purring softly. Near his paws lay a rusty bronze key half-buried in the soil.",
                        vi = "Ở trung tâm khu vườn, cạnh một đài phun nước bằng đá cũ, Oliver đang ngồi và kêu grừ grừ êm ái. Gần bàn chân nó là một chiếc chìa khóa đồng rỉ sét bị chôn vùi một nửa dưới đất."
                    },
                    new {
                        en = "Lily picked up the mysterious key with excitement. She knew that another wonderful adventure was waiting for them tomorrow.",
                        vi = "Lily háo hức nhặt chiếc chìa khóa bí ẩn lên. Cô biết rằng một chuyến phiêu lưu tuyệt vời khác đang chờ đợi họ vào ngày mai."
                    }
                }),
                VocabularyJson = JsonSerializer.Serialize(new[]
                {
                    new {
                        word = "ancient",
                        phonetic = "/ˈeɪn.ʃənt/",
                        pos = "adjective",
                        meaning = "cổ xưa, lâu đời",
                        example = "There was an ancient wooden fence.",
                        collocations = new[] { "ancient times", "ancient building" }
                    },
                    new {
                        word = "mischievous",
                        phonetic = "/ˈmɪs.tʃɪ.vəs/",
                        pos = "adjective",
                        meaning = "tinh nghịch, lém lỉnh",
                        example = "Lily played with her mischievous cat.",
                        collocations = new[] { "mischievous smile", "mischievous child" }
                    },
                    new {
                        word = "surprise",
                        phonetic = "/səˈpraɪz/",
                        pos = "noun / verb",
                        meaning = "sự ngạc nhiên, bất ngờ",
                        example = "To her great surprise, she discovered a garden.",
                        collocations = new[] { "pleasant surprise", "in surprise" }
                    },
                    new {
                        word = "mysterious",
                        phonetic = "/mɪˈstɪə.ri.əs/",
                        pos = "adjective",
                        meaning = "bí ẩn, khó hiểu",
                        example = "Lily picked up the mysterious key.",
                        collocations = new[] { "mysterious stranger", "mysterious sound" }
                    }
                }),
                QuestionsJson = JsonSerializer.Serialize(new[]
                {
                    new {
                        question = "Why did Oliver run into the fence?",
                        options = new[] { "He was scared of a dog", "He chased a bright butterfly", "He wanted to take a nap", "He heard a loud thunder" },
                        correctIndex = 1,
                        explanation = "Trong bài nêu: 'Oliver chased a bright butterfly and disappeared into a narrow gap in the fence'."
                    },
                    new {
                        question = "What did Lily find behind the wooden fence?",
                        options = new[] { "A busy highway", "A large swimming pool", "A peaceful hidden garden with flowers and birds", "A dark deep cave" },
                        correctIndex = 2,
                        explanation = "Lily đã tìm thấy một khu vườn bí mật ngập tràn hoa hồng dại và tiếng chim hót ('a peaceful hidden garden full of colorful wild roses')."
                    },
                    new {
                        question = "What object was found near Oliver's paws?",
                        options = new[] { "A rusty bronze key", "A gold coin", "A glass bottle", "A silver ring" },
                        correctIndex = 0,
                        explanation = "Gần bàn chân của Oliver là một chiếc chìa khóa đồng rỉ sét ('a rusty bronze key half-buried in the soil')."
                    }
                }),
                IsPublished = true,
                CreatedAt = DateTime.UtcNow
            },
            new Story
            {
                Title = "The Global Journey of a Coffee Bean",
                Slug = "the-global-journey-of-a-coffee-bean",
                Level = "B1",
                IeltsBand = "5.0 - 5.5",
                Category = "Xã hội & Văn hóa",
                Summary = "Khám phá chuỗi cung ứng kỳ diệu từ những đồi cà phê bạt ngàn tại cao nguyên nhiệt đới đến ly espresso thơm lừng khắp năm châu.",
                ThumbnailUrl = "https://images.unsplash.com/photo-1509042239860-f550ce710b93?w=800&auto=format&fit=crop&q=80",
                WordCount = 225,
                EstimatedMinutes = 4,
                ContentJson = JsonSerializer.Serialize(new[]
                {
                    new {
                        en = "Long before dawn breaks over the mist-covered volcanic hills of the Central Highlands, local farmers are already walking among rows of lush coffee shrubs. They carefully select only the ripest crimson cherries by hand.",
                        vi = "Trước khi bình minh ló rạng trên những ngọn đồi núi lửa phủ đầy sương của vùng Tây Nguyên, những người nông dân địa phương đã tản bộ giữa các hàng cây cà phê xanh mướt. Họ cẩn thận dùng tay hái chọn những trái cà phê chín đỏ mọng nhất."
                    },
                    new {
                        en = "Once harvested, the outer fruit pulp is washed away, leaving the precious raw beans to dry under the radiant tropical sun for days until they reach optimal moisture.",
                        vi = "Sau khi thu hoạch, lớp thịt quả bên ngoài được rửa sạch, để lại những hạt thô quý giá phơi dưới ánh nắng nhiệt đới rực rỡ trong nhiều ngày cho đến khi đạt độ ẩm tối ưu."
                    },
                    new {
                        en = "The dried beans are graded, packed into burlap sacks, and transported across oceans in international cargo vessels to specialty roasteries around the globe.",
                        vi = "Những hạt cà phê khô được phân loại, đóng vào bao tải gai và vận chuyển xuyên đại dương trên các tàu chở hàng quốc tế tới các xưởng rang xay chuyên dụng trên khắp toàn cầu."
                    },
                    new {
                        en = "Master roasters carefully calibrate heat and airflow to unlock rich aromas of chocolate, caramel, and floral notes. Finally, barista steam whips the dark extract into an invigorating cup that powers millions of morning routines.",
                        vi = "Các nghệ nhân rang xay chuyên nghiệp điều chỉnh nhiệt độ và luồng khí một cách tỉ mỉ để giải phóng những tầng hương phong phú của sô cô la, caramel và hoa quả. Cuối cùng, barista đánh bọt sữa và chiết xuất để tạo nên tách cà phê đậm đà tràn đầy năng lượng cho hàng triệu người mỗi sớm mai."
                    }
                }),
                VocabularyJson = JsonSerializer.Serialize(new[]
                {
                    new {
                        word = "harvest",
                        phonetic = "/ˈhɑː.vɪst/",
                        pos = "verb / noun",
                        meaning = "thu hoạch, vụ mùa",
                        example = "Once harvested, the outer fruit pulp is washed away.",
                        collocations = new[] { "harvest crops", "bountiful harvest" }
                    },
                    new {
                        word = "optimal",
                        phonetic = "/ˈɒp.tɪ.məl/",
                        pos = "adjective",
                        meaning = "tối ưu, tốt nhất",
                        example = "They dry until they reach optimal moisture.",
                        collocations = new[] { "optimal conditions", "optimal performance" }
                    },
                    new {
                        word = "calibrate",
                        phonetic = "/ˈkæl.ɪ.breɪt/",
                        pos = "verb",
                        meaning = "hiệu chỉnh, căn chỉnh chính xác",
                        example = "Roasters carefully calibrate heat and airflow.",
                        collocations = new[] { "carefully calibrate", "calibrate equipment" }
                    },
                    new {
                        word = "invigorating",
                        phonetic = "/ɪnˈvɪɡ.ər.eɪ.tɪŋ/",
                        pos = "adjective",
                        meaning = "làm hồi sức, tiếp thêm sinh lực",
                        example = "An invigorating cup that powers millions of routines.",
                        collocations = new[] { "invigorating breeze", "invigorating effect" }
                    }
                }),
                QuestionsJson = JsonSerializer.Serialize(new[]
                {
                    new {
                        question = "Which cherries do farmers handpick to ensure quality?",
                        options = new[] { "Only green cherries", "Only the ripest crimson cherries", "Fallen cherries on the ground", "Dried brown leaves" },
                        correctIndex = 1,
                        explanation = "Nông dân chỉ hái bằng tay những trái cà phê đỏ chín mọng nhất ('only the ripest crimson cherries by hand')."
                    },
                    new {
                        question = "What is the primary role of master roasters?",
                        options = new[] { "To water the trees", "To pack sacks onto ships", "To calibrate heat and airflow to develop rich flavor profiles", "To design coffee cup logos" },
                        correctIndex = 2,
                        explanation = "Thợ rang xay điều chỉnh nhiệt độ và luồng khí để giải phóng hương vị sô cô la, caramel ('calibrate heat and airflow to unlock rich aromas')."
                    },
                    new {
                        question = "How are the raw beans dried after pulp removal?",
                        options = new[] { "In microwave ovens", "Under the tropical sun", "In freezers", "Inside underground cellars" },
                        correctIndex = 1,
                        explanation = "Hạt được phơi dưới ánh nắng mặt trời nhiệt đới ('dry under the radiant tropical sun for days')."
                    }
                }),
                IsPublished = true,
                CreatedAt = DateTime.UtcNow
            },
            new Story
            {
                Title = "Smart Cities: Reimagining Urban Life",
                Slug = "smart-cities-reimagining-urban-life",
                Level = "B2",
                IeltsBand = "6.5 - 7.0",
                Category = "Công nghệ & Tương lai",
                Summary = "Cách các siêu đô thị hiện đại ứng dụng Internet vạn vật (IoT) và thuật toán AI để giảm khí thải, tối ưu giao thông và tiết kiệm năng lượng.",
                ThumbnailUrl = "https://images.unsplash.com/photo-1477959858617-67f30bc75b82?w=800&auto=format&fit=crop&q=80",
                WordCount = 285,
                EstimatedMinutes = 5,
                ContentJson = JsonSerializer.Serialize(new[]
                {
                    new {
                        en = "Rapid urbanization poses unprecedented ecological and infrastructure challenges across metropolitan centers worldwide. In response, municipal planners are turning toward digital innovation to engineer the next generation of sustainable smart cities.",
                        vi = "Quá trình đô thị hóa nhanh chóng đang đặt ra những thách thức chưa từng có về sinh thái và cơ sở hạ tầng tại các trung tâm siêu đô thị trên thế giới. Để đối phó, các nhà quy hoạch đô thị đang hướng tới đổi mới kỹ thuật số để thiết kế thế hệ thành phố thông minh bền vững tiếp theo."
                    },
                    new {
                        en = "Central to this transformation is an intricate network of Internet of Things (IoT) sensors embedded throughout public utilities. These sensors continuously monitor traffic flow, air quality index, and electrical grid consumption in real time.",
                        vi = "Trọng tâm của sự chuyển đổi này là một mạng lưới cảm biến Internet vạn vật (IoT) phức tạp được tích hợp trong toàn bộ các tiện ích công cộng. Những cảm biến này liên tục theo dõi lưu lượng giao thông, chỉ số chất lượng không khí và mức tiêu thụ lưới điện theo thời gian thực."
                    },
                    new {
                        en = "Machine learning algorithms analyze these massive data streams, automatically adjusting traffic signals to dissolve congestion and rerouting surplus solar power to neighborhoods experiencing peak demand.",
                        vi = "Các thuật toán học máy phân tích những luồng dữ liệu khổng lồ này, tự động điều chỉnh tín hiệu đèn giao thông để giải tỏa ùn tắc và điều hướng lượng điện mặt trời dư thừa tới các khu dân cư đang có nhu cầu cao điểm."
                    },
                    new {
                        en = "Furthermore, autonomous public transit vehicles synchronized with intelligent pedestrian pathways drastically reduce carbon footprints while enhancing civic livability for generations to come.",
                        vi = "Hơn thế nữa, các phương tiện giao thông công cộng tự hành được đồng bộ hóa với lối đi bộ thông minh giúp giảm đáng kể lượng khí thải carbon, đồng thời nâng cao chất lượng đáng sống cho cư dân trong nhiều thế hệ tương lai."
                    }
                }),
                VocabularyJson = JsonSerializer.Serialize(new[]
                {
                    new {
                        word = "urbanization",
                        phonetic = "/ˌɜː.bən.aɪˈzeɪ.ʃən/",
                        pos = "noun",
                        meaning = "đô thị hóa",
                        example = "Rapid urbanization poses unprecedented ecological challenges.",
                        collocations = new[] { "rapid urbanization", "urbanization process" }
                    },
                    new {
                        word = "infrastructure",
                        phonetic = "/ˈɪn.frəˌstrʌk.tʃər/",
                        pos = "noun",
                        meaning = "cơ sở hạ tầng",
                        example = "Sensors are embedded in public infrastructure.",
                        collocations = new[] { "critical infrastructure", "transport infrastructure" }
                    },
                    new {
                        word = "congestion",
                        phonetic = "/kənˈdʒes.tʃən/",
                        pos = "noun",
                        meaning = "sự tắc nghẽn, ùn ứ (giao thông)",
                        example = "Adjust signals to dissolve congestion.",
                        collocations = new[] { "traffic congestion", "ease congestion" }
                    },
                    new {
                        word = "autonomous",
                        phonetic = "/ɔːˈtɒn.ə.məs/",
                        pos = "adjective",
                        meaning = "tự hành, tự chủ",
                        example = "Autonomous public transit vehicles reduce emissions.",
                        collocations = new[] { "autonomous vehicle", "autonomous system" }
                    },
                    new {
                        word = "livability",
                        phonetic = "/ˌlɪv.əˈbɪl.ə.ti/",
                        pos = "noun",
                        meaning = "mức độ đáng sống, chất lượng sống",
                        example = "Enhancing civic livability for generations to come.",
                        collocations = new[] { "urban livability", "livability index" }
                    }
                }),
                QuestionsJson = JsonSerializer.Serialize(new[]
                {
                    new {
                        question = "What is the primary function of IoT sensors in smart cities?",
                        options = new[] { "To sell commercial advertisements", "To track real-time traffic, air quality, and grid power usage", "To replace traffic police completely", "To build underground parking spaces" },
                        correctIndex = 1,
                        explanation = "Cảm biến IoT liên tục giám sát lưu lượng giao thông, chất lượng không khí và điện năng ('monitor traffic flow, air quality index, and electrical grid consumption in real time')."
                    },
                    new {
                        question = "How do machine learning algorithms help manage clean energy?",
                        options = new[] { "By turning off electricity during peak hours", "By rerouting surplus solar power to areas with high demand", "By banning fossil fuel vehicles completely", "By building coal plants" },
                        correctIndex = 1,
                        explanation = "Thuật toán tự động điều phối điện mặt trời dư thừa tới các khu vực có nhu cầu tiêu thụ cao điểm ('rerouting surplus solar power to neighborhoods experiencing peak demand')."
                    },
                    new {
                        question = "What environmental benefit is mentioned regarding autonomous transit?",
                        options = new[] { "Drastic reduction in carbon footprint", "Complete elimination of bicycles", "Free electricity for all residents", "Faster highway speed limits" },
                        correctIndex = 0,
                        explanation = "Phương tiện công cộng tự hành giúp giảm mạnh phát thải carbon ('drastically reduce carbon footprints')."
                    }
                }),
                IsPublished = true,
                CreatedAt = DateTime.UtcNow
            },
            new Story
            {
                Title = "The Architecture of Human Consciousness",
                Slug = "the-architecture-of-human-consciousness",
                Level = "C2",
                IeltsBand = "8.5 - 9.0",
                Category = "Khoa học & Tự nhiên",
                Summary = "Một bài luận học thuật kinh điển về 'Vấn đề nan giải của Ý thức' (The Hard Problem of Consciousness), giao thoa giữa thần kinh học và triết học hiện đại.",
                ThumbnailUrl = "https://images.unsplash.com/photo-1507413245164-6160d8298b31?w=800&auto=format&fit=crop&q=80",
                WordCount = 330,
                EstimatedMinutes = 6,
                ContentJson = JsonSerializer.Serialize(new[]
                {
                    new {
                        en = "For centuries, the enigmatic genesis of subjective awareness has stood as an insurmountable bastion defying reductive neurobiological explanations. Known colloquially as 'the hard problem of consciousness', it probes why electrochemical impulses through neural synapses give rise to qualitative perceptual experiences—the rich redness of a rose or the poignant sting of nostalgia.",
                        vi = "Trong nhiều thế kỷ, nguồn gốc bí ẩn của nhận thức chủ quan đã sừng sững như một thành trì bất khả xâm phạm thách thức các giải thích giản lược mang tính sinh học thần kinh. Thường được gọi là 'vấn đề nan giải của ý thức', nó truy vấn lý do tại sao các xung điện hóa qua các khớp thần kinh synapse lại tạo ra các trải nghiệm cảm giác định tính—sắc đỏ rực rỡ của một đóa hồng hay cảm giác nhói lòng đầy hoài niệm."
                    },
                    new {
                        en = "Contemporary cognitive theorists remain divided along entrenched ontological boundaries. Physicalists postulate that phenomenal consciousness is merely an emergent epiphenomenon, arising inevitably once computational complexity surpasses a deterministic threshold within cerebral circuitry.",
                        vi = "Các nhà lý thuyết nhận thức đương đại vẫn bị chia rẽ sâu sắc theo các ranh giới bản thể học cố hữu. Phái duy vật mặc định rằng ý thức cảm tính chỉ đơn thuần là một hiện tượng phụ phát sinh, tất yếu xuất hiện khi độ phức tạp tính toán vượt qua một ngưỡng tiền định bên trong các mạch não bộ."
                    },
                    new {
                        en = "Conversely, panpsychists advocate that consciousness is not an incidental byproduct of evolutionary mechanics, but rather an intrinsic, immutable fabric of spacetime itself, akin to mass or electromagnetic charge.",
                        vi = "Ngược lại, các nhà thuyết phiếm tâm cho rằng ý thức không phải là một sản phẩm phụ ngẫu nhiên của các cơ chế tiến hóa, mà là một cấu trúc nội tại, bất biến của chính không-thời gian, tương tự như khối lượng hay điện tích."
                    },
                    new {
                        en = "As quantum decoherence experiments advance and neuroimaging attains sub-millisecond fidelity, science edges closer to elucidating this ultimate frontier of the human condition.",
                        vi = "Khi các thí nghiệm về sự mất kết hợp lượng tử tiến bộ và kỹ thuật chẩn đoán hình ảnh thần kinh đạt được độ chính xác dưới một phần nghìn giây, khoa học đang dần tiệm cận việc làm sáng tỏ ranh giới tối thượng này của thân phận con người."
                    }
                }),
                VocabularyJson = JsonSerializer.Serialize(new[]
                {
                    new {
                        word = "enigmatic",
                        phonetic = "/ˌen.ɪɡˈmæt.ɪk/",
                        pos = "adjective",
                        meaning = "bí ẩn, khó hiểu, đầy câu đố",
                        example = "The enigmatic genesis of subjective awareness.",
                        collocations = new[] { "enigmatic smile", "enigmatic figure" }
                    },
                    new {
                        word = "insurmountable",
                        phonetic = "/ˌɪn.səˈmaʊn.tə.bəl/",
                        pos = "adjective",
                        meaning = "không thể vượt qua, nan giải",
                        example = "Stood as an insurmountable bastion.",
                        collocations = new[] { "insurmountable obstacle", "insurmountable difficulty" }
                    },
                    new {
                        word = "epiphenomenon",
                        phonetic = "/ˌep.ɪ.fɪˈnɒm.ɪ.nən/",
                        pos = "noun",
                        meaning = "hiện tượng phụ sinh, hệ quả kèm theo",
                        example = "Consciousness is merely an emergent epiphenomenon.",
                        collocations = new[] { "mere epiphenomenon", "biological epiphenomenon" }
                    },
                    new {
                        word = "intrinsic",
                        phonetic = "/ɪnˈtrɪn.zɪk/",
                        pos = "adjective",
                        meaning = "nội tại, vốn có",
                        example = "An intrinsic, immutable fabric of spacetime.",
                        collocations = new[] { "intrinsic value", "intrinsic quality" }
                    },
                    new {
                        word = "elucidate",
                        phonetic = "/iˈluː.sɪ.deɪt/",
                        pos = "verb",
                        meaning = "làm sáng tỏ, giải thích tường tận",
                        example = "Edges closer to elucidating this ultimate frontier.",
                        collocations = new[] { "elucidate the mystery", "elucidate mechanisms" }
                    }
                }),
                QuestionsJson = JsonSerializer.Serialize(new[]
                {
                    new {
                        question = "What does the 'hard problem of consciousness' primarily investigate?",
                        options = new[] { "How sleep deprivation affects brain reflexes", "Why electrochemical brain signals translate into subjective, qualitative experiences", "How artificial intelligence will replace human labor", "The mathematical equations of brain mass" },
                        correctIndex = 1,
                        explanation = "Vấn đề nan giải điều tra lý do tại sao các xung điện hóa qua khớp thần kinh lại tạo ra trải nghiệm chủ quan định tính ('why electrochemical impulses through neural synapses give rise to qualitative perceptual experiences')."
                    },
                    new {
                        question = "According to panpsychism, what is consciousness?",
                        options = new[] { "An accidental side effect of computer software", "A human illusion fabricated by social language", "A fundamental and intrinsic property of the universe, like mass or charge", "A medical disease curable by pharmaceuticals" },
                        correctIndex = 2,
                        explanation = "Thuyết phiếm tâm cho rằng ý thức là một cấu trúc nội tại bất biến của không-thời gian, giống như khối lượng hay điện tích ('an intrinsic, immutable fabric of spacetime itself, akin to mass or electromagnetic charge')."
                    },
                    new {
                        question = "What technological advancements are bringing science closer to understanding consciousness?",
                        options = new[] { "Steam engine locomotives and telegraphs", "Sub-millisecond neuroimaging and quantum experiments", "Standard laboratory blood tests", "Satellite weather forecasting" },
                        correctIndex = 1,
                        explanation = "Các thí nghiệm mất kết hợp lượng tử và kỹ thuật chụp ảnh thần kinh có độ chuẩn xác dưới một phần nghìn giây ('sub-millisecond fidelity neuroimaging')."
                    }
                }),
                IsPublished = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        bool hasNewStories = false;
        foreach (var s in seedStories)
        {
            if (!dbContext.Stories.Any(existing => existing.Slug == s.Slug))
            {
                dbContext.Stories.Add(s);
                hasNewStories = true;
            }
        }
        if (hasNewStories)
        {
            await dbContext.SaveChangesAsync();
        }

        // Upload any stories missing JsonUrl to Cloudflare R2 in dedicated folder "stories/"
        if (r2Storage != null)
        {
            var unuploadedStories = dbContext.Stories.Where(s => string.IsNullOrEmpty(s.JsonUrl)).ToList();
            foreach (var s in unuploadedStories)
            {
                try
                {
                    var exportObj = new
                    {
                        title = s.Title,
                        level = s.Level,
                        ieltsBand = s.IeltsBand,
                        category = s.Category,
                        summary = s.Summary,
                        thumbnailUrl = s.ThumbnailUrl,
                        audioUrl = s.AudioUrl,
                        wordCount = s.WordCount,
                        estimatedMinutes = s.EstimatedMinutes,
                        paragraphs = !string.IsNullOrEmpty(s.ContentJson) ? JsonSerializer.Deserialize<JsonElement>(s.ContentJson) : default,
                        targetVocabulary = !string.IsNullOrEmpty(s.VocabularyJson) ? JsonSerializer.Deserialize<JsonElement>(s.VocabularyJson) : default,
                        questions = !string.IsNullOrEmpty(s.QuestionsJson) ? JsonSerializer.Deserialize<JsonElement>(s.QuestionsJson) : default
                    };

                    var jsonString = JsonSerializer.Serialize(exportObj, new JsonSerializerOptions { WriteIndented = true });
                    var jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonString);
                    using var ms = new MemoryStream(jsonBytes);
                    
                    var r2Url = await r2Storage.UploadFileAsync(ms, $"stories/{s.Slug}.json", "application/json");
                    s.JsonUrl = r2Url;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[StorySeedData] Error uploading seed story '{s.Slug}' to R2: {ex.Message}");
                }
            }

            if (unuploadedStories.Any(s => !string.IsNullOrEmpty(s.JsonUrl)))
            {
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
