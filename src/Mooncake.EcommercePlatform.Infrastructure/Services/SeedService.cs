namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>Populates complete, realistic demo data across all 30 database tables.</summary>
public class SeedService(ApplicationDbContext context, IPasswordHasher passwordHasher) : ISeedService
{
    public async Task<string> SeedDemoDataAsync(CancellationToken cancellationToken = default)
    {
        if (await context.Users.AnyAsync(cancellationToken))
        {
            return "Database already initialized. Demo seeding skipped.";
        }

        var now = DateTime.UtcNow;
        var defaultPasswordHash = passwordHasher.HashPassword("Password123!");

        // ── 1. Users ──────────────────────────────────────────────────────────
        var adminUser = new User
        {
            Email = "admin@mooncake.vn",
            PasswordHash = defaultPasswordHash,
            FullName = "Platform Administrator",
            Phone = "0901000001",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var b2cCustomerUser = new User
        {
            Email = "customer.an@gmail.com",
            PasswordHash = defaultPasswordHash,
            FullName = "Nguyễn Văn An",
            Phone = "0902000002",
            Role = UserRole.Customer,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var b2bCustomerUser = new User
        {
            Email = "procurement@vinamilk.com.vn",
            PasswordHash = defaultPasswordHash,
            FullName = "Vinamilk Procurement",
            Phone = "0903000003",
            Role = UserRole.Customer,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var supplierKinhDoUser = new User
        {
            Email = "sales@mondelezkinhdo.com",
            PasswordHash = defaultPasswordHash,
            FullName = "Kinh Đô Official",
            Phone = "0904000004",
            Role = UserRole.Supplier,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var supplierNhuLanUser = new User
        {
            Email = "contact@nhulan.vn",
            PasswordHash = defaultPasswordHash,
            FullName = "Như Lan Bakery",
            Phone = "0905000005",
            Role = UserRole.Supplier,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        context.Users.AddRange(adminUser, b2cCustomerUser, b2bCustomerUser, supplierKinhDoUser, supplierNhuLanUser);
        await context.SaveChangesAsync(cancellationToken);

        // ── 2. Customers ──────────────────────────────────────────────────────
        var b2cCustomer = new Customer
        {
            UserId = b2cCustomerUser.Id,
            CustomerType = CustomerType.Individual,
            DefaultAddress = "123 Hai Bà Trưng, Quận 1, TP. Hồ Chí Minh",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var b2bCustomer = new Customer
        {
            UserId = b2bCustomerUser.Id,
            CustomerType = CustomerType.Company,
            CompanyName = "Tập Đoàn Vinamilk Việt Nam",
            TaxCode = "0300588569",
            DefaultAddress = "10 Tân Trào, Tân Phú, Quận 7, TP. Hồ Chí Minh",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        context.Customers.AddRange(b2cCustomer, b2bCustomer);

        // ── 3. Suppliers ──────────────────────────────────────────────────────
        var supplierKinhDo = new Supplier
        {
            UserId = supplierKinhDoUser.Id,
            BusinessName = "CTCP Mondelez Kinh Đô Việt Nam",
            Description = "Thương hiệu bánh trung thu hàng đầu Việt Nam hơn 25 năm uy tín",
            Address = "138-142 Hai Bà Trưng, Đa Kao, Quận 1, TP. Hồ Chí Minh",
            TaxCode = "0301234567",
            ReputationScore = 125,
            IsVerified = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var supplierNhuLan = new Supplier
        {
            UserId = supplierNhuLanUser.Id,
            BusinessName = "Tiệm Bánh Như Lan TP.HCM",
            Description = "Bánh trung thu Như Lan hơn 50 năm gìn giữ phong vị Sài Gòn",
            Address = "64 Hàm Nghi, Bến Nghé, Quận 1, TP. Hồ Chí Minh",
            TaxCode = "0309876543",
            ReputationScore = 100,
            IsVerified = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        context.Suppliers.AddRange(supplierKinhDo, supplierNhuLan);
        await context.SaveChangesAsync(cancellationToken);

        // ── 4. Shop Templates ─────────────────────────────────────────────────
        var modernTemplate = new ShopTemplate
        {
            Name = "Modern Festive",
            Description = "Giao diện hiện đại phong cách lễ hội trăng tròn ánh kim sang trọng",
            PreviewUrl = "https://images.unsplash.com/photo-1509198397868-475647b2a1e5",
            Config = "{\"primaryColor\": \"#c53030\", \"accentColor\": \"#ecc94b\"}",
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var heritageTemplate = new ShopTemplate
        {
            Name = "Traditional Heritage",
            Description = "Giao diện cổ truyền hoài niệm mang hương vị trung thu xưa",
            PreviewUrl = "https://images.unsplash.com/photo-1541832676-9b763b0239ab",
            Config = "{\"primaryColor\": \"#9b2c2c\", \"accentColor\": \"#d69e2e\"}",
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        context.ShopTemplates.AddRange(modernTemplate, heritageTemplate);
        await context.SaveChangesAsync(cancellationToken);

        // ── 5. Shops ──────────────────────────────────────────────────────────
        var kinhDoShop = new Shop
        {
            SupplierId = supplierKinhDo.Id,
            TemplateId = modernTemplate.Id,
            TemplateOverrides = "{}",
            Name = "Kinh Đô Official Store",
            Slug = "kinh-do-official",
            Description = "Cửa hàng chính hãng Mondelez Kinh Đô - Đỉnh cao nghệ thuật bánh trung thu",
            BannerUrl = "https://images.unsplash.com/photo-1578985545062-69928b1d9587",
            LogoUrl = "https://images.unsplash.com/photo-1555396273-367ea4eb4db5",
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var nhuLanShop = new Shop
        {
            SupplierId = supplierNhuLan.Id,
            TemplateId = heritageTemplate.Id,
            TemplateOverrides = "{}",
            Name = "Như Lan Heritage Bakery",
            Slug = "nhu-lan-heritage",
            Description = "Bánh trung thu Như Lan hơn 50 năm gìn giữ phong vị Sài Gòn",
            BannerUrl = "https://images.unsplash.com/photo-1578985545062-69928b1d9587",
            LogoUrl = "https://images.unsplash.com/photo-1555396273-367ea4eb4db5",
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        context.Shops.AddRange(kinhDoShop, nhuLanShop);

        // ── 6. Categories ─────────────────────────────────────────────────────
        var bakedCategory = new Category
        {
            Name = "Bánh Nướng Truyền Thống",
            Slug = "banh-nuong-truyen-thong",
            Description = "Các dòng bánh nướng nhân mặn ngọt đậm đà thơm ngậy",
            CreatedAtUtc = now
        };

        var stickyCategory = new Category
        {
            Name = "Bánh Dẻo Hương Hoa Bưởi",
            Slug = "banh-deo-huong-hoa-buoi",
            Description = "Bánh dẻo mềm dẻo thanh tao thơm ngát hương hoa bưởi",
            CreatedAtUtc = now
        };

        var luxuryBoxCategory = new Category
        {
            Name = "Hộp Quà Trăng Vàng B2B",
            Slug = "hop-qua-trang-vang-b2b",
            Description = "Set quà cao cấp thiết kế dành riêng cho doanh nghiệp tri ân",
            CreatedAtUtc = now
        };

        context.Categories.AddRange(bakedCategory, stickyCategory, luxuryBoxCategory);
        await context.SaveChangesAsync(cancellationToken);

        // ── 7. Products ───────────────────────────────────────────────────────
        var productKinhDoThapCam = new Product
        {
            ShopId = kinhDoShop.Id,
            CategoryId = bakedCategory.Id,
            Name = "Bánh Nướng Thập Cẩm Gà Quay Vi Cá",
            Description = "Nhân thập cẩm 8 món hảo hạng hòa quyện sốt XO và gà quay thượng hạng",
            CustomPackagingFee = 0m,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var productKinhDoSet = new Product
        {
            ShopId = kinhDoShop.Id,
            CategoryId = luxuryBoxCategory.Id,
            Name = "Hộp Quà Trăng Vàng Hoàng Kim 4 Bánh",
            Description = "Hộp quà cao cấp ép kim bao gồm 4 bánh thượng hạng kèm trà ô long",
            CustomPackagingFee = 85000m,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var productNhuLanHatSen = new Product
        {
            ShopId = nhuLanShop.Id,
            CategoryId = bakedCategory.Id,
            Name = "Bánh Nướng Hạt Sen Hạt Dưa Trứng Muối",
            Description = "Hạt sen Huế thanh mát ngọt nhẹ kết hợp hạt dưa giòn bùi",
            CustomPackagingFee = 0m,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        context.Products.AddRange(productKinhDoThapCam, productKinhDoSet, productNhuLanHatSen);
        await context.SaveChangesAsync(cancellationToken);

        // ── 8. Product Variants ───────────────────────────────────────────────
        var variant150 = new ProductVariant
        {
            ProductId = productKinhDoThapCam.Id,
            Sku = "KD-TC-150G-1T",
            Name = "150g - 1 Trứng",
            Price = 145000m,
            StockQuantity = 500,
            MinOrderQuantity = 1,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var variant210 = new ProductVariant
        {
            ProductId = productKinhDoThapCam.Id,
            Sku = "KD-TC-210G-2T",
            Name = "210g - 2 Trứng",
            Price = 185000m,
            StockQuantity = 350,
            MinOrderQuantity = 1,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var variantB2BSet = new ProductVariant
        {
            ProductId = productKinhDoSet.Id,
            Sku = "KD-SET-HK04",
            Name = "Hộp 4 Bánh Thượng Hạng + Hộp Gỗ",
            Price = 1150000m,
            StockQuantity = 200,
            MinOrderQuantity = 1,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var variantNhuLan200 = new ProductVariant
        {
            ProductId = productNhuLanHatSen.Id,
            Sku = "NL-HS-200G-2T",
            Name = "200g - 2 Trứng",
            Price = 125000m,
            StockQuantity = 400,
            MinOrderQuantity = 1,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        context.ProductVariants.AddRange(variant150, variant210, variantB2BSet, variantNhuLan200);

        // ── 9. Product Images ─────────────────────────────────────────────────
        var img1 = new ProductImage
        {
            ProductId = productKinhDoThapCam.Id,
            Url = "https://images.unsplash.com/photo-1509198397868-475647b2a1e5",
            IsPrimary = true,
            SortOrder = 1,
            CreatedAtUtc = now
        };

        var img2 = new ProductImage
        {
            ProductId = productKinhDoSet.Id,
            Url = "https://images.unsplash.com/photo-1541832676-9b763b0239ab",
            IsPrimary = true,
            SortOrder = 1,
            CreatedAtUtc = now
        };

        context.ProductImages.AddRange(img1, img2);

        // ── 10. Promotion Rules ───────────────────────────────────────────────
        var earlyBirdPromo = new PromotionRule
        {
            ShopId = kinhDoShop.Id,
            ProductId = null,
            Name = "Tri Ân Khách Hàng Sớm - Giảm 10%",
            DiscountType = DiscountType.Percent,
            MinQuantity = 1,
            DiscountPercent = 10m,
            DiscountAmount = null,
            FreeQuantity = null,
            StartsAt = now.AddDays(-10),
            EndsAt = now.AddDays(30),
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var bulkPromo = new PromotionRule
        {
            ShopId = kinhDoShop.Id,
            ProductId = null,
            Name = "Chiết Khấu Doanh Nghiệp >= 50 Hộp",
            DiscountType = DiscountType.Percent,
            MinQuantity = 50,
            DiscountPercent = 15m,
            DiscountAmount = null,
            FreeQuantity = null,
            StartsAt = now.AddDays(-10),
            EndsAt = now.AddDays(60),
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        context.PromotionRules.AddRange(earlyBirdPromo, bulkPromo);
        await context.SaveChangesAsync(cancellationToken);

        // ── 11. Custom Packaging ──────────────────────────────────────────────
        var customPackaging = new CustomPackaging
        {
            CustomerId = b2bCustomer.Id,
            Name = "Hộp Gỗ Ép Nhũ Vàng Khắc Logo Vinamilk",
            LogoUrl = "https://assets.mooncake.vn/designs/vinamilk-logo.png",
            DesignNotes = "Khoá đồng mỹ thuật ép nhũ vàng 24K",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        context.CustomPackagings.Add(customPackaging);
        await context.SaveChangesAsync(cancellationToken);

        // ── 12. Retail Order & OrderItems ─────────────────────────────────────
        var retailOrder = new Order
        {
            CustomerId = b2cCustomer.Id,
            ShopId = kinhDoShop.Id,
            Status = OrderStatus.Delivered,
            Subtotal = 740000m,
            DiscountTotal = 74000m,
            ShippingFee = 30000m,
            TotalAmount = 696000m,
            DepositRequired = 0m,
            ReceiverName = "Nguyễn Văn An",
            ReceiverPhone = "0902000002",
            ShippingAddress = "123 Hai Bà Trưng, Quận 1, TP. Hồ Chí Minh",
            RequiredDeliveryDate = DateOnly.FromDateTime(now.AddDays(2)),
            Note = "Giao giờ hành chính giúp em",
            CreatedAtUtc = now.AddDays(-5),
            UpdatedAtUtc = now.AddDays(-1)
        };

        context.Orders.Add(retailOrder);
        await context.SaveChangesAsync(cancellationToken);

        var orderItem = new OrderItem
        {
            OrderId = retailOrder.Id,
            VariantId = variant210.Id,
            PromotionRuleId = earlyBirdPromo.Id,
            CustomPackagingId = null,
            Quantity = 4,
            UnitPrice = 185000m,
            PackagingFee = 0m,
            DiscountAmount = 74000m,
            LineTotal = 666000m, // 4 * (185000 + 0) - 74000 = 666,000
            CreatedAtUtc = now.AddDays(-5)
        };

        context.OrderItems.Add(orderItem);

        // ── 13. RFQ, RfqItems, Invitations ────────────────────────────────────
        var b2bRfq = new RequestForQuotation
        {
            CustomerId = b2bCustomer.Id,
            Title = "Yêu Cầu Báo Giá 500 Hộp Quà Trung Thu Tri Ân Cổ Đông Vinamilk",
            Description = "Cần gia công 500 hộp quà cao cấp có ép logo thương hiệu Vinamilk gửi tặng đối tác chiến lược",
            Visibility = RfqVisibility.Open,
            Status = RfqStatus.Awarded,
            QuoteDeadline = now.AddDays(7),
            RequiredDeliveryDate = DateOnly.FromDateTime(now.AddDays(30)),
            DeliveryAddress = "10 Tân Trào, Tân Phú, Quận 7, TP. Hồ Chí Minh",
            CreatedAtUtc = now.AddDays(-15),
            UpdatedAtUtc = now.AddDays(-10)
        };

        context.RequestForQuotations.Add(b2bRfq);
        await context.SaveChangesAsync(cancellationToken);

        var rfqItem = new RfqItem
        {
            RfqId = b2bRfq.Id,
            ProductId = productKinhDoSet.Id,
            ItemName = "Hộp Quà Trăng Vàng Hoàng Kim 4 Bánh Thượng Hạng",
            Specification = "Trọng lượng bánh >= 200g, hạn sử dụng tối thiểu 40 ngày",
            Quantity = 500,
            CustomPackagingId = customPackaging.Id,
            Note = "Khắc laser logo Vinamilk trên nắp hộp",
            CreatedAtUtc = now.AddDays(-15)
        };

        context.RfqItems.Add(rfqItem);

        var inviteKinhDo = new RfqInvitation
        {
            RfqId = b2bRfq.Id,
            SupplierId = supplierKinhDo.Id,
            Status = RfqInvitationStatus.Quoted,
            InvitedAtUtc = now.AddDays(-15)
        };

        var inviteNhuLan = new RfqInvitation
        {
            RfqId = b2bRfq.Id,
            SupplierId = supplierNhuLan.Id,
            Status = RfqInvitationStatus.Quoted,
            InvitedAtUtc = now.AddDays(-15)
        };

        context.RfqInvitations.AddRange(inviteKinhDo, inviteNhuLan);
        await context.SaveChangesAsync(cancellationToken);

        // ── 14. Quotations & Items ────────────────────────────────────────────
        var quoteKinhDo = new Quotation
        {
            RfqId = b2bRfq.Id,
            SupplierId = supplierKinhDo.Id,
            Status = QuotationStatus.Accepted,
            TotalAmount = 550000000m,
            LeadTimeDays = 20,
            ProposedDepositPercent = 30m,
            ValidUntilUtc = now.AddDays(15),
            Notes = "Đã bao gồm chi phí khắc logo và vận chuyển tận nơi tại TP.HCM",
            CreatedAtUtc = now.AddDays(-14),
            UpdatedAtUtc = now.AddDays(-10)
        };

        var quoteNhuLan = new Quotation
        {
            RfqId = b2bRfq.Id,
            SupplierId = supplierNhuLan.Id,
            Status = QuotationStatus.Rejected,
            TotalAmount = 590000000m,
            LeadTimeDays = 25,
            ProposedDepositPercent = 40m,
            ValidUntilUtc = now.AddDays(15),
            Notes = "Hộp quà truyền thống đặc biệt",
            CreatedAtUtc = now.AddDays(-14),
            UpdatedAtUtc = now.AddDays(-10)
        };

        context.Quotations.AddRange(quoteKinhDo, quoteNhuLan);
        await context.SaveChangesAsync(cancellationToken);

        var quoteItemKinhDo = new QuotationItem
        {
            QuotationId = quoteKinhDo.Id,
            RfqItemId = rfqItem.Id,
            Quantity = 500,
            UnitPrice = 1100000m,
            LineTotal = 550000000m,
            Note = "Đơn giá 1,100,000 VND / hộp"
        };

        var quoteItemNhuLan = new QuotationItem
        {
            QuotationId = quoteNhuLan.Id,
            RfqItemId = rfqItem.Id,
            Quantity = 500,
            UnitPrice = 1180000m,
            LineTotal = 590000000m,
            Note = "Đơn giá 1,180,000 VND / hộp"
        };

        context.QuotationItems.AddRange(quoteItemKinhDo, quoteItemNhuLan);

        // ── 15. Price Negotiation ─────────────────────────────────────────────
        var negotiationRound1 = new PriceNegotiation
        {
            QuotationId = quoteKinhDo.Id,
            RoundNo = 1,
            ProposedBy = NegotiationProposedBy.Customer,
            ProposedAmount = 550000000m,
            Message = "Vinamilk đề xuất mức giá ưu đãi 550 triệu cho đơn hàng 500 set",
            Status = NegotiationStatus.Accepted,
            CreatedAtUtc = now.AddDays(-12)
        };

        context.PriceNegotiations.Add(negotiationRound1);

        // ── 16. Contract & Milestones ─────────────────────────────────────────
        var b2bContract = new Contract
        {
            QuotationId = quoteKinhDo.Id,
            CustomerId = b2bCustomer.Id,
            SupplierId = supplierKinhDo.Id,
            Status = ContractStatus.Active,
            TotalAmount = 550000000m,
            DepositPercent = 30m,
            DeliveryDeadline = DateOnly.FromDateTime(now.AddDays(25)),
            LatePenaltyPercentPerDay = 0.5m,
            MaxPenaltyPercent = 10m,
            Terms = "Hợp đồng kinh tế gia công và cung ứng bánh trung thu Mondelez Kinh Đô & Vinamilk",
            CustomerSignedAtUtc = now.AddDays(-10),
            SupplierSignedAtUtc = now.AddDays(-9),
            CompletedAtUtc = null,
            CreatedAtUtc = now.AddDays(-10),
            UpdatedAtUtc = now.AddDays(-9)
        };

        context.Contracts.Add(b2bContract);
        await context.SaveChangesAsync(cancellationToken);

        var milestoneDeposit = new ContractMilestone
        {
            ContractId = b2bContract.Id,
            MilestoneNo = 1,
            Name = "Thanh Toán Đặt Cọc 30%",
            MilestoneType = MilestoneType.Deposit,
            Amount = 165000000m,
            DueDate = DateOnly.FromDateTime(now.AddDays(-3)),
            Status = MilestoneStatus.Paid,
            PaidAtUtc = now.AddDays(-2),
            CreatedAtUtc = now.AddDays(-10),
            UpdatedAtUtc = now.AddDays(-2)
        };

        var milestoneFinal = new ContractMilestone
        {
            ContractId = b2bContract.Id,
            MilestoneNo = 2,
            Name = "Thanh Toán Quyết Toán Đợt Cuối 70%",
            MilestoneType = MilestoneType.Final,
            Amount = 385000000m,
            DueDate = b2bContract.DeliveryDeadline,
            Status = MilestoneStatus.Pending,
            PaidAtUtc = null,
            CreatedAtUtc = now.AddDays(-10),
            UpdatedAtUtc = now.AddDays(-10)
        };

        context.ContractMilestones.AddRange(milestoneDeposit, milestoneFinal);
        await context.SaveChangesAsync(cancellationToken);

        // ── 17. Payments ──────────────────────────────────────────────────────
        var retailPayment = new Payment
        {
            OrderId = retailOrder.Id,
            MilestoneId = null,
            PaymentType = PaymentType.Full,
            Amount = 696000m,
            Method = PaymentMethod.EWallet,
            Status = PaymentStatus.Succeeded,
            TransactionRef = "TXN-SEED-MOMO-001",
            PaidAtUtc = now.AddDays(-5),
            CreatedAtUtc = now.AddDays(-5),
            UpdatedAtUtc = now.AddDays(-5)
        };

        var contractDepositPayment = new Payment
        {
            OrderId = null,
            MilestoneId = milestoneDeposit.Id,
            PaymentType = PaymentType.Deposit,
            Amount = 165000000m,
            Method = PaymentMethod.BankTransfer,
            Status = PaymentStatus.Succeeded,
            TransactionRef = "TXN-SEED-VCB-CORP-001",
            PaidAtUtc = now.AddDays(-2),
            CreatedAtUtc = now.AddDays(-2),
            UpdatedAtUtc = now.AddDays(-2)
        };

        context.Payments.AddRange(retailPayment, contractDepositPayment);

        // ── 18. Deliveries & Proofs ───────────────────────────────────────────
        var orderDelivery = new Delivery
        {
            OrderId = retailOrder.Id,
            ContractId = null,
            Direction = DeliveryDirection.SupplierToCustomer,
            Status = DeliveryStatus.Delivered,
            CarrierName = "Giao Hàng Tiết Kiệm (GHTK)",
            TrackingCode = "GHTK-MK-982138",
            DeliveryAddress = "123 Hai Bà Trưng, Quận 1, TP. Hồ Chí Minh",
            RecipientName = "Nguyễn Văn An",
            RecipientPhone = "0902000002",
            ScheduledAtUtc = now.AddDays(-2),
            ShippedAtUtc = now.AddDays(-2),
            DeliveredAtUtc = now.AddDays(-1),
            Note = "Giao tận tay khách hàng",
            CreatedAtUtc = now.AddDays(-3),
            UpdatedAtUtc = now.AddDays(-1)
        };

        var contractDelivery = new Delivery
        {
            OrderId = null,
            ContractId = b2bContract.Id,
            Direction = DeliveryDirection.SupplierToCustomer,
            Status = DeliveryStatus.Delivered,
            CarrierName = "Đội Xe Chuyên Dụng Mondelez",
            TrackingCode = "MDLZ-TRUCK-51C-9921",
            DeliveryAddress = "10 Tân Trào, Tân Phú, Quận 7, TP. Hồ Chí Minh",
            RecipientName = "Kho Tổng Vinamilk Quận 7",
            RecipientPhone = "0903000003",
            ScheduledAtUtc = now.AddDays(-2),
            ShippedAtUtc = now.AddDays(-2),
            DeliveredAtUtc = now.AddDays(-1),
            Note = "500 thùng carton nguyên seal",
            CreatedAtUtc = now.AddDays(-5),
            UpdatedAtUtc = now.AddDays(-1)
        };

        context.Deliveries.AddRange(orderDelivery, contractDelivery);
        await context.SaveChangesAsync(cancellationToken);

        var deliveryProofPickup = new DeliveryProof
        {
            DeliveryId = contractDelivery.Id,
            ProofType = ProofType.Pickup,
            PhotoUrl = "https://images.unsplash.com/photo-1586528116311-ad8dd3c8310d",
            TakenByUserId = supplierKinhDoUser.Id,
            TakenAtUtc = now.AddDays(-2),
            Latitude = 10.7769m,
            Longitude = 106.7009m,
            Note = "Bốc dỡ tại kho nhà máy Bình Dương"
        };

        var deliveryProofDropoff = new DeliveryProof
        {
            DeliveryId = contractDelivery.Id,
            ProofType = ProofType.Delivery,
            PhotoUrl = "https://images.unsplash.com/photo-1549465220-1a8b9238cd48",
            TakenByUserId = supplierKinhDoUser.Id,
            TakenAtUtc = now.AddDays(-1),
            Latitude = 10.7282m,
            Longitude = 106.7218m,
            Note = "Bàn giao biên bản giao nhận có ký nhận của Vinamilk"
        };

        context.DeliveryProofs.AddRange(deliveryProofPickup, deliveryProofDropoff);

        // ── 19. Reviews & Reputation Logs ─────────────────────────────────────
        var orderReview = new Review
        {
            CustomerId = b2cCustomer.Id,
            SupplierId = supplierKinhDo.Id,
            OrderId = retailOrder.Id,
            ContractId = null,
            Rating = 5,
            Comment = "Bánh nướng vi cá rất thơm ngon, bao bì đóng gói sang trọng, giao hàng đúng hẹn.",
            CreatedAtUtc = now.AddDays(-1)
        };

        var contractReview = new Review
        {
            CustomerId = b2bCustomer.Id,
            SupplierId = supplierKinhDo.Id,
            OrderId = null,
            ContractId = b2bContract.Id,
            Rating = 5,
            Comment = "Mondelez Kinh Đô thực hiện hợp đồng rất chuyên nghiệp. Logo khắc laser sắc nét, chất lượng bánh tuyệt hảo.",
            CreatedAtUtc = now
        };

        context.Reviews.AddRange(orderReview, contractReview);
        await context.SaveChangesAsync(cancellationToken);

        var repLog1 = new ReputationLog
        {
            SupplierId = supplierKinhDo.Id,
            EventType = ReputationEventType.Review,
            ScoreDelta = 10,
            ReviewId = orderReview.Id,
            ContractId = null,
            Reason = "Customer review 5/5 stars for Order",
            CreatedAtUtc = now.AddDays(-1)
        };

        var repLog2 = new ReputationLog
        {
            SupplierId = supplierKinhDo.Id,
            EventType = ReputationEventType.ContractOnTime,
            ScoreDelta = 15,
            ReviewId = null,
            ContractId = b2bContract.Id,
            Reason = $"Contract #{b2bContract.Id} fulfilled on time ahead of schedule",
            CreatedAtUtc = now.AddDays(-1)
        };

        context.ReputationLogs.AddRange(repLog1, repLog2);

        // ── 20. Notifications ─────────────────────────────────────────────────
        var notif1 = new Notification
        {
            UserId = b2cCustomerUser.Id,
            Type = "OrderDelivered",
            Title = "Đơn hàng đã giao thành công",
            Body = $"Đơn hàng #{retailOrder.Id} của bạn đã được giao thành công.",
            EntityType = "Order",
            EntityId = retailOrder.Id,
            IsRead = true,
            ReadAtUtc = now.AddDays(-1),
            CreatedAtUtc = now.AddDays(-1)
        };

        var notif2 = new Notification
        {
            UserId = supplierKinhDoUser.Id,
            Type = "ContractActive",
            Title = "Hợp đồng đã có hiệu lực",
            Body = $"Hợp đồng #{b2bContract.Id} với Vinamilk đã được hai bên ký điện tử thành công.",
            EntityType = "Contract",
            EntityId = b2bContract.Id,
            IsRead = false,
            ReadAtUtc = null,
            CreatedAtUtc = now.AddDays(-9)
        };

        context.Notifications.AddRange(notif1, notif2);

        // ── 21. AI Conversations, Messages, Analytics ─────────────────────────
        var aiConvo = new AiConversation
        {
            UserId = b2cCustomerUser.Id,
            Title = "Tư vấn chọn bánh trung thu cho gia đình",
            CreatedAtUtc = now.AddDays(-6),
            UpdatedAtUtc = now.AddDays(-6)
        };

        context.AiConversations.Add(aiConvo);
        await context.SaveChangesAsync(cancellationToken);

        var msgUser = new AiMessage
        {
            ConversationId = aiConvo.Id,
            Role = AiMessageRole.User,
            Content = "Gia đình tôi có người lớn tuổi thích ít đường và trẻ nhỏ thích vị socola, nên chọn combo bánh nào?",
            Metadata = "{}",
            CreatedAtUtc = now.AddDays(-6)
        };

        var msgAssistant = new AiMessage
        {
            ConversationId = aiConvo.Id,
            Role = AiMessageRole.Assistant,
            Content = "Chào bạn! Bạn có thể chọn Bánh Nướng Hạt Sen Hạt Dưa thanh ngọt tự nhiên ít đường cho người lớn tuổi, và Bánh Dẻo Nhân Socola / Lava Trứng Chảy cho các bé.",
            Metadata = "{}",
            CreatedAtUtc = now.AddDays(-6)
        };

        context.AiMessages.AddRange(msgUser, msgAssistant);

        var aiReport = new AiAnalyticsReport
        {
            SupplierId = supplierKinhDo.Id,
            ReportType = "MidAutumnSalesForecast",
            PeriodStart = DateOnly.FromDateTime(now.AddDays(-30)),
            PeriodEnd = DateOnly.FromDateTime(now.AddDays(30)),
            Parameters = "{\"region\": \"South\", \"segment\": \"B2B Corporate\"}",
            Result = "{\"projectedRevenue\": 2500000000, \"growthRate\": 18.5, \"topSellingSku\": \"KD-SET-HK04\"}",
            Status = AiReportStatus.Completed,
            CreatedAtUtc = now.AddDays(-3),
            CompletedAtUtc = now.AddDays(-3)
        };

        context.AiAnalyticsReports.Add(aiReport);
        await context.SaveChangesAsync(cancellationToken);

        return "Successfully seeded comprehensive demo data across all 30 tables!";
    }
}
