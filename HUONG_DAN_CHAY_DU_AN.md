# HƯỚNG DẪN CÀI ĐẶT & CHẠY DỰ ÁN TỪ FILE EXPORT `.unitypackage`
## Dự án: Bảo Tàng Hà Nội VR - Không Gian Trưng Bày Gốm Sứ Thời Lý (MuseumHaNoiVR)

---

> [!CRITICAL]
> ### ⚠️ TƯ DUY PHẢN BIỆN & LƯU Ý SỐNG CÒN TRƯỚC KHI THỰC HIỆN
> 1. **Bản chất của file `.unitypackage`**:
>    * File `.unitypackage` **CHỈ chứa các tài nguyên mã nguồn (Assets)** như Script, 3D Models, Materials, Prefabs, Audio và Scenes.
>    * File này **KHÔNG BAO GỒM** các cấu hình hệ thống nằm trong thư mục `ProjectSettings/` (chẳng hạn: Cấu hình Render Pipeline URP, Input System Settings, Graphics Settings, Editor Build Settings).
> 2. **Sai lầm phổ biến nhất**:
>    * Người nhận thường tạo một Project Unity rỗng dạng **3D Standard (Built-in)** rồi import gói vào. Kết quả: **Toàn bộ bảo tàng bị lỗi màu tím hồng (Pink Materials / Shader error)** và **nhân vật không di chuyển được** do thiếu New Input System.
> 3. **Yêu cầu môi trường chuẩn 100%**:
>    * **Phiên bản Unity**: Khuyến nghị **Unity 6 (6000.x.x)** (Phiên bản phát triển gốc: `6000.5.10f1`). Tối thiểu từ `6000.0.0+`.
>    * **Loại Template**: BẮT BUỘC chọn template **3D (URP) - Universal Render Pipeline** khi tạo Project trên Unity Hub.
> 4. **Mục đích chia sẻ**:
>    * Nếu bạn muốn chia sẻ cho người không biết dùng Unity (giảng viên chấm thi, ban giám khảo, người trải nghiệm thông thường), cách tối ưu nhất là gửi **Bản Build thực thi độc lập (Standalone Build `.exe` cho Windows hoặc `.app` cho macOS)** (xem Phần 4). File `.unitypackage` chỉ dành cho người làm kỹ thuật/lập trình viên mở trong Unity Editor.

---

## PHẦN 1: QUY TRÌNH IMPORT VÀ CHẠY DỰ ÁN (TỪNG BƯỚC CHI TIẾT)

### Bước 1: Khởi tạo Project đúng chuẩn trên Unity Hub

1. Mở phần mềm **Unity Hub**.
2. Nhấn nút **Projects** ở menu bên trái -> Nhấn nút **New project** (màu xanh ở góc trên bên phải).
3. Tại danh sách phiên bản Editor: Chọn **Unity 6 (6000.x)**.
4. Tại danh mục Template bên trái, chọn tab **3D**, sau đó chọn đúng template:
   * 👉 **3D (URP) Core** (Universal Render Pipeline).
   * *(Lưu ý: Tuyệt đối KHÔNG chọn template "3D Core" thông thường)*.
5. Ở bảng thông tin bên phải:
   * **Project name**: Đặt tên (ví dụ: `MuseumHaNoiVR_Test`).
   * **Location**: Chọn thư mục lưu trữ trên ổ cứng (ưu tiên ổ SSD, tránh đường dẫn có dấu tiếng Việt).
6. Nhấn nút **Create project** và chờ Unity tải môi trường làm việc ban đầu.

---

### Bước 2: Kiểm tra các Package nền tảng (Bắt buộc)

Dự án sử dụng kiến trúc hiện đại của Unity 6 với hệ thống Input System và Render Pipeline mới:

1. Vào menu thanh công cụ: **Window** -> **Package Manager**.
2. Chuyển bộ lọc ở góc trên bên trái sang **Packages: Unity Registry**.
3. Kiểm tra các gói sau (nếu chưa cài thì bấm `Install`):
   * **Universal RP**: Mặc định đã có sẵn trong template 3D URP.
   * **Input System (`com.unity.inputsystem`)**: Đảm bảo package này đã được cài đặt. Nếu Unity hiện hộp thoại:
     > *"This project is using the new input system... Do you want to restart the editor?"*  
     👉 Chọn **Yes** để Unity khởi động lại và kích hoạt New Input Backend.
   * **glTFast (`com.unity.cloud.gltfast`)**: Hỗ trợ tối ưu hóa và đọc nạp mô hình 3D (nếu có).

---

### Bước 3: Import file `.unitypackage` vào Project

1. Khi cửa sổ Unity Editor đã sẵn sàng, bạn có 2 cách để Import:
   * **Cách 1 (Nhanh nhất)**: Kéo trực tiếp file `... .unitypackage` từ File Explorer / Finder thả thẳng vào khung cửa sổ **Project** (khu vực thư mục `Assets`) của Unity.
   * **Cách 2**: Trên thanh Menu, chọn **Assets** -> **Import Package** -> **Custom Package...** -> Tìm và chọn file `.unitypackage` vừa tải về.
2. Hộp thoại **"Import Unity Package"** xuất hiện liệt kê tất cả các file trong gói:
   * Nhìn xuống góc dưới cùng bên trái của hộp thoại, nhấn nút **All** (để tích chọn 100% tất cả các file Assets, Scripts, Materials, Scenes, Prefabs).
   * Nhấn nút **Import** ở góc dưới cùng bên phải.
3. Chờ từ 1 đến 3 phút để Unity giải nén, thiết lập GUID metadata và tự động biên dịch lại mã nguồn C#.

---

### Bước 4: Mở đúng Scene Bảo Tàng (Tránh bẫy Scene rỗng)

> [!WARNING]
> Sau khi import xong, màn hình Unity vẫn sẽ hiển thị Scene mặc định ban đầu (`SampleScene` trống, không có bảo tàng). Đừng hoảng hốt! Hãy thực hiện thao tác sau:

1. Trong cửa sổ **Project** (phía dưới màn hình), tìm và mở thư mục:
   * 👉 **`Assets`** -> **`Scenes`**
2. Nhấp đúp chuột (Double click) vào file Scene:
   * 🏛️ **`MuseumScene.unity`** (Dung lượng lớn, chứa toàn bộ không gian triển lãm thực tế).
3. Ngay lập tức, toàn bộ không gian kiến trúc Bảo tàng Hà Nội, hệ thống 5 tủ kính trưng bày đồ gốm sứ Thời Lý, các mảng kính văn hóa, hệ thống chiếu sáng và người chơi sẽ xuất hiện đầy đủ trong cửa sổ Scene và Game.

---

### Bước 5: Trải nghiệm & Phím tắt điều khiển (Controls)

Nhấn nút **Play (▶)** ở giữa thanh công cụ phía trên cùng của Unity Editor để bắt đầu tham quan:

| Hành động | Phím điều khiển | Chức năng chi tiết |
| :--- | :--- | :--- |
| **Di chuyển** | `W`, `A`, `S`, `D` | Tiến, Lùi, Sang Trái, Sang Phải trong không gian triển lãm |
| **Xoay góc nhìn** | **Di chuyển Chuột** | Xoay Camera 360 độ theo góc nhìn thứ nhất (First-Person) |
| **Chạy nhanh** | Giữ phím `Left Shift` | Tăng tốc độ di chuyển khám phá không gian rộng |
| **Khóa/Mở trỏ chuột** | `Click chuột trái` | Ẩn/Hiện con trỏ chuột để điều hướng mượt mà |
| **Chiêm ngưỡng 3D** | `Phím E` hoặc `Click chuột trái` | Khi đứng gần tủ kính và nhìn vào hiện vật gốm sứ: Nhấc hiện vật ra chế độ quan sát 3D 360° & Mở bảng thông tin văn hóa |
| **Xoay hiện vật 3D** | `Giữ chuột trái + Kéo` | Xoay tự do mọi góc cạnh của cổ vật gốm sứ trong chế độ Inspect |
| **Di chuyển hiện vật** | `Giữ chuột phải + Kéo` | Pan/Dịch chuyển vị trí hiện vật trên màn hình |
| **Thu phóng (Zoom)** | `Lăn con lăn chuột (Scroll)` | Zoom in / Zoom out để soi cận cảnh từng đường nét hoa văn |
| **Thoát / Đóng bảng** | `Phím ESC` | Đóng chế độ xem 3D, trả hiện vật về vị trí cũ trong tủ kính và tiếp tục tham quan |

---

## PHẦN 2: XỬ LÝ SỰ CỐ THƯỜNG GẶP (TROUBLESHOOTING)

### 1. Sự cố: Toàn bộ đồ vật / phòng bị màu tím hồng (Pink / Magenta Shader)
* **Nguyên nhân**: Project đang chạy ở chế độ Render Pipeline cũ (Built-in) thay vì URP.
* **Cách xử lý**:
  * Đảm bảo Project được tạo bằng template **3D (URP)**.
  * Nếu lỡ tạo bằng 3D Standard: Vào menu **Window** -> **Package Manager** -> Cài đặt `Universal RP`.
  * Sau đó vào **Edit** -> **Rendering** -> **Materials** -> Chọn **Convert Selected Built-in Materials to URP** (hoặc gán URP Asset vào `Project Settings -> Graphics`).

### 2. Sự cố: Bấm WASD nhưng nhân vật đứng yên, không xoay được chuột
* **Nguyên nhân**: Chưa kích hoạt New Input System trong cấu hình người chơi.
* **Cách xử lý**:
  1. Vào menu **Edit** -> **Project Settings...** -> Chọn mục **Player** ở cột trái.
  2. Cuộn xuống phần **Other Settings**, tìm dòng **Active Input Handling**.
  3. Chọn giá trị: **Input System Package (New)** hoặc **Both**.
  4. Bấm **Apply** và đồng ý để Unity Restart lại Editor.

### 3. Sự cố: Bảng thông tin hiện vật bị mất chữ hoặc hiện ô vuông màu hồng
* **Nguyên nhân**: Thiếu bộ tài nguyên cốt lõi TextMesh Pro (TMP Essentials).
* **Cách xử lý**:
  1. Vào menu **Window** -> **TextMeshPro** -> **Import TMP Essential Resources**.
  2. Bấm **Import** khi cửa sổ hiện lên.

### 4. Sự cố: Bị chặn Playmode do lỗi "All compiler errors have to be fixed"
* **Nguyên nhân**: Có thể phiên bản Unity quá cũ (dưới 2022) không hỗ trợ một số API C# hiện đại.
* **Cách xử lý**:
  * Kiểm tra Console để xem chi tiết lỗi đỏ.
  * Khuyến nghị nâng cấp lên **Unity 6 (6000.x.x)** để tương thích 100% không cần chỉnh sửa bất kỳ dòng code nào.

---

## PHẦN 3: CÁCH XUẤT BẢN THỰC THI (STANDALONE BUILD) ĐỂ CHIA SẺ TIỆN LỢI NHẤT

Nếu bạn muốn gửi sản phẩm cho giảng viên hoặc người dùng cuối để họ **chỉ cần nhấp đúp chuột là chạy ngay** mà không cần phải cài đặt Unity, hãy làm theo các bước sau:

1. Trong Unity Editor, chọn menu **File** -> **Build Profiles** (hoặc **Build Settings...** trên Unity cũ).
2. Tại khung **Scenes In Build**:
   * Kéo file `Assets/Scenes/MuseumScene.unity` thả vào danh sách.
   * Bỏ tích (hoặc xóa) `SampleScene`. Đảm bảo `MuseumScene` nằm ở vị trí số **0**.
3. Tại khung **Platform**: Chọn **Windows** (hoặc **macOS** tùy máy người nhận).
4. Nhấn nút **Build** -> Chọn thư mục xuất file (ví dụ tạo thư mục `Build_MuseumVR/`).
5. Kết quả thu được:
   * Với Windows: Thư mục chứa file `MuseumHaNoiVR.exe` và thư mục data đi kèm. Nén cả thư mục này thành file `.zip` và gửi cho người nhận. Người nhận chỉ cần giải nén rồi nhấp đúp file `.exe` là có thể trải nghiệm ngay lập tức với đầy đủ đồ họa, âm thanh và chuyển động mượt mà!
