# Vấn đề còn mở

## 13. OPEN / chưa được phê duyệt — không tự bịa

| Mã | Vấn đề | Ảnh hưởng và hướng xử lý tạm |
|---|---|---|
| OPEN-01 | SePay trial, tương thích tài khoản MB cá nhân/TPBank, webhook thực, giá dịch vụ | Adapter/mock/sandbox; chưa bật tiền thật nếu chưa thử nghiệm |
| OPEN-05 | Tỷ lệ hoàn khi hủy/trả sớm, mốc thời gian, phụ phí thiệt hại, bồi thường | Chưa tự động tính/chi hoàn theo tỷ lệ không được duyệt; lưu policy placeholder |
| OPEN-07 | Thời hạn lưu CCCD/GPLX/passport, căn cứ pháp lý, dữ liệu xuyên biên giới, GPLX khách nước ngoài, hiệu lực xác nhận điện tử | Hạn chế truy cập, mã hóa/audit, chờ rà soát pháp lý; không bịa số ngày |
| CR-01 | Xác thực phê duyệt ngoại tuyến qua cuộc gọi | Không coi log do nhân viên tự khai là bằng chứng duy nhất |
| CR-02 | Hậu quả khi ngoại lệ chứng từ chuyển khoản không có tiền thực nhận | Ghi khoản cần đối soát; chuyển Admin xử lý, không giả settled |
| CR-03 | Xe thay trước/sau hủy tự động BR-46 | Tôn trọng BR-46; ghi rõ thứ tự chưa phê duyệt |
| CR-04 | Bằng chứng xác minh refund failed | Không retry khi chưa xác minh |
| OPS | Địa chỉ cửa hàng, số liên hệ/Zalo OA, Google Maps place ID, giá/fee thật, khu vực giao xe, email domain, review thật, phụ kiện, SLA cứu hộ, chính sách bảo hiểm | Cấu hình placeholder rõ ràng, không công bố thông tin bịa |


## Chủ trì cần xác nhận

| Chủ trì | Phạm vi | Ảnh hưởng |

|---|---|---|

| Chủ dự án + provider | OPEN-01 | Chặn thanh toán thật |

| Chủ dự án | OPEN-05, CR-02/03 | Chặn chính sách tài chính tự động |

| Chủ dự án + tư vấn pháp lý | OPEN-07 | Chặn giấy tờ và production pháp lý |

| Chủ dự án + bảo mật/vận hành | CR-01/04 | Chặn auto approval offline/retry refund thiếu chứng cứ |

| Chủ dự án + vận hành | OPS | Chặn công bố liên hệ/giá/SLA/review chưa xác minh |

| Chủ dự án + kỹ thuật | Identity + SQL + cookie đã duyệt 08/11; provisioning CLI/revocation đã triển khai và test. Còn MFA, recovery, UI cấp quyền và production key management | Không hỏi lại provider; các phần production còn là gate trước go-live |

| Chủ dự án + hạ tầng | SQL local có 16 bảng; SangRentalTestDb đã được duyệt và tạo để kiểm Identity/Catalog. Còn application login ít quyền, chứng chỉ production; migration Identity trên SangRentalDb chờ review riêng | Không cản kiểm thử test DB; chưa được coi là production-ready |

Không mục nào yêu cầu tự chốt để hoàn tất scaffold. Không chọn tỷ lệ hoàn hoặc số ngày lưu giấy tờ.
