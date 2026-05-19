namespace ProductInventory.API.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public string MaSanPham { get; set; } = string.Empty;
        public double Gia { get; set; }
        public int SoLuongTon { get; set; }
        public string DanhMuc { get; set; } = string.Empty;
        public string MoTa { get; set; } = string.Empty;
    }
}