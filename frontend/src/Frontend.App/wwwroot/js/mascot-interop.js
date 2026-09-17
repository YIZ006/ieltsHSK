/**
 * Mascot Interop - IELTS & HSK Companion
 * Based on page-mascot sprite mechanics (3x3 grid)
 */
window.MascotManager = (function () {
    const DIRECTIONS = [
        'up-left', 'up', 'up-right',
        'left', 'center', 'right',
        'down-left', 'down', 'down-right'
    ];

    const REACTIONS = [
        'blink', 'heart', 'sparkle',
        'surprised', 'wink', 'bashful',
        'sleepy', 'dizzy', 'delighted'
    ];

    const CLOCKWISE = [
        'right', 'down-right', 'down', 'down-left',
        'left', 'up-left', 'up', 'up-right'
    ];

    const SECTOR = (Math.PI * 2) / CLOCKWISE.length;
    const HYSTERESIS = 0.12;
    const DEAD_ZONE = 65;
    const PAYOFFS = ['heart', 'sparkle', 'delighted', 'wink', 'bashful'];
    const BOOP_PAYOFF = 120;
    const BOOP_END = 600;
    const SQUASH_MS = 420;
    const DIZZY_AFTER = 4;
    const DIZZY_WINDOW = 1600;
    const DIZZY_END = 1200;

    const SQUASH_KEYFRAMES = [
        { transform: 'scale(1, 1)', easing: 'ease-in' },
        { transform: 'scale(1.12, 0.84)', offset: 0.18, easing: 'ease-out' },
        { transform: 'scale(0.94, 1.08)', offset: 0.45, easing: 'ease-in-out' },
        { transform: 'scale(1.03, 0.97)', offset: 0.72, easing: 'ease-in-out' },
        { transform: 'scale(1, 1)' }
    ];

    const STUDY_QUOTES = [
        { text: "1% better every day! 💪", sub: "Mỗi ngày tiến bộ 1%!" },
        { text: "Practice makes progress! ✨", sub: "Luyện tập mỗi ngày nào!" },
        { text: "Keep going, you're doing great! 🌟", sub: "Cố lên bạn ơi, giỏi lắm!" },
        { text: "Mastering IELTS step by step! 📚", sub: "Chinh phục IELTS từng bước một!" },
        { text: "加油！天天向上！ 🐼", sub: "Cố lên! Ngày càng tiến bộ!" },
        { text: "千里之行，始于足下。 🐾", sub: "Hành trình vạn dặm khởi đầu từ một bước chân!" },
        { text: "坚持就是胜利！ 🔥", sub: "Kiên trì ắt sẽ thành công!" },
        { text: "Consistency is your superpower! 🚀", sub: "Sự kiên định là siêu năng lực!" },
        { text: "Đừng quên ôn từ vựng hôm nay nhé! 📖", sub: "IELTS & HSK Companion" },
        { text: "Nhấp vào mình để xả stress nè! ❤️", sub: "Boop me for good luck!" },
        { text: "Nhấp nhanh 4 lần để mình xoay vòng nha! 😵", sub: "Dizzy easter egg!" }
    ];

    const CHARACTERS = {
        wolf: {
            id: 'wolf',
            name: 'Sói Tuyết & Ma Nhỏ',
            icon: '🐺',
            directions: '/mascot/wolf-directions.webp',
            reactions: '/mascot/wolf-reactions.webp'
        }
    };

    const GAME_QUOTES = {
        start: [
            { text: "Sẵn sàng xuất kích! ✈️", sub: "Bắn rụng từ vựng nào!", reaction: "wink" },
            { text: "Cố lên bạn ơi! 🚀", sub: "HSK Shooter bắt đầu!", reaction: "delighted" }
        ],
        hit: [
            { text: "Bắn chuẩn lắm! 🎯", sub: "Trúng mục tiêu!", reaction: "delighted" },
            { text: "Tuyệt đỉnh! ✨", sub: "+10 điểm!", reaction: "sparkle" },
            { text: "Quá xuất sắc! 💥", sub: "Gõ nhanh như chớp!", reaction: "delighted" },
            { text: "Chuẩn không cần chỉnh! 🚀", sub: "Tiếp tục phát huy!", reaction: "wink" }
        ],
        combo: [
            { text: "Combo bốc lửa! 🔥", sub: "Thừa thắng xông lên!", reaction: "sparkle" },
            { text: "Xạ thủ thần sầu! ⚡", sub: "Gõ pinyin đỉnh cao!", reaction: "delighted" },
            { text: "Quá nhanh quá nguy hiểm! 🌟", sub: "Giữ vững chuỗi combo!", reaction: "sparkle" }
        ],
        high_combo: [
            { text: "SIÊU COMBO! ⚡🔥", sub: "Không thể ngăn cản!", reaction: "heart" },
            { text: "Huyền thoại xạ thủ! 👑", sub: "Tốc độ bàn thờ!", reaction: "sparkle" },
            { text: "Bất khả chiến bại! 🚀", sub: "Đỉnh chóp bạn ơi!", reaction: "heart" }
        ],
        damage: [
            { text: "Úi! Cẩn thận né đòn nhé! 🛡️", sub: "Bình tĩnh tập trung nào!", reaction: "surprised" },
            { text: "Không sao, còn mạng mà! ❤️", sub: "Lấy lại nhịp độ!", reaction: "bashful" },
            { text: "Hít thở sâu, ngắm kỹ lại! 🎯", sub: "Cố lên bạn ơi!", reaction: "surprised" }
        ],
        boss: [
            { text: "CẢNH BÁO: BOSS xuất hiện! 👑", sub: "Tập trung cao độ!", reaction: "surprised" },
            { text: "Boss tung chiêu kìa! Coi chừng! ⚡", sub: "Né đòn và bắn nhanh!", reaction: "surprised" },
            { text: "Đánh bại Boss nào! 💥", sub: "Quyết tâm phá đảo!", reaction: "wink" }
        ],
        stage_clear: [
            { text: "Phá đảo tầng này rồi! 🏆", sub: "Quá xuất sắc bạn ơi!", reaction: "delighted" },
            { text: "Thắng lợi giòn giã! 🎉", sub: "Tiến lên tầng tiếp theo!", reaction: "sparkle" }
        ],
        game_over: [
            { text: "Đừng nản nhé! Nghỉ tay rồi phục thù! 💪", sub: "Luyện thêm là thắng!", reaction: "bashful" },
            { text: "Lần sau nhất định phá kỷ lục! 🐺", sub: "Cố lên bạn ơi!", reaction: "wink" }
        ],
        win: [
            { text: "Chiến thắng vẻ vang! 🌟", sub: "Trí tuệ đỉnh cao!", reaction: "heart" },
            { text: "Đoán chuẩn xác 100%! 🏆", sub: "Chúc mừng bạn!", reaction: "delighted" }
        ]
    };

    const PAGE_GUIDANCE = {
        home: [
            { text: "Chào mừng bạn trở lại! 🐺✨", sub: "Hôm nay bạn muốn luyện IELTS, HSK hay TOEIC nào?", reaction: "delighted" },
            { text: "Mỗi ngày 15 phút là đủ tạo kỳ tích! 🚀", sub: "Chọn một kỹ năng và bắt đầu ngay thôi bạn ơi!", reaction: "wink" },
            { text: "Duy trì chuỗi học tập (Streak) nhé! 🔥", sub: "Sói Tuyết luôn đồng hành tiếp lửa cùng bạn!", reaction: "sparkle" },
            { text: "Cần trợ giúp gì, cứ bấm vào mình nha! ❤️", sub: "Mình sẽ mách nước cho bạn rất nhiều mẹo hay!", reaction: "heart" }
        ],
        ielts_dashboard: [
            { text: "Tổng quan hành trình IELTS của bạn! 📊", sub: "Xem lại tiến độ các kỹ năng để phân bổ thời gian hợp lý nhé!", reaction: "delighted" },
            { text: "Cân bằng cả 4 kỹ năng Nghe - Đọc - Viết - Nói nha! 🎯", sub: "Kỹ năng nào còn yếu thì ưu tiên luyện trước bạn nhé!", reaction: "wink" },
            { text: "Mục tiêu band điểm sắp nằm trong tay bạn rồi! 🌟", sub: "Kiên trì mỗi ngày, band 7.0+ không còn xa!", reaction: "sparkle" },
            { text: "Khám phá các tính năng luyện thi bên menu nhé! 🧭", sub: "Có cả luyện đề full, đọc truyện chêm và nhại giọng!", reaction: "heart" }
        ],
        ielts_mock_test: [
            { text: "Phòng thi thử IELTS chuẩn format Cambridge! 📝", sub: "Lựa chọn bộ đề thi thử phù hợp để đo thực lực nhé!", reaction: "delighted" },
            { text: "Mẹo nhỏ: Bật đồng hồ bấm giờ để quen áp lực thi! ⏱️", sub: "Không tra từ điển trong lúc làm đề để có kết quả chính xác nhất.", reaction: "wink" },
            { text: "Tập trung tối đa và hít thở thật sâu nào! 🧘‍♂️", sub: "Bạn đã ôn luyện rất kỹ, hãy tự tin vào chính mình!", reaction: "sparkle" },
            { text: "Làm xong nhớ bấm 'Xem đáp án' để đối chiếu nhé! 🔍", sub: "Học từ lỗi sai là cách tăng band điểm nhanh nhất!", reaction: "heart" }
        ],
        ielts_exam_room: [
            { text: "Tập trung cao độ nào! Chúc bạn làm bài thật tốt! 🎯", sub: "Đọc kỹ yêu cầu đề bài và giới hạn số từ (Word limit) nhé!", reaction: "wink" },
            { text: "Quản lý thời gian thật chặt chẽ bạn nhé! ⏳", sub: "Đừng dừng lại quá lâu ở câu khó, đánh dấu rồi làm câu sau trước!", reaction: "sparkle" },
            { text: "Giữ tinh thần bình tĩnh, tự tin làm chủ bài thi! 🐺✨", sub: "Sói Tuyết đang cổ vũ bạn hết mình đây!", reaction: "delighted" }
        ],
        ielts_review: [
            { text: "Góc phân tích và chữa đề chi tiết! 🧐", sub: "Xem kỹ phần giải thích đáp án và các bẫy thường gặp nhé!", reaction: "delighted" },
            { text: "Ghi chú từ mới và cấu trúc hay vào sổ tay nha! ✍️", sub: "Rút kinh nghiệm từ câu sai để lần thi sau chuẩn xác hơn!", reaction: "sparkle" },
            { text: "Điểm số là cột mốc, quan trọng là kiến thức đọng lại! 🌟", sub: "Cố lên, mỗi lần chữa đề là một bước tiến vượt bậc!", reaction: "heart" }
        ],
        ielts_stories: [
            { text: "Thư viện truyện chêm IELTS song ngữ! 📚", sub: "Phương pháp nạp từ vựng tự nhiên qua ngữ cảnh cực kỳ dễ nhớ!", reaction: "delighted" },
            { text: "Hướng dẫn: Bấm vào từ highlight để tra nghĩa tức thì! 💡", sub: "Có cả phiên âm IPA, loại từ và câu ví dụ minh họa chi tiết!", reaction: "wink" },
            { text: "Đọc xong nhớ làm bài kiểm tra trắc nghiệm cuối bài nhé! 📝", sub: "Vừa thư giãn đọc truyện, vừa khắc sâu từ vựng vào trí nhớ!", reaction: "sparkle" },
            { text: "Bạn có thể lọc truyện theo cấp độ A1, A2, B1, B2 tùy thích! 🔍", sub: "Bắt đầu từ truyện vừa sức rồi nâng dần độ khó nhé!", reaction: "heart" }
        ],
        ielts_vocab: [
            { text: "Kho từ vựng IELTS theo chủ đề học thuật! 🧠", sub: "Học từ vựng kết hợp Collocation và ngữ cảnh thực tế nhé!", reaction: "delighted" },
            { text: "Mẹo học: Dùng Flashcard lật mở và tự gõ lại từ! 🔄", sub: "Hệ thống lặp lại ngắt quãng sẽ giúp bạn ghi nhớ siêu lâu!", reaction: "wink" },
            { text: "Mỗi ngày 10-15 từ chất lượng hơn là nhồi nhét quá nhiều! 💎", sub: "Đặt câu với từ mới để biến từ vựng thành của riêng bạn nha!", reaction: "sparkle" }
        ],
        ielts_listen_fill: [
            { text: "Luyện nghe chép chính tả (Dictation) chuyên sâu! 🎧", sub: "Phương pháp vàng để cải thiện phản xạ bắt âm và nối từ!", reaction: "delighted" },
            { text: "Hướng dẫn: Nghe từng câu, gõ lại chính xác từ còn thiếu! ⌨️", sub: "Có thể chỉnh tốc độ 0.75x nếu người bản xứ nói nhanh nhé!", reaction: "wink" },
            { text: "Chú ý âm đuôi (ending sounds) và mạo từ 'a/an/the' nha! 👂", sub: "Bắt trọn từng chi tiết nhỏ sẽ giúp band Listening bứt phá!", reaction: "sparkle" }
        ],
        ielts_speak_along: [
            { text: "Phòng luyện nói nhại giọng (Shadowing Technique)! 🎙️", sub: "Nghe câu mẫu của người bản xứ và nói đè theo thật chuẩn nào!", reaction: "delighted" },
            { text: "Chú ý ngữ điệu (intonation) và ngắt nghỉ đúng chỗ nhé! 🎶", sub: "Nói to, rõ ràng và tự tin, phát âm sẽ chuẩn tự nhiên hơn!", reaction: "sparkle" },
            { text: "Luyện lặp lại 3-5 lần mỗi câu cho thật lưu loát! 🗣️", sub: "Cơ miệng quen dần thì Speaking sẽ tự nhiên như tiếng mẹ đẻ!", reaction: "wink" }
        ],
        ielts_grammar: [
            { text: "Hệ thống ngữ pháp trọng điểm cho IELTS! 📐", sub: "Nắm chắc cấu trúc câu phức, mệnh đề quan hệ và đảo ngữ!", reaction: "delighted" },
            { text: "Ngữ pháp chính xác là chìa khóa đạt band 7+ Writing & Speaking! 🔑", sub: "Đọc lý thuyết xong nhớ làm ngay bài tập ứng dụng nhé!", reaction: "wink" },
            { text: "Đừng chỉ học thuộc vẹt công thức, hãy hiểu bản chất dùng câu! 💡", sub: "Áp dụng ngữ pháp vào bài viết để ghi điểm tiêu chí Grammatical Range!", reaction: "sparkle" }
        ],
        ielts_priority_review: [
            { text: "Trung tâm ôn tập ưu tiên (Spaced Repetition)! ⏰", sub: "Đây là các từ vựng và câu hỏi bạn sắp quên hoặc từng làm sai!", reaction: "delighted" },
            { text: "Ôn đúng 'thời điểm vàng' giúp lưu giữ kiến thức trọn đời! 🧠✨", sub: "Giải quyết hết các thẻ đến hạn hôm nay để giữ phong độ nhé!", reaction: "wink" },
            { text: "Tập trung dứt điểm danh sách này, hiệu quả tăng gấp đôi! 🚀", sub: "Cố lên, vượt qua các câu hóc búa này là bạn lên trình ngay!", reaction: "sparkle" }
        ],
        hsk_dashboard: [
            { text: "Chào mừng đến với vũ trụ Tiếng Trung HSK! 🇨🇳🐼", sub: "Khám phá từ vựng, ngữ pháp và các đề thi mô phỏng chuẩn!", reaction: "delighted" },
            { text: "HSK 1 đến HSK 6, bạn đang hướng tới cấp độ nào? 🎯", sub: "Vững từng nét chữ và thanh điệu, tiếng Trung sẽ rất thú vị!", reaction: "sparkle" },
            { text: "好好学习，天天向上！ 🌸", sub: "Học tập chăm chỉ, mỗi ngày đều tiến bộ vượt bậc!", reaction: "heart" }
        ],
        hsk_vocab: [
            { text: "Kho từ vựng HSK toàn diện có kèm Pinyin & Hán tự! 📖", sub: "Bấm vào từng từ để nghe phát âm chuẩn giọng phổ thông nhé!", reaction: "delighted" },
            { text: "Mẹo nhớ chữ Hán: Nhìn bộ thủ và liên tưởng hình ảnh! 🧩", sub: "Hiểu ý nghĩa bộ thủ sẽ giúp bạn đoán nghĩa và nhớ mặt chữ cực lâu!", reaction: "wink" },
            { text: "Ôn từ vựng kết hợp gõ pinyin trong trò chơi Bắn máy bay nhé! 🎮", sub: "Vừa giải trí vừa rèn phản xạ gõ pinyin nhanh như gió!", reaction: "sparkle" }
        ],
        hsk_mock_tests: [
            { text: "Luyện đề thi HSK mô phỏng sát với đề thật! 📜", sub: "Kiểm tra toàn diện kỹ năng Nghe (听力), Đọc (阅读) và Viết (书写)!", reaction: "delighted" },
            { text: "Lưu ý phần nghe HSK chỉ phát 1 lần ở cấp cao! 🎧", sub: "Đọc lướt nhanh câu hỏi và phương án trước khi băng phát nhé!", reaction: "wink" },
            { text: "Tự tin vượt ải HSK với điểm số thật cao nào! 💯", sub: "祝你考试顺利！(Chúc bạn thi cử thuận lợi, đỗ đạt cao!)", reaction: "heart" }
        ],
        games: [
            { text: "Khu trò chơi luyện phản xạ từ vựng siêu cuốn! 🎮👾", sub: "Học mà chơi, chơi mà học - cách nạp từ không hề nhàm chán!", reaction: "delighted" },
            { text: "Vocab Shooter: Gõ đúng pinyin để tiêu diệt tàu địch! 🚀💥", sub: "Duy trì chuỗi combo để nhân đôi nhân ba điểm số nhé!", reaction: "wink" },
            { text: "Sói Tuyết đồng hành làm Co-pilot bắn hạ mọi quái vật! 🐺✈️", sub: "Ngón tay đặt sẵn trên bàn phím và sẵn sàng xuất kích!", reaction: "sparkle" }
        ],
        toeic: [
            { text: "Góc luyện thi TOEIC Listening & Reading cấp tốc! 💼🎯", sub: "Bộ câu hỏi sát thực tế môi trường công sở quốc tế!", reaction: "delighted" },
            { text: "Mẹo Part 5 & 6: Nhận diện nhanh từ loại trong 10-15 giây! ⏱️", sub: "Dành thời gian quý báu để đọc hiểu kỹ các đoạn văn dài ở Part 7!", reaction: "wink" },
            { text: "Chinh phục mốc điểm 750+ TOEIC để tự tin hội nhập! 🚀", sub: "Sói Tuyết luôn tiếp sức cho bạn trên từng chặng đường!", reaction: "sparkle" }
        ],
        profile: [
            { text: "Hồ sơ cá nhân và bảng thành tích học tập! 🏆👤", sub: "Xem lại chuỗi ngày học liên tục (Streak) và các huy hiệu đạt được nhé!", reaction: "delighted" },
            { text: "Nhìn lại chặng đường bạn đã vượt qua, thật đáng khen ngợi! 🌟", sub: "Hãy tự hào về sự nỗ lực không ngừng nghỉ của bản thân!", reaction: "heart" },
            { text: "Đặt ra mục tiêu mới cho tuần này và cùng mình chinh phục nhé! 🎯", sub: "Kỷ luật và kiên trì sẽ đưa bạn đến bất cứ đâu bạn muốn!", reaction: "sparkle" }
        ],
        admin: [
            { text: "Bảng điều khiển Quản trị hệ thống (Portal Hub)! 🛠️⚙️", sub: "Quản lý dữ liệu từ vựng, đề thi, truyện chêm và người dùng.", reaction: "delighted" },
            { text: "Chào Admin! Chúc bạn một ngày làm việc và quản trị hiệu quả! 💼", sub: "Mọi hệ thống học tập đang vận hành rất trơn tru và mượt mà!", reaction: "wink" }
        ]
    };

    let state = {
        character: 'wolf',
        size: 110,
        minimized: false,
        showBubble: true,
        soundEnabled: true,
        direction: 'center',
        reaction: null,
        quoteIndex: 0,
        isGameMode: false,
        gameName: '',
        preGameSize: 110,
        currentCategory: 'home',
        lastGuideIdx: -1
    };

    let elements = {
        container: null,
        mascotBtn: null,
        squashSpan: null,
        dirLayer: null,
        reactLayer: null,
        bubble: null,
        bubbleText: null,
        bubbleSub: null
    };

    let timers = [];
    let boops = { count: 0, at: 0, lastClickTime: 0 };
    let pointer = null;
    let quoteInterval = null;
    let idleInterval = null;
    let reactionTimeout = null;
    let lastUserActivity = Date.now();
    let isInitialized = false;
    let lastGameSayTime = 0;
    let _audioCtx = null;

    function getAudioContext() {
        if (!_audioCtx) {
            const AudioCtxClass = window.AudioContext || window.webkitAudioContext;
            if (AudioCtxClass) {
                _audioCtx = new AudioCtxClass();
            }
        }
        if (_audioCtx && _audioCtx.state === 'suspended') {
            _audioCtx.resume().catch(() => {});
        }
        return _audioCtx;
    }

    /**
     * Âm thanh giọt nước nhỏ nhẹ, trong trẻo (Pure Water Droplet Sound)
     * Tổng hợp trực tiếp qua Web Audio API:
     * - Sóng sine với pitch sweep vút lên nhanh mô phỏng bọt khí giọt nước (780Hz -> 1620Hz)
     * - Âm lượng nhẹ nhàng êm ái (0.11), không bị chói gắt
     * - Tiếng tí tách thứ hai cực nhỏ sau 46ms tạo chiều sâu không gian
     */
    function playWaterDropSfx(pitchMod = 1.0) {
        if (!state.soundEnabled) return;
        try {
            const ctx = getAudioContext();
            if (!ctx) return;

            // Nếu audio context đang suspended do trình duyệt yêu cầu tương tác, gọi resume trước khi phát
            if (ctx.state === 'suspended') {
                ctx.resume().then(() => {
                    playWaterDropSfx(pitchMod);
                }).catch(() => {});
                return;
            }

            const now = ctx.currentTime;

            // 1. Giọt nước chính (Primary Bubble Plop: 880Hz -> 1760Hz vút nhanh theo hàm mũ)
            const osc1 = ctx.createOscillator();
            const gain1 = ctx.createGain();

            osc1.type = 'sine';
            const f1Start = 880 * pitchMod;
            const f1End = 1760 * pitchMod;
            osc1.frequency.setValueAtTime(f1Start, now);
            osc1.frequency.exponentialRampToValueAtTime(f1End, now + 0.045);

            // Âm lượng vừa phải, rõ ràng (0.28) nhưng mềm mại, không chói tai
            gain1.gain.setValueAtTime(0.001, now);
            gain1.gain.linearRampToValueAtTime(0.28, now + 0.005);
            gain1.gain.exponentialRampToValueAtTime(0.0001, now + 0.14);

            osc1.connect(gain1);
            gain1.connect(ctx.destination);

            osc1.start(now);
            osc1.stop(now + 0.15);

            // 2. Tiếng tí tách thứ hai sau 48ms (Secondary Micro-droplet: 1600Hz -> 2400Hz)
            const osc2 = ctx.createOscillator();
            const gain2 = ctx.createGain();
            const t2 = now + 0.048;

            const f2Start = 1600 * pitchMod;
            const f2End = 2400 * pitchMod;
            osc2.type = 'sine';
            osc2.frequency.setValueAtTime(f2Start, t2);
            osc2.frequency.exponentialRampToValueAtTime(f2End, t2 + 0.035);

            gain2.gain.setValueAtTime(0.001, t2);
            gain2.gain.linearRampToValueAtTime(0.09, t2 + 0.004);
            gain2.gain.exponentialRampToValueAtTime(0.0001, t2 + 0.09);

            osc2.connect(gain2);
            gain2.connect(ctx.destination);

            osc2.start(t2);
            osc2.stop(t2 + 0.10);
        } catch (e) {
            // Không ngắt luồng giao diện nếu audio context bị hạn chế
        }
    }

    function getLiveElements() {
        if (elements.container && elements.container.isConnected) {
            return elements;
        }
        const container = document.getElementById('mascotCompanionWidget');
        if (!container) return elements;

        elements.container = container;
        elements.mascotBtn = container.querySelector('.mascot-actor-btn');
        elements.squashSpan = container.querySelector('.mascot-squash-span');
        elements.dirLayer = container.querySelector('.mascot-layer-directions');
        elements.reactLayer = container.querySelector('.mascot-layer-reactions');
        elements.bubble = container.querySelector('.mascot-speech-bubble');
        elements.bubbleText = container.querySelector('.mascot-bubble-text');
        elements.bubbleSub = container.querySelector('.mascot-bubble-sub');

        if (elements.mascotBtn && !elements.mascotBtn._hasMascotClick) {
            elements.mascotBtn._hasMascotClick = true;
            elements.mascotBtn.onmousedown = (e) => {
                if (state.isGameMode) e.preventDefault();
            };
            elements.mascotBtn.onclick = (e) => {
                e.stopPropagation();
                boop();
            };
        }

        return elements;
    }

    function getStorageState() {
        try {
            const saved = localStorage.getItem('mascot_user_settings');
            if (saved) {
                const parsed = JSON.parse(saved);
                if (parsed.character && !CHARACTERS[parsed.character]) {
                    parsed.character = 'wolf';
                }
                // Luôn mở Mascot và bật bóng thoại để bạn đồng hành luôn tương tác
                parsed.minimized = false;
                parsed.showBubble = true;
                if (typeof parsed.soundEnabled !== 'boolean') {
                    parsed.soundEnabled = true;
                }
                return Object.assign({}, state, parsed);
            }
        } catch (e) {
            console.warn('Mascot settings read error:', e);
        }
        return state;
    }

    function saveStorageState() {
        try {
            localStorage.setItem('mascot_user_settings', JSON.stringify({
                character: state.character,
                size: state.size,
                minimized: state.minimized,
                showBubble: state.showBubble,
                soundEnabled: state.soundEnabled
            }));
        } catch (e) { }
    }

    function cellPosition(index) {
        const col = index % 3;
        const row = Math.floor(index / 3);
        return `${col * 50}% ${row * 50}%`;
    }

    function updateDirectionUI() {
        const el = getLiveElements();
        if (!el.dirLayer) return;
        const idx = DIRECTIONS.indexOf(state.direction);
        if (idx >= 0) {
            el.dirLayer.style.backgroundPosition = cellPosition(idx);
        }
    }

    function updateReactionUI() {
        const el = getLiveElements();
        if (!el.dirLayer || !el.reactLayer) return;
        if (state.reaction) {
            const idx = REACTIONS.indexOf(state.reaction);
            el.reactLayer.style.backgroundPosition = cellPosition(idx >= 0 ? idx : 0);
            el.reactLayer.style.opacity = '1';
            el.dirLayer.style.opacity = '0';
        } else {
            el.reactLayer.style.opacity = '0';
            el.dirLayer.style.opacity = '1';
        }
    }

    function setReaction(reactionName, durationMs = 1800) {
        if (reactionTimeout) {
            clearTimeout(reactionTimeout);
            reactionTimeout = null;
        }

        state.reaction = reactionName;
        updateReactionUI();

        if (reactionName !== null && durationMs > 0) {
            reactionTimeout = setTimeout(() => {
                state.reaction = null;
                updateReactionUI();
                reactionTimeout = null;
            }, durationMs);
        }
    }

    function wrap(angle) {
        return Math.atan2(Math.sin(angle), Math.cos(angle));
    }

    let currentSector = -1;

    function aim() {
        const el = getLiveElements();
        if (!el.mascotBtn || !pointer || state.minimized) return;

        const box = el.mascotBtn.getBoundingClientRect();
        if (box.width === 0 || box.height === 0) return;

        const centerX = box.left + box.width / 2;
        const centerY = box.top + box.height / 2;
        const dx = pointer.x - centerX;
        const dy = pointer.y - centerY;

        // Khi con trỏ chuột ở trong phạm vi gần linh vật: nhìn thẳng trực diện vào bạn
        if (Math.hypot(dx, dy) < DEAD_ZONE) {
            currentSector = -1;
            if (state.direction !== 'center') {
                state.direction = 'center';
                updateDirectionUI();
            }
            return;
        }

        // Định hướng ánh mắt chính xác theo vector góc atan2 (chuẩn page-mascot)
        // Giữ sector hiện tại cho đến khi con trỏ vượt hẳn biên của góc (Hysteresis chống rung lắc)
        const angle = Math.atan2(dy, dx);
        if (currentSector !== -1 && Math.abs(wrap(angle - currentSector * SECTOR)) < SECTOR / 2 + HYSTERESIS) {
            return;
        }

        currentSector = (Math.round(angle / SECTOR) + CLOCKWISE.length) % CLOCKWISE.length;
        const newDirection = CLOCKWISE[currentSector];

        if (state.direction !== newDirection) {
            state.direction = newDirection;
            updateDirectionUI();
        }
    }

    function boop() {
        const now = Date.now();
        if (now - boops.lastClickTime < 100) return; // Debounce
        boops.lastClickTime = now;
        lastUserActivity = now;

        boops.count = (now - boops.at < DIZZY_WINDOW) ? boops.count + 1 : 1;
        boops.at = now;

        if (boops.count >= DIZZY_AFTER) {
            boops.count = 0;
            setReaction('dizzy', DIZZY_END);
            showCustomQuote("U oa... Chóng mặt quá bạn ơi! 😵💫", "Quay mòng mòng!");
            // Chuỗi giọt nước tí tách xoay vòng khi chóng mặt
            [1.25, 1.05, 0.85].forEach((p, idx) => {
                setTimeout(() => playWaterDropSfx(p), idx * 75);
            });
        } else {
            const payoff = PAYOFFS[(boops.count - 1) % PAYOFFS.length];
            setReaction('blink', BOOP_PAYOFF);
            setTimeout(() => {
                setReaction(payoff, BOOP_END);
            }, BOOP_PAYOFF);

            // Âm thanh giọt nước nhỏ nhẹ tinh tế mỗi lần bấm vào con cáo/sói
            const pitchMods = [1.0, 1.07, 1.15];
            playWaterDropSfx(pitchMods[(boops.count - 1) % pitchMods.length]);

            // Đổi câu thoại vui tươi khi nhấp
            if (state.isGameMode) {
                gameSay('hit', 'Gâu gâu! Đồng hành cùng bạn! 🐺', 'Bắn từ vựng nào!');
                setTimeout(() => {
                    const gameInput = document.querySelector('.hsk-input-dock input, .quantum-game-overlay input');
                    if (gameInput) gameInput.focus();
                }, 30);
            } else {
                triggerRandomQuote(false); // Không đè biểu cảm của hành động nhấp
            }
        }

        // Squash bounce animation
        const el = getLiveElements();
        if (el.squashSpan && !window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
            try {
                el.squashSpan.animate(SQUASH_KEYFRAMES, {
                    duration: SQUASH_MS,
                    easing: 'linear'
                });
            } catch (e) { }
        }
    }

    function showCustomQuote(text, sub = '', duration = 5500) {
        const el = getLiveElements();
        if (!el.bubble || !state.showBubble) return;
        if (el.bubbleText) el.bubbleText.innerText = text;
        if (el.bubbleSub) el.bubbleSub.innerText = sub;
        el.bubble.classList.add('visible');

        clearTimeout(el.bubble._hideTimer);
        el.bubble._hideTimer = setTimeout(() => {
            if (el.bubble) el.bubble.classList.remove('visible');
        }, duration);
    }

    function gameSay(type, customText = null, customSub = null, customReaction = null) {
        const el = getLiveElements();
        if (!el.bubble || state.minimized) return;

        const now = Date.now();
        lastUserActivity = now;
        const isPriority = ['damage', 'boss', 'high_combo', 'stage_clear', 'game_over', 'win', 'start'].includes(type);

        // Giảm tần suất bình luận cho lượt bắn thường để tránh chớp nháy
        if (!isPriority && (now - lastGameSayTime < 1300)) {
            return;
        }
        lastGameSayTime = now;

        let text = customText;
        let sub = customSub;
        let react = customReaction;

        if (!text && GAME_QUOTES[type]) {
            const list = GAME_QUOTES[type];
            const item = list[Math.floor(Math.random() * list.length)];
            text = item.text;
            sub = sub || item.sub;
            react = react || item.reaction;
        }

        if (!text) text = "Cố lên bạn ơi! 🎯";

        if (react) {
            setReaction(react, 1400);
        }

        if (el.bubbleText) el.bubbleText.innerText = text;
        if (el.bubbleSub) el.bubbleSub.innerText = sub || '';
        el.bubble.classList.add('visible');

        clearTimeout(el.bubble._hideTimer);
        el.bubble._hideTimer = setTimeout(() => {
            if (el.bubble) el.bubble.classList.remove('visible');
        }, isPriority ? 3000 : 2200);
    }

    function setGameMode(isGame, gameName = 'hsk') {
        state.isGameMode = isGame;
        state.gameName = gameName;
        const el = getLiveElements();

        if (el.container) {
            if (isGame) {
                el.container.classList.add('game-mode');
                state.preGameSize = state.size;
                updateSize(85); // Kích thước gọn nhẹ Co-Pilot góc màn hình
                if (el.mascotBtn) {
                    el.mascotBtn.setAttribute('tabindex', '-1');
                }
            } else {
                el.container.classList.remove('game-mode');
                updateSize(state.preGameSize || 110);
            }
        }
    }

    function triggerRandomQuote(setReact = true) {
        if (!state.showBubble) return;

        // 70% cơ hội đưa ra mẹo/hướng dẫn của trang hiện tại, 30% danh ngôn chung
        const cat = state.currentCategory || 'home';
        const catGuides = PAGE_GUIDANCE[cat];

        if (catGuides && catGuides.length > 0 && Math.random() < 0.72) {
            let idx = Math.floor(Math.random() * catGuides.length);
            if (catGuides.length > 1 && idx === state.lastGuideIdx) {
                idx = (idx + 1) % catGuides.length;
            }
            state.lastGuideIdx = idx;
            const g = catGuides[idx];

            if (setReact && g.reaction) {
                setReaction(g.reaction, 2000);
            }

            showCustomQuote(g.text, g.sub, 6500);
        } else {
            const randomIdx = Math.floor(Math.random() * STUDY_QUOTES.length);
            state.quoteIndex = randomIdx;
            const q = STUDY_QUOTES[randomIdx];
            showCustomQuote(q.text, q.sub, 5500);
        }
    }

    let lastNavigatedCategory = '';
    let lastNavigatedTime = 0;
    let navTimer = null;
    let pendingNavUrl = null;

    function getPageCategory(url) {
        if (!url) return 'home';
        let path = url;
        try {
            if (url.startsWith('http://') || url.startsWith('https://')) {
                path = new URL(url).pathname;
            } else {
                path = url.split('?')[0].split('#')[0];
            }
        } catch (e) {
            path = (url || '').split('?')[0].split('#')[0];
        }
        path = (path || '').toLowerCase();
        if (path.length > 1 && path.endsWith('/')) {
            path = path.slice(0, -1);
        }

        if (path.includes('/admin') || path.includes('/portal-hub')) return 'admin';
        if (path.includes('/profile')) return 'profile';
        if (path.includes('/games') || path.includes('/tro-choi') || path.includes('/vocab-shooter')) return 'games';
        if (path.includes('/toeic')) return 'toeic';

        // HSK routes
        if (path.includes('/hsk')) {
            if (path.includes('/tu-vung') || path.includes('/vocabulary')) return 'hsk_vocab';
            if (path.includes('/luyen-de') || path.includes('/mock-test') || path.includes('/mock-tests')) return 'hsk_mock_tests';
            return 'hsk_dashboard';
        }

        // IELTS routes
        if (path.includes('/ielts')) {
            if (path.includes('/doc-truyen') || path.includes('/stories') || path.includes('/story')) return 'ielts_stories';
            if (path.includes('/tu-vung') || path.includes('/vocabulary')) return 'ielts_vocab';
            if (path.includes('/nghe-dien') || path.includes('/listen-fill')) return 'ielts_listen_fill';
            if (path.includes('/speak-along') || path.includes('/noi-theo')) return 'ielts_speak_along';
            if (path.includes('/grammar') || path.includes('/ngu-phap')) return 'ielts_grammar';
            if (path.includes('/uu-tien') || path.includes('/priority-review')) return 'ielts_priority_review';
            if (path.includes('/review') || path.includes('/dap-an') || path.includes('/xem-dap-an')) return 'ielts_review';
            if (path.includes('/mock-test') || path.includes('/mock-tests') || path.includes('/phong-thi-thu') || path.includes('/luyen-de')) return 'ielts_mock_test';
            if (path.includes('/listening') || path.includes('/reading') || path.includes('/writing') || path.includes('/speaking')) return 'ielts_exam_room';
            return 'ielts_dashboard';
        }

        // Direct non-prefixed routes fallback
        if (path.includes('/doc-truyen')) return 'ielts_stories';
        if (path.includes('/nghe-dien')) return 'ielts_listen_fill';
        if (path.includes('/speak-along') || path.includes('/noi-theo')) return 'ielts_speak_along';
        if (path.includes('/grammar') || path.includes('/ngu-phap')) return 'ielts_grammar';
        if (path.includes('/tu-vung')) return 'ielts_vocab';

        if (path === '' || path === '/') return 'home';

        return 'home';
    }

    function onPageNavigated(url) {
        if (!elements.container || !elements.bubble) {
            pendingNavUrl = url;
            return;
        }

        const category = getPageCategory(url);
        const now = Date.now();

        // Tự động khôi phục chế độ thường nếu rời khỏi màn hình trò chơi
        if (state.isGameMode && category !== 'games') {
            setGameMode(false);
        }

        state.currentCategory = category;

        // Tránh nói lặp lại cùng một trang trong vòng 1.5 giây
        if (category === lastNavigatedCategory && (now - lastNavigatedTime < 1500)) {
            return;
        }
        lastNavigatedCategory = category;
        lastNavigatedTime = now;

        if (navTimer) clearTimeout(navTimer);
        navTimer = setTimeout(() => {
            if (state.minimized || !state.showBubble) return;

            // Nếu loader toàn trang đang hiển thị, chờ cho loader biến mất hẳn
            const loader = document.getElementById('app-loader');
            const isLoaderActive = loader && (!loader.classList.contains('fade-out') && loader.style.display !== 'none');
            if (isLoaderActive) {
                setTimeout(() => onPageNavigated(url), 500);
                return;
            }

            // Nếu đang trong trận game căng thẳng thì không ngắt quãng bằng lời thoại trang thông thường
            if (state.isGameMode) return;

            const guides = PAGE_GUIDANCE[category] || PAGE_GUIDANCE['home'];
            if (!guides || guides.length === 0) return;

            let idx = Math.floor(Math.random() * guides.length);
            if (guides.length > 1 && idx === state.lastGuideIdx) {
                idx = (idx + 1) % guides.length;
            }
            state.lastGuideIdx = idx;

            const guide = guides[idx];

            if (guide.reaction) {
                setReaction(guide.reaction, 2000);
            }

            showCustomQuote(guide.text, guide.sub, 6500);
        }, 450);
    }

    function updateCharacter(charId) {
        if (!CHARACTERS[charId]) return;
        state.character = charId;
        const char = CHARACTERS[charId];
        const el = getLiveElements();

        if (el.dirLayer) {
            el.dirLayer.style.backgroundImage = `url('${char.directions}')`;
        }
        if (el.reactLayer) {
            el.reactLayer.style.backgroundImage = `url('${char.reactions}')`;
        }

        showCustomQuote(`Xin chào! Mình là ${char.name} ${char.icon}`, "Rất vui được học cùng bạn!");
        saveStorageState();
    }

    function updateSize(newSize) {
        state.size = Math.max(70, Math.min(160, newSize));
        const el = getLiveElements();
        if (el.mascotBtn) {
            el.mascotBtn.style.width = `${state.size}px`;
            el.mascotBtn.style.height = `${state.size}px`;
        }
        saveStorageState();
    }

    function toggleMinimize(forceVal) {
        state.minimized = typeof forceVal === 'boolean' ? forceVal : !state.minimized;
        const el = getLiveElements();
        if (el.container) {
            if (state.minimized) {
                el.container.classList.add('is-minimized');
                if (el.bubble) el.bubble.classList.remove('visible');
            } else {
                el.container.classList.remove('is-minimized');
                setTimeout(() => triggerRandomQuote(true), 400);
            }
        }
        saveStorageState();
    }

    function toggleBubble() {
        state.showBubble = !state.showBubble;
        const el = getLiveElements();
        if (!state.showBubble && el.bubble) {
            el.bubble.classList.remove('visible');
        } else if (state.showBubble) {
            triggerRandomQuote(true);
        }
        saveStorageState();
    }

    function toggleSound() {
        state.soundEnabled = !state.soundEnabled;
        if (state.soundEnabled) {
            playWaterDropSfx(1.1);
        }
        saveStorageState();
        return state.soundEnabled;
    }

    function initListeners() {
        if (isInitialized) return;
        isInitialized = true;

        const onPointerMove = (e) => {
            pointer = { x: e.clientX, y: e.clientY };
            lastUserActivity = Date.now();
            aim();
        };

        window.addEventListener('pointermove', onPointerMove, { passive: true });
        window.addEventListener('scroll', () => {
            lastUserActivity = Date.now();
            aim();
        }, { passive: true });

        // Vòng lặp cử động tự nhiên (chớp mắt, mỉm cười) khi người dùng không di chuột
        if (idleInterval) clearInterval(idleInterval);
        idleInterval = setInterval(() => {
            if (state.minimized || state.isGameMode) return;
            const now = Date.now();

            // Nếu người dùng không tương tác > 2.5s và mascot đang rảnh rỗi (không có reaction nào đang phát)
            if (now - lastUserActivity > 2500 && state.reaction === null) {
                const rand = Math.random();
                if (rand < 0.72) {
                    // Chớp mắt tự nhiên 200ms
                    setReaction('blink', 200);
                } else {
                    // Mỉm cười nhắm mắt đáng yêu 750ms rồi trở lại hướng nhìn của chuột
                    setReaction('delighted', 750);
                }
            }
        }, 3200);

        // Chu kỳ hiện bóng thoại động viên mỗi 28s (chỉ khi không ở trong game)
        if (quoteInterval) clearInterval(quoteInterval);
        quoteInterval = setInterval(() => {
            if (!state.minimized && !state.isGameMode && state.showBubble && Math.random() > 0.25) {
                triggerRandomQuote(true);
            }
        }, 28000);
    }

    const manager = {
        init: function (containerEl) {
            if (!containerEl) return;
            elements.container = containerEl;
            elements.mascotBtn = containerEl.querySelector('.mascot-actor-btn');
            elements.squashSpan = containerEl.querySelector('.mascot-squash-span');
            elements.dirLayer = containerEl.querySelector('.mascot-layer-directions');
            elements.reactLayer = containerEl.querySelector('.mascot-layer-reactions');
            elements.bubble = containerEl.querySelector('.mascot-speech-bubble');
            elements.bubbleText = containerEl.querySelector('.mascot-bubble-text');
            elements.bubbleSub = containerEl.querySelector('.mascot-bubble-sub');

            // Nạp cấu hình đã lưu
            state = getStorageState();

            // Áp dụng nhân vật & kích thước
            updateCharacter(state.character);
            updateSize(state.size);
            toggleMinimize(state.minimized);

            // Đảm bảo trạng thái sprite ban đầu sẵn sàng
            state.reaction = null;
            updateDirectionUI();
            updateReactionUI();

            // Bắt sự kiện
            if (elements.mascotBtn && !elements.mascotBtn._hasMascotClick) {
                elements.mascotBtn._hasMascotClick = true;
                elements.mascotBtn.onmousedown = (e) => {
                    if (state.isGameMode) {
                        e.preventDefault(); // Không cướp focus của ô gõ pinyin trong game
                    }
                };
                elements.mascotBtn.onclick = (e) => {
                    e.stopPropagation();
                    boop();
                };
                elements.mascotBtn.onmouseenter = () => {
                    if (!state.minimized && !state.isGameMode && state.showBubble) {
                        const el = getLiveElements();
                        if (el.bubble && !el.bubble.classList.contains('visible')) {
                            triggerRandomQuote(false);
                        }
                    }
                };
            }

            initListeners();

            // Kích hoạt lời chào & hướng dẫn ngữ cảnh của trang hiện tại khi Mascot khởi tạo xong
            const triggerInitialGuidance = () => {
                const loader = document.getElementById('app-loader');
                const isLoaderActive = loader && (!loader.classList.contains('fade-out') && loader.style.display !== 'none');
                if (isLoaderActive) {
                    setTimeout(triggerInitialGuidance, 500);
                    return;
                }
                const initialUrl = pendingNavUrl || window.location.href;
                pendingNavUrl = null;
                onPageNavigated(initialUrl);
            };
            setTimeout(triggerInitialGuidance, 900);
        },

        setCharacter: function (charId) {
            updateCharacter(charId);
        },

        setSize: function (size) {
            updateSize(size);
        },

        toggleMinimize: function () {
            toggleMinimize();
        },

        toggleBubble: function () {
            toggleBubble();
        },

        toggleSound: function () {
            return toggleSound();
        },

        boop: function () {
            boop();
        },

        gameSay: function (type, customText, customSub, customReaction) {
            gameSay(type, customText, customSub, customReaction);
        },

        setGameMode: function (isGame, gameName) {
            setGameMode(isGame, gameName);
        },

        onPageNavigated: function (url) {
            onPageNavigated(url);
        },

        getState: function () {
            return {
                character: state.character,
                size: state.size,
                minimized: state.minimized,
                showBubble: state.showBubble,
                soundEnabled: state.soundEnabled,
                isGameMode: state.isGameMode,
                currentCategory: state.currentCategory
            };
        }
    };

    // Tự động giải phóng hàng đợi từ MascotManager stub nếu có trước khi file script nạp xong
    if (typeof window !== 'undefined') {
        const prevStub = window.MascotManager;
        window.MascotManager = manager;
        if (prevStub && prevStub._isStub && typeof prevStub._flush === 'function') {
            try {
                prevStub._flush(manager);
            } catch (err) {
                console.warn('[MascotManager] Warning flushing stub queue:', err);
            }
        }
    }

    return manager;
})();
