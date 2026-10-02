-- =====================================================================
--  MOONCAKE MARKETPLACE - SCHEMA POSTGRESQL
-- =====================================================================

BEGIN;

CREATE TABLE users (
    id            BIGINT       GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    email         VARCHAR(255) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role          VARCHAR(20)  NOT NULL,
    full_name     VARCHAR(255) NOT NULL,
    phone         VARCHAR(30)  NULL,
    is_active     BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at    TIMESTAMPTZ(3) NOT NULL DEFAULT NOW(),
    updated_at    TIMESTAMPTZ(3) NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_users_email UNIQUE (email),
    CONSTRAINT ck_users_role CHECK (role IN ('customer', 'supplier', 'admin'))
);

CREATE TABLE customers (
    id              BIGINT       GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    user_id         BIGINT       NOT NULL,
    customer_type   VARCHAR(20)  NOT NULL DEFAULT 'individual',
    company_name    VARCHAR(255) NULL,
    tax_code        VARCHAR(50)  NULL,
    default_address TEXT         NULL,
    created_at      TIMESTAMPTZ(3) NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ(3) NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_customers_user UNIQUE (user_id),
    CONSTRAINT fk_customers_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE,
    CONSTRAINT ck_customers_type CHECK (customer_type IN ('individual', 'company')),
    CONSTRAINT ck_customers_company CHECK (customer_type = 'individual' OR company_name IS NOT NULL)
);

CREATE TABLE suppliers (
    id               BIGINT       GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    user_id          BIGINT       NOT NULL,
    business_name    VARCHAR(255) NOT NULL,
    description      TEXT         NULL,
    address          TEXT         NULL,
    tax_code         VARCHAR(50)  NULL,
    is_verified      BOOLEAN      NOT NULL DEFAULT FALSE,
    reputation_score INT          NOT NULL DEFAULT 0,
    created_at       TIMESTAMPTZ(3) NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMPTZ(3) NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_suppliers_user UNIQUE (user_id),
    CONSTRAINT fk_suppliers_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
);

CREATE TABLE shop_templates (
    id          BIGINT       GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    name        VARCHAR(255) NOT NULL,
    description TEXT         NULL,
    preview_url VARCHAR(1000) NULL,
    config      JSONB        NOT NULL DEFAULT '{}',
    is_active   BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMPTZ(3) NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ(3) NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_shop_templates_name UNIQUE (name)
);

CREATE TABLE shops (
    id                 BIGINT       GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    supplier_id        BIGINT       NOT NULL,
    template_id        BIGINT       NULL,
    template_overrides JSONB        NOT NULL DEFAULT '{}',
    name               VARCHAR(255) NOT NULL,
    slug               VARCHAR(255) NOT NULL,
    description        TEXT         NULL,
    logo_url           VARCHAR(1000) NULL,
    banner_url         VARCHAR(1000) NULL,
    is_active          BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at         TIMESTAMPTZ(3) NOT NULL DEFAULT NOW(),
    updated_at         TIMESTAMPTZ(3) NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_shops_supplier UNIQUE (supplier_id),
    CONSTRAINT uq_shops_slug UNIQUE (slug),
    CONSTRAINT fk_shops_supplier FOREIGN KEY (supplier_id) REFERENCES suppliers (id) ON DELETE CASCADE,
    CONSTRAINT fk_shops_template FOREIGN KEY (template_id) REFERENCES shop_templates (id) ON DELETE SET NULL
);

CREATE TABLE categories (
    id          BIGINT       GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    parent_id   BIGINT       NULL,
    name        VARCHAR(255) NOT NULL,
    slug        VARCHAR(255) NOT NULL,
    description TEXT         NULL,
    created_at  TIMESTAMPTZ(3) NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_categories_slug UNIQUE (slug),
    CONSTRAINT fk_categories_parent FOREIGN KEY (parent_id) REFERENCES categories (id) ON DELETE NO ACTION
);

CREATE TABLE products (
    id                   BIGINT        GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    shop_id              BIGINT        NOT NULL,
    category_id          BIGINT        NULL,
    name                 VARCHAR(255)  NOT NULL,
    description          TEXT          NULL,
    custom_packaging_fee DECIMAL(12,2) NULL,
    is_active            BOOLEAN       NOT NULL DEFAULT TRUE,
    created_at           TIMESTAMPTZ(3)  NOT NULL DEFAULT NOW(),
    updated_at           TIMESTAMPTZ(3)  NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_products_shop     FOREIGN KEY (shop_id)     REFERENCES shops (id)      ON DELETE CASCADE,
    CONSTRAINT fk_products_category FOREIGN KEY (category_id) REFERENCES categories (id) ON DELETE SET NULL,
    CONSTRAINT ck_products_pack_fee CHECK (custom_packaging_fee IS NULL OR custom_packaging_fee >= 0)
);

CREATE TABLE product_variants (
    id                 BIGINT       GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    product_id         BIGINT       NOT NULL,
    sku                VARCHAR(100) NULL,
    name               VARCHAR(255) NOT NULL,
    price              DECIMAL(12,2) NOT NULL,
    stock_quantity     INT          NOT NULL DEFAULT 0,
    min_order_quantity INT          NOT NULL DEFAULT 1,
    is_active          BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at         TIMESTAMPTZ(3) NOT NULL DEFAULT NOW(),
    updated_at         TIMESTAMPTZ(3) NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_variants_product FOREIGN KEY (product_id) REFERENCES products (id) ON DELETE CASCADE,
    CONSTRAINT ck_variants_price CHECK (price >= 0),
    CONSTRAINT ck_variants_stock CHECK (stock_quantity >= 0),
    CONSTRAINT ck_variants_minqty CHECK (min_order_quantity > 0)
);
CREATE UNIQUE INDEX uq_variants_sku ON product_variants (sku) WHERE sku IS NOT NULL;

CREATE TABLE product_images (
    id         BIGINT        GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    product_id BIGINT        NOT NULL,
    url        VARCHAR(1000) NOT NULL,
    sort_order INT           NOT NULL DEFAULT 0,
    is_primary BOOLEAN       NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ(3)  NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_product_images_product FOREIGN KEY (product_id) REFERENCES products (id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX uq_product_primary_image ON product_images (product_id) WHERE is_primary = TRUE;

CREATE TABLE promotion_rules (
    id               BIGINT       GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    shop_id          BIGINT       NOT NULL,
    product_id       BIGINT       NULL,
    name             VARCHAR(255) NOT NULL,
    discount_type    VARCHAR(20)  NOT NULL,
    min_quantity     INT          NOT NULL,
    discount_percent DECIMAL(5,2) NULL,
    discount_amount  DECIMAL(12,2) NULL,
    free_quantity    INT          NULL,
    starts_at        TIMESTAMPTZ(3) NULL,
    ends_at          TIMESTAMPTZ(3) NULL,
    is_active        BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at       TIMESTAMPTZ(3) NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMPTZ(3) NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_promo_shop    FOREIGN KEY (shop_id)    REFERENCES shops (id)    ON DELETE CASCADE,
    CONSTRAINT fk_promo_product FOREIGN KEY (product_id) REFERENCES products (id) ON DELETE NO ACTION,
    CONSTRAINT ck_promo_type   CHECK (discount_type IN ('percent', 'fixed_amount', 'buy_x_get_y')),
    CONSTRAINT ck_promo_minqty CHECK (min_quantity > 0),
    CONSTRAINT ck_promo_values CHECK (
        (discount_type = 'percent'      AND discount_percent IS NOT NULL
                                        AND discount_percent > 0 AND discount_percent <= 100)
     OR (discount_type = 'fixed_amount' AND discount_amount IS NOT NULL AND discount_amount > 0)
     OR (discount_type = 'buy_x_get_y'  AND free_quantity IS NOT NULL AND free_quantity > 0)
    ),
    CONSTRAINT ck_promo_dates CHECK (starts_at IS NULL OR ends_at IS NULL OR ends_at > starts_at)
);

CREATE TABLE custom_packagings (
    id           BIGINT        GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    customer_id  BIGINT        NOT NULL,
    name         VARCHAR(255)  NOT NULL,
    logo_url     VARCHAR(1000) NOT NULL,
    design_notes TEXT          NULL,
    created_at   TIMESTAMPTZ(3)  NOT NULL DEFAULT NOW(),
    updated_at   TIMESTAMPTZ(3)  NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_packagings_customer FOREIGN KEY (customer_id) REFERENCES customers (id) ON DELETE CASCADE
);

CREATE TABLE orders (
    id                     BIGINT        GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    customer_id            BIGINT        NOT NULL,
    shop_id                BIGINT        NOT NULL,
    status                 VARCHAR(30)   NOT NULL DEFAULT 'pending',
    subtotal               DECIMAL(14,2) NOT NULL DEFAULT 0,
    discount_total         DECIMAL(14,2) NOT NULL DEFAULT 0,
    shipping_fee           DECIMAL(12,2) NOT NULL DEFAULT 0,
    total_amount           DECIMAL(14,2) NOT NULL DEFAULT 0,
    deposit_required       DECIMAL(14,2) NOT NULL DEFAULT 0,
    receiver_name          VARCHAR(255)  NOT NULL,
    receiver_phone         VARCHAR(30)   NOT NULL,
    shipping_address       TEXT          NOT NULL,
    required_delivery_date DATE          NULL,
    note                   TEXT          NULL,
    created_at             TIMESTAMPTZ(3)  NOT NULL DEFAULT NOW(),
    updated_at             TIMESTAMPTZ(3)  NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_orders_customer FOREIGN KEY (customer_id) REFERENCES customers (id),
    CONSTRAINT fk_orders_shop     FOREIGN KEY (shop_id)     REFERENCES shops (id),
    CONSTRAINT ck_orders_status CHECK (status IN ('pending', 'awaiting_deposit', 'confirmed', 'preparing',
                                                  'shipping', 'delivered', 'completed', 'cancelled')),
    CONSTRAINT ck_orders_amounts CHECK (subtotal >= 0 AND discount_total >= 0 AND shipping_fee >= 0
                                        AND total_amount >= 0 AND deposit_required >= 0),
    CONSTRAINT ck_orders_total   CHECK (total_amount = subtotal - discount_total + shipping_fee),
    CONSTRAINT ck_orders_deposit CHECK (deposit_required <= total_amount)
);

CREATE TABLE order_items (
    id                  BIGINT        GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    order_id            BIGINT        NOT NULL,
    variant_id          BIGINT        NOT NULL,
    promotion_rule_id   BIGINT        NULL,
    custom_packaging_id BIGINT        NULL,
    quantity            INT           NOT NULL,
    unit_price          DECIMAL(12,2) NOT NULL,
    packaging_fee       DECIMAL(12,2) NOT NULL DEFAULT 0,
    discount_amount     DECIMAL(14,2) NOT NULL DEFAULT 0,
    line_total          DECIMAL(14,2) NOT NULL,
    created_at          TIMESTAMPTZ(3)  NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_oi_order     FOREIGN KEY (order_id)            REFERENCES orders (id)            ON DELETE CASCADE,
    CONSTRAINT fk_oi_variant   FOREIGN KEY (variant_id)          REFERENCES product_variants (id),
    CONSTRAINT fk_oi_promo     FOREIGN KEY (promotion_rule_id)   REFERENCES promotion_rules (id)   ON DELETE SET NULL,
    CONSTRAINT fk_oi_packaging FOREIGN KEY (custom_packaging_id) REFERENCES custom_packagings (id),
    CONSTRAINT ck_oi_values CHECK (quantity > 0 AND unit_price >= 0 AND packaging_fee >= 0
                                   AND discount_amount >= 0 AND line_total >= 0),
    CONSTRAINT ck_oi_line_total CHECK (line_total = quantity * (unit_price + packaging_fee) - discount_amount)
);

CREATE TABLE request_for_quotations (
    id                     BIGINT       GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    customer_id            BIGINT       NOT NULL,
    title                  VARCHAR(255) NOT NULL,
    description            TEXT         NULL,
    visibility             VARCHAR(20)  NOT NULL DEFAULT 'invite_only',
    status                 VARCHAR(20)  NOT NULL DEFAULT 'draft',
    quote_deadline         TIMESTAMPTZ(3) NULL,
    required_delivery_date DATE         NULL,
    delivery_address       TEXT         NOT NULL,
    created_at             TIMESTAMPTZ(3) NOT NULL DEFAULT NOW(),
    updated_at             TIMESTAMPTZ(3) NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_rfq_customer FOREIGN KEY (customer_id) REFERENCES customers (id),
    CONSTRAINT ck_rfq_visibility CHECK (visibility IN ('open', 'invite_only')),
    CONSTRAINT ck_rfq_status CHECK (status IN ('draft', 'open', 'awarded', 'cancelled', 'expired'))
);

CREATE TABLE rfq_items (
    id                  BIGINT       GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    rfq_id              BIGINT       NOT NULL,
    product_id          BIGINT       NULL,
    item_name           VARCHAR(255) NOT NULL,
    specification       TEXT         NULL,
    quantity            INT          NOT NULL,
    custom_packaging_id BIGINT       NULL,
    note                TEXT         NULL,
    created_at          TIMESTAMPTZ(3) NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_rfq_items_rfq       FOREIGN KEY (rfq_id)              REFERENCES request_for_quotations (id) ON DELETE CASCADE,
    CONSTRAINT fk_rfq_items_product   FOREIGN KEY (product_id)          REFERENCES products (id)               ON DELETE SET NULL,
    CONSTRAINT fk_rfq_items_packaging FOREIGN KEY (custom_packaging_id) REFERENCES custom_packagings (id),
    CONSTRAINT ck_rfq_items_qty CHECK (quantity > 0)
);

CREATE TABLE rfq_invitations (
    id           BIGINT       GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    rfq_id       BIGINT       NOT NULL,
    supplier_id  BIGINT       NOT NULL,
    status       VARCHAR(20)  NOT NULL DEFAULT 'invited',
    invited_at   TIMESTAMPTZ(3) NOT NULL DEFAULT NOW(),
    responded_at TIMESTAMPTZ(3) NULL,
    CONSTRAINT uq_rfq_invitations UNIQUE (rfq_id, supplier_id),
    CONSTRAINT fk_rfq_inv_rfq      FOREIGN KEY (rfq_id)      REFERENCES request_for_quotations (id) ON DELETE CASCADE,
    CONSTRAINT fk_rfq_inv_supplier FOREIGN KEY (supplier_id) REFERENCES suppliers (id)              ON DELETE CASCADE,
    CONSTRAINT ck_rfq_inv_status CHECK (status IN ('invited', 'viewed', 'declined', 'quoted'))
);

CREATE TABLE quotations (
    id                       BIGINT        GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    rfq_id                   BIGINT        NOT NULL,
    supplier_id              BIGINT        NOT NULL,
    status                   VARCHAR(20)   NOT NULL DEFAULT 'submitted',
    total_amount             DECIMAL(14,2) NOT NULL,
    lead_time_days           INT           NULL,
    proposed_deposit_percent DECIMAL(5,2)  NOT NULL DEFAULT 30,
    valid_until              TIMESTAMPTZ(3)  NULL,
    notes                    TEXT          NULL,
    created_at               TIMESTAMPTZ(3)  NOT NULL DEFAULT NOW(),
    updated_at               TIMESTAMPTZ(3)  NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_quotations_rfq_supplier UNIQUE (rfq_id, supplier_id),
    CONSTRAINT fk_quotations_rfq      FOREIGN KEY (rfq_id)      REFERENCES request_for_quotations (id) ON DELETE CASCADE,
    CONSTRAINT fk_quotations_supplier FOREIGN KEY (supplier_id) REFERENCES suppliers (id),
    CONSTRAINT ck_quotations_status CHECK (status IN ('submitted', 'negotiating', 'accepted',
                                                      'rejected', 'withdrawn', 'expired')),
    CONSTRAINT ck_quotations_amount CHECK (total_amount >= 0),
    CONSTRAINT ck_quotations_lead   CHECK (lead_time_days IS NULL OR lead_time_days >= 0),
    CONSTRAINT ck_quotations_deposit CHECK (proposed_deposit_percent BETWEEN 0 AND 100)
);
CREATE UNIQUE INDEX uq_one_accepted_quotation_per_rfq ON quotations (rfq_id) WHERE status = 'accepted';

CREATE TABLE quotation_items (
    id           BIGINT        GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    quotation_id BIGINT        NOT NULL,
    rfq_item_id  BIGINT        NOT NULL,
    unit_price   DECIMAL(12,2) NOT NULL,
    quantity     INT           NOT NULL,
    line_total   DECIMAL(14,2) NOT NULL,
    note         TEXT          NULL,
    CONSTRAINT uq_quotation_items UNIQUE (quotation_id, rfq_item_id),
    CONSTRAINT fk_qi_quotation FOREIGN KEY (quotation_id) REFERENCES quotations (id) ON DELETE CASCADE,
    CONSTRAINT fk_qi_rfq_item  FOREIGN KEY (rfq_item_id)  REFERENCES rfq_items (id),
    CONSTRAINT ck_qi_values CHECK (unit_price >= 0 AND quantity > 0 AND line_total >= 0),
    CONSTRAINT ck_qi_line_total CHECK (line_total = quantity * unit_price)
);

CREATE TABLE price_negotiations (
    id              BIGINT        GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    quotation_id    BIGINT        NOT NULL,
    round_no        INT           NOT NULL,
    proposed_by     VARCHAR(20)   NOT NULL,
    proposed_amount DECIMAL(14,2) NOT NULL,
    message         TEXT          NULL,
    status          VARCHAR(20)   NOT NULL DEFAULT 'pending',
    created_at      TIMESTAMPTZ(3)  NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_negotiation_round UNIQUE (quotation_id, round_no),
    CONSTRAINT fk_negotiations_quotation FOREIGN KEY (quotation_id) REFERENCES quotations (id) ON DELETE CASCADE,
    CONSTRAINT ck_negotiations_round  CHECK (round_no > 0),
    CONSTRAINT ck_negotiations_by     CHECK (proposed_by IN ('customer', 'supplier')),
    CONSTRAINT ck_negotiations_amount CHECK (proposed_amount >= 0),
    CONSTRAINT ck_negotiations_status CHECK (status IN ('pending', 'accepted', 'rejected', 'superseded'))
);

CREATE TABLE contracts (
    id                           BIGINT        GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    quotation_id                 BIGINT        NOT NULL,
    customer_id                  BIGINT        NOT NULL,
    supplier_id                  BIGINT        NOT NULL,
    status                       VARCHAR(20)   NOT NULL DEFAULT 'draft',
    total_amount                 DECIMAL(14,2) NOT NULL,
    deposit_percent              DECIMAL(5,2)  NOT NULL DEFAULT 30,
    delivery_deadline            DATE          NOT NULL,
    late_penalty_percent_per_day DECIMAL(5,2)  NOT NULL DEFAULT 0,
    max_penalty_percent          DECIMAL(5,2)  NOT NULL DEFAULT 0,
    terms                        TEXT          NULL,
    customer_signed_at           TIMESTAMPTZ(3)  NULL,
    supplier_signed_at           TIMESTAMPTZ(3)  NULL,
    completed_at                 TIMESTAMPTZ(3)  NULL,
    created_at                   TIMESTAMPTZ(3)  NOT NULL DEFAULT NOW(),
    updated_at                   TIMESTAMPTZ(3)  NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_contracts_quotation UNIQUE (quotation_id),
    CONSTRAINT fk_contracts_quotation FOREIGN KEY (quotation_id) REFERENCES quotations (id),
    CONSTRAINT fk_contracts_customer  FOREIGN KEY (customer_id)  REFERENCES customers (id),
    CONSTRAINT fk_contracts_supplier  FOREIGN KEY (supplier_id)  REFERENCES suppliers (id),
    CONSTRAINT ck_contracts_status CHECK (status IN ('draft', 'active', 'completed', 'cancelled', 'breached')),
    CONSTRAINT ck_contracts_amount CHECK (total_amount >= 0),
    CONSTRAINT ck_contracts_deposit CHECK (deposit_percent BETWEEN 0 AND 100),
    CONSTRAINT ck_contracts_penalty_day CHECK (late_penalty_percent_per_day BETWEEN 0 AND 100),
    CONSTRAINT ck_contracts_penalty_max CHECK (max_penalty_percent BETWEEN 0 AND 100)
);

CREATE TABLE contract_milestones (
    id             BIGINT        GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    contract_id    BIGINT        NOT NULL,
    milestone_no   SMALLINT      NOT NULL,
    name           VARCHAR(255)  NOT NULL,
    milestone_type VARCHAR(20)   NOT NULL DEFAULT 'progress',
    amount         DECIMAL(14,2) NOT NULL,
    due_date       DATE          NULL,
    status         VARCHAR(20)   NOT NULL DEFAULT 'pending',
    paid_at        TIMESTAMPTZ(3)  NULL,
    created_at     TIMESTAMPTZ(3)  NOT NULL DEFAULT NOW(),
    updated_at     TIMESTAMPTZ(3)  NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_milestone_no UNIQUE (contract_id, milestone_no),
    CONSTRAINT fk_milestones_contract FOREIGN KEY (contract_id) REFERENCES contracts (id) ON DELETE CASCADE,
    CONSTRAINT ck_milestones_no     CHECK (milestone_no > 0),
    CONSTRAINT ck_milestones_type   CHECK (milestone_type IN ('deposit', 'progress', 'final')),
    CONSTRAINT ck_milestones_amount CHECK (amount > 0),
    CONSTRAINT ck_milestones_status CHECK (status IN ('pending', 'paid', 'overdue', 'cancelled'))
);
CREATE UNIQUE INDEX uq_one_deposit_per_contract ON contract_milestones (contract_id)
    WHERE milestone_type = 'deposit';

CREATE TABLE payments (
    id              BIGINT        GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    order_id        BIGINT        NULL,
    milestone_id    BIGINT        NULL,
    payment_type    VARCHAR(20)   NOT NULL,
    amount          DECIMAL(14,2) NOT NULL,
    method          VARCHAR(30)   NOT NULL,
    status          VARCHAR(20)   NOT NULL DEFAULT 'pending',
    transaction_ref VARCHAR(100)  NULL,
    paid_at         TIMESTAMPTZ(3)  NULL,
    created_at      TIMESTAMPTZ(3)  NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ(3)  NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_payments_order     FOREIGN KEY (order_id)     REFERENCES orders (id),
    CONSTRAINT fk_payments_milestone FOREIGN KEY (milestone_id) REFERENCES contract_milestones (id),
    CONSTRAINT ck_payments_type   CHECK (payment_type IN ('deposit', 'balance', 'full', 'refund')),
    CONSTRAINT ck_payments_amount CHECK (amount > 0),
    CONSTRAINT ck_payments_method CHECK (method IN ('bank_transfer', 'credit_card', 'e_wallet', 'cod')),
    CONSTRAINT ck_payments_status CHECK (status IN ('pending', 'succeeded', 'failed', 'refunded', 'cancelled')),
    CONSTRAINT ck_payments_target CHECK ((order_id IS NOT NULL AND milestone_id IS NULL)
                                      OR (order_id IS NULL AND milestone_id IS NOT NULL))
);
CREATE UNIQUE INDEX uq_payments_transaction_ref ON payments (transaction_ref) WHERE transaction_ref IS NOT NULL;

CREATE TABLE deliveries (
    id               BIGINT        GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    order_id         BIGINT        NULL,
    contract_id      BIGINT        NULL,
    direction        VARCHAR(30)   NOT NULL DEFAULT 'supplier_to_customer',
    status           VARCHAR(20)   NOT NULL DEFAULT 'pending',
    carrier_name     VARCHAR(100)  NULL,
    tracking_code    VARCHAR(100)  NULL,
    delivery_address TEXT          NOT NULL,
    recipient_name   VARCHAR(255)  NOT NULL,
    recipient_phone  VARCHAR(30)   NOT NULL,
    scheduled_at     TIMESTAMPTZ(3)  NULL,
    shipped_at       TIMESTAMPTZ(3)  NULL,
    delivered_at     TIMESTAMPTZ(3)  NULL,
    note             TEXT          NULL,
    created_at       TIMESTAMPTZ(3)  NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMPTZ(3)  NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_deliveries_order    FOREIGN KEY (order_id)    REFERENCES orders (id),
    CONSTRAINT fk_deliveries_contract FOREIGN KEY (contract_id) REFERENCES contracts (id),
    CONSTRAINT ck_deliveries_direction CHECK (direction IN ('supplier_to_customer', 'customer_to_supplier')),
    CONSTRAINT ck_deliveries_status    CHECK (status IN ('pending', 'in_transit', 'delivered', 'failed', 'returned')),
    CONSTRAINT ck_deliveries_target    CHECK ((order_id IS NOT NULL AND contract_id IS NULL)
                                           OR (order_id IS NULL AND contract_id IS NOT NULL))
);

CREATE TABLE delivery_proofs (
    id               BIGINT        GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    delivery_id      BIGINT        NOT NULL,
    proof_type       VARCHAR(20)   NOT NULL,
    photo_url        VARCHAR(1000) NOT NULL,
    taken_by_user_id BIGINT        NULL,
    taken_at         TIMESTAMPTZ(3)  NOT NULL DEFAULT NOW(),
    latitude         DECIMAL(9,6)  NULL,
    longitude        DECIMAL(9,6)  NULL,
    note             TEXT          NULL,
    CONSTRAINT fk_proofs_delivery FOREIGN KEY (delivery_id)      REFERENCES deliveries (id) ON DELETE CASCADE,
    CONSTRAINT fk_proofs_user     FOREIGN KEY (taken_by_user_id) REFERENCES users (id)      ON DELETE SET NULL,
    CONSTRAINT ck_proofs_type CHECK (proof_type IN ('pickup', 'delivery')),
    CONSTRAINT ck_proofs_lat  CHECK (latitude  IS NULL OR latitude  BETWEEN -90  AND 90),
    CONSTRAINT ck_proofs_lng  CHECK (longitude IS NULL OR longitude BETWEEN -180 AND 180)
);

CREATE TABLE reviews (
    id          BIGINT        GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    customer_id BIGINT        NOT NULL,
    supplier_id BIGINT        NOT NULL,
    order_id    BIGINT        NULL,
    contract_id BIGINT        NULL,
    rating      SMALLINT      NOT NULL,
    comment     TEXT          NULL,
    created_at  TIMESTAMPTZ(3)  NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_reviews_customer FOREIGN KEY (customer_id) REFERENCES customers (id),
    CONSTRAINT fk_reviews_supplier FOREIGN KEY (supplier_id) REFERENCES suppliers (id),
    CONSTRAINT fk_reviews_order    FOREIGN KEY (order_id)    REFERENCES orders (id),
    CONSTRAINT fk_reviews_contract FOREIGN KEY (contract_id) REFERENCES contracts (id),
    CONSTRAINT ck_reviews_rating CHECK (rating BETWEEN 1 AND 5),
    CONSTRAINT ck_reviews_target CHECK ((order_id IS NOT NULL AND contract_id IS NULL)
                                     OR (order_id IS NULL AND contract_id IS NOT NULL))
);
CREATE UNIQUE INDEX uq_reviews_order    ON reviews (order_id)    WHERE order_id    IS NOT NULL;
CREATE UNIQUE INDEX uq_reviews_contract ON reviews (contract_id) WHERE contract_id IS NOT NULL;

CREATE TABLE reputation_logs (
    id          BIGINT       GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    supplier_id BIGINT       NOT NULL,
    event_type  VARCHAR(30)  NOT NULL,
    score_delta INT          NOT NULL,
    review_id   BIGINT       NULL,
    contract_id BIGINT       NULL,
    reason      TEXT         NULL,
    created_at  TIMESTAMPTZ(3) NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_replog_supplier FOREIGN KEY (supplier_id) REFERENCES suppliers (id) ON DELETE CASCADE,
    CONSTRAINT fk_replog_review   FOREIGN KEY (review_id)   REFERENCES reviews (id)   ON DELETE SET NULL,
    CONSTRAINT fk_replog_contract FOREIGN KEY (contract_id) REFERENCES contracts (id) ON DELETE SET NULL,
    CONSTRAINT ck_replog_event CHECK (event_type IN ('review', 'contract_on_time', 'contract_late',
                                                     'contract_cancelled', 'manual_adjustment'))
);

CREATE TABLE notifications (
    id          BIGINT       GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    user_id     BIGINT       NOT NULL,
    type        VARCHAR(100) NOT NULL,
    title       VARCHAR(255) NOT NULL,
    body        TEXT         NULL,
    entity_type VARCHAR(50)  NULL,
    entity_id   BIGINT       NULL,
    is_read     BOOLEAN      NOT NULL DEFAULT FALSE,
    read_at     TIMESTAMPTZ(3) NULL,
    created_at  TIMESTAMPTZ(3) NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_notifications_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
);

CREATE TABLE ai_conversations (
    id         BIGINT       GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    user_id    BIGINT       NOT NULL,
    title      VARCHAR(255) NULL,
    created_at TIMESTAMPTZ(3) NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ(3) NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_ai_conv_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
);

CREATE TABLE ai_messages (
    id              BIGINT       GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    conversation_id BIGINT       NOT NULL,
    role            VARCHAR(20)  NOT NULL,
    content         TEXT         NOT NULL,
    metadata        JSONB        NOT NULL DEFAULT '{}',
    created_at      TIMESTAMPTZ(3) NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_ai_msg_conv FOREIGN KEY (conversation_id) REFERENCES ai_conversations (id) ON DELETE CASCADE,
    CONSTRAINT ck_ai_msg_role CHECK (role IN ('user', 'assistant', 'system'))
);

CREATE TABLE ai_analytics_reports (
    id           BIGINT       GENERATED ALWAYS AS IDENTITY NOT NULL PRIMARY KEY,
    supplier_id  BIGINT       NOT NULL,
    report_type  VARCHAR(100) NOT NULL,
    period_start DATE         NULL,
    period_end   DATE         NULL,
    parameters   JSONB        NOT NULL DEFAULT '{}',
    result       JSONB        NULL,
    status       VARCHAR(20)  NOT NULL DEFAULT 'pending',
    created_at   TIMESTAMPTZ(3) NOT NULL DEFAULT NOW(),
    completed_at TIMESTAMPTZ(3) NULL,
    CONSTRAINT fk_ai_reports_supplier FOREIGN KEY (supplier_id) REFERENCES suppliers (id) ON DELETE CASCADE,
    CONSTRAINT ck_ai_reports_status CHECK (status IN ('pending', 'completed', 'failed')),
    CONSTRAINT ck_ai_reports_period CHECK (period_start IS NULL OR period_end IS NULL OR period_end >= period_start)
);

-- INDEXES
CREATE INDEX ix_shops_template             ON shops (template_id);
CREATE INDEX ix_categories_parent          ON categories (parent_id);
CREATE INDEX ix_products_shop              ON products (shop_id);
CREATE INDEX ix_products_category          ON products (category_id);
CREATE INDEX ix_variants_product           ON product_variants (product_id);
CREATE INDEX ix_product_images_product     ON product_images (product_id);
CREATE INDEX ix_promotion_rules_shop       ON promotion_rules (shop_id);
CREATE INDEX ix_promotion_rules_product    ON promotion_rules (product_id);
CREATE INDEX ix_custom_packagings_customer ON custom_packagings (customer_id);
CREATE INDEX ix_orders_customer            ON orders (customer_id, created_at DESC);
CREATE INDEX ix_orders_shop                ON orders (shop_id, status);
CREATE INDEX ix_order_items_order          ON order_items (order_id);
CREATE INDEX ix_order_items_variant        ON order_items (variant_id);
CREATE INDEX ix_rfq_customer               ON request_for_quotations (customer_id);
CREATE INDEX ix_rfq_status                 ON request_for_quotations (status);
CREATE INDEX ix_rfq_items_rfq              ON rfq_items (rfq_id);
CREATE INDEX ix_rfq_invitations_supplier   ON rfq_invitations (supplier_id);
CREATE INDEX ix_quotations_supplier        ON quotations (supplier_id);
CREATE INDEX ix_quotation_items_rfq_item   ON quotation_items (rfq_item_id);
CREATE INDEX ix_contracts_customer         ON contracts (customer_id);
CREATE INDEX ix_contracts_supplier         ON contracts (supplier_id, status);
CREATE INDEX ix_payments_order             ON payments (order_id);
CREATE INDEX ix_payments_milestone         ON payments (milestone_id);
CREATE INDEX ix_deliveries_order           ON deliveries (order_id);
CREATE INDEX ix_deliveries_contract        ON deliveries (contract_id);
CREATE INDEX ix_delivery_proofs_delivery   ON delivery_proofs (delivery_id);
CREATE INDEX ix_reviews_supplier           ON reviews (supplier_id);
CREATE INDEX ix_reviews_customer           ON reviews (customer_id);
CREATE INDEX ix_reputation_logs_supplier   ON reputation_logs (supplier_id, created_at DESC);
CREATE INDEX ix_notifications_user         ON notifications (user_id, is_read, created_at DESC);
CREATE INDEX ix_ai_conversations_user      ON ai_conversations (user_id);
CREATE INDEX ix_ai_messages_conversation   ON ai_messages (conversation_id, created_at);
CREATE INDEX ix_ai_reports_supplier        ON ai_analytics_reports (supplier_id);

-- SAMPLE DATA
INSERT INTO shop_templates (name, description, config) VALUES
    ('Truyền thống', 'Tông đỏ - vàng, hoa văn trống đồng, hợp thương hiệu lâu đời',
     '{"primary":"#B91C1C","accent":"#F59E0B","font":"Merriweather","layout":"classic"}'),
    ('Hiện đại tối giản', 'Nền trắng, ảnh sản phẩm lớn, hợp thương hiệu cao cấp',
     '{"primary":"#111827","accent":"#10B981","font":"Inter","layout":"minimal"}'),
    ('Quà tặng doanh nghiệp', 'Nhấn mạnh đặt số lượng lớn và đóng hộp theo yêu cầu',
     '{"primary":"#1D4ED8","accent":"#F59E0B","font":"Roboto","layout":"corporate"}');

INSERT INTO categories (name, slug) VALUES
    ('Bánh nướng',         'banh-nuong'),
    ('Bánh dẻo',           'banh-deo'),
    ('Bánh chay',          'banh-chay'),
    ('Bánh thập cẩm',      'banh-thap-cam'),
    ('Bánh nhân đặc biệt', 'banh-nhan-dac-biet'),
    ('Hộp quà cao cấp',    'hop-qua-cao-cap');

COMMIT;
