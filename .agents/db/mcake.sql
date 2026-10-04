-- =============================================================================
-- MOONCAKE MARKETPLACE – PostgreSQL DDL Script
-- Generated from: mooncake-marketplace.md (Design v1.0)
-- Rules:
--   • All tables have audit columns: is_deleted, created_at, updated_at,
--     created_by, updated_by
--   • Only PK, NOT NULL, UNIQUE constraints are declared.
--     FK relationships exist logically (columns present) but NO FK CONSTRAINT.
--   • ENUMs defined as PostgreSQL TYPE before table creation.
--   • pgvector extension enabled for AI embedding support.
--   • Indexes at the end of file.
-- =============================================================================


-- ---------------------------------------------------------------------------
-- 0. EXTENSIONS
-- ---------------------------------------------------------------------------
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "vector";       -- pgvector for AI embeddings
CREATE EXTENSION IF NOT EXISTS "pg_trgm";      -- trigram for full-text search

-- ---------------------------------------------------------------------------
-- 1. ENUM TYPES
-- ---------------------------------------------------------------------------

CREATE TYPE user_role AS ENUM (
    'Customer',
    'Supplier',
    'Admin'
);

CREATE TYPE shop_status AS ENUM (
    'Active',
    'Inactive',
    'Suspended'
);

CREATE TYPE product_status AS ENUM (
    'Active',
    'Inactive',
    'OutOfStock'
);

CREATE TYPE discount_type AS ENUM (
    'Percentage',
    'FixedAmount',
    'FixedPrice'
);

CREATE TYPE order_status AS ENUM (
    'Pending',
    'Confirmed',
    'Processing',
    'Shipped',
    'Delivered',
    'Cancelled'
);

CREATE TYPE custom_packaging_status AS ENUM (
    'Pending',
    'DesignReview',
    'Approved',
    'InProduction',
    'Rejected'
);

CREATE TYPE rfq_status AS ENUM (
    'Draft',
    'Open',
    'InNegotiation',
    'Awarded',
    'Closed',
    'Cancelled'
);

CREATE TYPE rfq_invitation_status AS ENUM (
    'Invited',
    'Viewed',
    'BidSubmitted',
    'Declined'
);

CREATE TYPE bid_status AS ENUM (
    'Submitted',
    'Accepted',
    'Cancelled'
);

CREATE TYPE contract_status AS ENUM (
    'Draft',
    'PendingSignature',
    'Signed',
    'PendingDeposit',
    'Active',
    'CompletedOnTime',
    'CompletedLate',
    'Breached',
    'Disputed',
    'Cancelled'
);

CREATE TYPE milestone_status AS ENUM (
    'Pending',
    'AwaitingPayment',
    'Paid',
    'CompletedOnTime',
    'CompletedLate',
    'Overdue',
    'Failed'
);

CREATE TYPE delivery_type AS ENUM (
    'PlatformManaged',
    'SelfArranged'
);

CREATE TYPE delivery_status AS ENUM (
    'Pending',
    'Confirmed',
    'PickedUp',
    'InTransit',
    'Delivered',
    'Failed',
    'Returned'
);

CREATE TYPE proof_type AS ENUM (
    'Pickup',
    'InTransit',
    'Delivery',
    'FailedAttempt'
);

CREATE TYPE reputation_event AS ENUM (
    'CompletedOnTime',
    'CompletedLate',
    'Breached',
    'PositiveReview',
    'NegativeReview',
    'AdminPenalty',
    'AdminBonus'
);

CREATE TYPE ai_conversation_type AS ENUM (
    'ProductInfo',
    'MooncakeHistory',
    'SalesAnalytics',
    'General'
);

CREATE TYPE ai_message_role AS ENUM (
    'User',
    'Assistant',
    'System'
);

CREATE TYPE ai_report_type AS ENUM (
    'SalesTrend',
    'SupplierPerformance',
    'DemandForecast',
    'ProductAnalysis',
    'RevenueSummary'
);

CREATE TYPE ai_report_status AS ENUM (
    'Pending',
    'Processing',
    'Completed',
    'Failed'
);

CREATE TYPE payment_method AS ENUM (
    'BankTransfer',
    'Momo',
    'VNPay',
    'ZaloPay',
    'Cash'
);

CREATE TYPE payment_status AS ENUM (
    'Pending',
    'Processing',
    'Completed',
    'Failed',
    'Refunded'
);

-- ---------------------------------------------------------------------------
-- 2. TABLES
-- ---------------------------------------------------------------------------
-- Convention: every table ends with the 5 audit columns:
--   is_deleted  BOOLEAN NOT NULL DEFAULT FALSE,
--   created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
--   updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
--   created_by  UUID,          -- user id who created (nullable = system)
--   updated_by  UUID           -- user id who last updated
-- ---------------------------------------------------------------------------

-- ── GROUP: USER MANAGEMENT ──────────────────────────────────────────────────

CREATE TABLE users (
    id                  UUID         NOT NULL DEFAULT uuid_generate_v4(),
    email               VARCHAR(255) NOT NULL,
    password_hash       VARCHAR(255) NOT NULL,
    role                user_role    NOT NULL,
    full_name           VARCHAR(255),
    phone               VARCHAR(20),
    avatar_url          TEXT,
    is_active           BOOLEAN      NOT NULL DEFAULT TRUE,
    email_verified_at   TIMESTAMPTZ,
    -- audit
    is_deleted          BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by          UUID,
    updated_by          UUID,
    PRIMARY KEY (id),
    UNIQUE (email)
);

CREATE TABLE supplier_profiles (
    id                  UUID         NOT NULL DEFAULT uuid_generate_v4(),
    user_id             UUID         NOT NULL,           -- -> users.id
    company_name        VARCHAR(255),
    company_logo_url    TEXT,
    tax_code            VARCHAR(50),
    address             TEXT,
    description         TEXT,
    reputation_score    DECIMAL(5,2) NOT NULL DEFAULT 0,
    total_contracts     INT          NOT NULL DEFAULT 0,
    contracts_on_time   INT          NOT NULL DEFAULT 0,
    contracts_late      INT          NOT NULL DEFAULT 0,
    contracts_breached  INT          NOT NULL DEFAULT 0,
    avg_rating          DECIMAL(3,2),
    verified            BOOLEAN      NOT NULL DEFAULT FALSE,
    verified_at         TIMESTAMPTZ,
    -- audit
    is_deleted          BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by          UUID,
    updated_by          UUID,
    PRIMARY KEY (id),
    UNIQUE (tax_code)
);

CREATE TABLE customer_profiles (
    id                  UUID         NOT NULL DEFAULT uuid_generate_v4(),
    user_id             UUID         NOT NULL,           -- -> users.id
    company_name        VARCHAR(255),
    company_logo_url    TEXT,
    tax_code            VARCHAR(50),
    billing_address     TEXT,
    contact_person      VARCHAR(255),
    -- audit
    is_deleted          BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by          UUID,
    updated_by          UUID,
    PRIMARY KEY (id)
);

-- ── GROUP: SHOP & TEMPLATE ──────────────────────────────────────────────────

CREATE TABLE shop_templates (
    id                  UUID         NOT NULL DEFAULT uuid_generate_v4(),
    name                VARCHAR(100),
    description         TEXT,
    preview_image_url   TEXT,
    css_variables       JSONB,
    layout_config       JSONB,
    is_active           BOOLEAN      NOT NULL DEFAULT TRUE,
    sort_order          INT          NOT NULL DEFAULT 0,
    -- audit
    is_deleted          BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by          UUID,
    updated_by          UUID,
    PRIMARY KEY (id)
);

CREATE TABLE shops (
    id                  UUID         NOT NULL DEFAULT uuid_generate_v4(),
    supplier_id         UUID         NOT NULL,           -- -> supplier_profiles.id
    template_id         UUID         NOT NULL,           -- -> shop_templates.id
    name                VARCHAR(255),
    slug                VARCHAR(255) NOT NULL,
    description         TEXT,
    banner_url          TEXT,
    logo_url            TEXT,
    custom_colors       JSONB,
    custom_css          TEXT,
    status              shop_status  NOT NULL DEFAULT 'Active',
    -- audit
    is_deleted          BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by          UUID,
    updated_by          UUID,
    PRIMARY KEY (id),
    UNIQUE (slug)
);

-- ── GROUP: PRODUCTS ─────────────────────────────────────────────────────────

CREATE TABLE categories (
    id                  UUID         NOT NULL DEFAULT uuid_generate_v4(),
    parent_id           UUID,                            -- -> categories.id (self-ref)
    name                VARCHAR(100) NOT NULL,
    description         TEXT,
    icon_url            TEXT,
    sort_order          INT          NOT NULL DEFAULT 0,
    -- audit
    is_deleted          BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by          UUID,
    updated_by          UUID,
    PRIMARY KEY (id)
);

CREATE TABLE products (
    id                          UUID           NOT NULL DEFAULT uuid_generate_v4(),
    shop_id                     UUID           NOT NULL, -- -> shops.id
    category_id                 UUID           NOT NULL, -- -> categories.id
    name                        VARCHAR(255)   NOT NULL,
    description                 TEXT,
    base_price                  DECIMAL(12,2)  NOT NULL,
    min_order_qty               INT            NOT NULL DEFAULT 1,
    max_order_qty               INT,
    unit                        VARCHAR(50),
    supports_custom_packaging   BOOLEAN        NOT NULL DEFAULT FALSE,
    status                      product_status NOT NULL DEFAULT 'Active',
    -- AI embedding (1536-dim for OpenAI text-embedding-3-small)
    embedding                   vector(1536),
    -- audit
    is_deleted                  BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at                  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at                  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by                  UUID,
    updated_by                  UUID,
    PRIMARY KEY (id)
);

CREATE TABLE product_variants (
    id                  UUID          NOT NULL DEFAULT uuid_generate_v4(),
    product_id          UUID          NOT NULL,          -- -> products.id
    name                VARCHAR(255)  NOT NULL,
    sku                 VARCHAR(100)  NOT NULL,
    flavor              VARCHAR(100),
    filling             VARCHAR(100),
    size_label          VARCHAR(50),
    weight_gram         INT,
    price_adjustment    DECIMAL(12,2) NOT NULL DEFAULT 0,
    stock_qty           INT           NOT NULL DEFAULT 0,
    image_url           TEXT,
    -- audit
    is_deleted          BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by          UUID,
    updated_by          UUID,
    PRIMARY KEY (id),
    UNIQUE (sku)
);

CREATE TABLE product_images (
    id                  UUID         NOT NULL DEFAULT uuid_generate_v4(),
    product_id          UUID         NOT NULL,           -- -> products.id
    image_url           TEXT         NOT NULL,
    is_primary          BOOLEAN      NOT NULL DEFAULT FALSE,
    sort_order          INT          NOT NULL DEFAULT 0,
    -- audit
    is_deleted          BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by          UUID,
    updated_by          UUID,
    PRIMARY KEY (id)
);

-- ── GROUP: PROMOTIONS ───────────────────────────────────────────────────────

CREATE TABLE promotion_rules (
    id                  UUID          NOT NULL DEFAULT uuid_generate_v4(),
    shop_id             UUID          NOT NULL,          -- -> shops.id
    product_id          UUID,                            -- -> products.id (nullable)
    name                VARCHAR(255)  NOT NULL,
    description         TEXT,
    min_qty             INT,
    max_qty             INT,
    discount_type       discount_type NOT NULL,
    discount_value      DECIMAL(12,2) NOT NULL,
    start_date          TIMESTAMPTZ,
    end_date            TIMESTAMPTZ,
    is_active           BOOLEAN       NOT NULL DEFAULT TRUE,
    -- audit
    is_deleted          BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by          UUID,
    updated_by          UUID,
    PRIMARY KEY (id)
);

-- ── GROUP: ORDERS ───────────────────────────────────────────────────────────

CREATE TABLE orders (
    id                      UUID         NOT NULL DEFAULT uuid_generate_v4(),
    customer_id             UUID         NOT NULL, -- -> customer_profiles.id
    shop_id                 UUID         NOT NULL, -- -> shops.id
    order_number            VARCHAR(50)  NOT NULL,
    status                  order_status NOT NULL DEFAULT 'Pending',
    subtotal                DECIMAL(12,2) NOT NULL,
    discount_amount         DECIMAL(12,2) NOT NULL DEFAULT 0,
    tax_amount              DECIMAL(12,2) NOT NULL DEFAULT 0,
    shipping_fee            DECIMAL(12,2) NOT NULL DEFAULT 0,
    total_amount            DECIMAL(12,2) NOT NULL,
    notes                   TEXT,
    delivery_address        TEXT,
    delivery_date_expected  DATE,
    -- audit
    is_deleted              BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at              TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by              UUID,
    updated_by              UUID,
    PRIMARY KEY (id),
    UNIQUE (order_number)
);

CREATE TABLE order_items (
    id                  UUID          NOT NULL DEFAULT uuid_generate_v4(),
    order_id            UUID          NOT NULL, -- -> orders.id
    product_id          UUID          NOT NULL, -- -> products.id
    variant_id          UUID          NOT NULL, -- -> product_variants.id
    promotion_rule_id   UUID,                   -- -> promotion_rules.id (nullable)
    quantity            INT           NOT NULL,
    unit_price          DECIMAL(12,2) NOT NULL,
    discount_amount     DECIMAL(12,2) NOT NULL DEFAULT 0,
    subtotal            DECIMAL(12,2) NOT NULL,
    -- audit
    is_deleted          BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by          UUID,
    updated_by          UUID,
    PRIMARY KEY (id)
);

CREATE TABLE custom_packagings (
    id                  UUID                    NOT NULL DEFAULT uuid_generate_v4(),
    order_id            UUID                    NOT NULL, -- -> orders.id
    company_name        VARCHAR(255),
    company_logo_url    TEXT,
    box_design_url      TEXT,
    packaging_type      VARCHAR(100),
    color_scheme        JSONB,
    special_message     TEXT,
    notes               TEXT,
    status              custom_packaging_status NOT NULL DEFAULT 'Pending',
    -- audit
    is_deleted          BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by          UUID,
    updated_by          UUID,
    PRIMARY KEY (id)
);

-- ── GROUP: RFQ / BIDDING ────────────────────────────────────────────────────

CREATE TABLE request_for_quotations (
    id                      UUID        NOT NULL DEFAULT uuid_generate_v4(),
    customer_id             UUID        NOT NULL, -- -> customer_profiles.id
    rfq_number              VARCHAR(50) NOT NULL,
    title                   VARCHAR(255),
    description             TEXT,
    delivery_address        TEXT,
    delivery_date_required  DATE,
    budget_range_min        DECIMAL(12,2),
    budget_range_max        DECIMAL(12,2),
    bid_deadline            TIMESTAMPTZ,
    status                  rfq_status  NOT NULL DEFAULT 'Draft',
    -- audit
    is_deleted              BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at              TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by              UUID,
    updated_by              UUID,
    PRIMARY KEY (id),
    UNIQUE (rfq_number)
);

CREATE TABLE rfq_items (
    id                      UUID    NOT NULL DEFAULT uuid_generate_v4(),
    rfq_id                  UUID    NOT NULL, -- -> request_for_quotations.id
    product_name            VARCHAR(255),
    description             TEXT,
    quantity                INT     NOT NULL,
    unit                    VARCHAR(50),
    target_price            DECIMAL(12,2),
    specifications          TEXT,
    packaging_requirements  TEXT,
    needs_custom_packaging  BOOLEAN NOT NULL DEFAULT FALSE,
    -- audit
    is_deleted              BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at              TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by              UUID,
    updated_by              UUID,
    PRIMARY KEY (id)
);

CREATE TABLE rfq_invitations (
    id              UUID                  NOT NULL DEFAULT uuid_generate_v4(),
    rfq_id          UUID                  NOT NULL, -- -> request_for_quotations.id
    supplier_id     UUID                  NOT NULL, -- -> supplier_profiles.id
    status          rfq_invitation_status NOT NULL DEFAULT 'Invited',
    invited_at      TIMESTAMPTZ           NOT NULL DEFAULT NOW(),
    responded_at    TIMESTAMPTZ,
    -- audit
    is_deleted      BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by      UUID,
    updated_by      UUID,
    PRIMARY KEY (id),
    UNIQUE (rfq_id, supplier_id)
);

CREATE TABLE bids (
    id                      UUID        NOT NULL DEFAULT uuid_generate_v4(),
    rfq_id                  UUID        NOT NULL, -- -> request_for_quotations.id
    supplier_id             UUID        NOT NULL, -- -> supplier_profiles.id
    bid_number              VARCHAR(50) NOT NULL,
    total_price             DECIMAL(12,2),
    estimated_delivery_days INT,
    notes                   TEXT,
    validity_days           INT         NOT NULL DEFAULT 30,
    status                  bid_status  NOT NULL DEFAULT 'Submitted',
    submitted_at            TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    -- audit
    is_deleted              BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at              TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by              UUID,
    updated_by              UUID,
    PRIMARY KEY (id),
    UNIQUE (bid_number)
);

CREATE TABLE bid_items (
    id              UUID          NOT NULL DEFAULT uuid_generate_v4(),
    bid_id          UUID          NOT NULL, -- -> bids.id
    rfq_item_id     UUID          NOT NULL, -- -> rfq_items.id
    unit_price      DECIMAL(12,2) NOT NULL,
    quantity        INT           NOT NULL,
    subtotal        DECIMAL(12,2) NOT NULL,
    notes           TEXT,
    -- audit
    is_deleted      BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by      UUID,
    updated_by      UUID,
    PRIMARY KEY (id)
);

-- ── GROUP: CONTRACTS ────────────────────────────────────────────────────────

CREATE TABLE contract_deposit_rules (
    id                      UUID          NOT NULL DEFAULT uuid_generate_v4(),
    name                    VARCHAR(255)  NOT NULL,
    description             TEXT,
    deposit_percentage      DECIMAL(5,2)  NOT NULL,
    is_mandatory            BOOLEAN       NOT NULL DEFAULT TRUE,
    applies_to_all          BOOLEAN       NOT NULL DEFAULT TRUE,
    min_contract_value      DECIMAL(12,2),
    payment_deadline_hours  INT           NOT NULL DEFAULT 48,
    -- audit
    is_deleted              BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at              TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by              UUID,
    updated_by              UUID,
    PRIMARY KEY (id)
);

CREATE TABLE contracts (
    id                      UUID            NOT NULL DEFAULT uuid_generate_v4(),
    rfq_id                  UUID,                    -- -> request_for_quotations.id (nullable)
    bid_id                  UUID,                    -- -> bids.id (nullable)
    order_id                UUID,                    -- -> orders.id (nullable)
    customer_id             UUID            NOT NULL, -- -> customer_profiles.id
    supplier_id             UUID            NOT NULL, -- -> supplier_profiles.id
    deposit_rule_id         UUID,                    -- -> contract_deposit_rules.id (nullable)
    contract_number         VARCHAR(50)     NOT NULL,
    title                   VARCHAR(255),
    total_value             DECIMAL(12,2),
    delivery_deadline       DATE,
    terms_and_conditions    TEXT,
    penalty_terms           TEXT,
    status                  contract_status NOT NULL DEFAULT 'Draft',
    deposit_amount          DECIMAL(12,2),
    deposit_paid_at         TIMESTAMPTZ,
    signed_by_customer_at   TIMESTAMPTZ,
    signed_by_supplier_at   TIMESTAMPTZ,
    completed_at            TIMESTAMPTZ,
    -- audit
    is_deleted              BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at              TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by              UUID,
    updated_by              UUID,
    PRIMARY KEY (id),
    UNIQUE (contract_number)
);

CREATE TABLE contract_milestones (
    id                  UUID             NOT NULL DEFAULT uuid_generate_v4(),
    contract_id         UUID             NOT NULL, -- -> contracts.id
    sequence_number     INT              NOT NULL DEFAULT 1,
    name                VARCHAR(255)     NOT NULL,
    description         TEXT,
    is_deposit          BOOLEAN          NOT NULL DEFAULT FALSE,
    due_date            DATE,
    completion_date     DATE,
    amount              DECIMAL(12,2),
    percentage          DECIMAL(5,2),
    status              milestone_status NOT NULL DEFAULT 'Pending',
    -- audit
    is_deleted          BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by          UUID,
    updated_by          UUID,
    PRIMARY KEY (id)
);

-- ── GROUP: DELIVERIES ───────────────────────────────────────────────────────

CREATE TABLE deliveries (
    id                      UUID            NOT NULL DEFAULT uuid_generate_v4(),
    contract_id             UUID,                    -- -> contracts.id (nullable)
    order_id                UUID,                    -- -> orders.id (nullable)
    delivery_number         VARCHAR(50)     NOT NULL,
    delivery_type           delivery_type   NOT NULL,
    scheduled_date          DATE,
    actual_delivery_date    DATE,
    from_address            TEXT,
    to_address              TEXT,
    recipient_name          VARCHAR(255),
    recipient_phone         VARCHAR(20),
    status                  delivery_status NOT NULL DEFAULT 'Pending',
    tracking_code           VARCHAR(100),
    notes                   TEXT,
    -- audit
    is_deleted              BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at              TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by              UUID,
    updated_by              UUID,
    PRIMARY KEY (id),
    UNIQUE (delivery_number)
);

CREATE TABLE delivery_proofs (
    id              UUID       NOT NULL DEFAULT uuid_generate_v4(),
    delivery_id     UUID       NOT NULL, -- -> deliveries.id
    proof_type      proof_type NOT NULL,
    photo_url       TEXT,
    taken_by        VARCHAR(255),
    taken_at        TIMESTAMPTZ,
    location_lat    DECIMAL(10,8),
    location_lng    DECIMAL(11,8),
    signature_url   TEXT,
    notes           TEXT,
    -- audit
    is_deleted      BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by      UUID,
    updated_by      UUID,
    PRIMARY KEY (id)
);

-- ── GROUP: REVIEWS & REPUTATION ─────────────────────────────────────────────

-- Transaction reviews: post order / contract completion
CREATE TABLE reviews (
    id                  UUID    NOT NULL DEFAULT uuid_generate_v4(),
    customer_id         UUID    NOT NULL, -- -> customer_profiles.id
    supplier_id         UUID    NOT NULL, -- -> supplier_profiles.id
    contract_id         UUID,            -- -> contracts.id (nullable)
    order_id            UUID,            -- -> orders.id (nullable)
    overall_rating      SMALLINT,
    quality_rating      SMALLINT,
    delivery_rating     SMALLINT,
    service_rating      SMALLINT,
    comment             TEXT,
    is_public           BOOLEAN NOT NULL DEFAULT TRUE,
    -- audit
    is_deleted          BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by          UUID,
    updated_by          UUID,
    PRIMARY KEY (id)
);

-- Product feed reviews: TikTok/Shopee style comment on product page
CREATE TABLE product_reviews (
    id                   UUID    NOT NULL DEFAULT uuid_generate_v4(),
    customer_id          UUID    NOT NULL, -- -> customer_profiles.id
    product_id           UUID    NOT NULL, -- -> products.id
    product_variant_id   UUID,            -- -> product_variants.id (nullable)
    order_item_id        UUID    NOT NULL, -- -> order_items.id (Verified Purchase gate)
    rating               SMALLINT,
    title                VARCHAR(255),
    content              TEXT,
    media_urls           JSONB,           -- array of {url, type: "image"|"video"}
    is_verified_purchase BOOLEAN NOT NULL DEFAULT TRUE,
    like_count           INT     NOT NULL DEFAULT 0,
    supplier_reply       TEXT,
    supplier_replied_at  TIMESTAMPTZ,
    is_public            BOOLEAN NOT NULL DEFAULT TRUE,
    is_hidden_by_admin   BOOLEAN NOT NULL DEFAULT FALSE,
    -- audit
    is_deleted           BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at           TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at           TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by           UUID,
    updated_by           UUID,
    PRIMARY KEY (id),
    UNIQUE (customer_id, order_item_id)   -- 1 review per verified purchase
);

CREATE TABLE reputation_logs (
    id              UUID             NOT NULL DEFAULT uuid_generate_v4(),
    supplier_id     UUID             NOT NULL, -- -> supplier_profiles.id
    event_type      reputation_event NOT NULL,
    score_delta     DECIMAL(5,2)     NOT NULL,
    reason          TEXT,
    reference_id    UUID,
    reference_type  VARCHAR(50),
    -- audit
    is_deleted      BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by      UUID,
    updated_by      UUID,
    PRIMARY KEY (id)
);

-- ── GROUP: AI MODULE ────────────────────────────────────────────────────────

CREATE TABLE ai_conversations (
    id                  UUID                 NOT NULL DEFAULT uuid_generate_v4(),
    user_id             UUID                 NOT NULL, -- -> users.id
    session_token       VARCHAR(255)         NOT NULL,
    conversation_type   ai_conversation_type NOT NULL DEFAULT 'General',
    ended_at            TIMESTAMPTZ,
    -- audit
    is_deleted          BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by          UUID,
    updated_by          UUID,
    PRIMARY KEY (id),
    UNIQUE (session_token)
);

CREATE TABLE ai_messages (
    id                  UUID            NOT NULL DEFAULT uuid_generate_v4(),
    conversation_id     UUID            NOT NULL, -- -> ai_conversations.id
    role                ai_message_role NOT NULL,
    content             TEXT            NOT NULL,
    tokens_used         INT,
    model_used          VARCHAR(100),
    -- embedding for semantic search over conversation history
    embedding           vector(1536),
    -- audit
    is_deleted          BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by          UUID,
    updated_by          UUID,
    PRIMARY KEY (id)
);

CREATE TABLE ai_analytics_reports (
    id              UUID             NOT NULL DEFAULT uuid_generate_v4(),
    requested_by    UUID             NOT NULL, -- -> users.id
    report_type     ai_report_type   NOT NULL,
    parameters      JSONB,
    result_data     JSONB,
    status          ai_report_status NOT NULL DEFAULT 'Pending',
    completed_at    TIMESTAMPTZ,
    -- audit
    is_deleted      BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by      UUID,
    updated_by      UUID,
    PRIMARY KEY (id)
);

-- ── GROUP: PAYMENTS & NOTIFICATIONS ────────────────────────────────────────

CREATE TABLE payments (
    id              UUID           NOT NULL DEFAULT uuid_generate_v4(),
    order_id        UUID,                   -- -> orders.id (nullable)
    contract_id     UUID,                   -- -> contracts.id (nullable)
    milestone_id    UUID,                   -- -> contract_milestones.id (nullable)
    customer_id     UUID           NOT NULL, -- -> customer_profiles.id
    payment_number  VARCHAR(50)    NOT NULL,
    amount          DECIMAL(12,2)  NOT NULL,
    method          payment_method NOT NULL,
    status          payment_status NOT NULL DEFAULT 'Pending',
    transaction_id  VARCHAR(255),
    paid_at         TIMESTAMPTZ,
    -- audit
    is_deleted      BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by      UUID,
    updated_by      UUID,
    PRIMARY KEY (id),
    UNIQUE (payment_number)
);

CREATE TABLE notifications (
    id              UUID    NOT NULL DEFAULT uuid_generate_v4(),
    user_id         UUID    NOT NULL, -- -> users.id
    type            VARCHAR(100),
    title           VARCHAR(255),
    content         TEXT,
    is_read         BOOLEAN NOT NULL DEFAULT FALSE,
    reference_id    UUID,
    reference_type  VARCHAR(50),
    -- audit
    is_deleted      BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by      UUID,
    updated_by      UUID,
    PRIMARY KEY (id)
);

-- =============================================================================
-- 3. INDEXES
-- =============================================================================

-- ── users ────────────────────────────────────────────────────────────────────
CREATE INDEX idx_users_email      ON users (email)     WHERE is_deleted = FALSE;
CREATE INDEX idx_users_role       ON users (role)      WHERE is_deleted = FALSE;
CREATE INDEX idx_users_is_active  ON users (is_active) WHERE is_deleted = FALSE;

-- ── supplier_profiles ────────────────────────────────────────────────────────
CREATE INDEX idx_sp_user_id           ON supplier_profiles (user_id);
CREATE INDEX idx_sp_reputation_score  ON supplier_profiles (reputation_score DESC) WHERE is_deleted = FALSE;
CREATE INDEX idx_sp_verified          ON supplier_profiles (verified) WHERE is_deleted = FALSE;

-- ── customer_profiles ────────────────────────────────────────────────────────
CREATE INDEX idx_cp_user_id  ON customer_profiles (user_id);

-- ── shops ────────────────────────────────────────────────────────────────────
CREATE INDEX idx_shops_supplier_id  ON shops (supplier_id);
CREATE INDEX idx_shops_template_id  ON shops (template_id);
CREATE INDEX idx_shops_status       ON shops (status) WHERE is_deleted = FALSE;
CREATE INDEX idx_shops_slug         ON shops (slug);

-- ── categories ───────────────────────────────────────────────────────────────
CREATE INDEX idx_categories_parent_id   ON categories (parent_id) WHERE parent_id IS NOT NULL;
CREATE INDEX idx_categories_sort_order  ON categories (sort_order);

-- ── products ─────────────────────────────────────────────────────────────────
CREATE INDEX idx_products_shop_id      ON products (shop_id)      WHERE is_deleted = FALSE;
CREATE INDEX idx_products_category_id  ON products (category_id)  WHERE is_deleted = FALSE;
CREATE INDEX idx_products_status       ON products (status)       WHERE is_deleted = FALSE;
CREATE INDEX idx_products_base_price   ON products (base_price);
-- trigram full-text search on product name
CREATE INDEX idx_products_name_trgm    ON products USING gin (name gin_trgm_ops);
-- pgvector IVFFlat for semantic product search (requires vector extension)
CREATE INDEX idx_products_embedding    ON products USING ivfflat (embedding vector_cosine_ops)
    WITH (lists = 100);

-- ── product_variants ─────────────────────────────────────────────────────────
CREATE INDEX idx_pv_product_id  ON product_variants (product_id) WHERE is_deleted = FALSE;
CREATE INDEX idx_pv_stock_qty   ON product_variants (stock_qty)  WHERE is_deleted = FALSE;

-- ── product_images ───────────────────────────────────────────────────────────
CREATE INDEX idx_pi_product_id   ON product_images (product_id);
CREATE INDEX idx_pi_is_primary   ON product_images (product_id, is_primary);

-- ── promotion_rules ──────────────────────────────────────────────────────────
CREATE INDEX idx_pr_shop_id    ON promotion_rules (shop_id)    WHERE is_deleted = FALSE;
CREATE INDEX idx_pr_product_id ON promotion_rules (product_id) WHERE product_id IS NOT NULL AND is_deleted = FALSE;
CREATE INDEX idx_pr_active     ON promotion_rules (is_active, start_date, end_date) WHERE is_deleted = FALSE;

-- ── orders ───────────────────────────────────────────────────────────────────
CREATE INDEX idx_orders_customer_id  ON orders (customer_id) WHERE is_deleted = FALSE;
CREATE INDEX idx_orders_shop_id      ON orders (shop_id)     WHERE is_deleted = FALSE;
CREATE INDEX idx_orders_status       ON orders (status)      WHERE is_deleted = FALSE;
CREATE INDEX idx_orders_created_at   ON orders (created_at DESC);

-- ── order_items ──────────────────────────────────────────────────────────────
CREATE INDEX idx_oi_order_id          ON order_items (order_id);
CREATE INDEX idx_oi_product_id        ON order_items (product_id);
CREATE INDEX idx_oi_variant_id        ON order_items (variant_id);
CREATE INDEX idx_oi_promotion_rule_id ON order_items (promotion_rule_id) WHERE promotion_rule_id IS NOT NULL;

-- ── custom_packagings ────────────────────────────────────────────────────────
CREATE INDEX idx_cpkg_order_id  ON custom_packagings (order_id);
CREATE INDEX idx_cpkg_status    ON custom_packagings (status) WHERE is_deleted = FALSE;

-- ── request_for_quotations ───────────────────────────────────────────────────
CREATE INDEX idx_rfq_customer_id   ON request_for_quotations (customer_id) WHERE is_deleted = FALSE;
CREATE INDEX idx_rfq_status        ON request_for_quotations (status)      WHERE is_deleted = FALSE;
CREATE INDEX idx_rfq_bid_deadline  ON request_for_quotations (bid_deadline) WHERE is_deleted = FALSE;

-- ── rfq_items ────────────────────────────────────────────────────────────────
CREATE INDEX idx_rfqi_rfq_id  ON rfq_items (rfq_id);

-- ── rfq_invitations ──────────────────────────────────────────────────────────
CREATE INDEX idx_rfqinv_rfq_id      ON rfq_invitations (rfq_id);
CREATE INDEX idx_rfqinv_supplier_id ON rfq_invitations (supplier_id);
CREATE INDEX idx_rfqinv_status      ON rfq_invitations (status);

-- ── bids ─────────────────────────────────────────────────────────────────────
CREATE INDEX idx_bids_rfq_id      ON bids (rfq_id)      WHERE is_deleted = FALSE;
CREATE INDEX idx_bids_supplier_id ON bids (supplier_id) WHERE is_deleted = FALSE;
CREATE INDEX idx_bids_status      ON bids (status)      WHERE is_deleted = FALSE;
CREATE INDEX idx_bids_total_price ON bids (total_price);

-- ── bid_items ────────────────────────────────────────────────────────────────
CREATE INDEX idx_bi_bid_id      ON bid_items (bid_id);
CREATE INDEX idx_bi_rfq_item_id ON bid_items (rfq_item_id);

-- ── contract_deposit_rules ───────────────────────────────────────────────────
CREATE INDEX idx_cdr_is_mandatory  ON contract_deposit_rules (is_mandatory) WHERE is_deleted = FALSE;

-- ── contracts ────────────────────────────────────────────────────────────────
CREATE INDEX idx_contracts_customer_id      ON contracts (customer_id)    WHERE is_deleted = FALSE;
CREATE INDEX idx_contracts_supplier_id      ON contracts (supplier_id)    WHERE is_deleted = FALSE;
CREATE INDEX idx_contracts_status           ON contracts (status)         WHERE is_deleted = FALSE;
CREATE INDEX idx_contracts_rfq_id           ON contracts (rfq_id)         WHERE rfq_id IS NOT NULL;
CREATE INDEX idx_contracts_bid_id           ON contracts (bid_id)         WHERE bid_id IS NOT NULL;
CREATE INDEX idx_contracts_order_id         ON contracts (order_id)       WHERE order_id IS NOT NULL;
CREATE INDEX idx_contracts_deposit_rule_id  ON contracts (deposit_rule_id) WHERE deposit_rule_id IS NOT NULL;
CREATE INDEX idx_contracts_pending_deposit  ON contracts (status, deposit_paid_at)
    WHERE status = 'PendingDeposit' AND is_deleted = FALSE;

-- ── contract_milestones ──────────────────────────────────────────────────────
CREATE INDEX idx_cm_contract_id   ON contract_milestones (contract_id);
CREATE INDEX idx_cm_status        ON contract_milestones (status)     WHERE is_deleted = FALSE;
CREATE INDEX idx_cm_due_date      ON contract_milestones (due_date)   WHERE is_deleted = FALSE;
CREATE INDEX idx_cm_is_deposit    ON contract_milestones (contract_id, is_deposit);

-- ── deliveries ───────────────────────────────────────────────────────────────
CREATE INDEX idx_del_contract_id   ON deliveries (contract_id)  WHERE contract_id IS NOT NULL;
CREATE INDEX idx_del_order_id      ON deliveries (order_id)     WHERE order_id IS NOT NULL;
CREATE INDEX idx_del_status        ON deliveries (status)       WHERE is_deleted = FALSE;
CREATE INDEX idx_del_tracking_code ON deliveries (tracking_code) WHERE tracking_code IS NOT NULL;

-- ── delivery_proofs ──────────────────────────────────────────────────────────
CREATE INDEX idx_dp_delivery_id  ON delivery_proofs (delivery_id);
CREATE INDEX idx_dp_proof_type   ON delivery_proofs (delivery_id, proof_type);

-- ── reviews ──────────────────────────────────────────────────────────────────
CREATE INDEX idx_rev_customer_id    ON reviews (customer_id) WHERE is_deleted = FALSE;
CREATE INDEX idx_rev_supplier_id    ON reviews (supplier_id) WHERE is_deleted = FALSE;
CREATE INDEX idx_rev_contract_id    ON reviews (contract_id) WHERE contract_id IS NOT NULL;
CREATE INDEX idx_rev_order_id       ON reviews (order_id)    WHERE order_id IS NOT NULL;
CREATE INDEX idx_rev_overall_rating ON reviews (supplier_id, overall_rating) WHERE is_deleted = FALSE;

-- ── product_reviews ──────────────────────────────────────────────────────────
-- primary feed query: product page, newest first, visible only
CREATE INDEX idx_prev_product_feed   ON product_reviews (product_id, created_at DESC)
    WHERE is_deleted = FALSE AND is_hidden_by_admin = FALSE;
CREATE INDEX idx_prev_variant_id     ON product_reviews (product_variant_id)
    WHERE product_variant_id IS NOT NULL AND is_deleted = FALSE;
CREATE INDEX idx_prev_customer_id    ON product_reviews (customer_id) WHERE is_deleted = FALSE;
CREATE INDEX idx_prev_rating         ON product_reviews (product_id, rating) WHERE is_deleted = FALSE;
CREATE INDEX idx_prev_like_count     ON product_reviews (product_id, like_count DESC) WHERE is_deleted = FALSE;
CREATE INDEX idx_prev_admin_hidden   ON product_reviews (is_hidden_by_admin) WHERE is_hidden_by_admin = TRUE;

-- ── reputation_logs ──────────────────────────────────────────────────────────
CREATE INDEX idx_rl_supplier_id    ON reputation_logs (supplier_id, created_at DESC);
CREATE INDEX idx_rl_event_type     ON reputation_logs (event_type);
CREATE INDEX idx_rl_reference      ON reputation_logs (reference_id, reference_type)
    WHERE reference_id IS NOT NULL;

-- ── ai_conversations ─────────────────────────────────────────────────────────
CREATE INDEX idx_aic_user_id  ON ai_conversations (user_id)            WHERE is_deleted = FALSE;
CREATE INDEX idx_aic_type     ON ai_conversations (conversation_type)  WHERE is_deleted = FALSE;

-- ── ai_messages ──────────────────────────────────────────────────────────────
CREATE INDEX idx_aim_conversation_id ON ai_messages (conversation_id, created_at) WHERE is_deleted = FALSE;
-- semantic search over chat history
CREATE INDEX idx_aim_embedding ON ai_messages USING ivfflat (embedding vector_cosine_ops)
    WITH (lists = 50);

-- ── ai_analytics_reports ────────────────────────────────────────────────────
CREATE INDEX idx_aar_requested_by ON ai_analytics_reports (requested_by) WHERE is_deleted = FALSE;
CREATE INDEX idx_aar_status       ON ai_analytics_reports (status)       WHERE is_deleted = FALSE;
CREATE INDEX idx_aar_report_type  ON ai_analytics_reports (report_type);

-- ── payments ─────────────────────────────────────────────────────────────────
CREATE INDEX idx_pay_order_id      ON payments (order_id)     WHERE order_id IS NOT NULL;
CREATE INDEX idx_pay_contract_id   ON payments (contract_id)  WHERE contract_id IS NOT NULL;
CREATE INDEX idx_pay_milestone_id  ON payments (milestone_id) WHERE milestone_id IS NOT NULL;
CREATE INDEX idx_pay_customer_id   ON payments (customer_id)  WHERE is_deleted = FALSE;
CREATE INDEX idx_pay_status        ON payments (status)       WHERE is_deleted = FALSE;
CREATE INDEX idx_pay_paid_at       ON payments (paid_at DESC) WHERE paid_at IS NOT NULL;

-- ── notifications ────────────────────────────────────────────────────────────
CREATE INDEX idx_notif_user_id   ON notifications (user_id, created_at DESC) WHERE is_deleted = FALSE;
CREATE INDEX idx_notif_unread    ON notifications (user_id, is_read)
    WHERE is_deleted = FALSE AND is_read = FALSE;
CREATE INDEX idx_notif_type      ON notifications (type) WHERE is_deleted = FALSE;

-- ============================================= ================================
-- END OF SCRIPT
-- Total tables  : 31
-- Total ENUMs   : 19 types
-- Extensions    : uuid-ossp | vector (pgvector) | pg_trgm
-- Vector cols   : products.embedding, ai_messages.embedding (1536-dim)
-- Total indexes : ~80
-- =============================================================================
