using System.ComponentModel.DataAnnotations;

namespace ShopNgocLan.Models
{

    public class CheckoutViewModel
    {
    
        public List<CheckoutItemViewModel> Items { get; set; } = new List<CheckoutItemViewModel>();
        

        public string HovaTen {  get; set; }
        public string SoDienThoai {  get; set; }
        public List<DiaChiGiaoHang> DiaChiGiaoHangs { get; set; } = new List<DiaChiGiaoHang>();
        public string address_choice { get; set; }

        public DiaChiGiaoHang NewAddress { get; set; }

        public string payment_method { get; set; }
        public int Phuongthucthanhtoan { get; set; } // ("COD" hoặc "BankTransfer")
        
        public decimal Tamtinh { get; set; }
        public decimal Magiamgia { get; set; }
        public decimal Sotiengiam { get; set; }
        public decimal Ship { get; set; }

        public decimal Tongtien { get; set; }

    }
}
