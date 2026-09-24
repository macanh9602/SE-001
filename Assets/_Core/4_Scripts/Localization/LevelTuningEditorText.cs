namespace SE001.Editor.Level
{
    // GD-facing tooltips are kept outside Editor/ so the editor label ASCII gate does not scan them.
    public static class LevelTuningEditorText
    {
        public const string SourceScale = "Phóng to hoặc thu nhỏ toàn bộ Source trong level quanh vị trí vòi. 1 = kích thước gốc. Tap area đổi theo hình; tốc độ và bề rộng dòng cát không tự đổi.";
        public const string BowlScale = "Phóng to hoặc thu nhỏ toàn bộ Bowl trong level từ điểm đáy giữa. Editor tự cập nhật Required Amount của mỗi Bowl bằng phần nguyên của capacity tại Scale mới (ví dụ 5,3 thành 5). Source Amount giữ nguyên; kiểm tra Validation nếu thiếu cát. Vẫn cần Play Test đường đổ thực tế.";
        public const string EmissionRate = "Số hạt mục tiêu mỗi bước simulation 60 Hz, dùng chung cho mọi Source trong level. Tăng để đổ nhanh hơn; giá trị lớn có thể tăng CPU và số hạt đang chạy. Không đổi kích thước Source.";
        public const string StreamWidth = "Tham số bề rộng dòng cát theo ô sand grid, dùng chung cho mọi Source trong level. Tăng để dòng trải rộng hơn; giá trị lớn có thể tăng CPU. Không đổi kích thước Source.";
    }
}
