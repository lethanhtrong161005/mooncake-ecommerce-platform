-- Baseline for the existing Supabase schema only. Do not run on a new or partial database.
-- The 31 initial tables and 109 indexes were verified against this database before use.
-- This script records the initial EF migration; it does not create or alter application tables.
BEGIN;
LOCK TABLE "__EFMigrationsHistory" IN SHARE ROW EXCLUSIVE MODE;
DO $$
DECLARE missing text;
BEGIN
    WITH required(name) AS (VALUES
            ('ai_analytics_reports'),
            ('ai_conversations'),
            ('ai_messages'),
            ('bid_items'),
            ('bids'),
            ('categories'),
            ('contract_deposit_rules'),
            ('contract_milestones'),
            ('contracts'),
            ('custom_packagings'),
            ('customer_profiles'),
            ('deliveries'),
            ('delivery_proofs'),
            ('notifications'),
            ('order_items'),
            ('orders'),
            ('payments'),
            ('product_images'),
            ('product_reviews'),
            ('product_variants'),
            ('products'),
            ('promotion_rules'),
            ('reputation_logs'),
            ('request_for_quotations'),
            ('reviews'),
            ('rfq_invitations'),
            ('rfq_items'),
            ('shop_templates'),
            ('shops'),
            ('supplier_profiles'),
            ('users')
    )
    SELECT string_agg(name, ', ') INTO missing
    FROM required
    WHERE to_regclass(format('%I.%I', current_schema(), name)) IS NULL;
    IF missing IS NOT NULL THEN
        RAISE EXCEPTION 'Cannot baseline: missing tables: %', missing;
    END IF;

    WITH required(name) AS (VALUES
            ('idx_aar_report_type'),
            ('idx_aar_requested_by'),
            ('idx_aar_status'),
            ('ai_conversations_session_token_key'),
            ('idx_aic_type'),
            ('idx_aic_user_id'),
            ('idx_aim_conversation_id'),
            ('idx_bi_bid_id'),
            ('idx_bi_rfq_item_id'),
            ('bids_bid_number_key'),
            ('idx_bids_rfq_id'),
            ('idx_bids_status'),
            ('idx_bids_supplier_id'),
            ('idx_bids_total_price'),
            ('idx_categories_parent_id'),
            ('idx_categories_sort_order'),
            ('idx_cdr_is_mandatory'),
            ('idx_cm_contract_id'),
            ('idx_cm_due_date'),
            ('idx_cm_is_deposit'),
            ('idx_cm_status'),
            ('contracts_contract_number_key'),
            ('idx_contracts_bid_id'),
            ('idx_contracts_customer_id'),
            ('idx_contracts_deposit_rule_id'),
            ('idx_contracts_order_id'),
            ('idx_contracts_pending_deposit'),
            ('idx_contracts_rfq_id'),
            ('idx_contracts_status'),
            ('idx_contracts_supplier_id'),
            ('idx_cpkg_order_id'),
            ('idx_cpkg_status'),
            ('idx_cp_user_id'),
            ('deliveries_delivery_number_key'),
            ('idx_del_contract_id'),
            ('idx_del_order_id'),
            ('idx_del_status'),
            ('idx_del_tracking_code'),
            ('idx_dp_delivery_id'),
            ('idx_dp_proof_type'),
            ('idx_notif_type'),
            ('idx_notif_unread'),
            ('idx_notif_user_id'),
            ('idx_oi_order_id'),
            ('idx_oi_product_id'),
            ('idx_oi_promotion_rule_id'),
            ('idx_oi_variant_id'),
            ('idx_orders_created_at'),
            ('idx_orders_customer_id'),
            ('idx_orders_shop_id'),
            ('idx_orders_status'),
            ('orders_order_number_key'),
            ('idx_pay_contract_id'),
            ('idx_pay_customer_id'),
            ('idx_pay_milestone_id'),
            ('idx_pay_order_id'),
            ('idx_pay_paid_at'),
            ('idx_pay_status'),
            ('payments_payment_number_key'),
            ('idx_pi_is_primary'),
            ('idx_pi_product_id'),
            ('idx_prev_admin_hidden'),
            ('idx_prev_customer_id'),
            ('idx_prev_like_count'),
            ('idx_prev_product_feed'),
            ('idx_prev_rating'),
            ('idx_prev_variant_id'),
            ('product_reviews_customer_id_order_item_id_key'),
            ('idx_pv_product_id'),
            ('idx_pv_stock_qty'),
            ('product_variants_sku_key'),
            ('idx_products_base_price'),
            ('idx_products_category_id'),
            ('idx_products_name_trgm'),
            ('idx_products_shop_id'),
            ('idx_products_status'),
            ('idx_pr_active'),
            ('idx_pr_product_id'),
            ('idx_pr_shop_id'),
            ('idx_rl_event_type'),
            ('idx_rl_reference'),
            ('idx_rl_supplier_id'),
            ('idx_rfq_bid_deadline'),
            ('idx_rfq_customer_id'),
            ('idx_rfq_status'),
            ('request_for_quotations_rfq_number_key'),
            ('idx_rev_contract_id'),
            ('idx_rev_customer_id'),
            ('idx_rev_order_id'),
            ('idx_rev_overall_rating'),
            ('idx_rev_supplier_id'),
            ('idx_rfqinv_rfq_id'),
            ('idx_rfqinv_status'),
            ('idx_rfqinv_supplier_id'),
            ('rfq_invitations_rfq_id_supplier_id_key'),
            ('idx_rfqi_rfq_id'),
            ('idx_shops_slug'),
            ('idx_shops_status'),
            ('idx_shops_supplier_id'),
            ('idx_shops_template_id'),
            ('shops_slug_key'),
            ('idx_sp_reputation_score'),
            ('idx_sp_user_id'),
            ('idx_sp_verified'),
            ('supplier_profiles_tax_code_key'),
            ('idx_users_email'),
            ('idx_users_is_active'),
            ('idx_users_role'),
            ('users_email_key')
    )
    SELECT string_agg(required.name, ', ') INTO missing
    FROM required
    WHERE NOT EXISTS (
        SELECT 1 FROM pg_indexes
        WHERE schemaname = current_schema() AND indexname = required.name
    );
    IF missing IS NOT NULL THEN
        RAISE EXCEPTION 'Cannot baseline: missing indexes: %', missing;
    END IF;
END $$;
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261004072545_InitialSupabaseSchema', '8.0.11')
ON CONFLICT ("MigrationId") DO NOTHING;
COMMIT;
