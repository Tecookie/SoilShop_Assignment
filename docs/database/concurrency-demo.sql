-- SQL-only executable design prototype. Never include in application EF migrations.
-- Tests the transaction boundary, NOT gRPC, JWT, AutoMapper or a full API implementation.
CREATE OR REPLACE FUNCTION design_confirm_sale_demo(p_sale integer, p_key uuid, p_hold numeric DEFAULT 0)
RETURNS text LANGUAGE plpgsql AS $$
DECLARE
    doc sales%ROWTYPE;
    ln record;
    amount numeric(18,0) := 0;
    new_qty integer;
    new_version bigint;
    current_price numeric(18,0);
BEGIN
    SELECT * INTO STRICT doc FROM sales WHERE id=p_sale FOR UPDATE;
    IF doc.status='Confirmed' AND doc.confirmation_key=p_key THEN RETURN 'AlreadyConfirmed'; END IF;
    IF doc.status<>'Draft' THEN RAISE EXCEPTION 'DOCUMENT_CONFLICT'; END IF;
    IF NOT EXISTS (SELECT 1 FROM sale_lines WHERE sale_id=p_sale) THEN RAISE EXCEPTION 'EMPTY_SALE'; END IF;
    FOR ln IN SELECT * FROM sale_lines WHERE sale_id=p_sale ORDER BY sku_id LOOP
        UPDATE skus SET stock_qty=stock_qty-ln.quantity, stock_version=stock_version+1
        WHERE id=ln.sku_id AND is_active AND stock_qty>=ln.quantity
        RETURNING stock_qty,stock_version,sale_price_vnd INTO new_qty,new_version,current_price;
        IF NOT FOUND THEN RAISE EXCEPTION 'INSUFFICIENT_STOCK'; END IF;
        UPDATE sale_lines SET unit_price_vnd=current_price,sku_label_snapshot='Design demo SKU' WHERE id=ln.id;
        amount:=amount+ln.quantity*current_price;
        INSERT INTO stock_movements(sku_id,kind,delta_qty,balance_after,stock_version_after,sale_id,actor_id)
        VALUES(ln.sku_id,'Sale',-ln.quantity,new_qty,new_version,p_sale,doc.created_by);
    END LOOP;
    PERFORM pg_sleep(p_hold::double precision);
    UPDATE sales SET status='Confirmed',confirmed_by=doc.created_by,confirmed_at=now(),confirmation_key=p_key,total_vnd=amount,version=version+1 WHERE id=p_sale;
    INSERT INTO outbox_messages(id,kind,payload) VALUES(p_key,'InventoryChanged',jsonb_build_object('saleId',p_sale));
    RETURN 'Confirmed';
END $$;

CREATE OR REPLACE FUNCTION design_approve_count_demo(p_count integer, p_key uuid, p_admin integer)
RETURNS text LANGUAGE plpgsql AS $$
DECLARE
    ct stock_counts%ROWTYPE;
    current_sku skus%ROWTYPE;
    delta integer;
    adjustment integer;
BEGIN
    SELECT * INTO STRICT ct FROM stock_counts WHERE id=p_count FOR UPDATE;
    IF ct.status='Approved' THEN RETURN 'AlreadyApproved'; END IF;
    IF ct.status<>'Pending' THEN RAISE EXCEPTION 'COUNT_STATE_CONFLICT'; END IF;
    SELECT * INTO STRICT current_sku FROM skus WHERE id=ct.sku_id FOR UPDATE;
    IF current_sku.stock_version<>ct.expected_stock_version THEN
        UPDATE stock_counts SET status='RecountRequired',reviewed_by=p_admin,reviewed_at=now(),version=version+1 WHERE id=p_count;
        RETURN 'RecountRequired';
    END IF;
    delta:=ct.actual_qty-ct.expected_qty;
    IF delta<>0 THEN
        UPDATE skus SET stock_qty=stock_qty+delta,stock_version=stock_version+1 WHERE id=ct.sku_id AND stock_qty+delta>=0 RETURNING * INTO current_sku;
        IF NOT FOUND THEN RAISE EXCEPTION 'COUNT_STOCK_CONFLICT'; END IF;
        INSERT INTO stock_adjustments(request_key,sku_id,stock_count_id,delta_qty,reason,created_by)
        VALUES(p_key,ct.sku_id,p_count,delta,ct.reason,p_admin) RETURNING id INTO adjustment;
        INSERT INTO stock_movements(sku_id,kind,delta_qty,balance_after,stock_version_after,adjustment_id,actor_id)
        VALUES(ct.sku_id,'Adjustment',delta,current_sku.stock_qty,current_sku.stock_version,adjustment,p_admin);
        INSERT INTO outbox_messages(id,kind,payload) VALUES(p_key,'InventoryChanged',jsonb_build_object('stockCountId',p_count));
    END IF;
    UPDATE stock_counts SET status='Approved',reviewed_by=p_admin,reviewed_at=now(),version=version+1 WHERE id=p_count;
    RETURN 'Approved';
END $$;
