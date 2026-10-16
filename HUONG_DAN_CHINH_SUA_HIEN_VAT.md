# HƯỚNG DẪN QUẢN LÝ & CHỈNH SỬA THÔNG TIN 59 HIỆN VẬT GỐM SỨ

Dự án **MuseumHaNoiVR** đã được tích hợp bộ khung cơ sở dữ liệu mẫu (Template Framework) cho toàn bộ **59 hiện vật gốm sứ** phân chia theo 4 tủ trưng bày (`Tu1`, `tu2`, `tu3`, `tu4`).

---

## 📁 1. VỊ TRÍ FILE DỮ LIỆU CHÍNH

* Đường dẫn: **`Assets/Resources/PotteryData.json`**
* Bạn có thể mở file này bằng **VS Code**, **Sublime Text**, **Notepad** hoặc bất kỳ trình soạn thảo văn bản nào.

---

## 🏷️ 2. QUY ƯỚC MÃ ĐỊNH DANH (UNIQUE ID)

Để tránh nhầm lẫn giữa các tủ có cùng số thứ tự (ví dụ: Tủ 2, Tủ 3 và Tủ 4 đều có hiện vật số 7), hệ thống sử dụng quy ước mã hóa: **`[Tủ]_[Số]`**:

| Tủ trưng bày | Số lượng | Danh sách mã ID (`id`) | Tên file 3D gốc |
| :--- | :---: | :--- | :--- |
| **Tủ 1** *(Thời Lý - Trần)* | **13** | `Tu1_01` $\rightarrow$ `Tu1_26` | `1.glb`, `2.glb`, `3.glb`, `4.glb`, `5-1.glb`, `5-2.glb`, `5-3.glb`, `22-1.glb`, `22-2.glb`, `23.glb`, `24.glb`, `25.glb`, `26.glb` |
| **Tủ 2** *(Thời Lê Sơ - Mạc)* | **16** | `Tu2_06` $\rightarrow$ `Tu2_21` | `6.fbx`, `7.fbx`, `8.fbx`, `9.fbx`, `10.fbx`, `11.fbx`, `13.fbx`, `14.fbx`, `15.fbx`, `16.fbx`, `17.fbx`, `18_01`, `18_02`, `19.fbx`, `20.fbx`, `21.fbx` |
| **Tủ 3** *(Men Ngọc & Trắng)* | **12** | `Tu3_04_1` $\rightarrow$ `Tu3_11_2` | `4-1.glb`, `4-2.glb`, `5-1.glb`, `5-2.glb`, `6-1.glb`, `6-2.glb`, `7.glb`, `8.glb`, `9.glb`, `10.glb`, `11-1.glb`, `11-2.glb` |
| **Tủ 4** *(Gốm Chu Đậu & Bát Tràng)* | **18** | `Tu4_07` $\rightarrow$ `Tu4_21` | `7.glb`, `8.glb`, `9.glb`, `10.glb`, `11.glb`, `12.glb`, `13.glb`, `14-1.glb`, `14-2.glb`, `14-3.glb`, `14-4.glb`, `15.glb`, `16.glb`, `17.glb`, `18.glb`, `19.glb`, `20.glb`, `21.glb` |

---

## ✍️ 3. CÁCH CHỈNH SỬA THÔNG TIN HIỆN VẬT

Mỗi hiện vật trong file `PotteryData.json` có định dạng như sau:

```json
{
  "id": "Tu1_01",
  "cabinet": "Tủ 1 (Thời Lý - Trần)",
  "number": "1",
  "modelFile": "1.glb",
  "artifactName": "Thạp gốm men hoa nâu",
  "material": "Gốm men hoa nâu",
  "period": "Thời Trần (Thế kỷ XIII - XIV)",
  "origin": "Việt Nam",
  "notableFeatures": "Dáng hình trụ đứng, có gờ miệng, hoa văn cánh sen và lá đề chạm khắc men nâu nổi bật.",
  "description": "Thạp gốm men hoa nâu là một hiện vật gốm sứ quý giá thuộc Tủ 1. Mang đậm giá trị văn hóa nghệ thuật của người Việt."
}
```

### Các trường bạn có thể tự do thay đổi:
* **`artifactName`**: Tên chính thức của hiện vật (hiển thị nổi bật ở đầu bảng thông tin).
* **`material`**: Chất liệu men gốm (ví dụ: *Gốm men ngọc Celadon*, *Gốm hoa lam*, *Đất nung*...).
* **`period`**: Niên đại lịch sử (ví dụ: *Thời Lý - Trần (Thế kỷ XI - XIII)*, *Thời Lê Sơ (Thế kỷ XV)*...).
* **`origin`**: Xuất xứ / Địa điểm khảo cổ (ví dụ: *Lò gốm Chu Đậu - Hải Dương*, *Hoàng thành Thăng Long*...).
* **`notableFeatures`**: Mô tả vắn tắt dáng vóc, hoa văn đặc trưng.
* **`description`**: Bài văn thuyết minh chi tiết về giá trị văn hóa, lịch sử và nghệ thuật chế tác.

> ⚠️ **LƯU Ý:** Không thay đổi trường `"id"` vì hệ thống dùng `"id"` để khớp nối chính xác với từng mô hình 3D trong Scene.

---

## 🔄 4. CÁCH ÁP DỤNG DỮ LIỆU VÀO UNITY

Bạn có 2 cách cực kỳ đơn giản:

### Cách 1: Tự động 100% khi nhấn Play (Khuyên dùng)
* Sau khi chỉnh sửa xong file `PotteryData.json` và lưu lại (`Ctrl+S` / `Cmd+S`).
* Bạn chỉ cần mở Unity và bấm nút **Play**: Script `PotteryItem` trên từng chiếc bình sẽ tự động đọc dữ liệu mới nhất từ file JSON và cập nhật lên màn hình!

### Cách 2: Đồng bộ vĩnh viễn vào Scene qua Menu công cụ
* Trên thanh menu Unity Editor, chọn:
  * **`Tools` > `Museum VR` > `Đồng bộ lại dữ liệu từ PotteryData.json vào Scene`**.
* Toàn bộ các thông số mới sẽ được lưu trực tiếp vào từng GameObject trong Scene.

---

## 🎮 5. TRẢI NGHIỆM TƯƠNG TÁC TRONG GAME

1. **Tiến lại gần tủ kính** (cự ly < 3.5m).
2. **Hướng tâm ngắm (chấm trắng giữa màn hình)** vào bình gốm bất kỳ:
   * Chiếc bình sẽ sáng nhẹ viền men (Highlight).
   * Phía dưới màn hình xuất hiện thông báo: *`🔍 Nhấn [E] hoặc Click chuột: Xem chi tiết [Tên bình gốm]`*.
3. **Nhấn phím `E` hoặc Click chuột trái**:
   * Bảng thông tin chi tiết hiện lên ở giữa màn hình.
   * Nhân vật tự động tạm dừng di chuyển và nhả chuột để bạn đọc thông tin.
4. **Nhấn `E`, `Esc` hoặc bấm nút `Đóng`**:
   * Đóng bảng thông tin và tiếp tục chuyến tham quan bảo tàng.
