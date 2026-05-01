-- ============================================
-- Migration: Tạo bảng payments cho lưu trữ giao dịch thanh toán
-- Chạy script này trên PostgreSQL database
-- ============================================

CREATE TABLE IF NOT EXISTS sales.payments (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id        UUID NOT NULL REFERENCES sales.orders(id) ON DELETE CASCADE,
    provider        VARCHAR(20) NOT NULL,           -- VNPAY, MOMO, ZALOPAY
    transaction_id  VARCHAR(100),                    -- Mã giao dịch từ gateway
    amount          DECIMAL(18, 2) NOT NULL,
    status          VARCHAR(20) NOT NULL DEFAULT 'PENDING',  -- PENDING, SUCCESS, FAILED
    response_code   VARCHAR(10),                     -- 00, 24, ...
    bank_code       VARCHAR(20),                     -- NCB, VCB, ...
    card_type       VARCHAR(20),                     -- ATM, QRCODE, ...
    pay_date        VARCHAR(20),                     -- yyyyMMddHHmmss format từ gateway
    raw_response    TEXT,                             -- Full JSON response để audit
    signature       TEXT,                             -- Chữ ký nhận được
    currency        VARCHAR(10) NOT NULL DEFAULT 'VND',
    created_at      TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMP
);

-- Index cho tra cứu nhanh
CREATE INDEX IF NOT EXISTS idx_payments_order_id ON sales.payments(order_id);
CREATE INDEX IF NOT EXISTS idx_payments_transaction_id ON sales.payments(transaction_id, provider);
CREATE INDEX IF NOT EXISTS idx_payments_status ON sales.payments(status);

COMMENT ON TABLE sales.payments IS 'Lưu trữ lịch sử giao dịch thanh toán từ các cổng (VNPay, MoMo, ZaloPay) để đối soát';
