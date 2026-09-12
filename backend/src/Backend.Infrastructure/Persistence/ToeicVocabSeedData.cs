using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence;

public static class ToeicVocabSeedData
{
    public static async Task SeedToeicVocabularyAsync(AppDbContext dbContext)
    {
        try
        {
            if (await dbContext.ToeicVocabularies.AnyAsync())
            {
                return;
            }

            var items = new List<ToeicVocabulary>
            {
                new() { Word = "negotiate", Ipa = "/nɪˈɡoʊʃieɪt/", Meaning = "Đàm phán, thương lượng", Example = "We need to negotiate the price before signing.", Topic = "Kinh doanh" },
                new() { Word = "contract", Ipa = "/ˈkɑːntrækt/", Meaning = "Hợp đồng", Example = "Both parties signed the contract this morning.", Topic = "Kinh doanh" },
                new() { Word = "expand", Ipa = "/ɪkˈspænd/", Meaning = "Mở rộng", Example = "The company plans to expand into new markets.", Topic = "Kinh doanh" },
                new() { Word = "strategy", Ipa = "/ˈstrætədʒi/", Meaning = "Chiến lược", Example = "Our marketing strategy focuses on social media.", Topic = "Kinh doanh" },
                new() { Word = "partnership", Ipa = "/ˈpɑːrtnərʃɪp/", Meaning = "Quan hệ đối tác", Example = "The two firms formed a partnership last year.", Topic = "Kinh doanh" },
                new() { Word = "implement", Ipa = "/ˈɪmplɪment/", Meaning = "Triển khai, thực hiện", Example = "We will implement the new system in June.", Topic = "Kinh doanh" },
                new() { Word = "deadline", Ipa = "/ˈdedlaɪn/", Meaning = "Hạn chót", Example = "The deadline for the report is this Friday.", Topic = "Kinh doanh" },
                new() { Word = "enterprise", Ipa = "/ˈentərpraɪz/", Meaning = "Doanh nghiệp", Example = "She works for a large enterprise in Hanoi.", Topic = "Kinh doanh" },
                new() { Word = "venture", Ipa = "/ˈventʃər/", Meaning = "Dự án đầu tư, mạo hiểm", Example = "Starting a new venture requires careful planning.", Topic = "Kinh doanh" },
                new() { Word = "profit", Ipa = "/ˈprɑːfɪt/", Meaning = "Lợi nhuận", Example = "The division reported a 20% increase in profit.", Topic = "Kinh doanh" },

                new() { Word = "colleague", Ipa = "/ˈkɑːliːɡ/", Meaning = "Đồng nghiệp", Example = "My colleague will cover the front desk today.", Topic = "Văn phòng" },
                new() { Word = "department", Ipa = "/dɪˈpɑːrtmənt/", Meaning = "Phòng ban", Example = "Please forward the email to the sales department.", Topic = "Văn phòng" },
                new() { Word = "supervisor", Ipa = "/ˈsuːpərvaɪzər/", Meaning = "Người giám sát", Example = "Ask your supervisor before leaving early.", Topic = "Văn phòng" },
                new() { Word = "assignment", Ipa = "/əˈsaɪnmənt/", Meaning = "Nhiệm vụ được giao", Example = "The new assignment is due next Monday.", Topic = "Văn phòng" },
                new() { Word = "schedule", Ipa = "/ˈskedʒuːl/", Meaning = "Lịch trình", Example = "The meeting is scheduled for 9 a.m.", Topic = "Văn phòng" },
                new() { Word = "memo", Ipa = "/ˈmemoʊ/", Meaning = "Bản ghi nhớ nội bộ", Example = "HR sent a memo about the holiday schedule.", Topic = "Văn phòng" },
                new() { Word = "overtime", Ipa = "/ˈoʊvərtaɪm/", Meaning = "Làm ngoài giờ", Example = "Staff are paid extra for overtime work.", Topic = "Văn phòng" },
                new() { Word = "stationery", Ipa = "/ˈsteɪʃəneri/", Meaning = "Văn phòng phẩm", Example = "Order more stationery for the office.", Topic = "Văn phòng" },
                new() { Word = "meeting", Ipa = "/ˈmiːtɪŋ/", Meaning = "Cuộc họp", Example = "The budget meeting starts at noon.", Topic = "Văn phòng" },
                new() { Word = "reception", Ipa = "/rɪˈsepʃn/", Meaning = "Quầy tiếp tân", Example = "Leave your key at the reception.", Topic = "Văn phòng" },

                new() { Word = "invoice", Ipa = "/ˈɪnvɔɪs/", Meaning = "Hóa đơn", Example = "Please send the invoice to accounting.", Topic = "Tài chính" },
                new() { Word = "revenue", Ipa = "/ˈrevənuː/", Meaning = "Doanh thu", Example = "Revenue increased by 15% this quarter.", Topic = "Tài chính" },
                new() { Word = "expense", Ipa = "/ɪkˈspens/", Meaning = "Chi phí", Example = "Keep receipts for every business expense.", Topic = "Tài chính" },
                new() { Word = "budget", Ipa = "/ˈbʌdʒɪt/", Meaning = "Ngân sách", Example = "The project went over budget.", Topic = "Tài chính" },
                new() { Word = "refund", Ipa = "/ˈriːfʌnd/", Meaning = "Khoản hoàn tiền", Example = "Customers can request a full refund within 30 days.", Topic = "Tài chính" },
                new() { Word = "discount", Ipa = "/ˈdɪskaʊnt/", Meaning = "Giảm giá", Example = "Members get a 10% discount on all items.", Topic = "Tài chính" },
                new() { Word = "installment", Ipa = "/ɪnˈstɔːlmənt/", Meaning = "Kỳ trả góp", Example = "Pay for the laptop in twelve installments.", Topic = "Tài chính" },
                new() { Word = "transaction", Ipa = "/trænˈzækʃn/", Meaning = "Giao dịch", Example = "All transactions are recorded securely.", Topic = "Tài chính" },
                new() { Word = "quotation", Ipa = "/kwoʊˈteɪʃn/", Meaning = "Báo giá", Example = "We received a quotation from three suppliers.", Topic = "Tài chính" },
                new() { Word = "withdraw", Ipa = "/wɪðˈdrɔː/", Meaning = "Rút (tiền)", Example = "You can withdraw cash at any branch.", Topic = "Tài chính" },

                new() { Word = "qualification", Ipa = "/ˌkwɑːlɪfɪˈkeɪʃn/", Meaning = "Bằng cấp, trình độ", Example = "She has the right qualifications for the job.", Topic = "Nhân sự" },
                new() { Word = "applicant", Ipa = "/ˈæplɪkənt/", Meaning = "Người ứng tuyển", Example = "We interviewed five applicants yesterday.", Topic = "Nhân sự" },
                new() { Word = "recruit", Ipa = "/rɪˈkruːt/", Meaning = "Tuyển dụng", Example = "The company plans to recruit 20 engineers.", Topic = "Nhân sự" },
                new() { Word = "promotion", Ipa = "/prəˈmoʊʃn/", Meaning = "Sự thăng chức", Example = "He received a promotion after one year.", Topic = "Nhân sự" },
                new() { Word = "salary", Ipa = "/ˈsæləri/", Meaning = "Tiền lương", Example = "Negotiate your salary before accepting the offer.", Topic = "Nhân sự" },
                new() { Word = "benefit", Ipa = "/ˈbenɪfɪt/", Meaning = "Phúc lợi", Example = "Health insurance is part of the benefits package.", Topic = "Nhân sự" },
                new() { Word = "resign", Ipa = "/rɪˈzaɪn/", Meaning = "Từ chức", Example = "She resigned to start her own business.", Topic = "Nhân sự" },
                new() { Word = "candidate", Ipa = "/ˈkændɪdət/", Meaning = "Ứng viên", Example = "The final candidate will be announced soon.", Topic = "Nhân sự" },
                new() { Word = "experience", Ipa = "/ɪkˈspɪriəns/", Meaning = "Kinh nghiệm", Example = "Applicants need at least two years of experience.", Topic = "Nhân sự" },
                new() { Word = "retire", Ipa = "/rɪˈtaɪə/", Meaning = "Nghỉ hưu", Example = "Mr. Brown will retire at the end of the month.", Topic = "Nhân sự" },

                new() { Word = "itinerary", Ipa = "/aɪˈtɪnəreri/", Meaning = "Lịch trình chuyến đi", Example = "The itinerary includes three city tours.", Topic = "Du lịch" },
                new() { Word = "accommodation", Ipa = "/əˌkɑːməˈdeɪʃn/", Meaning = "Chỗ ở", Example = "Accommodation is included in the package.", Topic = "Du lịch" },
                new() { Word = "departure", Ipa = "/dɪˈpɑːrtʃər/", Meaning = "Sự khởi hành", Example = "Check the departure board for updates.", Topic = "Du lịch" },
                new() { Word = "reservation", Ipa = "/ˌrezərˈveɪʃn/", Meaning = "Sự đặt chỗ", Example = "I made a reservation for two nights.", Topic = "Du lịch" },
                new() { Word = "delay", Ipa = "/dɪˈleɪ/", Meaning = "Sự hoãn, trì hoãn", Example = "The flight was delayed by two hours.", Topic = "Du lịch" },
                new() { Word = "luggage", Ipa = "/ˈlʌɡɪdʒ/", Meaning = "Hành lý", Example = "Collect your luggage at carousel five.", Topic = "Du lịch" },
                new() { Word = "commute", Ipa = "/kəˈmjuːt/", Meaning = "Đi lại hằng ngày", Example = "She commutes to work by train.", Topic = "Du lịch" },
                new() { Word = "destination", Ipa = "/ˌdestɪˈneɪʃn/", Meaning = "Điểm đến", Example = "Paris is a popular tourist destination.", Topic = "Du lịch" },
                new() { Word = "excursion", Ipa = "/ɪkˈskɜːrʒn/", Meaning = "Chuyến tham quan ngắn", Example = "The hotel offers excursions to the old town.", Topic = "Du lịch" },
                new() { Word = "boarding", Ipa = "/ˈbɔːrdɪŋ/", Meaning = "Lên máy bay", Example = "Boarding starts at 10:30.", Topic = "Du lịch" },

                new() { Word = "purchase", Ipa = "/ˈpɜːrtʃəs/", Meaning = "Mua, sự mua hàng", Example = "Keep the receipt as proof of purchase.", Topic = "Mua sắm" },
                new() { Word = "receipt", Ipa = "/rɪˈsiːt/", Meaning = "Biên lai", Example = "Here is your receipt, sir.", Topic = "Mua sắm" },
                new() { Word = "warranty", Ipa = "/ˈwɔːrənti/", Meaning = "Chế độ bảo hành", Example = "The laptop comes with a two-year warranty.", Topic = "Mua sắm" },
                new() { Word = "exchange", Ipa = "/ɪksˈtʃeɪndʒ/", Meaning = "Đổi hàng", Example = "You can exchange items within seven days.", Topic = "Mua sắm" },
                new() { Word = "delivery", Ipa = "/dɪˈlɪvəri/", Meaning = "Sự giao hàng", Example = "Free delivery for orders over $50.", Topic = "Mua sắm" },
                new() { Word = "merchandise", Ipa = "/ˈmɜːrtʃəndaɪs/", Meaning = "Hàng hóa", Example = "The store displays its merchandise in the window.", Topic = "Mua sắm" },
                new() { Word = "available", Ipa = "/əˈveɪləbl/", Meaning = "Có sẵn", Example = "The model you want is available in blue.", Topic = "Mua sắm" },
                new() { Word = "catalog", Ipa = "/ˈkætəlɔːɡ/", Meaning = "Danh mục sản phẩm", Example = "Browse the catalog for more options.", Topic = "Mua sắm" },
                new() { Word = "out of stock", Ipa = "/aʊt əv stɑːk/", Meaning = "Hết hàng", Example = "Sorry, that item is out of stock.", Topic = "Mua sắm" },
                new() { Word = "loyalty", Ipa = "/ˈlɔɪəlti/", Meaning = "Sự trung thành", Example = "Join our loyalty program to earn points.", Topic = "Mua sắm" },

                new() { Word = "equipment", Ipa = "/ɪˈkwɪpmənt/", Meaning = "Thiết bị", Example = "All office equipment is insured.", Topic = "Công nghệ" },
                new() { Word = "software", Ipa = "/ˈsɔːftwer/", Meaning = "Phần mềm", Example = "Install the software before the training session.", Topic = "Công nghệ" },
                new() { Word = "upgrade", Ipa = "/ˈʌpɡreɪd/", Meaning = "Nâng cấp", Example = "We need to upgrade the server this weekend.", Topic = "Công nghệ" },
                new() { Word = "network", Ipa = "/ˈnetwɜːrk/", Meaning = "Mạng (máy tính)", Example = "The network will be down for maintenance.", Topic = "Công nghệ" },
                new() { Word = "database", Ipa = "/ˈdeɪtəbeɪs/", Meaning = "Cơ sở dữ liệu", Example = "Customer records are stored in a database.", Topic = "Công nghệ" },
                new() { Word = "malfunction", Ipa = "/ˌmælˈfʌŋkʃn/", Meaning = "Sự trục trặc", Example = "The printer malfunctioned during the audit.", Topic = "Công nghệ" },
                new() { Word = "compatible", Ipa = "/kəmˈpætəbl/", Meaning = "Tương thích", Example = "The app is compatible with all devices.", Topic = "Công nghệ" },
                new() { Word = "innovative", Ipa = "/ˈɪnəveɪtɪv/", Meaning = "Đổi mới, sáng tạo", Example = "They won an award for innovative design.", Topic = "Công nghệ" },
                new() { Word = "device", Ipa = "/dɪˈvaɪs/", Meaning = "Thiết bị", Example = "Save your work to the company device.", Topic = "Công nghệ" },
                new() { Word = "maintenance", Ipa = "/ˈmeɪntənəns/", Meaning = "Bảo trì", Example = "The elevator is under maintenance until Friday.", Topic = "Công nghệ" }
            };

            dbContext.ToeicVocabularies.AddRange(items);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ToeicVocabSeedData] Note: {ex.Message}");
        }
    }
}
