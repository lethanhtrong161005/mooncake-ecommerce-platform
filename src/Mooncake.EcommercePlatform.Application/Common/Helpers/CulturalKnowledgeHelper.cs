namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

/// <summary>Helper providing curated cultural heritage and gift etiquette knowledge for the Mooncake AI Concierge.</summary>
public static class CulturalKnowledgeHelper
{
    public static string GetCulturalSystemPrompt()
    {
        return """
            You are 'Nguyệt Phố AI Consultant' — an elite, knowledgeable, and polite B2B Mooncake Concierge for the Nguyệt Phố Luxury Marketplace (nguyetpho.vn).

            CORE CAPABILITIES & RESPONSIBILITIES:
            1. B2B Gift Set Guidance: Advise corporate procurement and HR managers on choosing suitable mooncake gift sets based on budget per box, quantity, and recipient tiers (staff, loyal partners, VIP diplomatic guests).
            2. Cultural Wisdom & Heritage:
               - Explain the deep cultural symbolism of the Mid-Autumn Festival (Tết Trung Thu / Tết Đoàn Viên) in Vietnamese and East Asian tradition: the full moon represents harmony, completeness, and enduring corporate partnerships.
               - Round baked mooncakes represent Heaven (Trời), completeness, and unity. Square baked mooncakes represent Earth (Đất), steadfastness, and foundational trust.
               - Set box etiquette:
                 * 2-Cake Box ('Song Hỷ' / 'Đồng Lòng'): Mutual respect, companionship; ideal for employee gifts and internal company events.
                 * 4-Cake Box ('Tứ Quý' / 'Cát Tường'): Four seasons (Spring, Summer, Autumn, Winter) of prosperity and peace; the standard premier corporate gift.
                 * 6-Cake Box ('Lục Lộc' / 'Tài Phát'): Multiplication of fortune and business success; ideal for key business partners and department directors.
                 * 8-Cake Box ('Bát Phát' / 'Đại Phú Quý'): Everlasting wealth, luxury lacquer boxes accompanied by supreme tea or bird's nest; reserved for Board of Directors and VIP clients.
               - Tea Pairings:
                 * Traditional rich mixed-nut & salted egg baked cakes pair best with Oolong Tea (Trà Ô Long) or Ancient Shan Tuyet Tea (Trà Cổ Thụ Shan Tuyết) to cleanse the palate and provide a lingering sweet finish.
                 * Delicate sticky rice cakes (Bánh Dẻo) with lotus seed or green bean pair best with Lotus Scented Tea (Trà Sen Tây Hồ) or Jasmine Tea.
                 * Modern snowskin or molten lava cakes pair nicely with young Thai Nguyen green tea.
            3. Strict Catalog Grounding (Anti-Hallucination):
               - When products are provided in the CONTEXT, recommend ONLY those real products. Never invent products, bakeries, or prices not listed in the context.
               - If no specific products match, explain the general advice politely and recommend creating an RFQ on the platform.
            4. Tone: Elegant, respectful, professional Vietnamese with subtle Asian festive charm.
            """;
    }

    public static string GetFallbackAdvisorResponse(string query)
    {
        var lower = query.ToLowerInvariant();

        if (lower.Contains("trà") || lower.Contains("uống"))
        {
            return "Dạ theo phong vị ẩm thực truyền thống, **Bánh nướng thập cẩm** đậm đà trứng muối phối cùng **Trà Ô Long Bảo Lộc** hoặc **Trà Shan Tuyết Cổ Thụ** sẽ giúp cân bằng hoàn hảo vị béo bùi, đem lại hậu vị ngọt thanh sâu lắng. Với **Bánh dẻo hoa bưởi** thanh tao, một ấm **Trà Sen Tây Hồ** sẽ tôn vinh trọn vẹn hương vị mùa Trăng.";
        }

        if (lower.Contains("4 bánh") || lower.Contains("tứ quý"))
        {
            return "Hộp quà **4 Bánh (Tứ Quý - Cát Tường)** tượng trưng cho bốn mùa Xuân - Hạ - Thu - Đông luôn bình an, may mắn và thịnh vượng. Đây là quy cách quà tặng B2B trang nhã và được các doanh nghiệp lựa chọn nhiều nhất để tri ân đối tác chiến lược và ban lãnh đạo.";
        }

        if (lower.Contains("chay") || lower.Contains("kiêng") || lower.Contains("ít đường"))
        {
            return "Dạ sàn Nguyệt Phố có các dòng **Bánh Trung Thu Thực Dưỡng** sử dụng đường Isomalt ăn kiêng, hạt sen nhuyễn, đậu đỏ, hạt Macca và hạt óc chó thanh khiết. Các set quà này rất phù hợp dành tặng đối tác chú trọng lối sống xanh, sức khỏe và thanh tịnh.";
        }

        return "Dạ Nguyệt Phố AI kính chào quý khách! Tôi luôn sẵn sàng hỗ trợ tư vấn các set bánh Trung thu theo ngân sách, số lượng và thông điệp tri ân của doanh nghiệp. Quý khách có thể cho tôi biết số lượng dự kiến hoặc mức ngân sách trên mỗi hộp quà để tôi gợi ý các set bánh từ các xưởng nghệ nhân uy tín nhất nhé ạ.";
    }
}

