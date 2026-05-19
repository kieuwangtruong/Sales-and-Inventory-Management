using Microsoft.AspNetCore.Mvc;
using ProductInventory.API.Models;

namespace ProductInventory.API.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductController : ControllerBase
    {
        private static List<Product> _products = new List<Product>
        {
            new Product { Id = 1, TenSanPham = "Laptop Dell XPS", MaSanPham = "SP001", Gia = 25000000, SoLuongTon = 10, DanhMuc = "Điện tử", MoTa = "Laptop cao cấp" },
            new Product { Id = 2, TenSanPham = "Chuột Logitech", MaSanPham = "SP002", Gia = 500000, SoLuongTon = 50, DanhMuc = "Phụ kiện", MoTa = "Chuột không dây" }
        };

        /// <summary>
        /// Lấy danh sách tất cả sản phẩm
        /// </summary>
        /// <param name="danhMuc">Lọc theo danh mục (tuỳ chọn)</param>
        /// <param name="tenSanPham">Lọc theo tên sản phẩm (tuỳ chọn)</param>
        [HttpGet]
        public IActionResult GetAll([FromQuery] string? danhMuc, [FromQuery] string? tenSanPham)
        {
            var result = _products.AsQueryable();
            if (!string.IsNullOrEmpty(danhMuc))
                result = result.Where(x => x.DanhMuc == danhMuc);
            if (!string.IsNullOrEmpty(tenSanPham))
                result = result.Where(x => x.TenSanPham.Contains(tenSanPham));
            return Ok(result.ToList());
        }

        /// <summary>
        /// Lấy thông tin sản phẩm theo ID
        /// </summary>
        /// <param name="id">Mã ID của sản phẩm</param>
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var product = _products.FirstOrDefault(x => x.Id == id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        /// <summary>
        /// Thêm sản phẩm mới vào kho
        /// </summary>
        /// <param name="product">Thông tin sản phẩm cần thêm</param>
        [HttpPost]
        public IActionResult Create(Product product)
        {
            product.Id = _products.Max(x => x.Id) + 1;
            _products.Add(product);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        /// <summary>
        /// Cập nhật thông tin sản phẩm theo ID
        /// </summary>
        /// <param name="id">Mã ID sản phẩm cần cập nhật</param>
        /// <param name="product">Thông tin mới của sản phẩm</param>
        [HttpPut("{id}")]
        public IActionResult Update(int id, Product product)
        {
            var existing = _products.FirstOrDefault(x => x.Id == id);
            if (existing == null) return NotFound();
            existing.TenSanPham = product.TenSanPham;
            existing.MaSanPham = product.MaSanPham;
            existing.Gia = product.Gia;
            existing.SoLuongTon = product.SoLuongTon;
            existing.DanhMuc = product.DanhMuc;
            existing.MoTa = product.MoTa;
            return NoContent();
        }

        /// <summary>
        /// Xóa sản phẩm khỏi kho theo ID
        /// </summary>
        /// <param name="id">Mã ID sản phẩm cần xóa</param>
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var product = _products.FirstOrDefault(x => x.Id == id);
            if (product == null) return NotFound();
            _products.Remove(product);
            return NoContent();
        }

        /// <summary>
        /// Cập nhật số lượng tồn kho của sản phẩm
        /// </summary>
        /// <param name="id">Mã ID sản phẩm</param>
        /// <param name="soLuong">Số lượng cần cập nhật (âm để giảm)</param>
        [HttpPatch("{id}/inventory")]
        public IActionResult UpdateInventory(int id, [FromQuery] int soLuong)
        {
            var product = _products.FirstOrDefault(x => x.Id == id);
            if (product == null) return NotFound();
            product.SoLuongTon += soLuong;
            if (product.SoLuongTon < 0) product.SoLuongTon = 0;
            return Ok(product);
        }
    }
}   