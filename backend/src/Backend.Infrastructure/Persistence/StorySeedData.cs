using Backend.Domain.Entities;
using System.Text.Json;

namespace Backend.Infrastructure.Persistence;

public static class StorySeedData
{
    public static async Task SeedStoriesAsync(AppDbContext dbContext)
    {
        if (dbContext.Stories.Any()) return;

        var stories = new List<Story>
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
            }
        };

        dbContext.Stories.AddRange(stories);
        await dbContext.SaveChangesAsync();
    }
}
