# Potting Soil Shop

Canonical business terminology for the new internal shop application. Previous real-estate terminology is superseded and remains available in Git history.

## Language

**Staff (Nhân viên)**:
Internal users who receive goods, create sales and query inventory.

**Product (Sản phẩm đất trồng)**:
A bagged potting-soil offering in the shop catalogue, available in one or more package variants.

**Product Type (Nhóm mặt hàng)**:
The primary catalogue group of a Product: general-purpose, foliage, flowering, succulent/cactus, orchid or seed-starting mix. Unclassified means the group is not supplied yet. A catalogue group describes the packaged offering; it does not certify plant suitability.

**Supplier (Nhà cung cấp)**:
A business supplying pre-packed soil bags for the shop to resell.

**Bag (Bao)**:
A pre-packed unit of soil bought and sold by the shop, rather than soil weighed or repacked at sale time.

**Stock (Tồn kho)**:
The shop's inventory of pre-packed bags.

**Admin / Owner**:
An internal user who also manages accounts, products and prices and can view all shop activity.

**Package Variant / SKU (Quy cách / Mã hàng)**:
A sellable packaging option of a Product, with its own code, prices and bag stock, such as a 5 kg bag.

**Sales Slip (Phiếu bán)**:
A staff-created record of goods sold, their quantities and their sale prices at confirmation time.

**Draft Sale (Phiếu nháp)**:
A sales slip awaiting confirmation; it does not reserve bags or reduce stock.

**Confirmed Sale (Phiếu đã xác nhận)**:
A completed sale whose bag quantities have been deducted from stock.

**Purchase Receipt (Phiếu nhập)**:
A staff-created record of bags received from a Supplier, including quantities and actual purchase unit prices; confirmation adds those quantities to stock.

**Cancelled Sale (Phiếu bán đã hủy)**:
A previously confirmed sale cancelled in full by an Admin with a reason, restoring the original bag quantities once.

**Stock Adjustment (Điều chỉnh tồn)**:
An Admin-authorized increase or decrease in bag counts, recorded with a reason such as damage or a physical-count discrepancy.

**Low-Stock Threshold (Ngưỡng sắp hết hàng)**:
The bag count at or below which a SKU should be flagged for replenishment.

**Daily Shop Summary (Thống kê hằng ngày)**:
A dated summary of effective sales revenue, bags sold and inventory levels.

**Product Advice (Tư vấn sản phẩm)**:
A ranked list of available products matching staff-selected needs, with reasons based on supplied product information.

**Metadata Source (Nguồn thông tin)**:
The label, supplier document or other recorded source supporting a product's descriptive information.

**Replenishment Suggestion (Đề xuất nhập hàng)**:
An internal suggestion to bring a low-stock SKU toward its target, optionally converted into a purchase-receipt draft.

**Target Stock (Tồn mục tiêu)**:
The desired number of bags for a SKU used to calculate suggested replenishment.

**Stock Count (Phiếu kiểm kê)**:
A recorded physical count compared to captured system stock, awaiting Admin review before any adjustment.
