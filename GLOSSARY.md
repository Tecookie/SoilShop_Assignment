# Broker Property Catalogue

Canonical business terminology. Vietnamese names preserve the language used during the interview.

## Language

**Employee (Nhân viên)**:
An internal staff member who maintains the broker's property catalogue.

**Broker Organization (Đơn vị môi giới)**:
The organization that manages units allocated by developers for brokerage; the supplied information may be incomplete.

**Property Catalogue (Danh mục bất động sản)**:
The collection of property information maintained for internal lookup and updates.

**Developer (Chủ đầu tư / CĐT)**:
An organization that owns or develops Projects and supplies inventory information to the broker.
_Avoid_: Software developer, customer

**Project (Dự án)**:
A collection of properties owned by one Developer, containing Buildings and Property Units.

**Building / Block (Tòa nhà)**:
A building in one Project with a defined floor count and associated Apartments.

**Property Unit (Căn)**:
An individually identifiable Apartment, Townhouse or Villa in the catalogue.
_Avoid_: Room, listing advertisement

**Apartment (Căn hộ)**:
A Property Unit in a specified Building and on a specified floor.

**Townhouse (Nhà phố)**:
A house directly associated with a Project, optionally having its own floor count.

**Villa (Biệt thự)**:
A villa directly associated with a Project, optionally having its own floor count and area details.

**Bulk Creation (Tạo căn hàng loạt)**:
Creation of multiple units of one type from shared information and a generated code sequence; prices are entered later.

**Unit Code (Mã căn)**:
A code that uniquely identifies a Property Unit within its Project.

**No Price Yet (Chưa có giá)**:
The absence of a supplied unit price, distinct from a price of zero.

**Managed (Đang quản lý)**:
The unit is currently allocated to the Broker Organization's catalogue.

**Unmanaged (Ngừng quản lý)**:
The unit is no longer allocated to the Broker Organization, such as when transferred to another broker.

**Sold (Đã bán)**:
The unit has been recorded as sold; this system does not process payment.

**Additional Information (Thông tin bổ sung)**:
Editable named descriptive values supplied in addition to the standard unit fields.

**Status History (Lịch sử trạng thái)**:
The record of management-status changes, including actor, time, previous/new status and reason.

**Admin**:
An internal staff member who also manages accounts and backup requests.

**Project Summary**:
A dated view of units in a Project by management status and managed units awaiting a price.
